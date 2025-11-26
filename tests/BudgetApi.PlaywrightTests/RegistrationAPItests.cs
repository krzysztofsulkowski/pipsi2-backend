using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class RegistrationAPITests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;
    private const string Port = "53568";
    private const string HttpsUrl = $"https://localhost:{Port}";
    private const string HttpUrl = $"http://localhost:{Port}";
    private string _baseUrl = HttpsUrl;

    private const string Password = "Test123!@#";

    [OneTimeSetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _request = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Content-Type", "application/json" }
            }
        });

        Console.WriteLine($"[Setup] Registration API tests initialized. BaseURL={_baseUrl}");
    }

    // Test 1(Registration): Register should return error when email already exists
    [Test, Order(1)]
    public async Task Register_Should_Return_Error_When_Email_Already_Exists()
    {
        // 1. Generate email
        var email = $"regtest_{Guid.NewGuid()}@example.com";
        var username = email.Split('@')[0];

        Console.WriteLine("[Test] Start: duplicate email test");
        Console.WriteLine($"[Test] Using email: {email}");

        var payload = new
        {
            email,
            username,
            password = Password
        };

        // 2. First account creation (should return 200)
        Console.WriteLine($"[Test] FIRST registration attempt for {email}");
        var first = await _request.PostAsync("/api/authentication/register", new() { DataObject = payload });
        var firstStatus = first.Status;
        var firstBody = await first.TextAsync();

        Console.WriteLine($"[Test] First status: {firstStatus}");
        Console.WriteLine($"[Test] First body: {firstBody}");

        Assert.That(firstStatus, Is.InRange(200, 299),
            $"Initial registration failed unexpectedly: HTTP {firstStatus}\n{firstBody}");

        // 3. Second account creation on the same email (should return 400 - user already exists!)
        Console.WriteLine($"[Test] SECOND registration attempt (should fail)");
        var second = await _request.PostAsync("/api/authentication/register", new() { DataObject = payload });
        var secondStatus = second.Status;
        var secondBody = await second.TextAsync();

        Console.WriteLine($"[Test] Second status: {secondStatus}");
        Console.WriteLine($"[Test] Second body: {secondBody}");

        Assert.That(secondStatus, Is.EqualTo(400).Or.EqualTo(409),
            $"Expected error for duplicate email, got HTTP {secondStatus}\n{secondBody}");

        Console.WriteLine("[Test] OK: Duplicate email correctly rejected.");
    }

    [Test, Order(2)]
    public async Task Register_Should_Return_Error_When_Username_Already_Exists()
    {
        var baseUsername = $"reglogin_{Guid.NewGuid():N}".Substring(0, 16);
        var email1 = $"{baseUsername}_1@example.com";
        var email2 = $"{baseUsername}_2@example.com";

        Console.WriteLine("[Test 2] Start: duplicate username test");
        Console.WriteLine($"[Test 2] Using username: {baseUsername}");
        Console.WriteLine($"[Test 2] First email: {email1}");
        Console.WriteLine($"[Test 2] Second email: {email2}");

        var payloadFirst = new
        {
            email = email1,
            username = baseUsername,
            password = Password
        };

        Console.WriteLine("[Test 2] FIRST registration attempt");
        var first = await _request.PostAsync("/api/authentication/register", new() { DataObject = payloadFirst });
        var firstStatus = first.Status;
        var firstBody = await first.TextAsync();

        Console.WriteLine($"[Test 2] First status: {firstStatus}");
        Console.WriteLine($"[Test 2] First body: {firstBody}");

        Assert.That(firstStatus, Is.InRange(200, 299),
            $"Initial registration with username failed unexpectedly: HTTP {firstStatus}\n{firstBody}");

        var payloadSecond = new
        {
            email = email2,
            username = baseUsername,
            password = Password
        };

        Console.WriteLine("[Test 2] SECOND registration attempt with same username (should fail)");
        var second = await _request.PostAsync("/api/authentication/register", new() { DataObject = payloadSecond });
        var secondStatus = second.Status;
        var secondBody = await second.TextAsync();

        Console.WriteLine($"[Test 2] Second status: {secondStatus}");
        Console.WriteLine($"[Test 2] Second body: {secondBody}");

        Assert.That(secondStatus, Is.EqualTo(400).Or.EqualTo(409),
            $"Expected error for duplicate username, got HTTP {secondStatus}\n{secondBody}");

        Console.WriteLine("[Test 2] OK: Duplicate username correctly rejected.");
    }

    // Test 3(Registration): Register should return error when email is empty
    [Test, Order(3)]
    public async Task Register_Should_Return_Error_When_Email_Is_Empty()
    {
        var uniqueUsername = $"empty_{Guid.NewGuid():N}".Substring(0, 16);

        var payload = new
        {
            email = "",
            username = uniqueUsername,
            password = Password
        };

        Console.WriteLine("[Test 3] Start: empty email test");
        Console.WriteLine($"[Test 3] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/register", new() { DataObject = payload });
        var status = response.Status;
        var body = await response.TextAsync();
        var lower = body.ToLowerInvariant();

        Console.WriteLine($"[Test 3] HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Body: {body}");

        Assert.That(status, Is.InRange(400, 499),
            $"Expected validation error for empty email, got HTTP {status}\n{body}");

        Assert.That(lower.Contains("already exists"), Is.False,
            $"API returned duplicate-user error instead of empty-email validation error:\n{body}");

        Console.WriteLine("[Test 3] OK: Empty email correctly rejected.");
    }



    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
