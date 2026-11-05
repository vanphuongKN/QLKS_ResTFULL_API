import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { nightAuditApi, type DayClose, type NightAuditFigures, type NightAuditPreview } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { useAuth } from '../../auth/AuthContext';
import { ErrorBox, Loading } from '../../components/common';
import { Modal, useXacNhan } from '../../components/ui/Dialog';
import { PageHeader, TrangIn } from '../../components/ui/Page';
import { useToast } from '../../components/ui/Toast';
import { addDays, homNay, ngay, so, so1, tien } from '../../utils/format';
import '../../pages/invoice-print.css';
import './accounting.css';

const gioNgay = (v?: string) => (v ? `${v.slice(11, 16)} ${v.slice(8, 10)}/${v.slice(5, 7)}` : '');
const LAM_MOI = [['night-audit'], ['reports'], ['front-desk'], ['bookings']];

// Tổng các ngày đã chốt (dòng "Cộng N ngày"): công suất / ADR / RevPAR tính lại từ tổng, như Views/NightAudit/Index
export function congNgayChot(ds: DayClose[]) {
  const c = (f: (x: NightAuditFigures) => number | undefined) => ds.reduce((t, d) => t + (f(d.SoLieu ?? {}) ?? 0), 0);
  const dem = c((x) => x.DemPhongBan), tienPhong = c((x) => x.TienPhong), phongNgay = c((x) => x.TongSoPhong);
  return {
    dem, tienPhong,
    congSuat: phongNgay ? 100 * dem / phongNgay : 0,
    adr: dem ? Math.round(tienPhong / dem) : 0,
    revpar: phongNgay ? Math.round(tienPhong / phongNgay) : 0,
    khac: c((x) => x.DoanhThuKhac), dichVu: c((x) => x.TienDichVu),
    den: c((x) => x.KhachDen), di: c((x) => x.KhachDi), khongDen: c((x) => x.KhachKhongDen)
  };
}

function BangDem({ s }: { s: NightAuditFigures }) {
  return (
    <div className="bang-cuon">
      <table className="bang" aria-label="Đêm phòng">
        <thead><tr><th>Phòng</th><th>Phiếu · khách</th><th>Quy tắc giá</th><th className="canh-phai">Giá đêm</th></tr></thead>
        <tbody>
          {!s.CacDem?.length && <tr><td colSpan={4} className="canh-giua muted">Không có phòng nào có khách đêm này.</td></tr>}
          {(s.CacDem ?? []).map((n) => <tr key={n.MaDatPhong + (n.MaPhong ?? '')} data-dem={n.MaDatPhong}><td><strong>P{n.SoPhong}</strong></td><td>{n.MaDatPhong}<div className="small muted">{n.HoTenKhachHang}</div></td><td className="small">{n.QuyTac ?? 'Giá thường'}</td><td className="canh-phai nowrap"><strong>{so(n.Gia)}</strong></td></tr>)}
          {(s.CocGiuLai ?? []).map((c) => <tr key={`c${c.MaDatPhong}`} className="kt-coc-giu"><td className="small muted">Cọc giữ lại</td><td>{c.MaDatPhong}<div className="small muted">{c.HoTenKhachHang} · nhận dự kiến {ngay(c.NgayNhan).slice(0, 5)}</div></td><td className="small">Doanh thu khác</td><td className="canh-phai nowrap">{so(c.TienCoc)}</td></tr>)}
        </tbody>
      </table>
    </div>
  );
}

// Chốt ngày (NightAudit/Index): xem trước ngày cần chốt, khách không đến, cảnh báo, chốt; danh sách ngày đã chốt; Admin mở lại ngày gần nhất
export function NightAuditPage() {
  const [params, setParams] = useSearchParams();
  const { session } = useAuth();
  const laQuanLy = session?.vaiTro === 'Admin';
  const ngayXem = params.get('ngay') || null;
  let denNgay = params.get('denNgay') || homNay();
  let tuNgay = params.get('tuNgay') || addDays(denNgay, -30);
  if (denNgay < tuNgay) [tuNgay, denNgay] = [denNgay, tuNgay];
  const pv = useQuery({ queryKey: ['night-audit', 'preview', ngayXem], queryFn: () => nightAuditApi.preview(ngayXem) });
  const ds = useQuery({ queryKey: ['night-audit', 'list', tuNgay, denNgay], queryFn: () => nightAuditApi.list(tuNgay, denNgay) });
  const [xem, setXem] = useState(ngayXem ?? '');
  const [khoang, setKhoang] = useState({ tuNgay, denNgay });
  const [moLai, setMoLai] = useState(false);
  useEffect(() => { if (pv.data?.Ngay) setXem(pv.data.Ngay.slice(0, 10)); }, [pv.data?.Ngay]);

  const datParams = (p: Record<string, string | null>) => {
    const n = new URLSearchParams(params);
    Object.entries(p).forEach(([k, v]) => { if (v) n.set(k, v); else n.delete(k); });
    setParams(n);
  };

  if (pv.isLoading) return <Loading />;
  if (!pv.data) return <ErrorBox message={pv.error?.message ?? 'Không tải được số liệu chốt ngày.'} />;
  const m = pv.data;
  const s = m.SoLieu ?? {};
  const st = m.TinhTrang ?? {};
  const daChot = ds.data ?? [];
  const ngayM = m.Ngay?.slice(0, 10) ?? '';
  const thuRong = (s.TheoPhuongThuc ?? []).reduce((t, x) => t + (x.Rong ?? 0), 0);
  const canhBao = (m.QuaHanTra?.length ?? 0) + ((m.HoaDonChuaThanhToan ?? 0) > 0 ? 1 : 0) + (m.CaChuaChot?.length ?? 0);
  const laHomNay = ngayM >= homNay();
  const ganNhat = st.NgayChotGanNhat?.slice(0, 10);
  const tong = congNgayChot(daChot);

  return (
    <>
      <PageHeader tieuDe="Chốt ngày"
        phu={`Áp dụng từ ${ngay(st.NgayBatDau)} · chốt gần nhất: ${st.NgayChotGanNhat ? ngay(st.NgayChotGanNhat) : 'chưa có'} · ${st.NgayCanChot ? `cần chốt: ${ngay(st.NgayCanChot)} (${st.SoNgayChuaChot} ngày chưa chốt)` : 'đã chốt tới hôm qua'}`}>
        <form className="o-ghep" onSubmit={(e) => { e.preventDefault(); datParams({ ngay: xem || null }); }} aria-label="Xem ngày">
          <input type="date" value={xem} max={homNay()} onChange={(e) => setXem(e.target.value)} aria-label="Xem ngày" />
          <button type="submit" className="btn btn-outline btn-sm">Xem</button>
        </form>
      </PageHeader>

      <section className="the-trang" aria-label="Số liệu ngày">
        <div className="the-dau">
          <h2>{m.ChoPhepChot ? 'Xem trước khi chốt' : laHomNay ? 'Số liệu tạm tính' : 'Số liệu'} ngày {ngay(ngayM)}</h2>
          {!m.ChoPhepChot
            ? <small className="muted na-ly-do">ⓘ {m.LyDoKhongChot}{daChot.some((c) => c.Ngay?.slice(0, 10) === ngayM) && <> <Link to={`/nhan-vien/in/chot-ngay/${ngayM}`}>Xem bản đã chốt</Link></>}</small>
            : <small className="muted">Doanh thu phòng ghi nhận theo đêm, giá đêm theo quy tắc giá, trước giảm giá, chưa gồm VAT.</small>}
        </div>
        <div className="kpi-luoi kpi-luoi-trong">
          <div className="kpi kpi-toi"><span>Tiền phòng theo đêm</span><strong className="kpi-nho" data-na="tien-phong">{tien(s.TienPhong)}</strong><small>{s.DemPhongBan} / {s.TongSoPhong} đêm phòng · công suất {so1(s.CongSuat)}%</small></div>
          <div className="kpi"><span>ADR · RevPAR</span><strong className="kpi-nho">{tien(s.ADR)}</strong><small>RevPAR {tien(s.RevPAR)} · đến {s.KhachDen} · đi {s.KhachDi} khách</small></div>
          <div className="kpi"><span>Doanh thu khác</span><strong className="kpi-nho">{tien(s.DoanhThuKhac)}</strong><small>Cọc giữ lại · {s.CocGiuLai?.length ?? 0} phiếu{m.ChoPhepChot && m.KhachKhongDen?.some((k) => (k.TienCoc ?? 0) > 0) ? ' (+ khách không đến đánh dấu huỷ)' : ''}</small></div>
          <div className="kpi"><span>Tiền thu ròng</span><strong className="kpi-nho">{tien(thuRong)}</strong><small>Dịch vụ phát sinh {tien(s.TienDichVu)} · {s.SoHoaDonThanhToan} hoá đơn thanh toán</small></div>
        </div>
      </section>

      <div className="kt-luoi">
        {m.ChoPhepChot ? <FormChot key={ngayM} ngayM={ngayM} m={m} onXong={() => datParams({ ngay: null })} /> : (
          <section className="the-trang the-bang">
            <div className="the-dau"><h2>Đêm phòng ({s.CacDem?.length ?? 0})</h2></div>
            <BangDem s={s} />
          </section>
        )}
        <section className="the-trang na-canh-bao">
          <div className="the-dau"><h2>Cần kiểm tra trước khi chốt {canhBao > 0 ? `(${canhBao})` : ''}</h2></div>
          <ul>
            {canhBao === 0 && <li className="chu-ok">✓ Không có cảnh báo.</li>}
            {!!m.QuaHanTra?.length && <li><strong>Khách quá hạn trả phòng:</strong><ul>{m.QuaHanTra.map((q) => <li key={q}>{q}</li>)}</ul></li>}
            {(m.HoaDonChuaThanhToan ?? 0) > 0 && <li><strong>{m.HoaDonChuaThanhToan} hoá đơn chưa thanh toán</strong> lập tới ngày này.</li>}
            {!!m.CaChuaChot?.length && <li><strong>Chưa chốt ca sau lần thu tiền cuối trong ngày:</strong> {m.CaChuaChot.join(', ')}</li>}
          </ul>
          <h3 className="quay-h">Tiền thu / chi theo phương thức</h3>
          {!s.TheoPhuongThuc?.length && <p className="muted small">Không có giao dịch tiền trong ngày.</p>}
          {(s.TheoPhuongThuc ?? []).map((x) => (
            <div key={x.PhuongThuc} className="kt-dong na-pt"><span>{x.PhuongThuc} <span className="muted">· {x.SoGiaoDich} GD</span></span><span>{so(x.Thu)} {(x.Chi ?? 0) > 0 ? `/ -${so(x.Chi)}` : ''} → <strong>{so(x.Rong)}</strong></span></div>
          ))}
        </section>
      </div>

      {m.ChoPhepChot && (
        <section className="the-trang the-bang">
          <div className="the-dau"><h2>Đêm phòng sẽ ghi nhận ({s.CacDem?.length ?? 0})</h2></div>
          <BangDem s={s} />
        </section>
      )}

      <section className="the-trang the-bang">
        <div className="the-dau">
          <div><h2>Các ngày đã chốt · doanh thu theo đêm</h2><small className="muted">Số liệu lưu tại thời điểm chốt. Báo cáo doanh thu theo ngày thanh toán vẫn ở trang Báo cáo.</small></div>
          <form className="o-ghep" onSubmit={(e) => { e.preventDefault(); datParams({ tuNgay: khoang.tuNgay, denNgay: khoang.denNgay }); }} aria-label="Khoảng ngày đã chốt">
            <input type="date" value={khoang.tuNgay} aria-label="Từ ngày" onChange={(e) => setKhoang({ ...khoang, tuNgay: e.target.value })} />
            <input type="date" value={khoang.denNgay} aria-label="Đến ngày" onChange={(e) => setKhoang({ ...khoang, denNgay: e.target.value })} />
            <button type="submit" className="btn btn-gold btn-sm">Xem</button>
          </form>
        </div>
        <ErrorBox message={ds.error?.message} />
        <div className="bang-cuon">
          <table className="bang" aria-label="Các ngày đã chốt">
            <thead className="nowrap"><tr><th>Ngày</th><th className="canh-phai">Đêm phòng</th><th className="canh-phai">Công suất</th><th className="canh-phai">Tiền phòng</th><th className="canh-phai">ADR</th><th className="canh-phai">RevPAR</th><th className="canh-phai">DT khác</th><th className="canh-phai">Dịch vụ</th><th className="canh-giua">Đến / đi / không đến</th><th>Người chốt</th><th /></tr></thead>
            <tbody>
              {!daChot.length && <tr><td colSpan={11} className="canh-giua muted">Chưa có ngày nào được chốt trong khoảng này.</td></tr>}
              {daChot.map((c) => {
                const x = c.SoLieu ?? {};
                const n = c.Ngay?.slice(0, 10) ?? '';
                return (
                  <tr key={n} data-ngay-chot={n}>
                    <td><strong>{ngay(n)}</strong>{c.GhiChu && <div className="small muted">{c.GhiChu}</div>}</td>
                    <td className="canh-phai">{x.DemPhongBan}</td><td className="canh-phai">{so1(x.CongSuat)}%</td>
                    <td className="canh-phai nowrap"><strong>{so(x.TienPhong)}</strong></td><td className="canh-phai nowrap">{so(x.ADR)}</td><td className="canh-phai nowrap">{so(x.RevPAR)}</td>
                    <td className="canh-phai nowrap">{so(x.DoanhThuKhac)}</td><td className="canh-phai nowrap">{so(x.TienDichVu)}</td>
                    <td className="canh-giua small">{x.KhachDen} / {x.KhachDi} / {x.KhachKhongDen}</td>
                    <td className="small">{c.TenNhanVien}<div className="muted">{gioNgay(c.ThoiGianChot)}</div></td>
                    <td className="nowrap canh-phai">
                      <Link to={`/nhan-vien/in/chot-ngay/${n}`} className="btn btn-outline btn-sm" title="Xem / in">In</Link>
                      {laQuanLy && ganNhat === n && <> <button type="button" className="btn btn-outline-danger btn-sm" title="Mở lại ngày này" onClick={() => setMoLai(true)}>Mở lại</button></>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
            {daChot.length > 1 && (
              <tfoot>
                <tr className="kt-tong">
                  <td>Cộng {daChot.length} ngày</td><td className="canh-phai">{tong.dem}</td><td className="canh-phai">{so1(tong.congSuat)}%</td>
                  <td className="canh-phai nowrap">{so(tong.tienPhong)}</td><td className="canh-phai nowrap">{so(tong.adr)}</td><td className="canh-phai nowrap">{so(tong.revpar)}</td>
                  <td className="canh-phai nowrap">{so(tong.khac)}</td><td className="canh-phai nowrap">{so(tong.dichVu)}</td>
                  <td className="canh-giua small">{tong.den} / {tong.di} / {tong.khongDen}</td><td colSpan={2} />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </section>
      {moLai && ganNhat && <HopMoLai ngayMo={ganNhat} onDong={() => setMoLai(false)} onXong={() => { setMoLai(false); datParams({ ngay: ganNhat }); }} />}
    </>
  );
}

function FormChot({ ngayM, m, onXong }: { ngayM: string; m: NightAuditPreview; onXong: () => void }) {
  const xacNhan = useXacNhan();
  const ds = m.KhachKhongDen ?? [];
  const [huy, setHuy] = useState<Set<string>>(() => new Set(ds.map((k) => k.MaDatPhong!)));
  const [ghiChu, setGhiChu] = useState('');
  const chot = useThaoTac({
    mutationFn: () => nightAuditApi.close(ngayM, ds.map((k) => k.MaDatPhong!).filter((x) => !huy.has(x)), ghiChu.trim() || null), lamMoi: LAM_MOI,
    thanhCong: (c) => `Đã chốt ngày ${ngay(c.Ngay)}: ${c.SoLieu?.DemPhongBan} đêm phòng, tiền phòng ${so(c.SoLieu?.TienPhong)} ₫`
      + ((c.SoLieu?.KhachKhongDen ?? 0) > 0 ? `, huỷ ${c.SoLieu?.KhachKhongDen} phiếu khách không đến.` : '.'),
    tienToLoi: 'Không chốt được ngày. ', onSuccess: onXong
  });
  const gui = async (e: FormEvent) => {
    e.preventDefault();
    if (await xacNhan({ tieuDe: `Chốt ngày ${ngay(ngayM)}?`, noiDung: 'Các phiếu còn đánh dấu sẽ bị huỷ và giữ cọc. Sau khi chốt chỉ quản lý mở lại được.', nutXacNhan: 'Chốt ngày' })) chot.mutate();
  };
  const doi = (ma: string) => setHuy((h) => { const n = new Set(h); if (n.has(ma)) n.delete(ma); else n.add(ma); return n; });
  return (
    <form className="the-trang the-bang na-chot" onSubmit={gui} aria-label="Chốt ngày">
      <div className="the-dau"><div><h2>Khách không đến ({ds.length})</h2>
        <small className="muted">Phiếu có ngày nhận tới {ngay(ngayM).slice(0, 5)} mà chưa nhận phòng. Mặc định huỷ và giữ cọc; bỏ đánh dấu nếu khách đã báo đến muộn.</small></div></div>
      <div className="bang-cuon">
        <table className="bang" aria-label="Khách không đến">
          <thead><tr><th title="Huỷ phiếu, giữ cọc">Huỷ</th><th>Phiếu</th><th>Ngày nhận</th><th className="canh-phai">Tiền cọc</th></tr></thead>
          <tbody>
            {!ds.length && <tr><td colSpan={4} className="canh-giua muted">Không có khách nào chưa nhận phòng.</td></tr>}
            {ds.map((k) => (
              <tr key={k.MaDatPhong} data-khong-den={k.MaDatPhong}>
                <td className="canh-giua"><input type="checkbox" checked={huy.has(k.MaDatPhong!)} onChange={() => doi(k.MaDatPhong!)} aria-label={`Huỷ phiếu ${k.MaDatPhong}`} /></td>
                <td><strong>{k.MaDatPhong}</strong><div className="small muted">{k.HoTenKhachHang}{k.SoPhong ? ` · P${k.SoPhong}` : ''}{k.GioDenDuKien ? ` · đến ${k.GioDenDuKien}` : ''}</div></td>
                <td className="small nowrap">{ngay(k.NgayNhan)}</td>
                <td className="canh-phai nowrap"><strong>{(k.TienCoc ?? 0) > 0 ? so(k.TienCoc) : '—'}</strong></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="na-gui">
        <label className="field"><span>Ghi chú chốt ngày</span><input maxLength={500} value={ghiChu} placeholder="Không bắt buộc" onChange={(e) => setGhiChu(e.target.value)} /></label>
        <button type="submit" className="btn btn-gold" disabled={chot.isPending}>Chốt ngày {ngay(ngayM)}</button>
      </div>
    </form>
  );
}

function HopMoLai({ ngayMo, onDong, onXong }: { ngayMo: string; onDong: () => void; onXong: () => void }) {
  const [lyDo, setLyDo] = useState('');
  const mo = useThaoTac({
    mutationFn: () => nightAuditApi.reopen(ngayMo, lyDo.trim()), lamMoi: LAM_MOI,
    thanhCong: `Đã mở lại ngày ${ngay(ngayMo)}. Sửa xong hãy chốt lại.`, tienToLoi: 'Không mở lại được ngày chốt. ', onSuccess: onXong
  });
  return (
    <Modal open title={`Mở lại ngày ${ngay(ngayMo)}`} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); mo.mutate(); }}>
        <p className="small muted">Doanh thu theo đêm và cọc giữ lại của ngày này được gỡ ra để chốt lại. Phiếu khách không đến đã huỷ giữ nguyên. Lý do được ghi vào nhật ký thao tác.</p>
        <label className="field"><span>Lý do mở lại</span><textarea rows={3} maxLength={300} required value={lyDo} onChange={(e) => setLyDo(e.target.value)} autoFocus /></label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-danger" disabled={mo.isPending}>Mở lại</button></div>
      </form>
    </Modal>
  );
}

// Bản in một ngày đã chốt (NightAudit/Detail): ngoài khung trang nhân viên; ngày chưa chốt → báo lỗi, về trang chốt ngày
export function NightAuditPrintPage() {
  const { ngay: n = '' } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const q = useQuery({ queryKey: ['night-audit', 'day', n], queryFn: () => nightAuditApi.get(n) });
  const baoRoi = useRef(false);
  useEffect(() => {
    if (q.isSuccess && !q.data && !baoRoi.current) { baoRoi.current = true; toast.loi(`Ngày ${ngay(n)} chưa chốt.`); navigate('/nhan-vien/chot-ngay', { replace: true }); }
  }, [q.isSuccess, q.data, n, navigate, toast]);
  const quayLai = () => navigate('/nhan-vien/chot-ngay');
  if (q.isLoading || (q.isSuccess && !q.data)) return <Loading />;
  if (q.error) return <TrangIn tieuDe="Chốt ngày" onQuayLai={quayLai} nhanQuayLai="← Chốt ngày"><ErrorBox message={q.error.message} /></TrangIn>;
  const c = q.data!;
  const s = c.SoLieu ?? {};
  const pt = s.TheoPhuongThuc ?? [];
  const cong = (f: (x: (typeof pt)[number]) => number | undefined) => pt.reduce((t, x) => t + (f(x) ?? 0), 0);

  return (
    <TrangIn tieuDe={`Chốt ngày ${ngay(c.Ngay)}`} onQuayLai={quayLai} nhanQuayLai="← Chốt ngày">
      <div className="hd na-in">
        <div className="hd-dau">
          <div><div className="hd-brand">LUXE RESERVE</div><div className="muted">Báo cáo chốt ngày (night audit)</div></div>
          <div className="hd-so">Chốt lúc {gioNgay(c.ThoiGianChot).slice(0, 5)} {ngay(c.ThoiGianChot)}<br /><span className="muted">bởi {c.TenNhanVien ?? '—'}</span></div>
        </div>
        <h1>NGÀY {ngay(c.Ngay)}</h1>
        <p className="hd-phu muted">Tiền phòng ghi nhận theo đêm theo giá đêm (quy tắc giá), trước giảm giá, chưa gồm VAT.{c.GhiChu ? ` Ghi chú: ${c.GhiChu}` : ''}</p>
        <div className="na-kpi">
          <div><span>Tiền phòng</span><strong>{so(s.TienPhong)} ₫</strong></div>
          <div><span>Đêm phòng · công suất</span><strong>{s.DemPhongBan}/{s.TongSoPhong} · {so1(s.CongSuat)}%</strong></div>
          <div><span>ADR</span><strong>{so(s.ADR)} ₫</strong></div>
          <div><span>RevPAR</span><strong>{so(s.RevPAR)} ₫</strong></div>
          <div><span>Doanh thu khác (cọc giữ lại)</span><strong>{so(s.DoanhThuKhac)} ₫</strong></div>
          <div><span>Dịch vụ phát sinh</span><strong>{so(s.TienDichVu)} ₫</strong></div>
          <div><span>Hoá đơn thanh toán</span><strong>{s.SoHoaDonThanhToan} · {so(s.TongHoaDonThanhToan)} ₫</strong></div>
          <div><span>Đến / đi / không đến</span><strong>{s.KhachDen} / {s.KhachDi} / {s.KhachKhongDen}</strong></div>
        </div>
        <h2>Tiền thu / chi theo phương thức</h2>
        <table className="hd-bang">
          <thead><tr><th>Phương thức</th><th className="so">Số GD</th><th className="so">Thu</th><th className="so">Chi</th><th className="so">Ròng</th></tr></thead>
          <tbody>
            {!pt.length && <tr><td colSpan={5} className="muted">Không có giao dịch.</td></tr>}
            {pt.map((x) => <tr key={x.PhuongThuc}><td>{x.PhuongThuc}</td><td className="so">{x.SoGiaoDich}</td><td className="so">{so(x.Thu)}</td><td className="so">{so(x.Chi)}</td><td className="so">{so(x.Rong)}</td></tr>)}
          </tbody>
          <tfoot><tr><td>Cộng</td><td className="so">{cong((x) => x.SoGiaoDich)}</td><td className="so">{so(cong((x) => x.Thu))}</td><td className="so">{so(cong((x) => x.Chi))}</td><td className="so">{so(cong((x) => x.Rong))}</td></tr></tfoot>
        </table>
        <h2>Đêm phòng ({s.CacDem?.length ?? 0})</h2>
        <table className="hd-bang">
          <thead><tr><th>Phòng</th><th>Phiếu</th><th>Khách</th><th>Quy tắc giá</th><th className="so">Giá đêm</th></tr></thead>
          <tbody>
            {!s.CacDem?.length && <tr><td colSpan={5} className="muted">Không có.</td></tr>}
            {(s.CacDem ?? []).map((x) => <tr key={x.MaDatPhong + (x.MaPhong ?? '')}><td>P{x.SoPhong}</td><td>{x.MaDatPhong}</td><td>{x.HoTenKhachHang}</td><td>{x.QuyTac ?? 'Giá thường'}</td><td className="so">{so(x.Gia)}</td></tr>)}
          </tbody>
          <tfoot><tr><td colSpan={4}>Cộng tiền phòng</td><td className="so">{so(s.TienPhong)}</td></tr></tfoot>
        </table>
        {!!s.CocGiuLai?.length && (
          <>
            <h2>Cọc giữ lại ghi nhận doanh thu ({s.CocGiuLai.length})</h2>
            <table className="hd-bang">
              <thead><tr><th>Phiếu</th><th>Khách</th><th>Ngày nhận dự kiến</th><th className="so">Tiền cọc</th></tr></thead>
              <tbody>{s.CocGiuLai.map((x) => <tr key={x.MaDatPhong}><td>{x.MaDatPhong}</td><td>{x.HoTenKhachHang}</td><td>{ngay(x.NgayNhan)}</td><td className="so">{so(x.TienCoc)}</td></tr>)}</tbody>
            </table>
          </>
        )}
        <div className="hd-ky"><div><p>Người chốt</p>{c.TenNhanVien}</div><div><p>Kế toán / Quản lý</p></div></div>
      </div>
    </TrangIn>
  );
}
