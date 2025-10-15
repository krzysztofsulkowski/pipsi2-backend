namespace budget_api.Services.Errors
{
    public sealed record ServiceError(string Code, string Description, List<string> Errors = null)
    {
        public static readonly ServiceError None = new ServiceError(string.Empty, string.Empty);
        public static readonly ServiceError Generic = new ServiceError("Generic", "Error occured");

        public List<string> Errors = Errors ?? new List<string>();

        public string GetErrorMessage()
        {
            return Description + Environment.NewLine + string.Join(Environment.NewLine, Errors);
        }
    }
}
