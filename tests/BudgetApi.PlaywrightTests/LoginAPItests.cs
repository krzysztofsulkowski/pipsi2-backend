using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class LoginAPITests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;

    // Backend port
    private const string Port = "64052";

    // HTTPS + HTTP fallback
    private const string HttpsUrl = $"https://localhost:{Port}";
    private const string HttpUrl = $"http://localhost:{Port}";
    private string _baseUrl = HttpsUrl;

    [OneTimeSetUp]
    public async Task Setup()
    {
        // Initialize Playwright & API context
        _playwright = await Playwright.CreateAsync();

        _request = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true, // allow localhost self-signed certs
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Content-Type", "application/json" }
            }
        });

        Console.WriteLine($"[Setup] Login API tests initialized. BaseURL={_baseUrl}");
    }

    // Test 1(Login): should succeed for valid registered user
    [Test, Order(1)]
    public async Task Login_Should_Succeed_For_Registered_User()
    {
        // Auto-generated credentials
        var email = $"login_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var password = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Payload for registration
        var registrationPayload = new
        {
            email,
            username,
            password
        };

        Console.WriteLine("[Test 1] Registering test user before login");
        Console.WriteLine($"[Test 1] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        IAPIResponse registerResponse;

        try
        {
            // Attempt registration over HTTPS
            registerResponse = await _request.PostAsync("/api/authentication/register",
                new() { DataObject = registrationPayload });
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"[Test 1] HTTPS registration failed: {ex.Message}");
            Console.WriteLine("[Test 1] Retrying on HTTP...");

            // Switch to HTTP
            await _request.DisposeAsync();
            _baseUrl = HttpUrl;

            _request = await _playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = _baseUrl,
                IgnoreHTTPSErrors = true,
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "Content-Type", "application/json" }
                }
            });

            // Retry over HTTP
            registerResponse = await _request.PostAsync("/api/authentication/register",
                new() { DataObject = registrationPayload });
        }

        // Registration result
        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 1] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 1] Registration Body: {regBody}");

        // Must succeed
        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before login test:\nHTTP {regStatus}\n{regBody}");

        // Payload for login
        var loginPayload = new
        {
            email,
            password
        };

        Console.WriteLine("[Test 1] Attempting login with newly registered user");
        Console.WriteLine($"[Test 1] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 1] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 1] Login Body: {loginBody}");

        // Login MUST return HTTP 200
        Assert.That(loginStatus, Is.InRange(200, 299),
            $"Login failed for valid test user:\nHTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 1] OK: Login succeeded for registered user.");
    }

    // Test 2(Login): should return error when password is incorrect
    [Test, Order(2)]
    public async Task Login_Should_Return_Error_When_Password_Is_Wrong()
    {
        // Generate test credentials
        var email = $"loginwrong_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);

        // Correct password for registration
        var correctPassword = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Wrong password used for login attempt
        var wrongPassword = "Invalid123!";

        // Payload for registration
        var registrationPayload = new
        {
            email,
            username,
            password = correctPassword
        };

        Console.WriteLine("[Test 2] Registering test user for incorrect password test");
        Console.WriteLine($"[Test 2] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        // Register the user
        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 2] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 2] Registration Body: {regBody}");

        // Registration must succeed
        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before wrong-password test:\nHTTP {regStatus}\n{regBody}");

        // Payload for login with WRONG password
        var loginPayload = new
        {
            email,
            password = wrongPassword
        };

        Console.WriteLine("[Test 2] Attempting login with WRONG password");
        Console.WriteLine($"[Test 2] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 2] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 2] Login Body: {loginBody}");

        // Expected: 400 or 401 for invalid credentials
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for wrong password, got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 2] OK: Login rejected correctly with wrong password.");
    }

    // Test 3(Login): should return error when email is incorrect
    [Test, Order(3)]
    public async Task Login_Should_Return_Error_When_Email_Is_Wrong()
    {
        // Generate correct user data
        var correctEmail = $"loginemail_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var password = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Different (wrong) email for login attempt
        var wrongEmail = $"wrong_{Guid.NewGuid()}@example.com";

        // Payload for registration
        var registrationPayload = new
        {
            email = correctEmail,
            username,
            password
        };

        Console.WriteLine("[Test 3] Registering test user for wrong-email test");
        Console.WriteLine($"[Test 3] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 3] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 3] Registration Body: {regBody}");

        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before wrong-email test:\nHTTP {regStatus}\n{regBody}");

        // Payload for login with WRONG email but correct password
        var loginPayload = new
        {
            email = wrongEmail,
            password
        };

        Console.WriteLine("[Test 3] Attempting login with WRONG email");
        Console.WriteLine($"[Test 3] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 3] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 3] Login Body: {loginBody}");

        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for wrong email, got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 3] OK: Login rejected correctly with wrong email.");
    }

    // Test 4(Login): should return error when password is empty
    [Test, Order(4)]
    public async Task Login_Should_Return_Error_When_Password_Is_Empty()
    {
        // Generate test credentials
        var email = $"loginempty_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var correctPassword = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Payload for registration
        var registrationPayload = new
        {
            email,
            username,
            password = correctPassword
        };

        Console.WriteLine("[Test 4] Registering user for empty-password login test");
        Console.WriteLine($"[Test 4] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        // Register the user
        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 4] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 4] Registration Body: {regBody}");

        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before empty-password login test:\nHTTP {regStatus}\n{regBody}");

        // Payload for login with EMPTY password
        var loginPayload = new
        {
            email,
            password = ""
        };

        Console.WriteLine("[Test 4] Attempting login with EMPTY password");
        Console.WriteLine($"[Test 4] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login request
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 4] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 4] Login Body: {loginBody}");

        // Expected: 400 or 401 for invalid credentials
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for empty password, got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 4] OK: Login rejected correctly with empty password.");
    }

    // Test 5(Login): should return error when email is empty
    [Test, Order(5)]
    public async Task Login_Should_Return_Error_When_Email_Is_Empty()
    {
        // Generate test credentials
        var email = $"loginnoemail_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var correctPassword = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Payload for registration
        var registrationPayload = new
        {
            email,
            username,
            password = correctPassword
        };

        Console.WriteLine("[Test 5] Registering user for empty-email login test");
        Console.WriteLine($"[Test 5] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        // Register the user first
        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 5] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 5] Registration Body: {regBody}");

        // Registration must succeed
        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before empty-email login test:\nHTTP {regStatus}\n{regBody}");

        // Payload for login with EMPTY email
        var loginPayload = new
        {
            email = "",
            password = correctPassword
        };

        Console.WriteLine("[Test 5] Attempting login with EMPTY email");
        Console.WriteLine($"[Test 5] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login request
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 5] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 5] Login Body: {loginBody}");

        // Expected: 400 or 401
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for empty email, got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 5] OK: Login rejected correctly with empty email.");
    }

    // Test 6(Login): should return error when both email and password are empty
    [Test, Order(6)]
    public async Task Login_Should_Return_Error_When_Email_And_Password_Are_Empty()
    {
        // Generate valid credentials for registration
        var email = $"loginboth_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var correctPassword = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Registration payload
        var registrationPayload = new
        {
            email,
            username,
            password = correctPassword
        };

        Console.WriteLine("[Test 6] Registering user for empty email & password test");
        Console.WriteLine($"[Test 6] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        // Register a valid user first
        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 6] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 6] Registration Body: {regBody}");

        // Registration must be successful
        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before empty-fields login test:\nHTTP {regStatus}\n{regBody}");

        // Payload with BOTH fields empty
        var loginPayload = new
        {
            email = "",
            password = ""
        };

        Console.WriteLine("[Test 6] Attempting login with EMPTY email AND EMPTY password");
        Console.WriteLine($"[Test 6] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login request
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 6] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 6] Login Body: {loginBody}");

        // Expected: 400 or 401
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for empty email AND password, but got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 6] OK: Login rejected correctly with empty email AND password.");
    }

    // Test 7(Login): registration attempt with empty fields should fail, and login should fail too
    [Test, Order(7)]
    public async Task Register_And_Login_Should_Fail_With_Empty_Email_And_Password()
    {
        // Payload with all empty fields for registration
        var registrationPayload = new
        {
            email = "",
            username = "",
            password = ""
        };

        Console.WriteLine("[Test 7] Attempting registration with EMPTY email, username, and password");
        Console.WriteLine($"[Test 7] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        // Try to register with empty email, username and password
        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 7] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 7] Registration Body: {regBody}");

        // Expected: registration MUST fail (validation error)
        Assert.That(regStatus, Is.InRange(400, 499),
            $"Expected registration to fail with empty fields, but got HTTP {regStatus}\n{regBody}");

        Console.WriteLine("[Test 7] OK: Registration with empty fields correctly rejected.");

        // Login payload also empty
        var loginPayload = new
        {
            email = "",
            password = ""
        };

        Console.WriteLine("[Test 7] Attempting login with EMPTY email and password");
        Console.WriteLine($"[Test 7] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Attempt login
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 7] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 7] Login Body: {loginBody}");

        // Login must also fail
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected login to fail with empty email and password, but got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 7] OK: Login with empty fields correctly rejected.");
    }

    // Test 8(Login): should return error when email format is invalid
    [Test, Order(8)]
    public async Task Login_Should_Return_Error_When_Email_Format_Is_Invalid()
    {
        // Generate proper credentials for registration
        var validEmail = $"logininvalid_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}".Substring(0, 12);
        var validPassword = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        // Invalid email formats to test
        var invalidEmail = "notanemail";

        // Payload for registering the valid user
        var registrationPayload = new
        {
            email = validEmail,
            username,
            password = validPassword
        };

        Console.WriteLine("[Test 8] Registering user for invalid email-format login test");
        Console.WriteLine($"[Test 8] Registration payload: {JsonSerializer.Serialize(registrationPayload)}");

        // Perform registration
        var registerResponse = await _request.PostAsync("/api/authentication/register",
            new() { DataObject = registrationPayload });

        var regStatus = registerResponse.Status;
        var regBody = await registerResponse.TextAsync();

        Console.WriteLine($"[Test 8] Registration HTTP Status: {regStatus}");
        Console.WriteLine($"[Test 8] Registration Body: {regBody}");

        Assert.That(regStatus, Is.InRange(200, 299),
            $"User registration failed unexpectedly before invalid-email login test:\nHTTP {regStatus}\n{regBody}");

        // Login payload with INVALID email format
        var loginPayload = new
        {
            email = invalidEmail,
            password = validPassword
        };

        Console.WriteLine("[Test 8] Attempting login with INVALID email format");
        Console.WriteLine($"[Test 8] Login payload: {JsonSerializer.Serialize(loginPayload)}");

        // Perform login attempt
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 8] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 8] Login Body: {loginBody}");

        // Expected: 400 or 401
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for invalid email format, but got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 8] OK: Login rejected correctly with invalid email format.");
    }

    // Test 9(Login): should return error when user does NOT exist
    [Test, Order(9)]
    public async Task Login_Should_Return_Error_When_User_Does_Not_Exist()
    {
        // Generate credentials for a user that does NOT exist in the database
        var nonexistentEmail = $"nouser_{Guid.NewGuid()}@example.com";
        var randomPassword = $"P@ss{Guid.NewGuid():N}".Substring(0, 12);

        Console.WriteLine("[Test 9] Attempting login with NON-EXISTING user");
        Console.WriteLine($"[Test 9] Email: {nonexistentEmail}");
        Console.WriteLine($"[Test 9] Password: {randomPassword}");

        // Payload for login with fake user
        var loginPayload = new
        {
            email = nonexistentEmail,
            password = randomPassword
        };

        Console.WriteLine("[Test 9] Login payload: " +
            $"{JsonSerializer.Serialize(loginPayload)}");

        // Perform login request
        var loginResponse = await _request.PostAsync("/api/authentication/login",
            new() { DataObject = loginPayload });

        var loginStatus = loginResponse.Status;
        var loginBody = await loginResponse.TextAsync();

        Console.WriteLine($"[Test 9] Login HTTP Status: {loginStatus}");
        Console.WriteLine($"[Test 9] Login Body: {loginBody}");

        // Expected: invalid credentials → 400 or 401
        Assert.That(loginStatus, Is.EqualTo(400).Or.EqualTo(401),
            $"Expected error for non-existing user, but got HTTP {loginStatus}\n{loginBody}");

        Console.WriteLine("[Test 9] OK: Login correctly rejected for non-existing user.");
    }


    [OneTimeTearDown]
    public async Task Teardown()
    {
        Console.WriteLine("[Teardown] Disposing Playwright context (Login tests).");
        await _request.DisposeAsync();
        _playwright.Dispose();
    }
}
