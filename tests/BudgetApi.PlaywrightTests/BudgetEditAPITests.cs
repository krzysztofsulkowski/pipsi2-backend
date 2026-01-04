using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetEditAPITests : BudgetApiTestBase
{
    // Test 1(BudgetEdit): Edit budget should return 200 and update name when user is authenticated (owner)
    [Test]
    public async Task Budget_Edit_Should_Return_200_And_Update_Name_When_Authorized()
    {
        Console.WriteLine("[Test 1] Start: create budget, edit it, then verify updated name in my-budgets");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 1");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listAfterCreateResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listAfterCreateBody = await listAfterCreateResponse.TextAsync();

        var budgetId = FindBudgetIdByName(listAfterCreateBody, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets");

        Console.WriteLine($"[Test 1] Created budgetId: {budgetId}");

        var updatedName = $"Updated budget {Guid.NewGuid()}";

        var editResponse = await authRequest.PostAsync(
            $"/api/budget/{budgetId}/edit",
            new() { DataObject = new { id = budgetId, name = updatedName } }
        );

        var editStatus = editResponse.Status;
        var editBody = await editResponse.TextAsync();

        Console.WriteLine($"[Test 1] Edit budget HTTP Status: {editStatus}");
        Console.WriteLine($"[Test 1] Edit budget Body: {editBody}");

        Assert.That(editStatus == 200,
            $"Expected 200 when editing budget, got HTTP {editStatus}\n{editBody}");

        var listAfterEditResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listAfterEditBody = await listAfterEditResponse.TextAsync();

        Console.WriteLine($"[Test 1] My-budgets (after edit) Body: {listAfterEditBody}");

        Assert.That(listAfterEditBody.Contains(updatedName),
            $"Expected updated name '{updatedName}' to be present in my-budgets\n{listAfterEditBody}");
    }

    // Test 2(BudgetEdit): Edit budget should return 403 or masked not-found when user has no access
    [Test]
    public async Task Budget_Edit_Should_Return_403_Or_Masked_NotFound_When_User_Has_No_Access()
    {
        Console.WriteLine("[Test 2] Start: create budget as User A, then try to edit it as User B");

        var authA = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 2A");
        var authB = await CreateAuthorizedRequest("TEST_USER2_EMAIL", "TEST_USER2_PASSWORD", "Test 2B");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authA.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget as User A failed");

        var listResponseA = await authA.GetAsync("/api/budget/my-budgets");
        var listBodyA = await listResponseA.TextAsync();

        var budgetId = FindBudgetIdByName(listBodyA, createdName);

        Assert.That(budgetId > 0, $"Created budget '{createdName}' not found in my-budgets for A");

        Console.WriteLine($"[Test 2] Created budgetId (A): {budgetId}");

        var updatedName = $"Updated budget {Guid.NewGuid()}";

        var editResponse = await authB.PostAsync(
            $"/api/budget/{budgetId}/edit",
            new() { DataObject = new { id = budgetId, name = updatedName } }
        );

        var status = editResponse.Status;
        var body = await editResponse.TextAsync();

        Console.WriteLine($"[Test 2] Edit budget as B HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Edit budget as B Body: {body}");

        var isForbidden = status == 403;
        var isMaskedNotFound = status == 400 && body.Contains("Error Budget.NotFound");

        Assert.That(isForbidden || isMaskedNotFound,
            $"Expected 403 or masked not-found (400 Error Budget.NotFound) when editing budget without permission, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetEdit): Edit budget should return 401/403 when user is not authenticated
    [Test]
    public async Task Budget_Edit_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 3] Start: try to edit budget WITHOUT authentication");

        var budgetId = 1;

        var payload = new { id = budgetId, name = $"Updated budget {Guid.NewGuid()}" };

        Console.WriteLine($"[Test 3] Edit payload: id={payload.id}, name='{payload.name}'");

        var response = await _request.PostAsync(
            $"/api/budget/{budgetId}/edit",
            new() { DataObject = payload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] Edit budget HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Edit budget Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 4(BudgetEdit): Edit budget should return 400 Budget.NotFound when budget does not exist (authenticated user)
    [Test]
    public async Task Budget_Edit_Should_Return_400_When_Budget_Does_Not_Exist()
    {
        Console.WriteLine("[Test 4] Start: edit non-existing budget WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 4");

        var nonExistingBudgetId = 999999;

        var payload = new { id = nonExistingBudgetId, name = $"Updated budget {Guid.NewGuid()}" };

        Console.WriteLine($"[Test 4] Edit payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");

        var response = await authRequest.PostAsync(
            $"/api/budget/{nonExistingBudgetId}/edit",
            new() { DataObject = payload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 4] Edit budget HTTP Status: {status}");
        Console.WriteLine($"[Test 4] Edit budget Body: {body}");

        Assert.That(status == 400 && body.Contains("Error Budget.NotFound"),
            $"Expected 400 Budget.NotFound for non-existing budget, got HTTP {status}\n{body}");
    }

}
