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
}
