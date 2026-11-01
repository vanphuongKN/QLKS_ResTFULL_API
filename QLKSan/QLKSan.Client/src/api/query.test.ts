import { describe, expect, it } from 'vitest';
import { ApiError } from './client';
import { nenThuLai, thongBaoLoi } from './query';

describe('tầng dữ liệu (TanStack Query)', () => {
  it('lỗi 4xx không thử lại; lỗi mạng / 5xx thử lại đúng 1 lần', () => {
    expect(nenThuLai(0, new ApiError(400, 'x'))).toBe(false);
    expect(nenThuLai(0, new ApiError(403, 'x'))).toBe(false);
    expect(nenThuLai(0, new ApiError(404, 'x'))).toBe(false);
    expect(nenThuLai(0, new ApiError(0, 'mất mạng'))).toBe(true);
    expect(nenThuLai(0, new ApiError(502, 'x'))).toBe(true);
    expect(nenThuLai(1, new ApiError(502, 'x'))).toBe(false);
  });

  it('thông báo lỗi lấy từ ApiError, lỗi lạ có câu mặc định', () => {
    expect(thongBaoLoi(new ApiError(409, 'Phiếu đã nhận phòng.'))).toBe('Phiếu đã nhận phòng.');
    expect(thongBaoLoi('???')).toBe('Có lỗi xảy ra, vui lòng thử lại.');
  });
});
