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

    // Test 1(ResetPassword): Reset password using full reset link from config and verify login with new password succeeds
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

    // Test 2(ResetPassword): Reset password should fail when invalid token is provided
    [Test, Order(2)]
    public async Task ResetPassword_WithInvalidToken_ReturnsError()
    {
        var invalidToken = $"invalid-token-{Guid.NewGuid()}";

        var newPassword = $"HasloTestowe!{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}A1";

        var resetBody = new
        {
            Email = TestUserEmail,
            Token = invalidToken,
            NewPassword = newPassword
        };

        var resetResponse = await _request.PostAsync("/api/authentication/reset-password", new()
        {
            Data = JsonSerializer.Serialize(resetBody)
        });

        var resetStatus = resetResponse.Status;
        var resetResponseBody = await resetResponse.TextAsync();

        Console.WriteLine($"[Test 2] Reset-password HTTP Status: {resetStatus}");
        Console.WriteLine($"[Test 2] Reset-password Body: {resetResponseBody}");

        Assert.That((int)resetStatus, Is.GreaterThanOrEqualTo(400));
    }

    // Test 3(ResetPassword): Reset password should fail when token is null or empty
    [Test, Order(3)]
    public async Task ResetPassword_WithMissingToken_ReturnsError()
    {
        string? missingToken = null;

        var newPassword = $"NoweHaslo123!{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}X1";

        var resetBody = new
        {
            Email = TestUserEmail,
            Token = missingToken,
            NewPassword = newPassword
        };

        var resetResponse = await _request.PostAsync("/api/authentication/reset-password", new()
        {
            Data = JsonSerializer.Serialize(resetBody)
        });

        var resetStatus = resetResponse.Status;
        var resetResponseBody = await resetResponse.TextAsync();

        Console.WriteLine($"[Test 3] Reset-password HTTP Status: {resetStatus}");
        Console.WriteLine($"[Test 3] Reset-password Body: {resetResponseBody}");

        Assert.That((int)resetStatus, Is.GreaterThanOrEqualTo(400));
    }

    // Test 4(ResetPassword): Reset password should fail when the new password is too weak
    [Test, Order(4)]
    public async Task ResetPassword_WithWeakPassword_ReturnsError()
    {
        var fullLink = TestBackendConfig.ResetPasswordFullLink;

        if (string.IsNullOrWhiteSpace(fullLink))
        {
            Assert.Fail("TestBackendConfig.ResetPasswordFullLink is empty.");
        }

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
            Assert.Fail("token= not found in ResetPasswordFullLink.");
            return;
        }

        var decodedToken = WebUtility.UrlDecode(encodedToken);

        var weakPassword = "haslo"; // too weak: no uppercase, no digit, no special character, too short

        var resetBody = new
        {
            Email = TestUserEmail,
            Token = decodedToken,
            NewPassword = weakPassword
        };

        var resetResponse = await _request.PostAsync("/api/authentication/reset-password", new()
        {
            Data = JsonSerializer.Serialize(resetBody)
        });

        var resetStatus = resetResponse.Status;
        var resetResponseBody = await resetResponse.TextAsync();

        Console.WriteLine($"[Test 4] Reset-password HTTP Status: {resetStatus}");
        Console.WriteLine($"[Test 4] Reset-password Body: {resetResponseBody}");

        Assert.That((int)resetStatus, Is.GreaterThanOrEqualTo(400));
    }

    // Test 5(ResetPassword): Reset password should fail when email does not exist
    [Test, Order(5)]
    public async Task ResetPassword_WithNonExistingEmail_ReturnsError()
    {
        var nonExistingEmail = "nonexistinguser_balancr@test.local";

        var newPassword = $"NoweHaslo123!{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}Y1";

        var resetBody = new
        {
            Email = nonExistingEmail,
            Token = "invalid-or-foreign-token",
            NewPassword = newPassword
        };

        var resetResponse = await _request.PostAsync("/api/authentication/reset-password", new()
        {
            Data = JsonSerializer.Serialize(resetBody)
        });

        var resetStatus = resetResponse.Status;
        var resetResponseBody = await resetResponse.TextAsync();

        Console.WriteLine($"[Test 5] Reset-password HTTP Status: {resetStatus}");
        Console.WriteLine($"[Test 5] Reset-password Body: {resetResponseBody}");

        Assert.That((int)resetStatus, Is.GreaterThanOrEqualTo(400));
    }


    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
