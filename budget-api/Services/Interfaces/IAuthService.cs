using budget_api.Models.Dto;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult> RegisterAsync(RegisterDto registerDto, bool isAdmin = false);
        Task<ServiceResult<LoginResponse>> Login(LoginDto loginDto);
        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<ServiceResult<LoginResponse>> HandleExternalLoginAsync();
        Task<ServiceResult<string>> MetabaseUrl(int dashboard);
    }
}