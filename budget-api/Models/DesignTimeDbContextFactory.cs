using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace budget_api.Models
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BudgetApiDbContext>
    {
        public BudgetApiDbContext CreateDbContext(string[] args)
        {
            var envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables() // To wczyta zmienne z pliku .env
                .Build();

            var builder = new DbContextOptionsBuilder<BudgetApiDbContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetValue<string>("ConnectionStrings:DefaultConnection_LOCAL");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Could not find a connection string. Ensure it is set in appsettings.json, appsettings.Development.json, or an .env file.");
            }

            builder.UseNpgsql(connectionString);

            return new BudgetApiDbContext(builder.Options);
        }
    }
}