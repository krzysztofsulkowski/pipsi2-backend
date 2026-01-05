using System;
using System.Text.Json;
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

    // Test 2 (TransactionSearch): Search transactions should return 200 and valid table structure when authorized user sends a correct search request for an existing budget.
    [Test]
    public async Task Transaction_Search_Should_Return_200_When_Request_Is_Valid()
    {
        var testLabel = "Transaction Search Test 2";

        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        var budgetId = FindBudgetIdByName(myBudgetsBody, "Test budget 323667b8-4420-416c-b921-bf9d5a2b624f");
        Assert.That(budgetId > 0, "Test budget not found");

        var searchResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/transactions/search",
            new()
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
            }
        );

        var status = searchResponse.Status;
        var body = await searchResponse.TextAsync();

        Console.WriteLine($"[{testLabel}] HTTP Status: {status}");
        Console.WriteLine($"[{testLabel}] Response Body: {body}");

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");

        using var json = JsonDocument.Parse(body);
        Assert.That(json.RootElement.TryGetProperty("data", out _), "Missing data property");
        Assert.That(json.RootElement.TryGetProperty("recordsTotal", out _), "Missing recordsTotal property");
        Assert.That(json.RootElement.TryGetProperty("recordsFiltered", out _), "Missing recordsFiltered property");
    }

    // Test 3 (TransactionSearch): Search transactions should return 401 or 403 when request is sent without authorization token.
    [Test]
    public async Task Transaction_Search_Should_Return_401_Or_403_When_Unauthorized()
    {
        var testLabel = "Transaction Search Test 3 - Unauthorized";

        var budgetId = 1;

        var response = await _request.PostAsync(
            $"/api/budget/{budgetId}/transactions/search",
            new()
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
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[{testLabel}] HTTP Status: {status}");
        Console.WriteLine($"[{testLabel}] Response Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }


}
