using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetGetByIdAPITests : BudgetApiTestBase
{
    // Test 1(BudgetGetById): Get budget by id should return 401/403 when user is not authenticated
    [Test]
    public async Task Budget_GetById_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 1] Start: get budget by ID WITHOUT authentication");

        var budgetId = 123123;
        var response = await _request.GetAsync($"/api/budget/{budgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Get budget by ID HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Get budget by ID Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetGetById): Get budget by id should return 200 when user is authenticated (owner)
    [Test]
    public async Task Budget_GetById_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 2] Start: create budget, then get it by ID WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 2");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 2] My-budgets HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 2] My-budgets Body: {listBody}");

        Assert.That(listStatus == 200, $"Expected 200 from my-budgets, got HTTP {listStatus}\n{listBody}");

        var budgetId = FindBudgetIdByName(listBody, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 2] Found created budgetId: {budgetId}");

        var response = await authRequest.GetAsync($"/api/budget/{budgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] Get budget by ID HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Get budget by ID Body: {body}");

        Assert.That(status == 200,
            $"Expected 200 when getting budget by ID, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetGetById): Get budget by id should return 403 or masked not-found when user has no access
    [Test]
    public async Task Budget_GetById_Should_Return_403_Or_Masked_NotFound_When_User_Has_No_Access()
    {
        Console.WriteLine("[Test 3] Start: create budget as User A, then try to get it by ID as User B");

        var authA = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 3A");
        var authB = await CreateAuthorizedRequest("TEST_USER2_EMAIL", "TEST_USER2_PASSWORD", "Test 3B");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authA.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget as User A failed");

        var listResponse = await authA.GetAsync("/api/budget/my-budgets");
        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 3] My-budgets (A) HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 3] My-budgets (A) Body: {listBody}");

        Assert.That(listStatus == 200, $"Expected 200 from my-budgets as A, got HTTP {listStatus}\n{listBody}");

        var budgetId = FindBudgetIdByName(listBody, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets for A");

        Console.WriteLine($"[Test 3] Created budgetId (A): {budgetId}");

        var response = await authB.GetAsync($"/api/budget/{budgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] Get budget by ID as B HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Get budget by ID as B Body: {body}");

        var isForbidden = status == 403;
        var isMaskedNotFound = status == 400 && body.Contains("Error Budget.NotFound");

        Assert.That(isForbidden || isMaskedNotFound,
            $"Expected 403 or masked not-found (400 Error Budget.NotFound) when accessing budget without permission, got HTTP {status}\n{body}");
    }

    // Test 4(BudgetGetById): Get budget by id should return 400 when budget does not exist (authenticated user)
    [Test]
    public async Task Budget_GetById_Should_Return_400_When_Budget_Does_Not_Exist()
    {
        Console.WriteLine("[Test 4] Start: get non-existing budget by ID WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 4");

        var nonExistingBudgetId = 999999;

        var response = await authRequest.GetAsync($"/api/budget/{nonExistingBudgetId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 4] Get budget by ID HTTP Status: {status}");
        Console.WriteLine($"[Test 4] Get budget by ID Body: {body}");

        Assert.That(status == 400 && body.Contains("Error Budget.NotFound"),
            $"Expected 400 Budget.NotFound for non-existing budget, got HTTP {status}\n{body}");
    }
}
