import { describe, expect, it } from 'vitest';
import { phanTramThayDoi } from './AccountingPages';
import { congNgayChot } from './NightAuditPages';
import { so1 } from '../../utils/format';

describe('phanTramThayDoi (Delta của Accounting/Index)', () => {
  it('làm tròn 1 chữ số, âm khi giảm, null khi kỳ trước bằng 0', () => {
    expect(phanTramThayDoi(1890000, 1684800)).toBe(12.2);
    expect(phanTramThayDoi(800000, 1200000)).toBe(-33.3);
    expect(phanTramThayDoi(5, 0)).toBeNull();
    expect(phanTramThayDoi(undefined, 10)).toBe(-100);
  });
});

describe('congNgayChot (dòng "Cộng N ngày" của NightAudit/Index)', () => {
  it('công suất / ADR / RevPAR tính lại từ tổng, không cộng trung bình', () => {
    const t = congNgayChot([
      { SoLieu: { DemPhongBan: 1, TongSoPhong: 9, TienPhong: 960000, DoanhThuKhac: 0, TienDichVu: 0, KhachDen: 1, KhachDi: 0, KhachKhongDen: 0 } },
      { SoLieu: { DemPhongBan: 2, TongSoPhong: 9, TienPhong: 2000000, DoanhThuKhac: 300000, TienDichVu: 250000, KhachDen: 1, KhachDi: 1, KhachKhongDen: 1 } }
    ]);
    expect(t.dem).toBe(3);
    expect(t.tienPhong).toBe(2960000);
    expect(so1(t.congSuat)).toBe('16,7');
    expect(t.adr).toBe(986667);
    expect(t.revpar).toBe(164444);
    expect([t.khac, t.dichVu, t.den, t.di, t.khongDen]).toEqual([300000, 250000, 2, 1, 1]);
  });
  it('không có ngày nào → 0, không chia cho 0', () => {
    expect(congNgayChot([])).toMatchObject({ dem: 0, congSuat: 0, adr: 0, revpar: 0 });
  });
});
