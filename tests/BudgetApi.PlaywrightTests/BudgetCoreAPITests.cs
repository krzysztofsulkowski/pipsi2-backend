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

    // Test 6(BudgetCore): Get budget by id should return 200 when user is authenticated (owner)
    [Test, Order(6)]
    public async Task Budget_GetById_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 6] Start: create budget, then get it by ID WITH authentication");

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

        Console.WriteLine($"[Test 6] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 6] Login Body: {loginBody}");

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

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createPayload = new
        {
            id = 0,
            name = createdName
        };

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = createPayload }
        );

        var createStatus = createResponse.Status;

        Console.WriteLine($"[Test 6] Create budget HTTP Status: {createStatus}");

        Assert.That(createStatus == 200,
            $"Expected 200 when creating budget, got HTTP {createStatus}");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");

        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 6] My-budgets HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 6] My-budgets Body: {listBody}");

        Assert.That(listStatus == 200,
            $"Expected 200 from my-budgets, got HTTP {listStatus}\n{listBody}");

        using var listJson = JsonDocument.Parse(listBody);

        var budgets = listJson.RootElement.EnumerateArray();

        int budgetId = -1;

        foreach (var budget in budgets)
        {
            if (budget.GetProperty("name").GetString() == createdName)
            {
                budgetId = budget.GetProperty("id").GetInt32();
                break;
            }
        }

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 6] Found created budgetId: {budgetId}");

        var response = await authRequest.GetAsync($"/api/budget/{budgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 6] Get budget by ID HTTP Status: {status}");
        Console.WriteLine($"[Test 6] Get budget by ID Body: {body}");

        Assert.That(status == 200,
            $"Expected 200 when getting budget by ID, got HTTP {status}\n{body}");
    }

    // Test 7(BudgetCore): Get budget by id should return 403 or masked not-found when user has no access
    [Test, Order(7)]
    public async Task Budget_GetById_Should_Return_403_Or_Masked_NotFound_When_User_Has_No_Access()
    {
        Console.WriteLine("[Test 7] Start: create budget as User A, then try to get it by ID as User B");

        var emailA = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var passwordA = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        var emailB = Environment.GetEnvironmentVariable("TEST_USER2_EMAIL");
        var passwordB = Environment.GetEnvironmentVariable("TEST_USER2_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(emailA) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(passwordA) == false, "TEST_USER_PASSWORD is missing");
        Assert.That(string.IsNullOrWhiteSpace(emailB) == false, "TEST_USER2_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(passwordB) == false, "TEST_USER2_PASSWORD is missing");

        var loginResponseA = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = emailA, password = passwordA } }
        );

        var loginStatusA = loginResponseA.Status;
        var loginBodyA = await loginResponseA.TextAsync();

        Console.WriteLine($"[Test 7] Login A HTTP Status: {loginStatusA}");
        Console.WriteLine($"[Test 7] Login A Body: {loginBodyA}");

        Assert.That(loginStatusA == 200, $"Login A failed\n{loginBodyA}");

        using var loginJsonA = JsonDocument.Parse(loginBodyA);
        var tokenA = loginJsonA.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(tokenA) == false, "JWT token for User A is missing");

        var authRequestA = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {tokenA}" }
        }
        });

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequestA.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        var createStatus = createResponse.Status;

        Console.WriteLine($"[Test 7] Create budget (A) HTTP Status: {createStatus}");

        Assert.That(createStatus == 200, $"Expected 200 when creating budget as A, got HTTP {createStatus}");

        var listResponse = await authRequestA.GetAsync("/api/budget/my-budgets");

        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 7] My-budgets (A) HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 7] My-budgets (A) Body: {listBody}");

        Assert.That(listStatus == 200, $"Expected 200 from my-budgets as A, got HTTP {listStatus}\n{listBody}");

        using var listJson = JsonDocument.Parse(listBody);

        int budgetId = -1;

        foreach (var budget in listJson.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("name").GetString() == createdName)
            {
                budgetId = budget.GetProperty("id").GetInt32();
                break;
            }
        }

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets for A");

        Console.WriteLine($"[Test 7] Created budgetId (A): {budgetId}");

        var loginResponseB = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = emailB, password = passwordB } }
        );

        var loginStatusB = loginResponseB.Status;
        var loginBodyB = await loginResponseB.TextAsync();

        Console.WriteLine($"[Test 7] Login B HTTP Status: {loginStatusB}");
        Console.WriteLine($"[Test 7] Login B Body: {loginBodyB}");

        Assert.That(loginStatusB == 200, $"Login B failed\n{loginBodyB}");

        using var loginJsonB = JsonDocument.Parse(loginBodyB);
        var tokenB = loginJsonB.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(tokenB) == false, "JWT token for User B is missing");

        var authRequestB = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {tokenB}" }
        }
        });

        var response = await authRequestB.GetAsync($"/api/budget/{budgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 7] Get budget by ID as B HTTP Status: {status}");
        Console.WriteLine($"[Test 7] Get budget by ID as B Body: {body}");

        var isForbidden = status == 403;
        var isMaskedNotFound = status == 400 && body.Contains("Error Budget.NotFound");

        Assert.That(isForbidden || isMaskedNotFound,
            $"Expected 403 or masked not-found (400 Error Budget.NotFound) when accessing budget without permission, got HTTP {status}\n{body}");
    }

    // Test 8(BudgetCore): Archive budget should return 200 when user is authenticated (owner)
    [Test, Order(8)]
    public async Task Budget_Archive_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 8] Start: create budget, then archive it WITH authentication");

        var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(email) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(password) == false, "TEST_USER_PASSWORD is missing");

        var loginResponse = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = email, password = password } }
        );

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 8] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 8] Login Body: {loginBody}");

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

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        var createStatus = createResponse.Status;

        Console.WriteLine($"[Test 8] Create budget HTTP Status: {createStatus}");

        Assert.That(createStatus == 200, $"Expected 200 when creating budget, got HTTP {createStatus}");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");

        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 8] My-budgets HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 8] My-budgets Body: {listBody}");

        Assert.That(listStatus == 200, $"Expected 200 from my-budgets, got HTTP {listStatus}\n{listBody}");

        using var listJson = JsonDocument.Parse(listBody);

        int budgetId = -1;

        foreach (var budget in listJson.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("name").GetString() == createdName)
            {
                budgetId = budget.GetProperty("id").GetInt32();
                break;
            }
        }

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 8] Created budgetId: {budgetId}");

        var response = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 8] Archive budget HTTP Status: {status}");
        Console.WriteLine($"[Test 8] Archive budget Body: {body}");

        Assert.That(status == 200,
            $"Expected 200 when archiving budget, got HTTP {status}\n{body}");
    }

    // Test 9(BudgetCore): Archive budget should return 403 or masked not-found when user is authenticated but is not owner
    [Test, Order(9)]
    public async Task Budget_Archive_Should_Return_403_Or_Masked_NotFound_When_User_Has_No_Access()
    {
        Console.WriteLine("[Test 9] Start: create budget as User A, then try to archive it as User B");

        var emailA = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var passwordA = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        var emailB = Environment.GetEnvironmentVariable("TEST_USER2_EMAIL");
        var passwordB = Environment.GetEnvironmentVariable("TEST_USER2_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(emailA) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(passwordA) == false, "TEST_USER_PASSWORD is missing");
        Assert.That(string.IsNullOrWhiteSpace(emailB) == false, "TEST_USER2_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(passwordB) == false, "TEST_USER2_PASSWORD is missing");

        var loginResponseA = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = emailA, password = passwordA } }
        );

        var loginStatusA = loginResponseA.Status;
        var loginBodyA = await loginResponseA.TextAsync();

        Console.WriteLine($"[Test 9] Login A HTTP Status: {loginStatusA}");
        Console.WriteLine($"[Test 9] Login A Body: {loginBodyA}");

        Assert.That(loginStatusA == 200, $"Login A failed\n{loginBodyA}");

        using var loginJsonA = JsonDocument.Parse(loginBodyA);
        var tokenA = loginJsonA.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(tokenA) == false, "JWT token for User A is missing");

        var authRequestA = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {tokenA}" }
        }
        });

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequestA.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        var createStatus = createResponse.Status;

        Console.WriteLine($"[Test 9] Create budget (A) HTTP Status: {createStatus}");

        Assert.That(createStatus == 200, $"Expected 200 when creating budget as A, got HTTP {createStatus}");

        var listResponse = await authRequestA.GetAsync("/api/budget/my-budgets");

        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 9] My-budgets (A) HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 9] My-budgets (A) Body: {listBody}");

        Assert.That(listStatus == 200, $"Expected 200 from my-budgets as A, got HTTP {listStatus}\n{listBody}");

        using var listJson = JsonDocument.Parse(listBody);

        int budgetId = -1;

        foreach (var budget in listJson.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("name").GetString() == createdName)
            {
                budgetId = budget.GetProperty("id").GetInt32();
                break;
            }
        }

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets for A");

        Console.WriteLine($"[Test 9] Created budgetId (A): {budgetId}");

        var loginResponseB = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = emailB, password = passwordB } }
        );

        var loginStatusB = loginResponseB.Status;
        var loginBodyB = await loginResponseB.TextAsync();

        Console.WriteLine($"[Test 9] Login B HTTP Status: {loginStatusB}");
        Console.WriteLine($"[Test 9] Login B Body: {loginBodyB}");

        Assert.That(loginStatusB == 200, $"Login B failed\n{loginBodyB}");

        using var loginJsonB = JsonDocument.Parse(loginBodyB);
        var tokenB = loginJsonB.RootElement.GetProperty("token").GetString();

        Assert.That(string.IsNullOrWhiteSpace(tokenB) == false, "JWT token for User B is missing");

        var authRequestB = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" },
            { "Authorization", $"Bearer {tokenB}" }
        }
        });

        var response = await authRequestB.PostAsync($"/api/budget/{budgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 9] Archive budget as B HTTP Status: {status}");
        Console.WriteLine($"[Test 9] Archive budget as B Body: {body}");

        var isForbidden = status == 403;
        var isMaskedNotFound = status == 400 && body.Contains("Error Budget.NotFound");

        Assert.That(isForbidden || isMaskedNotFound,
            $"Expected 403 or masked not-found (400 Error Budget.NotFound) when archiving budget without permission, got HTTP {status}\n{body}");
    }

    // Test 10(BudgetCore): My-budgets should show budget as archived after archiving it (authenticated user)
    [Test, Order(10)]
    public async Task Budget_MyBudgets_Should_Show_Archived_Status_After_Archive()
    {
        Console.WriteLine("[Test 10] Start: create budget, archive it, then verify status in my-budgets");

        var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(email) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(password) == false, "TEST_USER_PASSWORD is missing");

        var loginResponse = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = email, password = password } }
        );

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 10] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 10] Login Body: {loginBody}");

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

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        var createStatus = createResponse.Status;

        Console.WriteLine($"[Test 10] Create budget HTTP Status: {createStatus}");

        Assert.That(createStatus == 200, $"Expected 200 when creating budget, got HTTP {createStatus}");

        var listAfterCreateResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listAfterCreateStatus = listAfterCreateResponse.Status;
        var listAfterCreateBody = await listAfterCreateResponse.TextAsync();

        Console.WriteLine($"[Test 10] My-budgets (after create) HTTP Status: {listAfterCreateStatus}");
        Console.WriteLine($"[Test 10] My-budgets (after create) Body: {listAfterCreateBody}");

        Assert.That(listAfterCreateStatus == 200,
            $"Expected 200 from my-budgets, got HTTP {listAfterCreateStatus}\n{listAfterCreateBody}");

        using var listAfterCreateJson = JsonDocument.Parse(listAfterCreateBody);

        int budgetId = -1;

        foreach (var budget in listAfterCreateJson.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("name").GetString() == createdName)
            {
                budgetId = budget.GetProperty("id").GetInt32();
                break;
            }
        }

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 10] Created budgetId: {budgetId}");

        var archiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");
        var archiveStatus = archiveResponse.Status;
        var archiveBody = await archiveResponse.TextAsync();

        Console.WriteLine($"[Test 10] Archive budget HTTP Status: {archiveStatus}");
        Console.WriteLine($"[Test 10] Archive budget Body: {archiveBody}");

        Assert.That(archiveStatus == 200,
            $"Expected 200 when archiving budget, got HTTP {archiveStatus}\n{archiveBody}");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");

        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 10] My-budgets (after archive) HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 10] My-budgets (after archive) Body: {listBody}");

        Assert.That(listStatus == 200,
            $"Expected 200 from my-budgets after archive, got HTTP {listStatus}\n{listBody}");

        using var listJson = JsonDocument.Parse(listBody);

        string? statusText = null;

        foreach (var budget in listJson.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("id").GetInt32() == budgetId)
            {
                statusText = budget.GetProperty("status").GetString();
                break;
            }
        }

        Assert.That(string.IsNullOrWhiteSpace(statusText) == false,
            $"Budget id {budgetId} not found in my-budgets after archive");

        Console.WriteLine($"[Test 10] Archived budget status in my-budgets: {statusText}");

        Assert.That(statusText != null && statusText.ToLower().Contains("arch"),
            $"Expected budget status to indicate archived, got '{statusText}'");
    }

    // Test 11(BudgetCore): Archiving the same budget twice should return 400/409 (authenticated user)
    [Test, Order(11)]
    public async Task Budget_Archive_Should_Return_400_Or_409_When_Already_Archived()
    {
        Console.WriteLine("[Test 11] Start: create budget, archive it twice, expect error on second archive");

        var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

        Assert.That(string.IsNullOrWhiteSpace(email) == false, "TEST_USER_EMAIL is missing");
        Assert.That(string.IsNullOrWhiteSpace(password) == false, "TEST_USER_PASSWORD is missing");

        var loginResponse = await _request.PostAsync(
            "/api/authentication/login",
            new() { DataObject = new { email = email, password = password } }
        );

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 11] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 11] Login Body: {loginBody}");

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

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        var createStatus = createResponse.Status;

        Console.WriteLine($"[Test 11] Create budget HTTP Status: {createStatus}");

        Assert.That(createStatus == 200, $"Expected 200 when creating budget, got HTTP {createStatus}");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 11] My-budgets HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 11] My-budgets Body: {listBody}");

        Assert.That(listStatus == 200, $"Expected 200 from my-budgets, got HTTP {listStatus}\n{listBody}");

        using var listJson = JsonDocument.Parse(listBody);

        int budgetId = -1;

        foreach (var budget in listJson.RootElement.EnumerateArray())
        {
            if (budget.GetProperty("name").GetString() == createdName)
            {
                budgetId = budget.GetProperty("id").GetInt32();
                break;
            }
        }

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 11] Created budgetId: {budgetId}");

        var firstArchiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");
        var firstArchiveStatus = firstArchiveResponse.Status;
        var firstArchiveBody = await firstArchiveResponse.TextAsync();

        Console.WriteLine($"[Test 11] First archive HTTP Status: {firstArchiveStatus}");
        Console.WriteLine($"[Test 11] First archive Body: {firstArchiveBody}");

        Assert.That(firstArchiveStatus == 200,
            $"Expected 200 on first archive, got HTTP {firstArchiveStatus}\n{firstArchiveBody}");

        var secondArchiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");
        var secondArchiveStatus = secondArchiveResponse.Status;
        var secondArchiveBody = await secondArchiveResponse.TextAsync();

        Console.WriteLine($"[Test 11] Second archive HTTP Status: {secondArchiveStatus}");
        Console.WriteLine($"[Test 11] Second archive Body: {secondArchiveBody}");

        Assert.That(secondArchiveStatus == 400 || secondArchiveStatus == 409,
            $"Expected 400 or 409 on second archive (already archived), got HTTP {secondArchiveStatus}\n{secondArchiveBody}");
    }

    // Test 12(BudgetCore): Archive budget should return 401/403 when user is not authenticated
    [Test, Order(12)]
    public async Task Budget_Archive_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 12] Start: try to archive budget WITHOUT authentication");

        var budgetId = 1;
        var response = await _request.PostAsync($"/api/budget/{budgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 12] Archive budget HTTP Status: {status}");
        Console.WriteLine($"[Test 12] Archive budget Body: {body}");

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
