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

    // Test 1(ExternalLogin): External login should return redirect (3xx) for Facebook provider and valid returnUrl
    [Test, Order(1)]
    public async Task ExternalLogin_Facebook_WithValidReturnUrl_ReturnsRedirect()
    {
        var provider = "Facebook";
        var returnUrl = "https://localhost:3000/login";

        var response = await _request.GetAsync("/api/authentication/external-login", new()
        {
            Params = new Dictionary<string, object>
        {
            { "provider", provider },
            { "returnUrl", returnUrl }
        },
            MaxRedirects = 0
        });

        var status = response.Status;
        var headers = response.Headers;

        Console.WriteLine($"[Test 1] External-login HTTP Status: {status}");
        Console.WriteLine("[Test 1] External-login headers:");
        foreach (var header in headers)
        {
            Console.WriteLine($"[Test 1]   {header.Key}: {header.Value}");
        }

        Assert.That((int)status, Is.InRange(300, 399));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
