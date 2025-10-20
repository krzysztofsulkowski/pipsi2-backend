using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace budget_api.Models
{
    public class BudgetApiDbContext : IdentityDbContext
    {
        public BudgetApiDbContext(DbContextOptions<BudgetApiDbContext> options) : base(options) { }

        public DbSet<Budget> Budgets { get; set; }
        public DbSet<UserBudget> UserBudgets { get; set; }
        public DbSet<BudgetInvitation> BudgetInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDto>().HasNoKey().ToView("UserWithRoles");

            modelBuilder.Entity<UserBudget>()
                .HasKey(ub => new { ub.UserId, ub.BudgetId });
        }
    }
}