import type { components } from './schema';
import { request } from './client';

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
  today: () => request<FrontDeskToday>('GET', '/api/front-desk/today')
};

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
