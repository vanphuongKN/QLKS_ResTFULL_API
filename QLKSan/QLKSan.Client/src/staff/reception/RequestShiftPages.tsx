import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { frontDeskApi, guestRequestApi, LOAI_YEU_CAU, TRANG_THAI_YEU_CAU, type GuestRequest, type GuestRequestStatus } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { Modal, useXacNhan } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { ngay, so, tien } from '../../utils/format';
import './reception.css';

const LOC = [['mo', 'Đang mở'], ['xong', 'Đã xong (7 ngày)'], ['tatca', 'Tất cả (30 ngày)']] as const;
const BADGE: Record<GuestRequestStatus, string> = { New: 'badge-warn', InProgress: 'badge-info', Done: 'badge-ok', Cancelled: 'badge-muted' };
const gioNgay = (v?: string | null) => (v ? `${v.slice(11, 16)} ${ngay(v).slice(0, 5)}` : '');
const LAM_MOI = [['guest-requests'], ['front-desk']];

// Sổ yêu cầu khách (Reception/Requests của MVC): báo thức, đặt xe, nhắn gửi, đồ thất lạc… theo dõi tới khi xử lý xong
export function RequestsPage() {
  const [params] = useSearchParams();
  const loc = LOC.some(([k]) => k === params.get('loc')) ? params.get('loc')! : 'mo';
  const maDatPhong = params.get('maDatPhong');
  const ds = useQuery({ queryKey: ['guest-requests', loc], queryFn: () => guestRequestApi.list(loc === 'tatca' ? null : loc) });
  // Đi từ quầy lễ tân ("Ghi yêu cầu của khách") thì mở sẵn hộp ghi yêu cầu cho phiếu đó
  const [moGhi, setMoGhi] = useState(!!maDatPhong);
  const [capNhat, setCapNhat] = useState<{ y: GuestRequest; trangThai: GuestRequestStatus } | null>(null);
  const nhan = useThaoTac({
    mutationFn: (y: GuestRequest) => guestRequestApi.update(y.MaYeuCau!, 'InProgress', null), lamMoi: LAM_MOI,
    thanhCong: (_, y) => `Yêu cầu #${y.MaYeuCau}: ${TRANG_THAI_YEU_CAU.InProgress.toLowerCase()}.`, tienToLoi: 'Không cập nhật được yêu cầu. '
  });

  return (
    <>
      <PageHeader tieuDe="Sổ yêu cầu khách" phu="Báo thức, đặt xe / đưa đón, nhắn gửi, đồ thất lạc… Ghi lại để mọi ca cùng theo dõi tới khi xử lý xong.">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setMoGhi(true)}>+ Ghi yêu cầu</button>
      </PageHeader>
      <nav className="loc-chip" aria-label="Lọc yêu cầu">
        {LOC.map(([k, n]) => <Link key={k} to={`?loc=${k}`} className={loc === k ? 'active' : ''} aria-current={loc === k ? 'page' : undefined}>{n}</Link>)}
      </nav>
      <ErrorBox message={ds.error?.message} />
      {ds.isLoading ? <Loading /> : !ds.data?.length ? <EmptyState>{loc === 'mo' ? 'Không có yêu cầu nào đang chờ xử lý.' : 'Chưa có yêu cầu nào.'}</EmptyState> : (
        <div className="phieu-ds">
          {ds.data.map((y) => {
            const mo = y.TrangThai === 'New' || y.TrangThai === 'InProgress';
            return (
              <article key={y.MaYeuCau} className={`phieu${y.QuaHen ? ' phieu-khan' : ''}${mo ? '' : ' phieu-xong'}`} aria-label={`Yêu cầu #${y.MaYeuCau}`} data-yeu-cau={y.MaYeuCau}>
                <div className="phieu-chinh">
                  <div className="phieu-dau">
                    <strong>{y.Loai}</strong>
                    {y.SoPhong && <span>Phòng {y.SoPhong}</span>}
                    {y.HoTenKhachHang && <span className="muted small">{y.HoTenKhachHang}</span>}
                    <span className={`badge ${BADGE[y.TrangThai ?? 'New']}`}>{TRANG_THAI_YEU_CAU[y.TrangThai ?? 'New']}</span>
                    {y.QuaHen && <span className="badge badge-danger">Quá giờ hẹn</span>}
                  </div>
                  <p className="phieu-mo-ta">{y.NoiDung}</p>
                  <p className="phieu-meta small muted">
                    {y.ThoiGianHen && <span>Hẹn {gioNgay(y.ThoiGianHen)}</span>}
                    <span>#{y.MaYeuCau} · {gioNgay(y.ThoiGianTao)} · {y.TenNhanVienTao}</span>
                    {y.ThoiGianXuLy && <span>{gioNgay(y.ThoiGianXuLy)} · {y.TenNhanVienXuLy}</span>}
                    {y.KetQua && <span className="phieu-ket-qua">{y.KetQua}</span>}
                  </p>
                </div>
                {mo && (
                  <div className="phieu-nut">
                    {y.TrangThai === 'New' && <button type="button" className="btn btn-outline btn-sm" disabled={nhan.isPending} onClick={() => nhan.mutate(y)}>Nhận xử lý</button>}
                    <button type="button" className="btn btn-gold btn-sm" onClick={() => setCapNhat({ y, trangThai: 'Done' })}>Hoàn thành</button>
                    <button type="button" className="btn btn-outline-danger btn-sm" aria-label={`Huỷ yêu cầu #${y.MaYeuCau}`} onClick={() => setCapNhat({ y, trangThai: 'Cancelled' })}>Huỷ</button>
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}
      {moGhi && <HopGhiYeuCau maDatPhong={maDatPhong} onDong={() => setMoGhi(false)} />}
      {capNhat && <HopCapNhatYeuCau {...capNhat} onDong={() => setCapNhat(null)} />}
    </>
  );
}

function HopGhiYeuCau({ maDatPhong, onDong }: { maDatPhong: string | null; onDong: () => void }) {
  const o = useQuery({ queryKey: ['bookings', 'active'], queryFn: frontDeskApi.activeStays });
  const [f, setF] = useState({ MaDatPhong: maDatPhong ?? '', Loai: LOAI_YEU_CAU[0], ThoiGianHen: '', NoiDung: '' });
  const ghi = useThaoTac({
    mutationFn: () => guestRequestApi.create({ MaDatPhong: f.MaDatPhong || null, Loai: f.Loai, NoiDung: f.NoiDung.trim(), ThoiGianHen: f.ThoiGianHen || null }), lamMoi: LAM_MOI,
    thanhCong: (y) => `Đã ghi yêu cầu #${y.MaYeuCau} (${y.Loai}${y.SoPhong ? `, phòng ${y.SoPhong}` : ''}).`, tienToLoi: 'Không ghi được yêu cầu. ', onSuccess: onDong
  });
  const phong = (o.data ?? []).map((s) => ({ ma: s.MaDatPhong!, so: s.ChiTietDatPhongs?.[0]?.Phong?.SoPhong ?? '', ten: `${s.KhachHang?.Ho ?? ''} ${s.KhachHang?.Ten ?? ''}`.trim() }))
    .sort((a, b) => a.so.localeCompare(b.so, 'vi', { numeric: true }));
  return (
    <Modal open title="Ghi yêu cầu của khách" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); ghi.mutate(); }}>
        <div className="form-grid">
          <label className="field span-2"><span>Khách đang ở</span>
            <select value={f.MaDatPhong} onChange={(e) => setF({ ...f, MaDatPhong: e.target.value })}>
              <option value="">— Không gắn phòng (khách ngoài, đồ thất lạc…) —</option>
              {phong.map((p) => <option key={p.ma} value={p.ma}>Phòng {p.so} — {p.ten}</option>)}
            </select>
          </label>
          <label className="field"><span>Loại</span><select value={f.Loai} onChange={(e) => setF({ ...f, Loai: e.target.value })}>{LOAI_YEU_CAU.map((x) => <option key={x}>{x}</option>)}</select></label>
          <label className="field"><span>Giờ hẹn (tuỳ chọn)</span><input type="datetime-local" value={f.ThoiGianHen} onChange={(e) => setF({ ...f, ThoiGianHen: e.target.value })} /></label>
          <label className="field span-2"><span>Nội dung</span><textarea rows={3} maxLength={500} required value={f.NoiDung} placeholder="VD: Báo thức 5:30, đặt taxi ra sân bay 6:15, 2 vali" onChange={(e) => setF({ ...f, NoiDung: e.target.value })} /></label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={ghi.isPending}>Ghi yêu cầu</button></div>
      </form>
    </Modal>
  );
}

function HopCapNhatYeuCau({ y, trangThai, onDong }: { y: GuestRequest; trangThai: GuestRequestStatus; onDong: () => void }) {
  const huy = trangThai === 'Cancelled';
  const [ketQua, setKetQua] = useState('');
  const luu = useThaoTac({
    mutationFn: () => guestRequestApi.update(y.MaYeuCau!, trangThai, ketQua.trim() || null), lamMoi: LAM_MOI,
    thanhCong: `Yêu cầu #${y.MaYeuCau}: ${TRANG_THAI_YEU_CAU[trangThai].toLowerCase()}.`, tienToLoi: 'Không cập nhật được yêu cầu. ', onSuccess: onDong
  });
  return (
    <Modal open title={`${huy ? 'Huỷ' : 'Hoàn thành'} yêu cầu #${y.MaYeuCau}`} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <label className="field"><span>Kết quả / lý do</span>
          <textarea rows={2} maxLength={500} required={huy} value={ketQua} placeholder="VD: Đã gọi báo thức, khách xác nhận" onChange={(e) => setKetQua(e.target.value)} autoFocus />
          <small className="muted">{huy ? 'Bắt buộc nhập lý do huỷ.' : 'Không bắt buộc.'}</small>
        </label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu</button></div>
      </form>
    </Modal>
  );
}

// Giao ca & đối soát tiền (Reception/Shift của MVC): tiền trong ca của nhân viên đang đăng nhập, chốt ca + ghi chú bàn giao
export function ShiftPage() {
  const xacNhan = useXacNhan();
  const ca = useQuery({ queryKey: ['shift'], queryFn: frontDeskApi.shift });
  const bg = useQuery({ queryKey: ['shift', 'handovers'], queryFn: () => frontDeskApi.handovers(7) });
  const [thucTe, setThucTe] = useState<number | null>(null);
  const [ghiChu, setGhiChu] = useState('');
  const chot = useThaoTac({
    mutationFn: () => frontDeskApi.closeShift(thucTe ?? ca.data?.TienMatHeThong ?? 0, ghiChu.trim() || null), lamMoi: [['shift'], ['front-desk']], tienToLoi: 'Không chốt được ca. ',
    thanhCong: (h) => `Đã chốt ca ${h.BatDau?.slice(11, 16)} – ${h.KetThuc?.slice(11, 16)}: tiền mặt hệ thống ${so(h.TienMatHeThong)}đ, thực tế ${so(h.TienMatThucTe)}đ`
      + (h.ChenhLech === 0 ? ' — khớp.' : ` — chênh lệch ${so(h.ChenhLech)}đ.`),
    onSuccess: () => { setThucTe(null); setGhiChu(''); }
  });
  if (ca.isLoading) return <Loading />;
  const s = ca.data;
  if (!s) return <ErrorBox message={ca.error?.message ?? 'Không tải được ca làm việc.'} />;
  const tm = thucTe ?? Math.round(s.TienMatHeThong ?? 0);
  const lech = Math.round(tm) - Math.round(s.TienMatHeThong ?? 0);
  const guiChot = async () => { if (await xacNhan({ tieuDe: 'Chốt ca và lưu bàn giao?', nutXacNhan: 'Chốt ca & bàn giao' })) chot.mutate(); };

  return (
    <>
      <PageHeader tieuDe="Giao ca & đối soát tiền" phu={`${s.TenNhanVien} · từ ${gioNgay(s.TuLuc)} đến ${gioNgay(s.DenLuc)}. Gồm tiền thanh toán hoá đơn, hoàn tiền, thu và hoàn cọc do bạn thực hiện (không tính phần trả bằng điểm thưởng).`} />
      <div className="kpi-luoi">
        <div className="kpi kpi-toi"><span>Tiền mặt trong két (hệ thống)</span><strong className="kpi-nho">{tien(s.TienMatHeThong)}</strong><small>Thu trừ chi bằng tiền mặt trong ca</small></div>
        <div className="kpi"><span>Tổng thu</span><strong className="kpi-nho">{tien(s.TongThu)}</strong><small>Mọi phương thức</small></div>
        <div className="kpi"><span>Tổng chi trả khách</span><strong className="kpi-nho">{tien(s.TongChi)}</strong><small>Hoàn tiền, hoàn cọc</small></div>
        <div className="kpi"><span>Thực thu ròng</span><strong className="kpi-nho">{tien(s.TongThuRong)}</strong><small>{s.GiaoDich?.length ?? 0} giao dịch</small></div>
      </div>
      <div className="giao-ca-luoi">
        <section className="the-trang">
          <h2 className="quay-h">Theo phương thức</h2>
          <div className="bang-cuon"><table className="bang">
            <thead><tr><th>Phương thức</th><th className="canh-giua">Giao dịch</th><th className="canh-phai">Thu</th><th className="canh-phai">Chi</th><th className="canh-phai">Ròng</th></tr></thead>
            <tbody>
              {!s.TheoPhuongThuc?.length && <tr><td colSpan={5} className="bang-trong">Chưa có giao dịch trong ca.</td></tr>}
              {s.TheoPhuongThuc?.map((m) => <tr key={m.PhuongThuc}><td><strong>{m.PhuongThuc}</strong></td><td className="canh-giua">{m.SoGiaoDich}</td><td className="canh-phai">{so(m.Thu)}</td><td className="canh-phai chu-loi">{(m.Chi ?? 0) > 0 ? `-${so(m.Chi)}` : '0'}</td><td className="canh-phai"><strong>{so(m.Rong)}</strong></td></tr>)}
            </tbody>
          </table></div>
          <h2 className="quay-h gc-tieu-de">Chi tiết giao dịch</h2>
          <div className="bang-cuon"><table className="bang">
            <thead><tr><th>Giờ</th><th>Loại</th><th>Mã</th><th>Phương thức</th><th className="canh-phai">Số tiền</th></tr></thead>
            <tbody>
              {!s.GiaoDich?.length && <tr><td colSpan={5} className="bang-trong">—</td></tr>}
              {s.GiaoDich?.map((g, i) => <tr key={i} title={g.GhiChu ?? ''}><td>{g.ThoiGian?.slice(11, 16)}</td><td>{g.Loai}</td><td className="muted">{g.MaThamChieu}</td><td>{g.PhuongThuc}</td><td className={`canh-phai${(g.SoTien ?? 0) < 0 ? ' chu-loi' : ''}`}><strong>{so(g.SoTien)}</strong></td></tr>)}
            </tbody>
          </table></div>
        </section>
        <form className="the-trang chot-ca" onSubmit={(e) => { e.preventDefault(); void guiChot(); }} aria-label="Chốt ca">
          <h2 className="quay-h">Chốt ca</h2>
          <label className="field"><span>Tiền mặt đếm thực tế</span><input type="number" min={0} step={1000} required value={tm} onChange={(e) => setThucTe(e.target.valueAsNumber || 0)} /></label>
          <small className={lech === 0 ? 'chu-ok' : 'chu-loi'} role="status">{lech === 0 ? 'Khớp với hệ thống.' : `Chênh lệch ${lech > 0 ? '+' : ''}${so(lech)} ₫ so với hệ thống.`}</small>
          <label className="field"><span>Ghi chú bàn giao cho ca sau</span><textarea rows={5} maxLength={1000} value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} placeholder="VD: Phòng 202 hẹn trả muộn 14:00; khách 301 chờ xe sân bay 6:15; két còn 2 triệu tiền lẻ." /></label>
          <button type="submit" className="btn btn-gold" disabled={chot.isPending}>Chốt ca &amp; bàn giao</button>
        </form>
      </div>
      <section className="the-trang">
        <h2 className="quay-h">Bàn giao 7 ngày gần đây</h2>
        <div className="bang-cuon"><table className="bang">
          <thead><tr><th>Ca</th><th>Nhân viên</th><th className="canh-phai">Thu ròng</th><th className="canh-phai">Tiền mặt HT / thực tế</th><th className="canh-phai">Chênh lệch</th><th>Ghi chú bàn giao</th></tr></thead>
          <tbody>
            {!bg.data?.length && <tr><td colSpan={6} className="bang-trong">Chưa có lần chốt ca nào.</td></tr>}
            {bg.data?.map((h) => (
              <tr key={h.MaGiaoCa}>
                <td className="nowrap">{h.BatDau?.slice(11, 16)} – {gioNgay(h.KetThuc)}</td>
                <td>{h.TenNhanVien}</td>
                <td className="canh-phai">{so(h.TongThuRong)}</td>
                <td className="canh-phai small">{so(h.TienMatHeThong)} / {so(h.TienMatThucTe)}</td>
                <td className={`canh-phai ${h.ChenhLech === 0 ? 'chu-ok' : 'chu-loi'}`}><strong>{h.ChenhLech === 0 ? 'Khớp' : so(h.ChenhLech)}</strong></td>
                <td className="small ban-giao-ghi-chu">{h.GhiChuBanGiao}</td>
              </tr>
            ))}
          </tbody>
        </table></div>
      </section>
    </>
  );
}
