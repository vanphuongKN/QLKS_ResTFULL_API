import type { components } from '../api/schema';

export type Role = components['schemas']['Role'];
export type StaffRole = Exclude<Role, 'Customer'>;

export const NHAN_VAI_TRO: Record<Role, string> = {
  Admin: 'Quản trị',
  Receptionist: 'Lễ tân',
  Housekeeping: 'Buồng phòng',
  Accountant: 'Kế toán',
  Customer: 'Khách hàng'
};

// Nhóm vai trò khớp phân quyền của API / Web MVC (WebRoles)
export const VAI_TRO = {
  quanTri: ['Admin'],
  leTan: ['Admin', 'Receptionist'],
  thuNgan: ['Admin', 'Receptionist', 'Accountant'],
  keToan: ['Admin', 'Accountant'],
  buongPhong: ['Housekeeping'],
  // Khớp AppRoles của API: người dọn (Việc của tôi), cả đội buồng phòng (minibar, mở phiếu kỹ thuật), người kiểm tra phòng
  nguoiDon: ['Admin', 'Housekeeping'],
  doiBuongPhong: ['Admin', 'Receptionist', 'Housekeeping'],
  kiemTraPhong: ['Admin', 'Receptionist']
} satisfies Record<string, StaffRole[]>;

export const laNhanVien = (role?: Role | null): role is StaffRole => !!role && role !== 'Customer';

export interface MucMenu {
  nhom: string;
  ten: string;
  path: string;
  roles: StaffRole[];
  // Đường dẫn của trang tương ứng trên cổng MVC (dự phòng): link cũ được chuyển sang trang React này (utils/links.ts)
  mvc: string;
  // Vai trò được mở trang (gõ đường dẫn / nút trong trang) dù menu không hiện mục này, như [Authorize] của MVC
  quyenThem?: StaffRole[];
}

// Menu cổng nhân viên: cùng nhóm, thứ tự và quyền với sidebar của QLKSan.Web (Views/Shared/_Layout.cshtml)
export const MENU_NHAN_VIEN: MucMenu[] = [
  { nhom: '', ten: 'Bảng điều khiển', path: '/nhan-vien/quan-tri', roles: VAI_TRO.quanTri, mvc: '/Admin' },
  { nhom: '', ten: 'Việc của tôi', path: '/nhan-vien/buong-phong/viec-cua-toi', roles: VAI_TRO.buongPhong, mvc: '/Housekeeping/MyTasks', quyenThem: VAI_TRO.nguoiDon },
  { nhom: '', ten: 'Ghi minibar', path: '/nhan-vien/buong-phong/minibar', roles: VAI_TRO.buongPhong, mvc: '/Housekeeping/Minibar', quyenThem: VAI_TRO.doiBuongPhong },
  { nhom: '', ten: 'Kỹ thuật', path: '/nhan-vien/ky-thuat', roles: VAI_TRO.buongPhong, mvc: '/Housekeeping/Maintenance' },

  { nhom: 'Vận hành', ten: 'Lễ tân', path: '/nhan-vien/le-tan', roles: VAI_TRO.leTan, mvc: '/Reception' },
  { nhom: 'Vận hành', ten: 'Phòng nghỉ', path: '/nhan-vien/phong', roles: VAI_TRO.thuNgan, mvc: '/Room/Status' },
  { nhom: 'Vận hành', ten: 'Lịch phòng', path: '/nhan-vien/lich-phong', roles: VAI_TRO.thuNgan, mvc: '/Room/Timeline' },
  { nhom: 'Vận hành', ten: 'Buồng phòng', path: '/nhan-vien/buong-phong', roles: VAI_TRO.leTan, mvc: '/Housekeeping/Tasks' },
  { nhom: 'Vận hành', ten: 'Kỹ thuật', path: '/nhan-vien/ky-thuat', roles: VAI_TRO.leTan, mvc: '/Housekeeping/Maintenance' },
  { nhom: 'Vận hành', ten: 'Gọi dịch vụ', path: '/nhan-vien/dich-vu', roles: VAI_TRO.leTan, mvc: '/Service' },
  { nhom: 'Vận hành', ten: 'Yêu cầu khách', path: '/nhan-vien/yeu-cau', roles: VAI_TRO.leTan, mvc: '/Reception/Requests' },
  { nhom: 'Vận hành', ten: 'Giao ca', path: '/nhan-vien/giao-ca', roles: VAI_TRO.leTan, mvc: '/Reception/Shift' },
  { nhom: 'Vận hành', ten: 'Chốt ngày', path: '/nhan-vien/chot-ngay', roles: VAI_TRO.thuNgan, mvc: '/NightAudit' },
  { nhom: 'Vận hành', ten: 'Khách hàng', path: '/nhan-vien/khach-hang', roles: VAI_TRO.leTan, mvc: '/Reception/Customers' },
  { nhom: 'Vận hành', ten: 'Sổ lưu trú', path: '/nhan-vien/so-luu-tru', roles: VAI_TRO.thuNgan, mvc: '/Room/Guests' },

  { nhom: 'Doanh thu', ten: 'Tổng quan kế toán', path: '/nhan-vien/ke-toan', roles: VAI_TRO.keToan, mvc: '/Accounting' },
  { nhom: 'Doanh thu', ten: 'Hoá đơn', path: '/nhan-vien/ke-toan/hoa-don', roles: VAI_TRO.keToan, mvc: '/Accounting/Invoices' },
  { nhom: 'Doanh thu', ten: 'Đối soát', path: '/nhan-vien/ke-toan/doi-soat', roles: VAI_TRO.keToan, mvc: '/Accounting/Reconciliation' },
  { nhom: 'Doanh thu', ten: 'Hoá đơn VAT công ty', path: '/nhan-vien/ke-toan/vat', roles: VAI_TRO.keToan, mvc: '/Accounting/VatInvoices' },
  { nhom: 'Doanh thu', ten: 'Báo cáo', path: '/nhan-vien/ke-toan/bao-cao', roles: VAI_TRO.keToan, mvc: '/Accounting/Reports' },
  { nhom: 'Doanh thu', ten: 'Bảng giá', path: '/nhan-vien/quan-tri/bang-gia', roles: VAI_TRO.quanTri, mvc: '/Admin/Pricing' },
  { nhom: 'Doanh thu', ten: 'Khuyến mãi', path: '/nhan-vien/quan-tri/khuyen-mai', roles: VAI_TRO.quanTri, mvc: '/Admin/Promotions' },

  { nhom: 'Danh mục', ten: 'Loại phòng', path: '/nhan-vien/quan-tri/loai-phong', roles: VAI_TRO.quanTri, mvc: '/Admin/RoomTypes' },
  { nhom: 'Danh mục', ten: 'Phòng', path: '/nhan-vien/quan-tri/phong', roles: VAI_TRO.quanTri, mvc: '/Admin/Rooms' },
  { nhom: 'Danh mục', ten: 'Tiện nghi', path: '/nhan-vien/quan-tri/tien-nghi', roles: VAI_TRO.quanTri, mvc: '/Admin/Amenities' },
  { nhom: 'Danh mục', ten: 'Dịch vụ & giá', path: '/nhan-vien/quan-tri/dich-vu', roles: VAI_TRO.quanTri, mvc: '/Admin/Services' },
  { nhom: 'Danh mục', ten: 'Đánh giá', path: '/nhan-vien/quan-tri/danh-gia', roles: VAI_TRO.quanTri, mvc: '/Admin/Reviews' },

  { nhom: 'Hệ thống', ten: 'Nhân sự', path: '/nhan-vien/quan-tri/nhan-su', roles: VAI_TRO.quanTri, mvc: '/Admin/Staff' },
  { nhom: 'Hệ thống', ten: 'Cấu hình khách sạn', path: '/nhan-vien/quan-tri/cau-hinh', roles: VAI_TRO.quanTri, mvc: '/Admin/Settings' },
  { nhom: 'Hệ thống', ten: 'Nhật ký thao tác', path: '/nhan-vien/quan-tri/nhat-ky', roles: VAI_TRO.quanTri, mvc: '/Admin/AuditLog' }
];

// Menu theo vai trò, gom nhóm giữ nguyên thứ tự (nhóm '' = mục riêng của vai trò, đứng đầu)
export function menuCho(role: StaffRole): { nhom: string; muc: MucMenu[] }[] {
  const nhoms: { nhom: string; muc: MucMenu[] }[] = [];
  for (const m of MENU_NHAN_VIEN.filter((x) => x.roles.includes(role))) {
    const g = nhoms.find((n) => n.nhom === m.nhom);
    if (g) g.muc.push(m);
    else nhoms.push({ nhom: m.nhom, muc: [m] });
  }
  return nhoms;
}

// Mỗi đường dẫn một trang: quyền = hợp các vai trò của mọi mục menu trỏ tới đường dẫn đó
export function cacTrangNhanVien(): { path: string; ten: string; roles: StaffRole[]; mvc: string }[] {
  const map = new Map<string, { path: string; ten: string; roles: StaffRole[]; mvc: string }>();
  for (const m of MENU_NHAN_VIEN) {
    const them = [...m.roles, ...(m.quyenThem ?? [])];
    const t = map.get(m.path);
    if (t) t.roles = [...new Set([...t.roles, ...them])];
    else map.set(m.path, { path: m.path, ten: m.ten, roles: [...new Set(them)], mvc: m.mvc });
  }
  return [...map.values()];
}

// Trang mặc định sau khi đăng nhập (giống Auth/Portal của Web MVC)
export function trangChuCua(role?: Role | null): string {
  switch (role) {
    case 'Customer': return '/phieu';
    case 'Receptionist': return '/nhan-vien/le-tan';
    case 'Housekeeping': return '/nhan-vien/buong-phong/viec-cua-toi';
    case 'Accountant': return '/nhan-vien/ke-toan';
    case 'Admin': return '/nhan-vien/quan-tri';
    default: return '/';
  }
}

export const coQuyen = (role: Role | undefined | null, roles: readonly Role[]) => !!role && roles.includes(role);
