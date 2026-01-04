using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

namespace BudgetApi.PlaywrightTests;

public class PaymentMethodApiTests : BudgetApiTestBase
{
    // Get payment methods dictionary should return 401 or 403 when user is not authenticated
    [Test]
    public async Task Dictionaries_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.GetAsync("/api/dictionaries");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[PaymentMethod Test 1] HTTP Status: " + status);
        Console.WriteLine("[PaymentMethod Test 1] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 401 || status == 403,
            $"Expected 401 or 403, got {status}\n{body}"
        );
    }

    // Get payment methods dictionary should return 200 when user is authenticated
    [Test]
    public async Task Dictionaries_Should_Return_200_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[PaymentMethod Test 2]"
        );

        var response = await authRequest.GetAsync("/api/dictionaries");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[PaymentMethod Test 2] HTTP Status: " + status);
        Console.WriteLine("[PaymentMethod Test 2] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(!string.IsNullOrWhiteSpace(body), "Response body is empty");
    }

    // Get payment methods dictionary should return valid schema
    [Test]
    public async Task Dictionaries_Should_Return_Valid_Schema_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[PaymentMethod Test 3 - Schema]"
        );

        var response = await authRequest.GetAsync("/api/dictionaries");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[PaymentMethod Test 3 - Schema] HTTP Status: " + status);
        Console.WriteLine("[PaymentMethod Test 3 - Schema] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(!string.IsNullOrWhiteSpace(body), "Response body is empty");

        using var json = JsonDocument.Parse(body);
        Assert.That(json.RootElement.ValueKind == JsonValueKind.Array, "Response is not an array");

        foreach (var item in json.RootElement.EnumerateArray())
        {
            Assert.That(item.TryGetProperty("value", out var value), "Missing 'value'");
            Assert.That(value.ValueKind == JsonValueKind.Number, "'value' is not a number");

            Assert.That(item.TryGetProperty("name", out var name), "Missing 'name'");
            Assert.That(name.ValueKind == JsonValueKind.String, "'name' is not a string");
        }
    }

    // Get payment frequencies dictionary should return 401 or 403 when user is not authenticated
    [Test]
    public async Task Frequencies_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.GetAsync("/api/dictionaries/frequencies");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[PaymentMethod Test 4] HTTP Status: " + status);
        Console.WriteLine("[PaymentMethod Test 4] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 401 || status == 403, $"Expected 401 or 403, got {status}\n{body}");
    }

    // Get payment frequencies dictionary should return 200 when user is authenticated
    [Test]
    public async Task Frequencies_Should_Return_200_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[PaymentMethod Test 5]"
        );

        var response = await authRequest.GetAsync("/api/dictionaries/frequencies");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[PaymentMethod Test 5] HTTP Status: " + status);
        Console.WriteLine("[PaymentMethod Test 5] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(!string.IsNullOrWhiteSpace(body), "Response body is empty");
    }

    // Get payment frequencies dictionary should return valid schema
    [Test]
    public async Task Frequencies_Should_Return_Valid_Schema_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[PaymentMethod Test 6 - Frequencies Schema]"
        );

        var response = await authRequest.GetAsync("/api/dictionaries/frequencies");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[PaymentMethod Test 6 - Frequencies Schema] HTTP Status: " + status);
        Console.WriteLine("[PaymentMethod Test 6 - Frequencies Schema] Response Body:");
        Console.WriteLine(body);

        Assert.That(status == 200, $"Expected 200, got {status}\n{body}");
        Assert.That(!string.IsNullOrWhiteSpace(body), "Response body is empty");

        using var json = JsonDocument.Parse(body);
        Assert.That(json.RootElement.ValueKind == JsonValueKind.Array, "Response is not an array");

        foreach (var item in json.RootElement.EnumerateArray())
        {
            Assert.That(item.TryGetProperty("value", out var value), "Missing 'value'");
            Assert.That(value.ValueKind == JsonValueKind.Number, "'value' is not a number");

            Assert.That(item.TryGetProperty("name", out var name), "Missing 'name'");
            Assert.That(name.ValueKind == JsonValueKind.String, "'name' is not a string");
        }
    }
}
