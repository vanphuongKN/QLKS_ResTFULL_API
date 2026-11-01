import { describe, expect, it } from 'vitest';
import { addDays, anh, isoDate, ngay, soDem, tien } from './format';
import { duongQuayLai, spaPath } from './links';

describe('định dạng', () => {
  it('tiền VND làm tròn, phân tách nghìn kiểu Việt Nam', () => {
    expect(tien(1500000)).toBe('1.500.000 ₫');
    expect(tien(999.6)).toBe('1.000 ₫');
    expect(tien(null)).toBe('0 ₫');
  });

  it('ngày yyyy-MM-dd theo giờ máy, cộng ngày qua cuối tháng / cuối năm', () => {
    expect(isoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
    expect(addDays('2026-11-30', 1)).toBe('2026-12-01');
    expect(addDays('2026-12-31', 2)).toBe('2027-01-02');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('hiển thị ngày dd/MM/yyyy (cả chuỗi có giờ), số đêm', () => {
    expect(ngay('2026-11-20T14:00:00')).toBe('20/11/2026');
    expect(ngay(null)).toBe('');
    expect(soDem('2026-11-20', '2026-11-22')).toBe(2);
  });

  it('ảnh thiếu thì dùng ảnh mẫu', () => {
    expect(anh('/uploads/a.jpg')).toBe('/uploads/a.jpg');
    expect(anh(null)).toBe('/images/room_garden.png');
  });
});

describe('liên kết của trợ lý ảo', () => {
  it('trang đặt phòng MVC → trang đặt phòng SPA, giữ nguyên tham số điền sẵn', () => {
    expect(spaPath('https://localhost:7234/Customer/BookRoom?maLoai=LP01&checkIn=2026-11-20&checkOut=2026-11-22&soNguoi=2'))
      .toBe('/dat-phong?maLoai=LP01&checkIn=2026-11-20&checkOut=2026-11-22&soNguoi=2');
  });

  it('trang hạng phòng MVC → trang hạng phòng SPA', () => {
    expect(spaPath('http://web:8080/Home/RoomType/LP03')).toBe('/hang-phong/LP03');
  });

  it('liên kết khác (bản đồ) hoặc không phải URL tuyệt đối → null (mở ngoài)', () => {
    expect(spaPath('https://maps.google.com/?q=hotel')).toBeNull();
    expect(spaPath('/Customer/BookRoom')).toBeNull();
    expect(spaPath('https://localhost/Home/RoomType/LP01/them')).toBeNull();
  });
});

describe('đường quay lại sau đăng nhập', () => {
  it('nhận đường dẫn nội bộ', () => {
    expect(duongQuayLai('/dat-phong?maLoai=LP01')).toBe('/dat-phong?maLoai=LP01');
  });

  it('chặn chuyển hướng ra trang ngoài', () => {
    for (const v of ['https://evil.example', '//evil.example', '/\\evil.example', 'javascript:alert(1)', '', null, undefined])
      expect(duongQuayLai(v)).toBe('/');
  });
});
