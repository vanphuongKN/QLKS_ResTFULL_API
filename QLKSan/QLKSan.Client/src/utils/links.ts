import { MENU_NHAN_VIEN } from '../auth/roles';

// Liên kết của trợ lý ảo trỏ tới trang Web MVC (/Customer/BookRoom, /Home/RoomType/{ma}):
// đổi sang trang tương ứng của SPA để khách ở lại trong ứng dụng; null = liên kết ngoài (bản đồ...), mở tab mới
export function spaPath(url: string): string | null {
  try {
    const u = new URL(url);
    if (u.pathname === '/Customer/BookRoom') return `/dat-phong${u.search}`;
    const m = u.pathname.match(/^\/Home\/RoomType\/([^/]+)$/);
    if (m) return `/hang-phong/${m[1]}`;
  } catch {
    /* không phải URL tuyệt đối */
  }
  return null;
}

// Đường quay lại sau đăng nhập chỉ nhận đường dẫn nội bộ ("/..." nhưng không phải "//host"), tránh chuyển hướng ra trang lạ
export function duongQuayLai(v: string | null | undefined, macDinh = '/'): string {
  return v && v.startsWith('/') && !v.startsWith('//') && !v.startsWith('/\\') ? v : macDinh;
}


// Đường dẫn cũ của cổng MVC (link đã lưu, bookmark, liên kết trong email / trợ lý ảo) → trang tương ứng của SPA.
// Không phân biệt hoa thường như MVC; giữ nguyên tham số (?maPhong=, ?search=...). null = không phải đường dẫn MVC đã biết.
const CHI_TIET_MVC: [RegExp, (m: RegExpMatchArray, q: URLSearchParams) => string][] = [
  [/^\/customer\/bookroom$/i, () => '/dat-phong'],
  [/^\/home\/roomtype\/([^/]+)$/i, (m) => `/hang-phong/${m[1]}`],
  [/^\/auth\/login$/i, () => '/dang-nhap'],
  [/^\/room\/details\/([^/]+)$/i, (m) => `/nhan-vien/phong/${m[1]}`],
  [/^\/reception\/customerdetails\/([^/]+)$/i, (m) => `/nhan-vien/khach-hang/${m[1]}`],
  [/^\/reception\/bookinghistory$/i, () => '/nhan-vien/lich-su-dat-phong'],
  [/^\/reception\/changeroom\/([^/]+)$/i, (m) => `/nhan-vien/le-tan/doi-phong/${m[1]}`],
  [/^\/reception\/newbooking$/i, () => '/nhan-vien/dat-phong-quay'],
  [/^\/reception\/walkin$/i, () => '/nhan-vien/vang-lai'],
  [/^\/checkout\/process$/i, () => '/nhan-vien/tra-phong'],
  [/^\/accounting\/invoice\/([^/]+)$/i, (m) => `/nhan-vien/ke-toan/hoa-don/${m[1]}`],
  [/^\/admin\/roomtypeimages\/([^/]+)$/i, (m) => `/nhan-vien/quan-tri/loai-phong/${m[1]}/anh`],
  [/^\/invoice\/print\/([^/]+)$/i, (m) => `/in/hoa-don/${m[1]}`],
  [/^\/nightaudit\/detail$/i, (_, q) => { const n = q.get('ngay'); q.delete('ngay'); return n ? `/nhan-vien/in/chot-ngay/${n.slice(0, 10)}` : '/nhan-vien/chot-ngay'; }]
];

export function duongDanMoi(pathname: string, search = ''): string | null {
  const p = pathname.replace(/\/+$/, '') || '/';
  const q = new URLSearchParams(search);
  let den: string | null = null;
  for (const [re, f] of CHI_TIET_MVC) {
    const m = p.match(re);
    if (m) { den = f(m, q); break; }
  }
  den ??= MENU_NHAN_VIEN.find((x) => x.mvc.toLowerCase() === p.toLowerCase())?.path ?? null;
  if (!den) return null;
  const s = q.toString();
  return s ? `${den}?${s}` : den;
}
