FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /app
COPY src/SecurityTokenService .
RUN dotnet publish SecurityTokenService.csproj -c Release -o out
RUN rm -rf /app/out/wwwroot/css/site.css
RUN rm -rf /app/out/wwwroot/js/site.js
RUN rm -rf /app/out/sts.json
RUN rm -rf /app/out/runtimes/linux-arm64
RUN rm -rf /app/out/runtimes/osx-x64
RUN rm -rf /app/out/runtimes/win-x64
RUN rm -rf /app/out/runtimes/win-x86
RUN rm -rf /app/out/appsettings.Nacos.json
RUN mv -f /app/out/sts_backup.json /app/sts.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
ENV LANG zh_CN.UTF-8
EXPOSE 8080
