import { useState, type FormEvent, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import {
  adminApi, TEN_VAI_TRO, VAI_TRO_NHAN_VIEN, type DashboardStay, type EmployeeStatus, type HotelInfoSettings, type HotelPolicySettings,
  type HotelSettings, type LoyaltySettings, type Role, type Staff
} from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { Modal, useXacNhan } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { addDays, homNay, ngay, ngayGio, so, so1, tenHangPhong, tien } from '../../utils/format';
import { phanTramThayDoi } from '../accounting/AccountingPages';
import './admin.css';

const THU = ['Chủ nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
const THU_NGAN = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
const ngayCua = (v?: string) => { const [y, m, d] = (v ?? homNay()).slice(0, 10).split('-').map(Number); return new Date(y, m - 1, d); };
const ddmm = (v?: string) => (v ? `${v.slice(8, 10)}/${v.slice(5, 7)}` : '');

// Số tiền rút gọn trên cột biểu đồ (như Rut() của Admin/Index): 1,2 tr · 3,45 tỷ · 800k
export function rutGon(v: number) {
  if (v >= 1_000_000_000) return `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(v / 1_000_000_000)} tỷ`;
  if (v >= 1_000_000) return `${so1(v / 1_000_000)} tr`;
  if (v >= 1_000) return `${Math.round(v / 1_000)}k`;
  return so(v);
}

function ThayDoi({ v }: { v: number | null }) {
  if (v == null) return <span className="kpi-thay-doi">—</span>;
  return <span className={`kpi-thay-doi ${v >= 0 ? 'kpi-tang' : 'kpi-giam'}`}>{v >= 0 ? '▲' : '▼'} {so1(Math.abs(v))}%</span>;
}

// Biểu đồ cột một chuỗi: cao theo % (0–100), nhãn giá trị trên cột, tooltip khi rê / chạm, có nhãn cho trình đọc màn hình
function BieuDoCot({ ten, cot }: { ten: string; cot: { khoa: string; nhan: ReactNode; giaTri: string; caoPhanTram: number; moTa: string; nhan2?: boolean; homNay?: boolean; mo?: boolean }[] }) {
  return (
    <div className="bd-cot" role="img" aria-label={ten}>
      {cot.map((c) => (
        <div key={c.khoa} className={`bd-cot-o${c.mo ? ' is-mo' : ''}`} title={c.moTa} data-cot={c.khoa}>
          <small className="bd-gia-tri">{c.giaTri}</small>
          <div className="bd-ray"><div className={`bd-thanh${c.homNay ? ' is-nay' : ''}`} style={{ height: `${c.caoPhanTram}%` }} /></div>
          <small className={`bd-nhan${c.nhan2 ? ' chu-vang' : ''}`}>{c.nhan}</small>
        </div>
      ))}
    </div>
  );
}

function BangKhach({ tieuDe, ds, rong, xong, cho }: { tieuDe: string; ds: DashboardStay[]; rong: string; xong: string; cho: string }) {
  return (
    <section className="the-trang the-bang" aria-label={tieuDe}>
      <div className="the-dau"><h2>{tieuDe}</h2><span className="muted small">{ds.length} phiếu</span></div>
      {!ds.length ? <p className="muted small">{rong}</p> : (
        <div className="bang-cuon">
          <table className="bang">
            <thead><tr><th>Khách</th><th>Phòng</th><th>Ghi chú</th><th /></tr></thead>
            <tbody>
              {ds.map((k) => (
                <tr key={k.MaDatPhong}>
                  <td><strong>{k.HoTenKhachHang}</strong>{k.LaVIP && <> <span className="the-vip">♛ VIP</span></>}<div className="small muted">{k.MaDatPhong} · {k.SoKhach} khách</div></td>
                  <td>{k.SoPhong}<div className="small muted">{tenHangPhong(k.TenLoaiPhong)}</div></td>
                  <td className="small">
                    {k.GioDenDuKien && <div>Đến lúc {k.GioDenDuKien}</div>}
                    {k.YeuCauDacBiet && <div className="qt-cat" title={k.YeuCauDacBiet}>✎ {k.YeuCauDacBiet}</div>}
                    {k.SoThich && <div className="qt-cat" title={k.SoThich}>♥ {k.SoThich}</div>}
                  </td>
                  <td className="canh-phai"><span className={`badge ${k.DaXuLy ? 'badge-ok' : 'badge-warn'}`}>{k.DaXuLy ? xong : cho}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

// Bảng điều khiển quản trị (Admin/Index): hôm nay, 30 ngày qua so với kỳ trước, công suất 14 ngày tới, việc cần xử lý,
// khách đến / đi, doanh thu thuần theo tháng, đánh giá mới
export function AdminDashboardPage() {
  const q = useQuery({ queryKey: ['dashboard'], queryFn: adminApi.dashboard, refetchInterval: 120_000 });
  if (q.isLoading) return <Loading />;
  const d = q.data;
  if (!d) return <ErrorBox message={q.error?.message ?? 'Không tải được bảng điều khiển.'} />;
  const hn = new Date();
  const ky = d.KyNay ?? {}, tr = d.KyTruoc ?? {};
  const thang = d.DoanhThuTheoThang ?? [];
  const maxThang = Math.max(1, ...thang.map((m) => m.DoanhThuThuan ?? 0));
  const homNayIso = homNay();
  const viec: [number, string, string, string][] = [
    [d.PhieuChoDuyet ?? 0, 'Phiếu online chờ duyệt', '/nhan-vien/le-tan', (d.PhieuChoDuyet ?? 0) > 0 ? 'warn' : ''],
    [d.SuCoBuongPhong ?? 0, 'Sự cố khi dọn phòng', '/nhan-vien/buong-phong', (d.SuCoBuongPhong ?? 0) > 0 ? 'danger' : ''],
    [d.PhongBaoTri ?? 0, 'Phòng đang bảo trì', '/nhan-vien/phong', (d.PhongBaoTri ?? 0) > 0 ? 'danger' : ''],
    [d.PhongDangDon ?? 0, 'Phòng đang dọn', '/nhan-vien/buong-phong', ''],
    [d.ViecBuongPhongChuaXong ?? 0, 'Việc buồng phòng chưa xong', '/nhan-vien/buong-phong', ''],
    [d.KhuyenMaiSapHetHan?.length ?? 0, 'Khuyến mãi hết hạn trong 7 ngày', '/nhan-vien/quan-tri/khuyen-mai', (d.KhuyenMaiSapHetHan?.length ?? 0) > 0 ? 'warn' : '']
  ];

  return (
    <>
      <PageHeader tieuDe={`${THU[hn.getDay()]}, ${ngay(homNayIso)}`} phu={`Tổng quan điều hành · cập nhật lúc ${ngayGio(d.NgayCapNhat).slice(11)} · ${d.TongSoPhong} phòng`}>
        <Link to="/nhan-vien/le-tan" className="btn btn-outline btn-sm">Lễ tân</Link>
        <Link to="/nhan-vien/ke-toan/bao-cao" className="btn btn-gold btn-sm">Báo cáo chi tiết</Link>
      </PageHeader>

      <div className="kpi-luoi" aria-label="Hôm nay">
        <div className="kpi kpi-toi"><span>Công suất hôm nay</span><strong data-qt="cong-suat">{so1(d.CongSuatHomNay)}%</strong>
          <div className="qt-meter" aria-hidden="true"><span style={{ width: `${Math.min(100, d.CongSuatHomNay ?? 0)}%` }} /></div>
          <small>{d.PhongCoKhach} / {d.TongSoPhong} phòng có khách</small></div>
        <div className="kpi"><span>Khách đến</span><strong>{d.SoKhachDen}</strong><small>Đã nhận phòng {d.SoKhachDenDaNhan} · còn {(d.SoKhachDen ?? 0) - (d.SoKhachDenDaNhan ?? 0)}</small></div>
        <div className="kpi"><span>Khách đi</span><strong>{d.SoKhachDi}</strong><small>Đã trả phòng {d.SoKhachDiDaTra} · còn {(d.SoKhachDi ?? 0) - (d.SoKhachDiDaTra ?? 0)}</small></div>
        <div className="kpi"><span>Đang lưu trú</span><strong>{d.KhachDangLuuTru}</strong><small>phiếu đã nhận phòng</small></div>
      </div>

      <div className="the-dau qt-ky"><h2 className="quay-h">30 ngày qua</h2><small className="muted">{ddmm(ky.TuNgay)} – {ddmm(ky.DenNgay)} · so với {ddmm(tr.TuNgay)} – {ddmm(tr.DenNgay)}</small></div>
      <div className="kpi-luoi" aria-label="30 ngày qua">
        <div className="kpi"><span>Doanh thu thuần</span><strong className="kpi-nho">{tien(ky.DoanhThuThuan)}</strong><small><ThayDoi v={phanTramThayDoi(ky.DoanhThuThuan, tr.DoanhThuThuan)} /> Đã trừ hoàn tiền, gồm VAT</small></div>
        <div className="kpi"><span>Công suất trung bình</span><strong className="kpi-nho">{so1(ky.CongSuat)}%</strong><small><ThayDoi v={phanTramThayDoi(ky.CongSuat, tr.CongSuat)} /> {ky.DemPhongDaBan} đêm phòng đã bán</small></div>
        <div className="kpi"><span>ADR</span><strong className="kpi-nho">{tien(ky.ADR)}</strong><small><ThayDoi v={phanTramThayDoi(ky.ADR, tr.ADR)} /> Giá trung bình mỗi đêm phòng đã bán</small></div>
        <div className="kpi"><span>RevPAR</span><strong className="kpi-nho">{tien(ky.RevPAR)}</strong><small><ThayDoi v={phanTramThayDoi(ky.RevPAR, tr.RevPAR)} /> Doanh thu phòng trên mỗi phòng sẵn có</small></div>
      </div>

      <div className="giao-ca-luoi qt-luoi">
        <section className="the-trang">
          <div className="the-dau"><h2>Công suất đã đặt — 14 ngày tới</h2><Link to="/nhan-vien/lich-phong" className="small">Lịch phòng</Link></div>
          <BieuDoCot ten="Công suất đã đặt 14 ngày tới (%)" cot={(d.DuBao14Ngay ?? []).map((n) => {
            const dn = ngayCua(n.Ngay);
            return {
              khoa: n.Ngay!.slice(0, 10), giaTri: `${Math.round(n.TyLe ?? 0)}%`, caoPhanTram: Math.max(2, n.TyLe ?? 0),
              moTa: `${ddmm(n.Ngay)}: ${n.SoPhongCoKhach} / ${d.TongSoPhong} phòng`, nhan: <>{THU_NGAN[dn.getDay()]}<br />{ddmm(n.Ngay)}</>,
              nhan2: dn.getDay() === 0 || dn.getDay() === 6, homNay: n.Ngay!.slice(0, 10) === homNayIso
            };
          })} />
          <small className="muted">Gồm phiếu chờ duyệt, đã duyệt và đang lưu trú.</small>
        </section>
        <section className="the-trang">
          <div className="the-dau"><h2>Việc cần xử lý</h2></div>
          <div className="viec-kt">
            {viec.map(([soViec, ten, to, muc]) => (
              <Link key={ten} to={to} className={`viec-kt-dong ${soViec === 0 ? 'is-zero' : muc}`}><span>{ten}</span><strong>{soViec}</strong></Link>
            ))}
          </div>
          {(d.KhuyenMaiSapHetHan ?? []).map((km) => <small key={km.MaKhuyenMai} className="muted qt-km">{km.MaKhuyenMai} — {km.TenKhuyenMai}, hết hạn {ngay(km.NgayKetThuc)}</small>)}
        </section>
      </div>

      <div className="qt-hai-cot">
        <BangKhach tieuDe="Khách đến hôm nay" ds={d.KhachDen ?? []} rong="Không có khách đến hôm nay." xong="Đã nhận" cho="Chờ nhận" />
        <BangKhach tieuDe="Khách đi hôm nay" ds={d.KhachDi ?? []} rong="Không có khách trả phòng hôm nay." xong="Đã trả" cho="Đang ở" />
      </div>

      <div className="giao-ca-luoi qt-luoi">
        <section className="the-trang">
          <div className="the-dau"><h2>Doanh thu thuần năm {hn.getFullYear()}</h2><strong data-qt="dt-nam">{tien(thang.reduce((t, m) => t + (m.DoanhThuThuan ?? 0), 0))}</strong></div>
          <BieuDoCot ten={`Doanh thu thuần theo tháng năm ${hn.getFullYear()}`} cot={thang.map((m) => ({
            khoa: `T${m.Thang}`, giaTri: m.DoanhThuThuan ? rutGon(m.DoanhThuThuan) : '', caoPhanTram: Math.max((m.DoanhThuThuan ?? 0) > 0 ? 3 : 0, (m.DoanhThuThuan ?? 0) * 100 / maxThang),
            moTa: `Tháng ${m.Thang}: ${tien(m.DoanhThuThuan)}`, nhan: `T${m.Thang}`, homNay: m.Thang === hn.getMonth() + 1, mo: (m.Thang ?? 0) > hn.getMonth() + 1
          }))} />
        </section>
        <section className="the-trang">
          <div className="the-dau"><h2>Đánh giá của khách</h2><Link to="/nhan-vien/quan-tri/danh-gia" className="small">Tất cả</Link></div>
          <p className="qt-diem"><strong data-qt="diem-dg">{d.DiemDanhGiaTrungBinh != null ? d.DiemDanhGiaTrungBinh.toFixed(1).replace('.', ',') : '—'}</strong> <span className="muted small">/ 5 · {d.TongSoDanhGia} đánh giá</span></p>
          {(d.DanhGiaMoi ?? []).map((r) => (
            <div key={r.MaDanhGia} className={`qt-dg${(r.SoSao ?? 0) <= 3 ? ' is-thap' : ''}`}>
              <div className="the-dau"><span className="qt-sao" aria-label={`${r.SoSao} sao`}>{'★'.repeat(r.SoSao ?? 0)}<span className="muted">{'★'.repeat(5 - (r.SoSao ?? 0))}</span></span><small className="muted">{ddmm(r.NgayDanhGia)}</small></div>
              <p className="small">{r.NoiDung || '(Không có nhận xét)'}</p>
              <small className="muted">{r.HoTenKhachHang} · {tenHangPhong(r.TenLoaiPhong)}</small>
            </div>
          ))}
        </section>
      </div>
    </>
  );
}

// ===== Nhân sự (Admin/Staff) =====
const viTat = (ten?: string | null) => { const w = (ten ?? '').trim().split(/\s+/).filter(Boolean); return (w.length >= 2 ? w.slice(0, 2).map((x) => x[0]).join('') : (ten ?? '?').slice(0, 1)).toUpperCase(); };
const MAU_SDT = '^0[35789][0-9]{8}$';
const GOI_Y_SDT = 'Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09)';

export function StaffAdminPage() {
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['staff'], queryFn: adminApi.staff });
  const [them, setThem] = useState(false);
  const [sua, setSua] = useState<Staff | null>(null);
  const [datLai, setDatLai] = useState<Staff | null>(null);
  const xoa = useThaoTac({ mutationFn: (ma: string) => adminApi.deleteStaff(ma), lamMoi: [['staff']], thanhCong: 'Xóa nhân viên thành công!' });

  return (
    <>
      <PageHeader tieuDe="Danh sách nhân viên" phu="Quản lý nhân sự">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setThem(true)}>+ Thêm nhân viên</button>
      </PageHeader>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : (
        <section className="the-trang the-bang">
          <div className="bang-cuon">
            <table className="bang" aria-label="Danh sách nhân viên">
              <thead><tr><th>Nhân viên</th><th>Chức vụ / vai trò</th><th>Liên hệ</th><th>Ngày vào làm</th><th>Tài khoản</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
              <tbody>
                {!q.data?.length && <tr><td colSpan={7} className="canh-giua muted">Chưa có nhân viên nào.</td></tr>}
                {(q.data ?? []).map((nv) => {
                  const nghi = nv.TrangThai === 'Resigned';
                  return (
                    <tr key={nv.MaNhanVien} className={nghi ? 'qt-nghi' : ''} data-nhan-vien={nv.MaNhanVien}>
                      <td><span className="qt-ten"><span className="qt-avatar" aria-hidden="true">{viTat(nv.TenNhanVien)}</span><span><strong className="nowrap">{nv.TenNhanVien}</strong><small className="muted">{nv.MaNhanVien}</small></span></span></td>
                      <td>{nv.ChucVu}<div className="small muted">Quyền: {nv.VaiTro ? TEN_VAI_TRO[nv.VaiTro] : 'chưa có tài khoản'}</div></td>
                      <td><span className="nowrap">{nv.SDT}</span><div className="small muted">{nv.Email}</div></td>
                      <td className="nowrap">{ngay(nv.NgayVaoLam)}</td>
                      <td>{nv.TenDangNhap ? <strong className="small">{nv.TenDangNhap}</strong> : <span className="muted small">Chưa có TK</span>}</td>
                      <td><span className={`badge ${nghi ? 'badge-muted' : 'badge-ok'}`}>{nghi ? 'Nghỉ việc' : 'Đang làm'}</span></td>
                      <td className="nowrap">
                        <button type="button" className="btn btn-outline btn-sm" title="Sửa thông tin / vai trò / trạng thái" onClick={() => setSua(nv)}>Sửa</button>{' '}
                        {nv.TenDangNhap && <><button type="button" className="btn btn-outline btn-sm" title="Đặt lại mật khẩu" onClick={() => setDatLai(nv)}>Mật khẩu</button>{' '}</>}
                        <button type="button" className="btn btn-outline-danger btn-sm" title="Xóa nhân viên" disabled={xoa.isPending}
                          onClick={async () => { if (await xacNhan({ tieuDe: 'Xoá nhân viên?', noiDung: `Bạn có chắc chắn muốn xóa nhân viên ${nv.TenNhanVien}?`, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(nv.MaNhanVien!); }}>Xoá</button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      )}
      {them && <HopThemNhanVien onDong={() => setThem(false)} />}
      {sua && <HopSuaNhanVien nv={sua} onDong={() => setSua(null)} />}
      {datLai && <HopDatLaiMatKhau nv={datLai} onDong={() => setDatLai(null)} />}
    </>
  );
}

function HopThemNhanVien({ onDong }: { onDong: () => void }) {
  const [f, setF] = useState({ TenNhanVien: '', ChucVu: '', SDT: '', Email: '', VaiTro: 'Receptionist' as Role, TenDangNhap: '', MatKhau: '' });
  const tao = useThaoTac({ mutationFn: () => adminApi.createStaff({ ...f, SDT: f.SDT.trim() }), lamMoi: [['staff']], thanhCong: 'Thêm nhân viên thành công!', onSuccess: onDong });
  const dat = (p: Partial<typeof f>) => setF({ ...f, ...p });
  return (
    <Modal open rong title="Thêm nhân viên mới" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); tao.mutate(); }}>
        <div className="form-grid">
          <label className="field span-2"><span>Họ và tên</span><input required value={f.TenNhanVien} placeholder="Nguyễn Văn A" onChange={(e) => dat({ TenNhanVien: e.target.value })} /></label>
          <label className="field"><span>Chức vụ</span><input required value={f.ChucVu} placeholder="Lễ tân, Buồng phòng..." onChange={(e) => dat({ ChucVu: e.target.value })} /></label>
          <label className="field"><span>Số điện thoại</span><input required pattern={MAU_SDT} title={GOI_Y_SDT} value={f.SDT} placeholder="0901234567" onChange={(e) => dat({ SDT: e.target.value })} /></label>
          <label className="field span-2"><span>Email</span><input type="email" required value={f.Email} placeholder="nhanvien@luxestay.com" onChange={(e) => dat({ Email: e.target.value })} /></label>
        </div>
        <p className="small muted qt-ngan">— Thông tin tài khoản —</p>
        <div className="ba-o">
          <label className="field"><span>Vai trò</span><select required value={f.VaiTro} onChange={(e) => dat({ VaiTro: e.target.value as Role })}>{VAI_TRO_NHAN_VIEN.map((r) => <option key={r} value={r}>{TEN_VAI_TRO[r]}</option>)}</select></label>
          <label className="field"><span>Tên đăng nhập</span><input required value={f.TenDangNhap} placeholder="nhanvien01" autoComplete="off" onChange={(e) => dat({ TenDangNhap: e.target.value })} /></label>
          <label className="field"><span>Mật khẩu</span><input type="password" required minLength={6} title="Mật khẩu phải từ 6 ký tự trở lên" value={f.MatKhau} autoComplete="new-password" onChange={(e) => dat({ MatKhau: e.target.value })} /></label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={tao.isPending}>Tạo nhân viên</button></div>
      </form>
    </Modal>
  );
}

function HopSuaNhanVien({ nv, onDong }: { nv: Staff; onDong: () => void }) {
  const [f, setF] = useState({ TenNhanVien: nv.TenNhanVien ?? '', ChucVu: nv.ChucVu ?? '', SDT: nv.SDT ?? '', Email: nv.Email ?? '', VaiTro: nv.VaiTro ?? null, TrangThai: (nv.TrangThai ?? 'Working') as EmployeeStatus });
  // Nhân viên chưa có tài khoản: không gửi vai trò (giữ nguyên), như MVC
  const luu = useThaoTac({ mutationFn: () => adminApi.updateStaff(nv.MaNhanVien!, { ...f, SDT: f.SDT.trim(), VaiTro: nv.VaiTro ? f.VaiTro : null }), lamMoi: [['staff']], thanhCong: 'Cập nhật nhân viên thành công!', onSuccess: onDong });
  const dat = (p: Partial<typeof f>) => setF({ ...f, ...p });
  return (
    <Modal open rong title="Cập nhật thông tin nhân viên" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <div className="form-grid">
          <label className="field"><span>Họ và tên</span><input required value={f.TenNhanVien} onChange={(e) => dat({ TenNhanVien: e.target.value })} /></label>
          <label className="field"><span>Chức vụ</span><input required value={f.ChucVu} onChange={(e) => dat({ ChucVu: e.target.value })} /></label>
          <label className="field"><span>Số điện thoại</span><input required pattern={MAU_SDT} title={GOI_Y_SDT} value={f.SDT} onChange={(e) => dat({ SDT: e.target.value })} /></label>
          <label className="field"><span>Email</span><input type="email" required value={f.Email} onChange={(e) => dat({ Email: e.target.value })} /></label>
          <label className="field"><span>Vai trò tài khoản</span>
            <select value={f.VaiTro ?? ''} disabled={!nv.VaiTro} onChange={(e) => dat({ VaiTro: e.target.value as Role })}>
              {!nv.VaiTro && <option value="">—</option>}
              {VAI_TRO_NHAN_VIEN.map((r) => <option key={r} value={r}>{TEN_VAI_TRO[r]}</option>)}
            </select>
            {!nv.VaiTro && <small className="muted">Nhân viên chưa có tài khoản đăng nhập.</small>}
          </label>
          <label className="field"><span>Trạng thái</span>
            <select value={f.TrangThai} onChange={(e) => dat({ TrangThai: e.target.value as EmployeeStatus })}>
              <option value="Working">Đang làm việc</option><option value="Resigned">Nghỉ việc (khoá đăng nhập)</option>
            </select>
          </label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-primary" disabled={luu.isPending}>Cập nhật</button></div>
      </form>
    </Modal>
  );
}

function HopDatLaiMatKhau({ nv, onDong }: { nv: Staff; onDong: () => void }) {
  const [mk, setMk] = useState('');
  const dat = useThaoTac({ mutationFn: () => adminApi.resetPassword(nv.MaNhanVien!, mk), thanhCong: 'Đã đặt lại mật khẩu cho nhân viên.', onSuccess: onDong });
  return (
    <Modal open title={`Đặt lại mật khẩu — ${nv.TenNhanVien}`} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); dat.mutate(); }}>
        <label className="field"><span>Mật khẩu mới</span><input type="password" required minLength={6} placeholder="Tối thiểu 6 ký tự" value={mk} autoComplete="new-password" onChange={(e) => setMk(e.target.value)} autoFocus /></label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={dat.isPending}>Đặt lại mật khẩu</button></div>
      </form>
    </Modal>
  );
}

// ===== Cấu hình khách sạn (Admin/Settings) =====
const TAB_CH = [['thong-tin', 'Thông tin khách sạn'], ['chinh-sach', 'Chính sách lưu trú'], ['thanh-vien', 'Chương trình thành viên']] as const;
const nguon = (tuyChinh?: boolean) => (tuyChinh ? 'Đã tuỳ chỉnh' : 'Mặc định (appsettings.json)');
const LAM_MOI_CH = [['settings'], ['hotel']];

export function SettingsPage() {
  const [params] = useSearchParams();
  const tab = TAB_CH.some(([k]) => k === params.get('tab')) ? params.get('tab')! : 'thong-tin';
  const q = useQuery({ queryKey: ['settings'], queryFn: adminApi.settings });
  const d = q.data;
  return (
    <>
      <PageHeader tieuDe="Cấu hình khách sạn"
        phu={`Thay đổi có hiệu lực ngay cho trang khách, đặt phòng và các hoá đơn lập từ bây giờ (hoá đơn đã thanh toán không đổi).${d?.CapNhatLuc ? ` Cập nhật gần nhất ${ngayGio(d.CapNhatLuc).slice(11)} ${ngay(d.CapNhatLuc)}.` : ''}`} />
      <nav className="loc-chip" aria-label="Nhóm cấu hình">
        {TAB_CH.map(([k, n]) => <Link key={k} to={`?tab=${k}`} className={tab === k ? 'active' : ''} aria-current={tab === k ? 'page' : undefined}>{n}</Link>)}
      </nav>
      <ErrorBox message={q.error?.message} />
      {q.isLoading || !d ? (q.isLoading && <Loading />) : tab === 'thong-tin' ? <FormThongTin key={d.CapNhatLuc ?? ''} d={d} /> : tab === 'chinh-sach' ? <FormChinhSach key={d.CapNhatLuc ?? ''} d={d} /> : <FormThanhVien key={d.CapNhatLuc ?? ''} d={d} />}
    </>
  );
}

function FormThongTin({ d }: { d: HotelSettings }) {
  const [f, setF] = useState<HotelInfoSettings>({ Ten: '', MoTaNgan: '', DiaChi: '', Hotline: '', Email: '', BanDoUrl: '', ...d.ThongTin });
  const luu = useThaoTac({ mutationFn: () => adminApi.saveHotelInfo(f), lamMoi: LAM_MOI_CH, thanhCong: 'Đã lưu thông tin khách sạn — footer và trang khách đã cập nhật.' });
  const dat = (p: Partial<HotelInfoSettings>) => setF({ ...f, ...p });
  return (
    <form className="the-trang" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Thông tin liên hệ">
      <div className="the-dau"><h2>Thông tin liên hệ</h2><span className="badge badge-muted">{nguon(d.ThongTinDaTuyChinh)}</span></div>
      <div className="form-grid">
        <label className="field"><span>Tên khách sạn</span><input required maxLength={100} value={f.Ten ?? ''} onChange={(e) => dat({ Ten: e.target.value })} /></label>
        <label className="field"><span>Hotline</span><input maxLength={30} value={f.Hotline ?? ''} onChange={(e) => dat({ Hotline: e.target.value })} /></label>
        <label className="field span-2"><span>Mô tả ngắn (trang chủ, footer)</span><textarea rows={2} maxLength={300} value={f.MoTaNgan ?? ''} onChange={(e) => dat({ MoTaNgan: e.target.value })} /></label>
        <label className="field"><span>Địa chỉ</span><input maxLength={255} value={f.DiaChi ?? ''} onChange={(e) => dat({ DiaChi: e.target.value })} /></label>
        <label className="field"><span>Email đặt phòng</span><input type="email" maxLength={100} value={f.Email ?? ''} onChange={(e) => dat({ Email: e.target.value })} /></label>
        <label className="field span-2"><span>Liên kết bản đồ</span><input type="url" maxLength={500} placeholder="https://maps.google.com/…" value={f.BanDoUrl ?? ''} onChange={(e) => dat({ BanDoUrl: e.target.value })} /></label>
      </div>
      <button type="submit" className="btn btn-gold qt-luu" disabled={luu.isPending}>Lưu thông tin</button>
    </form>
  );
}

function FormChinhSach({ d }: { d: HotelSettings }) {
  const cs = d.ChinhSach ?? {};
  const [f, setF] = useState({ GioNhanPhong: String(cs.GioNhanPhong ?? 14), GioTraPhong: String(cs.GioTraPhong ?? 12), PhanTramTienCocVangLai: String(cs.PhanTramTienCocVangLai ?? 0), TuDongXacNhanDatPhong: !!cs.TuDongXacNhanDatPhong });
  // 4 mốc phụ thu (để trống dòng không dùng), như MVC
  const [moc, setMoc] = useState(() => { const ds = (cs.PhuThuTraMuon ?? []).map((m) => ({ DenGio: String(m.DenGio ?? ''), PhanTram: String(m.PhanTram ?? '') })); while (ds.length < 4) ds.push({ DenGio: '', PhanTram: '' }); return ds; });
  const r: HotelPolicySettings = {
    GioNhanPhong: Number(f.GioNhanPhong), GioTraPhong: Number(f.GioTraPhong), PhanTramTienCocVangLai: Number(f.PhanTramTienCocVangLai), TuDongXacNhanDatPhong: f.TuDongXacNhanDatPhong,
    PhuThuTraMuon: moc.filter((m) => Number(m.DenGio) > 0).map((m) => ({ DenGio: Number(m.DenGio), PhanTram: Number(m.PhanTram || 0) }))
  };
  const luu = useThaoTac({ mutationFn: () => adminApi.savePolicy(r), lamMoi: LAM_MOI_CH, thanhCong: 'Đã lưu chính sách lưu trú — áp dụng cho hoá đơn lập từ bây giờ.' });
  return (
    <form className="the-trang" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Chính sách lưu trú">
      <div className="the-dau"><h2>Chính sách lưu trú</h2><span className="badge badge-muted">{nguon(d.ChinhSachDaTuyChinh)}</span></div>
      <div className="ba-o">
        <label className="field"><span>Giờ nhận phòng</span><input type="number" min={0} max={23} required value={f.GioNhanPhong} onChange={(e) => setF({ ...f, GioNhanPhong: e.target.value })} /></label>
        <label className="field"><span>Giờ trả phòng</span><input type="number" min={0} max={23} required value={f.GioTraPhong} onChange={(e) => setF({ ...f, GioTraPhong: e.target.value })} /></label>
        <label className="field"><span>Cọc khách vãng lai (%)</span><input type="number" min={0} max={100} required value={f.PhanTramTienCocVangLai} onChange={(e) => setF({ ...f, PhanTramTienCocVangLai: e.target.value })} /></label>
      </div>
      <label className="o-chon qt-o-chon"><input type="checkbox" checked={f.TuDongXacNhanDatPhong} onChange={(e) => setF({ ...f, TuDongXacNhanDatPhong: e.target.checked })} /> Tự xác nhận phiếu đặt online khi còn phòng</label>
      <p className="small muted">Khách không đến được xử lý khi chốt ngày (trang Chốt ngày).</p>
      <h3 className="quay-h">Phụ thu trả phòng muộn</h3>
      <p className="small muted">Trả phòng đến mốc giờ: tính % giá 1 đêm. Mốc phải sau giờ trả phòng và tăng dần; sau mốc cuối tính thêm 1 đêm. Để trống dòng không dùng.</p>
      <div className="qt-moc">
        {moc.map((m, i) => (
          <div key={i} className="qt-moc-o">
            <span>Đến</span><input type="number" min={0} max={23} aria-label={`Mốc ${i + 1}: đến giờ`} value={m.DenGio} onChange={(e) => setMoc(moc.map((x, j) => (j === i ? { ...x, DenGio: e.target.value } : x)))} />
            <span>h</span><input type="number" min={0} max={100} aria-label={`Mốc ${i + 1}: phần trăm`} value={m.PhanTram} onChange={(e) => setMoc(moc.map((x, j) => (j === i ? { ...x, PhanTram: e.target.value } : x)))} /><span>%</span>
          </div>
        ))}
      </div>
      <button type="submit" className="btn btn-gold qt-luu" disabled={luu.isPending}>Lưu chính sách</button>
    </form>
  );
}

function FormThanhVien({ d }: { d: HotelSettings }) {
  const tv = d.ThanhVien ?? {};
  const [f, setF] = useState({ SoTienMotDiem: String(tv.SoTienMotDiem ?? ''), GiaTriMotDiem: String(tv.GiaTriMotDiem ?? ''), DiemDoiToiThieu: String(tv.DiemDoiToiThieu ?? '') });
  // 6 dòng hạng (để trống tên để bỏ hạng); quyền lợi mỗi dòng một ý
  const [hang, setHang] = useState(() => {
    const ds = (tv.Hangs ?? []).map((h) => ({ Ten: h.Ten ?? '', DiemToiThieu: String(h.DiemToiThieu ?? 0), PhanTramGiamPhong: String(h.PhanTramGiamPhong ?? 0), QuyenLoi: (h.QuyenLoi ?? []).join('\n') }));
    while (ds.length < 6) ds.push({ Ten: '', DiemToiThieu: '', PhanTramGiamPhong: '', QuyenLoi: '' });
    return ds;
  });
  const r: LoyaltySettings = {
    SoTienMotDiem: Number(f.SoTienMotDiem), GiaTriMotDiem: Number(f.GiaTriMotDiem), DiemDoiToiThieu: Number(f.DiemDoiToiThieu),
    Hangs: hang.filter((h) => h.Ten.trim()).map((h) => ({ Ten: h.Ten.trim(), DiemToiThieu: Number(h.DiemToiThieu || 0), PhanTramGiamPhong: Number(h.PhanTramGiamPhong || 0), QuyenLoi: h.QuyenLoi.split('\n').map((x) => x.trim()).filter(Boolean) }))
  };
  const luu = useThaoTac({ mutationFn: () => adminApi.saveLoyalty(r), lamMoi: LAM_MOI_CH, thanhCong: 'Đã lưu chương trình thành viên.' });
  const datHang = (i: number, p: Partial<(typeof hang)[number]>) => setHang(hang.map((h, j) => (j === i ? { ...h, ...p } : h)));
  return (
    <form className="the-trang" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Chương trình thành viên">
      <div className="the-dau"><h2>Chương trình thành viên</h2><span className="badge badge-muted">{nguon(d.ThanhVienDaTuyChinh)}</span></div>
      <div className="ba-o">
        <label className="field"><span>Tích 1 điểm cho mỗi (₫)</span><input type="number" min={1000} required value={f.SoTienMotDiem} onChange={(e) => setF({ ...f, SoTienMotDiem: e.target.value })} /></label>
        <label className="field"><span>Giá trị 1 điểm khi đổi (₫)</span><input type="number" min={1} required value={f.GiaTriMotDiem} onChange={(e) => setF({ ...f, GiaTriMotDiem: e.target.value })} /></label>
        <label className="field"><span>Số điểm đổi tối thiểu</span><input type="number" min={1} required value={f.DiemDoiToiThieu} onChange={(e) => setF({ ...f, DiemDoiToiThieu: e.target.value })} /></label>
      </div>
      <h3 className="quay-h">Các hạng</h3>
      <p className="small muted">Hạng xét theo điểm trọn đời (điểm hiện có + đã đổi). Hạng thấp nhất bắt đầu từ 0 điểm. Quyền lợi: mỗi dòng một ý. Để trống tên để bỏ hạng.</p>
      <div className="bang-cuon">
        <table className="bang qt-bang-hang" aria-label="Các hạng thành viên">
          <thead><tr><th>Tên hạng</th><th>Từ điểm</th><th>Giảm tiền phòng (%)</th><th>Quyền lợi hiển thị cho khách</th></tr></thead>
          <tbody>
            {hang.map((h, i) => (
              <tr key={i} data-hang={i}>
                <td><input maxLength={50} aria-label={`Tên hạng ${i + 1}`} value={h.Ten} onChange={(e) => datHang(i, { Ten: e.target.value })} /></td>
                <td><input type="number" min={0} aria-label={`Từ điểm ${i + 1}`} value={h.DiemToiThieu} onChange={(e) => datHang(i, { DiemToiThieu: e.target.value })} /></td>
                <td><input type="number" min={0} max={50} aria-label={`Giảm tiền phòng ${i + 1}`} value={h.PhanTramGiamPhong} onChange={(e) => datHang(i, { PhanTramGiamPhong: e.target.value })} /></td>
                <td><textarea rows={2} aria-label={`Quyền lợi ${i + 1}`} value={h.QuyenLoi} onChange={(e) => datHang(i, { QuyenLoi: e.target.value })} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <button type="submit" className="btn btn-gold qt-luu" disabled={luu.isPending}>Lưu chương trình thành viên</button>
    </form>
  );
}

// ===== Nhật ký thao tác (Admin/AuditLog) =====
const TEN_VAI_TRO_NK: Record<string, string> = { Admin: 'Quản lý', Receptionist: 'Lễ tân', Housekeeping: 'Buồng phòng', Accountant: 'Kế toán', Customer: 'Khách hàng' };

export function AuditLogPage() {
  const [params, setParams] = useSearchParams();
  const loc = {
    tuNgay: params.get('tuNgay') || addDays(homNay(), -6), denNgay: params.get('denNgay') || homNay(),
    doiTuong: params.get('doiTuong') || '', nguoiDung: params.get('nguoiDung') || '', tuKhoa: params.get('tuKhoa') || '',
    trang: Math.max(1, Number(params.get('trang')) || 1)
  };
  const [f, setF] = useState(loc);
  const q = useQuery({ queryKey: ['audit-logs', loc], queryFn: () => adminApi.auditLogs(loc) });
  const d = q.data;
  const soTrang = Math.max(1, Math.ceil((d?.TongSo ?? 0) / (d?.SoDong || 1)));
  const sangTrang = (trang: number) => { const p = new URLSearchParams(params); p.set('trang', String(trang)); setParams(p); };
  const gui = (e: FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams();
    ([['tuNgay', f.tuNgay], ['denNgay', f.denNgay], ['doiTuong', f.doiTuong], ['nguoiDung', f.nguoiDung], ['tuKhoa', f.tuKhoa.trim()]] as const).forEach(([k, v]) => { if (v) p.set(k, v); });
    setParams(p);
  };
  return (
    <>
      <PageHeader tieuDe="Nhật ký thao tác" phu="Ghi tự động mỗi thao tác thay đổi dữ liệu thành công: ai làm, lúc nào, trên đối tượng nào và dữ liệu gửi lên (không lưu mật khẩu)." />
      <form className="bo-loc kt-loc" onSubmit={gui} aria-label="Lọc nhật ký">
        <label>Từ ngày <input type="date" value={f.tuNgay} onChange={(e) => setF({ ...f, tuNgay: e.target.value })} /></label>
        <label>Đến ngày <input type="date" value={f.denNgay} onChange={(e) => setF({ ...f, denNgay: e.target.value })} /></label>
        <label>Đối tượng <select value={f.doiTuong} onChange={(e) => setF({ ...f, doiTuong: e.target.value })}><option value="">Tất cả</option>{(d?.DoiTuongs ?? []).map((x) => <option key={x}>{x}</option>)}</select></label>
        <label>Người thực hiện <select value={f.nguoiDung} onChange={(e) => setF({ ...f, nguoiDung: e.target.value })}><option value="">Tất cả</option>{(d?.NguoiDungs ?? []).map((x) => <option key={x}>{x}</option>)}</select></label>
        <label className="kt-tim">Từ khoá <input type="search" value={f.tuKhoa} placeholder="Mã phiếu, hoá đơn, hành động…" onChange={(e) => setF({ ...f, tuKhoa: e.target.value })} /></label>
        <button type="submit" className="btn btn-primary btn-sm">Lọc</button>
      </form>
      <ErrorBox message={q.error?.message} />
      {q.isLoading || !d ? (q.isLoading && <Loading />) : (
        <section className="the-trang the-bang">
          <div className="the-dau"><span className="small muted" data-nk="tong">{d.TongSo} thao tác · trang {d.Trang} / {soTrang}</span></div>
          {!d.Dong?.length ? <EmptyState>Không có thao tác nào trong khoảng đã chọn.</EmptyState> : (
            <div className="bang-cuon">
              <table className="bang" aria-label="Nhật ký thao tác">
                <thead><tr><th>Thời gian</th><th>Người thực hiện</th><th>Hành động</th><th>Đối tượng</th><th>Chi tiết</th></tr></thead>
                <tbody>
                  {d.Dong.map((n) => {
                    const hd = n.HanhDong ?? '';
                    return (
                      <tr key={n.MaNhatKy} data-nhat-ky={n.MaNhatKy}>
                        <td className="small nowrap">{ngay(n.ThoiGian)} {n.ThoiGian?.slice(11, 19)}</td>
                        <td><strong>{n.TenDangNhap ?? '—'}</strong><div className="small muted">{TEN_VAI_TRO_NK[n.VaiTro ?? ''] ?? n.VaiTro ?? ''}</div></td>
                        <td><span className={`qt-hanh-dong${hd.startsWith('Xoá') || hd.startsWith('Huỷ') || hd.includes('khoá') ? ' is-nguy' : ''}`}>{hd}</span></td>
                        <td className="small">{n.DoiTuong}<div className="muted">{n.MaDoiTuong}</div></td>
                        <td className="small qt-chi-tiet" title={n.ChiTiet ?? ''}>{n.ChiTiet}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
          {soTrang > 1 && (
            <nav className="qt-trang" aria-label="Phân trang">
              {(d.Trang ?? 1) > 1 && <button type="button" className="btn btn-outline btn-sm" onClick={() => sangTrang((d.Trang ?? 1) - 1)}>‹ Trước</button>}
              {(d.Trang ?? 1) < soTrang && <button type="button" className="btn btn-outline btn-sm" onClick={() => sangTrang((d.Trang ?? 1) + 1)}>Sau ›</button>}
            </nav>
          )}
        </section>
      )}
    </>
  );
}
