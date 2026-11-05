# QLKSan.Client — build từ thư mục QLKSan/ (cần ảnh mẫu trong QLKSan.Web/wwwroot/images): docker build -f QLKSan.Client/Dockerfile .
FROM node:22-alpine AS build
WORKDIR /src/QLKSan.Client
COPY QLKSan.Client/package.json QLKSan.Client/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY QLKSan.Client/ ./
COPY QLKSan.Web/wwwroot/images /src/QLKSan.Web/wwwroot/images
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.27-alpine
COPY QLKSan.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/QLKSan.Client/dist /usr/share/nginx/html
EXPOSE 8080
