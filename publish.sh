cd src/SecurityTokenService && yarn install
dotnet publish --os linux -c Release -o security_token_service_out
rm -rf security_token_service_out/wwwroot/css/site.css
rm -rf security_token_service_out/wwwroot/js/site.js
rm -rf security_token_service_out/sts.json
(cd security_token_service_out/wwwroot/lib/bootstrap && rm -rf `ls | grep -v "dist"`) && return
(cd security_token_service_out/wwwroot/lib/jquery && rm -rf `ls | grep -v "dist"`) && return
(cd security_token_service_out/wwwroot/lib/jquery-validation && rm -rf `ls | grep -v "dist"`) && return
(cd security_token_service_out/wwwroot/lib/jquery-validation-unobtrusive && rm -rf `ls | grep -v "dist"`) && return
docker build -t zlzforever/security-token-service .
docker push zlzforever/security-token-service