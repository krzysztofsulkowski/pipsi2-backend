using System;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class BudgetCreateAPITests : BudgetApiTestBase
{
    // Test 1(BudgetCreate): Create budget should return 401/403 when user is not authenticated
    [Test]
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
    [Test]
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
}
