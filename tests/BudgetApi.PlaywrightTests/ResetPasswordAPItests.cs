using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class ResetPasswordAPITests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;
    private string _baseUrl = TestBackendConfig.HttpsUrl;

    private const string TestUserEmail = "balancrtestuser@gmail.com";

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
    }

    [Test, Order(1)]
    public async Task ResetPassword_UsingFullLinkFromConfig_ThenLoginWithNewPassword()
    {
        var fullLink = TestBackendConfig.ResetPasswordFullLink;

        if (string.IsNullOrWhiteSpace(fullLink))
        {
            Assert.Fail("TestBackendConfig.ResetPasswordFullLink jest pusty.");
        }

        Console.WriteLine($"[Test 1] Full reset link from config: {fullLink}");

        string encodedToken;

        if (fullLink.Contains("token="))
        {
            var start = fullLink.IndexOf("token=") + "token=".Length;
            var end = fullLink.IndexOf("&", start);
            if (end == -1) end = fullLink.Length;
            encodedToken = fullLink.Substring(start, end - start);
        }
        else
        {
            Assert.Fail("Nie znaleziono token= w ResetPasswordFullLink.");
            return;
        }

        Console.WriteLine($"[Test 1] Encoded token: {encodedToken}");

        var decodedToken = WebUtility.UrlDecode(encodedToken);

        Console.WriteLine($"[Test 1] Decoded token length: {decodedToken.Length}");

        var newPassword = $"NoweHaslo123!{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        Console.WriteLine($"[Test 1] New password: {newPassword}");

        var resetBody = new
        {
            Email = TestUserEmail,
            Token = decodedToken,
            NewPassword = newPassword
        };

        var resetResponse = await _request.PostAsync("/api/authentication/reset-password", new()
        {
            Data = JsonSerializer.Serialize(resetBody)
        });

        var resetStatus = resetResponse.Status;
        var resetResponseBody = await resetResponse.TextAsync();

        Console.WriteLine($"[Test 1] Reset-password HTTP Status: {resetStatus}");
        Console.WriteLine($"[Test 1] Reset-password Body: {resetResponseBody}");

        Assert.That(resetStatus, Is.EqualTo(200));

        var loginBody = new
        {
            Email = TestUserEmail,
            Password = newPassword
        };

        var loginResponse = await _request.PostAsync("/api/authentication/login", new()
        {
            Data = JsonSerializer.Serialize(loginBody)
        });

        var loginStatus = loginResponse.Status;
        var loginResponseBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 1] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 1] Login Body: {loginResponseBody}");

        Assert.That(loginStatus, Is.EqualTo(200));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
