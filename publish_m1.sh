sts_image=zlzforever/security-token-service
docker buildx build --platform linux/amd64 -t $sts_image .
docker push $sts_image