using budget_api.Models.Dto;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace budget_api.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<ServiceResult<DataTableResponse<UserDto>>> GetAllUsers(DataTableRequest request);
        Task<ServiceResult> LockUser(string userId);
        Task<ServiceResult> UnlockUser(string userId);
        Task<ServiceResult<UserDto>> CreateUser(UserDto user, ClaimsPrincipal currentUser);
        Task<ServiceResult> UpdateUser(UserDto model);
        Task<ServiceResult<UserDto>> GetUserById(string userId);
        Task<ServiceResult<List<IdentityRole>>> GetAllRolesAsync();
    }
}