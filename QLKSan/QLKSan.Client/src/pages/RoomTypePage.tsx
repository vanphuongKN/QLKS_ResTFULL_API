import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorBox, Loading, Stars, useAsync } from '../components/common';
import { addDays, anh, homNay, ngay, soDem, tien } from '../utils/format';

export function RoomTypePage() {
  const { maLoai = '' } = useParams();
  const room = useAsync(() => api.roomType(maLoai), [maLoai]);
  const [ci, setCi] = useState(homNay());
  const [co, setCo] = useState(addDays(homNay(), 1));
  const [n, setN] = useState(2);
  const [kq, setKq] = useState<{ loading?: boolean; error?: string; tong?: number; con?: number; checked?: boolean }>({});

  if (room.loading && !room.data) return <div className="container section"><Loading /></div>;
  if (room.error || !room.data) return <div className="container section"><ErrorBox message={room.error ?? 'Không tìm thấy hạng phòng.'} /></div>;
  const r = room.data;
  const anhs = [r.AnhDaiDien, ...(r.HinhAnhs ?? []).map((h) => h.DuongDan)].filter((x, i, a): x is string => !!x && a.indexOf(x) === i);

  async function kiemTra() {
    if (co <= ci) return setKq({ error: 'Ngày trả phòng phải sau ngày nhận phòng.' });
    setKq({ loading: true });
    try {
      const x = (await api.available(ci, co, n)).find((a) => a.LoaiPhong?.MaLoai === maLoai);
      setKq({ checked: true, tong: x?.BaoGia?.TongTien ?? undefined, con: x?.SoPhongTrong ?? 0 });
    } catch (e) {
      setKq({ error: (e as Error).message });
    }
  }

  return (
    <div className="container section">
      <p className="breadcrumb"><Link to="/">Trang chủ</Link> / {r.TenLoai}</p>
      <div className="detail-grid">
        <div>
          <div className="gallery">
            {anhs.slice(0, 5).map((a, i) => <img key={a} src={anh(a)} alt={i === 0 ? r.TenLoai ?? '' : ''} className={i === 0 ? 'main' : ''} />)}
          </div>
          <h1>{r.TenLoai}</h1>
          <p className="room-meta">
            Tối đa {r.SoNguoiToiDa} khách{r.DienTich ? ` · ${r.DienTich} m²` : ''}{r.LoaiGiuong ? ` · ${r.LoaiGiuong}` : ''}{r.HuongNhin ? ` · ${r.HuongNhin}` : ''}
          </p>
          {(r.SoDanhGia ?? 0) > 0 && <p><Stars value={r.DiemDanhGia} /> {r.DiemDanhGia?.toFixed(1)} · {r.SoDanhGia} đánh giá</p>}
          <p className="lead">{r.MoTaChiTiet || r.MoTa}</p>
          {(r.TienIchs?.length ?? 0) > 0 && (
            <>
              <h2>Tiện nghi</h2>
              <ul className="amenities">{r.TienIchs!.map((t) => <li key={t.MaTienIch}>{t.TenTienIch}</li>)}</ul>
            </>
          )}
          {(r.DanhGiaGanDay?.length ?? 0) > 0 && (
            <>
              <h2>Đánh giá gần đây</h2>
              {r.DanhGiaGanDay!.map((d) => (
                <figure key={d.MaDanhGia} className="review review-inline">
                  <Stars value={d.SoSao} /> <span className="muted">{ngay(d.NgayDanhGia)}</span>
                  <blockquote>“{d.NoiDung}”</blockquote>
                  <figcaption>{d.TenHienThi ?? 'Khách lưu trú'}</figcaption>
                  {d.PhanHoi && <p className="reply"><strong>Khách sạn phản hồi:</strong> {d.PhanHoi}</p>}
                </figure>
              ))}
            </>
          )}
        </div>
        <aside className="booking-box" aria-label="Đặt phòng">
          <p className="room-price">từ <strong>{tien(r.GiaThueNgay)}</strong> / đêm</p>
          <label><span>Nhận phòng</span><input type="date" value={ci} min={homNay()} onChange={(e) => { setCi(e.target.value); setKq({}); if (co <= e.target.value) setCo(addDays(e.target.value, 1)); }} /></label>
          <label><span>Trả phòng</span><input type="date" value={co} min={addDays(ci, 1)} onChange={(e) => { setCo(e.target.value); setKq({}); }} /></label>
          <label><span>Số khách</span>
            <select value={n} onChange={(e) => { setN(Number(e.target.value)); setKq({}); }}>
              {Array.from({ length: r.SoNguoiToiDa ?? 2 }, (_, i) => i + 1).map((x) => <option key={x} value={x}>{x} khách</option>)}
            </select>
          </label>
          <button type="button" className="btn btn-outline w-100" onClick={kiemTra} disabled={kq.loading}>Kiểm tra giá</button>
          <ErrorBox message={kq.error} />
          {kq.checked && (kq.con ? (
            <div className="quote">
              <p>{soDem(ci, co)} đêm · còn {kq.con} phòng</p>
              <p className="quote-total">{tien(kq.tong)}</p>
              <Link className="btn btn-gold w-100" to={`/dat-phong?maLoai=${maLoai}&checkIn=${ci}&checkOut=${co}&soNguoi=${n}`}>Đặt phòng</Link>
            </div>
          ) : <p className="alert alert-warn">Hạng phòng này đã hết cho ngày đã chọn.</p>)}
        </aside>
      </div>
    </div>
  );
}
