import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { accountingApi, TEN_TRANG_THAI_PHIEU, type ShiftMethod } from '../../api/staff';
import { ErrorBox, Loading } from '../../components/common';
import { BoLocNgay, NutTaiTep, PageHeader, useKhoangNgay } from '../../components/ui/Page';
import { ngay, so, so1, tien } from '../../utils/format';
import { thangNay } from './AccountingPages';
import './accounting.css';

const gioNgay = (v?: string) => (v ? `${v.slice(11, 16)} ${v.slice(8, 10)}/${v.slice(5, 7)}` : '');
export const theoPhuongThuc = (ds?: ShiftMethod[] | null) => (ds ?? []).map((m) => `${m.PhuongThuc}: ${so(m.Rong)}`).join(' · ');

// Đối soát (Accounting/Reconciliation): giao ca lễ tân (tiền mặt hệ thống / thực tế) và sổ cọc
export function ReconciliationPage() {
  const [k, setK] = useKhoangNgay(thangNay);
  const ca = useQuery({ queryKey: ['reports', 'shifts', k], queryFn: () => accountingApi.shifts(k.tuNgay, k.denNgay) });
  const coc = useQuery({ queryKey: ['reports', 'deposits', k], queryFn: () => accountingApi.deposits(k.tuNgay, k.denNgay) });
  const shifts = ca.data ?? [];
  const d = coc.data;
  const gd = d?.GiaoDich ?? [];
  const thu = gd.filter((g) => g.LoaiGiaoDich === 'Payment');
  const hoan = gd.filter((g) => g.LoaiGiaoDich === 'Refund');
  const cong = (ds: { SoTien?: number }[]) => ds.reduce((t, g) => t + (g.SoTien ?? 0), 0);

  return (
    <>
      <PageHeader tieuDe="Đối soát giao ca & sổ cọc"
        phu={`${ngay(k.tuNgay)} – ${ngay(k.denNgay)}. Giao ca: tiền mặt hệ thống so với tiền lễ tân đếm khi chốt ca. Sổ cọc: tiền khách đặt trước khách sạn đang giữ và các lần thu / hoàn cọc.`} />
      <BoLocNgay key={`${k.tuNgay}${k.denNgay}`} value={k} onChange={setK} />
      <ErrorBox message={ca.error?.message ?? coc.error?.message} />
      {ca.isLoading || coc.isLoading ? <Loading /> : (
        <>
          <section className="the-trang the-bang">
            <div className="the-dau"><h2>Giao ca lễ tân ({shifts.length})</h2>
              <small className="muted">{shifts.filter((s) => s.ChenhLech).length} ca lệch · tổng lệch {so(shifts.reduce((t, s) => t + (s.ChenhLech ?? 0), 0))} ₫</small></div>
            <div className="bang-cuon">
              <table className="bang" aria-label="Giao ca lễ tân">
                <thead><tr><th>Ca</th><th>Nhân viên</th><th>Theo phương thức</th><th className="canh-phai">Thu ròng</th><th className="canh-phai">Tiền mặt HT / thực tế</th><th className="canh-phai">Chênh lệch</th><th>Bàn giao</th></tr></thead>
                <tbody>
                  {!shifts.length && <tr><td colSpan={7} className="canh-giua muted">Chưa có lần chốt ca nào trong kỳ.</td></tr>}
                  {shifts.map((s) => (
                    <tr key={s.MaGiaoCa} className={s.ChenhLech ? 'kt-lech' : ''} data-ca={s.MaGiaoCa}>
                      <td className="small nowrap">{gioNgay(s.BatDau)} – {gioNgay(s.KetThuc)}</td>
                      <td>{s.TenNhanVien}</td>
                      <td className="small">{theoPhuongThuc(s.TheoPhuongThuc)}</td>
                      <td className="canh-phai nowrap">{so(s.TongThuRong)}</td>
                      <td className="canh-phai small nowrap">{so(s.TienMatHeThong)} / {so(s.TienMatThucTe)}</td>
                      <td className={`canh-phai nowrap ${s.ChenhLech ? 'chu-loi' : 'chu-ok'}`}><strong>{s.ChenhLech ? so(s.ChenhLech) : 'Khớp'}</strong></td>
                      <td className="small">{s.GhiChuBanGiao}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          {d && (
            <>
              <div className="kpi-luoi">
                <div className="kpi kpi-toi"><span>Cọc đang giữ</span><strong className="kpi-nho">{tien(d.TongDangGiu)}</strong><small>{d.DangGiu?.length ?? 0} phiếu chưa trả phòng</small></div>
                <div className="kpi"><span>Thu cọc trong kỳ</span><strong className="kpi-nho">{tien(cong(thu))}</strong><small>{thu.length} lần thu</small></div>
                <div className="kpi"><span>Hoàn cọc trong kỳ</span><strong className="kpi-nho">{tien(cong(hoan))}</strong><small>Huỷ phiếu, cọc thừa khi trả phòng</small></div>
                <div className="kpi"><span>Giữ lại khi khách huỷ</span><strong className="kpi-nho">{tien(d.TongGiuLai)}</strong><small>Phiếu huỷ có ngày nhận dự kiến trong kỳ · chưa vào doanh thu</small></div>
              </div>
              <div className="kt-luoi">
                <div>
                  <section className="the-trang the-bang">
                    <div className="the-dau"><h2>Cọc đang giữ</h2></div>
                    <div className="bang-cuon">
                      <table className="bang" aria-label="Cọc đang giữ">
                        <thead><tr><th>Phiếu</th><th>Ngày nhận</th><th className="canh-phai">Tiền cọc</th></tr></thead>
                        <tbody>
                          {!d.DangGiu?.length && <tr><td colSpan={3} className="canh-giua muted">Không có.</td></tr>}
                          {(d.DangGiu ?? []).map((c) => (
                            <tr key={c.MaDatPhong}><td><strong>{c.MaDatPhong}</strong><div className="small muted">{c.HoTenKhachHang}{c.SoPhong ? ` · P${c.SoPhong}` : ''} · {TEN_TRANG_THAI_PHIEU[c.TrangThai ?? 'Pending']}</div></td>
                              <td className="small nowrap">{ngay(c.NgayNhan)}</td><td className="canh-phai nowrap"><strong>{so(c.TienCoc)}</strong></td></tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </section>
                  {!!d.GiuLaiKhiHuy?.length && (
                    <section className="the-trang the-bang">
                      <div className="the-dau"><h2>Cọc giữ lại khi khách huỷ</h2></div>
                      <div className="bang-cuon">
                        <table className="bang" aria-label="Cọc giữ lại khi khách huỷ">
                          <thead><tr><th>Phiếu</th><th>Ngày nhận dự kiến</th><th className="canh-phai">Tiền cọc</th></tr></thead>
                          <tbody>{d.GiuLaiKhiHuy.map((c) => <tr key={c.MaDatPhong}><td><strong>{c.MaDatPhong}</strong><div className="small muted">{c.HoTenKhachHang}</div></td><td className="small">{ngay(c.NgayNhan)}</td><td className="canh-phai">{so(c.TienCoc)}</td></tr>)}</tbody>
                        </table>
                      </div>
                    </section>
                  )}
                </div>
                <section className="the-trang the-bang">
                  <div className="the-dau"><h2>Thu / hoàn cọc trong kỳ</h2><small className="muted">{theoPhuongThuc(d.TheoPhuongThuc)}</small></div>
                  <div className="bang-cuon">
                    <table className="bang" aria-label="Thu / hoàn cọc trong kỳ">
                      <thead><tr><th>Thời gian</th><th>Phiếu</th><th>Loại</th><th>Phương thức</th><th>Người thu</th><th className="canh-phai">Số tiền</th></tr></thead>
                      <tbody>
                        {!gd.length && <tr><td colSpan={6} className="canh-giua muted">Không có giao dịch cọc trong kỳ.</td></tr>}
                        {gd.map((g, i) => {
                          const laHoan = g.LoaiGiaoDich === 'Refund';
                          return (
                            <tr key={i} title={g.GhiChu ?? ''}>
                              <td className="small nowrap">{gioNgay(g.ThoiGian)}</td>
                              <td><strong>{g.MaDatPhong}</strong><div className="small muted">{g.HoTenKhachHang}</div></td>
                              <td><span className={`badge ${laHoan ? 'badge-warn' : 'badge-muted'}`}>{laHoan ? 'Hoàn' : 'Thu'}</span></td>
                              <td className="small">{g.PhuongThuc}</td><td className="small">{g.TenNhanVien}</td>
                              <td className={`canh-phai nowrap${laHoan ? ' chu-loi' : ''}`}><strong>{laHoan ? '-' : ''}{so(g.SoTien)}</strong></td>
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
      )}
    </>
  );
}

function BangNho({ ten, cot, dong }: { ten: string; cot: string[]; dong: (string | number | undefined)[][] }) {
  return (
    <section className="the-trang the-bang">
      <div className="the-dau"><h2>{ten}</h2></div>
      <div className="bang-cuon">
        <table className="bang" aria-label={ten}>
          <thead><tr>{cot.map((c, i) => <th key={c} className={i === cot.length - 1 ? 'canh-phai' : undefined}>{c}</th>)}</tr></thead>
          <tbody>
            {!dong.length && <tr><td colSpan={cot.length} className="canh-giua muted">Không có dữ liệu</td></tr>}
            {dong.map((d, i) => <tr key={i}>{d.map((x, j) => <td key={j} className={j === d.length - 1 ? 'canh-phai nowrap' : undefined}>{x}</td>)}</tr>)}
          </tbody>
        </table>
      </div>
    </section>
  );
}

// Báo cáo doanh thu & công suất phòng (Accounting/Reports), xuất Excel
export function ReportsPage() {
  const [k, setK] = useKhoangNgay(thangNay);
  const dt = useQuery({ queryKey: ['reports', 'revenue', k], queryFn: () => accountingApi.revenue(k.tuNgay, k.denNgay) });
  const cs = useQuery({ queryKey: ['reports', 'occupancy', k], queryFn: () => accountingApi.occupancy(k.tuNgay, k.denNgay) });
  const r = dt.data, occ = cs.data;
  const maxNgay = Math.max(1, ...(r?.TheoNgay ?? []).map((d) => d.DoanhThu ?? 0));
  const qs = `tuNgay=${k.tuNgay}&denNgay=${k.denNgay}`;

  return (
    <>
      <PageHeader tieuDe="Báo cáo doanh thu & công suất phòng" phu="Kế toán">
        <Link to={`/nhan-vien/chot-ngay?${qs}`} className="btn btn-outline btn-sm">Doanh thu theo đêm</Link>
        <NutTaiTep path={accountingApi.revenueExportPath(k.tuNgay, k.denNgay)} tenMacDinh={`BaoCaoDoanhThu_${k.tuNgay.replace(/-/g, '')}_${k.denNgay.replace(/-/g, '')}.xlsx`}>Xuất Excel</NutTaiTep>
      </PageHeader>
      <BoLocNgay key={`${k.tuNgay}${k.denNgay}`} value={k} onChange={setK} />
      <ErrorBox message={dt.error?.message ?? cs.error?.message} />
      {dt.isLoading || cs.isLoading || !r ? (dt.isLoading && <Loading />) : (
        <>
          <div className="kpi-luoi">
            <div className="kpi"><span>Doanh thu thuần</span><strong className="kpi-nho" data-kt="thuan">{tien(r.DoanhThuThuan)}</strong><small>{r.SoHoaDon} hoá đơn đã thanh toán</small></div>
            <div className="kpi"><span>Tổng doanh thu / Hoàn tiền</span><strong className="kpi-nho">{tien(r.TongDoanhThu)}</strong><small className="chu-loi">Hoàn: -{tien(r.TongHoanTien)}</small></div>
            <div className="kpi"><span>Công suất phòng TB</span><strong className="kpi-nho">{so1(occ?.TyLeTrungBinh)}%</strong><small>{occ?.TongDemPhongDaBan ?? 0} đêm phòng / {occ?.TongSoPhong ?? 0} phòng</small></div>
            <div className="kpi kt-co-cau"><span>Cơ cấu</span>
              <small>Phòng: <strong>{so(r.TienPhong)}</strong></small>
              <small>Dịch vụ: <strong>{so(r.TienDichVu)}</strong></small>
              <small>Phụ thu: <strong>{so(r.PhuThu)}</strong> · KM: <strong className="chu-ok">-{so(r.GiamGia)}</strong></small>
              <small>Ưu đãi hạng: <strong className="chu-ok">-{so(r.GiamGiaThanhVien)}</strong></small>
              <small>VAT: <strong>{so(r.ThueVAT)}</strong></small>
            </div>
          </div>
          <div className="kt-luoi kt-luoi-bc">
            <section className="the-trang the-bang">
              <div className="the-dau"><h2>Doanh thu &amp; công suất theo ngày</h2></div>
              <div className="bang-cuon">
                <table className="bang" aria-label="Doanh thu và công suất theo ngày">
                  <thead><tr><th>Ngày</th><th>HĐ</th><th className="kt-ty-trong">Doanh thu</th><th className="canh-phai">Hoàn</th><th className="canh-phai">Công suất</th></tr></thead>
                  <tbody>
                    {(r.TheoNgay ?? []).map((d) => {
                      const o = occ?.TheoNgay?.find((x) => x.Ngay?.slice(0, 10) === d.Ngay?.slice(0, 10));
                      return (
                        <tr key={d.Ngay} data-ngay={d.Ngay?.slice(0, 10)}>
                          <td className="small">{d.Ngay?.slice(8, 10)}/{d.Ngay?.slice(5, 7)}</td>
                          <td>{d.SoHoaDon}</td>
                          <td><div className="kt-cot-dt"><div className="kt-thanh kt-thanh-dac"><span style={{ width: `${Math.floor((d.DoanhThu ?? 0) * 100 / maxNgay)}%` }} /></div><small className="nowrap">{(d.DoanhThu ?? 0) > 0 ? so(d.DoanhThu) : ''}</small></div></td>
                          <td className="canh-phai chu-loi small">{(d.HoanTien ?? 0) > 0 ? `-${so(d.HoanTien)}` : ''}</td>
                          <td className="canh-phai small nowrap">{o?.SoPhongCoKhach ?? 0} phòng · {so1(o?.TyLe)}%</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </section>
            <div>
              <BangNho ten="Theo loại phòng" cot={['Loại phòng', 'Lượt', 'Đêm', 'Tiền phòng']} dong={(r.TheoLoaiPhong ?? []).map((x) => [x.TenLoai ?? '', x.SoLuot, x.SoDem, so(x.DoanhThuPhong)])} />
              <BangNho ten="Theo dịch vụ" cot={['Dịch vụ', 'SL', 'Doanh thu']} dong={(r.TheoDichVu ?? []).map((x) => [x.TenDichVu ?? '', x.SoLuong, so(x.DoanhThu)])} />
              <BangNho ten="Theo phương thức thanh toán" cot={['Phương thức', 'GD', 'Số tiền']} dong={(r.TheoPhuongThuc ?? []).map((x) => [x.PhuongThuc ?? '', x.SoGiaoDich, so(x.SoTien)])} />
              <BangNho ten="Theo kênh đặt phòng" cot={['Kênh', 'Lượt', 'Đêm', 'Doanh thu']} dong={(r.TheoKenh ?? []).map((x) => [x.Kenh ?? '', x.SoLuot, x.SoDem, so(x.DoanhThu)])} />
              <BangNho ten="Đối soát theo nhân viên thu" cot={['Nhân viên', 'GD', 'Đã thu']} dong={(r.TheoNhanVien ?? []).map((x) => [x.TenNhanVien ?? '', x.SoGiaoDich, so(x.SoTienThu)])} />
            </div>
          </div>
        </>
      )}
    </>
  );
}
