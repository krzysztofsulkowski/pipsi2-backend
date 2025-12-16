using DotNetEnv;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public abstract class BudgetApiTestBase
{
    protected IPlaywright _playwright = null!;
    protected IAPIRequestContext _request = null!;
    protected string _baseUrl = TestBackendConfig.HttpsUrl;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var envPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            ".env"
        ));

        Console.WriteLine($"[Setup] Loading .env from: {envPath}");
        Console.WriteLine($"[Setup] .env exists: {File.Exists(envPath)}");

        Env.Load(envPath);

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

        Console.WriteLine($"[Setup] API tests initialized. BaseURL={_baseUrl}");
    }

    protected async Task<IAPIRequestContext> CreateAuthorizedRequest(string emailEnvKey, string passwordEnvKey, string testLabel)
    {
        var email = Environment.GetEnvironmentVariable(emailEnvKey);
        var password = Environment.GetEnvironmentVariable(passwordEnvKey);

        Assert.That(string.IsNullOrWhiteSpace(email) == false, $"{emailEnvKey} is missing");
        Assert.That(string.IsNullOrWhiteSpace(password) == false, $"{passwordEnvKey} is missing");

        var loginResponse = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = email, password = password } }
        );

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[{testLabel}] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[{testLabel}] Login Body: {loginBody}");

        Assert.That(loginStatus == 200, $"Login failed\n{loginBody}");

        using var loginJson = JsonDocument.Parse(loginBody);
        var token = loginJson.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(token) == false, "JWT token is missing");

        return await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Content-Type", "application/json" },
                { "Authorization", $"Bearer {token}" }
            }
        });
    }

    protected static int FindBudgetIdByName(string myBudgetsJson, string name)
    {
        using var doc = JsonDocument.Parse(myBudgetsJson);
        foreach (var budget in doc.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("name").GetString() == name)
                return budget.GetProperty("id").GetInt32();
        }
        return -1;
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context.");
        if (_request != null) await _request.DisposeAsync();
        _playwright?.Dispose();
    }
}
