namespace BudgetApi.PlaywrightTests;

public static class TestBackendConfig
{
    public const string Port = "58698";

    public static readonly string HttpsUrl = $"https://localhost:{Port}";
    public static readonly string HttpUrl = $"http://localhost:{Port}";

    public const string ResetPasswordFullLink = "http://localhost:3000/reset-password?token=CfDJ8GaTPM%2FmLT5Ho2%2BFHAe0ro4vh9hHmeiy2MY7Og5W54vIpF2zzhaukHxGg3Y%2FSaMtPr86L1aabmhnL4gT2xMdkBUlJdHvJDDQhp%2FSP6WaVX6iFlWJxiM8CU8WpOdBZ3RZ4aSYqbvZeDf21kNU2cSX9DcgQJKnL2igMd4QhFYPamqX8M6phoZAuMw2vcSS1qdvUMios44txX5LIXVoa0Ldvn7tzvxEdEg7W3a5PljHIHT1&email=balancrtestuser%40gmail.com"; // Paste reset password link here for reset password test
}
