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

    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing APIRequest context and Playwright.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
