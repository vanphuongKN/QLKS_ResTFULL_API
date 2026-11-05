import { describe, expect, it } from 'vitest';
import { cacTrangNhanVien } from '../auth/roles';
import { TRANG_MENU } from '../staff/StaffApp';
import { duongDanMoi } from './links';

describe('duongDanMoi: đường dẫn cũ của cổng MVC → trang React', () => {
  it('trang khách và đăng nhập', () => {
    expect(duongDanMoi('/Customer/BookRoom', '?maLoai=LP01&soNguoi=2')).toBe('/dat-phong?maLoai=LP01&soNguoi=2');
    expect(duongDanMoi('/Home/RoomType/LP03')).toBe('/hang-phong/LP03');
    expect(duongDanMoi('/auth/login')).toBe('/dang-nhap');
  });
  it('mục menu nhân viên theo trường mvc, không phân biệt hoa thường, giữ tham số', () => {
    expect(duongDanMoi('/Reception')).toBe('/nhan-vien/le-tan');
    expect(duongDanMoi('/admin/pricing/')).toBe('/nhan-vien/quan-tri/bang-gia');
    expect(duongDanMoi('/Service', '?maPhong=P201')).toBe('/nhan-vien/dich-vu?maPhong=P201');
    expect(duongDanMoi('/Accounting/Invoices', '?trangThai=Unpaid')).toBe('/nhan-vien/ke-toan/hoa-don?trangThai=Unpaid');
  });
  it('trang chi tiết', () => {
    expect(duongDanMoi('/Checkout/Process', '?maPhong=P302')).toBe('/nhan-vien/tra-phong?maPhong=P302');
    expect(duongDanMoi('/Reception/CustomerDetails/KH04')).toBe('/nhan-vien/khach-hang/KH04');
    expect(duongDanMoi('/Accounting/Invoice/HD03')).toBe('/nhan-vien/ke-toan/hoa-don/HD03');
    expect(duongDanMoi('/Invoice/Print/HD03')).toBe('/in/hoa-don/HD03');
    expect(duongDanMoi('/NightAudit/Detail', '?ngay=2026-11-03')).toBe('/nhan-vien/in/chot-ngay/2026-11-03');
    expect(duongDanMoi('/Admin/RoomTypeImages/LP02')).toBe('/nhan-vien/quan-tri/loai-phong/LP02/anh');
    expect(duongDanMoi('/Reception/ChangeRoom/DP07')).toBe('/nhan-vien/le-tan/doi-phong/DP07');
  });
  it('đường dẫn lạ → null (trang không tìm thấy)', () => {
    expect(duongDanMoi('/khong-co')).toBeNull();
    expect(duongDanMoi('/Admin/KhongCo')).toBeNull();
  });
  it('mọi mục menu đều có trang React và có đường dẫn MVC cũ chuyển về đúng trang', () => {
    for (const t of cacTrangNhanVien()) {
      expect(TRANG_MENU[t.path], t.path).toBeTruthy();
      expect(duongDanMoi(t.mvc)).toBe(t.path);
    }
  });
});
