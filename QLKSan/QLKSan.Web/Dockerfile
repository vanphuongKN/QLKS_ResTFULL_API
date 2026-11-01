# QLKSan.Web — build từ thư mục QLKSan/ (cần cả QLKSan.Shared): docker build -f QLKSan.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY QLKSan.Shared/QLKSan.Shared.csproj QLKSan.Shared/
COPY QLKSan.Web/QLKSan.Web.csproj QLKSan.Web/
RUN dotnet restore QLKSan.Web/QLKSan.Web.csproj
COPY QLKSan.Shared/ QLKSan.Shared/
COPY QLKSan.Web/ QLKSan.Web/
RUN dotnet publish QLKSan.Web/QLKSan.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh
WORKDIR /app
COPY --from=build /app .
# Khoá mã hoá cookie đăng nhập (gắn volume) để khởi động lại container không bị đăng xuất
RUN mkdir -p /app/keys && chown -R $APP_UID /app/keys
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "QLKSan.Web.dll"]
