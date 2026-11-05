import { describe, expect, it } from 'vitest';
import { chiaO } from './TimelinePage';
import { anhPhong, soDemLuuTru } from './RoomPages';

const ngays = ['2026-11-02', '2026-11-03', '2026-11-04', '2026-11-05', '2026-11-06', '2026-11-07', '2026-11-08'];
const phieu = (ma: string, tu: string, den: string) => ({ MaDatPhong: ma, TuNgay: `${tu}T00:00:00`, DenNgay: `${den}T00:00:00`, TrangThai: 'Approved' as const });

describe('lịch phòng: chia ô theo đêm (như Room/Timeline của MVC)', () => {
  it('phiếu chiếm từ đêm nhận tới trước ngày trả; ô trống còn lại giữ từng đêm', () => {
    const o = chiaO(ngays, [phieu('A', '2026-11-03', '2026-11-05')]);
    expect(o.map((x) => ('b' in x ? `${x.b.MaDatPhong}×${x.span}` : '.'))).toEqual(['.', 'A×2', '.', '.', '.', '.']);
  });

  it('phiếu bắt đầu trước khung xem → từ cột đầu; kéo quá khung → cắt ở cột cuối', () => {
    const o = chiaO(ngays, [phieu('A', '2026-10-30', '2026-11-04'), phieu('B', '2026-11-07', '2026-11-20')]);
    expect(o.map((x) => ('b' in x ? `${x.b.MaDatPhong}×${x.span}` : '.'))).toEqual(['A×2', '.', '.', '.', 'B×2']);
  });

  it('hai phiếu liền nhau (trả sáng, nhận chiều cùng ngày) không chồng nhau', () => {
    const o = chiaO(ngays, [phieu('A', '2026-11-02', '2026-11-04'), phieu('B', '2026-11-04', '2026-11-06')]);
    expect(o.map((x) => ('b' in x ? `${x.b.MaDatPhong}×${x.span}` : '.'))).toEqual(['A×2', 'B×2', '.', '.', '.']);
  });

  it('tổng số cột luôn bằng số đêm của khung xem', () => {
    const o = chiaO(ngays, [phieu('A', '2026-10-01', '2026-12-01')]);
    expect(o.reduce((t, x) => t + ('b' in x ? x.span : 1), 0)).toBe(7);
  });
});

describe('chi tiết phòng', () => {
  it('số đêm lưu trú làm tròn lên, tối thiểu 1', () => {
    expect(soDemLuuTru('2026-11-01T14:00:00', '2026-11-03T00:00:00')).toBe(2);
    expect(soDemLuuTru('2026-11-02T20:00:00', '2026-11-03T00:00:00')).toBe(1);
    expect(soDemLuuTru('2026-11-02T00:00:00', '2026-11-02T00:00:00')).toBe(1);
  });

  it('ảnh phòng: ảnh riêng, không có thì ảnh mẫu theo tên hạng', () => {
    expect(anhPhong({ HinhAnhs: [{ DuongDanAnh: '/uploads/p1.jpg' }] })).toBe('/uploads/p1.jpg');
    expect(anhPhong({ LoaiPhong: { TenLoai: 'Suite Hoàng Gia' } })).toBe('/images/room_presidential.png');
    expect(anhPhong({ LoaiPhong: { TenLoai: 'Phòng Deluxe Hướng Biển' } })).toBe('/images/room_ocean.png');
    expect(anhPhong({ LoaiPhong: { TenLoai: 'Phòng Tiêu Chuẩn' } })).toBe('/images/room_garden.png');
  });
});
