// Cổng MVC chạy song song trong thời gian chuyển giao diện sang React (dev: Visual Studio / run.bat, Docker: cổng 7081)
export const MVC_URL = (import.meta.env.VITE_MVC_URL ?? 'https://localhost:7234').replace(/\/$/, '');
