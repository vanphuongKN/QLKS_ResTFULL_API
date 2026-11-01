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
