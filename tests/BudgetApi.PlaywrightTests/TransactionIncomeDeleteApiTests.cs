using System.Text.Json;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionIncomeDeleteApiTests : BudgetApiTestBase
{
    // Test 1: Delete income should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Income_Delete_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;
        var incomeId = 1;

        var response = await _request.DeleteAsync(
            $"/api/budget/{budgetId}/income/{incomeId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }

    // Test 2: Delete income should return 400 when authorized and income does not exist
    [Test]
    public async Task Income_Delete_Should_Return_400_When_Authorized_And_Income_Does_Not_Exist()
    {
        var testLabel = "[[Income Delete Test 2 - Income Not Found]]";
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

        var response = await authorizedRequest.DeleteAsync(
            $"/api/budget/{budgetId}/income/{nonExistingIncomeId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 3: Delete income should return 400 when authorized and incomeId is invalid (0)
    [Test]
    public async Task Income_Delete_Should_Return_400_When_Authorized_And_IncomeId_Is_Invalid()
    {
        var testLabel = "[[Income Delete Test 3 - Invalid IncomeId]]";
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

        var response = await authorizedRequest.DeleteAsync(
            $"/api/budget/{budgetId}/income/{invalidIncomeId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 4: Delete income should return 404 when authorized and incomeId is not a number (route does not match)
    [Test]
    public async Task Income_Delete_Should_Return_404_When_Authorized_And_IncomeId_Is_Not_A_Number()
    {
        var testLabel = "[[Income Delete Test 4 - NonNumeric IncomeId]]";
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

        var response = await authorizedRequest.DeleteAsync(
            $"/api/budget/{budgetId}/income/{nonNumericIncomeId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 5: Delete income should return 404 when authorized and budgetId is not a number (route does not match)
    [Test]
    public async Task Income_Delete_Should_Return_404_When_Authorized_And_BudgetId_Is_Not_A_Number()
    {
        var testLabel = "[[Income Delete Test 5 - NonNumeric BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var nonNumericBudgetId = "abc";
        var incomeId = 1;

        var response = await authorizedRequest.DeleteAsync(
            $"/api/budget/{nonNumericBudgetId}/income/{incomeId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 6: Delete income should return 405 when authorized and incomeId is missing (route exists but DELETE is not allowed)
    [Test]
    public async Task Income_Delete_Should_Return_405_When_Authorized_And_IncomeId_Is_Missing()
    {
        var testLabel = "[[Income Delete Test 6 - Missing IncomeId]]";
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

        var response = await authorizedRequest.DeleteAsync($"/api/budget/{budgetId}/income");
        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 405, $"Expected 405, got {status}\n{body}");
    }

    // Test 7: Delete income should return 200 when authorized and income exists (requires create to return created id)
    [Test]
    public async Task Income_Delete_Should_Return_200_When_Authorized_And_Income_Exists()
    {
        var testLabel = "[[Income Delete Test 7 - Authorized Happy Path]]";
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
                    description = $"Income to delete {Guid.NewGuid()}",
                    amount = 25,
                    date = DateTime.UtcNow
                }
            }
        );

        var createStatus = createResponse.Status;
        var createBody = await createResponse.TextAsync();

        Assert.That(createStatus == 200, $"Failed to create income\n{createBody}");

        if (string.IsNullOrWhiteSpace(createBody))
            Assert.Inconclusive("Create income returned empty body, cannot extract created income id to perform delete happy path.");

        int incomeId;
        using (var createdJson = JsonDocument.Parse(createBody))
            incomeId = createdJson.RootElement.GetProperty("id").GetInt32();

        Assert.That(incomeId > 0, Is.True, "Created income id is missing");

        var deleteResponse = await authorizedRequest.DeleteAsync(
            $"/api/budget/{budgetId}/income/{incomeId}"
        );

        var status = deleteResponse.Status;
        var body = await deleteResponse.TextAsync();

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

}