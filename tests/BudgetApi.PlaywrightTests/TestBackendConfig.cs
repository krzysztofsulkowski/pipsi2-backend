namespace BudgetApi.PlaywrightTests;

public static class TestBackendConfig
{
    public const string Port = "62411";

    public static readonly string HttpsUrl = $"https://localhost:{Port}";
    public static readonly string HttpUrl = $"http://localhost:{Port}";

    public const string ResetPasswordFullLink = ""; // Paste reset password link here for reset password test
}
