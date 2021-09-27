docker stop sts && docker rm sts
docker run --name sts -d -p 3000:80 zlzforever/security-token-service