using budget_api.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using budget_api.Services.Interfaces;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [Route("api/historyLog")]
    public class HistoryLogController : BudgetApiBaseController
    {
        private readonly IHistoryLogService _historyLogService;

        public HistoryLogController(IHistoryLogService historyLogService)
        {
            _historyLogService = historyLogService;
        }

        [HttpPost("get-history-logs")]
        public async Task<IActionResult> GetHistoryLogs([FromBody] DataTableRequest request)
        {
            var serviceResponse = await _historyLogService.GetHistoryLogs(request);
            return HandleStatusCodeServiceResult(serviceResponse);
        }
    }
}
