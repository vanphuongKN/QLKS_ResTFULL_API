import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorBox, Loading, StatusBadge } from '../components/common';
import { ngay, tenHangPhong, tien } from '../utils/format';

// Lịch sử kỳ nghỉ của khách: trạng thái, hoá đơn, đánh giá (như Customer/MyStays của MVC)
export function BookingsPage() {
  const phieu = useQuery({ queryKey: ['me', 'bookings'], queryFn: api.myBookings });
  const danhGia = useQuery({ queryKey: ['me', 'reviews'], queryFn: api.myReviews });
  const daDanhGia = new Set((danhGia.data ?? []).map((r) => r.MaDatPhong).filter(Boolean));
  const list = [...(phieu.data ?? [])].sort((a, b) => (b.NgayDat ?? '').localeCompare(a.NgayDat ?? ''));

  return (
    <div className="container section">
      <h1>Kỳ nghỉ của tôi</h1>
      <ErrorBox message={phieu.error?.message} />
      {phieu.isLoading ? <Loading /> : list.length === 0 ? (
        <p className="empty">Quý khách chưa có phiếu đặt phòng nào. <Link to="/tim-phong">Tìm phòng ngay</Link></p>
      ) : (
        <ul className="booking-list">
          {list.map((b) => {
            const phong = b.ChiTietDatPhongs?.[0]?.Phong;
            const daTra = b.TrangThai === 'CheckedOut' || b.TrangThai === 'Completed';
            return (
              <li key={b.MaDatPhong} className="booking-item">
                <div>
                  <Link to={`/phieu/${b.MaDatPhong}`} className="booking-link"><strong>{tenHangPhong(phong?.LoaiPhong?.TenLoai) || 'Phòng'}</strong></Link>
                  <span className="muted"> · {b.MaDatPhong}{phong?.SoPhong ? ` · phòng ${phong.SoPhong}` : ''}</span>
                  <p className="muted small">{ngay(b.NgayCheckInDuKien)} – {ngay(b.NgayCheckOutDuKien)} · {b.SoLuongKhach} khách</p>
                  <p className="small row-actions">
                    <Link to={`/phieu/${b.MaDatPhong}`}>Chi tiết phiếu</Link>
                    {b.HoaDon?.MaHoaDon && <a href={`/in/hoa-don/${b.HoaDon.MaHoaDon}`} target="_blank" rel="noopener">Hoá đơn {b.HoaDon.MaHoaDon}</a>}
                    {b.TrangThai === 'CheckedOut' && !daDanhGia.has(b.MaDatPhong) && <Link to={`/danh-gia?maDatPhong=${b.MaDatPhong}`}>Đánh giá kỳ nghỉ</Link>}
                  </p>
                </div>
                <div className="booking-item-right">
                  <StatusBadge status={b.TrangThai} />
                  {daTra && daDanhGia.has(b.MaDatPhong) && <span className="badge badge-ok">Đã đánh giá</span>}
                  {b.HoaDon?.TongTienCanThanhToan != null && <p className="small">{tien(b.HoaDon.TongTienCanThanhToan)}</p>}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
