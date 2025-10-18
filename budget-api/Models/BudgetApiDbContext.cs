using budget_api.Models.Dto;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace budget_api.Models
{
    public class BudgetApiDbContext : IdentityDbContext
    {
        public BudgetApiDbContext(DbContextOptions<BudgetApiDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDto>().HasNoKey().ToView("UserWithRoles");
        }
    }
}
