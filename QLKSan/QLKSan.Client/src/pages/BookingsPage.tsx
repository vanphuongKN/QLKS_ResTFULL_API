import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorBox, Loading, StatusBadge, useAsync } from '../components/common';
import { ngay, tien } from '../utils/format';

export function BookingsPage() {
  const ds = useAsync(() => api.myBookings(), []);
  const list = [...(ds.data ?? [])].sort((a, b) => (b.NgayDat ?? '').localeCompare(a.NgayDat ?? ''));

  return (
    <div className="container section">
      <h1>Phiếu của tôi</h1>
      <ErrorBox message={ds.error} />
      {ds.loading ? <Loading /> : list.length === 0 ? (
        <p className="empty">Quý khách chưa có phiếu đặt phòng nào. <Link to="/tim-phong">Tìm phòng ngay</Link></p>
      ) : (
        <ul className="booking-list">
          {list.map((b) => {
            const phong = b.ChiTietDatPhongs?.[0]?.Phong;
            return (
              <li key={b.MaDatPhong}>
                <Link to={`/phieu/${b.MaDatPhong}`} className="booking-item">
                  <div>
                    <strong>{phong?.LoaiPhong?.TenLoai ?? 'Phòng'}</strong>
                    <span className="muted"> · {b.MaDatPhong}{phong?.SoPhong ? ` · phòng ${phong.SoPhong}` : ''}</span>
                    <p className="muted small">{ngay(b.NgayCheckInDuKien)} – {ngay(b.NgayCheckOutDuKien)} · {b.SoLuongKhach} khách</p>
                  </div>
                  <div className="booking-item-right">
                    <StatusBadge status={b.TrangThai} />
                    {b.HoaDon?.TongTienCanThanhToan != null && <p className="small">{tien(b.HoaDon.TongTienCanThanhToan)}</p>}
                  </div>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
