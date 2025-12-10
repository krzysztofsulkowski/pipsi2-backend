using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class ExternalLoginAPITests
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
                { "Accept", "application/json" }
            }
        });
    }

    // Test 1(ExternalLogin): External login should return 200 and non-empty response for Facebook provider and valid returnUrl
    [Test, Order(1)]
    public async Task ExternalLogin_Facebook_WithValidReturnUrl_Returns200()
    {
        var provider = "Facebook";
        var returnUrl = "https://localhost:3000";

        var response = await _request.GetAsync("/api/authentication/external-login", new()
        {
            Params = new Dictionary<string, object>
        {
            { "provider", provider },
            { "returnUrl", returnUrl }
        }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] External-login HTTP Status: {status}");

        Assert.That(status, Is.EqualTo(200));
        Assert.That(string.IsNullOrWhiteSpace(body), Is.False);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
