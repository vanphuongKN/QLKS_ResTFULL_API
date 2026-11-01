import { Link } from 'react-router-dom';
import type { RoomType } from '../api/client';
import { anh, tien } from '../utils/format';
import { Stars } from './common';

export function RoomTypeCard({ room }: { room: RoomType }) {
  return (
    <article className="room-card">
      <Link to={`/hang-phong/${room.MaLoai}`} className="room-card-img" aria-hidden="true" tabIndex={-1}>
        <img src={anh(room.AnhDaiDien)} alt="" loading="lazy" />
      </Link>
      <div className="room-card-body">
        <h3><Link to={`/hang-phong/${room.MaLoai}`}>{room.TenLoai}</Link></h3>
        <p className="room-meta">
          Tối đa {room.SoNguoiToiDa} khách{room.DienTich ? ` · ${room.DienTich} m²` : ''}{room.HuongNhin ? ` · ${room.HuongNhin}` : ''}
        </p>
        {(room.SoDanhGia ?? 0) > 0 && (
          <p className="room-rating"><Stars value={room.DiemDanhGia} /> {room.DiemDanhGia?.toFixed(1)} · {room.SoDanhGia} đánh giá</p>
        )}
        <p className="room-price">từ <strong>{tien(room.GiaThueNgay)}</strong> / đêm</p>
      </div>
    </article>
  );
}
