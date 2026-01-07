using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
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

    // Test 2: Get user roles should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task UserRoles_Get_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.GetAsync("/api/adminPanel/users/roles");

        Assert.That(response.Status == 401 || response.Status == 403, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 3: Get user roles should return 403 when request is sent by authorized non-admin user
    [Test]
    public async Task UserRoles_Get_Should_Return_403_When_Non_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "UserRoles_Get_Should_Return_403_When_Non_Admin_Is_Authorized"
        );

        var response = await authRequest.GetAsync("/api/adminPanel/users/roles");

        Assert.That(response.Status, Is.EqualTo(403));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 4: Get user roles should return 405 when unsupported HTTP method is used
    [Test]
    public async Task UserRoles_Post_Should_Return_405_When_Method_Not_Allowed()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "UserRoles_Post_Should_Return_405_When_Method_Not_Allowed"
        );

        var response = await authRequest.PostAsync("/api/adminPanel/users/roles");

        Assert.That(response.Status, Is.EqualTo(405));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 5: Get user roles should return 404 when endpoint path is incorrect
    [Test]
    public async Task UserRoles_Get_Should_Return_404_When_Endpoint_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "UserRoles_Get_Should_Return_404_When_Endpoint_Is_Invalid"
        );

        var response = await authRequest.GetAsync("/api/adminPanel/users/role");

        Assert.That(response.Status, Is.EqualTo(404));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 6: Get user roles should return 401 when request is sent with invalid JWT token
    [Test]
    public async Task UserRoles_Get_Should_Return_401_When_Token_Is_Invalid()
    {
        var invalidAuthRequest = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" },
            { "Authorization", "Bearer invalid.token.value" }
        }
        });

        var response = await invalidAuthRequest.GetAsync("/api/adminPanel/users/roles");

        Assert.That(response.Status, Is.EqualTo(401));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 7: Get user roles should return 401 when Authorization header is missing Bearer token
    [Test]
    public async Task UserRoles_Get_Should_Return_401_When_Authorization_Header_Is_Missing()
    {
        var noAuthRequest = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" }
        }
        });

        var response = await noAuthRequest.GetAsync("/api/adminPanel/users/roles");

        Assert.That(response.Status, Is.EqualTo(401));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 8: Get user roles should return 405 when HEAD method is used
    [Test]
    public async Task UserRoles_Head_Should_Return_405_When_Method_Not_Allowed()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "UserRoles_Head_Should_Return_405_When_Method_Not_Allowed"
        );

        var response = await authRequest.FetchAsync(
            "/api/adminPanel/users/roles",
            new APIRequestContextOptions { Method = "HEAD" }
        );

        Assert.That(response.Status, Is.EqualTo(405));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 9: OPTIONS on user roles endpoint should return 200/204 when enabled, otherwise 401/403/405 depending on configuration
    [Test]
    public async Task UserRoles_Options_Should_Return_200_204_401_403_Or_405_Depending_On_Config()
    {
        var response = await _request.FetchAsync(
            "/api/adminPanel/users/roles",
            new APIRequestContextOptions { Method = "OPTIONS" }
        );

        Assert.That(
            response.Status == 200 ||
            response.Status == 204 ||
            response.Status == 401 ||
            response.Status == 403 ||
            response.Status == 405,
            Is.True
        );

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }


}
