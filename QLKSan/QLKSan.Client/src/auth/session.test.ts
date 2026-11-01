import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { LoginResponse } from '../api/client';
import { load, save, toSession, type Session } from './session';

const login = (r: Partial<LoginResponse>) => ({ TenDangNhap: 'khach1', HoTen: 'Võ Văn A', ...r }) as LoginResponse;

describe('phiên đăng nhập của khách', () => {
  beforeEach(() => {
    const store = new Map<string, string>();
    vi.stubGlobal('sessionStorage', {
      getItem: (k: string) => store.get(k) ?? null,
      setItem: (k: string, v: string) => void store.set(k, v),
      removeItem: (k: string) => void store.delete(k)
    });
  });
  afterEach(() => vi.unstubAllGlobals());

  it('chỉ nhận tài khoản khách', () => {
    const s = toSession(login({ VaiTro: 'Customer', AccessToken: 't', MaKhachHang: 'KH01', ExpiresAt: '2030-01-01T00:00:00' }));
    expect(s).toEqual({ token: 't', expiresAt: '2030-01-01T00:00:00', hoTen: 'Võ Văn A', tenDangNhap: 'khach1', maKhachHang: 'KH01' });
  });

  it('tài khoản nhân viên bị từ chối, hướng dẫn dùng cổng nhân viên', () => {
    expect(() => toSession(login({ VaiTro: 'Receptionist', AccessToken: 't' }))).toThrow('cổng nhân viên');
    expect(() => toSession(login({ VaiTro: 'Customer', AccessToken: 't' }))).toThrow();
  });

  it('lưu rồi đọc lại; phiên hết hạn coi như chưa đăng nhập; đăng xuất xoá phiên', () => {
    const s: Session = { token: 't', expiresAt: new Date(Date.now() + 60_000).toISOString(), hoTen: 'A', tenDangNhap: 'a', maKhachHang: 'KH01' };
    save(s);
    expect(load()).toEqual(s);
    save({ ...s, expiresAt: new Date(Date.now() - 1000).toISOString() });
    expect(load()).toBeNull();
    save(s);
    save(null);
    expect(load()).toBeNull();
  });

  it('trình duyệt chặn lưu trữ / dữ liệu hỏng → không lỗi', () => {
    vi.stubGlobal('sessionStorage', {
      getItem: () => '{hỏng',
      setItem: () => { throw new Error('QuotaExceeded'); },
      removeItem: () => {}
    });
    expect(load()).toBeNull();
    expect(() => save({ token: 't', expiresAt: '', hoTen: '', tenDangNhap: '', maKhachHang: '' })).not.toThrow();
  });
});
