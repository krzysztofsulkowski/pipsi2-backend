using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionIncomeCreateApiTests : BudgetApiTestBase
{
    // Test 1 (IncomeCreate): Create income should return 401/403 when user is not authenticated
    [Test]
    public async Task Income_Create_Should_Return_401_Or_403_When_Unauthorized()
    {
        const string testLabel = "Income Create Test 1 - Unauthorized";

        var authorized = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", $"[{testLabel}]");

        var budgetsResponse = await authorized.GetAsync("/api/budget/my-budgets");
        var budgetsStatus = budgetsResponse.Status;
        var budgetsBody = await budgetsResponse.TextAsync();

        Console.WriteLine($"[{testLabel}] My-budgets HTTP Status: {budgetsStatus}");
        Console.WriteLine($"[{testLabel}] My-budgets Body: {budgetsBody}");

        Assert.That(budgetsStatus == 200, $"Cannot fetch my-budgets\n{budgetsBody}");

        int budgetId;
        using (var doc = JsonDocument.Parse(budgetsBody))
        {
            var arr = doc.RootElement;
            Assert.That(arr.ValueKind == JsonValueKind.Array, "my-budgets should return JSON array");
            Assert.That(arr.GetArrayLength() > 0, "No budgets returned for TEST_USER");

            budgetId = arr[0].GetProperty("id").GetInt32();
        }

        var payload = new
        {
            description = $"api_test_income_unauth_{Guid.NewGuid()}",
            amount = 12.34,
            date = DateTime.UtcNow.ToString("O")
        };

        var response = await _request.PostAsync($"/api/budget/{budgetId}/income", new() { DataObject = payload });
        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[{testLabel}] Create income (UNAUTHORIZED) HTTP Status: {status}");
        Console.WriteLine($"[{testLabel}] Create income (UNAUTHORIZED) Response Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got {status}\n{body}");
    }

    // Test 2 (IncomeCreate): Create income should return 400 when request body is invalid

    [Test]
    public async Task Income_Create_Should_Return_400_When_Request_Body_Is_Invalid()
    {
        var testLabel = "[Income Create Test 2 - Invalid Body]";

        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        System.Console.WriteLine($"{testLabel} My-budgets HTTP Status: {myBudgetsStatus}");
        System.Console.WriteLine($"{testLabel} My-budgets Body: {myBudgetsBody}");

        Assert.That(myBudgetsStatus == 200);

        int budgetId;
        using (var doc = JsonDocument.Parse(myBudgetsBody))
        {
            var arr = doc.RootElement;
            Assert.That(arr.ValueKind == JsonValueKind.Array);
            Assert.That(arr.GetArrayLength() > 0);
            budgetId = arr[0].GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0);

        var invalidBody = new
        {
            description = "",
            amount = -10,
            date = ""
        };

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income",
            new() { DataObject = invalidBody }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        System.Console.WriteLine($"{testLabel} HTTP Status: {status}");
        System.Console.WriteLine($"{testLabel} Response Body: {body}");

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 3 (IncomeCreate): Create income should return 200 when request is valid
    [Test]
    public async Task Income_Create_Should_Return_200_When_Request_Is_Valid()
    {
        var testLabel = "[Income Create Test 3 - Valid Body]";

        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        System.Console.WriteLine($"{testLabel} My-budgets HTTP Status: {myBudgetsStatus}");
        System.Console.WriteLine($"{testLabel} My-budgets Body: {myBudgetsBody}");

        Assert.That(myBudgetsStatus == 200);

        int budgetId;
        using (var doc = JsonDocument.Parse(myBudgetsBody))
        {
            var arr = doc.RootElement;
            Assert.That(arr.ValueKind == JsonValueKind.Array);
            Assert.That(arr.GetArrayLength() > 0);
            budgetId = arr[0].GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0);

        var validBody = new
        {
            description = "api_test_income_" + Guid.NewGuid().ToString("N"),
            amount = 123.45,
            date = DateTime.UtcNow.ToString("O")
        };

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income",
            new() { DataObject = validBody }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        System.Console.WriteLine($"{testLabel} HTTP Status: {status}");
        System.Console.WriteLine($"{testLabel} Response Body: {body}");

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }


}
