using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class HistoryLogApiTests : BudgetApiTestBase
{
    //Get history logs should return 401 or 403 when unauthorized
    [Test]
    public async Task HistoryLog_GetHistoryLogs_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.PostAsync("/api/historyLog/get-history-logs", new APIRequestContextOptions
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

        Console.WriteLine("[HistoryLog Test 1] HTTP Status: " + status);
        Console.WriteLine("[HistoryLog Test 1] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 401 || status == 403, $"Expected 401 or 403, got {status}\n{body}");
    }
}
