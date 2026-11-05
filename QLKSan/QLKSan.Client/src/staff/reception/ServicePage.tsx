import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { bookingHistoryApi, frontDeskApi, serviceApi, type Service, type ServiceUsageRow } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { Modal } from '../../components/ui/Dialog';
import { PageHeader } from '../../components/ui/Page';
import { useToast } from '../../components/ui/Toast';
import { tien } from '../../utils/format';
import './reception.css';

// Danh mục suy từ tên dịch vụ, như Views/Service/Index của MVC
export function danhMucDichVu(ten?: string | null) {
  const t = (ten ?? '').toLowerCase();
  if (t.includes('rượu') || t.includes('bữa') || t.includes('nước')) return 'Ẩm thực';
  if (t.includes('massage') || t.includes('yoga') || t.includes('spa')) return 'Sức khỏe';
  if (t.includes('đưa đón') || t.includes('xe')) return 'Vận chuyển';
  if (t.includes('giặt') || t.includes('ủi')) return 'Giặt là';
  return 'Dịch vụ chung';
}

interface MucGio { ma: string; ten: string; gia: number; sl: number }
const SL_TOI_DA = 20;

// Gọi dịch vụ cho khách đang ở (Service/Index của MVC): chọn phòng, giỏ dịch vụ (1–20 mỗi món), dịch vụ đã gọi + huỷ gọi nhầm
export function ServicePage() {
  const [params, setParams] = useSearchParams();
  const toast = useToast();
  const stays = useQuery({ queryKey: ['bookings', 'active'], queryFn: frontDeskApi.activeStays });
  const dv = useQuery({ queryKey: ['services'], queryFn: serviceApi.list });
  const [gio, setGio] = useState<MucGio[]>([]);
  const [loc, setLoc] = useState('Tất cả');
  const [huy, setHuy] = useState<ServiceUsageRow | null>(null);

  const phongs = useMemo(() => (stays.data ?? []).map((s) => ({ s, p: s.ChiTietDatPhongs?.[0]?.Phong })).filter((x) => x.p?.MaPhong), [stays.data]);
  // ?maPhong= hoặc ?roomNumber= (số phòng) chọn sẵn phòng
  const soPhong = params.get('roomNumber');
  const maPhong = params.get('maPhong') || (soPhong ? phongs.find((x) => x.p?.SoPhong === soPhong)?.p?.MaPhong : '') || '';
  const chon = phongs.find((x) => x.p?.MaPhong === maPhong);
  const chonPhong = (ma: string) => { const p = new URLSearchParams(params); p.delete('roomNumber'); if (ma) p.set('maPhong', ma); else p.delete('maPhong'); setParams(p, { replace: true }); };

  const them = (s: Service) => setGio((g) => {
    const m = g.find((x) => x.ma === s.MaDichVu);
    if (m) return g.map((x) => (x === m ? { ...x, sl: Math.min(SL_TOI_DA, x.sl + 1) } : x));
    return [...g, { ma: s.MaDichVu!, ten: s.TenDichVu ?? '', gia: s.GiaDichVu ?? 0, sl: 1 }];
  });
  const doiSl = (ma: string, d: number) => setGio((g) => g.map((x) => (x.ma === ma && x.sl + d >= 1 && x.sl + d <= SL_TOI_DA ? { ...x, sl: x.sl + d } : x)));
  const tong = gio.reduce((t, x) => t + x.gia * x.sl, 0);

  const goi = useThaoTac({
    mutationFn: () => bookingHistoryApi.orderServices(chon!.s.MaDatPhong!, gio.map((x) => ({ MaDichVu: x.ma, SoLuong: x.sl }))),
    lamMoi: [['bookings'], ['front-desk'], ['rooms'], ['checkout']], thanhCong: 'Đã thêm các dịch vụ thành công!', tienToLoi: 'Có lỗi xảy ra khi thêm dịch vụ. ',
    onSuccess: () => setGio([])
  });
  const xacNhan = () => {
    if (!maPhong || !gio.length) { toast.loi('Dữ liệu không hợp lệ. Vui lòng chọn ít nhất 1 dịch vụ.'); return; }
    if (!chon) { toast.loi('Không tìm thấy thông tin lưu trú hợp lệ cho phòng này.'); return; }
    goi.mutate();
  };

  const dsDv = dv.data ?? [];
  const danhMuc = ['Tất cả', ...new Set(dsDv.map((s) => danhMucDichVu(s.TenDichVu)))];
  const daGoi: ServiceUsageRow[] = phongs.flatMap(({ s, p }) => (s.SuDungDichVus ?? []).map((u) => ({ u, maDatPhong: s.MaDatPhong!, maPhong: p!.MaPhong!, soPhong: p!.SoPhong ?? '' })))
    .sort((a, b) => (b.u.NgaySuDung ?? '').localeCompare(a.u.NgaySuDung ?? ''));
  const daGoiLoc = daGoi.filter((x) => !maPhong || x.maPhong === maPhong);

  if (stays.isLoading || dv.isLoading) return <Loading />;
  return (
    <>
      <PageHeader tieuDe="Quản lý dịch vụ" phu="Lễ tân" />
      <ErrorBox message={stays.error?.message ?? dv.error?.message} />
      <div className="dat-quay-luoi dv-luoi">
        <div>
          <section className="the-trang">
            <div className="the-dau"><h2>Dịch vụ hiện có</h2>
              <nav className="loc-chip dv-loc" aria-label="Lọc danh mục">
                {danhMuc.map((d) => <button key={d} type="button" className={loc === d ? 'active' : ''} aria-pressed={loc === d} onClick={() => setLoc(d)}>{d}</button>)}
              </nav>
            </div>
            <div className="bang-cuon">
              <table className="bang" aria-label="Danh mục dịch vụ">
                <thead><tr><th>Tên dịch vụ</th><th>Danh mục</th><th>Đơn giá</th><th>Trạng thái</th><th /></tr></thead>
                <tbody>
                  {dsDv.filter((s) => loc === 'Tất cả' || danhMucDichVu(s.TenDichVu) === loc).map((s) => {
                    const co = s.TrangThai === 'Available';
                    return (
                      <tr key={s.MaDichVu} data-dich-vu={s.MaDichVu}>
                        <td><strong>{s.TenDichVu}</strong><div className="small muted">Đơn vị: {s.DonViTinh}</div></td>
                        <td className="muted">{danhMucDichVu(s.TenDichVu)}</td>
                        <td className="nowrap"><strong>{tien(s.GiaDichVu)}</strong></td>
                        <td><span className={`badge ${co ? 'badge-gold' : 'badge-muted'}`}>{co ? 'SẴN SÀNG' : 'ĐÃ HẾT'}</span></td>
                        <td className="canh-phai"><button type="button" className="btn btn-outline btn-sm" disabled={!co} aria-label={`Thêm ${s.TenDichVu}`} onClick={() => them(s)}>+</button></td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <p className="small muted">{dsDv.length} dịch vụ</p>
          </section>
          <section className="the-trang">
            <div className="the-dau"><h2>Dịch vụ đã gọi</h2><small className="muted" id="dv-da-goi-info">{daGoiLoc.length} dòng{maPhong ? ' của phòng đang chọn' : ''}</small></div>
            {!daGoiLoc.length ? <p className="muted small canh-giua">Khách đang ở chưa gọi dịch vụ nào.</p> : (
              <div className="bang-cuon">
                <table className="bang" aria-label="Dịch vụ đã gọi">
                  <thead><tr><th>Phòng</th><th>Dịch vụ</th><th className="canh-phai">SL</th><th>Thời gian</th><th className="canh-phai">Thành tiền</th><th /></tr></thead>
                  <tbody>
                    {daGoiLoc.map((x) => (
                      <tr key={x.u.MaSDDV} data-sddv={x.u.MaSDDV}>
                        <td><strong>{x.soPhong}</strong></td>
                        <td>{x.u.TenDichVu ?? x.u.MaDichVu}</td>
                        <td className="canh-phai">{x.u.SoLuong}</td>
                        <td className="small muted nowrap">{x.u.NgaySuDung?.slice(11, 16)} {x.u.NgaySuDung?.slice(8, 10)}/{x.u.NgaySuDung?.slice(5, 7)}</td>
                        <td className="canh-phai nowrap"><strong>{tien(x.u.ThanhTien)}</strong></td>
                        <td className="canh-phai"><button type="button" className="btn btn-outline-danger btn-sm" title="Huỷ dịch vụ gọi nhầm" onClick={() => setHuy(x)}>Huỷ</button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
        <aside className="so-cai" aria-label="Chỉ định dịch vụ">
          <h2>Chỉ định dịch vụ</h2>
          <label className="field"><span>Chọn phòng</span>
            <select value={chon ? maPhong : ''} onChange={(e) => chonPhong(e.target.value)} required>
              <option value="">-- Chọn phòng --</option>
              {phongs.map(({ p }) => <option key={p!.MaPhong} value={p!.MaPhong!}>Phòng {p!.SoPhong} - {p!.LoaiPhong?.TenLoai}</option>)}
            </select>
          </label>
          <label className="field"><span>Tên khách hàng</span>
            <input readOnly value={chon ? (chon.s.KhachHang ? `${chon.s.KhachHang.Ho} ${chon.s.KhachHang.Ten}` : 'Khách vãng lai') : ''} placeholder="Chọn phòng để xem..." />
          </label>
          <div className="sc-muc"><span>Danh sách dịch vụ đã chọn</span></div>
          {!gio.length ? <p className="muted small">Chưa chọn dịch vụ nào. Hãy bấm nút + ở danh sách dịch vụ.</p> : (
            <ul className="dv-gio" aria-label="Giỏ dịch vụ">
              {gio.map((x) => (
                <li key={x.ma}>
                  <div><strong>{x.ten}</strong><small className="chu-vang">{tien(x.gia)}</small></div>
                  <div className="dv-sl">
                    <button type="button" className="btn btn-outline btn-sm" aria-label={`Bớt ${x.ten}`} onClick={() => doiSl(x.ma, -1)}>−</button>
                    <span aria-label={`Số lượng ${x.ten}`}>{x.sl}</span>
                    <button type="button" className="btn btn-outline btn-sm" aria-label={`Thêm một ${x.ten}`} onClick={() => doiSl(x.ma, 1)}>+</button>
                  </div>
                  <strong className="nowrap">{tien(x.gia * x.sl)}</strong>
                  <button type="button" className="link-btn chu-loi" aria-label={`Xoá ${x.ten}`} onClick={() => setGio((g) => g.filter((y) => y.ma !== x.ma))}>×</button>
                </li>
              ))}
            </ul>
          )}
          <hr />
          <div className="sc-muc"><div><strong>Tổng phí dịch vụ mới</strong><span className="sc-tong" id="dv-tong">{tien(tong)}</span></div></div>
          <button type="button" className={`btn w-100 ${chon && gio.length ? 'btn-gold' : 'btn-outline'}`} disabled={!chon || !gio.length || goi.isPending} onClick={xacNhan}>Xác nhận sử dụng dịch vụ</button>
        </aside>
      </div>
      {huy && <HopHuyDichVu x={huy} onDong={() => setHuy(null)} />}
    </>
  );
}

function HopHuyDichVu({ x, onDong }: { x: ServiceUsageRow; onDong: () => void }) {
  const [sl, setSl] = useState(1);
  const [lyDo, setLyDo] = useState('');
  const huy = useThaoTac({
    mutationFn: () => bookingHistoryApi.voidService(x.maDatPhong, x.u.MaSDDV!, sl, lyDo.trim()),
    lamMoi: [['bookings'], ['front-desk'], ['checkout']], thanhCong: 'Đã huỷ dịch vụ gọi nhầm.', tienToLoi: 'Không thể huỷ dịch vụ. ', onSuccess: onDong
  });
  return (
    <Modal open title="Huỷ dịch vụ gọi nhầm" onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); huy.mutate(); }}>
        <p><strong>{x.u.TenDichVu ?? x.u.MaDichVu} — đang ghi {x.u.SoLuong}</strong></p>
        <div className="form-grid dv-huy">
          <label className="field"><span>Số lượng huỷ</span><input type="number" min={1} max={x.u.SoLuong} required value={sl} onChange={(e) => setSl(Number(e.target.value))} /></label>
          <label className="field"><span>Lý do</span><input maxLength={200} required value={lyDo} placeholder="VD: Nhập nhầm phòng" onChange={(e) => setLyDo(e.target.value)} autoFocus /></label>
        </div>
        <small className="muted">Chỉ huỷ được trước khi hoá đơn thanh toán; lý do được ghi vào phiếu và nhật ký thao tác.</small>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button><button type="submit" className="btn btn-danger" disabled={huy.isPending}>Huỷ dịch vụ</button></div>
      </form>
    </Modal>
  );
}
