namespace BudgetApi.PlaywrightTests;

public static class TestBackendConfig
{
    public const string Port = "62411";

    public static readonly string HttpsUrl = $"https://localhost:{Port}";
    public static readonly string HttpUrl = $"http://localhost:{Port}";

    public const string ResetPasswordFullLink = "http://localhost:3000/reset-password?token=CfDJ8GaTPM%2FmLT5Ho2%2BFHAe0ro7TN5Ohg1rB4kR0hhajuNj8HGSH5LT1QhxM58vD77WkYrTE1tY%2F4csZsb95IlF1q6jtiqzNR5pzy8AmP3FNWDbShqbKs0lvF%2BQcny%2F%2FVh%2Fvd1xnFjezMkvNm2dln2nd29QMb0895MTTKTn2fbFqkrF8OGSi7uQLC9hnl6biuvW87SWXjKthaGBDTdxegVXH5eZdsF6DAx9jYeSW%2BpCcQkye&email=balancrtestuser%40gmail.com"; // Paste reset password link here for reset password test
}
