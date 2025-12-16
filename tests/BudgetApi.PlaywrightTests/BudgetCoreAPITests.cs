/*
POST /api/budget/create

GET / api / budget /{ budgetId}

GET / api / budget / my - budgets

POST / api / budget /{ budgetId}/ edit

POST / api / budget /{ budgetId}/ archive

POST / api / budget /{ budgetId}/ unarchive
*/
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



    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context.");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
