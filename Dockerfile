FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
RUN sed -i s@/archive.ubuntu.com/@/mirrors.aliyun.com/@g /etc/apt/sources.list
RUN apt-get update && apt-get install gnupg2 curl -y
RUN curl -sS https://dl.yarnpkg.com/debian/pubkey.gpg | apt-key add -
RUN echo "deb https://dl.yarnpkg.com/debian/ stable main" | tee /etc/apt/sources.list.d/yarn.list
RUN apt-get update && apt-get install yarn -y
RUN yarn config set registry https://registry.npm.taobao.org

FROM base AS prebuild
WORKDIR /app
COPY src/SecurityTokenService/SecurityTokenService.csproj ./SecurityTokenService.csproj
RUN ls
RUN dotnet restore SecurityTokenService.csproj

FROM prebuild as build
WORKDIR /app
COPY src/SecurityTokenService ./
RUN dotnet build SecurityTokenService.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish SecurityTokenService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN rm /app/wwwroot/css/site.css
RUN rm /app/wwwroot/js/site.js
RUN rm /app/sts.json
RUN rm -rf /app/build 
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
EXPOSE 80
EXPOSE 443
