using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetArchiveAPITests : BudgetApiTestBase
{
    // Test 1(BudgetArchive): Archive budget should return 401/403 when user is not authenticated
    [Test]
    public async Task Budget_Archive_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 1] Start: try to archive budget WITHOUT authentication");

        var budgetId = 1;
        var response = await _request.PostAsync($"/api/budget/{budgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Archive budget HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Archive budget Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetArchive): Archive budget should return 200 when user is authenticated (owner)
    [Test]
    public async Task Budget_Archive_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 2] Start: create budget, then archive it WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 2");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listBody = await listResponse.TextAsync();

        var budgetId = FindBudgetIdByName(listBody, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 2] Created budgetId: {budgetId}");

        var response = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] Archive budget HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Archive budget Body: {body}");

        Assert.That(status == 200,
            $"Expected 200 when archiving budget, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetArchive): Archive budget should return 403 or masked not-found when user is authenticated but is not owner
    [Test]
    public async Task Budget_Archive_Should_Return_403_Or_Masked_NotFound_When_User_Has_No_Access()
    {
        Console.WriteLine("[Test 3] Start: create budget as User A, then try to archive it as User B");

        var authA = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 3A");
        var authB = await CreateAuthorizedRequest("TEST_USER2_EMAIL", "TEST_USER2_PASSWORD", "Test 3B");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authA.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget as User A failed");

        var listResponse = await authA.GetAsync("/api/budget/my-budgets");
        var listBody = await listResponse.TextAsync();

        var budgetId = FindBudgetIdByName(listBody, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets for A");

        Console.WriteLine($"[Test 3] Created budgetId (A): {budgetId}");

        var response = await authB.PostAsync($"/api/budget/{budgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] Archive budget as B HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Archive budget as B Body: {body}");

        var isForbidden = status == 403;
        var isMaskedNotFound = status == 400 && body.Contains("Error Budget.NotFound");

        Assert.That(isForbidden || isMaskedNotFound,
            $"Expected 403 or masked not-found (400 Error Budget.NotFound) when archiving budget without permission, got HTTP {status}\n{body}");
    }

    // Test 4(BudgetArchive): Archiving the same budget twice should return 400/409 (authenticated user)
    [Test]
    public async Task Budget_Archive_Should_Return_400_Or_409_When_Already_Archived()
    {
        Console.WriteLine("[Test 4] Start: create budget, archive it twice, expect error on second archive");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 4");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listBody = await listResponse.TextAsync();

        var budgetId = FindBudgetIdByName(listBody, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        var firstArchiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");
        Assert.That(firstArchiveResponse.Status == 200, "First archive should return 200");

        var secondArchiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");

        var status = secondArchiveResponse.Status;
        var body = await secondArchiveResponse.TextAsync();

        Console.WriteLine($"[Test 4] Second archive HTTP Status: {status}");
        Console.WriteLine($"[Test 4] Second archive Body: {body}");

        Assert.That(status == 400 || status == 409,
            $"Expected 400 or 409 on second archive (already archived), got HTTP {status}\n{body}");
    }

    // Test 5(BudgetArchive): Archive budget should return 400 Budget.NotFound when budget does not exist (authenticated user)
    [Test]
    public async Task Budget_Archive_Should_Return_400_When_Budget_Does_Not_Exist()
    {
        Console.WriteLine("[Test 5] Start: archive non-existing budget WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 5");

        var nonExistingBudgetId = 999999;

        var response = await authRequest.PostAsync($"/api/budget/{nonExistingBudgetId}/archive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 5] Archive budget HTTP Status: {status}");
        Console.WriteLine($"[Test 5] Archive budget Body: {body}");

        Assert.That(status == 400 && body.Contains("Error Budget.NotFound"),
            $"Expected 400 Budget.NotFound for non-existing budget, got HTTP {status}\n{body}");
    }
}
