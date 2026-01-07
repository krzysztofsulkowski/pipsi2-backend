using System;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

[TestFixture]
public class UserCreateUserAPITests : BudgetApiTestBase
{
    // Test 1: Create user should return 200 when request is sent by authorized admin and roleId is a valid role GUID from roles endpoint
    [Test]
    public async Task User_Create_Should_Return_200_When_Admin_Is_Authorized()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Create_Should_Return_200_When_Admin_Is_Authorized"
        );

        var rolesResponse = await authRequest.GetAsync("/api/adminPanel/users/roles");
        Assert.That(rolesResponse.Status, Is.EqualTo(200));

        var rolesBody = await rolesResponse.TextAsync();
        Console.WriteLine(rolesBody);

        using var rolesDoc = JsonDocument.Parse(rolesBody);

        string? userRoleId = null;

        foreach (var role in rolesDoc.RootElement.EnumerateArray())
        {
            var name = role.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.Equals(name, "User", StringComparison.OrdinalIgnoreCase))
            {
                userRoleId = role.GetProperty("id").GetString();
                break;
            }
        }

        Assert.That(string.IsNullOrWhiteSpace(userRoleId), Is.False);

        var uniqueSuffix = DateTime.UtcNow.Ticks;

        var requestBody = new
        {
            userName = $"apitestuser_{uniqueSuffix}",
            email = $"apitestuser_{uniqueSuffix}@example.com",
            roleId = userRoleId,
            roleName = "User",
            isLocked = false
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/create-user",
            new() { DataObject = requestBody }
        );

        var body = await response.TextAsync();
        Console.WriteLine(body);

        Assert.That(response.Status, Is.EqualTo(200));

        using var doc = JsonDocument.Parse(body);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(doc.RootElement.TryGetProperty("userId", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("userName", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("email", out _), Is.True);
    }

    // Test 2: Create user should return 401 or 403 when request is sent without authentication
    [Test]
    public async Task User_Create_Should_Return_401_Or_403_When_Unauthorized()
    {
        var rolesResponse = await _request.GetAsync("/api/adminPanel/users/roles");
        var rolesBody = await rolesResponse.TextAsync();
        Console.WriteLine(rolesBody);

        string? userRoleId = null;

        if (rolesResponse.Status == 200)
        {
            using var rolesDoc = JsonDocument.Parse(rolesBody);
            foreach (var role in rolesDoc.RootElement.EnumerateArray())
            {
                var name = role.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.Equals(name, "User", StringComparison.OrdinalIgnoreCase))
                {
                    userRoleId = role.GetProperty("id").GetString();
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userRoleId))
            userRoleId = "00000000-0000-0000-0000-000000000000";

        var uniqueSuffix = DateTime.UtcNow.Ticks;

        var requestBody = new
        {
            userName = $"apitestuser_{uniqueSuffix}",
            email = $"apitestuser_{uniqueSuffix}@example.com",
            roleId = userRoleId,
            roleName = "User",
            isLocked = false
        };

        var response = await _request.PostAsync(
            "/api/adminPanel/users/create-user",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status == 401 || response.Status == 403, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 3: Create user should return 403 when request is sent by authorized non-admin user
    [Test]
    public async Task User_Create_Should_Return_403_When_Non_Admin_Is_Authorized()
    {
        var adminRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Create_Should_Return_403_When_Non_Admin_Is_Authorized_AdminRolesFetch"
        );

        var rolesResponse = await adminRequest.GetAsync("/api/adminPanel/users/roles");
        Assert.That(rolesResponse.Status, Is.EqualTo(200));

        var rolesBody = await rolesResponse.TextAsync();
        Console.WriteLine(rolesBody);

        using var rolesDoc = JsonDocument.Parse(rolesBody);

        string? userRoleId = null;

        foreach (var role in rolesDoc.RootElement.EnumerateArray())
        {
            var name = role.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.Equals(name, "User", StringComparison.OrdinalIgnoreCase))
            {
                userRoleId = role.GetProperty("id").GetString();
                break;
            }
        }

        Assert.That(string.IsNullOrWhiteSpace(userRoleId), Is.False);

        var nonAdminRequest = await CreateAuthorizedRequest(
            "TEST_USER2_EMAIL",
            "TEST_USER2_PASSWORD",
            "User_Create_Should_Return_403_When_Non_Admin_Is_Authorized"
        );

        var uniqueSuffix = DateTime.UtcNow.Ticks;

        var requestBody = new
        {
            userName = $"apitestuser_{uniqueSuffix}",
            email = $"apitestuser_{uniqueSuffix}@example.com",
            roleId = userRoleId,
            roleName = "User",
            isLocked = false
        };

        var response = await nonAdminRequest.PostAsync(
            "/api/adminPanel/users/create-user",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(403));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 4: Create user should return 400 when request body is invalid (missing required fields)
    [Test]
    public async Task User_Create_Should_Return_400_When_Request_Body_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Create_Should_Return_400_When_Request_Body_Is_Invalid"
        );

        var requestBody = new
        {
            email = "invalid@example.com"
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/create-user",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(400));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 5: Create user should return 400 or 409 when email already exists
    [Test]
    public async Task User_Create_Should_Return_400_Or_409_When_Email_Already_Exists()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Create_Should_Return_400_Or_409_When_Email_Already_Exists"
        );

        var rolesResponse = await authRequest.GetAsync("/api/adminPanel/users/roles");
        Assert.That(rolesResponse.Status, Is.EqualTo(200));

        var rolesBody = await rolesResponse.TextAsync();
        Console.WriteLine(rolesBody);

        using var rolesDoc = JsonDocument.Parse(rolesBody);

        string? userRoleId = null;

        foreach (var role in rolesDoc.RootElement.EnumerateArray())
        {
            var name = role.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.Equals(name, "User", StringComparison.OrdinalIgnoreCase))
            {
                userRoleId = role.GetProperty("id").GetString();
                break;
            }
        }

        Assert.That(string.IsNullOrWhiteSpace(userRoleId), Is.False);

        var requestBody = new
        {
            userName = "duplicate_user_test",
            email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL"),
            roleId = userRoleId,
            roleName = "User",
            isLocked = false
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/create-user",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status == 400 || response.Status == 409, Is.True);

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 6: Create user should return 405 when unsupported HTTP method is used
    [Test]
    public async Task User_Create_Should_Return_405_When_Method_Not_Allowed()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Create_Should_Return_405_When_Method_Not_Allowed"
        );

        var response = await authRequest.GetAsync("/api/adminPanel/users/create-user");

        Assert.That(response.Status, Is.EqualTo(405));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 7: Create user should return 404 when endpoint path is incorrect
    [Test]
    public async Task User_Create_Should_Return_404_When_Endpoint_Is_Invalid()
    {
        var authRequest = await CreateAuthorizedRequest(
            "TEST_USER_EMAIL",
            "TEST_USER_PASSWORD",
            "User_Create_Should_Return_404_When_Endpoint_Is_Invalid"
        );

        var requestBody = new
        {
            userName = "apitestuser_invalid_endpoint",
            email = "apitestuser_invalid_endpoint@example.com",
            roleId = "00000000-0000-0000-0000-000000000000",
            roleName = "User",
            isLocked = false
        };

        var response = await authRequest.PostAsync(
            "/api/adminPanel/users/create-users",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(404));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

    // Test 8: Create user should return 401 when request is sent with invalid JWT token
    [Test]
    public async Task User_Create_Should_Return_401_When_Token_Is_Invalid()
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
            userName = "apitestuser_invalid_token",
            email = "apitestuser_invalid_token@example.com",
            roleId = "00000000-0000-0000-0000-000000000000",
            roleName = "User",
            isLocked = false
        };

        var response = await invalidAuthRequest.PostAsync(
            "/api/adminPanel/users/create-user",
            new() { DataObject = requestBody }
        );

        Assert.That(response.Status, Is.EqualTo(401));

        var body = await response.TextAsync();
        Console.WriteLine(body);
    }

}
