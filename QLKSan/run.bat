@echo off
rem Chay he thong khi phat trien: QLKSan.API (https://localhost:7001) + QLKSan.Gateway (https://localhost:7000) + QLKSan.Web (https://localhost:7234)
rem Web goi API qua Gateway. Service thong bao (QLKSan.Notification) can RabbitMQ, chay rieng khi can (xem README).
rem Moi project chay qua launch profile "https" => moi truong Development, Web doc CSS/JS/anh tu wwwroot.
rem Khong mo truc tiep bin\...\QLKSan.Web.exe: khi do la moi truong Production, file tinh trong wwwroot khong duoc phuc vu.
cd /d "%~dp0"

echo [1/3] Build solution...
dotnet build QLKSan.slnx -v q -nologo
if errorlevel 1 (
    echo Build loi, xem thong bao phia tren.
    pause
    exit /b 1
)

echo [2/3] Chay API, Gateway va Web (moi server mot cua so, dong cua so de tat)...
start "QLKSan.API - https://localhost:7001" cmd /k dotnet run --no-build --project QLKSan.API --launch-profile https
start "QLKSan.Gateway - https://localhost:7000" cmd /k dotnet run --no-build --project QLKSan.Gateway --launch-profile https
start "QLKSan.Web - https://localhost:7234" cmd /k dotnet run --no-build --project QLKSan.Web --launch-profile https

echo [3/3] Doi server khoi dong roi mo trinh duyet...
timeout /t 10 /nobreak >nul
start "" https://localhost:7234
