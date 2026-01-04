using System;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetCreateAPITests : BudgetApiTestBase
{
    // Test 1(BudgetCreate): Create budget should return 401/403 when user is not authenticated
    [Test, Order(1)]
    public async Task Budget_Create_Should_Return_401_Or_403_When_Unauthorized()
    {
        var payload = new { };

        Console.WriteLine("[Test 1] Start: create budget WITHOUT authentication");
        Console.WriteLine($"[Test 1] Create payload: {JsonSerializer.Serialize(payload)}");

        var response = await _request.PostAsync("/api/budget/create", new()
        {
            DataObject = payload
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 1] Create HTTP Status: {status}");
        Console.WriteLine($"[Test 1] Create Body: {body}");

        Assert.That(status == 401 || status == 403,
            $"Expected 401 or 403 when unauthorized, got HTTP {status}\n{body}");
    }

    // Test 2(BudgetCreate): Create budget should return 200 when user is authenticated
    [Test, Order(2)]
    public async Task Budget_Create_Should_Return_200_When_Authorized()
    {
        Console.WriteLine("[Test 2] Start: create budget WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 2");

        var createPayload = new
        {
            id = 0,
            name = $"Test budget {Guid.NewGuid()}"
        };

        var response = await authRequest.PostAsync(
            "/api/budget/create",
            new() { DataObject = createPayload }
        );

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine($"[Test 2] Create budget HTTP Status: {status}");
        Console.WriteLine($"[Test 2] Create budget Body: {body}");

        Assert.That(status == 200,
            $"Expected 200 when creating budget, got HTTP {status}\n{body}");
    }

    // Test 3(BudgetCreate): Create budget should return 400/422 when name is missing/empty (validation)
    [Test, Order(3)]
    public async Task Budget_Create_Should_Return_400_Or_422_When_Name_Is_Missing_Or_Empty()
    {
        Console.WriteLine("[Test 3] Start: create budget with invalid name (missing/empty) WITH authentication");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 3");

        var payloadMissing = new { id = 0 };
        var payloadEmpty = new { id = 0, name = "" };

        Console.WriteLine($"[Test 3] Payload missing name: {JsonSerializer.Serialize(payloadMissing)}");
        var respMissing = await authRequest.PostAsync("/api/budget/create", new() { DataObject = payloadMissing });
        var statusMissing = respMissing.Status;
        var bodyMissing = await respMissing.TextAsync();
        Console.WriteLine($"[Test 3] Missing-name HTTP Status: {statusMissing}");
        Console.WriteLine($"[Test 3] Missing-name Body: {bodyMissing}");

        Console.WriteLine($"[Test 3] Payload empty name: {JsonSerializer.Serialize(payloadEmpty)}");
        var respEmpty = await authRequest.PostAsync("/api/budget/create", new() { DataObject = payloadEmpty });
        var statusEmpty = respEmpty.Status;
        var bodyEmpty = await respEmpty.TextAsync();
        Console.WriteLine($"[Test 3] Empty-name HTTP Status: {statusEmpty}");
        Console.WriteLine($"[Test 3] Empty-name Body: {bodyEmpty}");

        Assert.That(statusMissing == 400 || statusMissing == 422,
            $"Expected 400 or 422 for missing name, got HTTP {statusMissing}\n{bodyMissing}");
        Assert.That(statusEmpty == 400 || statusEmpty == 422,
            $"Expected 400 or 422 for empty name, got HTTP {statusEmpty}\n{bodyEmpty}");
    }

    // Test 4(BudgetCreate): Create budget (valid name) should return 200/201 and created budget should appear in my-budgets
    [Test, Order(4)]
    public async Task Budget_Create_Should_Appear_In_MyBudgets_After_Create()
    {
        Console.WriteLine("[Test 4] Start: create budget with valid name, then verify it appears in my-budgets");

        var authRequest = await CreateAuthorizedRequest("TEST_USER_EMAIL", "TEST_USER_PASSWORD", "Test 4");

        var createdName = $"Test budget {Guid.NewGuid()}";
        var createPayload = new { id = 0, name = createdName };

        Console.WriteLine($"[Test 4] Create payload: {JsonSerializer.Serialize(createPayload)}");

        var createResponse = await authRequest.PostAsync("/api/budget/create", new() { DataObject = createPayload });
        var createStatus = createResponse.Status;
        var createBody = await createResponse.TextAsync();

        Console.WriteLine($"[Test 4] Create HTTP Status: {createStatus}");
        Console.WriteLine($"[Test 4] Create Body: {createBody}");

        Assert.That(createStatus == 200 || createStatus == 201,
            $"Expected 200/201 when creating budget, got HTTP {createStatus}\n{createBody}");

        var listResponse = await authRequest.GetAsync("/api/budget/my-budgets");
        var listStatus = listResponse.Status;
        var listBody = await listResponse.TextAsync();

        Console.WriteLine($"[Test 4] My-budgets HTTP Status: {listStatus}");
        Console.WriteLine($"[Test 4] My-budgets Body: {listBody}");

        Assert.That(listStatus == 200,
            $"Expected 200 from my-budgets after create, got HTTP {listStatus}\n{listBody}");

        Assert.That(listBody.Contains(createdName),
            $"Expected created budget name to appear in my-budgets, but it was not found.\n{listBody}");
    }


}
