FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY ./security_token_service_out ./
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
ENV LANG zh_CN.UTF-8
EXPOSE 80
EXPOSE 443
