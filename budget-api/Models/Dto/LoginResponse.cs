namespace budget_api.Models.Dto
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public LoginResponse(string token)
        {
            Token = token;
        }
    }
}
