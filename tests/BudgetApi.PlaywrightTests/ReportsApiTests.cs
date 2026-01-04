using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class ReportsApiTests : BudgetApiTestBase
{
    // Get reports stats should return 401 or 403 when user is not authenticated
    [Test]
    public async Task Reports_Stats_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.GetAsync("/api/Reports/stats?year=2025&month=0");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Reports Test 1] HTTP Status: " + status);
        Console.WriteLine("[Reports Test 1] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 401 || status == 403, $"Expected 401 or 403, got {status}\n{body}");
    }
}
