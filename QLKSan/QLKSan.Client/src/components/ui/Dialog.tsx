import { createContext, useCallback, useContext, useEffect, useId, useRef, useState, type FormEvent, type ReactNode } from 'react';
import './ui.css';

interface ModalProps {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  rong?: boolean;
}

// Hộp thoại dùng <dialog> gốc: trình duyệt lo khoá focus, phím Esc và lớp phủ phía sau
export function Modal({ open, title, onClose, children, footer, rong }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const tieuDeId = useId();

  useEffect(() => {
    const d = ref.current;
    if (!d) return;
    if (open && !d.open) d.showModal();
    if (!open && d.open) d.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      className={`modal${rong ? ' modal-rong' : ''}`}
      aria-labelledby={tieuDeId}
      onCancel={(e) => { e.preventDefault(); onClose(); }}
      onClick={(e) => { if (e.target === ref.current) onClose(); }}
    >
      {open && (
        <div className="modal-khung">
          <div className="modal-dau">
            <h2 id={tieuDeId}>{title}</h2>
            <button type="button" className="modal-dong" aria-label="Đóng" onClick={onClose}>×</button>
          </div>
          <div className="modal-than">{children}</div>
          {footer && <div className="modal-chan">{footer}</div>}
        </div>
      )}
    </dialog>
  );
}

export interface XacNhanOptions {
  tieuDe: string;
  noiDung?: ReactNode;
  nutXacNhan?: string;
  nguyHiem?: boolean;
  // Có thì hiện ô nhập lý do (bắt buộc nếu lyDoBatBuoc)
  nhanLyDo?: string;
  lyDoBatBuoc?: boolean;
}
type KetQua = { lyDo: string } | null;

const XacNhanContext = createContext<((o: XacNhanOptions) => Promise<KetQua>) | null>(null);

export function XacNhanProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ o: XacNhanOptions; resolve: (k: KetQua) => void } | null>(null);
  const [lyDo, setLyDo] = useState('');

  const hoi = useCallback((o: XacNhanOptions) => new Promise<KetQua>((resolve) => {
    setLyDo('');
    setState({ o, resolve });
  }), []);

  const xong = (k: KetQua) => {
    state?.resolve(k);
    setState(null);
  };
  const gui = (e: FormEvent) => {
    e.preventDefault();
    if (state?.o.lyDoBatBuoc && !lyDo.trim()) return;
    xong({ lyDo: lyDo.trim() });
  };
  const o = state?.o;

  return (
    <XacNhanContext.Provider value={hoi}>
      {children}
      <Modal open={!!state} title={o?.tieuDe ?? ''} onClose={() => xong(null)}>
        <form onSubmit={gui} className="xac-nhan">
          {o?.noiDung && <div className="xac-nhan-noi-dung">{o.noiDung}</div>}
          {o?.nhanLyDo && (
            <label className="field">
              <span>{o.nhanLyDo}{o.lyDoBatBuoc && ' *'}</span>
              <textarea value={lyDo} onChange={(e) => setLyDo(e.target.value)} rows={3} maxLength={500} required={o.lyDoBatBuoc} autoFocus />
            </label>
          )}
          <div className="modal-chan">
            <button type="button" className="btn btn-outline" onClick={() => xong(null)}>Quay lại</button>
            <button type="submit" className={`btn ${o?.nguyHiem ? 'btn-danger' : 'btn-primary'}`} autoFocus={!o?.nhanLyDo}>
              {o?.nutXacNhan ?? 'Xác nhận'}
            </button>
          </div>
        </form>
      </Modal>
    </XacNhanContext.Provider>
  );
}

// const ok = await xacNhan({ tieuDe: 'Huỷ phiếu?', nhanLyDo: 'Lý do', lyDoBatBuoc: true }); if (ok) ... ok.lyDo
export function useXacNhan() {
  const ctx = useContext(XacNhanContext);
  if (!ctx) throw new Error('useXacNhan phải nằm trong XacNhanProvider');
  return ctx;
}
