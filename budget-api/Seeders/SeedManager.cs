namespace budget_api.Seeders
{
    public class SeedManager
    {
        private readonly RoleSeeder _roleSeeder;
        private readonly CategorySeeder _categorySeeder;

        public SeedManager(RoleSeeder roleSeeder, CategorySeeder categorySeeder)
        {
            _roleSeeder = roleSeeder;
            _categorySeeder = categorySeeder;
        }

        public async Task Seed()
        {
            await _roleSeeder.SeedRolesAsync();
            await _categorySeeder.SeedCategoriesAsync();
        }
    }
}
