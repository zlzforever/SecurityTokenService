dotnet ef migrations add SecurityTokenServiceInit --context PostgreSqlSecurityTokenServiceDbContext  --output-dir Data/PostgreSql/Migrations/SecurityTokenService
dotnet ef migrations add PersistedGrantInit --context PostgreSqlPersistedGrantDbContext --output-dir Data/PostgreSql/Migrations/PersistedGrant

dotnet ef migrations add SecurityTokenServiceInit --context MySqlSecurityTokenServiceDbContext  --output-dir Data/MySql/Migrations/SecurityTokenService
dotnet ef migrations add PersistedGrantInit --context MySqlPersistedGrantDbContext --output-dir Data/MySql/Migrations/PersistedGrant