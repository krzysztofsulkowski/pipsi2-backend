using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

[TestFixture]
public class UserRolesAPITests : BudgetApiTestBase
{
    // Test 1: Get user roles should return 200 when request is sent by authorized admin
    [Test]
    public async Task UserRoles_Get_Should_Return_200_When_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "UserRoles_Get_Should_Return_200_When_Admin_Is_Authorized"
        );

        var response = await authRequest.GetAsync("/api/adminPanel/users/roles");
        Assert.That(response.Status, Is.EqualTo(200));

        var body = await response.TextAsync();
        Console.WriteLine(body);

        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(doc.RootElement.GetArrayLength(), Is.GreaterThan(0));

        var first = doc.RootElement[0];

        Assert.That(first.TryGetProperty("id", out var idProp), Is.True);
        Assert.That(idProp.ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(string.IsNullOrWhiteSpace(idProp.GetString()), Is.False);

        Assert.That(first.TryGetProperty("name", out var nameProp), Is.True);
        Assert.That(nameProp.ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(string.IsNullOrWhiteSpace(nameProp.GetString()), Is.False);
    }
}
