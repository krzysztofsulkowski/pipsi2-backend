using budget_api.Models;
using budget_api.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace budget_api.Seeders
{
    public class CategorySeeder
    {
        private readonly BudgetApiDbContext _dbContext;

        public CategorySeeder(BudgetApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedCategoriesAsync()
        {
            if (await _dbContext.Categories.AnyAsync())
            {
                return;
            }

            var categories = new List<Category>
            {
                new Category { Name = "Jedzenie" },
                new Category { Name = "Mieszkanie / Rachunki" },
                new Category { Name = "Transport" },
                new Category { Name = "Telekomunikacja (Telefon/Internet)" },
                new Category { Name = "Opieka zdrowotna" },
                new Category { Name = "Ubranie" },
                new Category { Name = "Higiena" },
                new Category { Name = "Dzieci" },
                new Category { Name = "Rozrywka" },
                new Category { Name = "Edukacja" },
                new Category { Name = "Spłata długów" },
                new Category { Name = "Inne" }
            };

            await _dbContext.Categories.AddRangeAsync(categories);
            await _dbContext.SaveChangesAsync();
        }
    }
}


