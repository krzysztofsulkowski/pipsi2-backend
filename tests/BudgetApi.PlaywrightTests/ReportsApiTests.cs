using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

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

    // Get reports stats should return 200 when user is authenticated
    [Test]
    public async Task Reports_Stats_Should_Return_200_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Reports Test 2]"
        );

        var response = await authRequest.GetAsync("/api/Reports/stats?year=2025&month=0");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Reports Test 2] HTTP Status: " + status);
        Console.WriteLine("[Reports Test 2] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(body != null, "Response body is null");
    }

    // Get reports stats should return list with valid transaction schema for authorized user
    [Test]
    public async Task Reports_Stats_Should_Return_Valid_Schema_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Reports Test 3 - Schema]"
        );

        var response = await authRequest.GetAsync("/api/Reports/stats?year=2025&month=0");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Reports Test 3 - Schema] HTTP Status: " + status);
        Console.WriteLine("[Reports Test 3 - Schema] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(!string.IsNullOrWhiteSpace(body));

        using var json = JsonDocument.Parse(body);
        Assert.That(json.RootElement.ValueKind == JsonValueKind.Array);

        foreach (var item in json.RootElement.EnumerateArray())
        {
            Assert.That(item.TryGetProperty("id", out var id));
            Assert.That(id.ValueKind == JsonValueKind.Number);

            Assert.That(item.TryGetProperty("date", out var date));
            Assert.That(date.ValueKind == JsonValueKind.String);

            Assert.That(item.TryGetProperty("title", out var title));
            Assert.That(title.ValueKind == JsonValueKind.String);

            Assert.That(item.TryGetProperty("amount", out var amount));
            Assert.That(amount.ValueKind == JsonValueKind.Number);

            Assert.That(item.TryGetProperty("type", out var type));
            Assert.That(type.ValueKind == JsonValueKind.Number);

            Assert.That(item.TryGetProperty("categoryName", out var categoryName));
            Assert.That(categoryName.ValueKind == JsonValueKind.String);

            Assert.That(item.TryGetProperty("status", out var statusProp));
            Assert.That(statusProp.ValueKind == JsonValueKind.Number);

            Assert.That(item.TryGetProperty("paymentMethod", out var paymentMethod));
            Assert.That(paymentMethod.ValueKind == JsonValueKind.Number);

            Assert.That(item.TryGetProperty("userName", out var userName));
            Assert.That(userName.ValueKind == JsonValueKind.String || userName.ValueKind == JsonValueKind.Null);
        }
    }

    // Get reports stats should return 400 when required year parameter is missing
    [Test]
    public async Task Reports_Stats_Should_Return_400_When_Year_Is_Missing()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Reports Test 4 - Missing Year]"
        );

        var response = await authRequest.GetAsync("/api/Reports/stats?month=1");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Reports Test 4 - Missing Year] HTTP Status: " + status);
        Console.WriteLine("[Reports Test 4 - Missing Year] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

}
