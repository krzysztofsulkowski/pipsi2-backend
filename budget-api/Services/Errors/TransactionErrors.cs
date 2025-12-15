namespace budget_api.Services.Errors
{
    public static class TransactionErrors
    {
        private const string ObjectName = "Transaction";

        public static ServiceError NoAccess() => new ServiceError(
            $"{ObjectName}.NoAccess", "Brak uprawnień do tego budżetu.");

        public static ServiceError InsufficientFunds() => new ServiceError(
            $"{ObjectName}.NoFunds", "Brak wystarczających środków w budżecie. Operacja odrzucona.");

        public static ServiceError IncomeNotFound(int id) => new ServiceError(
            $"{ObjectName}.IncomeNotFound", $"Nie znaleziono przychodu o identyfikatorze '{id}'.");

        public static ServiceError ExpenseNotFound(int id) => new ServiceError(
            $"{ObjectName}.ExpenseNotFound", $"Nie znaleziono wydatku o identyfikatorze '{id}'.");

        public static ServiceError BalanceChangeDenied() => new ServiceError(
            $"{ObjectName}.NegativeBalance", "Zmiana kwoty spowodowałaby ujemne saldo budżetu. Operacja odrzucona.");
    }
}
