FROM mcr.microsoft.com/dotnet/sdk:6.0 as base
RUN sed -i s@/archive.ubuntu.com/@/mirrors.aliyun.com/@g /etc/apt/sources.list
RUN apt-get update && apt-get install gnupg2 curl -y
RUN curl -sS https://dl.yarnpkg.com/debian/pubkey.gpg | apt-key add -
RUN echo "deb https://dl.yarnpkg.com/debian/ stable main" | tee /etc/apt/sources.list.d/yarn.list
RUN apt-get update && apt-get install yarn -y
RUN yarn config set registry https://registry.npm.taobao.org

FROM base as build
WORKDIR /app
COPY src/SecurityTokenService ./
RUN yarn install
RUN dotnet build SecurityTokenService.csproj -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/build .
RUN rm -rf /app/wwwroot/css/site.css
RUN rm -rf /app/wwwroot/js/site.js
RUN rm -rf /app/sts.json
ENTRYPOINT ["dotnet", "SecurityTokenService.dll"]
EXPOSE 80
EXPOSE 443
