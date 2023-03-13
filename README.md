dotnet ef migrations add SecurityTokenServiceInit --context PostgreSqlSecurityTokenServiceDbContext  --output-dir Data/PostgreSql/Migrations/SecurityTokenService
dotnet ef migrations add PersistedGrantInit --context PostgreSqlPersistedGrantDbContext --output-dir Data/PostgreSql/Migrations/PersistedGrant

dotnet ef migrations add SecurityTokenServiceInit --context MySqlSecurityTokenServiceDbContext  --output-dir Data/MySql/Migrations/SecurityTokenService
dotnet ef migrations add PersistedGrantInit --context MySqlPersistedGrantDbContext --output-dir Data/MySql/Migrations/PersistedGrant

### 变更

20230309

1. 通过原密码修改新密码
2. 移除记录密码强弱信息， 通过前端、后端密码策略配置来限制登录
3. 重构代码
4. 程序启动后判断是否 HTML 含有 site.js， 若有则替换为 site.min.js
5. IdentityServer TablePrefix 独立配置