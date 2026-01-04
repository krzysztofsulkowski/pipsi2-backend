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

    // Get categories should return list of categories with id and name
    [Test]
    public async Task Categories_Should_Return_List_With_Id_And_Name()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "[Category Test 3]"
        );

        var response = await authRequest.GetAsync("/api/categories");

        var body = await response.TextAsync();

        Console.WriteLine("[Category Test 3] Response Body:");
        Console.WriteLine(body);

        Assert.That(response.Status == 200, $"Expected 200, got {response.Status}\n{body}");

        using var json = JsonDocument.Parse(body);
        Assert.That(json.RootElement.ValueKind == JsonValueKind.Array, "Response is not an array");

        foreach (var category in json.RootElement.EnumerateArray())
        {
            Assert.That(category.TryGetProperty("id", out var id), "Category missing id");
            Assert.That(id.ValueKind == JsonValueKind.Number, "Category id is not a number");

            Assert.That(category.TryGetProperty("name", out var name), "Category missing name");
            Assert.That(name.ValueKind == JsonValueKind.String, "Category name is not a string");
            Assert.That(string.IsNullOrWhiteSpace(name.GetString()) == false, "Category name is empty");
        }
    }

}
