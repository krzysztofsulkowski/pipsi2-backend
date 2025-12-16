using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetUnarchiveAPITests : BudgetApiTestBase
{
    // Test 1(BudgetUnarchive): Unarchive budget should return 200 and restore active status when user is authenticated (owner)
    [Test]
    public async Task Budget_Unarchive_Should_Return_200_And_Restore_Active_Status_When_Authorized()
    {
        Console.WriteLine("[Test 1] Start: create budget, archive it, unarchive it, then verify status in my-budgets");

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

        var archiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/archive");
        Assert.That(archiveResponse.Status == 200, "Archive should return 200");

        var unarchiveResponse = await authRequest.PostAsync($"/api/budget/{budgetId}/unarchive");

        var unarchiveStatus = unarchiveResponse.Status;
        var unarchiveBody = await unarchiveResponse.TextAsync();

        Console.WriteLine($"[Test 1] Unarchive budget HTTP Status: {unarchiveStatus}");
        Console.WriteLine($"[Test 1] Unarchive budget Body: {unarchiveBody}");

        Assert.That(unarchiveStatus == 200,
            $"Expected 200 when unarchiving budget, got HTTP {unarchiveStatus}\n{unarchiveBody}");

        var listAfterUnarchiveResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listAfterUnarchiveBody = await listAfterUnarchiveResponse.TextAsync();

        Console.WriteLine($"[Test 1] My-budgets (after unarchive) Body: {listAfterUnarchiveBody}");

        Assert.That(listAfterUnarchiveBody.Contains($"\"id\":{budgetId}"),
            $"Expected budget {budgetId} to exist in my-budgets after unarchive");

        Assert.That(listAfterUnarchiveBody.ToLower().Contains("akty") || listAfterUnarchiveBody.ToLower().Contains("active"),
            $"Expected active status after unarchive\n{listAfterUnarchiveBody}");
    }

    // Test 2(BudgetUnarchive): Unarchive active budget should return 400/409 (authenticated user)
    [Test]
    public async Task Budget_Unarchive_Should_Return_400_Or_409_When_Already_Active()
    {
        Console.WriteLine("[Test 2] Start: create budget (active), then unarchive it, expect error");

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

        var response = await authRequest.PostAsync($"/api/budget/{budgetId}/unarchive");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] Unarchive active budget HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Unarchive active budget Body: {body}");

        Assert.That(status == 400 || status == 409,
            $"Expected 400 or 409 when unarchiving already active budget, got HTTP {status}\n{body}");
    }
}
