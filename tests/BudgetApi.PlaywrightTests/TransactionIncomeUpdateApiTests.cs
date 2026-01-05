using System.Linq;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionIncomeUpdateApiTests : BudgetApiTestBase
{
    // Test 1: Update income should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Income_Update_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;
        var incomeId = 1;

        var response = await _request.PostAsync(
            $"/api/budget/{budgetId}/income/{incomeId}",
            new()
            {
                DataObject = new
                {
                    description = "Unauthorized update attempt",
                    amount = 10,
                    date = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }

    // Test 2: Update income should return 400 when authorized and income does not exist
    [Test]
    public async Task Income_Update_Should_Return_400_When_Authorized_And_Income_Does_Not_Exist()
    {
        var testLabel = "[[Income Update Test 2 - Income Not Found]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Assert.That(myBudgetsStatus == 200, $"Failed to get my-budgets\n{myBudgetsBody}");
        Assert.That(string.IsNullOrWhiteSpace(myBudgetsBody) == false, "my-budgets response body is empty");

        int budgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var nonExistingIncomeId = 999999999;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income/{nonExistingIncomeId}",
            new()
            {
                DataObject = new
                {
                    description = "Update non existing income",
                    amount = 100,
                    date = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 3: Update income should return 200 when authorized and income exists
    [Test]
    public async Task Income_Update_Should_Return_200_When_Authorized_And_Income_Exists()
    {
        var testLabel = "[[Income Update Test 3 - Authorized Happy Path]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Assert.That(myBudgetsStatus == 200, $"Failed to get my-budgets\n{myBudgetsBody}");
        Assert.That(string.IsNullOrWhiteSpace(myBudgetsBody) == false, "my-budgets response body is empty");

        int budgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var createResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income",
            new()
            {
                DataObject = new
                {
                    description = "Income to update",
                    amount = 50,
                    date = DateTime.UtcNow
                }
            }
        );

        var createStatus = createResponse.Status;
        var createBody = await createResponse.TextAsync();

        Assert.That(createStatus == 200, $"Failed to create income\n{createBody}");
        Assert.That(string.IsNullOrWhiteSpace(createBody) == false, "Create response body is empty");

        int incomeId;
        using (var createdJson = JsonDocument.Parse(createBody))
            incomeId = createdJson.RootElement.GetProperty("id").GetInt32();

        Assert.That(incomeId > 0, Is.True, "Created income id is missing");

        var updateResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income/{incomeId}",
            new()
            {
                DataObject = new
                {
                    description = "Updated income",
                    amount = 75,
                    date = DateTime.UtcNow
                }
            }
        );

        var status = updateResponse.Status;
        var body = await updateResponse.TextAsync();

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

    // Test 4: Update income should return 400 when authorized and incomeId is invalid (0)
    [Test]
    public async Task Income_Update_Should_Return_400_When_Authorized_And_IncomeId_Is_Invalid()
    {
        var testLabel = "[[Income Update Test 4 - Invalid IncomeId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Assert.That(myBudgetsStatus == 200, $"Failed to get my-budgets\n{myBudgetsBody}");
        Assert.That(string.IsNullOrWhiteSpace(myBudgetsBody) == false, "my-budgets response body is empty");

        int budgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var invalidIncomeId = 0;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income/{invalidIncomeId}",
            new()
            {
                DataObject = new
                {
                    description = "Invalid incomeId update",
                    amount = 10,
                    date = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 5: Update income should return 400 when authorized and budgetId is invalid (0)
    [Test]
    public async Task Income_Update_Should_Return_400_When_Authorized_And_BudgetId_Is_Invalid()
    {
        var testLabel = "[[Income Update Test 5 - Invalid BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var invalidBudgetId = 0;
        var incomeId = 1;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{invalidBudgetId}/income/{incomeId}",
            new()
            {
                DataObject = new
                {
                    description = "Invalid budgetId update",
                    amount = 10,
                    date = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 6: Update income should return 404 when authorized and incomeId is not a number (route does not match)
    [Test]
    public async Task Income_Update_Should_Return_404_When_Authorized_And_IncomeId_Is_Not_A_Number()
    {
        var testLabel = "[[Income Update Test 6 - NonNumeric IncomeId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Assert.That(myBudgetsStatus == 200, $"Failed to get my-budgets\n{myBudgetsBody}");
        Assert.That(string.IsNullOrWhiteSpace(myBudgetsBody) == false, "my-budgets response body is empty");

        int budgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var nonNumericIncomeId = "abc";

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income/{nonNumericIncomeId}",
            new()
            {
                DataObject = new
                {
                    description = "Non numeric incomeId update",
                    amount = 10,
                    date = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

}