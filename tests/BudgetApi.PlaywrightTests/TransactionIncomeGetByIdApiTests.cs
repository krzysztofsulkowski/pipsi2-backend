using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionIncomeGetByIdApiTests : BudgetApiTestBase
{
    // Test 1: Get income by id should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Income_GetById_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;
        var incomeId = 1;

        var response = await _request.GetAsync($"/api/budget/{budgetId}/income/{incomeId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }
}
