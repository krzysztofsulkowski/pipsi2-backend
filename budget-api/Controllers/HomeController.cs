using budget_api.Services.Results;
using Microsoft.AspNetCore.Mvc;
using budget_api.Services;
using budget_api.Models.ViewModel;

namespace budget_api.Controllers
{
    public class HomeController : BudgetApiBaseController
    {
        private readonly ContactService _contactService;

        public HomeController(ILogger<HomeController> logger, ContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpPost]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> SubmitMessage([FromBody] ContactMessageViewModel msg)
        {
            var result = await _contactService.SubmitMessage(msg);
            return HandleServiceResult(result);
        }
    }
}