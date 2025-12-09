using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class ForgotPasswordAPITests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;
    private string _baseUrl = TestBackendConfig.HttpsUrl;

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

        Console.WriteLine($"[Setup] ForgotPassword API tests initialized. BaseURL={_baseUrl}");
    }

    // Test 1(ForgotPassword): Forgot password should return 200 for existing email
    [Test, Order(1)]
    public async Task ForgotPassword_Should_Return_200_For_Existing_Email()
    {
        var email = $"fp_existing_{Guid.NewGuid()}@example.com";
        var username = email.Split('@')[0];
        var password = $"Test123!@#{Guid.NewGuid():N}";

        var registerPayload = new
        {
            email,
            username,
            password
        };

        Console.WriteLine("[Test 1] Start: register user for forgot-password test");
        Console.WriteLine($"[Test 1] Register payload: {JsonSerializer.Serialize(registerPayload)}");

        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registerPayload });

        var registerStatus = registerResponse.Status;
        var registerBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 1] Register HTTP Status: {registerStatus}");
        Console.WriteLine($"[Test 1] Register Body: {registerBody}");

        Assert.That(registerStatus, Is.InRange(200, 299),
            $"User registration failed before forgot-password test: HTTP {registerStatus}\n{registerBody}");

        var forgotPayload = new
        {
            email
        };

        Console.WriteLine("[Test 1] Calling forgot-password for existing email");
        Console.WriteLine($"[Test 1] Forgot-password payload: {JsonSerializer.Serialize(forgotPayload)}");

        var response = await _request.PostAsync("/api/authentication/forgot-password",
            new() { DataObject = forgotPayload });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Forgot-password HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Forgot-password Body: {body}");

        Assert.That(status, Is.EqualTo(200),
            $"Expected 200 for existing email, got HTTP {status}\n{body}");
    }

    // Test 2(ForgotPassword): Forgot password should return 200 for non-existing email
    [Test, Order(2)]
    public async Task ForgotPassword_Should_Return_200_For_NonExisting_Email()
    {
        var email = $"fp_nonexisting_{Guid.NewGuid()}@example.com";

        var payload = new
        {
            email
        };

        Console.WriteLine("[Test 2] Start: forgot-password for NON-existing email");
        Console.WriteLine($"[Test 2] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/forgot-password",
            new() { DataObject = payload });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Body: {body}");

        Assert.That(status, Is.EqualTo(200),
            $"Expected 200 for non-existing email, got HTTP {status}\n{body}");
    }

    // Test 3(ForgotPassword): Forgot password should return validation error when email is empty
    [Test, Order(3)]
    public async Task ForgotPassword_Should_Return_Error_When_Email_Is_Empty()
    {
        var payload = new
        {
            email = ""
        };

        Console.WriteLine("[Test 3] Start: forgot-password with EMPTY email");
        Console.WriteLine($"[Test 3] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/forgot-password",
            new() { DataObject = payload });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Body: {body}");

        Assert.That(status, Is.InRange(400, 499),
            $"Expected validation error (400–499) for empty email, got HTTP {status}\n{body}");
    }

    // Test 4(ForgotPassword): Forgot password should return validation error for invalid email format
    [Test, Order(4)]
    public async Task ForgotPassword_Should_Return_Error_For_Invalid_Email_Format()
    {
        var payload = new
        {
            email = "not-an-email"
        };

        Console.WriteLine("[Test 4] Start: forgot-password with INVALID email format");
        Console.WriteLine($"[Test 4] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/forgot-password",
            new() { DataObject = payload });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 4] HTTP Status: {status}");
        Console.WriteLine($"[Test 4] Body: {body}");

        Assert.That(status, Is.InRange(400, 499),
            $"Expected validation error (400–499) for invalid email format, got HTTP {status}\n{body}");
    }

    // Test 5(ForgotPassword): Forgot password should return validation error when email field is missing
    [Test, Order(5)]
    public async Task ForgotPassword_Should_Return_Error_When_Email_Is_Missing()
    {
        var payload = new { };

        Console.WriteLine("[Test 5] Start: forgot-password with MISSING email field");
        Console.WriteLine("[Test 5] Payload: {} (email field not included)");

        var response = await _request.PostAsync("/api/authentication/forgot-password",
            new() { DataObject = payload });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 5] HTTP Status: {status}");
        Console.WriteLine($"[Test 5] Body: {body}");

        Assert.That(status, Is.InRange(400, 499),
            $"Expected validation error (400–499) for missing email field, got HTTP {status}\n{body}");
    }


    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
