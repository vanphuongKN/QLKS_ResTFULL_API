# QLKSan.Gateway — build từ thư mục QLKSan/: docker build -f QLKSan.Gateway/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY QLKSan.Gateway/QLKSan.Gateway.csproj QLKSan.Gateway/
RUN dotnet restore QLKSan.Gateway/QLKSan.Gateway.csproj
COPY QLKSan.Gateway/ QLKSan.Gateway/
RUN dotnet publish QLKSan.Gateway/QLKSan.Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "QLKSan.Gateway.dll"]
