using System.Buffers.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class TransactionIncomeGetByIdApiTests : BudgetApiTestBase
{
    // Test 1: Get income by id should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task Income_GetById_Should_Return_401_Or_403_When_Unauthorized()
    {
        var budgetId = 1;
        var incomeId = 1;

        var response = await _request.GetAsync($"/api/budget/{budgetId}/income/{incomeId}");

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }

    // Test 2: Get income by id should return 200 when authorized and income exists
    [Test]
    public async Task Income_GetById_Should_Return_200_When_Authorized_And_Income_Exists()
    {
        var testLabel = "[[Income Get Test 2 - Authorized]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Console.WriteLine($"{testLabel} My-budgets HTTP Status: {myBudgetsStatus}");
        Console.WriteLine($"{testLabel} My-budgets Body: {myBudgetsBody}");

        Assert.That(myBudgetsStatus == 200, $"Failed to get my-budgets\n{myBudgetsBody}");

        int budgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            foreach (var b in budgetsDoc.RootElement.EnumerateArray())
            {
                var name = b.GetProperty("name").GetString() ?? "";
                if (name.StartsWith("Test budget"))
                {
                    budgetId = b.GetProperty("id").GetInt32();
                    break;
                }
            }

            if (budgetId <= 0)
            {
                var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
                if (first.ValueKind != JsonValueKind.Undefined)
                    budgetId = first.GetProperty("id").GetInt32();
            }
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var createIncomeResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{budgetId}/income",
            new()
            {
                DataObject = new
                {
                    description = "GetById test income",
                    amount = 123.45,
                    date = DateTime.UtcNow
                }
            }
        );

        var createStatus = createIncomeResponse.Status;
        var createBody = await createIncomeResponse.TextAsync();

        Console.WriteLine($"{testLabel} Create HTTP Status: {createStatus}");
        Console.WriteLine($"{testLabel} Create Response Body: {createBody}");

        Assert.That(createStatus == 200, $"Expected 200 on create, got {createStatus}\n{createBody}");
        Assert.That(string.IsNullOrWhiteSpace(createBody) == false, "Create response body is empty");

        int incomeId;
        using (var createdJson = JsonDocument.Parse(createBody))
            incomeId = createdJson.RootElement.GetProperty("id").GetInt32();

        Assert.That(incomeId > 0, Is.True, "Created income id is missing");

        var getResponse = await authorizedRequest.GetAsync($"/api/budget/{budgetId}/income/{incomeId}");
        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Console.WriteLine($"{testLabel} Get HTTP Status: {status}");
        Console.WriteLine($"{testLabel} Get Response Body: {body}");

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
    }

    // Test 3: Get income by id should return 400 when authorized and income does not exist (API returns IncomeNotFound as BadRequest)
    [Test]
    public async Task Income_GetById_Should_Return_400_When_Authorized_And_Income_Does_Not_Exist()
    {
        var testLabel = "[[Income Get Test 3 - Authorized Not Found]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var myBudgetsResponse = await authorizedRequest.GetAsync("/api/budget/my-budgets");
        var myBudgetsStatus = myBudgetsResponse.Status;
        var myBudgetsBody = await myBudgetsResponse.TextAsync();

        Assert.That(myBudgetsStatus == 200, $"Failed to get my-budgets\n{myBudgetsBody}");

        int budgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            var first = budgetsDoc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Undefined)
                budgetId = first.GetProperty("id").GetInt32();
        }

        Assert.That(budgetId > 0, Is.True, "No budgets available for this user");

        var nonExistingIncomeId = 999999999;

        var getResponse = await authorizedRequest.GetAsync(
            $"/api/budget/{budgetId}/income/{nonExistingIncomeId}"
        );

        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");

        using var errorDoc = JsonDocument.Parse(body);
        var type = errorDoc.RootElement.GetProperty("type").GetString() ?? "";
        Assert.That(type == "Error Transaction.IncomeNotFound", $"Unexpected error type: {type}\n{body}");
    }

    // Test 4: Get income by id should return 400 when authorized but income belongs to another budget (requires create to return created id)
    [Test]
    public async Task Income_GetById_Should_Return_400_When_Authorized_And_Income_Belongs_To_Another_Budget()
    {
        var testLabel = "[[Income Get Test 4 - Authorized Wrong Budget]]";
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

        int firstBudgetId = -1;
        int secondBudgetId = -1;

        using (var budgetsDoc = JsonDocument.Parse(myBudgetsBody))
        {
            var budgets = budgetsDoc.RootElement.EnumerateArray().ToList();
            Assert.That(budgets.Count >= 2, Is.True, "User must have at least two budgets for this test");

            firstBudgetId = budgets[0].GetProperty("id").GetInt32();
            secondBudgetId = budgets[1].GetProperty("id").GetInt32();
        }

        var createIncomeResponse = await authorizedRequest.PostAsync(
            $"/api/budget/{firstBudgetId}/income",
            new()
            {
                DataObject = new
                {
                    description = $"Income for wrong budget test {Guid.NewGuid()}",
                    amount = 50,
                    date = DateTime.UtcNow
                }
            }
        );

        var createStatus = createIncomeResponse.Status;
        var createBody = await createIncomeResponse.TextAsync();

        Assert.That(createStatus == 200, $"Failed to create income\n{createBody}");

        if (string.IsNullOrWhiteSpace(createBody))
            Assert.Inconclusive("Create income returned empty body, cannot extract created income id without a list/get-all endpoint.");

        int incomeId;
        using (var createdJson = JsonDocument.Parse(createBody))
            incomeId = createdJson.RootElement.GetProperty("id").GetInt32();

        Assert.That(incomeId > 0, Is.True, "Created income id is missing");

        var getResponse = await authorizedRequest.GetAsync(
            $"/api/budget/{secondBudgetId}/income/{incomeId}"
        );

        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");

        using var errorDoc = JsonDocument.Parse(body);
        var type = errorDoc.RootElement.GetProperty("type").GetString() ?? "";
        Assert.That(type == "Error Transaction.IncomeNotFound", $"Unexpected error type: {type}\n{body}");
    }

    // Test 5: Get income by id should return 400 when authorized and incomeId is invalid (e.g. 0)
    [Test]
    public async Task Income_GetById_Should_Return_400_When_Authorized_And_IncomeId_Is_Invalid()
    {
        var testLabel = "[[Income Get Test 5 - Authorized Invalid Id]]";
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

        var getResponse = await authorizedRequest.GetAsync($"/api/budget/{budgetId}/income/{invalidIncomeId}");
        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 6: Get income by id should return 400 when authorized and budgetId is invalid (e.g. 0)
    [Test]
    public async Task Income_GetById_Should_Return_400_When_Authorized_And_BudgetId_Is_Invalid()
    {
        var testLabel = "[[Income Get Test 6 - Authorized Invalid BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var invalidBudgetId = 0;
        var incomeId = 1;

        var getResponse = await authorizedRequest.GetAsync($"/api/budget/{invalidBudgetId}/income/{incomeId}");
        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 7: Get income by id should return 404 when authorized and incomeId is not a number (route does not match)
    [Test]
    public async Task Income_GetById_Should_Return_404_When_Authorized_And_IncomeId_Is_Not_A_Number()
    {
        var testLabel = "[[Income Get Test 7 - Authorized NonNumeric IncomeId]]";
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

        var getResponse = await authorizedRequest.GetAsync($"/api/budget/{budgetId}/income/{nonNumericIncomeId}");
        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 8: Get income by id should return 404 when authorized and budgetId is not a number (route does not match)
    [Test]
    public async Task Income_GetById_Should_Return_404_When_Authorized_And_BudgetId_Is_Not_A_Number()
    {
        var testLabel = "[[Income Get Test 8 - Authorized NonNumeric BudgetId]]";
        var authorizedRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            testLabel
        );

        var nonNumericBudgetId = "abc";
        var incomeId = 1;

        var getResponse = await authorizedRequest.GetAsync($"/api/budget/{nonNumericBudgetId}/income/{incomeId}");
        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 404, $"Expected 404, got {status}\n{body}");
    }

    // Test 9: Get income by id should return 401 or 403 when Authorization token is invalid
    [Test]
    public async Task Income_GetById_Should_Return_401_Or_403_When_Token_Is_Invalid()
    {
        var budgetId = 1;
        var incomeId = 1;

        var response = await _request.FetchAsync(
            $"/api/budget/{budgetId}/income/{incomeId}",
            new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer invalid.token.value"
                }
            }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}");
    }

    // Test 10: Get income by id should return 400 when authorized and incomeId is extremely large (int.MaxValue)
    [Test]
    public async Task Income_GetById_Should_Return_400_When_Authorized_And_IncomeId_Is_Too_Large()
    {
        var testLabel = "[[Income Get Test 10 - Authorized Large IncomeId]]";
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

        var largeIncomeId = int.MaxValue;

        var getResponse = await authorizedRequest.GetAsync(
            $"/api/budget/{budgetId}/income/{largeIncomeId}"
        );

        var status = getResponse.Status;
        var body = await getResponse.TextAsync();

        Assert.That(status == 400, $"Expected 400, got {status}\n{body}");
    }

    // Test 11: Get income by id should return 405 when authorized and incomeId is missing (route exists but GET is not allowed)
    [Test]
    public async Task Income_GetById_Should_Return_405_When_Authorized_And_IncomeId_Is_Missing()
    {
        var testLabel = "[[Income Get Test 11 - Missing IncomeId]]";
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

        var response = await authorizedRequest.GetAsync($"/api/budget/{budgetId}/income");
        var status = response.Status;
        var body = await response.TextAsync();

        Assert.That(status == 405, $"Expected 405, got {status}\n{body}");
    }

}