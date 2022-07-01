FROM docker.io/zlzforever/dotnet-yarn:6.0 as build
WORKDIR /app
COPY src/SecurityTokenService .
RUN yarn install
RUN dotnet build SecurityTokenService.csproj -c Release -o /app/build
RUN rm -rf /app/wwwroot/css/site.css
RUN rm -rf /app/wwwroot/js/site.js
RUN rm -rf /app/sts.json
RUN rm -rf /app/runtimes/linux-arm64
RUN rm -rf /app/runtimes/osx-x64
RUN rm -rf /app/runtimes/win-x64
RUN rm -rf /app/runtimes/win-x86

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
ENV LANG zh_CN.UTF-8
EXPOSE 80
EXPOSE 443
