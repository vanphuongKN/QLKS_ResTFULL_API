import { api } from '../api/client';
import { ErrorBox, Loading, Stars, useAsync } from '../components/common';
import { RoomTypeCard } from '../components/RoomTypeCard';
import { SearchForm } from '../components/SearchForm';
import { tien } from '../utils/format';

export function HomePage() {
  const rooms = useAsync(() => api.roomTypes(), []);
  const reviews = useAsync(() => api.featuredReviews(3), []);
  const services = useAsync(() => api.services(), []);
  const trainghiem = (services.data ?? []).filter((s) => s.LaTraiNghiem && s.TrangThai === 'Available');

  return (
    <>
      <section className="hero" style={{ backgroundImage: 'linear-gradient(rgba(20,26,36,.55), rgba(20,26,36,.7)), url(/images/hero.png)' }}>
        <div className="container hero-inner">
          <p className="eyebrow">Khách sạn & khu nghỉ dưỡng</p>
          <h1>Nơi thời gian <em>chậm lại</em></h1>
          <p className="hero-sub">Bên bờ biển Đà Nẵng, mỗi kỳ nghỉ được chăm chút như một trải nghiệm riêng.</p>
          <SearchForm />
        </div>
      </section>

      <section className="section container" id="hang-phong" aria-labelledby="tieuDeHangPhong">
        <p className="eyebrow">Lưu trú</p>
        <h2 id="tieuDeHangPhong">Hạng phòng</h2>
        <ErrorBox message={rooms.error} />
        {rooms.loading && !rooms.data ? <Loading /> : (
          <div className="grid-cards">
            {[...(rooms.data ?? [])].sort((a, b) => (a.GiaThueNgay ?? 0) - (b.GiaThueNgay ?? 0)).map((r) => <RoomTypeCard key={r.MaLoai} room={r} />)}
          </div>
        )}
      </section>

      {trainghiem.length > 0 && (
        <section className="section section-sand" aria-labelledby="tieuDeTraiNghiem">
          <div className="container">
            <p className="eyebrow">Trải nghiệm</p>
            <h2 id="tieuDeTraiNghiem">Dành riêng cho kỳ nghỉ của bạn</h2>
            <div className="grid-services">
              {trainghiem.map((s) => (
                <div key={s.MaDichVu} className="service">
                  <h3>{s.TenDichVu}</h3>
                  {s.MoTa && <p>{s.MoTa}</p>}
                  <p className="service-price">{tien(s.GiaDichVu)}{s.DonViTinh ? ` / ${s.DonViTinh}` : ''}</p>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {(reviews.data?.length ?? 0) > 0 && (
        <section className="section container" aria-labelledby="tieuDeDanhGia">
          <p className="eyebrow">Cảm nhận</p>
          <h2 id="tieuDeDanhGia">Khách lưu trú nói gì</h2>
          <div className="grid-reviews">
            {reviews.data!.map((r) => (
              <figure key={r.MaDanhGia} className="review">
                <Stars value={r.SoSao} />
                <blockquote>“{r.NoiDung}”</blockquote>
                <figcaption>{r.TenHienThi ?? 'Khách lưu trú'}{r.TenLoaiPhong ? ` · ${r.TenLoaiPhong}` : ''}</figcaption>
              </figure>
            ))}
          </div>
        </section>
      )}
    </>
  );
}
