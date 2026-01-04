using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class CategoryApiTests : BudgetApiTestBase
{
    // Get categories should return 401 or 403 when user is not authenticated
    [Test]
    public async Task Categories_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.GetAsync("/api/categories");

        Assert.That(
            response.Status == 401 || response.Status == 403,
            $"Expected 401 or 403, got {response.Status}\n{await response.TextAsync()}"
        );
    }

    // Get categories should return 200 when user is authenticated
    [Test]
    public async Task Categories_Should_Return_200_When_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Category Test 2]"
        );

        var response = await authRequest.GetAsync("/api/categories");

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Category Test 2] HTTP Status: " + status);
        Console.WriteLine("[Category Test 2] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 200,
            $"Expected 200, got {status}\n{body}"
        );

        Assert.That(!string.IsNullOrWhiteSpace(body));

        JsonDocument.Parse(body);
    }

}
