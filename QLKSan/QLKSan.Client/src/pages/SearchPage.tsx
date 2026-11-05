import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorBox, Loading, useAsync } from '../components/common';
import { SearchForm } from '../components/SearchForm';
import { anh, ngay, soDem, tien } from '../utils/format';

export function SearchPage() {
  const [params] = useSearchParams();
  const checkIn = params.get('checkIn') ?? '';
  const checkOut = params.get('checkOut') ?? '';
  const soNguoi = Number(params.get('soNguoi') ?? 2) || 2;
  const coNgay = !!checkIn && !!checkOut;
  const kq = useAsync(() => (coNgay ? api.available(checkIn, checkOut, soNguoi) : Promise.resolve([])), [checkIn, checkOut, soNguoi]);
  const con = (kq.data ?? []).filter((x) => (x.SoPhongTrong ?? 0) > 0).sort((a, b) => (a.BaoGia?.TongTien ?? 0) - (b.BaoGia?.TongTien ?? 0));
  const dem = coNgay ? soDem(checkIn, checkOut) : 0;

  return (
    <div className="container section">
      <h1>Tìm phòng trống</h1>
      <SearchForm key={`${checkIn}${checkOut}${soNguoi}`} checkIn={checkIn || undefined} checkOut={checkOut || undefined} soNguoi={soNguoi} compact />
      {coNgay && (
        <section aria-live="polite" className="results">
          <ErrorBox message={kq.error} />
          {kq.loading ? <Loading text="Đang tìm phòng…" /> : !kq.error && (
            <>
              <p className="muted">{ngay(checkIn)} – {ngay(checkOut)} · {dem} đêm · {soNguoi} khách · {con.length} hạng phòng còn trống</p>
              {con.length === 0 && <p className="alert alert-warn">Rất tiếc, không còn phòng phù hợp. Quý khách vui lòng chọn ngày khác.</p>}
              {con.map((x) => (
                <article key={x.LoaiPhong?.MaLoai} className="result">
                  <img src={anh(x.LoaiPhong?.AnhDaiDien)} alt="" loading="lazy" />
                  <div className="result-body">
                    <h2><Link to={`/hang-phong/${x.LoaiPhong?.MaLoai}`}>{x.LoaiPhong?.TenLoai}</Link></h2>
                    <p className="room-meta">Tối đa {x.LoaiPhong?.SoNguoiToiDa} khách{x.LoaiPhong?.HuongNhin ? ` · ${x.LoaiPhong.HuongNhin}` : ''} · còn {x.SoPhongTrong} phòng</p>
                    {x.BaoGia?.CacDem && x.BaoGia.CacDem.some((d) => d.QuyTac) && (
                      <p className="muted small">Giá theo đêm: {x.BaoGia.CacDem.map((d) => `${ngay(d.Dem)} ${tien(d.Gia)}${d.QuyTac ? ` (${d.QuyTac})` : ''}`).join(' · ')}</p>
                    )}
                  </div>
                  <div className="result-price">
                    <p className="quote-total">{tien(x.BaoGia?.TongTien)}</p>
                    <p className="muted small">{dem > 1 ? `TB ${tien(x.BaoGia?.GiaTrungBinhDem)}/đêm · ` : ''}chưa gồm VAT</p>
                    <Link className="btn btn-gold" to={`/dat-phong?maLoai=${x.LoaiPhong?.MaLoai}&checkIn=${checkIn}&checkOut=${checkOut}&soNguoi=${soNguoi}`}>Đặt phòng</Link>
                  </div>
                </article>
              ))}
            </>
          )}
        </section>
      )}
    </div>
  );
}
