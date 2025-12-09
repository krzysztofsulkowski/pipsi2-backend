using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class LoginAPITests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;

    // Backend port
    private const string Port = "64052";

    // HTTPS + HTTP fallback
    private const string HttpsUrl = $"https://localhost:{Port}";
    private const string HttpUrl = $"http://localhost:{Port}";
    private string _baseUrl = HttpsUrl;

    [OneTimeSetUp]
    public async Task Setup()
    {
        // Initialize Playwright & API context
        _playwright = await Playwright.CreateAsync();

        _request = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true, // allow localhost self-signed certs
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Content-Type", "application/json" }
            }
        });

        Console.WriteLine($"[Setup] Login API tests initialized. BaseURL={_baseUrl}");
    }

    // Test 1(Login): should succeed for valid registered user
    [Test, Order(1)]
    public async Task Login_Should_Succeed_For_Registered_User()
    {
        // Auto-generated credentials
        var email = $"login_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var password = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Payload for registration
        var registrationPayload = new
        {
            email,
            username,
            password
        };

        Console.WriteLine("[Test 1] Registering test user before login");
        Console.WriteLine($"[Test 1] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        IAPIResponse registerResponse;

        try
        {
            // Attempt registration over HTTPS
            registerResponse = await _request.PostAsync("/api/authentication/register",
                new() { DataObject = registrationPayload });
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"[Test 1] HTTPS registration failed: {ex.Message}");
            Console.WriteLine("[Test 1] Retrying on HTTP...");

            // Switch to HTTP
            await _request.DisposeAsync();
            _baseUrl = HttpUrl;

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

            // Retry over HTTP
            registerResponse = await _request.PostAsync("/api/authentication/register",
                new() { DataObject = registrationPayload });
        }

        // Registration result
        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 1] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 1] Registration Body: {regBody}");

        // Must succeed
        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before login test:\nHTTP {regStatus}\n{regBody}");

        // Payload for login
        var loginPayload = new
        {
            email,
            password
        };

        Console.WriteLine("[Test 1] Attempting login with newly registered user");
        Console.WriteLine($"[Test 1] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 1] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 1] Login Body: {loginBody}");

        // Login MUST return HTTP 200
        Assert.That(loginStatus, Is.InRange(200, 299),
            $"Login failed for valid test user:\nHTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 1] OK: Login succeeded for registered user.");
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context (Login tests).");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
