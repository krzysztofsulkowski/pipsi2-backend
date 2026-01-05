using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionSearchApiTests : BudgetApiTestBase
{
    // Test 1 (TransactionSearch): Search transactions should return 401/403 when user is not authenticated
    [Test]
    public async Task Transactions_Search_Should_Return_401_Or_403_When_Unauthorized()
    {
        Console.WriteLine("[Transaction Search Test 1 - Unauthorized] Start: search transactions WITHOUT authentication");

        var budgetId = 1;

        var response = await _request.PostAsync($"/api/budget/{budgetId}/transactions/search", new()
        {
            DataObject = new
            {
                draw = 1,
                start = 0,
                length = 10,
                searchValue = "",
                orderColumn = 0,
                orderDir = "asc",
                extraFilters = new { }
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Transaction Search Test 1 - Unauthorized] HTTP Status: {status}");
        Console.WriteLine($"[Transaction Search Test 1 - Unauthorized] Response Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }
}
