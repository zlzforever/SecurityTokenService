FROM docker.io/zlzforever/dotnet-yarn:6.0 as build
WORKDIR /app
COPY src/SecurityTokenService .
RUN yarn install
RUN dotnet publish SecurityTokenService.csproj -c Release -o /app
RUN rm -rf /app/wwwroot/css/site.css
RUN rm -rf /app/wwwroot/js/site.js
RUN rm -rf /app/sts.json
RUN rm -rf /app/runtimes/linux-arm64
RUN rm -rf /app/runtimes/osx-x64
RUN rm -rf /app/runtimes/win-x64
RUN rm -rf /app/runtimes/win-x86
RUN rm -rf /app/appsettings.Nacos.json
RUN mv -f /app/sts_backup.json /app/sts.json
RUN ls /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
ENV LANG zh_CN.UTF-8
EXPOSE 80
EXPOSE 443
