namespace budget_api.Services.Errors
{
    public static class AuthErrors
    {
        public static ServiceError EmailRequired() => new ServiceError(
            "Auth.EmailRequired", "Adres e-mail jest wymagany.");

        public static ServiceError UserAlreadyExists() => new ServiceError(
            "Auth.UserExists", "Użytkownik o podanym adresie e-mail już istnieje.");

        public static ServiceError UsernameTaken() => new ServiceError(
            "Auth.UsernameTaken", "Ta nazwa użytkownika jest już zajęta.");

        public static ServiceError UserCreationFailed(string details = null) => new ServiceError(
            "Auth.CreationFailed", details ?? "Nie udało się utworzyć konta użytkownika.");

        public static ServiceError InvalidCredentials() => new ServiceError(
            "Auth.InvalidCredentials", "Nieprawidłowy adres e-mail lub hasło.");

        public static ServiceError InvalidConfiguration(string msg) => new ServiceError(
            "Auth.ConfigError", msg);

        public static ServiceError ExternalAuthFailed(string provider) => new ServiceError(
            "Auth.ExternalFailed", $"Nie udało się pobrać danych od dostawcy {provider}.");

        public static ServiceError AccountLinkingFailed() => new ServiceError(
            "Auth.LinkFailed", "Nie udało się powiązać konta zewnętrznego.");
    }
}
