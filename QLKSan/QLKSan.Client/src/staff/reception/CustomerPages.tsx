import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { bookingHistoryApi, customerApi, type BookingStatus, type CustomerDetail, type CustomerProfile, type StaffBooking } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { useAuth } from '../../auth/AuthContext';
import { ErrorBox, Loading } from '../../components/common';
import { Modal } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { ngay, ngayGio, so, tenHangPhong, tien } from '../../utils/format';
import './reception.css';

// Nhãn trạng thái phiếu như các trang lễ tân của MVC (Đang ở / Hoàn thành…)
const NHAN_PHIEU: Record<BookingStatus, { nhan: string; css: string }> = {
  Pending: { nhan: 'Chờ duyệt', css: 'badge-warn' },
  Approved: { nhan: 'Đã duyệt', css: 'badge-info' },
  CheckedIn: { nhan: 'Đang ở', css: 'badge-danger' },
  CheckedOut: { nhan: 'Hoàn thành', css: 'badge-ok' },
  Completed: { nhan: 'Hoàn thành', css: 'badge-ok' },
  Cancelled: { nhan: 'Đã hủy', css: 'badge-muted' }
};

// Ô tìm kiếm đẩy ?search= lên địa chỉ (giữ các tham số khác như ?vip=1)
function OTimKiem({ goiY, nhan }: { goiY: string; nhan: string }) {
  const [params, setParams] = useSearchParams();
  const [q, setQ] = useState(params.get('search') ?? '');
  const tim = (e: FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams(params);
    if (q.trim()) p.set('search', q.trim()); else p.delete('search');
    setParams(p);
  };
  return (
    <form className="tim-phieu" onSubmit={tim} role="search">
      <input type="search" value={q} onChange={(e) => setQ(e.target.value)} placeholder={goiY} aria-label={nhan} />
      <button type="submit" className="btn btn-gold btn-sm">Tìm kiếm</button>
    </form>
  );
}

// Hồ sơ khách hàng (Reception/Customers của MVC): tìm theo họ tên, CCCD, SĐT; lọc khách VIP
export function CustomersPage() {
  const [params, setParams] = useSearchParams();
  const search = params.get('search') ?? '';
  const vip = params.get('vip') === '1';
  const ds = useQuery({ queryKey: ['customers', search], queryFn: () => customerApi.list(search) });
  const dong = (ds.data ?? []).filter((k) => !vip || k.LaVIP);
  const doiVip = () => { const p = new URLSearchParams(params); if (vip) p.delete('vip'); else p.set('vip', '1'); setParams(p); };

  return (
    <>
      <PageHeader tieuDe="Hồ sơ khách hàng" phu="Lễ tân">
        <OTimKiem key={search} goiY="Họ tên khách, CCCD, SĐT..." nhan="Tìm khách hàng" />
        <button type="button" className={`btn btn-sm ${vip ? 'btn-gold' : 'btn-outline'}`} aria-pressed={vip} onClick={doiVip}>♛ VIP</button>
        {search && <Link to="/nhan-vien/khach-hang" className="btn btn-outline btn-sm">Xóa lọc</Link>}
      </PageHeader>
      <ErrorBox message={ds.error?.message} />
      {ds.isLoading ? <Loading /> : (
        <section className="the-trang">
          <div className="the-dau"><h2>Danh sách khách hàng</h2><span className="badge badge-muted">Tổng số: {dong.length} khách hàng</span></div>
          {!dong.length ? <EmptyState>Không tìm thấy khách hàng nào trong hệ thống phù hợp.</EmptyState> : (
            <div className="bang-cuon">
              <table className="bang" aria-label="Danh sách khách hàng">
                <thead><tr><th>Mã khách</th><th>Họ và tên</th><th>Số CCCD / Passport</th><th>Điện thoại liên hệ</th><th>Ngày sinh</th><th>Số lượt đặt phòng</th><th className="canh-phai">Hành động</th></tr></thead>
                <tbody>
                  {dong.map((k) => (
                    <tr key={k.MaKhachHang} data-khach={k.MaKhachHang}>
                      <td><strong>#{k.MaKhachHang}</strong></td>
                      <td>
                        <strong>{k.Ho} {k.Ten}</strong>
                        {k.LaVIP && <> <span className="the-vip">♛ VIP</span></>}
                        {k.HanChe && <> <span className="the-han-che" title={k.LyDoHanChe ?? ''}>Hạn chế</span></>}
                        <div className="small muted">{k.DiemTichLuy ?? 0} điểm · {k.ThanhVien?.Hang ?? 'Thành viên'}</div>
                      </td>
                      <td>{k.CCCD_Passport}</td>
                      <td className="muted">{k.LienHeDiaChis?.[0]?.SDT ?? 'Chưa cập nhật'}</td>
                      <td className="muted">{ngay(k.NgaySinh)}</td>
                      <td><span className={`badge ${k.SoLanDatPhong ? 'badge-gold' : 'badge-muted'}`}>{k.SoLanDatPhong ?? 0} chuyến đi</span></td>
                      <td className="canh-phai"><Link to={`/nhan-vien/khach-hang/${k.MaKhachHang}`} className="btn btn-outline btn-sm">Hồ sơ</Link></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </>
  );
}

// Chi tiết hồ sơ khách (Reception/CustomerDetails): KPI, chăm sóc khách, lịch sử kỳ nghỉ / điểm, sửa hồ sơ; điều chỉnh điểm chỉ Admin
export function CustomerDetailPage() {
  const { maKhachHang = '' } = useParams();
  const { session } = useAuth();
  const k = useQuery({ queryKey: ['customers', 'detail', maKhachHang], queryFn: () => customerApi.get(maKhachHang) });
  const diem = useQuery({ queryKey: ['customers', 'points', maKhachHang], queryFn: () => customerApi.points(maKhachHang) });
  const [suaHoSo, setSuaHoSo] = useState(false);

  if (k.isLoading) return <Loading />;
  if (k.error) return <ErrorBox message={k.error.message} />;
  if (!k.data) return (<><PageHeader tieuDe="Không tìm thấy khách hàng" /><p><Link to="/nhan-vien/khach-hang">← Khách hàng</Link></p></>);
  const c = k.data;
  const lh = c.LienHeDiaChis?.[0];
  const tv = c.ThanhVien;
  const phieu = [...(c.PhieuDatPhongs ?? [])].sort((a, b) => (b.NgayCheckInDuKien ?? '').localeCompare(a.NgayCheckInDuKien ?? ''));
  const daXong = phieu.filter((b) => b.TrangThai === 'CheckedOut' || b.TrangThai === 'Completed').length;
  const chiTieu = phieu.filter((b) => b.HoaDon?.TrangThai === 'Paid').reduce((t, b) => t + (b.HoaDon?.TongTienCanThanhToan ?? 0), 0);
  const ls = diem.data ?? [];

  return (
    <>
      <p className="small"><Link to="/nhan-vien/khach-hang">← Khách hàng</Link></p>
      <PageHeader tieuDe={c.HoTen ?? `${c.Ho} ${c.Ten}`}
        phu={<>#{c.MaKhachHang} · {tv?.Hang ?? 'Thành viên'}{c.LaVIP && <> · <span className="the-vip">♛ VIP</span></>}{!c.MaDangNhap && ' · khách vãng lai (chưa có tài khoản)'}</>}>
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setSuaHoSo(true)}>Sửa hồ sơ</button>
      </PageHeader>
      {c.HanChe && <div className="alert alert-danger" role="alert"><strong>Hạn chế đặt phòng trực tuyến:</strong> {c.LyDoHanChe}</div>}
      <div className="kpi-luoi">
        <div className="kpi"><span>Kỳ nghỉ đã hoàn thành</span><strong>{daXong}</strong><small>{c.SoLanDatPhong} phiếu đặt</small></div>
        <div className="kpi"><span>Tổng chi tiêu</span><strong className="kpi-nho">{tien(chiTieu)}</strong><small>hoá đơn đã thanh toán</small></div>
        <div className="kpi kpi-toi"><span>Điểm hiện có</span><strong>{so(c.DiemTichLuy)}</strong><small>đã đổi {so(c.DiemDaDoi)} điểm</small></div>
        <div className="kpi"><span>Hạng thành viên</span><strong className="kpi-nho">{tv?.Hang ?? '—'}</strong><small>{tv?.HangKeTiep ? `còn ${so(tv.DiemLenHang)} điểm lên ${tv.HangKeTiep}` : 'hạng cao nhất'}</small></div>
      </div>
      <div className="giao-ca-luoi kh-luoi">
        <div>
          <section className="the-trang">
            <div className="the-dau"><h2>Lịch sử kỳ nghỉ</h2></div>
            {!phieu.length ? <p className="muted small">Khách chưa có phiếu đặt phòng.</p> : (
              <div className="bang-cuon">
                <table className="bang" aria-label="Lịch sử kỳ nghỉ">
                  <thead><tr><th>Phiếu</th><th>Phòng</th><th>Lưu trú</th><th>Hoá đơn</th><th /></tr></thead>
                  <tbody>
                    {phieu.map((b) => {
                      const p = b.ChiTietDatPhongs?.[0]?.Phong;
                      const tt = NHAN_PHIEU[b.TrangThai ?? 'Pending'];
                      return (
                        <tr key={b.MaDatPhong}>
                          <td><strong>{b.MaDatPhong}</strong><div className="small muted">đặt {ngay(b.NgayDat)}</div></td>
                          <td>{p?.SoPhong ?? '—'}<div className="small muted">{tenHangPhong(p?.LoaiPhong?.TenLoai)}</div></td>
                          <td>{ngay(b.NgayCheckInDuKien).slice(0, 5)} – {ngay(b.NgayCheckOutDuKien)}
                            {b.YeuCauDacBiet && <div className="small muted kh-yeu-cau" title={b.YeuCauDacBiet}>✎ {b.YeuCauDacBiet}</div>}</td>
                          <td>{b.HoaDon ? <><a href={`/in/hoa-don/${b.HoaDon.MaHoaDon}`} target="_blank" rel="noopener">{b.HoaDon.MaHoaDon}</a><div className="small muted">{tien(b.HoaDon.TongTienCanThanhToan)}</div></> : <span className="muted">—</span>}</td>
                          <td className="canh-phai"><span className={`badge ${tt.css}`}>{b.TrangThai === 'Cancelled' ? 'Đã huỷ' : tt.nhan}</span></td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          <section className="the-trang">
            <div className="the-dau"><h2>Lịch sử điểm</h2><small className="muted">Tổng = {so(ls.reduce((t, x) => t + (x.SoDiem ?? 0), 0))} điểm</small></div>
            <ErrorBox message={diem.error?.message} />
            {!ls.length ? <p className="muted small">Chưa có thay đổi điểm.</p> : (
              <div className="bang-cuon">
                <table className="bang" aria-label="Lịch sử điểm">
                  <thead><tr><th>Thời gian</th><th>Loại</th><th>Nội dung</th><th className="canh-phai">Điểm</th></tr></thead>
                  <tbody>
                    {ls.map((x) => (
                      <tr key={x.MaLichSu}>
                        <td className="small nowrap">{ngayGio(x.ThoiGian)}</td>
                        <td><span className="badge badge-muted">{x.Loai}</span></td>
                        <td className="small">{x.NoiDung}{x.TenNhanVien ? ` · ${x.TenNhanVien}` : ''}</td>
                        <td className={`canh-phai ${(x.SoDiem ?? 0) >= 0 ? 'chu-ok' : 'chu-loi'}`}><strong>{(x.SoDiem ?? 0) > 0 ? '+' : ''}{so(x.SoDiem)}</strong></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
        <div>
          <section className="the-trang">
            <div className="the-dau"><h2>Thông tin cá nhân</h2></div>
            <dl className="kh-dl">
              <dt>CCCD / Passport</dt><dd>{c.CCCD_Passport}</dd>
              <dt>Ngày sinh</dt><dd>{ngay(c.NgaySinh)}</dd>
              <dt>Điện thoại</dt><dd>{lh?.SDT || '—'}</dd>
              <dt>Email</dt><dd>{lh?.Email || '—'}</dd>
              <dt>Địa chỉ</dt><dd>{lh?.DiaChi || '—'}{lh?.QuocGia ? `, ${lh.QuocGia}` : ''}</dd>
            </dl>
          </section>
          <section className="the-trang cham-soc-kh">
            <div className="the-dau"><h2>♥ Chăm sóc khách</h2></div>
            <p className="kh-nhan">Sở thích lưu trú <small className="muted">(khách tự cập nhật được)</small></p>
            <p>{c.SoThich || 'Chưa ghi nhận'}</p>
            <p className="kh-nhan">Ghi chú nội bộ <small className="muted">(khách không thấy)</small></p>
            <p>{c.GhiChuNoiBo || 'Không có'}</p>
          </section>
          {session?.vaiTro === 'Admin' && <DieuChinhDiem ma={c.MaKhachHang!} />}
        </div>
      </div>
      {suaHoSo && <HopSuaHoSo k={c} onDong={() => setSuaHoSo(false)} />}
    </>
  );
}

function DieuChinhDiem({ ma }: { ma: string }) {
  const [soDiem, setSoDiem] = useState('');
  const [lyDo, setLyDo] = useState('');
  const ghi = useThaoTac({
    mutationFn: () => customerApi.adjustPoints(ma, Number(soDiem), lyDo.trim()), lamMoi: [['customers']],
    thanhCong: () => `Đã ${Number(soDiem) >= 0 ? 'cộng' : 'trừ'} ${so(Math.abs(Number(soDiem)))} điểm.`,
    onSuccess: () => { setSoDiem(''); setLyDo(''); }
  });
  return (
    <section className="the-trang">
      <div className="the-dau"><h2>Điều chỉnh điểm</h2></div>
      <form className="chot-ca" onSubmit={(e) => { e.preventDefault(); ghi.mutate(); }}>
        <label className="field"><span>Số điểm (âm để trừ)</span><input type="number" step={1} required value={soDiem} placeholder="VD: 200 hoặc -50" onChange={(e) => setSoDiem(e.target.value)} /></label>
        <label className="field"><span>Lý do</span><input required maxLength={255} value={lyDo} placeholder="VD: Bù đắp sự cố điều hoà phòng 302" onChange={(e) => setLyDo(e.target.value)} /></label>
        <button type="submit" className="btn btn-outline w-100" disabled={ghi.isPending}>Ghi điều chỉnh</button>
      </form>
    </section>
  );
}

function HopSuaHoSo({ k, onDong }: { k: CustomerDetail; onDong: () => void }) {
  const lh = k.LienHeDiaChis?.[0];
  const [f, setF] = useState<CustomerProfile>({
    Ho: k.Ho ?? '', Ten: k.Ten ?? '', NgaySinh: (k.NgaySinh ?? '').slice(0, 10), CCCD_Passport: k.CCCD_Passport ?? '', SDT: lh?.SDT ?? '', Email: lh?.Email ?? '',
    DiaChi: lh?.DiaChi ?? '', QuocGia: lh?.QuocGia ?? 'Việt Nam', LaVIP: !!k.LaVIP, SoThich: k.SoThich ?? '', GhiChuNoiBo: k.GhiChuNoiBo ?? '', HanChe: !!k.HanChe, LyDoHanChe: k.LyDoHanChe ?? ''
  });
  const dat = (p: Partial<CustomerProfile>) => setF({ ...f, ...p });
  const luu = useThaoTac({
    mutationFn: () => customerApi.update(k.MaKhachHang!, { ...f, Email: f.Email || null, DiaChi: f.DiaChi || null, QuocGia: f.QuocGia || null, SoThich: f.SoThich || null, GhiChuNoiBo: f.GhiChuNoiBo || null, LyDoHanChe: f.LyDoHanChe || null }),
    lamMoi: [['customers']], thanhCong: 'Đã cập nhật hồ sơ khách.', onSuccess: onDong
  });
  return (
    <Modal open rong title={`Sửa hồ sơ ${k.HoTen}`} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <div className="form-grid ba-o">
          <label className="field"><span>Họ</span><input required maxLength={50} value={f.Ho ?? ''} onChange={(e) => dat({ Ho: e.target.value })} /></label>
          <label className="field"><span>Tên</span><input required maxLength={50} value={f.Ten ?? ''} onChange={(e) => dat({ Ten: e.target.value })} /></label>
          <label className="field"><span>Ngày sinh</span><input type="date" required value={f.NgaySinh ?? ''} onChange={(e) => dat({ NgaySinh: e.target.value })} /></label>
          <label className="field"><span>CCCD / Passport</span><input required pattern="[0-9]{9,12}" title="9–12 chữ số" value={f.CCCD_Passport ?? ''} onChange={(e) => dat({ CCCD_Passport: e.target.value })} /></label>
          <label className="field"><span>Điện thoại</span><input required pattern="0[35789][0-9]{8}" title="Số di động Việt Nam 10 chữ số" value={f.SDT ?? ''} onChange={(e) => dat({ SDT: e.target.value })} /></label>
          <label className="field"><span>Email</span><input type="email" maxLength={100} value={f.Email ?? ''} onChange={(e) => dat({ Email: e.target.value })} /></label>
          <label className="field span-2"><span>Địa chỉ</span><input maxLength={255} value={f.DiaChi ?? ''} onChange={(e) => dat({ DiaChi: e.target.value })} /></label>
          <label className="field"><span>Quốc gia</span><input maxLength={50} value={f.QuocGia ?? ''} onChange={(e) => dat({ QuocGia: e.target.value })} /></label>
        </div>
        <label className="o-chon"><input type="checkbox" checked={!!f.LaVIP} onChange={(e) => dat({ LaVIP: e.target.checked })} /> <strong>♛ Khách VIP — lễ tân được nhắc khi khách đến</strong></label>
        <label className="field"><span>Sở thích lưu trú</span><textarea rows={2} maxLength={500} value={f.SoThich ?? ''} placeholder="Gối, tầng, loại giường, dị ứng, đồ uống yêu thích…" onChange={(e) => dat({ SoThich: e.target.value })} /></label>
        <label className="field"><span>Ghi chú nội bộ (khách không thấy)</span><textarea rows={2} maxLength={1000} value={f.GhiChuNoiBo ?? ''} onChange={(e) => dat({ GhiChuNoiBo: e.target.value })} /></label>
        <div className="kh-han-che">
          <label className="o-chon chu-loi"><input type="checkbox" checked={!!f.HanChe} onChange={(e) => dat({ HanChe: e.target.checked })} /> <strong>Hạn chế đặt online</strong></label>
          <input maxLength={255} required={!!f.HanChe} value={f.LyDoHanChe ?? ''} placeholder="Lý do (bắt buộc khi hạn chế)" aria-label="Lý do hạn chế" onChange={(e) => dat({ LyDoHanChe: e.target.value })} />
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu hồ sơ</button></div>
      </form>
    </Modal>
  );
}

// Lịch sử đặt phòng (Reception/BookingHistory): tìm theo mã đơn, tên khách, số phòng; phiếu đang ở mở thẳng trang trả phòng
export function BookingHistoryPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const search = params.get('search') ?? '';
  const ds = useQuery({ queryKey: ['bookings', 'history', search], queryFn: () => bookingHistoryApi.list(search) });
  const [xem, setXem] = useState<StaffBooking | null>(null);

  return (
    <>
      <PageHeader tieuDe="Lịch sử đặt phòng" phu="Lễ tân">
        <OTimKiem key={search} goiY="Mã đơn, tên khách, số phòng..." nhan="Tìm phiếu đặt phòng" />
        {search && <Link to="/nhan-vien/lich-su-dat-phong" className="btn btn-outline btn-sm">Xóa lọc</Link>}
      </PageHeader>
      <ErrorBox message={ds.error?.message} />
      {ds.isLoading ? <Loading /> : (
        <section className="the-trang">
          <div className="the-dau"><h2>Hồ sơ đặt phòng hệ thống</h2><span className="badge badge-muted">Tổng số: {ds.data?.length ?? 0} giao dịch</span></div>
          {!ds.data?.length ? <EmptyState>Không tìm thấy lịch sử đặt phòng nào phù hợp.</EmptyState> : (
            <div className="bang-cuon">
              <table className="bang" aria-label="Lịch sử đặt phòng">
                <thead><tr><th>Mã đơn</th><th>Khách hàng</th><th>Phòng nghỉ</th><th>Ngày lưu trú</th><th>Tiền cọc</th><th>Trạng thái</th><th className="canh-phai">Hành động</th></tr></thead>
                <tbody>
                  {ds.data.map((p) => {
                    const r = p.ChiTietDatPhongs?.[0]?.Phong;
                    const kh = p.KhachHang;
                    const tt = NHAN_PHIEU[p.TrangThai ?? 'Pending'];
                    return (
                      <tr key={p.MaDatPhong} data-phieu={p.MaDatPhong}>
                        <td><strong>#{p.MaDatPhong}</strong><div className="small muted">Đặt: {ngay(p.NgayDat)}</div></td>
                        <td>{kh ? <><strong>{kh.Ho} {kh.Ten}</strong><div className="small muted">CCCD: {kh.CCCD_Passport} | SĐT: {kh.SDT ?? 'N/A'}</div></> : <span className="muted">Khách vãng lai</span>}</td>
                        <td>{r ? <><strong>Phòng {r.SoPhong}</strong><div className="small chu-vang">{r.LoaiPhong?.TenLoai ?? 'Premium Room'}</div></> : <span className="muted">Chưa chỉ định</span>}</td>
                        <td className="small nowrap"><div><strong>Check-In:</strong> {ngay(p.NgayCheckInDuKien)}</div><div className="muted">Check-Out: {ngay(p.NgayCheckOutDuKien)}</div></td>
                        <td className="nowrap"><strong>{tien(p.TienCoc)}</strong></td>
                        <td><span className={`badge ${tt.css}`}>{tt.nhan.toUpperCase()}</span></td>
                        <td className="canh-phai">
                          {p.TrangThai === 'CheckedIn'
                            ? <button type="button" className="btn btn-gold btn-sm" onClick={() => navigate(`/nhan-vien/tra-phong?maPhong=${r?.MaPhong ?? ''}`)}>Trả phòng</button>
                            : <button type="button" className="btn btn-outline btn-sm" onClick={() => setXem(p)}>Xem</button>}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
      <Modal open={!!xem} title={`Phiếu #${xem?.MaDatPhong ?? ''}`} onClose={() => setXem(null)}
        footer={<button type="button" className="btn btn-primary" onClick={() => setXem(null)}>Đóng</button>}>
        {xem && <p>Mã đơn: #{xem.MaDatPhong}. Đơn đặt phòng này hiện đang ở trạng thái {NHAN_PHIEU[xem.TrangThai ?? 'Pending'].nhan}.</p>}
      </Modal>
    </>
  );
}
