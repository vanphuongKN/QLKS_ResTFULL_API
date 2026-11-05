import { describe, expect, it } from 'vitest';
import { rutGon } from './AdminPages';
import { trangThaiKm } from './RevenuePages';

describe('rutGon (Rut() của Admin/Index)', () => {
  it('rút gọn tỷ / triệu / nghìn', () => {
    expect(rutGon(3_450_000_000)).toBe('3,45 tỷ');
    expect(rutGon(17_190_800)).toBe('17,2 tr');
    expect(rutGon(800_000)).toBe('800k');
    expect(rutGon(950)).toBe('950');
  });
});

describe('trangThaiKm (thứ tự: tắt > ngày > hết lượt)', () => {
  const hn = '2026-11-05';
  const km = { KichHoat: true, NgayBatDau: '2026-11-01T00:00:00', NgayKetThuc: '2026-11-30T00:00:00', SoLuotDaDung: 2, SoLuotToiDa: 10 };
  it('đang áp dụng / sắp diễn ra / đã kết thúc / hết lượt / đã tắt', () => {
    expect(trangThaiKm(km, hn).nhan).toBe('Đang áp dụng');
    expect(trangThaiKm({ ...km, NgayBatDau: '2026-11-06T00:00:00' }, hn).nhan).toBe('Sắp diễn ra');
    expect(trangThaiKm({ ...km, NgayKetThuc: '2026-11-04T00:00:00' }, hn).nhan).toBe('Đã kết thúc');
    expect(trangThaiKm({ ...km, SoLuotDaDung: 10 }, hn).nhan).toBe('Hết lượt');
    expect(trangThaiKm({ ...km, KichHoat: false, SoLuotDaDung: 10 }, hn)).toMatchObject({ nhom: 'tat', nhan: 'Đã tắt' });
  });
  it('ngày kết thúc là hôm nay vẫn đang áp dụng', () => {
    expect(trangThaiKm({ ...km, NgayKetThuc: '2026-11-05T00:00:00' }, hn).nhom).toBe('dang');
  });
});
