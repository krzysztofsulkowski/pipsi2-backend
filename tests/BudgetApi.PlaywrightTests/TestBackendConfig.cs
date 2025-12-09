namespace BudgetApi.PlaywrightTests;

public static class TestBackendConfig
{
    public const string Port = "64052";

    public static readonly string HttpsUrl = $"https://localhost:{Port}";
    public static readonly string HttpUrl = $"http://localhost:{Port}";
}
