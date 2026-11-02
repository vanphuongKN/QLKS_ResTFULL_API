import { describe, expect, it } from 'vitest';
import { cacTrangNhanVien, coQuyen, laNhanVien, MENU_NHAN_VIEN, menuCho, trangChuCua, type StaffRole } from './roles';

const ten = (role: StaffRole) => menuCho(role).flatMap((g) => g.muc.map((m) => (g.nhom ? `${g.nhom}/` : '') + m.ten));

describe('menu cổng nhân viên (khớp sidebar Views/Shared/_Layout.cshtml của MVC)', () => {
  it('buồng phòng: chỉ 3 mục riêng, không nhóm', () => {
    expect(ten('Housekeeping')).toEqual(['Việc của tôi', 'Ghi minibar', 'Kỹ thuật']);
  });

  it('lễ tân: toàn bộ nhóm Vận hành, không có Doanh thu / Danh mục / Hệ thống', () => {
    expect(ten('Receptionist')).toEqual([
      'Vận hành/Lễ tân', 'Vận hành/Phòng nghỉ', 'Vận hành/Lịch phòng', 'Vận hành/Buồng phòng', 'Vận hành/Kỹ thuật', 'Vận hành/Gọi dịch vụ',
      'Vận hành/Yêu cầu khách', 'Vận hành/Giao ca', 'Vận hành/Chốt ngày', 'Vận hành/Khách hàng', 'Vận hành/Sổ lưu trú'
    ]);
  });

  it('kế toán: phòng nghỉ, lịch phòng, chốt ngày, sổ lưu trú + 5 mục doanh thu (không có bảng giá, khuyến mãi)', () => {
    expect(ten('Accountant')).toEqual([
      'Vận hành/Phòng nghỉ', 'Vận hành/Lịch phòng', 'Vận hành/Chốt ngày', 'Vận hành/Sổ lưu trú',
      'Doanh thu/Tổng quan kế toán', 'Doanh thu/Hoá đơn', 'Doanh thu/Đối soát', 'Doanh thu/Hoá đơn VAT công ty', 'Doanh thu/Báo cáo'
    ]);
  });

  it('quản trị: bảng điều khiển đứng đầu, đủ 4 nhóm, 27 mục', () => {
    const nhoms = menuCho('Admin');
    expect(nhoms.map((g) => g.nhom)).toEqual(['', 'Vận hành', 'Doanh thu', 'Danh mục', 'Hệ thống']);
    expect(nhoms[0].muc.map((m) => m.ten)).toEqual(['Bảng điều khiển']);
    expect(nhoms.flatMap((g) => g.muc)).toHaveLength(MENU_NHAN_VIEN.filter((m) => m.roles.includes('Admin')).length);
  });

  it('mỗi đường dẫn một trang; trang dùng chung cộng quyền (Kỹ thuật: buồng phòng + lễ tân + quản trị)', () => {
    const trang = cacTrangNhanVien();
    expect(new Set(trang.map((t) => t.path)).size).toBe(trang.length);
    expect(trang.find((t) => t.path === '/nhan-vien/ky-thuat')!.roles.sort()).toEqual(['Admin', 'Housekeeping', 'Receptionist']);
    expect(trang.every((t) => t.path.startsWith('/nhan-vien/') && t.mvc.startsWith('/'))).toBe(true);
  });

  it('quyền vào trang rộng hơn menu đúng như [Authorize] của MVC (Việc của tôi: + quản trị; Ghi minibar: + lễ tân, quản trị)', () => {
    const q = (p: string) => cacTrangNhanVien().find((t) => t.path === p)!.roles.sort();
    expect(q('/nhan-vien/buong-phong/viec-cua-toi')).toEqual(['Admin', 'Housekeeping']);
    expect(q('/nhan-vien/buong-phong/minibar')).toEqual(['Admin', 'Housekeeping', 'Receptionist']);
    expect(q('/nhan-vien/buong-phong')).toEqual(['Admin', 'Receptionist']);
    expect(ten('Admin')).not.toContain('Việc của tôi');
  });
});

describe('vai trò', () => {
  it('trang làm việc sau đăng nhập giống Auth/Portal của MVC', () => {
    expect(trangChuCua('Customer')).toBe('/phieu');
    expect(trangChuCua('Receptionist')).toBe('/nhan-vien/le-tan');
    expect(trangChuCua('Housekeeping')).toBe('/nhan-vien/buong-phong/viec-cua-toi');
    expect(trangChuCua('Accountant')).toBe('/nhan-vien/ke-toan');
    expect(trangChuCua('Admin')).toBe('/nhan-vien/quan-tri');
    expect(trangChuCua(null)).toBe('/');
  });

  it('trang làm việc của mỗi vai trò nằm trong menu của chính vai trò đó', () => {
    for (const r of ['Admin', 'Receptionist', 'Housekeeping', 'Accountant'] as StaffRole[])
      expect(menuCho(r).flatMap((g) => g.muc).some((m) => m.path === trangChuCua(r))).toBe(true);
  });

  it('kiểm tra quyền', () => {
    expect(laNhanVien('Customer')).toBe(false);
    expect(laNhanVien('Accountant')).toBe(true);
    expect(laNhanVien(undefined)).toBe(false);
    expect(coQuyen('Housekeeping', ['Admin', 'Receptionist'])).toBe(false);
    expect(coQuyen(undefined, ['Customer'])).toBe(false);
  });
});
