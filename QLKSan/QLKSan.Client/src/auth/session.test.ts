import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { LoginResponse } from '../api/client';
import { load, save, toSession, type Session } from './session';

const login = (r: Partial<LoginResponse>) => ({ TenDangNhap: 'khach1', HoTen: 'Võ Văn A', ...r }) as LoginResponse;
const phien = (r: Partial<Session> = {}): Session =>
  ({ token: 't', expiresAt: new Date(Date.now() + 60_000).toISOString(), hoTen: 'A', tenDangNhap: 'a', vaiTro: 'Customer', maKhachHang: 'KH01', ...r });

describe('phiên đăng nhập', () => {
  beforeEach(() => {
    const store = new Map<string, string>();
    vi.stubGlobal('sessionStorage', {
      getItem: (k: string) => store.get(k) ?? null,
      setItem: (k: string, v: string) => void store.set(k, v),
      removeItem: (k: string) => void store.delete(k)
    });
  });
  afterEach(() => vi.unstubAllGlobals());

  it('khách: giữ mã khách hàng và vai trò', () => {
    const s = toSession(login({ VaiTro: 'Customer', AccessToken: 't', MaKhachHang: 'KH01', ExpiresAt: '2030-01-01T00:00:00' }));
    expect(s).toEqual({ token: 't', expiresAt: '2030-01-01T00:00:00', hoTen: 'Võ Văn A', tenDangNhap: 'khach1', vaiTro: 'Customer', maKhachHang: 'KH01', maNhanVien: undefined });
  });

  it('nhân viên: được nhận, giữ mã nhân viên', () => {
    const s = toSession(login({ VaiTro: 'Receptionist', AccessToken: 't', MaNhanVien: 'NV02', TenDangNhap: 'letan1', HoTen: null }));
    expect(s.vaiTro).toBe('Receptionist');
    expect(s.maNhanVien).toBe('NV02');
    expect(s.hoTen).toBe('letan1');
  });

  it('phản hồi thiếu token / vai trò, hoặc khách chưa gắn hồ sơ → lỗi', () => {
    expect(() => toSession(login({ VaiTro: 'Admin' }))).toThrow();
    expect(() => toSession(login({ AccessToken: 't' }))).toThrow();
    expect(() => toSession(login({ VaiTro: 'Customer', AccessToken: 't' }))).toThrow('hồ sơ khách hàng');
  });

  it('lưu rồi đọc lại; phiên hết hạn coi như chưa đăng nhập; đăng xuất xoá phiên', () => {
    const s = phien();
    save(s);
    expect(load()).toEqual(s);
    save({ ...s, expiresAt: new Date(Date.now() - 1000).toISOString() });
    expect(load()).toBeNull();
    save(s);
    save(null);
    expect(load()).toBeNull();
  });

  it('phiên lưu từ bản cũ (chưa có vai trò) → coi như chưa đăng nhập', () => {
    const { vaiTro: _, ...cu } = phien();
    sessionStorage.setItem('qlksan.session', JSON.stringify(cu));
    expect(load()).toBeNull();
  });

  it('trình duyệt chặn lưu trữ / dữ liệu hỏng → không lỗi', () => {
    vi.stubGlobal('sessionStorage', {
      getItem: () => '{hỏng',
      setItem: () => { throw new Error('QuotaExceeded'); },
      removeItem: () => {}
    });
    expect(load()).toBeNull();
    expect(() => save(phien())).not.toThrow();
  });
});
