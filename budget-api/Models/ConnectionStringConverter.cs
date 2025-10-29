namespace budget_api.Models
{
    public static class ConnectionStringConverter
    {
        public static string Convert(string databaseUrl)
        {
            if (string.IsNullOrEmpty(databaseUrl))
            {
                return string.Empty;
            }

            if (!databaseUrl.StartsWith("postgresql://"))
            {
                return databaseUrl;
            }
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');

                var user = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
                }
            catch (Exception e)
            {
                Console.WriteLine($"Error converting database URL: {e.Message}");
                throw; 
            }
        }
    }
}
