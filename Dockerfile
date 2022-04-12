FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
WORKDIR /app
COPY src/SecurityTokenService ./
RUN dotnet build SecurityTokenService.csproj -c Release -o /app/build


FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/build .
RUN rm /app/wwwroot/css/site.css
RUN rm /app/wwwroot/js/site.js
RUN rm /app/sts.json
RUN rm -rf /app/build 
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
EXPOSE 80
EXPOSE 443
