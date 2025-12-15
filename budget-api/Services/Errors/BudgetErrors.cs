namespace budget_api.Services.Errors
{
    public static class BudgetErrors
    {
        private const string ObjectName = "Budget";

        public static ServiceError OnlyOwnerCanArchive() => new ServiceError(
            $"{ObjectName}.RoleRestriction", "Tylko właściciel może archiwizować budżet.");

        public static ServiceError OnlyOwnerCanRestore() => new ServiceError(
            $"{ObjectName}.RoleRestriction", "Tylko właściciel może przywrócić budżet z archiwum.");

        public static ServiceError OnlyOwnerCanEdit() => new ServiceError(
            $"{ObjectName}.RoleRestriction", "Tylko właściciel może edytować ustawienia budżetu.");

        public static ServiceError OwnerCannotLeave() => new ServiceError(
            $"{ObjectName}.LogicError", "Właściciel nie może opuścić budżetu. Usuń budżet lub przekaż uprawnienia.");

        public static ServiceError CannotRemoveOwner() => new ServiceError(
             $"{ObjectName}.LogicError", "Nie można usunąć właściciela budżetu.");

        public static ServiceError AlreadyArchived() => new ServiceError(
            $"{ObjectName}.StateError", "Ten budżet jest już zarchiwizowany.");

        public static ServiceError NotArchived() => new ServiceError(
            $"{ObjectName}.StateError", "Ten budżet nie znajduje się w archiwum.");

        public static ServiceError MemberNotFound() => new ServiceError(
            $"{ObjectName}.MemberNotFound", "Użytkownik nie jest członkiem tego budżetu.");
    }
}
