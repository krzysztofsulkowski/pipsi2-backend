using budget_api.Models;
using budget_api.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : BudgetApiBaseController
    {
        private readonly BudgetApiDbContext _context;

        public ReportsController(BudgetApiDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public IActionResult GetStats([FromQuery] int year, [FromQuery] int month)
        {
            var mockData = new List<ChartDataDto>();
            var rand = new Random();

            string[] categories = { "Jedzenie", "Transport", "Biuro", "Marketing", "Czynsz", "Szkolenia" };
            string[] users = { "Ania", "Tomek", "Marek", "Zosia" };

            int targetYear = year > 0 ? year : DateTime.Now.Year;

            int startM = month > 0 ? month : 1;
            int endM = month > 0 ? month : 12;

            for (int i = 0; i < 50; i++)
            {
                int m = rand.Next(startM, endM + 1);

                int d = rand.Next(1, 28);

                var record = new ChartDataDto
                {
                    Date = new DateTime(targetYear, m, d),

                    Amount = rand.Next(10, 500),

                    Category = categories[rand.Next(categories.Length)],

                    UserName = users[rand.Next(users.Length)]
                };

                mockData.Add(record);
            }

            return Ok(mockData);
        }
    }
}


//var data = await _context.Expenses
//    .Where(e => e.Date.Year == year && e.Date.Month == month)
//    .Select(e => new ChartDataDto
//    {
//        Amount = e.Amount,
//        Category = e.CategoryName,
//        Date = ,
//        UserName = ,

//    })
//    .ToListAsync();
//return Ok(data);