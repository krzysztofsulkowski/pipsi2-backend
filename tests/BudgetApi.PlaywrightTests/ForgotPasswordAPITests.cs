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

    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
