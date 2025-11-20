using budget_api.Services;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text.Json;

namespace budget_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetabaseController : BudgetApiBaseController
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;

        public MetabaseController(IConfiguration configuration, IAuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        [HttpGet("{dashboardId}", Name = "DashboardUrl")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DashboardUrl(int dashboardId)
        {
            var result = await _authService.MetabaseUrl(dashboardId);
            return HandleServiceResult(result);
        }
    }
}
