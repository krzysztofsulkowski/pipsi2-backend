using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetInvitationsAPITests : BudgetApiTestBase
{
    // Test 1(BudgetInvitation): Send invitation should return 401/403 when user is not authenticated
    [Test]
    public async Task Budget_SendInvitation_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Test 1] Start: send invitation WITHOUT authentication");

        var payload = new
        {
            budgetId = 1,
            email = "someone@example.com"
        };

        var response = await _request.PostAsync(
            "/api/budget/send-invitation",
            new() { DataObject = payload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Send-invitation HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Send-invitation Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetInvitation): Send invitation should return 200 when user is owner of budget
    [Test]
    public async Task Budget_SendInvitation_Should_Return_200_When_User_Is_Owner()
    {
        Console.WriteLine("[Test 2] Start: create budget, then send invitation as owner");

        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "Test 2"
        );

        var budgetName = $"Test budget {Guid.NewGuid()}";

        var createResponse = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = new { id = 0, name = budgetName } }
        );

        Assert.That(createResponse.Status == 200, "Create budget failed");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        Assert.That(listResponse.Status == 200, "My-budgets failed");

        var listBody = await listResponse.TextAsync();
        var budgetId = FindBudgetIdByName(listBody, budgetName);

        Assert.That(budgetId > 0, $"Created budget '{budgetName}' not found in my-budgets");

        Console.WriteLine($"[Test 2] Created budgetId: {budgetId}");

        var recipientEmail = Environment.GetEnvironmentVariable("TEST_USER2_EMAIL");
        Assert.That(string.IsNullOrWhiteSpace(recipientEmail) == false, "TEST_USER2_EMAIL is missing");

        var invitationPayload = new
        {
            recipientEmail = recipientEmail,
            budgetName = budgetName,
            budgetId = budgetId
        };

        Console.WriteLine($"[Test 2] Send-invitation payload: {System.Text.Json.JsonSerializer.Serialize(invitationPayload)}");

        var response = await authRequest.PostAsync(
            "/api/budget/send-invitation",
            new() { DataObject = invitationPayload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] Send-invitation HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Send-invitation Body: {body}");

        Assert.That(status == 200 || status == 204,
            $"Expected 200/204 when sending invitation as owner, got HTTP {status}\n{body}");
    }



}
