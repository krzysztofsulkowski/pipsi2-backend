using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

namespace budget_api.Models
{
    public class BudgetApiDbContext : IdentityDbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BudgetApiDbContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public BudgetApiDbContext() { }
        public BudgetApiDbContext(DbContextOptions<BudgetApiDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public BudgetApiDbContext(DbContextOptions<BudgetApiDbContext> options) : base(options)
        {
        }


        public DbSet<Budget> Budgets { get; set; }
        public DbSet<UserBudget> UserBudgets { get; set; }
        public DbSet<BudgetInvitation> BudgetInvitations { get; set; }
        public DbSet<HistoryLog> HistoryLogs { get; set; }
        public DbSet<BudgetTransaction> BudgetTransactions { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDto>().HasNoKey().ToView("UserWithRoles");

            modelBuilder.Entity<UserBudget>()
                .HasKey(ub => new { ub.UserId, ub.BudgetId });

            modelBuilder.Entity<HistoryLog>()
                .Property(h => h.CreationDate)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<BudgetTransaction>()
                .Property(t => t.Date)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<BudgetTransaction>()
                .Property(t => t.CreatedAt)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Jedzenie" },
                new Category { Id = 2, Name = "Mieszkanie / Rachunki" },
                new Category { Id = 3, Name = "Transport" },
                new Category { Id = 4, Name = "Telekomunikacja" },
                new Category { Id = 5, Name = "Opieka zdrowotna" },
                new Category { Id = 6, Name = "Ubranie" },
                new Category { Id = 7, Name = "Higiena" },
                new Category { Id = 8, Name = "Dzieci" },
                new Category { Id = 9, Name = "Rozrywka" },
                new Category { Id = 10, Name = "Edukacja" },
                new Category { Id = 11, Name = "Spłata długów" },
                new Category { Id = 12, Name = "Inne" }
            );
        }

        private async Task<IdentityUser?> GetIdentityUser()
        {
            IdentityUser? identityUser = null;
            var userIdentity = _httpContextAccessor?.HttpContext?.User?.Identity;
            if (userIdentity != null)
            {
                var claimsUserIdentity = (ClaimsIdentity)userIdentity;
                var userNameIdentifier = claimsUserIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (userNameIdentifier != null)
                {
                    identityUser = await Users.FirstAsync(u => u.Id == userNameIdentifier.Value);
                }
            }

            return identityUser;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.DetectChanges();

            var entries = ChangeTracker
                .Entries()
                .Where(e =>
                        (e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted) &&
                        e.Entity.GetType() != typeof(HistoryLog)
                        );

            IdentityUser? identityUser = null;
            bool isAnyEntityChanged = entries.Any();
            if (isAnyEntityChanged)
                identityUser = await GetIdentityUser();

            var historyLogs = new List<HistoryLog>();
            var addedEntities = new Dictionary<EntityEntry, HistoryLog>();

            foreach (var entry in entries)
            {
                if (entry.Metadata.FindPrimaryKey().Properties.Count > 1)
                {
                    continue;
                }


                string typeName = entry.Entity.GetType() == typeof(IdentityUser)
                    ? "User"
                    : entry.Entity.GetType().Name;

                bool skipHistoryLog = false;
                string before = string.Empty;
                string after = string.Empty;

                switch (entry.State)
                {
                    case EntityState.Added:
                        var (afterJson, isAfterEmpty) = SerializeEntityChanges(entry, EntityState.Added);
                        if (isAfterEmpty)
                        {
                            skipHistoryLog = true;
                            break;
                        }
                        after = afterJson;
                        break;

                    case EntityState.Modified:
                        var (beforeModifiedJson, isBeforeModifiedEmpty) = SerializeEntityChanges(entry, EntityState.Modified, "Before");
                        var (afterModifiedJson, isAfterModifiedEmpty) = SerializeEntityChanges(entry, EntityState.Modified, "After");

                        if (isBeforeModifiedEmpty && isAfterModifiedEmpty)
                        {
                            skipHistoryLog = true;
                            break;
                        }
                        before = beforeModifiedJson;
                        after = afterModifiedJson;
                        break;

                    case EntityState.Deleted:
                        var (beforeJson, isBeforeEmpty) = SerializeEntityChanges(entry, EntityState.Added);
                        if (isBeforeEmpty)
                        {
                            skipHistoryLog = true;
                            break;
                        }
                        before = beforeJson;
                        break;
                }

                if (skipHistoryLog)
                    continue;

                var historyLog = new HistoryLog
                {
                    CreationDate = DateTime.UtcNow,
                    ObjectId = GetPrimaryKeyValue(entry).ToString() ?? "UnknownId",
                    ObjectType = typeName,
                    UserEmail = identityUser?.Email,
                    UserId = identityUser?.Id,
                    EventType = $"{entry.State} {typeName}",
                    Before = before,
                    After = after,
                };

                if (entry.State == EntityState.Added)
                {
                    addedEntities.Add(entry, historyLog);
                }

                historyLogs.Add(historyLog);
            }

            if (historyLogs.Any())
            {
                await HistoryLogs.AddRangeAsync(historyLogs, cancellationToken);
            }
            var result = await base.SaveChangesAsync(cancellationToken);

            if (addedEntities.Any())
            {
                foreach (var addedEntity in addedEntities)
                {
                    var entry = addedEntity.Key;
                    var historyLog = addedEntity.Value;
                    var realId = GetPrimaryKeyValue(entry)?.ToString() ?? "UnknownId";

                    if (historyLog.ObjectId != realId)
                    {
                        historyLog.ObjectId = realId;
                    }
                }
                if (historyLogs.Any())
                {
                    await base.SaveChangesAsync(cancellationToken);
                }
            }
            return result;
        }

        private (string json, bool isEmpty) SerializeEntityChanges(EntityEntry entry, EntityState state, string version = "")
        {
            try
            {
                var changes = new Dictionary<string, object>();

                string[] userFieldsToExclude = { "ConcurrencyStamp", "AccessFailedCount", "PasswordHash", "SecurityStamp" };

                bool isIdentityUser = entry.Entity.GetType().Equals(typeof(IdentityUser));

                if (state == EntityState.Added)
                {
                    foreach (var property in entry.CurrentValues.Properties)
                    {
                        if (property.Name != "Id" && property.Name != "CreatedAt" &&
                            !(isIdentityUser && userFieldsToExclude.Contains(property.Name)))
                        {
                            changes.Add(property.Name, entry.CurrentValues[property] ?? DBNull.Value);
                        }
                    }
                }
                else if (state == EntityState.Deleted)
                {
                    foreach (var property in entry.OriginalValues.Properties)
                    {
                        if (property.Name != "Id" && property.Name != "CreatedAt" &&
                            !(isIdentityUser && userFieldsToExclude.Contains(property.Name)))
                        {
                            changes.Add(property.Name, entry.OriginalValues[property] ?? DBNull.Value);
                        }
                    }
                }
                else if (state == EntityState.Modified)
                {
                    foreach (var property in entry.OriginalValues.Properties)
                    {
                        var originalValue = entry.OriginalValues[property];
                        var currentValue = entry.CurrentValues[property];

                        if (!object.Equals(originalValue, currentValue) &&
                            !(isIdentityUser && userFieldsToExclude.Contains(property.Name)))
                        {
                            if (version == "Before")
                            {
                                changes.Add(property.Name, originalValue ?? DBNull.Value);
                            }
                            else
                            {
                                changes.Add(property.Name, currentValue ?? DBNull.Value);
                            }
                        }
                    }
                }
                return (JsonSerializer.Serialize(changes), changes.Count == 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing changes: {ex}");

                return (string.Empty, true);
            }
        }


        private object GetPrimaryKeyValue(EntityEntry entry)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            var keyName = primaryKey.Properties.Single().Name;
            return entry.Property(keyName).CurrentValue;
        }
    }
}
