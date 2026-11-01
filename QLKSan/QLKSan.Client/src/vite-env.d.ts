/// <reference types="vite/client" />

interface ImportMetaEnv {
  // Địa chỉ cổng MVC (QLKSan.Web) cho các trang nhân viên chưa chuyển sang React
  readonly VITE_MVC_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
