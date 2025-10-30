using budget_api.Services.Errors;

namespace budget_api.Services.Errors
{
    public class InvitationError
    {
        private static string objectName = "Invitation";

        public static ServiceError UserRegistrationRequiredError() => new ServiceError(
            $"{objectName}.NoUnauthorized", $"Aby zaakceptować zaproszenie, najpierw załóż konto");
        public static ServiceError ExpiredError() => new ServiceError(
           $"{objectName}.Expired", "Twoje zaproszenie wygasło.");
        public static ServiceError InvalidOrUsedError() => new ServiceError(
           $"{objectName}.InvalidOrUsed", "Zaproszenie jest nieprawidłowe lub zostało już wykorzystane.");
        public static ServiceError AlreadyMemberError() => new ServiceError(
           $"{objectName}.AlreadyMember", "Ten użytkownik jest już członkiem tego budżetu.");
        public static ServiceError PermissionDeniedError() => new ServiceError(
            $"{objectName}.PermissionDenied", "Brak uprawnień do zapraszania użytkowników do tego budżetu.");
    }
}