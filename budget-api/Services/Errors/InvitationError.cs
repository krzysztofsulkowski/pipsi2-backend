using budget_api.Services.Errors;

namespace budget_api.Services.Errors
{
    public class InvitationError
    {
        private static string objectName = "Invitation";

        public static ServiceError UserRegistrationRequiredError() => new ServiceError(
            $"{objectName}.NoUnauthorized", $"Aby zaakceptować zaproszenie, najpierw załóż konto");
    }
}