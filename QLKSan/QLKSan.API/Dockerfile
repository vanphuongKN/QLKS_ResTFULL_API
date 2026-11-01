# QLKSan.API — build từ thư mục QLKSan/ (cần cả QLKSan.Shared): docker build -f QLKSan.API/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY QLKSan.Shared/QLKSan.Shared.csproj QLKSan.Shared/
COPY QLKSan.API/QLKSan.API.csproj QLKSan.API/
RUN dotnet restore QLKSan.API/QLKSan.API.csproj
COPY QLKSan.Shared/ QLKSan.Shared/
COPY QLKSan.API/ QLKSan.API/
RUN dotnet publish QLKSan.API/QLKSan.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Nghiệp vụ dùng giờ địa phương (ngày nhận/trả phòng, chốt ngày): cần tzdata để TZ có hiệu lực
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh
WORKDIR /app
COPY --from=build /app .
# Thư mục ảnh tải lên (gắn volume) phải ghi được bởi user không phải root
RUN mkdir -p wwwroot/uploads && chown -R $APP_UID wwwroot/uploads
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "QLKSan.API.dll"]
