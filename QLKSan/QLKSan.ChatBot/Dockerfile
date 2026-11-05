# QLKSan.ChatBot — build từ thư mục QLKSan/ (cần cả QLKSan.Shared): docker build -f QLKSan.ChatBot/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY QLKSan.Shared/QLKSan.Shared.csproj QLKSan.Shared/
COPY QLKSan.ChatBot/QLKSan.ChatBot.csproj QLKSan.ChatBot/
RUN dotnet restore QLKSan.ChatBot/QLKSan.ChatBot.csproj
COPY QLKSan.Shared/ QLKSan.Shared/
COPY QLKSan.ChatBot/ QLKSan.ChatBot/
RUN dotnet publish QLKSan.ChatBot/QLKSan.ChatBot.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh
WORKDIR /app
COPY --from=build /app .
# CSDL SQLite của hội thoại (gắn volume)
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "QLKSan.ChatBot.dll"]
