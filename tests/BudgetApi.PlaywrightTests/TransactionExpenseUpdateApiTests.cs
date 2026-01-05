using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionExpenseUpdateApiTests : BudgetApiTestBase
{
    // Test 1: Update expense should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Expense_Update_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;
        var expenseId = 1;

        var response = await _request.PostAsync(
            $"/api/budget/{budgetId}/expenses/{expenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Unauthorized expense update attempt",
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

    // Test 2: Update expense should return 400 when authorized and expense does not exist
    [Test]
    public async Task Expense_Update_Should_Return_400_When_Authorized_And_Expense_Does_Not_Exist()
    {
        var testLabel = "[[Expense Update Test 2 - Expense Not Found]]";
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

        var nonExistingExpenseId = 999999999;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses/{nonExistingExpenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Update non-existing expense",
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

    // Test 3: Update expense should return 400 when authorized and expenseId is invalid (0)
    [Test]
    public async Task Expense_Update_Should_Return_400_When_Authorized_And_ExpenseId_Is_Invalid()
    {
        var testLabel = "[[Expense Update Test 3 - Invalid ExpenseId]]";
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

        var invalidExpenseId = 0;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses/{invalidExpenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Invalid expenseId update",
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

    // Test 4: Update expense should return 404 when authorized and expenseId is not a number (route does not match)
    [Test]
    public async Task Expense_Update_Should_Return_404_When_Authorized_And_ExpenseId_Is_Not_A_Number()
    {
        var testLabel = "[[Expense Update Test 4 - NonNumeric ExpenseId]]";
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

        var nonNumericExpenseId = "abc";

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses/{nonNumericExpenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Non numeric expenseId update",
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

    // Test 5: Update expense should return 400 when authorized and increased amount would cause negative budget balance
    [Test]
    public async Task Expense_Update_Should_Return_400_When_Authorized_And_Insufficient_Funds()
    {
        var testLabel = "[[Expense Update Test 5 - Insufficient Funds]]";
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

        var existingExpenseId = 1;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses/{existingExpenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Increase expense beyond balance",
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

    // Test 6: Update expense should return 404 when authorized and budgetId is not a number (route does not match)
    [Test]
    public async Task Expense_Update_Should_Return_404_When_Authorized_And_BudgetId_Is_Not_A_Number()
    {
        var testLabel = "[[Expense Update Test 6 - NonNumeric BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var nonNumericBudgetId = "abc";
        var expenseId = 1;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{nonNumericBudgetId}/expenses/{expenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Non numeric budgetId update",
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

    // Test 7: Update expense should return 400 when authorized and budgetId is invalid (0)
    [Test]
    public async Task Expense_Update_Should_Return_400_When_Authorized_And_BudgetId_Is_Invalid()
    {
        var testLabel = "[[Expense Update Test 7 - Invalid BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var invalidBudgetId = 0;
        var expenseId = 1;

        var response = await authorizedRequest.PostAsync(
            $"/api/budget/{invalidBudgetId}/expenses/{expenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Invalid budgetId update",
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

    // Test 8: Update expense should return 200 when authorized and expense exists (requires create to return created id)
    [Test]
    public async Task Expense_Update_Should_Return_200_When_Authorized_And_Expense_Exists()
    {
        var testLabel = "[[Expense Update Test 8 - Authorized Happy Path]]";
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

        var createResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = $"Expense to update {Guid.NewGuid()}",
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

        var createStatus = createResponse.Status;
        var createBody = await createResponse.TextAsync();

        Assert.That(createStatus == 200, $"Failed to create expense\n{createBody}");

        if (string.IsNullOrWhiteSpace(createBody))
            Assert.Inconclusive("Create expense returned empty body, cannot extract created expense id to perform update happy path.");

        int expenseId;
        using (var createdJson = System.Text.Json.JsonDocument.Parse(createBody))
            expenseId = createdJson.RootElement.GetProperty("id").GetInt32();

        Assert.That(expenseId > 0, Is.True, "Created expense id is missing");

        var updateResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/expenses/{expenseId}",
            new()
            {
                DataObject = new
                {
                    categoryId = 1,
                    description = "Updated expense",
                    paymentMethod = 0,
                    amount = 15,
                    expenseType = 0,
                    receiptImageUrl = "",
                    frequency = 0,
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow
                }
            }
        );

        var status = updateResponse.Status;
        var body = await updateResponse.TextAsync();

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

}
