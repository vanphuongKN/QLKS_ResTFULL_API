import { useState, type FormEvent, type ReactNode } from 'react';
import { useSearchParams } from 'react-router-dom';
import { download } from '../../api/client';
import { addDays, homNay } from '../../utils/format';
import { thongBaoLoi } from '../../api/query';
import { useToast } from './Toast';
import './ui.css';

export function PageHeader({ tieuDe, phu, children }: { tieuDe: string; phu?: ReactNode; children?: ReactNode }) {
  return (
    <div className="trang-dau">
      <div>
        <h1>{tieuDe}</h1>
        {phu && <p className="muted">{phu}</p>}
      </div>
      {children && <div className="trang-dau-nut">{children}</div>}
    </div>
  );
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <div className="trong-rong">{children}</div>;
}

export function Field({ nhan, loi, children, goiY }: { nhan: string; loi?: string; goiY?: string; children: ReactNode }) {
  return (
    <label className="field">
      <span>{nhan}</span>
      {children}
      {goiY && !loi && <small className="muted">{goiY}</small>}
      {loi && <small className="field-loi" role="alert">{loi}</small>}
    </label>
  );
}

export interface KhoangNgay { tuNgay: string; denNgay: string }

// Khoảng ngày lưu trên URL (?tuNgay=&denNgay=) như trang MVC: tải lại / quay lại vẫn giữ bộ lọc, gửi link được
export function useKhoangNgay(macDinh: () => KhoangNgay): [KhoangNgay, (k: KhoangNgay) => void] {
  const [params, setParams] = useSearchParams();
  const md = macDinh();
  const ngayHopLe = (v: string | null) => (v && /^\d{4}-\d{2}-\d{2}$/.test(v) ? v : null);
  const gt = { tuNgay: ngayHopLe(params.get('tuNgay')) ?? md.tuNgay, denNgay: ngayHopLe(params.get('denNgay')) ?? md.denNgay };
  const dat = (k: KhoangNgay) => {
    const p = new URLSearchParams(params);
    p.set('tuNgay', k.tuNgay);
    p.set('denNgay', k.denNgay);
    setParams(p);
  };
  return [gt, dat];
}

export const dauThang = () => `${homNay().slice(0, 8)}01`;

export function BoLocNgay({ value, onChange }: { value: KhoangNgay; onChange: (k: KhoangNgay) => void }) {
  const [tu, setTu] = useState(value.tuNgay);
  const [den, setDen] = useState(value.denNgay);
  const sai = tu > den;
  const gui = (e: FormEvent) => {
    e.preventDefault();
    if (!sai) onChange({ tuNgay: tu, denNgay: den });
  };
  const chon = (k: KhoangNgay) => { setTu(k.tuNgay); setDen(k.denNgay); onChange(k); };
  return (
    <form className="bo-loc" onSubmit={gui} aria-label="Lọc theo khoảng ngày">
      <label>Từ ngày <input type="date" value={tu} onChange={(e) => setTu(e.target.value)} required /></label>
      <label>Đến ngày <input type="date" value={den} onChange={(e) => setDen(e.target.value)} required /></label>
      <button type="submit" className="btn btn-primary btn-sm" disabled={sai}>Xem</button>
      <span className="bo-loc-nhanh">
        <button type="button" className="link-btn" onClick={() => chon({ tuNgay: homNay(), denNgay: homNay() })}>Hôm nay</button>
        <button type="button" className="link-btn" onClick={() => chon({ tuNgay: addDays(homNay(), -6), denNgay: homNay() })}>7 ngày</button>
        <button type="button" className="link-btn" onClick={() => chon({ tuNgay: dauThang(), denNgay: homNay() })}>Tháng này</button>
      </span>
      {sai && <small className="field-loi" role="alert">Ngày bắt đầu phải trước ngày kết thúc.</small>}
    </form>
  );
}

// Nút tải tệp cần đăng nhập (Excel...): tải qua fetch kèm token rồi lưu, báo lỗi bằng thông báo nổi
export function NutTaiTep({ path, tenMacDinh, children }: { path: string; tenMacDinh: string; children: ReactNode }) {
  const toast = useToast();
  const [dang, setDang] = useState(false);
  const tai = async () => {
    setDang(true);
    try {
      toast.thanhCong(`Đã tải ${await download(path, tenMacDinh)}`);
    } catch (e) {
      toast.loi(thongBaoLoi(e));
    } finally {
      setDang(false);
    }
  };
  return <button type="button" className="btn btn-outline btn-sm" onClick={tai} disabled={dang}>{dang ? 'Đang tải…' : children}</button>;
}

// Khung trang in (hoá đơn, bản in chốt ngày): thanh công cụ ẩn khi in
export function TrangIn({ tieuDe, children, onQuayLai }: { tieuDe: string; children: ReactNode; onQuayLai?: () => void }) {
  return (
    <div className="trang-in">
      <div className="trang-in-cong-cu no-print">
        <button type="button" className="btn btn-outline btn-sm" onClick={onQuayLai ?? (() => history.back())}>← Quay lại</button>
        <span>{tieuDe}</span>
        <button type="button" className="btn btn-primary btn-sm" onClick={() => window.print()}>In</button>
      </div>
      <article className="giay-in">{children}</article>
    </div>
  );
}
