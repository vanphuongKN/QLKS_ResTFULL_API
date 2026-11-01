# QLKSan.Notification — build từ thư mục QLKSan/ (cần cả QLKSan.Shared): docker build -f QLKSan.Notification/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY QLKSan.Shared/QLKSan.Shared.csproj QLKSan.Shared/
COPY QLKSan.Notification/QLKSan.Notification.csproj QLKSan.Notification/
RUN dotnet restore QLKSan.Notification/QLKSan.Notification.csproj
COPY QLKSan.Shared/ QLKSan.Shared/
COPY QLKSan.Notification/ QLKSan.Notification/
RUN dotnet publish QLKSan.Notification/QLKSan.Notification.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh
WORKDIR /app
COPY --from=build /app .
# Email ghi ra file khi chưa cấu hình SMTP (gắn volume) và dấu "đã xử lý" của từng sự kiện
RUN mkdir -p /app/mail && chown -R $APP_UID /app/mail
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "QLKSan.Notification.dll"]
