import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { catalogApi, type PricingRule, type PricingRuleForm, type PromotionAdmin, type PromotionForm, type ReviewAdmin, type RoomTypeAdmin } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { Modal, useXacNhan } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { homNay, ngay, so, tenHangPhong, tien } from '../../utils/format';
import './admin.css';

const TEN_THU = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
const THU_TU_THU = [1, 2, 3, 4, 5, 6, 0];
const so2 = (v?: number | null) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(v ?? 0);

// ===== Bảng giá theo mùa / cuối tuần / lễ (Admin/Pricing) =====
export function PricingPage() {
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['pricing-rules'], queryFn: catalogApi.pricingRules });
  const loai = useQuery({ queryKey: ['room-types'], queryFn: catalogApi.roomTypes });
  const [form, setForm] = useState<PricingRule | 'moi' | null>(null);
  const lamMoi = [['pricing-rules']];
  const bat = useThaoTac({ mutationFn: (r: PricingRule) => catalogApi.setPricingRuleActive(r.MaQuyTac!, !r.KichHoat), lamMoi, thanhCong: (_, r) => `Đã ${r.KichHoat ? 'tắt' : 'bật'} quy tắc ${r.MaQuyTac}.` });
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deletePricingRule(ma), lamMoi, thanhCong: (_, ma) => `Đã xoá quy tắc ${ma}.` });
  const hn = homNay();
  return (
    <>
      <PageHeader tieuDe="Bảng giá theo mùa / cuối tuần / lễ" phu="Mỗi đêm áp dụng 1 quy tắc khớp có độ ưu tiên cao nhất. Giá đêm = giá niêm yết × (100% + điều chỉnh). Hoá đơn đã lập không bị ảnh hưởng.">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setForm('moi')}>+ Thêm quy tắc</button>
      </PageHeader>
      <div className="qt-luoi-gia" aria-label="Giá niêm yết">
        {(loai.data ?? []).map((lp) => <div key={lp.MaLoai} className="the-trang qt-gia-the"><small className="muted">{lp.MaLoai}</small><strong>{lp.TenLoai}</strong><span className="chu-vang">{so(lp.GiaThueNgay)} ₫/đêm</span></div>)}
      </div>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : (
        <section className="the-trang the-bang">
          <div className="bang-cuon">
            <table className="bang" aria-label="Quy tắc giá">
              <thead><tr><th>Mã</th><th>Tên quy tắc</th><th>Loại phòng</th><th>Thời gian</th><th>Đêm áp dụng</th><th className="canh-phai">Điều chỉnh</th><th className="canh-giua">Ưu tiên</th><th>Trạng thái</th><th /></tr></thead>
              <tbody>
                {!q.data?.length && <tr><td colSpan={9} className="canh-giua muted">Chưa có quy tắc giá — mọi đêm tính theo giá niêm yết.</td></tr>}
                {(q.data ?? []).map((r) => {
                  const hetHan = !!r.DenNgay && r.DenNgay.slice(0, 10) < hn;
                  const pct = r.PhanTramDieuChinh ?? 0;
                  return (
                    <tr key={r.MaQuyTac} className={r.KichHoat && !hetHan ? '' : 'qt-mo'} data-quy-tac={r.MaQuyTac}>
                      <td>{r.MaQuyTac}</td>
                      <td><strong>{r.TenQuyTac}</strong></td>
                      <td>{r.TenLoai ?? 'Tất cả'}</td>
                      <td className="small nowrap">{!r.TuNgay && !r.DenNgay ? 'Quanh năm' : `${r.TuNgay ? ngay(r.TuNgay) : '…'} – ${r.DenNgay ? ngay(r.DenNgay) : '…'}`}{hetHan && <div className="chu-loi">Đã hết hạn</div>}</td>
                      <td>{r.CacThu?.length ? r.CacThu.map((t) => TEN_THU[t]).join(', ') : 'Mọi đêm'}</td>
                      <td className={`canh-phai nowrap ${pct >= 0 ? 'chu-loi' : 'chu-ok'}`}><strong>{pct > 0 ? '+' : ''}{so2(pct)}%</strong></td>
                      <td className="canh-giua">{r.UuTien}</td>
                      <td><span className={`badge ${r.KichHoat ? 'badge-ok' : 'badge-muted'}`}>{r.KichHoat ? 'Đang bật' : 'Đã tắt'}</span></td>
                      <td className="nowrap canh-phai">
                        <button type="button" className="btn btn-outline btn-sm" onClick={() => setForm(r)}>Sửa</button>{' '}
                        <button type="button" className="btn btn-outline btn-sm" disabled={bat.isPending} onClick={() => bat.mutate(r)}>{r.KichHoat ? 'Tắt' : 'Bật'}</button>{' '}
                        <button type="button" className="btn btn-outline-danger btn-sm" disabled={xoa.isPending}
                          onClick={async () => { if (await xacNhan({ tieuDe: `Xoá quy tắc ${r.MaQuyTac}?`, noiDung: r.TenQuyTac, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(r.MaQuyTac!); }}>Xoá</button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      )}
      {form && <HopQuyTac r={form === 'moi' ? null : form} loai={loai.data ?? []} onDong={() => setForm(null)} />}
    </>
  );
}

function HopQuyTac({ r, loai, onDong }: { r: PricingRule | null; loai: RoomTypeAdmin[]; onDong: () => void }) {
  const [f, setF] = useState({ TenQuyTac: r?.TenQuyTac ?? '', MaLoai: r?.MaLoai ?? '', TuNgay: r?.TuNgay?.slice(0, 10) ?? '', DenNgay: r?.DenNgay?.slice(0, 10) ?? '', PhanTramDieuChinh: r ? String(r.PhanTramDieuChinh ?? 0) : '', UuTien: String(r?.UuTien ?? 1) });
  const [thu, setThu] = useState<Set<number>>(() => new Set(r?.CacThu ?? []));
  const dat = (p: Partial<typeof f>) => setF({ ...f, ...p });
  const req: PricingRuleForm = {
    TenQuyTac: f.TenQuyTac.trim(), MaLoai: f.MaLoai || null, TuNgay: f.TuNgay || null, DenNgay: f.DenNgay || null,
    CacThu: THU_TU_THU.filter((t) => thu.has(t)), PhanTramDieuChinh: Number(f.PhanTramDieuChinh), UuTien: Number(f.UuTien)
  };
  const luu = useThaoTac({
    mutationFn: () => (r ? catalogApi.updatePricingRule(r.MaQuyTac!, req) : catalogApi.createPricingRule(req)), lamMoi: [['pricing-rules']],
    thanhCong: r ? `Đã cập nhật quy tắc ${r.MaQuyTac}.` : `Đã thêm quy tắc giá "${req.TenQuyTac}".`, onSuccess: onDong
  });
  const doiThu = (t: number) => setThu((s) => { const n = new Set(s); if (n.has(t)) n.delete(t); else n.add(t); return n; });
  return (
    <Modal open title={r ? `Sửa quy tắc ${r.MaQuyTac}` : 'Thêm quy tắc giá'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Quy tắc giá">
        <label className="field"><span>Tên quy tắc</span><input required maxLength={100} placeholder="VD: Tết Nguyên đán, Phụ thu cuối tuần" value={f.TenQuyTac} onChange={(e) => dat({ TenQuyTac: e.target.value })} /></label>
        <label className="field"><span>Loại phòng</span><select value={f.MaLoai} onChange={(e) => dat({ MaLoai: e.target.value })}><option value="">Tất cả loại phòng</option>{loai.map((x) => <option key={x.MaLoai} value={x.MaLoai!}>{x.TenLoai}</option>)}</select></label>
        <div className="form-grid">
          <label className="field"><span>Từ ngày</span><input type="date" value={f.TuNgay} onChange={(e) => dat({ TuNgay: e.target.value })} /></label>
          <label className="field"><span>Đến ngày</span><input type="date" value={f.DenNgay} onChange={(e) => dat({ DenNgay: e.target.value })} /></label>
        </div>
        <small className="muted">Để trống = áp dụng quanh năm.</small>
        <fieldset className="qt-thu"><legend className="small"><strong>Đêm trong tuần</strong></legend>
          {THU_TU_THU.map((t) => <label key={t} className="o-chon"><input type="checkbox" checked={thu.has(t)} onChange={() => doiThu(t)} /> {TEN_THU[t]}</label>)}
        </fieldset>
        <small className="muted">Đêm thứ Sáu (T6) và thứ Bảy (T7) là đêm cuối tuần. Không chọn = mọi đêm.</small>
        <div className="form-grid">
          <label className="field"><span>Điều chỉnh (%)</span><input type="number" min={-90} max={300} step={1} required placeholder="+20 hoặc -15" value={f.PhanTramDieuChinh} onChange={(e) => dat({ PhanTramDieuChinh: e.target.value })} /></label>
          <label className="field"><span>Độ ưu tiên</span><input type="number" min={1} max={100} required value={f.UuTien} onChange={(e) => dat({ UuTien: e.target.value })} /></label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu quy tắc</button></div>
      </form>
    </Modal>
  );
}

// ===== Khuyến mãi & ưu đãi (Admin/Promotions) =====
type Nhom = 'dang' | 'sap' | 'het' | 'tat';
// Trạng thái hiển thị: tắt thủ công > theo ngày > hết lượt (như TrangThai() của MVC)
export function trangThaiKm(p: PromotionAdmin, hn = homNay()): { nhom: Nhom; nhan: string; css: string } {
  if (!p.KichHoat) return { nhom: 'tat', nhan: 'Đã tắt', css: 'badge-muted' };
  if ((p.NgayKetThuc ?? '').slice(0, 10) < hn) return { nhom: 'het', nhan: 'Đã kết thúc', css: 'badge-muted' };
  if ((p.NgayBatDau ?? '').slice(0, 10) > hn) return { nhom: 'sap', nhan: 'Sắp diễn ra', css: 'badge-info' };
  if (p.SoLuotToiDa != null && (p.SoLuotDaDung ?? 0) >= p.SoLuotToiDa) return { nhom: 'het', nhan: 'Hết lượt', css: 'badge-muted' };
  return { nhom: 'dang', nhan: 'Đang áp dụng', css: 'badge-ok' };
}
const LOC_KM: [Nhom | '', string][] = [['', 'Tất cả'], ['dang', 'Đang áp dụng'], ['sap', 'Sắp diễn ra'], ['het', 'Đã kết thúc'], ['tat', 'Đã tắt']];

export function PromotionsPage() {
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['promotions'], queryFn: catalogApi.promotions });
  const loai = useQuery({ queryKey: ['room-types'], queryFn: catalogApi.roomTypes });
  const [loc, setLoc] = useState<Nhom | ''>('');
  const [form, setForm] = useState<PromotionAdmin | 'moi' | null>(null);
  const lamMoi = [['promotions'], ['dashboard']];
  const bat = useThaoTac({ mutationFn: (p: PromotionAdmin) => catalogApi.setPromotionActive(p.MaKhuyenMai!, !p.KichHoat), lamMoi, thanhCong: (_, p) => `Đã ${p.KichHoat ? 'tắt' : 'bật'} mã ${p.MaKhuyenMai}.` });
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deletePromotion(ma), lamMoi, thanhCong: (_, ma) => `Đã xoá mã ${ma}.` });
  const ds = q.data ?? [];
  const dem = (n: Nhom | '') => (n ? ds.filter((p) => trangThaiKm(p).nhom === n).length : ds.length);
  return (
    <>
      <PageHeader tieuDe="Khuyến mãi & ưu đãi" phu="Mã do khách nhập khi đặt phòng hoặc lễ tân nhập khi lập hoá đơn; hệ thống tự kiểm tra hạn, hạng phòng, số đêm và số lượt.">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setForm('moi')}>+ Tạo khuyến mãi</button>
      </PageHeader>
      <nav className="loc-chip" aria-label="Lọc khuyến mãi">
        {LOC_KM.map(([k, n]) => <button key={k} type="button" className={loc === k ? 'active' : ''} aria-pressed={loc === k} onClick={() => setLoc(k)}>{n} <span className="qt-dem">{dem(k)}</span></button>)}
      </nav>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : !ds.length ? <EmptyState>Chưa có chương trình khuyến mãi nào.</EmptyState> : (
        <div className="qt-luoi-the">
          {ds.filter((p) => !loc || trangThaiKm(p).nhom === loc).map((p) => {
            const tt = trangThaiKm(p);
            const pt = p.LoaiGiam === 'Phần trăm';
            const pctDung = p.SoLuotToiDa != null ? Math.min(100, Math.floor((p.SoLuotDaDung ?? 0) * 100 / Math.max(1, p.SoLuotToiDa))) : null;
            return (
              <article key={p.MaKhuyenMai} className={`the-trang qt-km-the${tt.nhom === 'tat' || tt.nhom === 'het' ? ' is-tat' : ''}`} data-khuyen-mai={p.MaKhuyenMai} data-nhom={tt.nhom}>
                <div className="the-dau"><span className="qt-ma-km">{p.MaKhuyenMai}</span><span className={`badge ${tt.css}`}>{tt.nhan}</span></div>
                <h3>{p.TenKhuyenMai}</h3>
                <p className="qt-km-gia">Giảm {pt ? `${so2(p.GiaTriGiam)}%` : tien(p.GiaTriGiam)}<small>{pt ? ' tiền phòng' : ''}</small></p>
                <ul className="qt-km-dk small">
                  <li>Thời gian: {ngay(p.NgayBatDau)} – {ngay(p.NgayKetThuc)}</li>
                  <li>Áp dụng: {p.TenLoai == null ? 'Mọi hạng phòng' : tenHangPhong(p.TenLoai)}</li>
                  {p.SoDemToiThieu != null && <li>Từ {p.SoDemToiThieu} đêm</li>}
                </ul>
                <div className="small">
                  <div className="kt-dong"><span>Đã dùng</span><strong>{p.SoLuotDaDung}{p.SoLuotToiDa != null ? ` / ${p.SoLuotToiDa}` : ''} lượt</strong></div>
                  {pctDung != null && <div className="kt-thanh" aria-hidden="true"><span style={{ width: `${pctDung}%` }} /></div>}
                </div>
                <div className="the-dau-nut qt-km-nut">
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => setForm(p)}>Sửa</button>
                  <button type="button" className="btn btn-outline btn-sm" disabled={bat.isPending} onClick={() => bat.mutate(p)}>{p.KichHoat ? 'Tắt' : 'Bật lại'}</button>
                  {!p.SoLuotDaDung && <button type="button" className="btn btn-outline-danger btn-sm" disabled={xoa.isPending}
                    onClick={async () => { if (await xacNhan({ tieuDe: `Xoá mã ${p.MaKhuyenMai}?`, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(p.MaKhuyenMai!); }}>Xoá</button>}
                </div>
              </article>
            );
          })}
        </div>
      )}
      {form && <HopKhuyenMai p={form === 'moi' ? null : form} loai={loai.data ?? []} onDong={() => setForm(null)} />}
    </>
  );
}

function HopKhuyenMai({ p, loai, onDong }: { p: PromotionAdmin | null; loai: RoomTypeAdmin[]; onDong: () => void }) {
  const [f, setF] = useState({
    MaKhuyenMai: p?.MaKhuyenMai ?? '', TenKhuyenMai: p?.TenKhuyenMai ?? '', LoaiGiam: p?.LoaiGiam ?? 'Phần trăm', GiaTriGiam: p ? String(p.GiaTriGiam ?? '') : '',
    MaLoai: p?.MaLoai ?? '', NgayBatDau: p?.NgayBatDau?.slice(0, 10) ?? '', NgayKetThuc: p?.NgayKetThuc?.slice(0, 10) ?? '',
    SoDemToiThieu: p?.SoDemToiThieu != null ? String(p.SoDemToiThieu) : '', SoLuotToiDa: p?.SoLuotToiDa != null ? String(p.SoLuotToiDa) : ''
  });
  const dat = (x: Partial<typeof f>) => setF({ ...f, ...x });
  const req: PromotionForm = {
    MaKhuyenMai: f.MaKhuyenMai.trim().toUpperCase(), TenKhuyenMai: f.TenKhuyenMai.trim(), LoaiGiam: f.LoaiGiam, GiaTriGiam: Number(f.GiaTriGiam),
    MaLoai: f.MaLoai || null, NgayBatDau: f.NgayBatDau, NgayKetThuc: f.NgayKetThuc,
    SoDemToiThieu: f.SoDemToiThieu ? Number(f.SoDemToiThieu) : null, SoLuotToiDa: f.SoLuotToiDa ? Number(f.SoLuotToiDa) : null
  };
  const luu = useThaoTac({
    mutationFn: () => (p ? catalogApi.updatePromotion(p.MaKhuyenMai!, { ...req, MaKhuyenMai: p.MaKhuyenMai! }) : catalogApi.createPromotion(req)), lamMoi: [['promotions'], ['dashboard']],
    thanhCong: p ? `Đã cập nhật khuyến mãi ${p.MaKhuyenMai}.` : 'Thêm khuyến mãi thành công!', onSuccess: onDong
  });
  return (
    <Modal open rong title={p ? `Sửa khuyến mãi ${p.MaKhuyenMai}` : 'Tạo khuyến mãi'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Khuyến mãi">
        <div className="form-grid">
          <label className="field"><span>Mã</span><input required maxLength={10} className="qt-hoa" placeholder="VD: SUMMER26" readOnly={!!p} value={f.MaKhuyenMai} onChange={(e) => dat({ MaKhuyenMai: e.target.value })} /></label>
          <label className="field"><span>Tên chương trình</span><input required maxLength={100} value={f.TenKhuyenMai} onChange={(e) => dat({ TenKhuyenMai: e.target.value })} /></label>
          <label className="field"><span>Loại giảm</span><select value={f.LoaiGiam} onChange={(e) => dat({ LoaiGiam: e.target.value })}><option value="Phần trăm">Phần trăm tiền phòng (%)</option><option value="Số tiền">Số tiền (₫)</option></select></label>
          <label className="field"><span>Giá trị giảm</span><input type="number" min={0} step="any" required value={f.GiaTriGiam} onChange={(e) => dat({ GiaTriGiam: e.target.value })} /></label>
          <label className="field span-2"><span>Hạng phòng áp dụng</span><select value={f.MaLoai} onChange={(e) => dat({ MaLoai: e.target.value })}><option value="">Mọi hạng phòng</option>{loai.map((x) => <option key={x.MaLoai} value={x.MaLoai!}>{tenHangPhong(x.TenLoai)}</option>)}</select></label>
          <label className="field"><span>Từ ngày</span><input type="date" required value={f.NgayBatDau} onChange={(e) => dat({ NgayBatDau: e.target.value })} /></label>
          <label className="field"><span>Đến ngày</span><input type="date" required value={f.NgayKetThuc} onChange={(e) => dat({ NgayKetThuc: e.target.value })} /></label>
          <label className="field"><span>Số đêm tối thiểu</span><input type="number" min={1} max={60} placeholder="Không giới hạn" value={f.SoDemToiThieu} onChange={(e) => dat({ SoDemToiThieu: e.target.value })} /></label>
          <label className="field"><span>Số lượt tối đa</span><input type="number" min={1} placeholder="Không giới hạn" value={f.SoLuotToiDa} onChange={(e) => dat({ SoLuotToiDa: e.target.value })} /></label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>{p ? 'Lưu thay đổi' : 'Tạo khuyến mãi'}</button></div>
      </form>
    </Modal>
  );
}

// ===== Đánh giá của khách (Admin/Reviews) =====
export function ReviewsAdminPage() {
  const [params] = useSearchParams();
  const soSao = /^[1-5]$/.test(params.get('soSao') ?? '') ? Number(params.get('soSao')) : null;
  const chuaPhanHoi = params.get('chuaPhanHoi') === 'true';
  const an = params.get('an') === 'true' ? true : params.get('an') === 'false' ? false : null;
  const macDinh = soSao == null && !chuaPhanHoi && an == null;
  const all = useQuery({ queryKey: ['reviews', 'admin'], queryFn: () => catalogApi.reviews() });
  const loc = useQuery({ queryKey: ['reviews', 'admin', soSao, chuaPhanHoi, an], queryFn: () => catalogApi.reviews(soSao, chuaPhanHoi, an), enabled: !macDinh });
  const ds = (macDinh ? all.data : loc.data) ?? [];
  const tat = all.data ?? [];
  const tb = tat.length ? tat.reduce((t, r) => t + (r.SoSao ?? 0), 0) / tat.length : 0;
  const daPh = tat.filter((r) => r.PhanHoi != null).length;
  const dangAn = tat.filter((r) => r.An).length;

  return (
    <>
      <PageHeader tieuDe="Đánh giá của khách" phu="Phản hồi hiển thị công khai dưới đánh giá trên trang hạng phòng. Đánh giá bị ẩn không hiện với khách nhưng vẫn tính vào điểm trung bình." />
      <ErrorBox message={all.error?.message ?? loc.error?.message} />
      {all.isLoading ? <Loading /> : (
        <>
          <div className="kpi-luoi qt-dg-kpi">
            <div className="kpi kpi-toi"><span>Điểm trung bình</span><strong data-dg="tb">{tat.length ? tb.toFixed(1).replace('.', ',') : '—'}<small> / 5</small></strong><small>{tat.length} đánh giá</small></div>
            <div className="kpi qt-phan-bo"><span>Phân bố số sao</span>
              {[5, 4, 3, 2, 1].map((s) => {
                const n = tat.filter((r) => r.SoSao === s).length;
                return (
                  <Link key={s} to={`?soSao=${s}`} className={`qt-sao-dong${soSao === s ? ' active' : ''}`} data-sao={s}>
                    <span>{s} ★</span><span className="kt-thanh"><span style={{ width: `${tat.length ? Math.floor(n * 100 / tat.length) : 0}%` }} /></span><span>{n}</span>
                  </Link>
                );
              })}
            </div>
            <div className="kpi"><span>Đã phản hồi</span><strong>{tat.length ? `${Math.floor(daPh * 100 / tat.length)}%` : '—'}</strong><small>{tat.length - daPh} chưa phản hồi · {dangAn} đang ẩn</small></div>
          </div>
          <nav className="loc-chip" aria-label="Lọc đánh giá">
            <Link to="/nhan-vien/quan-tri/danh-gia" className={macDinh ? 'active' : ''}>Tất cả <span className="qt-dem">{tat.length}</span></Link>
            <Link to="?chuaPhanHoi=true" className={chuaPhanHoi ? 'active' : ''}>Chưa phản hồi <span className="qt-dem">{tat.length - daPh}</span></Link>
            <Link to="?an=true" className={an === true ? 'active' : ''}>Đang ẩn <span className="qt-dem">{dangAn}</span></Link>
            {soSao != null && <Link to={`?soSao=${soSao}`} className="active">{soSao} sao <span className="qt-dem">{ds.length}</span></Link>}
          </nav>
          {!macDinh && loc.isLoading ? <Loading /> : !ds.length ? <EmptyState>Không có đánh giá phù hợp bộ lọc.</EmptyState> : ds.map((r) => <TheDanhGia key={r.MaDanhGia} r={r} />)}
        </>
      )}
    </>
  );
}

function TheDanhGia({ r }: { r: ReviewAdmin }) {
  const xacNhan = useXacNhan();
  const [mo, setMo] = useState(false);
  const [nd, setNd] = useState(r.PhanHoi ?? '');
  const lamMoi = [['reviews'], ['dashboard']];
  const ph = useThaoTac({ mutationFn: () => catalogApi.replyReview(r.MaDanhGia!, nd.trim()), lamMoi, thanhCong: 'Đã gửi phản hồi — phản hồi hiển thị công khai trên trang hạng phòng.', onSuccess: () => setMo(false) });
  const go = useThaoTac({ mutationFn: () => catalogApi.deleteReply(r.MaDanhGia!), lamMoi, thanhCong: 'Đã gỡ phản hồi.', onSuccess: () => setNd('') });
  const an = useThaoTac({ mutationFn: () => catalogApi.setReviewHidden(r.MaDanhGia!, !r.An), lamMoi, thanhCong: r.An ? 'Đã hiện lại đánh giá.' : 'Đã ẩn đánh giá khỏi trang khách (vẫn tính vào điểm trung bình).' });
  return (
    <article className={`the-trang qt-dg-the${r.An ? ' is-an' : ''}${(r.SoSao ?? 0) <= 3 ? ' is-thap' : ''}`} data-danh-gia={r.MaDanhGia}>
      <div className="the-dau">
        <span>
          <span className="qt-sao" aria-label={`${r.SoSao} sao`}>{'★'.repeat(r.SoSao ?? 0)}<span className="muted">{'★'.repeat(5 - (r.SoSao ?? 0))}</span></span>
          <strong> {r.HoTenKhachHang}</strong>
          {r.AnDanh && <> <span className="badge badge-muted" title="Khách chọn ẩn danh: trang khách hiển thị 'Khách ẩn danh'">gửi ẩn danh</span></>}
          {r.An && <> <span className="badge badge-muted">Đang ẩn</span></>}
        </span>
        <small className="muted">{ngay(r.NgayDanhGia)} · {tenHangPhong(r.TenLoaiPhong)} (P{r.SoPhong}){r.MaDatPhong && ` · Phiếu ${r.MaDatPhong}`} · <Link to={`/nhan-vien/khach-hang/${r.MaKhachHang}`}>Hồ sơ khách</Link></small>
      </div>
      <p>{r.NoiDung || '(Không có nhận xét)'}</p>
      {r.PhanHoi != null && (
        <div className="qt-phan-hoi">
          <div className="the-dau"><strong className="small">↩ Phản hồi của khách sạn</strong><small className="muted">{r.TenNhanVienPhanHoi} · {ngay(r.NgayPhanHoi)}</small></div>
          <p className="small">{r.PhanHoi}</p>
        </div>
      )}
      <div className="the-dau-nut">
        <button type="button" className="btn btn-outline btn-sm" aria-expanded={mo} onClick={() => setMo(!mo)}>{r.PhanHoi == null ? 'Phản hồi' : 'Sửa phản hồi'}</button>
        {r.PhanHoi != null && <button type="button" className="btn btn-outline btn-sm" disabled={go.isPending} onClick={async () => { if (await xacNhan({ tieuDe: 'Gỡ phản hồi này?', nutXacNhan: 'Gỡ', nguyHiem: true })) go.mutate(); }}>Gỡ phản hồi</button>}
        <button type="button" className={`btn btn-sm ${r.An ? 'btn-outline' : 'btn-outline-danger'} qt-day-phai`} disabled={an.isPending} onClick={() => an.mutate()}>{r.An ? 'Hiện lại' : 'Ẩn khỏi trang khách'}</button>
      </div>
      {mo && (
        <form className="qt-tra-loi" onSubmit={(e) => { e.preventDefault(); ph.mutate(); }} aria-label={`Phản hồi đánh giá ${r.MaDanhGia}`}>
          <textarea rows={3} maxLength={1000} required placeholder="Cảm ơn quý khách đã chia sẻ…" value={nd} onChange={(e) => setNd(e.target.value)} autoFocus />
          <button type="submit" className="btn btn-gold btn-sm" disabled={ph.isPending}>Gửi phản hồi</button>
        </form>
      )}
    </article>
  );
}
