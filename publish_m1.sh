image=zlzforever/security-token-service
docker buildx build --platform linux/amd64 --no-cache -t $image .
docker push $image