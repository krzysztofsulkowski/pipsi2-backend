using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionExpenseCreateApiTests : BudgetApiTestBase
{
    // Test 1: Create expense should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Expense_Create_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;

        var response = await _request.PostAsync(
            $"/api/budget/{budgetId}/expenses",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Unauthorized expense create attempt",
                    paymentMethod = 0,
                    amount = 10,
                    expenseType = 0,
                    receiptImageUrl = "",
                    frequency = 0,
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }

    // Test 2: Create expense should return 400 when authorized and amount exceeds available budget balance
    [Test]
    public async Task Expense_Create_Should_Return_400_When_Authorized_And_Insufficient_Funds()
    {
        var testLabel = "[[Expense Create Test 2 - Insufficient Funds]]";
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

        using (var budgetsDoc = System.Text.Json.JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Expense exceeding balance",
                    paymentMethod = 0,
                    amount = 9999999,
                    expenseType = 0,
                    receiptImageUrl = "",
                    frequency = 0,
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    [Test]
    public async Task Expense_Create_Should_Return_400_When_Authorized_And_BudgetId_Is_Invalid()
    {
        var testLabel = "[[Expense Create Test 3 - Invalid BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var invalidBudgetId = 0;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{invalidBudgetId}/expenses",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Invalid budgetId expense",
                    paymentMethod = 0,
                    amount = 10,
                    expenseType = 0,
                    receiptImageUrl = "",
                    frequency = 0,
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 4: Create expense should return 404 when authorized and budgetId is not a number (route does not match)
    [Test]
    public async Task Expense_Create_Should_Return_404_When_Authorized_And_BudgetId_Is_Not_A_Number()
    {
        var testLabel = "[[Expense Create Test 4 - NonNumeric BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var nonNumericBudgetId = "abc";

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{nonNumericBudgetId}/expenses",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Non numeric budgetId expense",
                    paymentMethod = 0,
                    amount = 10,
                    expenseType = 0,
                    receiptImageUrl = "",
                    frequency = 0,
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 5: Create expense should return 200 when authorized and request data is valid
    [Test]
    public async Task Expense_Create_Should_Return_200_When_Authorized_And_Data_Is_Valid()
    {
        var testLabel = "[[Expense Create Test 5 - Authorized Happy Path]]";
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

        using (var budgetsDoc = System.Text.Json.JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = $"Valid expense {Guid.NewGuid()}",
                    paymentMethod = 0,
                    amount = 10,
                    expenseType = 0,
                    receiptImageUrl = "",
                    frequency = 0,
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

}
