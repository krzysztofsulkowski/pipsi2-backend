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

    // Test 3(BudgetInvitations): Accept invitation should return 400 when token is invalid
    [Test]
    public async Task Budget_AcceptInvitation_Should_Return_400_When_Token_Is_Invalid()
    {
        Console.WriteLine("[Test 3] Start: accept invitation with invalid token");

        var invalidToken = Guid.NewGuid().ToString();

        var response = await _request.GetAsync($"/api/budget/accept-invitation?token={invalidToken}");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 3] Accept-invitation HTTP Status: {status}");
        Console.WriteLine($"[Test 3] Accept-invitation Body: {body}");

        Assert.That(status == 400,
            $"Expected 400 when token is invalid, got HTTP {status}\n{body}");
    }

    // Test 4(BudgetInvitations): Accept invitation should return 400 when token is missing
    [Test]
    public async Task Budget_AcceptInvitation_Should_Return_400_When_Token_Is_Missing()
    {
        Console.WriteLine("[Test 4] Start: accept invitation WITHOUT token query param");

        var response = await _request.GetAsync("/api/budget/accept-invitation");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 4] Accept-invitation HTTP Status: {status}");
        Console.WriteLine($"[Test 4] Accept-invitation Body: {body}");

        Assert.That(status == 400,
            $"Expected 400 when token is missing, got HTTP {status}\n{body}");
    }

    // Test 5(BudgetInvitation): Send invitation should return 400 when payload is invalid (missing required fields)
    [Test]
    public async Task Budget_SendInvitation_Should_Return_400_When_Payload_Is_Invalid()
    {
        Console.WriteLine("[Test 5] Start: send invitation with invalid payload (missing recipientEmail) as owner");

        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "Test 5"
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

        Console.WriteLine($"[Test 5] Created budgetId: {budgetId}");

        var invalidPayload = new
        {
            budgetId = budgetId,
            budgetName = budgetName
        };

        Console.WriteLine($"[Test 5] Send-invitation invalid payload: {System.Text.Json.JsonSerializer.Serialize(invalidPayload)}");

        var response = await authRequest.PostAsync(
            "/api/budget/send-invitation",
            new() { DataObject = invalidPayload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 5] Send-invitation HTTP Status: {status}");
        Console.WriteLine($"[Test 5] Send-invitation Body: {body}");

        Assert.That(status == 400,
            $"Expected 400 when payload is invalid, got HTTP {status}\n{body}");
    }


}
