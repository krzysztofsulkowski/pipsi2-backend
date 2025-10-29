namespace budget_api.Models
{
    public static class ConnectionStringConverter
    {
        public static string Convert(string databaseUrl)
        {
            if (string.IsNullOrEmpty(databaseUrl))
            {
                throw new ArgumentNullException(nameof(databaseUrl), "Database URL cannot be null or empty.");
            }

            if (!databaseUrl.StartsWith("postgresql://"))
            {
                return databaseUrl;
            }

            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            var user = userInfo[0];
            var password = userInfo[1];
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
        }
    }
}
