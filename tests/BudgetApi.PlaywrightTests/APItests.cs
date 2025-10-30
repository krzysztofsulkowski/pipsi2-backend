using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class ApiTests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;
    private string _testUserEmail = $"apitest_{Guid.NewGuid()}@example.com";
    private const string Port = "56059";
    private const string HttpsUrl = $"https://localhost:{Port}";
    private const string HttpUrl = $"http://localhost:{Port}";
    private string _baseUrl = HttpsUrl;
    private const string _testUserPassword = "Test123!@#";

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
        Console.WriteLine($"[Setup] APIRequest context initialized. BaseURL={_baseUrl}");
    }

    // Test 1: Rejestracja użytkownika (POST /api/authentication/register)
    [Test, Order(1)]
    public async Task Register_Should_Succeed()
    {
        Console.WriteLine("[Test 1] Start: Rejestracja użytkownika");
        var payload = new
        {
            email = _testUserEmail,
            username = _testUserEmail.Split('@')[0],
            password = "Test123!@#"
        };

        Console.WriteLine($"[Test 1] Endpoint: POST {_baseUrl}/api/authentication/register");
        Console.WriteLine($"[Test 1] Payload: {JsonSerializer.Serialize(payload)}");

        try
        {
            var r1 = await _request.PostAsync("/api/authentication/register", new() { DataObject = payload });
            var s1 = r1.Status; var b1 = await r1.TextAsync();
            Console.WriteLine($"[Test 1] HTTP Status: {s1}");
            Console.WriteLine($"[Test 1] Body: {b1}");
            Assert.That(s1, Is.InRange(200, 299), $"Registration failed: HTTP {s1}\n{b1}");
            Console.WriteLine("[Test 1] OK: Rejestracja zakończona powodzeniem.");
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"[Test 1] HTTPS failed: {ex.Message}");
            Console.WriteLine("[Test 1] Retry na HTTP…");
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
            Console.WriteLine($"[Test 1] Now BaseURL={_baseUrl}");

            var r2 = await _request.PostAsync("/api/authentication/register", new() { DataObject = payload });
            var s2 = r2.Status; var b2 = await r2.TextAsync();
            Console.WriteLine($"[Test 1] HTTP Status: {s2}");
            Console.WriteLine($"[Test 1] Body: {b2}");
            Assert.That(s2, Is.InRange(200, 299), $"Registration failed: HTTP {s2}\n{b2}");
            Console.WriteLine("[Test 1] OK: Rejestracja zakończona powodzeniem (HTTP).");
        }
    }

    // Test 2: Logowanie (POST /api/authentication/login)
    [Test, Order(2)]
    public async Task Login_Should_Succeed()
    {
        Console.WriteLine("[Test 2] Start: Logowanie użytkownika");
        var payload = new
        {
            email = _testUserEmail,
            password = _testUserPassword
        };

        Console.WriteLine($"[Test 2] Endpoint: POST {_baseUrl}/api/authentication/login");
        Console.WriteLine($"[Test 2] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = payload });

        var status = response.Status;
        var text = await response.TextAsync();

        Console.WriteLine($"[Test 2] HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Body: {text}");

        Assert.That(status, Is.InRange(200, 299), $"Login failed: HTTP {status}\n{text}");
        Console.WriteLine("[Test 2] OK: Logowanie zakończone powodzeniem.");
    }

    // Test 3: Forgot Password (POST /api/authentication/forgot-password)
    [Test, Order(3)]
    public async Task ForgotPassword_Should_Succeed()
    {
        Console.WriteLine("[Test 3] Start: Forgot Password");
        var payload = new { email = _testUserEmail };

        Console.WriteLine($"[Test 3] Endpoint: POST {_baseUrl}/api/authentication/forgot-password");
        Console.WriteLine($"[Test 3] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/forgot-password",
            new() { DataObject = payload });

        var status = response.Status;
        var text = await response.TextAsync();

        Console.WriteLine($"[Test 3] HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Body: {text}");

        Assert.That(status, Is.InRange(200, 299), $"Forgot password failed: HTTP {status}\n{text}");
        Console.WriteLine("[Test 3] OK: Forgot Password zakończone powodzeniem.");
    }

    // Test 4: Reset Password (bad token) (POST /api/authentication/reset-password) -> should return 400
    [Test, Order(4)]
    public async Task ResetPassword_Should_Return_400_For_InvalidToken()
    {
        Console.WriteLine("[Test 4] Start: Reset Password (invalid token)");
        var payload = new
        {
            email = _testUserEmail,
            token = "invalid-reset-token",
            newPassword = "NewTest123!@#"
        };

        Console.WriteLine($"[Test 4] Endpoint: POST {_baseUrl}/api/authentication/reset-password");
        Console.WriteLine($"[Test 4] Payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/authentication/reset-password",
            new() { DataObject = payload });

        var status = response.Status;
        var text = await response.TextAsync();

        Console.WriteLine($"[Test 4] HTTP Status: {status}");
        Console.WriteLine($"[Test 4] Body: {text}");

        Assert.That(status, Is.EqualTo(400), $"Expected 400 for invalid token, got {status}\n{text}");
        Console.WriteLine("[Test 4] OK: API poprawnie odrzuciło nieprawidłowy token.");
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing APIRequest context and Playwright.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
