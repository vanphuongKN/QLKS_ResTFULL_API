import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { accountingApi, PHUONG_THUC_QUAY, TRANG_THAI_HOA_DON, type InvoiceListItem, type InvoiceStatus } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { useXacNhan } from '../../components/ui/Dialog';
import { BoLocNgay, dauThang, EmptyState, NutTaiTep, PageHeader, useKhoangNgay } from '../../components/ui/Page';
import { homNay, ngay, ngayGio, so, so1, tien } from '../../utils/format';
import './accounting.css';

const BADGE_HD: Record<InvoiceStatus, string> = { Paid: 'badge-ok', Unpaid: 'badge-warn', Cancelled: 'badge-muted' };
const ngayNgan = (v?: string) => (v ? `${v.slice(8, 10)}/${v.slice(5, 7)}` : '');
export const thangNay = () => ({ tuNgay: dauThang(), denNgay: homNay() });

// % thay đổi so với kỳ trước (null khi kỳ trước bằng 0), như Delta() của Accounting/Index
export function phanTramThayDoi(nay?: number, truoc?: number): number | null {
  if (!truoc) return null;
  return Math.round(((nay ?? 0) - truoc) * 1000 / truoc) / 10;
}

function ThayDoi({ v }: { v: number | null }) {
  if (v == null) return <span className="kpi-thay-doi">—</span>;
  return <span className={`kpi-thay-doi ${v >= 0 ? 'kpi-tang' : 'kpi-giam'}`}>{v >= 0 ? '▲' : '▼'} {so1(Math.abs(v))}%</span>;
}

// Tổng quan kế toán (Accounting/Index): kỳ chọn so với kỳ liền trước, việc cần đối soát, doanh thu theo kênh
export function AccountingOverviewPage() {
  const [k, setK] = useKhoangNgay(thangNay);
  const q = useQuery({ queryKey: ['reports', 'overview', k], queryFn: () => accountingApi.overview(k.tuNgay, k.denNgay) });
  const d = q.data;
  const qs = `tuNgay=${k.tuNgay}&denNgay=${k.denNgay}`;
  const tongKenh = Math.max(1, (d?.TheoKenh ?? []).reduce((t, x) => t + (x.DoanhThu ?? 0), 0));
  const ky = d?.KyNay, tr = d?.KyTruoc;

  return (
    <>
      <PageHeader tieuDe="Tổng quan kế toán"
        phu={<>{ngay(k.tuNgay)} – {ngay(k.denNgay)}{tr && <> · so với {ngayNgan(tr.TuNgay)} – {ngayNgan(tr.DenNgay)} (cùng số ngày liền trước)</>}</>}>
        <NutTaiTep path={accountingApi.revenueExportPath(k.tuNgay, k.denNgay)} tenMacDinh={`BaoCaoDoanhThu_${k.tuNgay.replace(/-/g, '')}_${k.denNgay.replace(/-/g, '')}.xlsx`}>Xuất Excel</NutTaiTep>
      </PageHeader>
      <BoLocNgay key={`${k.tuNgay}${k.denNgay}`} value={k} onChange={setK} />
      <ErrorBox message={q.error?.message} />
      {q.isLoading || !d ? (q.isLoading && <Loading />) : (
        <>
          <div className="kpi-luoi">
            <div className="kpi"><span>Doanh thu thuần</span><strong className="kpi-nho">{tien(ky?.DoanhThuThuan)}</strong>
              <small><ThayDoi v={phanTramThayDoi(ky?.DoanhThuThuan, tr?.DoanhThuThuan)} /> {d.SoHoaDon} hoá đơn · đã trừ hoàn {so(d.TongHoanTien)} ₫</small></div>
            <div className="kpi"><span>Công suất trung bình</span><strong className="kpi-nho">{so1(ky?.CongSuat)}%</strong>
              <small><ThayDoi v={phanTramThayDoi(ky?.CongSuat, tr?.CongSuat)} /> {ky?.DemPhongDaBan} đêm phòng đã bán</small></div>
            <div className="kpi"><span>ADR</span><strong className="kpi-nho">{tien(ky?.ADR)}</strong>
              <small><ThayDoi v={phanTramThayDoi(ky?.ADR, tr?.ADR)} /> Giá trung bình mỗi đêm phòng</small></div>
            <div className="kpi"><span>RevPAR</span><strong className="kpi-nho">{tien(ky?.RevPAR)}</strong>
              <small><ThayDoi v={phanTramThayDoi(ky?.RevPAR, tr?.RevPAR)} /> Doanh thu phòng / phòng sẵn có</small></div>
          </div>
          <div className="kt-luoi">
            <section className="the-trang">
              <div className="the-dau"><h2>Việc cần đối soát</h2></div>
              <div className="viec-kt">
                <ViecKt so={d.HoaDonChuaThanhToan ?? 0} ten="Hoá đơn chưa thanh toán" phu={(d.TienChuaThanhToan ?? 0) > 0 ? tien(d.TienChuaThanhToan) : ''} to="/nhan-vien/ke-toan/hoa-don?trangThai=Unpaid" muc={(d.HoaDonChuaThanhToan ?? 0) > 0 ? 'warn' : ''} />
                <ViecKt so={d.HoaDonCongTyChuaXuat ?? 0} ten="HĐ công ty chưa ghi số hoá đơn điện tử" to="/nhan-vien/ke-toan/vat?chuaXuat=1" muc={(d.HoaDonCongTyChuaXuat ?? 0) > 0 ? 'warn' : ''} />
                <ViecKt so={d.SoCaChenhLech ?? 0} ten="Ca lễ tân lệch tiền mặt" phu={d.TongChenhLech ? `${so(d.TongChenhLech)} ₫` : ''} to={`/nhan-vien/ke-toan/doi-soat?${qs}`} muc={(d.SoCaChenhLech ?? 0) > 0 ? 'danger' : ''} />
                <ViecKt so={d.SoPhieuGiuCoc ?? 0} ten="Phiếu đang giữ cọc (nợ phải trả khách)" phu={(d.CocDangGiu ?? 0) > 0 ? tien(d.CocDangGiu) : ''} to={`/nhan-vien/ke-toan/doi-soat?${qs}`} muc="" />
                <ViecKt so={(d.CocGiuLaiKhiHuy ?? 0) > 0 ? tien(d.CocGiuLaiKhiHuy) : 0} ten="Cọc giữ lại khi khách huỷ (theo ngày nhận dự kiến, chưa vào doanh thu)" to={`/nhan-vien/ke-toan/doi-soat?${qs}`} muc={(d.CocGiuLaiKhiHuy ?? 0) > 0 ? 'warn' : ''} />
              </div>
            </section>
            <section className="the-trang">
              <div className="the-dau"><h2>Doanh thu theo kênh đặt phòng</h2><Link to={`/nhan-vien/ke-toan/bao-cao?${qs}`} className="small">Báo cáo chi tiết</Link></div>
              <div className="bang-cuon">
                <table className="bang" aria-label="Doanh thu theo kênh đặt phòng">
                  <thead><tr><th>Kênh</th><th className="canh-giua">Lượt</th><th className="canh-giua">Đêm</th><th className="kt-ty-trong">Tỷ trọng</th><th className="canh-phai">Doanh thu</th></tr></thead>
                  <tbody>
                    {!d.TheoKenh?.length && <tr><td colSpan={5} className="canh-giua muted">Chưa có hoá đơn thanh toán trong kỳ.</td></tr>}
                    {(d.TheoKenh ?? []).map((x) => {
                      const pct = (x.DoanhThu ?? 0) * 100 / tongKenh;
                      return (
                        <tr key={x.Kenh}>
                          <td><strong>{x.Kenh}</strong></td><td className="canh-giua">{x.SoLuot}</td><td className="canh-giua">{x.SoDem}</td>
                          <td><div className="kt-thanh"><span style={{ width: `${pct}%` }} /></div><small className="muted">{so1(pct)}%</small></td>
                          <td className="canh-phai nowrap">{so(x.DoanhThu)}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </section>
          </div>
        </>
      )}
    </>
  );
}

function ViecKt({ so: giaTri, ten, phu, to, muc }: { so: number | string; ten: string; phu?: string; to: string; muc: string }) {
  const khong = giaTri === 0 || giaTri === '0';
  return (
    <Link to={to} className={`viec-kt-dong ${khong ? 'is-zero' : muc}`}>
      <span>{ten}{phu && <small className="muted">{phu}</small>}</span><strong>{giaTri}</strong>
    </Link>
  );
}

// Danh sách hoá đơn (Accounting/Invoices): lọc ngày lập, trạng thái, tìm mã HĐ / mã phiếu / tên khách
export function InvoicesPage() {
  const [params, setParams] = useSearchParams();
  const loc = {
    tuNgay: params.get('tuNgay') || '', denNgay: params.get('denNgay') || '',
    trangThai: (['Paid', 'Unpaid'].includes(params.get('trangThai') ?? '') ? params.get('trangThai') : '') as InvoiceStatus | '',
    search: params.get('search') ?? ''
  };
  const [f, setF] = useState(loc);
  const q = useQuery({ queryKey: ['invoices', loc], queryFn: () => accountingApi.invoices({ ...loc, trangThai: loc.trangThai || null }) });
  const ds = q.data ?? [];
  const gui = (e: FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams();
    Object.entries(f).forEach(([k, v]) => { if (v) p.set(k, v.trim()); });
    setParams(p);
  };
  const tong = (lay: (h: InvoiceListItem) => number | undefined) => ds.reduce((t, h) => t + (lay(h) ?? 0), 0);
  const daHoan = tong((h) => h.DaHoan);

  return (
    <>
      <PageHeader tieuDe="Danh sách hoá đơn" phu="Kế toán" />
      <form className="bo-loc kt-loc" onSubmit={gui} aria-label="Lọc hoá đơn">
        <label>Từ ngày <input type="date" value={f.tuNgay} onChange={(e) => setF({ ...f, tuNgay: e.target.value })} /></label>
        <label>Đến ngày <input type="date" value={f.denNgay} onChange={(e) => setF({ ...f, denNgay: e.target.value })} /></label>
        <label>Trạng thái
          <select value={f.trangThai} onChange={(e) => setF({ ...f, trangThai: e.target.value as InvoiceStatus | '' })}>
            <option value="">Tất cả</option><option value="Paid">Đã thanh toán</option><option value="Unpaid">Chưa thanh toán</option>
          </select>
        </label>
        <label className="kt-tim">Tìm kiếm <input type="search" value={f.search} placeholder="Mã hoá đơn, mã phiếu, tên khách..." onChange={(e) => setF({ ...f, search: e.target.value })} /></label>
        <button type="submit" className="btn btn-primary btn-sm">Lọc</button>
        <button type="button" className="btn btn-outline btn-sm" onClick={() => { setF({ tuNgay: '', denNgay: '', trangThai: '', search: '' }); setParams(new URLSearchParams()); }}>Xoá lọc</button>
      </form>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : (
        <section className="the-trang the-bang">
          <div className="bang-cuon">
            <table className="bang" aria-label="Danh sách hoá đơn">
              <thead><tr><th>Mã HĐ</th><th>Khách hàng</th><th>Phòng</th><th>Ngày lập</th><th className="canh-phai">Tổng tiền</th><th className="canh-phai">Cọc trừ vào HĐ</th><th className="canh-phai">Đã thu</th><th className="canh-phai">Đã hoàn</th><th>Trạng thái</th><th /></tr></thead>
              <tbody>
                {!ds.length && <tr><td colSpan={10} className="canh-giua muted">Không có hoá đơn phù hợp.</td></tr>}
                {ds.map((h) => (
                  <tr key={h.MaHoaDon} data-hoa-don={h.MaHoaDon}>
                    <td><strong>{h.MaHoaDon}</strong><div className="small muted">{h.MaDatPhong}</div></td>
                    <td>{h.HoTenKhachHang}</td>
                    <td>{h.SoPhong}</td>
                    <td className="small nowrap">{ngayGio(h.NgayLapHoaDon)}<div className="muted">{h.TenNhanVienLap}</div></td>
                    <td className="canh-phai nowrap"><strong>{so(h.TongTienCanThanhToan)}</strong></td>
                    <td className="canh-phai nowrap">{(h.CocDaTru ?? 0) > 0 ? so(h.CocDaTru) : '—'}{(h.CocThua ?? 0) > 0 && <div className="small muted" title="Cọc thừa đã hoàn lại khách khi thanh toán">+{so(h.CocThua)} đã hoàn</div>}</td>
                    <td className="canh-phai nowrap">{so(h.DaThu)}<div className="small muted">{h.PhuongThuc}</div></td>
                    <td className={`canh-phai nowrap${(h.DaHoan ?? 0) > 0 ? ' chu-loi' : ''}`}>{(h.DaHoan ?? 0) > 0 ? `-${so(h.DaHoan)}` : '—'}</td>
                    <td><span className={`badge ${BADGE_HD[h.TrangThai ?? 'Unpaid']}`}>{TRANG_THAI_HOA_DON[h.TrangThai ?? 'Unpaid']}</span></td>
                    <td><Link to={`/nhan-vien/ke-toan/hoa-don/${h.MaHoaDon}`} className="btn btn-outline btn-sm">Chi tiết</Link></td>
                  </tr>
                ))}
              </tbody>
              {ds.length > 0 && (
                <tfoot>
                  <tr className="kt-tong">
                    <td colSpan={4} className="canh-phai">Tổng ({ds.length} hoá đơn)</td>
                    <td className="canh-phai nowrap">{so(tong((h) => h.TongTienCanThanhToan))}</td>
                    <td className="canh-phai nowrap">{so(tong((h) => h.CocDaTru))}</td>
                    <td className="canh-phai nowrap">{so(tong((h) => h.DaThu))}</td>
                    <td className="canh-phai nowrap chu-loi">{daHoan > 0 ? `-${so(daHoan)}` : '—'}</td>
                    <td colSpan={2} />
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
        </section>
      )}
    </>
  );
}

// Chi tiết hoá đơn (Accounting/Invoice): thông tin lưu trú, VAT công ty, giao dịch thu / hoàn (gồm sổ cọc), hoàn tiền
export function InvoiceDetailPage() {
  const { maHoaDon = '' } = useParams();
  const q = useQuery({ queryKey: ['invoices', 'detail', maHoaDon], queryFn: () => accountingApi.invoice(maHoaDon) });
  if (q.isLoading) return <Loading />;
  if (q.error) return <ErrorBox message={q.error.message} />;
  if (!q.data) return (<><PageHeader tieuDe="Không tìm thấy hoá đơn" /><p><Link to="/nhan-vien/ke-toan/hoa-don">← Danh sách hoá đơn</Link></p></>);
  const ct = q.data;
  const h = ct.HoaDon!;
  const b = ct.PhieuDatPhong;
  const r = b?.ChiTietDatPhongs?.[0];
  const tt = h.TrangThai ?? 'Unpaid';

  return (
    <>
      <p className="small"><Link to="/nhan-vien/ke-toan/hoa-don">← Danh sách hoá đơn</Link></p>
      <PageHeader tieuDe={`Hoá đơn #${h.MaHoaDon}`}>
        <span className={`badge ${BADGE_HD[tt]}`}>{TRANG_THAI_HOA_DON[tt]}</span>
        <a href={`/in/hoa-don/${h.MaHoaDon}`} target="_blank" rel="noopener" className="btn btn-outline btn-sm">In / PDF</a>
      </PageHeader>
      <div className="giao-ca-luoi">
        <div>
          <section className="the-trang">
            <div className="the-dau"><h2>Thông tin lưu trú</h2></div>
            <dl className="kt-dl">
              <dt>Khách hàng</dt><dd>{b?.KhachHang?.HoTen} ({b?.KhachHang?.CCCD_Passport})</dd>
              <dt>Phiếu đặt phòng</dt><dd>{b?.MaDatPhong}</dd>
              <dt>Phòng</dt><dd>{r?.Phong?.SoPhong} — {r?.Phong?.LoaiPhong?.TenLoai} ({so(r?.GiaThueThucTe)} ₫/đêm)</dd>
              <dt>Nhận phòng thực tế</dt><dd>{b?.NgayCheckInThucTe ? ngayGio(b.NgayCheckInThucTe) : '—'}</dd>
              <dt>Trả phòng thực tế</dt><dd>{b?.NgayCheckOutThucTe ? ngayGio(b.NgayCheckOutThucTe) : '—'}</dd>
              <dt>Người lập hoá đơn</dt><dd>{ct.TenNhanVienLap} — {ngayGio(h.NgayLapHoaDon)}</dd>
            </dl>
          </section>
          {h.MaSoThue && (
            <section className="the-trang kt-vat">
              <div className="the-dau"><h2>Xuất hoá đơn VAT cho công ty</h2></div>
              <dl className="kt-dl">
                <dt>Đơn vị</dt><dd>{h.TenCongTy}</dd>
                <dt>Mã số thuế</dt><dd><strong>{h.MaSoThue}</strong></dd>
                <dt>Địa chỉ</dt><dd>{h.DiaChiCongTy ?? '—'}</dd>
                <dt>Email nhận</dt><dd>{h.EmailHoaDon ?? '—'}</dd>
              </dl>
            </section>
          )}
          <section className="the-trang the-bang">
            <div className="the-dau"><h2>Giao dịch thu / hoàn</h2></div>
            <div className="bang-cuon">
              <table className="bang" aria-label="Giao dịch thu / hoàn">
                <thead><tr><th>Mã GD</th><th>Loại</th><th>Thời gian</th><th>Phương thức</th><th>Nhân viên</th><th className="canh-phai">Số tiền</th></tr></thead>
                <tbody>
                  {(b?.GiaoDichCocs ?? []).map((g) => {
                    const hoan = g.LoaiGiaoDich === 'Refund';
                    return (
                      <tr key={`c${g.MaGiaoDichCoc}`} data-loai="coc">
                        <td>—</td><td><span className={`badge ${hoan ? 'badge-warn' : 'badge-muted'}`}>{hoan ? 'Hoàn cọc' : 'Cọc'}</span></td>
                        <td className="small nowrap">{ngayGio(g.ThoiGian)}</td><td>{g.PhuongThuc}{g.GhiChu && <div className="small muted">{g.GhiChu}</div>}</td>
                        <td>{g.TenNhanVien ?? g.MaNhanVien}</td><td className={`canh-phai nowrap${hoan ? ' chu-loi' : ''}`}>{hoan ? '-' : ''}{so(g.SoTien)}</td>
                      </tr>
                    );
                  })}
                  {(ct.GiaoDich ?? []).map((t) => {
                    const hoan = t.LoaiGiaoDich === 'Refund';
                    return (
                      <tr key={t.MaThanhToan} data-loai={hoan ? 'hoan' : 'thu'}>
                        <td>{t.MaThanhToan}</td><td><span className={`badge ${hoan ? 'badge-danger' : 'badge-ok'}`}>{hoan ? 'Hoàn' : 'Thu'}</span></td>
                        <td className="small nowrap">{ngayGio(t.NgayThanhToan)}</td><td>{t.PhuongThuc}{t.GhiChu && <div className="small muted">{t.GhiChu}</div>}</td>
                        <td>{t.TenNhanVien ?? t.MaNhanVien}</td><td className={`canh-phai nowrap${hoan ? ' chu-loi' : ''}`}><strong>{hoan ? '-' : ''}{so(t.SoTienDaTra)}</strong></td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </section>
        </div>
        <div>
          <section className="so-cai kt-so-cai" aria-label="Chi tiết hoá đơn">
            <h2>Chi tiết hoá đơn</h2>
            <div className="kt-dong"><span className="muted">Tiền phòng ({h.SoDem} đêm)</span><strong>{so(h.TongTienPhong)}</strong></div>
            <div className="kt-dong"><span className="muted">Tiền dịch vụ</span><strong>{so(h.TongTienDichVu)}</strong></div>
            {(h.TienPhat ?? 0) > 0 && <div className="kt-dong chu-loi"><span>Phụ thu trả muộn ({h.PhanTramPhuThu}%)</span><strong>{so(h.TienPhat)}</strong></div>}
            {(h.GiamGiaKhuyenMai ?? 0) > 0 && <div className="kt-dong chu-ok"><span>Khuyến mãi ({h.MaKhuyenMai})</span><strong>-{so(h.GiamGiaKhuyenMai)}</strong></div>}
            {(h.GiamGiaThanhVien ?? 0) > 0 && <div className="kt-dong chu-ok"><span>Ưu đãi {h.HangThanhVien}</span><strong>-{so(h.GiamGiaThanhVien)}</strong></div>}
            <div className="kt-dong"><span className="muted">VAT 8%</span><strong>{so(h.ThueVAT)}</strong></div>
            <hr />
            <div className="kt-dong" data-kt="tong"><strong>Tổng giá trị</strong><strong>{so(h.TongTienCanThanhToan)}</strong></div>
            {(ct.CocDaTru ?? 0) > 0 && <div className="kt-dong"><span className="muted">Cọc trừ vào hoá đơn</span><strong>{so(ct.CocDaTru)}</strong></div>}
            {(ct.CocThua ?? 0) > 0 && <div className="kt-dong small muted"><span>Cọc thừa đã hoàn lại khách</span><span>{so(ct.CocThua)}</span></div>}
            <div className="kt-dong" data-kt="da-thu"><span className="muted">Đã thu (gồm cọc)</span><strong>{so((ct.DaThu ?? 0) + (ct.CocDaTru ?? 0))}</strong></div>
            <div className="kt-dong chu-loi" data-kt="da-hoan"><span>Đã hoàn</span><strong>{(ct.DaHoan ?? 0) > 0 ? `-${so(ct.DaHoan)}` : '0'}</strong></div>
          </section>
          {tt === 'Paid' && <HoanTien ma={h.MaHoaDon!} coTheHoan={ct.CoTheHoan ?? 0} />}
        </div>
      </div>
    </>
  );
}

function HoanTien({ ma, coTheHoan }: { ma: string; coTheHoan: number }) {
  const xacNhan = useXacNhan();
  const [f, setF] = useState({ soTien: '', phuongThuc: PHUONG_THUC_QUAY[0], lyDo: '' });
  const hoan = useThaoTac({
    mutationFn: () => accountingApi.refund(ma, Number(f.soTien), f.phuongThuc, f.lyDo.trim()), lamMoi: [['invoices'], ['reports']],
    thanhCong: () => `Đã hoàn ${so(Number(f.soTien))}đ cho hoá đơn ${ma}.`, tienToLoi: 'Không thể hoàn tiền. ',
    onSuccess: () => setF({ soTien: '', phuongThuc: PHUONG_THUC_QUAY[0], lyDo: '' })
  });
  const gui = async (e: FormEvent) => {
    e.preventDefault();
    if (await xacNhan({ tieuDe: 'Xác nhận hoàn tiền cho khách?', noiDung: `Hoàn ${so(Number(f.soTien))}đ (${f.phuongThuc}) cho hoá đơn ${ma}.`, nutXacNhan: 'Hoàn tiền', nguyHiem: true })) hoan.mutate();
  };
  return (
    <section className="the-trang kt-hoan">
      <div className="the-dau"><h2>Hoàn tiền cho khách</h2></div>
      {coTheHoan <= 0 ? <p className="muted small">Hoá đơn đã được hoàn toàn bộ.</p> : (
        <form className="chot-ca" onSubmit={gui} aria-label="Hoàn tiền">
          <label className="field"><span>Số tiền (tối đa {so(coTheHoan)}đ)</span><input type="number" min={1000} max={coTheHoan} step={1000} required value={f.soTien} onChange={(e) => setF({ ...f, soTien: e.target.value })} /></label>
          <label className="field"><span>Phương thức</span><select value={f.phuongThuc} onChange={(e) => setF({ ...f, phuongThuc: e.target.value })}>{PHUONG_THUC_QUAY.map((x) => <option key={x}>{x}</option>)}</select></label>
          <label className="field"><span>Lý do</span><input maxLength={200} required value={f.lyDo} placeholder="vd: Khách phàn nàn dịch vụ" onChange={(e) => setF({ ...f, lyDo: e.target.value })} /></label>
          <button type="submit" className="btn btn-outline-danger w-100" disabled={hoan.isPending}>Hoàn tiền</button>
        </form>
      )}
    </section>
  );
}

// Hoá đơn VAT công ty (Accounting/VatInvoices): ghi số hoá đơn điện tử đã xuất trên hệ thống thuế
export function VatInvoicesPage() {
  const [params] = useSearchParams();
  const chuaXuat = params.get('chuaXuat') === '1' || params.get('chuaXuat') === 'true';
  const q = useQuery({ queryKey: ['invoices', 'vat'], queryFn: () => accountingApi.invoices({ trangThai: 'Paid', congTy: true }) });
  const ds = (q.data ?? []).filter((h) => !chuaXuat || !h.SoHoaDonDienTu);
  return (
    <>
      <PageHeader tieuDe="Hoá đơn VAT công ty" phu="Hoá đơn đã thanh toán có thông tin công ty (lễ tân nhập khi trả phòng). Kế toán xuất hoá đơn điện tử trên hệ thống thuế rồi ghi lại số hoá đơn tại đây." />
      <nav className="loc-chip" aria-label="Lọc hoá đơn VAT">
        <Link to="/nhan-vien/ke-toan/vat" className={!chuaXuat ? 'active' : ''} aria-current={!chuaXuat ? 'page' : undefined}>Tất cả</Link>
        <Link to="/nhan-vien/ke-toan/vat?chuaXuat=1" className={chuaXuat ? 'active' : ''} aria-current={chuaXuat ? 'page' : undefined}>Chưa ghi số</Link>
      </nav>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : !ds.length ? <EmptyState>{chuaXuat ? 'Mọi hoá đơn công ty đã được ghi số.' : 'Chưa có hoá đơn công ty.'}</EmptyState> : (
        <section className="the-trang the-bang">
          <div className="bang-cuon">
            <table className="bang" aria-label="Hoá đơn VAT công ty">
              <thead><tr><th>Hoá đơn</th><th>Công ty</th><th>Mã số thuế</th><th className="canh-phai">Tổng tiền</th><th className="kt-cot-hddt">Số hoá đơn điện tử</th></tr></thead>
              <tbody>{ds.map((h) => <DongVat key={h.MaHoaDon} h={h} />)}</tbody>
            </table>
          </div>
        </section>
      )}
    </>
  );
}

function DongVat({ h }: { h: InvoiceListItem }) {
  const [soHd, setSoHd] = useState(h.SoHoaDonDienTu ?? '');
  const luu = useThaoTac({
    mutationFn: () => accountingApi.setEInvoice(h.MaHoaDon!, soHd.trim() || null), lamMoi: [['invoices'], ['reports']], tienToLoi: 'Không ghi được số hoá đơn điện tử. ',
    thanhCong: () => (soHd.trim() ? `Đã ghi số hoá đơn điện tử ${soHd.trim()} cho ${h.MaHoaDon}.` : `Đã bỏ số hoá đơn điện tử của ${h.MaHoaDon}.`)
  });
  return (
    <tr data-hoa-don={h.MaHoaDon}>
      <td><Link to={`/nhan-vien/ke-toan/hoa-don/${h.MaHoaDon}`}><strong>{h.MaHoaDon}</strong></Link><div className="small muted">{ngay(h.NgayLapHoaDon)} · {h.HoTenKhachHang}</div></td>
      <td>{h.TenCongTy}</td>
      <td><strong>{h.MaSoThue}</strong></td>
      <td className="canh-phai nowrap">{so(h.TongTienCanThanhToan)}</td>
      <td>
        <form className="o-ghep" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
          <input maxLength={30} value={soHd} placeholder="VD: 1C26TAA-0000123" aria-label={`Số hoá đơn điện tử ${h.MaHoaDon}`} onChange={(e) => setSoHd(e.target.value)} />
          <button type="submit" className={`btn btn-sm ${h.SoHoaDonDienTu ? 'btn-outline' : 'btn-gold'}`} disabled={luu.isPending}>Lưu</button>
        </form>
        {h.NgayXuatHoaDonDienTu && <small className="chu-ok">✓ Ghi lúc {ngayGio(h.NgayXuatHoaDonDienTu).slice(11)} {ngay(h.NgayXuatHoaDonDienTu)}</small>}
      </td>
    </tr>
  );
}
