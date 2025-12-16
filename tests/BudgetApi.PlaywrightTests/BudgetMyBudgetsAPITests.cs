using System;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetMyBudgetsAPITests : BudgetApiTestBase
{
    // Test 1(BudgetMyBudgets): Get my budgets should return 401/403 when user is not authenticated
    [Test]
    public async Task Budget_MyBudgets_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 1] Start: get my-budgets WITHOUT authentication");

        var response = await _request.GetAsync("/api/budget/my-budgets");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] My-budgets HTTP Status: {status}");
        Console.WriteLine($"[Test 1] My-budgets Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetMyBudgets): Get my budgets should return 200 when user is authenticated
    [Test]
    public async Task Budget_MyBudgets_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 2] Start: login and get my-budgets WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 2");

        var response = await authRequest.GetAsync("/api/budget/my-budgets");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] My-budgets HTTP Status: {status}");
        Console.WriteLine($"[Test 2] My-budgets Body: {body}");

        Assert.That(status == 200, $"Expected 200, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetMyBudgets): My-budgets should return only budgets owned by authenticated user
    [Test]
    public async Task Budget_MyBudgets_Should_Return_Only_User_Own_Budgets()
    {
        Console.WriteLine("[Test 3] Start: create budget as User A, then verify it is not visible for User B");

        var authA = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 3A");
        var authB = await CreateAuthorizedRequest("TEST_USER2_EMAIL", "TEST_USER2_PASSWORD", "Test 3B");

        var createdName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authA.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = createdName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget as User A failed");

        var response = await authB.GetAsync("/api/budget/my-budgets");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] My-budgets (User B) HTTP Status: {status}");
        Console.WriteLine($"[Test 3] My-budgets (User B) Body: {body}");

        Assert.That(status == 200, $"Expected 200 from my-budgets for User B, got HTTP {status}");

        using var json = JsonDocument.Parse(body);

        foreach (var budget in json.RootElement.EnumerateArray())
        {
            var name = budget.GetProperty("name").GetString();
            Assert.That(name != createdName,
                $"Budget created by User A should not be visible for User B (found '{name}')");
        }
    }
}
