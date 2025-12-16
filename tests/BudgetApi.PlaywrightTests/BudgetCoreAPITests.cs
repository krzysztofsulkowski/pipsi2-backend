/*
POST /api/budget/create

GET / api / budget /{ budgetId}

GET / api / budget / my - budgets

POST / api / budget /{ budgetId}/ edit

POST / api / budget /{ budgetId}/ archive

POST / api / budget /{ budgetId}/ unarchive
*/
using DotNetEnv;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;


namespace BudgetApi.PlaywrightTests;

public class BudgetCoreAPITests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;
    private string _baseUrl = TestBackendConfig.HttpsUrl;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var envPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", ".env"));
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

        Console.WriteLine($"[Setup] BudgetCore API tests initialized. BaseURL={_baseUrl}");
    }

    // Test 1(BudgetCore): Create budget should return 401/403 when user is not authenticated
    [Test, Order(1)]
    public async Task Budget_Create_Should_Return_401_Or_403_When_Unauthorized()
    {
        var payload = new { };

        Console.WriteLine("[Test 1] Start: create budget WITHOUT authentication");
        Console.WriteLine($"[Test 1] Create payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/budget/create", new()
        {
            DataObject = payload
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Create HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Create Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetCore): Get my budgets should return 401/403 when user is not authenticated
    [Test, Order(2)]
    public async Task Budget_MyBudgets_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 2] Start: get my-budgets WITHOUT authentication");

        var response = await _request.GetAsync("/api/budget/my-budgets");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] My-budgets HTTP Status: {status}");
        Console.WriteLine($"[Test 2] My-budgets Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetCore): Get budget by id should return 401/403 when user is not authenticated
    [Test, Order(3)]
    public async Task Budget_GetById_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 3] Start: get budget by ID WITHOUT authentication");

        var budgetId = 123123;
        var response = await _request.GetAsync($"/api/budget/{budgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] Get budget by ID HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Get budget by ID Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 4(BudgetCore): Get my budgets should return 200 when user is authenticated (login via /api/authentication/login using credentials from .env)
    [Test, Order(4)]
    public async Task Budget_MyBudgets_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 4] Start: login and get my-budgets WITH authentication");

        var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(email) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(password) == false, "TEST_USER_PASSWORD is missing");

        var loginPayload = new
        {
            email = email,
            password = password
        };

        var loginResponse = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = loginPayload }
        );

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 4] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 4] Login Body: {loginBody}");

        Assert.That(loginStatus == 200, $"Login failed\n{loginBody}");

        using var loginJson = JsonDocument.Parse(loginBody);
        var token = loginJson.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(token) == false, "JWT token is missing");

        var authRequest = await _playwright.APIRequest.NewContextAsync(new()
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

        var response = await authRequest.GetAsync("/api/budget/my-budgets");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 4] My-budgets HTTP Status: {status}");
        Console.WriteLine($"[Test 4] My-budgets Body: {body}");

        Assert.That(status == 200, $"Expected 200, got HTTP {status}\n{body}");
    }

    // Test 5(BudgetCore): Create budget should return 200 when user is authenticated
    [Test, Order(5)]
    public async Task Budget_Create_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 5] Start: create budget WITH authentication");

        var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(email) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(password) == false, "TEST_USER_PASSWORD is missing");

        var loginPayload = new
        {
            email = email,
            password = password
        };

        var loginResponse = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = loginPayload }
        );

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 5] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 5] Login Body: {loginBody}");

        Assert.That(loginStatus == 200, $"Login failed\n{loginBody}");

        using var loginJson = JsonDocument.Parse(loginBody);
        var token = loginJson.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(token) == false, "JWT token is missing");

        var authRequest = await _playwright.APIRequest.NewContextAsync(new()
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

        var createPayload = new
        {
            id = 0,
            name = $"Test budget {Guid.NewGuid()}"
        };

        var response = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = createPayload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 5] Create budget HTTP Status: {status}");
        Console.WriteLine($"[Test 5] Create budget Body: {body}");

        Assert.That(status == 200,
            $"Expected 200 when creating budget, got HTTP {status}\n{body}");
    }



    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
