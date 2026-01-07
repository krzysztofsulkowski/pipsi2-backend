using System;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

[TestFixture]
public class UserGetAllUsersAPITests : BudgetApiTestBase
{
    // Test 1: Get all users should return 200 and valid table response when request is sent by authorized admin
    [Test]
    public async Task User_GetAll_Should_Return_200_When_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetAll_Should_Return_200_When_Admin_Is_Authorized"
        );

        var requestBody = new
        {
            draw = 1,
            start = 0,
            length = 10,
            searchValue = "",
            orderColumn = 0,
            orderDir = "asc",
            extraFilters = new { }
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/get-all-users",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(200));

        var body = await response.TextAsync();
        Console.WriteLine(body);

        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(doc.RootElement.TryGetProperty("data", out var dataProp), Is.True);
        Assert.That(dataProp.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    // Test 2: Get all users should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task User_GetAll_Should_Return_401_Or_403_When_Unauthorized()
    {
        var requestBody = new
        {
            draw = 1,
            start = 0,
            length = 10,
            searchValue = "",
            orderColumn = 0,
            orderDir = "asc",
            extraFilters = new { }
        };

        var response = await _request.PostAsync(
            "/api/adminPanel/users/get-all-users",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status == 401 || response.Status == 403, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 3: Get all users should return 403 when request is sent by authorized non-admin user
    [Test]
    public async Task User_GetAll_Should_Return_403_When_Non_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "User_GetAll_Should_Return_403_When_Non_Admin_Is_Authorized"
        );

        var requestBody = new
        {
            draw = 1,
            start = 0,
            length = 10,
            searchValue = "",
            orderColumn = 0,
            orderDir = "asc",
            extraFilters = new { }
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/get-all-users",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(403));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 4: Get all users should return 400 when request body is missing
    [Test]
    public async Task User_GetAll_Should_Return_400_When_Request_Body_Is_Missing()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetAll_Should_Return_400_When_Request_Body_Is_Missing"
        );

        var response = await authRequest.PostAsync("/api/adminPanel/users/get-all-users");

        Assert.That(response.Status, Is.EqualTo(400));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 5: Get all users should return 405 when unsupported HTTP method is used
    [Test]
    public async Task User_GetAll_Should_Return_405_When_Method_Not_Allowed()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetAll_Should_Return_405_When_Method_Not_Allowed"
        );

        var response = await authRequest.GetAsync("/api/adminPanel/users/get-all-users");

        Assert.That(response.Status, Is.EqualTo(405));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 6: Get all users should return 404 when endpoint path is incorrect
    [Test]
    public async Task User_GetAll_Should_Return_404_When_Endpoint_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetAll_Should_Return_404_When_Endpoint_Is_Invalid"
        );

        var requestBody = new
        {
            draw = 1,
            start = 0,
            length = 10,
            searchValue = "",
            orderColumn = 0,
            orderDir = "asc",
            extraFilters = new { }
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/get-all-user",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(404));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 7: Get all users should return 401 when request is sent with invalid JWT token
    [Test]
    public async Task User_GetAll_Should_Return_401_When_Token_Is_Invalid()
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

        var requestBody = new
        {
            draw = 1,
            start = 0,
            length = 10,
            searchValue = "",
            orderColumn = 0,
            orderDir = "asc",
            extraFilters = new { }
        };

        var response = await invalidAuthRequest.PostAsync(
            "/api/adminPanel/users/get-all-users",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(401));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

}
