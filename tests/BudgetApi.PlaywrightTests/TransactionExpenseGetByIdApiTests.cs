using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionExpenseGetByIdApiTests : BudgetApiTestBase
{
    // Test 1: Get expense by id should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Expense_GetById_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;
        var expenseId = 1;

        var response = await _request.GetAsync($"/api/budget/{budgetId}/expenses/{expenseId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }

    // Test 2: Get expense by id should return 400 when authorized and expense does not exist
    [Test]
    public async Task Expense_GetById_Should_Return_400_When_Authorized_And_Expense_Does_Not_Exist()
    {
        var testLabel = "[[Expense GetById Test 2 - Expense Not Found]]";
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

        var response = await authorizedRequest.GetAsync(
            $"/api/budget/{budgetId}/expenses/{nonExistingExpenseId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 3: Get expense by id should return 400 when authorized and expenseId is invalid (0)
    [Test]
    public async Task Expense_GetById_Should_Return_400_When_Authorized_And_ExpenseId_Is_Invalid()
    {
        var testLabel = "[[Expense GetById Test 3 - Invalid ExpenseId]]";
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

        var response = await authorizedRequest.GetAsync(
            $"/api/budget/{budgetId}/expenses/{invalidExpenseId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 4: Get expense by id should return 404 when authorized and expenseId is not a number (route does not match)
    [Test]
    public async Task Expense_GetById_Should_Return_404_When_Authorized_And_ExpenseId_Is_Not_A_Number()
    {
        var testLabel = "[[Expense GetById Test 4 - NonNumeric ExpenseId]]";
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

        var response = await authorizedRequest.GetAsync(
            $"/api/budget/{budgetId}/expenses/{nonNumericExpenseId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 5: Get expense by id should return 404 when authorized and budgetId is not a number (route does not match)
    [Test]
    public async Task Expense_GetById_Should_Return_404_When_Authorized_And_BudgetId_Is_Not_A_Number()
    {
        var testLabel = "[[Expense GetById Test 5 - NonNumeric BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var nonNumericBudgetId = "abc";
        var expenseId = 1;

        var response = await authorizedRequest.GetAsync(
            $"/api/budget/{nonNumericBudgetId}/expenses/{expenseId}"
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 6: Get expense by id should return 405 when authorized and expenseId is missing (route exists but GET is not allowed)
    [Test]
    public async Task Expense_GetById_Should_Return_405_When_Authorized_And_ExpenseId_Is_Missing()
    {
        var testLabel = "[[Expense GetById Test 6 - Missing ExpenseId]]";
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

        var response = await authorizedRequest.GetAsync($"/api/budget/{budgetId}/expenses");
        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 405, $"Expected 405, got {status}\n{body}");
    }

    // Test 7: Get expense by id should return 200 when authorized and expense exists (requires create to return created id)
    [Test]
    public async Task Expense_GetById_Should_Return_200_When_Authorized_And_Expense_Exists()
    {
        var testLabel = "[[Expense GetById Test 7 - Authorized Happy Path]]";
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
                    description = $"Expense for get by id {Guid.NewGuid()}",
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
            Assert.Inconclusive("Create expense returned empty body, cannot extract created expense id to perform get by id happy path.");

        int expenseId;
        using (var createdJson = System.Text.Json.JsonDocument.Parse(createBody))
            expenseId = createdJson.RootElement.GetProperty("id").GetInt32();

        Assert.That(expenseId > 0, Is.True, "Created expense id is missing");

        var getResponse = await authorizedRequest.GetAsync(
            $"/api/budget/{budgetId}/expenses/{expenseId}"
        );

        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

}
