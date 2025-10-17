using budget_api.Models.Dto;
using budget_api.Models;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<ServiceResult<DataTableResponse<UserDto>>> GetAllUsers(DataTableRequest request);
        Task<ServiceResult> LockUser(string userId);
        Task<ServiceResult> UnlockUser(string userId);
        Task<ServiceResult<UserDto>> CreateUser(UserDto user);
        Task<ServiceResult> UpdateUser(UserDto model);
        Task<ServiceResult<UserDto>> GetUserById(string userId);
    }
}