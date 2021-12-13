image=zlzforever/security-token-service
docker buildx build --platform linux/amd64 -t $image .
docker push $image