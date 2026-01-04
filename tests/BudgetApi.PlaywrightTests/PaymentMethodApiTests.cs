using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

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

}
