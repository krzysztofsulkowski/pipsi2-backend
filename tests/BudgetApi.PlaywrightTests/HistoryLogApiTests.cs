using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

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

    // Get history logs should return 403 when user is authenticated but not admin
    [Test]
    public async Task HistoryLog_GetHistoryLogs_Should_Return_403_For_Normal_User()
    {
        var userRequest = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "[HistoryLog Test 2 - User]"
        );

        var response = await userRequest.PostAsync("/api/historyLog/get-history-logs", new APIRequestContextOptions
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

        Console.WriteLine("[HistoryLog Test 2 - User] HTTP Status: " + status);
        Console.WriteLine("[HistoryLog Test 2 - User] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 403, $"Expected 403, got {status}\n{body}");
    }

    // Get history logs should return 200 when user is admin
    [Test]
    public async Task HistoryLog_GetHistoryLogs_Should_Return_200_For_Admin()
    {
        var adminRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[HistoryLog Test 3 - Admin]"
        );

        var response = await adminRequest.PostAsync("/api/historyLog/get-history-logs", new APIRequestContextOptions
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

        Console.WriteLine("[HistoryLog Test 3 - Admin] HTTP Status: " + status);
        Console.WriteLine("[HistoryLog Test 3 - Admin] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

    // Get history logs should return DataTables-like response schema for admin
    [Test]
    public async Task HistoryLog_GetHistoryLogs_Should_Return_Valid_Schema_For_Admin()
    {
        var adminRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[HistoryLog Test 4 - Schema]"
        );

        var response = await adminRequest.PostAsync("/api/historyLog/get-history-logs", new APIRequestContextOptions
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

        Console.WriteLine("[HistoryLog Test 4 - Schema] HTTP Status: " + status);
        Console.WriteLine("[HistoryLog Test 4 - Schema] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(!string.IsNullOrWhiteSpace(body));

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.That(root.TryGetProperty("draw", out var draw), "Missing 'draw'");
        Assert.That(draw.ValueKind == JsonValueKind.Number, "'draw' is not a number");

        Assert.That(root.TryGetProperty("recordsTotal", out var recordsTotal), "Missing 'recordsTotal'");
        Assert.That(recordsTotal.ValueKind == JsonValueKind.Number, "'recordsTotal' is not a number");

        Assert.That(root.TryGetProperty("recordsFiltered", out var recordsFiltered), "Missing 'recordsFiltered'");
        Assert.That(recordsFiltered.ValueKind == JsonValueKind.Number, "'recordsFiltered' is not a number");

        Assert.That(root.TryGetProperty("data", out var data), "Missing 'data'");
        Assert.That(data.ValueKind == JsonValueKind.Array, "'data' is not an array");

        foreach (var row in data.EnumerateArray())
        {
            Assert.That(row.TryGetProperty("creationDate", out var creationDate), "Row missing 'creationDate'");
            Assert.That(creationDate.ValueKind == JsonValueKind.String, "'creationDate' is not a string");

            Assert.That(row.TryGetProperty("eventType", out var eventType), "Row missing 'eventType'");
            Assert.That(eventType.ValueKind == JsonValueKind.String, "'eventType' is not a string");

            Assert.That(row.TryGetProperty("objectId", out var objectId), "Row missing 'objectId'");
            Assert.That(objectId.ValueKind == JsonValueKind.String, "'objectId' is not a string");

            Assert.That(row.TryGetProperty("objectType", out var objectType), "Row missing 'objectType'");
            Assert.That(objectType.ValueKind == JsonValueKind.String, "'objectType' is not a string");

            Assert.That(row.TryGetProperty("before", out var before), "Row missing 'before'");
            Assert.That(before.ValueKind == JsonValueKind.String, "'before' is not a string");

            Assert.That(row.TryGetProperty("after", out var after), "Row missing 'after'");
            Assert.That(after.ValueKind == JsonValueKind.String, "'after' is not a string");

            Assert.That(row.TryGetProperty("userId", out var userId), "Row missing 'userId'");
            Assert.That(userId.ValueKind == JsonValueKind.String || userId.ValueKind == JsonValueKind.Null, "'userId' is not a string or null");

            Assert.That(row.TryGetProperty("userEmail", out var userEmail), "Row missing 'userEmail'");
            Assert.That(userEmail.ValueKind == JsonValueKind.String || userEmail.ValueKind == JsonValueKind.Null, "'userEmail' is not a string or null");
        }
    }

}
