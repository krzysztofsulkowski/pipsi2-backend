using System;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

[TestFixture]
public class UserUpdateUserAPITests : BudgetApiTestBase
{
    // Test 1: Update user should return 200 when request is sent by authorized admin with valid user data
    [Test]
    public async Task User_Update_Should_Return_200_When_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Update_Should_Return_200_When_Admin_Is_Authorized"
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
        var firstUser = usersDoc.RootElement.GetProperty("data")[0];

        var userId = firstUser.GetProperty("userId").GetString();
        var roleId = firstUser.GetProperty("roleId").GetString();
        var roleName = firstUser.GetProperty("roleName").GetString();

        Assert.That(string.IsNullOrWhiteSpace(userId), Is.False);

        var updatedUserName = $"updated_{DateTime.UtcNow.Ticks}";
        var updatedEmail = $"updated_{DateTime.UtcNow.Ticks}@example.com";

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/update-user",
            new()
            {
                DataObject = new
                {
                    userId = userId,
                    userName = updatedUserName,
                    email = updatedEmail,
                    roleId = roleId,
                    roleName = roleName,
                    isLocked = false
                }
            }
        );

        var body = await response.TextAsync();
        Console.WriteLine(body);

        Assert.That(response.Status, Is.EqualTo(200));
    }

    // Test 2: Update user should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task User_Update_Should_Return_401_Or_403_When_Unauthorized()
    {
        var response = await _request.PostAsync(
            "/api/adminPanel/users/update-user",
            new()
            {
                DataObject = new
                {
                    userId = "00000000-0000-0000-0000-000000000000",
                    userName = "unauthorized_update",
                    email = "unauthorized_update@example.com",
                    roleId = "00000000-0000-0000-0000-000000000000",
                    roleName = "User",
                    isLocked = false
                }
            }
        );

        Assert.That(response.Status == 401 || response.Status == 403, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 3: Update user should return 403 when request is sent by authorized non-admin user
    [Test]
    public async Task User_Update_Should_Return_403_When_Non_Admin_Is_Authorized()
    {
        var adminRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Update_Should_Return_403_When_Non_Admin_Is_Authorized_AdminFetch"
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
        var firstUser = usersDoc.RootElement.GetProperty("data")[0];

        var userId = firstUser.GetProperty("userId").GetString();
        var roleId = firstUser.GetProperty("roleId").GetString();
        var roleName = firstUser.GetProperty("roleName").GetString();

        var nonAdminRequest = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "User_Update_Should_Return_403_When_Non_Admin_Is_Authorized"
        );

        var response = await nonAdminRequest.PostAsync(
            "/api/adminPanel/users/update-user",
            new()
            {
                DataObject = new
                {
                    userId = userId,
                    userName = "non_admin_update",
                    email = "non_admin_update@example.com",
                    roleId = roleId,
                    roleName = roleName,
                    isLocked = false
                }
            }
        );

        Assert.That(response.Status, Is.EqualTo(403));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 4: Update user should return 400 when request body is invalid (missing required fields)
    [Test]
    public async Task User_Update_Should_Return_400_When_Request_Body_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Update_Should_Return_400_When_Request_Body_Is_Invalid"
        );

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/update-user",
            new()
            {
                DataObject = new
                {
                    email = "invalid_only_email@example.com"
                }
            }
        );

        Assert.That(response.Status, Is.EqualTo(400));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 5: Update user should return 404 when endpoint path is incorrect
    [Test]
    public async Task User_Update_Should_Return_404_When_Endpoint_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Update_Should_Return_404_When_Endpoint_Is_Invalid"
        );

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/update-users",
            new()
            {
                DataObject = new
                {
                    userId = "00000000-0000-0000-0000-000000000000",
                    userName = "invalid_endpoint_update",
                    email = "invalid_endpoint_update@example.com",
                    roleId = "00000000-0000-0000-0000-000000000000",
                    roleName = "User",
                    isLocked = false
                }
            }
        );

        Assert.That(response.Status, Is.EqualTo(404));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 6: Update user should return 405 when unsupported HTTP method is used
    [Test]
    public async Task User_Update_Should_Return_405_When_Method_Not_Allowed()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Update_Should_Return_405_When_Method_Not_Allowed"
        );

        var response = await authRequest.GetAsync("/api/adminPanel/users/update-user");

        Assert.That(response.Status, Is.EqualTo(405));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 7: Update user should return 401 when request is sent with invalid JWT token
    [Test]
    public async Task User_Update_Should_Return_401_When_Token_Is_Invalid()
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

        var response = await invalidAuthRequest.PostAsync(
            "/api/adminPanel/users/update-user",
            new()
            {
                DataObject = new
                {
                    userId = "00000000-0000-0000-0000-000000000000",
                    userName = "invalid_token_update",
                    email = "invalid_token_update@example.com",
                    roleId = "00000000-0000-0000-0000-000000000000",
                    roleName = "User",
                    isLocked = false
                }
            }
        );

        Assert.That(response.Status, Is.EqualTo(401));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 8: Update user should return 400 or 404 when userId does not exist (depending on backend validation)
    [Test]
    public async Task User_Update_Should_Return_400_Or_404_When_User_Does_Not_Exist()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Update_Should_Return_400_Or_404_When_User_Does_Not_Exist"
        );

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/update-user",
            new()
            {
                DataObject = new
                {
                    userId = "ffffffff-ffff-ffff-ffff-ffffffffffff",
                    userName = "non_existing_user_update",
                    email = "non_existing_user_update@example.com",
                    roleId = "00000000-0000-0000-0000-000000000000",
                    roleName = "User",
                    isLocked = false
                }
            }
        );

        Assert.That(response.Status == 400 || response.Status == 404, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }


}
