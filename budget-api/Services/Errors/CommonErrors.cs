namespace budget_api.Services.Errors
{
    public static class CommonErrors
    {
        public static ServiceError NotFound(string objectName, object id) => new ServiceError(
            $"{objectName}.NotFound", $"Obiekt '{objectName}' o identyfikatorze '{id}' nie został znaleziony.");
        public static ServiceError Conflict(string objectName, string details) => new ServiceError(
            $"{objectName}.Conflict", details);
        public static ServiceError Unauthorized() => new ServiceError(
            "Auth.Unauthorized", "Brak autoryzacji. Musisz być zalogowany, aby wykonać tę akcję.");

        public static ServiceError Forbidden(string details = "Brak uprawnień do wykonania tej operacji.") => new ServiceError(
           "Auth.Forbidden", details);

        public static ServiceError BadRequest(string details = "Nieprawidłowe żądanie. Sprawdź poprawność wysłanych danych.") => new ServiceError(
            "Request.BadRequest", details);

        public static ServiceError InternalServerError(string details = "Wystąpił nieoczekiwany błąd serwera.") => new ServiceError(
            "Server.InternalError", details);
        public static ServiceError DataProcessingError(string details = "Wystąpił nieoczekiwany błąd podczas przetwarzania danych.") => new ServiceError(
            "Server.DataProcessingError", details);
        public static ServiceError CreateFailed(string objectName, string details = null)
        {
            string message = details ?? $"Nie udało się utworzyć obiektu '{objectName}'. Wystąpił nieoczekiwany błąd podczas zapisu.";
            return new ServiceError($"{objectName}.CreateFailed", message);
        }
        public static ServiceError UpdateFailed(string objectName, object id, string details = null)
        {
            string message = details ?? $"Nie udało się zaktualizować obiektu '{objectName}' o identyfikatorze '{id}'.";
            return new ServiceError($"{objectName}.UpdateFailed", message);
        }
        public static ServiceError FetchFailed(string objectName, string details = null)
        {
            string message = details ?? $"Wystąpił nieoczekiwany błąd podczas pobierania danych dla obiektu '{objectName}'.";
            return new ServiceError($"{objectName}.FetchFailed", message);
        }
        public static ServiceError DeleteFailed(string objectName, object id = null, string details = null)
        {
            var idPart = id != null ? $" (ID: {id})" : "";
            return new ServiceError($"{objectName}.DeleteFailed", details ?? $"Nie udało się usunąć obiektu '{objectName}'{idPart}.");
        }
    }
}
