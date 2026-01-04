using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace BudgetApi.PlaywrightTests;

public class DashboardApiTests : BudgetApiTestBase
{
    // Submit message should return 200 when anonymous user sends valid payload
    [Test]
    public async Task Dashboard_SubmitMessage_Should_Return_200_When_Anonymous_And_Valid()
    {
        var response = await _request.PostAsync("/api/dashboard/submit-message", new APIRequestContextOptions
        {
            DataObject = new
            {
                name = "Test User",
                email = "test@example.com",
                message = "Automated dashboard message"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Dashboard Test 1] HTTP Status: " + status);
        Console.WriteLine("[Dashboard Test 1] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 200,
            $"Expected 200, got {status}\n{body}"
        );
    }

    // Submit message should return 400 when anonymous user sends invalid payload
    [Test]
    public async Task Dashboard_SubmitMessage_Should_Return_400_When_Anonymous_And_Invalid()
    {
        var response = await _request.PostAsync("/api/dashboard/submit-message", new APIRequestContextOptions
        {
            DataObject = new { }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Dashboard Test 2] HTTP Status: " + status);
        Console.WriteLine("[Dashboard Test 2] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 400,
            $"Expected 400, got {status}\n{body}"
        );
    }

    // Submit message should return 400 when email is missing
    [Test]
    public async Task Dashboard_SubmitMessage_Should_Return_400_When_Email_Is_Missing()
    {
        var response = await _request.PostAsync("/api/dashboard/submit-message", new APIRequestContextOptions
        {
            DataObject = new
            {
                name = "Test User",
                message = "Automated dashboard message"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Dashboard Test 3] HTTP Status: " + status);
        Console.WriteLine("[Dashboard Test 3] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 400,
            $"Expected 400, got {status}\n{body}"
        );
    }

    // Submit message should return 400 when message is missing
    [Test]
    public async Task Dashboard_SubmitMessage_Should_Return_400_When_Message_Is_Missing()
    {
        var response = await _request.PostAsync("/api/dashboard/submit-message", new APIRequestContextOptions
        {
            DataObject = new
            {
                name = "Test User",
                email = "test@example.com"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Dashboard Test 4] HTTP Status: " + status);
        Console.WriteLine("[Dashboard Test 4] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 400,
            $"Expected 400, got {status}\n{body}"
        );
    }

    // Submit message should return 400 when email format is invalid
    [Test]
    public async Task Dashboard_SubmitMessage_Should_Return_400_When_Email_Format_Is_Invalid()
    {
        var response = await _request.PostAsync("/api/dashboard/submit-message", new APIRequestContextOptions
        {
            DataObject = new
            {
                name = "Test User",
                email = "invalid-email-format",
                message = "Automated dashboard message"
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Dashboard Test 5] HTTP Status: " + status);
        Console.WriteLine("[Dashboard Test 5] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 400,
            $"Expected 400, got {status}\n{body}"
        );
    }

    // Submit message should reject too long message (400 or 413)
    [Test]
    public async Task Dashboard_SubmitMessage_Should_Reject_When_Message_Is_Too_Long()
    {
        var longMessage = new string('a', 10000);

        var response = await _request.PostAsync("/api/dashboard/submit-message", new APIRequestContextOptions
        {
            DataObject = new
            {
                name = "Test User",
                email = "test@example.com",
                message = longMessage
            }
        });

        var status = response.Status;
        var body = await response.TextAsync();

        Console.WriteLine("[Dashboard Test 6] HTTP Status: " + status);
        Console.WriteLine("[Dashboard Test 6] Response Body:");
        Console.WriteLine(body);

        Assert.That(
            status == 400 || status == 413,
            $"Expected 400 or 413, got {status}\n{body}"
        );
    }


}
