using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetMembersAPITests : BudgetApiTestBase
{
    // Test 1(BudgetMembers): Remove member should return 401/403 when user is not authenticated
    [Test]
    public async Task Budget_RemoveMember_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 1] Start: remove member WITHOUT authentication");

        var budgetId = 1;
        var userId = "some-user-id";

        var response = await _request.DeleteAsync($"/api/budget/{budgetId}/members/{userId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Remove member HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Remove member Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetMembers): Owner should not be able to remove himself from budget
    [Test]
    public async Task Budget_RemoveMember_Should_Return_400_Or_403_When_Owner_Tries_To_Remove_Himself()
    {
        Console.WriteLine("[Test 2] Start: owner tries to remove himself from budget");

        var authOwner = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "Members Test 2"
        );

        var budgetName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authOwner.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = budgetName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listResponse = await authOwner.GetAsync("/api/budget/my-budgets");
        Assert.That(listResponse.Status == 200, "My-budgets failed");

        var listBody = await listResponse.TextAsync();
        var budgetId = FindBudgetIdByName(listBody, budgetName);

        Assert.That(budgetId > 0, "Budget not found");

        var membersResponse = await authOwner.GetAsync($"/api/budget/{budgetId}/members");
        Assert.That(membersResponse.Status == 200, "Get members failed");

        var membersBody = await membersResponse.TextAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(membersBody);

        string ownerUserId = null;

        foreach (var m in doc.RootElement.EnumerateArray())
        {
            if (m.GetProperty("role").GetString() == "Właściciel")
            {
                ownerUserId = m.GetProperty("userId").GetString();
                break;
            }
        }

        Assert.That(ownerUserId != null, "Owner not found in members");

        var deleteResponse = await authOwner.DeleteAsync(
            $"/api/budget/{budgetId}/members/{ownerUserId}"
        );

        var status = deleteResponse.Status;
        var body = await deleteResponse.TextAsync();

        Console.WriteLine($"[Test 2] DELETE owner status: {status}");
        Console.WriteLine($"[Test 2] DELETE owner body: {body}");

        Assert.That(status == 400 || status == 403,
            $"Expected 400 or 403 when owner tries to remove himself, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetMembers): Remove member should return 403 or masked not-found when user has no access
    [Test]
    public async Task Budget_RemoveMember_Should_Return_403_Or_Masked_NotFound_When_User_Has_No_Access()
    {
        Console.WriteLine("[Test 3] Start: User A creates budget, User B tries to remove owner from it");

        var authOwner = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "Members Test 3A"
        );

        var authOther = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "Members Test 3B"
        );

        var budgetName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authOwner.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = budgetName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listResponse = await authOwner.GetAsync("/api/budget/my-budgets");
        Assert.That(listResponse.Status == 200, "My-budgets failed");

        var listBody = await listResponse.TextAsync();
        var budgetId = FindBudgetIdByName(listBody, budgetName);

        Assert.That(budgetId > 0, "Budget not found");

        var membersResponse = await authOwner.GetAsync($"/api/budget/{budgetId}/members");
        Assert.That(membersResponse.Status == 200, "Get members failed");

        var membersBody = await membersResponse.TextAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(membersBody);

        string? ownerUserId = null;

        foreach (var m in doc.RootElement.EnumerateArray())
        {
            var role = m.TryGetProperty("role", out var roleEl) ? roleEl.GetString() : null;
            if (role == "Właściciel")
            {
                ownerUserId = m.TryGetProperty("userId", out var idEl) ? idEl.GetString() : null;
                break;
            }
        }

        Assert.That(string.IsNullOrWhiteSpace(ownerUserId) == false, "Owner not found in members");

        var deleteResponse = await authOther.DeleteAsync(
            $"/api/budget/{budgetId}/members/{ownerUserId}"
        );

        var status = deleteResponse.Status;
        var body = await deleteResponse.TextAsync();

        Console.WriteLine($"[Test 3] DELETE as User B status: {status}");
        Console.WriteLine($"[Test 3] DELETE as User B body: {body}");

        var isForbidden = status == 403;
        var isMaskedNotFound = status == 400 && body.Contains("Error Budget.NotFound");

        Assert.That(isForbidden || isMaskedNotFound,
            $"Expected 403 or masked not-found (400 Error Budget.NotFound) when removing member without access, got HTTP {status}\n{body}");
    }

}
