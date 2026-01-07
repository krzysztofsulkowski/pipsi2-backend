using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

[TestFixture]
public class UserGetByIdAPITests : BudgetApiTestBase
{
    // Test 1: Get user by id should return 200 when request is sent by authorized admin for existing user
    [Test]
    public async Task User_GetById_Should_Return_200_When_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_200_When_Admin_Is_Authorized"
        );

        var usersResponse = await authRequest.PostAsync(
            "/api/adminPanel/users/get-all-users",
            new()
            {
                DataObject = new
                {
                    draw = 1,
                    start = 0,
                    length = 1,
                    searchValue = "",
                    orderColumn = 0,
                    orderDir = "asc",
                    extraFilters = new { }
                }
            }
        );

        Assert.That(usersResponse.Status, Is.EqualTo(200));

        var usersBody = await usersResponse.TextAsync();
        Console.WriteLine(usersBody);

        using var usersDoc = JsonDocument.Parse(usersBody);
        var userId = usersDoc.RootElement
            .GetProperty("data")[0]
            .GetProperty("userId")
            .GetString();

        Assert.That(string.IsNullOrWhiteSpace(userId), Is.False);

        var response = await authRequest.GetAsync(
            $"/api/adminPanel/users/get-user-by-id?userId={userId}"
        );

        Assert.That(response.Status, Is.EqualTo(200));

        var body = await response.TextAsync();
        Console.WriteLine(body);

        using var doc = JsonDocument.Parse(body);

        Assert.That(doc.RootElement.TryGetProperty("userId", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("userName", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("roleId", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("roleName", out _), Is.True);
    }

    // Test 2: Get user by id should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task User_GetById_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.GetAsync(
            "/api/adminPanel/users/get-user-by-id?userId=00000000-0000-0000-0000-000000000000"
        );

        Assert.That(response.Status == 401 || response.Status == 403, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 3: Get user by id should return 403 when request is sent by authorized non-admin user
    [Test]
    public async Task User_GetById_Should_Return_403_When_Non_Admin_Is_Authorized()
    {
        var adminRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_403_When_Non_Admin_Is_Authorized_AdminFetch"
        );

        var usersResponse = await adminRequest.PostAsync(
            "/api/adminPanel/users/get-all-users",
            new()
            {
                DataObject = new
                {
                    draw = 1,
                    start = 0,
                    length = 1,
                    searchValue = "",
                    orderColumn = 0,
                    orderDir = "asc",
                    extraFilters = new { }
                }
            }
        );

        Assert.That(usersResponse.Status, Is.EqualTo(200));

        var usersBody = await usersResponse.TextAsync();
        Console.WriteLine(usersBody);

        using var usersDoc = System.Text.Json.JsonDocument.Parse(usersBody);
        var targetUserId = usersDoc.RootElement.GetProperty("data")[0].GetProperty("userId").GetString();

        var nonAdminRequest = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "User_GetById_Should_Return_403_When_Non_Admin_Is_Authorized"
        );

        var response = await nonAdminRequest.GetAsync(
            $"/api/adminPanel/users/get-user-by-id?userId={targetUserId}"
        );

        Assert.That(response.Status, Is.EqualTo(403));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 4: Get user by id should return 400 or 404 when user does not exist (depending on backend validation)
    [Test]
    public async Task User_GetById_Should_Return_400_Or_404_When_User_Does_Not_Exist()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_400_Or_404_When_User_Does_Not_Exist"
        );

        var response = await authRequest.GetAsync(
            "/api/adminPanel/users/get-user-by-id?userId=ffffffff-ffff-ffff-ffff-ffffffffffff"
        );

        Assert.That(response.Status == 400 || response.Status == 404, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 5: Get user by id should return 400 when userId query parameter is missing
    [Test]
    public async Task User_GetById_Should_Return_400_When_UserId_Is_Missing()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_400_When_UserId_Is_Missing"
        );

        var response = await authRequest.GetAsync(
            "/api/adminPanel/users/get-user-by-id"
        );

        Assert.That(response.Status, Is.EqualTo(400));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 6: Get user by id should return 400 when userId is not a valid GUID
    [Test]
    public async Task User_GetById_Should_Return_400_When_UserId_Is_Not_Valid_Guid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_400_When_UserId_Is_Not_Valid_Guid"
        );

        var response = await authRequest.GetAsync(
            "/api/adminPanel/users/get-user-by-id?userId=not-a-guid"
        );

        Assert.That(response.Status, Is.EqualTo(400));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 7: Get user by id should return 405 when unsupported HTTP method is used
    [Test]
    public async Task User_GetById_Should_Return_405_When_Method_Not_Allowed()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_405_When_Method_Not_Allowed"
        );

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/get-user-by-id?userId=00000000-0000-0000-0000-000000000000"
        );

        Assert.That(response.Status, Is.EqualTo(405));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 8: Get user by id should return 401 when request is sent with invalid JWT token
    [Test]
    public async Task User_GetById_Should_Return_401_When_Token_Is_Invalid()
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

        var response = await invalidAuthRequest.GetAsync(
            "/api/adminPanel/users/get-user-by-id?userId=00000000-0000-0000-0000-000000000000"
        );

        Assert.That(response.Status, Is.EqualTo(401));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 9: Get user by id should return 400 when query parameter name is incorrect
    [Test]
    public async Task User_GetById_Should_Return_400_When_Query_Param_Name_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_GetById_Should_Return_400_When_Query_Param_Name_Is_Invalid"
        );

        var response = await authRequest.GetAsync(
            "/api/adminPanel/users/get-user-by-id?id=00000000-0000-0000-0000-000000000000"
        );

        Assert.That(response.Status, Is.EqualTo(400));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

}
