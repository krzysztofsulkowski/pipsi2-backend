using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionIncomeApiTests : BudgetApiTestBase
{
    // Create income should return 401 or 403 when user is not authenticated
    [Test]
    public async Task Income_Create_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;

        var response = await _request.PostAsync($"/api/budget/{budgetId}/income", new()
        {
            DataObject = new
            {
                description = "unauthorized income",
                amount = 10.01,
                date = "2026-01-05T06:25:12.034Z"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Income Test 1] HTTP Status: " + status);
        Console.WriteLine("[Income Test 1] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 401 || status == 403, $"Expected 401 or 403, got {status}\n{body}");
    }
}
