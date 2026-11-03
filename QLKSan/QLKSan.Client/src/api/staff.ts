import type { components } from './schema';
import { ApiError, request } from './client';

// API cho cổng nhân viên (cùng endpoint mà QLKSan.Web gọi qua ApiClients/); quyền do API kiểm tra theo JWT
type S = components['schemas'];
export type HousekeepingSummary = S['HousekeepingDto'];
export type HousekeepingTask = S['HousekeepingTaskDto'];
export type HousekeepingRoom = S['HousekeepingRoomDto'];
export type HousekeepingTaskStatus = S['HousekeepingTaskStatus'];
export type MaintenanceTicket = S['MaintenanceTicketDto'];
export type MaintenanceCreate = S['MaintenanceCreateRequest'];
export type GuestRequestStatus = S['GuestRequestStatus'];
export type InspectTask = S['InspectTaskRequest'];
export type DailyTasksResult = S['DailyTasksResultDto'];
export type ServiceOrderItem = S['ServiceOrderItem'];
export type Service = S['ServiceDto'];
export type Staff = S['StaffDto'];
export type RoomStatus = S['RoomStatus'];
export type Room = S['RoomDto'];
export type RoomDetail = S['RoomDetailDto'];
export type TimelineBooking = S['TimelineBookingDto'];
export type GuestRegister = S['GuestRegisterDto'];
export type BookingStatus = S['BookingStatus'];
export type FrontDeskToday = S['FrontDeskTodayDto'];
export type FrontDeskStay = S['FrontDeskStayDto'];
export type StayQuote = S['StayChangeQuoteDto'];
export type Companion = S['CompanionRequest'];
export type CheckInBooking = S['CheckInBookingRequest'];
export type StaffBooking = S['BookingDto'];
export type FrontDeskBooking = S['FrontDeskBookingRequest'];
export type WalkIn = S['CheckInRequest'];
export type CustomerLookup = S['CustomerDto'];
export type GuestRequest = S['GuestRequestDto'];
export type GuestRequestCreate = S['GuestRequestCreateRequest'];
export type ShiftSummary = S['ShiftSummaryDto'];
export type ShiftHandover = S['ShiftHandoverDto'];
export type RoomTypeAvailability = S['RoomTypeAvailabilityDto'];
export type Customer = S['CustomerDto'];
export type CustomerDetail = S['CustomerDetailDto'];
export type CustomerProfile = S['UpdateCustomerProfileRequest'];
export type PointHistory = S['PointHistoryDto'];
export type Folio = S['FolioDto'];
export type Invoice = S['InvoiceDto'];
export type PaymentPart = S['PaymentPart'];
export type InvoiceCompany = S['InvoiceCompanyRequest'];
export type ServiceUsage = S['ServiceUsageDto'];
// Một dòng dịch vụ đã gọi kèm phiếu / phòng (bảng "Dịch vụ đã gọi")
export interface ServiceUsageRow { u: ServiceUsage; maDatPhong: string; maPhong: string; soPhong: string }

const e = encodeURIComponent;

export const hkApi = {
  summary: () => request<HousekeepingSummary>('GET', '/api/housekeeping'),
  myTasks: () => request<HousekeepingTask[]>('GET', '/api/housekeeping/tasks/mine'),
  markClean: (maPhong: string) => request<void>('POST', `/api/housekeeping/rooms/${e(maPhong)}/clean`),
  assign: (maPhanCong: string, maNhanVien: string) => request<void>('POST', `/api/housekeeping/tasks/${e(maPhanCong)}/assign`, { MaNhanVien: maNhanVien }),
  assignBulk: (maPhanCongs: string[], maNhanVien: string) => request<number>('POST', '/api/housekeeping/tasks/assign-bulk', { MaPhanCongs: maPhanCongs, MaNhanVien: maNhanVien }),
  inspect: (maPhanCong: string, r: InspectTask) => request<void>('POST', `/api/housekeeping/tasks/${e(maPhanCong)}/inspect`, r),
  generateDaily: () => request<DailyTasksResult>('POST', '/api/housekeeping/daily-tasks'),
  skipDnd: (maPhanCong: string) => request<void>('POST', `/api/housekeeping/tasks/${e(maPhanCong)}/dnd`),
  minibar: (maPhong: string, items: ServiceOrderItem[]) => request<number>('POST', `/api/housekeeping/rooms/${e(maPhong)}/minibar`, { Items: items }),
  start: (maPhanCong: string) => request<void>('POST', `/api/housekeeping/tasks/${e(maPhanCong)}/start`),
  complete: (maPhanCong: string) => request<void>('POST', `/api/housekeeping/tasks/${e(maPhanCong)}/complete`),
  reportIssue: (maPhanCong: string, ghiChu: string) => request<void>('POST', `/api/housekeeping/tasks/${e(maPhanCong)}/issue`, { GhiChu: ghiChu })
};

export const maintenanceApi = {
  // loc: mo (đang mở) | xong (7 ngày) | null = tất cả (30 ngày)
  list: (loc: string | null) => request<MaintenanceTicket[]>('GET', '/api/maintenance' + (loc ? `?loc=${e(loc)}` : '')),
  create: (r: MaintenanceCreate) => request<MaintenanceTicket>('POST', '/api/maintenance', r),
  update: (maPhieu: number, trangThai: GuestRequestStatus, ketQua: string | null) =>
    request<MaintenanceTicket>('PATCH', `/api/maintenance/${maPhieu}`, { TrangThai: trangThai, KetQua: ketQua })
};

export const roomApi = {
  list: () => request<Room[]>('GET', '/api/rooms'),
  // null = không có phòng (404)
  get: (maPhong: string) => request<RoomDetail>('GET', `/api/rooms/${e(maPhong)}`),
  setStatus: (maPhong: string, trangThai: RoomStatus) => request<void>('PATCH', `/api/rooms/${e(maPhong)}`, { TrangThai: trangThai }),
  // Lịch phòng: phiếu chiếm phòng trong khoảng ngày (tối đa 62 ngày)
  timeline: (tuNgay: string, denNgay: string) => request<TimelineBooking[]>('GET', `/api/bookings/timeline?tuNgay=${tuNgay}&denNgay=${denNgay}`)
};

export const frontDeskApi = {
  today: () => request<FrontDeskToday>('GET', '/api/front-desk/today'),
  shift: () => request<ShiftSummary>('GET', '/api/front-desk/shift'),
  closeShift: (tienMatThucTe: number, ghiChu: string | null) => request<ShiftHandover>('POST', '/api/front-desk/shift/close', { TienMatThucTe: tienMatThucTe, GhiChuBanGiao: ghiChu }),
  handovers: (soNgay = 7) => request<ShiftHandover[]>('GET', `/api/front-desk/shifts?soNgay=${soNgay}`),
  // Đặt phòng qua quầy (điện thoại / email / tại quầy / đại lý), nhận khách vãng lai
  book: (r: FrontDeskBooking) => request<StaffBooking>('POST', '/api/bookings/front-desk', r),
  walkIn: (r: WalkIn) => request<StaffBooking>('POST', '/api/bookings/walk-in', r),
  availableTypes: (checkIn: string, checkOut: string, soNguoi: number) =>
    request<RoomTypeAvailability[]>('GET', `/api/room-types/available?checkIn=${checkIn}&checkOut=${checkOut}&soNguoi=${soNguoi}`),
  // Tra khách cũ theo đúng số CCCD / hộ chiếu hoặc số điện thoại
  lookup: (q: string) => request<CustomerLookup[]>('GET', `/api/customers/lookup?q=${e(q)}`),
  activeStays: () => request<StaffBooking[]>('GET', '/api/bookings/active')
};

export const guestRequestApi = {
  // loc: mo (đang mở) | xong (7 ngày) | null = tất cả (30 ngày)
  list: (loc: string | null) => request<GuestRequest[]>('GET', '/api/guest-requests' + (loc ? `?loc=${e(loc)}` : '')),
  create: (r: GuestRequestCreate) => request<GuestRequest>('POST', '/api/guest-requests', r),
  update: (ma: number, trangThai: GuestRequestStatus, ketQua: string | null) => request<GuestRequest>('PATCH', `/api/guest-requests/${ma}`, { TrangThai: trangThai, KetQua: ketQua })
};

// Loại yêu cầu khách ghi tại quầy; tên hiển thị trạng thái yêu cầu (QLKSan.Shared)
export const LOAI_YEU_CAU = ['Báo thức', 'Đặt xe / đưa đón', 'Nhắn gửi', 'Đồ thất lạc', 'Buồng phòng', 'Kỹ thuật', 'Khác'];
export const TRANG_THAI_YEU_CAU: Record<GuestRequestStatus, string> = { New: 'Mới', InProgress: 'Đang xử lý', Done: 'Hoàn thành', Cancelled: 'Đã huỷ' };

// Thao tác trên phiếu đặt phòng tại quầy (như BookingApiClient của Web)
export const bookingApi = {
  get: (ma: string) => request<StaffBooking>('GET', `/api/bookings/${e(ma)}`),
  approve: (ma: string) => request<void>('POST', `/api/bookings/${e(ma)}/approve`),
  // Không có thông tin thêm (số khách, cọc, người đi cùng) thì gửi không body, như MVC
  checkIn: (ma: string, r: CheckInBooking | null) => request<void>('POST', `/api/bookings/${e(ma)}/check-in`, r ?? undefined),
  cancel: (ma: string, lyDo: string, hoanCoc: boolean) => request<void>('POST', `/api/bookings/${e(ma)}/cancel`, { LyDo: lyDo, HoanCoc: hoanCoc }),
  deposit: (ma: string, soTien: number, phuongThuc: string, ghiChu: string | null) => request<StaffBooking>('POST', `/api/bookings/${e(ma)}/deposits`, { SoTien: soTien, PhuongThuc: phuongThuc, GhiChu: ghiChu }),
  quoteStay: (ma: string, ngayTra: string, ngayNhan?: string | null) =>
    request<StayQuote>('GET', `/api/bookings/${e(ma)}/stay-dates/quote?ngayTra=${ngayTra}` + (ngayNhan ? `&ngayNhan=${ngayNhan}` : '')),
  changeStay: (ma: string, ngayTra: string, ngayNhan?: string | null) => request<StaffBooking>('PUT', `/api/bookings/${e(ma)}/stay-dates`, { NgayNhan: ngayNhan ?? null, NgayTra: ngayTra }),
  companions: (ma: string, ds: Companion[]) => request<StaffBooking>('PUT', `/api/bookings/${e(ma)}/companions`, { KhachDiCung: ds }),
  changeRoom: (ma: string, maPhong: string) => request<void>('POST', `/api/bookings/${e(ma)}/change-room`, { MaPhong: maPhong }),
  moveRoom: (ma: string, maPhong: string, lyDo: string, giuGiaCu: boolean) => request<StaffBooking>('POST', `/api/bookings/${e(ma)}/move`, { MaPhong: maPhong, LyDo: lyDo, GiuGiaCu: giuGiaCu }),
  hkPrefs: (ma: string, r: { KhongLamPhienHomNay: boolean; GioDonMongMuon: string | null; TuChoiDonHangNgay: boolean }) =>
    request<S['HousekeepingPreferencesDto']>('PUT', `/api/housekeeping/stays/${e(ma)}/preferences`, r)
};

// Phòng trống ngay lúc này (không tham số, như WalkIn của MVC)
export const availableRoomsNow = () => request<Room[]>('GET', '/api/rooms/available');
// Phòng trống trong khoảng ngày, đủ sức chứa
export const availableRooms = (checkIn: string, checkOut: string, soNguoi: number) =>
  request<Room[]>('GET', `/api/rooms/available?checkIn=${checkIn}&checkOut=${checkOut}&soNguoi=${soNguoi}`);

// Phương thức thu tiền tại quầy, kênh đặt qua quầy (QLKSan.Shared/Enums/FrontDeskConstants.cs)
export const PHUONG_THUC_QUAY = ['Tiền mặt', 'Thẻ tín dụng', 'Chuyển khoản'];
export const NGUON_QUA_QUAY = ['Điện thoại', 'Email', 'Tại quầy', 'Đại lý'];

export const reportApi = {
  guests: (tuNgay: string, denNgay: string) => request<GuestRegister[]>('GET', `/api/reports/guests?tuNgay=${tuNgay}&denNgay=${denNgay}`),
  guestsExportPath: (tuNgay: string, denNgay: string) => `/api/reports/guests/export?tuNgay=${tuNgay}&denNgay=${denNgay}`
};

// Tên hiển thị trạng thái phiếu đặt phòng (Display(Name) của enum BookingStatus)
export const TRANG_THAI_PHIEU: Record<BookingStatus, string> = {
  Pending: 'Chờ duyệt', Approved: 'Đã duyệt', CheckedIn: 'Đã nhận phòng', CheckedOut: 'Đã trả phòng', Cancelled: 'Đã hủy', Completed: 'Hoàn thành'
};

export const serviceApi = {
  list: () => request<Service[]>('GET', '/api/services')
};

// Tên hiển thị trạng thái việc buồng phòng (Display(Name) của enum HousekeepingTaskStatus)
export const TRANG_THAI_VIEC: Record<HousekeepingTaskStatus, string> = {
  Unassigned: 'Chờ giao', Assigned: 'Đã giao', InProgress: 'Đang dọn', Completed: 'Hoàn thành', Issue: 'Sự cố', AwaitingInspection: 'Chờ kiểm tra'
};

// 404 → null (như HttpGetOrDefaultAsync của MVC)
async function hoacNull<T>(p: Promise<T>): Promise<T | null> {
  try { return (await p) ?? null; } catch (e) { if (e instanceof ApiError && e.status === 404) return null; throw e; }
}

export const customerApi = {
  list: (search?: string | null) => request<Customer[]>('GET', '/api/customers' + (search?.trim() ? `?search=${e(search.trim())}` : '')),
  get: (ma: string) => hoacNull(request<CustomerDetail>('GET', `/api/customers/${e(ma)}`)),
  update: (ma: string, r: CustomerProfile) => request<void>('PUT', `/api/customers/${e(ma)}`, r),
  points: (ma: string) => request<PointHistory[]>('GET', `/api/customers/${e(ma)}/points`),
  adjustPoints: (ma: string, soDiem: number, lyDo: string) => request<void>('POST', `/api/customers/${e(ma)}/point-adjustments`, { SoDiem: soDiem, LyDo: lyDo })
};

// Lịch sử đặt phòng (tìm theo mã phiếu, tên khách, số phòng), gọi dịch vụ / huỷ dịch vụ gọi nhầm
export const bookingHistoryApi = {
  list: (search?: string | null) => request<StaffBooking[]>('GET', '/api/bookings' + (search?.trim() ? `?search=${e(search.trim())}` : '')),
  orderServices: (ma: string, items: ServiceOrderItem[]) => request<StaffBooking>('POST', `/api/bookings/${e(ma)}/services`, { Services: items }),
  voidService: (ma: string, maSDDV: string, soLuong: number, lyDo: string) => request<StaffBooking>('POST', `/api/bookings/${e(ma)}/services/${e(maSDDV)}/void`, { SoLuong: soLuong, LyDo: lyDo })
};

// Trả phòng: phiếu đang ở của phòng, bảng kê, lập hoá đơn (kèm mã KM), thanh toán (đổi điểm, tách khoản), thông tin VAT công ty
export const checkoutApi = {
  currentStay: (maPhong: string) => hoacNull(request<StaffBooking>('GET', `/api/rooms/${e(maPhong)}/current-stay`)),
  folio: (maDatPhong: string) => request<Folio>('GET', `/api/bookings/${e(maDatPhong)}/folio`),
  generate: (maDatPhong: string, maKhuyenMai: string | null) => request<Invoice>('POST', '/api/invoices', { MaDatPhong: maDatPhong, MaKhuyenMai: maKhuyenMai }),
  pay: (maHoaDon: string, phuongThuc: string, diemSuDung: number, cacKhoan: PaymentPart[] | null) =>
    request<Invoice>('POST', `/api/invoices/${e(maHoaDon)}/payments`, { PhuongThuc: phuongThuc, DiemSuDung: diemSuDung, CacKhoan: cacKhoan }),
  setCompany: (maHoaDon: string, r: InvoiceCompany) => request<Invoice>('PUT', `/api/invoices/${e(maHoaDon)}/company`, r)
};

// ===== R4: kế toán, chốt ngày =====
export type AccountingOverview = S['AccountingOverviewDto'];
export type InvoiceListItem = S['InvoiceListItemDto'];
export type InvoiceDetail = S['InvoiceDetailDto'];
export type InvoiceStatus = NonNullable<S['InvoiceStatus']>;
export type DepositReport = S['DepositReportDto'];
export type RevenueReport = S['RevenueReportDto'];
export type OccupancyReport = S['OccupancyReportDto'];
export type ShiftMethod = S['ShiftMethodDto'];
export type NightAuditPreview = S['NightAuditPreviewDto'];
export type NightAuditFigures = S['NightAuditFiguresDto'];
export type DayClose = S['DayCloseDto'];

const khoang = (tuNgay: string, denNgay: string) => `tuNgay=${tuNgay}&denNgay=${denNgay}`;

export const TRANG_THAI_HOA_DON: Record<InvoiceStatus, string> = { Unpaid: 'Chưa thanh toán', Paid: 'Đã thanh toán', Cancelled: 'Đã hủy' };
export const TEN_TRANG_THAI_PHIEU: Record<BookingStatus, string> = {
  Pending: 'Chờ duyệt', Approved: 'Đã duyệt', CheckedIn: 'Đã nhận phòng', CheckedOut: 'Đã trả phòng', Cancelled: 'Đã hủy', Completed: 'Hoàn thành'
};

export interface LocHoaDon { tuNgay?: string | null; denNgay?: string | null; trangThai?: InvoiceStatus | null; search?: string | null; congTy?: boolean }

export const accountingApi = {
  overview: (tuNgay: string, denNgay: string) => request<AccountingOverview>('GET', `/api/reports/overview?${khoang(tuNgay, denNgay)}`),
  invoices: (l: LocHoaDon) => {
    const q = new URLSearchParams();
    if (l.congTy) q.set('congTy', 'true');
    if (l.tuNgay) q.set('tuNgay', l.tuNgay);
    if (l.denNgay) q.set('denNgay', l.denNgay);
    if (l.trangThai) q.set('trangThai', l.trangThai);
    if (l.search?.trim()) q.set('search', l.search.trim());
    const s = q.toString();
    return request<InvoiceListItem[]>('GET', '/api/invoices' + (s ? `?${s}` : ''));
  },
  invoice: (ma: string) => hoacNull(request<InvoiceDetail>('GET', `/api/invoices/${e(ma)}`)),
  refund: (ma: string, soTien: number, phuongThuc: string, lyDo: string) => request<InvoiceDetail>('POST', `/api/invoices/${e(ma)}/refunds`, { SoTien: soTien, PhuongThuc: phuongThuc, LyDo: lyDo }),
  setEInvoice: (ma: string, so: string | null) => request<Invoice>('PUT', `/api/invoices/${e(ma)}/e-invoice`, { SoHoaDonDienTu: so }),
  deposits: (tuNgay: string, denNgay: string) => request<DepositReport>('GET', `/api/reports/deposits?${khoang(tuNgay, denNgay)}`),
  shifts: (tuNgay: string, denNgay: string) => request<ShiftHandover[]>('GET', `/api/reports/shifts?${khoang(tuNgay, denNgay)}`),
  revenue: (tuNgay: string, denNgay: string) => request<RevenueReport>('GET', `/api/reports/revenue?${khoang(tuNgay, denNgay)}`),
  occupancy: (tuNgay: string, denNgay: string) => request<OccupancyReport>('GET', `/api/reports/occupancy?${khoang(tuNgay, denNgay)}`),
  revenueExportPath: (tuNgay: string, denNgay: string) => `/api/reports/revenue/export?${khoang(tuNgay, denNgay)}`
};

export const nightAuditApi = {
  preview: (ngay?: string | null) => request<NightAuditPreview>('GET', '/api/night-audit/preview' + (ngay ? `?ngay=${ngay}` : '')),
  list: (tuNgay: string, denNgay: string) => request<DayClose[]>('GET', `/api/night-audit?${khoang(tuNgay, denNgay)}`),
  get: (ngay: string) => hoacNull(request<DayClose>('GET', `/api/night-audit/${ngay}`)),
  close: (ngay: string, giuLai: string[], ghiChu: string | null) => request<DayClose>('POST', '/api/night-audit/close', { Ngay: ngay, GiuLai: giuLai, GhiChu: ghiChu }),
  reopen: (ngay: string, lyDo: string) => request<void>('POST', `/api/night-audit/${ngay}/reopen`, { LyDo: lyDo })
};
