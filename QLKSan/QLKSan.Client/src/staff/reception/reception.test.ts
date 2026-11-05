import { describe, expect, it } from 'vitest';
import { danhMucDichVu } from './ServicePage';

describe('danhMucDichVu (như Views/Service/Index của MVC)', () => {
  it('xếp danh mục theo từ khoá trong tên dịch vụ', () => {
    expect(danhMucDichVu('Bữa sáng tại phòng')).toBe('Ẩm thực');
    expect(danhMucDichVu('Rượu vang đỏ')).toBe('Ẩm thực');
    expect(danhMucDichVu('Massage chân')).toBe('Sức khỏe');
    expect(danhMucDichVu('Đưa đón sân bay')).toBe('Vận chuyển');
    expect(danhMucDichVu('Thuê xe máy')).toBe('Vận chuyển');
    expect(danhMucDichVu('Giặt ủi')).toBe('Giặt là');
    expect(danhMucDichVu('Trông trẻ')).toBe('Dịch vụ chung');
    expect(danhMucDichVu(null)).toBe('Dịch vụ chung');
  });
});
