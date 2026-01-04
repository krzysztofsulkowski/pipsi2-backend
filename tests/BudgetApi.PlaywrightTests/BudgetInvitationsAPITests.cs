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
}
