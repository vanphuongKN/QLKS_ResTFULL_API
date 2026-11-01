import { QueryClient, useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query';
import { ApiError } from './client';
import { useToast } from '../components/ui/Toast';

// Lỗi 4xx (dữ liệu sai, không có quyền, không tìm thấy) thử lại cũng vô ích; lỗi mạng / 5xx thử lại 1 lần
export function nenThuLai(soLan: number, loi: unknown) {
  if (loi instanceof ApiError && loi.status >= 400 && loi.status < 500) return false;
  return soLan < 1;
}

export function taoQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { staleTime: 30_000, retry: nenThuLai, refetchOnWindowFocus: true },
      mutations: { retry: false }
    }
  });
}

export const thongBaoLoi = (e: unknown) => (e instanceof Error ? e.message : 'Có lỗi xảy ra, vui lòng thử lại.');

interface ThaoTacOptions<TBien, TKetQua> {
  mutationFn: (bien: TBien) => Promise<TKetQua>;
  // Dữ liệu cần tải lại sau khi thao tác thành công (vd ['bookings'] làm mới mọi query bắt đầu bằng 'bookings')
  lamMoi?: QueryKey[];
  thanhCong?: string | ((kq: TKetQua, bien: TBien) => string);
  onSuccess?: (kq: TKetQua, bien: TBien) => void;
}

// Thao tác ghi (POST/PUT/PATCH/DELETE): báo kết quả bằng thông báo nổi, tự làm mới dữ liệu liên quan
export function useThaoTac<TBien = void, TKetQua = unknown>(o: ThaoTacOptions<TBien, TKetQua>) {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation<TKetQua, Error, TBien>({
    mutationFn: o.mutationFn,
    onSuccess: async (kq, bien) => {
      await Promise.all((o.lamMoi ?? []).map((k) => qc.invalidateQueries({ queryKey: k })));
      const msg = typeof o.thanhCong === 'function' ? o.thanhCong(kq, bien) : o.thanhCong;
      if (msg) toast.thanhCong(msg);
      o.onSuccess?.(kq, bien);
    },
    onError: (e) => toast.loi(thongBaoLoi(e))
  });
}
