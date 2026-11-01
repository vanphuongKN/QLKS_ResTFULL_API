import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';
import './ui.css';

type Loai = 'ok' | 'loi' | 'info';
interface ThongBao { id: number; loai: Loai; noiDung: string }
interface ToastApi {
  thanhCong: (noiDung: string) => void;
  loi: (noiDung: string) => void;
  info: (noiDung: string) => void;
}

const ToastContext = createContext<ToastApi | null>(null);
const THOI_GIAN: Record<Loai, number> = { ok: 4000, info: 5000, loi: 8000 };

// Thông báo nổi góc màn hình; vùng aria-live để trình đọc màn hình đọc kết quả thao tác
export function ToastProvider({ children }: { children: ReactNode }) {
  const [ds, setDs] = useState<ThongBao[]>([]);
  const idRef = useRef(0);

  const dong = useCallback((id: number) => setDs((x) => x.filter((t) => t.id !== id)), []);
  const them = useCallback((loai: Loai, noiDung: string) => {
    const id = ++idRef.current;
    setDs((x) => [...x.slice(-3), { id, loai, noiDung }]);
    window.setTimeout(() => dong(id), THOI_GIAN[loai]);
  }, [dong]);

  const api = useMemo<ToastApi>(() => ({
    thanhCong: (n) => them('ok', n),
    loi: (n) => them('loi', n),
    info: (n) => them('info', n)
  }), [them]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toast-vung" aria-live="polite" aria-atomic="false">
        {ds.map((t) => (
          <div key={t.id} className={`toast toast-${t.loai}`} role={t.loai === 'loi' ? 'alert' : 'status'}>
            <span>{t.noiDung}</span>
            <button type="button" className="toast-dong" aria-label="Đóng thông báo" onClick={() => dong(t.id)}>×</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast phải nằm trong ToastProvider');
  return ctx;
}
