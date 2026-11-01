import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError, setAccessToken, setUnauthorizedHandler } from './client';

const json = (status: number, body: unknown, type = 'application/json') =>
  new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': type } });

function mockFetch(...responses: (Response | Error)[]) {
  const fn = vi.fn(async () => {
    const r = responses.shift();
    if (r instanceof Error) throw r;
    return r!;
  });
  vi.stubGlobal('fetch', fn);
  return fn;
}

afterEach(() => {
  vi.unstubAllGlobals();
  setAccessToken(null);
  setUnauthorizedHandler(() => {});
});

describe('gọi API (cùng nguồn, qua Gateway)', () => {
  it('GET đúng đường dẫn, tham số tìm phòng mã hoá trong query', async () => {
    const fetch = mockFetch(json(200, []));
    await api.available('2026-11-20', '2026-11-22', 2);
    expect(fetch).toHaveBeenCalledWith('/api/room-types/available?checkIn=2026-11-20&checkOut=2026-11-22&soNguoi=2', expect.objectContaining({ method: 'GET' }));
  });

  it('đã đăng nhập thì gửi Bearer token; POST gửi JSON PascalCase', async () => {
    const fetch = mockFetch(json(204, undefined));
    setAccessToken('abc');
    await api.cancel('PDP0000001', 'Đổi lịch');
    const [url, init] = fetch.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toBe('/api/bookings/PDP0000001/cancel');
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer abc');
    expect(JSON.parse(init.body as string)).toEqual({ LyDo: 'Đổi lịch' });
  });

  it('200 trả dữ liệu JSON', async () => {
    mockFetch(json(200, { Ten: 'LUXE' }));
    expect(await api.hotel()).toEqual({ Ten: 'LUXE' });
  });

  it('lỗi kiểm tra dữ liệu (ProblemDetails errors) gộp thành thông báo tiếng Việt', async () => {
    mockFetch(json(400, { title: 'One or more validation errors occurred.', errors: { SoDienThoai: ['Số điện thoại không hợp lệ.'], Email: ['Email không hợp lệ.'] } }, 'application/problem+json'));
    await expect(api.me()).rejects.toMatchObject({ status: 400, message: 'Số điện thoại không hợp lệ. Email không hợp lệ.' });
  });

  it('lỗi nghiệp vụ dùng detail; 429 của Gateway có thông báo riêng', async () => {
    mockFetch(json(409, { detail: 'Phiếu đã nhận phòng, không thể huỷ.' }), new Response('', { status: 429 }));
    await expect(api.cancel('DP1', 'x')).rejects.toMatchObject({ status: 409, message: 'Phiếu đã nhận phòng, không thể huỷ.' });
    await expect(api.hotel()).rejects.toMatchObject({ status: 429, message: 'Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.' });
  });

  it('401 khi đang có token → gọi xử lý đăng xuất; chưa đăng nhập thì không', async () => {
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    mockFetch(new Response('', { status: 401 }), new Response('', { status: 401 }));
    await expect(api.me()).rejects.toMatchObject({ status: 401, message: 'Vui lòng đăng nhập.' });
    expect(onUnauthorized).not.toHaveBeenCalled();
    setAccessToken('het-han');
    await expect(api.me()).rejects.toBeInstanceOf(ApiError);
    expect(onUnauthorized).toHaveBeenCalledOnce();
  });

  it('mất mạng → ApiError status 0', async () => {
    mockFetch(new TypeError('Failed to fetch'));
    await expect(api.hotel()).rejects.toMatchObject({ status: 0, message: 'Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.' });
  });
});
