using System;
using System.Text.Json;
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

    // Create income should return 200 when request is valid and user is authenticated
    [Test]
    public async Task Income_Create_Should_Return_200_When_Valid_Request()
    {
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Income Test 2]"
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Console.WriteLine("[Income Test 2] My-budgets HTTP Status: " + myBudgetsStatus);
        Console.WriteLine("[Income Test 2] My-budgets Response Body:");
        Console.WriteLine(myBudgetsBody);

        Assert.That(myBudgetsStatus == 200, $"Expected 200 from my-budgets, got {myBudgetsStatus}\n{myBudgetsBody}");

        using var budgetsJson = JsonDocument.Parse(myBudgetsBody);
        Assert.That(budgetsJson.RootElement.ValueKind == JsonValueKind.Array, "my-budgets response is not an array");
        Assert.That(budgetsJson.RootElement.GetArrayLength() > 0, "User has no budgets to use in test");

        var budgetId = budgetsJson.RootElement[0].GetProperty("id").GetInt32();
        Assert.That(budgetId > 0, "Invalid budgetId from my-budgets");

        var response = await authorizedRequest.PostAsync($"/api/budget/{budgetId}/income", new()
        {
            DataObject = new
            {
                description = "test income",
                amount = 123.45,
                date = "2026-01-05T06:25:12.034Z"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Income Test 2] HTTP Status: " + status);
        Console.WriteLine("[Income Test 2] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

    // Create income should return 200 and may return empty body (current API behavior)
    [Test]
    public async Task Income_Create_Should_Return_200_Even_If_ResponseBody_Is_Empty()
    {
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Income Test 3 - Create Empty Body]"
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        using var budgetsJson = JsonDocument.Parse(myBudgetsBody);
        Assert.That(budgetsJson.RootElement.ValueKind == JsonValueKind.Array);
        Assert.That(budgetsJson.RootElement.GetArrayLength() > 0);

        var budgetId = budgetsJson.RootElement[0].GetProperty("id").GetInt32();
        Assert.That(budgetId > 0);

        var response = await authorizedRequest.PostAsync($"/api/budget/{budgetId}/income", new()
        {
            DataObject = new
            {
                description = "income create empty body",
                amount = 11.11,
                date = "2026-01-05T06:25:12.034Z"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Income Test 3 - Create Empty Body] HTTP Status: " + status);
        Console.WriteLine("[Income Test 3 - Create Empty Body] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }


}
