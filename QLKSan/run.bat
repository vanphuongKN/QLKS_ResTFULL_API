@echo off
rem Chay he thong khi phat trien: QLKSan.API (https://localhost:7001) + QLKSan.Gateway (https://localhost:7000) + QLKSan.ChatBot (https://localhost:7004)
rem + giao dien chinh React QLKSan.Client (http://localhost:5173: trang khach + cong nhan vien /nhan-vien, Vite proxy /api toi Gateway; can Node.js)
rem + cong MVC QLKSan.Web (https://localhost:7234) giu lam du phong.
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

echo [2/3] Chay API, Gateway, ChatBot, Web du phong va giao dien React (moi server mot cua so, dong cua so de tat)...
start "QLKSan.API - https://localhost:7001" cmd /k dotnet run --no-build --project QLKSan.API --launch-profile https
start "QLKSan.Gateway - https://localhost:7000" cmd /k dotnet run --no-build --project QLKSan.Gateway --launch-profile https
start "QLKSan.ChatBot - https://localhost:7004" cmd /k dotnet run --no-build --project QLKSan.ChatBot --launch-profile https
start "QLKSan.Web - https://localhost:7234" cmd /k dotnet run --no-build --project QLKSan.Web --launch-profile https

set CLIENT=
where npm >nul 2>nul
if not errorlevel 1 set CLIENT=1
if defined CLIENT (
    if not exist QLKSan.Client\node_modules (
        echo Cai goi npm cho giao dien React - chi lan dau...
        pushd QLKSan.Client
        call npm install --no-audit --no-fund
        popd
    )
    start "QLKSan.Client - http://localhost:5173" cmd /k "cd /d QLKSan.Client && npm run dev"
)

echo [3/3] Doi server khoi dong roi mo trinh duyet...
timeout /t 10 /nobreak >nul
rem Giao dien chinh la React; may khong co Node.js thi mo cong MVC du phong
if defined CLIENT (
    start "" http://localhost:5173
) else (
    echo Khong tim thay Node.js: mo cong MVC du phong https://localhost:7234 - cai Node.js 22 de dung giao dien React.
    start "" https://localhost:7234
)
