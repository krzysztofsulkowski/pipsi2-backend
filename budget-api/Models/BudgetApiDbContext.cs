using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace budget_api.Models
{
    public class BudgetApiDbContext : IdentityDbContext
    {
        public BudgetApiDbContext(DbContextOptions<BudgetApiDbContext> options) : base(options) { }
    }
}
