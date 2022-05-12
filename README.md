dotnet ef migrations add SecurityTokenServiceInit --context PostgreSqlSecurityTokenServiceDbContext  --output-dir Data/PostgreSql/Migrations
dotnet ef migrations add PersistedGrantInit --context PostgreSqlPersistedGrantDbContext --output-dir Data/PostgreSql/Migrations


dotnet ef migrations add SecurityTokenServiceInit --context MySqlSecurityTokenServiceDbContext  --output-dir Data/MySql/Migrations
dotnet ef migrations add PersistedGrantInit --context MySqlPersistedGrantDbContext --output-dir Data/MySql/Migrations