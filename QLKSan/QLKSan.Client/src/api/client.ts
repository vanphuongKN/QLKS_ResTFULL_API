// Gọi API qua Gateway (cùng nguồn: /api được proxy khi dev, do nginx chuyển tiếp khi chạy Docker).
// Kiểu dữ liệu sinh từ OpenAPI của QLKSan.API: npm run gen:api
import type { components } from './schema';

type S = components['schemas'];
export type HotelInfo = S['HotelInfoDto'];
export type RoomType = S['RoomTypeDto'];
export type RoomTypeDetail = S['RoomTypeDetailDto'];
export type RoomTypeAvailability = S['RoomTypeAvailabilityDto'];
export type Review = S['ReviewDto'];
export type Service = S['ServiceDto'];
export type Booking = S['BookingDto'];
export type BookingStatus = S['BookingStatus'];
export type Customer = S['CustomerDto'];
export type PointHistory = S['PointHistoryDto'];
export type LoginResponse = S['LoginResponse'];
export type RegisterRequest = S['RegisterRequest'];
export type BookRoomRequest = S['BookRoomRequest'];
export type UpdateContactRequest = S['UpdateContactRequest'];
export type ReviewRequest = S['ReviewRequest'];
export type ChangePasswordRequest = S['ChangePasswordRequest'];
export type NightAuditStatus = S['NightAuditStatusDto'];
export type Promotion = S['PromotionDto'];
export type InvoiceDetail = S['InvoiceDetailDto'];

// Lỗi từ API (ProblemDetails): thông báo tiếng Việt đọc được cho người dùng
export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

let accessToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

// Gửi yêu cầu kèm token; lỗi mạng / HTTP đổi thành ApiError (thông báo tiếng Việt)
async function send(method: string, path: string, body?: BodyInit, contentType?: string, accept = 'application/json'): Promise<Response> {
  const headers: Record<string, string> = { Accept: accept };
  if (contentType) headers['Content-Type'] = contentType;
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`;

  let res: Response;
  try {
    res = await fetch(path, { method, headers, body });
  } catch {
    throw new ApiError(0, 'Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.');
  }

  if (res.status === 401 && accessToken) onUnauthorized?.();
  if (!res.ok) throw new ApiError(res.status, await problemMessage(res));
  return res;
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await send(method, path, body === undefined ? undefined : JSON.stringify(body), body === undefined ? undefined : 'application/json');
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

async function problemMessage(res: Response): Promise<string> {
  try {
    const p = await res.json();
    const errors = p.errors ? Object.values(p.errors as Record<string, string[]>).flat() : [];
    if (errors.length) return errors.join(' ');
    if (p.detail) return p.detail;
    if (p.title) return p.title;
  } catch {
    /* không phải JSON */
  }
  if (res.status === 401) return 'Vui lòng đăng nhập.';
  if (res.status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
  if (res.status === 429) return 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.';
  return 'Có lỗi xảy ra, vui lòng thử lại.';
}

// Tải lên tệp (multipart/form-data); trình duyệt tự đặt Content-Type kèm boundary
async function upload<T>(path: string, file: File, field = 'file'): Promise<T> {
  const form = new FormData();
  form.append(field, file);
  const res = await send('POST', path, form);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

// Tên tệp từ Content-Disposition: ưu tiên filename*=UTF-8''... (tên tiếng Việt), sau đó filename="..."
export function tenTepTuHeader(header: string | null): string | null {
  if (!header) return null;
  const utf8 = header.match(/filename\*\s*=\s*(?:UTF-8|utf-8)''([^;]+)/);
  if (utf8) {
    try {
      return decodeURIComponent(utf8[1].trim().replace(/^"|"$/g, ''));
    } catch {
      /* mã hoá hỏng: thử filename= */
    }
  }
  const thuong = header.match(/filename\s*=\s*("([^"]*)"|[^;]+)/);
  return thuong ? (thuong[2] ?? thuong[1]).trim() : null;
}

// Tải tệp cần đăng nhập (vd Excel báo cáo): không mở link trực tiếp được vì phải gửi kèm Bearer token
async function download(path: string, tenMacDinh: string): Promise<string> {
  const res = await send('GET', path, undefined, undefined, '*/*');
  const ten = tenTepTuHeader(res.headers.get('Content-Disposition')) ?? tenMacDinh;
  const url = URL.createObjectURL(await res.blob());
  const a = document.createElement('a');
  a.href = url;
  a.download = ten;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 30_000);
  return ten;
}

const q = (params: Record<string, string | number>) => new URLSearchParams(Object.entries(params).map(([k, v]) => [k, String(v)])).toString();

export const api = {
  hotel: () => request<HotelInfo>('GET', '/api/hotel'),
  roomTypes: () => request<RoomType[]>('GET', '/api/room-types'),
  roomType: (maLoai: string) => request<RoomTypeDetail>('GET', `/api/room-types/${encodeURIComponent(maLoai)}`),
  available: (checkIn: string, checkOut: string, soNguoi: number) =>
    request<RoomTypeAvailability[]>('GET', `/api/room-types/available?${q({ checkIn, checkOut, soNguoi })}`),
  featuredReviews: (soLuong = 3) => request<Review[]>('GET', `/api/reviews/featured?soLuong=${soLuong}`),
  services: () => request<Service[]>('GET', '/api/services'),

  login: (tenDangNhap: string, matKhau: string) => request<LoginResponse>('POST', '/api/auth/login', { TenDangNhap: tenDangNhap, MatKhau: matKhau }),
  register: (r: RegisterRequest) => request<LoginResponse>('POST', '/api/auth/register', r),

  me: () => request<Customer>('GET', '/api/customers/me'),
  updateContact: (r: UpdateContactRequest) => request<Customer>('PUT', '/api/customers/me/contact', r),
  myPoints: () => request<PointHistory[]>('GET', '/api/customers/me/points'),
  myBookings: () => request<Booking[]>('GET', '/api/customers/me/bookings'),
  myReviews: () => request<Review[]>('GET', '/api/customers/me/reviews'),
  booking: (maDatPhong: string) => request<Booking>('GET', `/api/bookings/${encodeURIComponent(maDatPhong)}`),
  book: (r: BookRoomRequest) => request<Booking>('POST', '/api/bookings', r),
  cancel: (maDatPhong: string, lyDo: string) => request<void>('POST', `/api/bookings/${encodeURIComponent(maDatPhong)}/cancel`, { LyDo: lyDo }),
  review: (r: ReviewRequest) => request<Review>('POST', '/api/reviews', r),

  changePassword: (r: ChangePasswordRequest) => request<void>('POST', '/api/auth/change-password', r),
  nightAuditStatus: () => request<NightAuditStatus>('GET', '/api/night-audit/status'),
  // Khuyến mãi đang hiệu lực theo mã: 404 không tồn tại, 400 hết hạn / chưa kích hoạt
  promotion: (ma: string) => request<Promotion>('GET', `/api/promotions/${encodeURIComponent(ma)}`),
  invoice: (maHoaDon: string) => request<InvoiceDetail>('GET', `/api/invoices/${encodeURIComponent(maHoaDon)}`)
};

export { request, upload, download };
