import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api, type Booking } from '../api/client';
import { ErrorBox, Loading, Stars } from '../components/common';
import { ngay, tenHangPhong } from '../utils/format';

const tenKyNghi = (b: Booking) =>
  `${tenHangPhong(b.ChiTietDatPhongs?.[0]?.Phong?.LoaiPhong?.TenLoai ?? 'Kỳ nghỉ')} · ${ngay(b.NgayCheckInDuKien).slice(0, 5)} – ${ngay(b.NgayCheckOutThucTe ?? b.NgayCheckOutDuKien)}`;

// Đánh giá theo lần lưu trú (như Customer/Feedback của MVC): chỉ kỳ nghỉ đã trả phòng và chưa đánh giá; mỗi kỳ đánh giá một lần
export function FeedbackPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [params] = useSearchParams();
  const phieu = useQuery({ queryKey: ['me', 'bookings'], queryFn: api.myBookings });
  const danhGia = useQuery({ queryKey: ['me', 'reviews'], queryFn: api.myReviews });
  const [chon, setChon] = useState<string | null>(null);
  const [soSao, setSoSao] = useState(5);
  const [noiDung, setNoiDung] = useState('');
  const [anDanh, setAnDanh] = useState(false);
  const [gui, setGui] = useState<{ busy?: boolean; error?: string }>({});

  if (phieu.isLoading || danhGia.isLoading) return <div className="container section"><Loading /></div>;
  const daDanhGia = new Set((danhGia.data ?? []).map((r) => r.MaDatPhong).filter(Boolean));
  const choDanhGia = (phieu.data ?? [])
    .filter((b) => b.TrangThai === 'CheckedOut' && !daDanhGia.has(b.MaDatPhong))
    .sort((a, b) => (b.NgayCheckOutThucTe ?? b.NgayCheckOutDuKien ?? '').localeCompare(a.NgayCheckOutThucTe ?? a.NgayCheckOutDuKien ?? ''));
  const ma = chon ?? (choDanhGia.some((b) => b.MaDatPhong === params.get('maDatPhong')) ? params.get('maDatPhong')! : choDanhGia[0]?.MaDatPhong ?? '');
  const cuaToi = [...(danhGia.data ?? [])].sort((a, b) => (b.NgayDanhGia ?? '').localeCompare(a.NgayDanhGia ?? ''));

  async function submit(e: FormEvent) {
    e.preventDefault();
    setGui({ busy: true });
    try {
      await api.review({ MaDatPhong: ma, SoSao: soSao, NoiDung: noiDung.trim(), AnDanh: anDanh });
      await qc.invalidateQueries({ queryKey: ['me'] });
      navigate(`/phieu/${ma}`, { state: { daDanhGia: true } });
    } catch (err) {
      setGui({ error: (err as Error).message });
    }
  }

  return (
    <div className="container section narrow">
      <p className="eyebrow">Trải nghiệm khách hàng</p>
      <h1>Cảm nhận của bạn tạo nên di sản của chúng tôi.</h1>
      <p className="muted">Mỗi kỳ nghỉ đã trả phòng được đánh giá một lần. Mọi chia sẻ đều giúp chúng tôi hoàn thiện nghệ thuật hiếu khách.</p>
      <ErrorBox message={phieu.error?.message ?? danhGia.error?.message} />

      {choDanhGia.length === 0 ? (
        <div className="card empty canh-giua">
          <h2>Chưa có kỳ nghỉ nào chờ đánh giá</h2>
          <p className="muted">Bạn có thể đánh giá sau khi trả phòng. Các kỳ nghỉ đã đánh giá nằm ở danh sách bên dưới.</p>
          <Link to="/phieu" className="btn btn-outline">Lịch sử kỳ nghỉ</Link>
        </div>
      ) : (
        <form className="card card-form" onSubmit={submit}>
          <label><span>Kỳ nghỉ</span>
            <select value={ma} onChange={(e) => setChon(e.target.value)} required>
              {choDanhGia.map((b) => <option key={b.MaDatPhong} value={b.MaDatPhong!}>{b.MaDatPhong} — {tenKyNghi(b)}</option>)}
            </select>
          </label>
          <fieldset className="star-input">
            <legend>Ấn tượng chung</legend>
            {[1, 2, 3, 4, 5].map((n) => (
              <label key={n}><input type="radio" name="soSao" value={n} checked={soSao === n} onChange={() => setSoSao(n)} />{'★'.repeat(n)}</label>
            ))}
          </fieldset>
          <label><span>Ý kiến của bạn</span>
            <textarea rows={4} required maxLength={1000} value={noiDung} onChange={(e) => setNoiDung(e.target.value)} placeholder="Chia sẻ những khoảnh khắc đáng nhớ trong kỳ nghỉ của bạn..." />
          </label>
          <label className="check"><input type="checkbox" checked={anDanh} onChange={(e) => setAnDanh(e.target.checked)} /> Gửi ẩn danh — tên của bạn sẽ không hiển thị trên các bài đánh giá công khai.</label>
          <ErrorBox message={gui.error} />
          <div className="row-actions">
            <button type="submit" className="btn btn-gold" disabled={gui.busy}>{gui.busy ? 'Đang gửi…' : 'Gửi đánh giá'}</button>
            <Link to="/phieu" className="btn btn-outline">Để sau</Link>
          </div>
        </form>
      )}

      {cuaToi.length > 0 && (
        <section className="section-nho" aria-labelledby="daGui">
          <p className="eyebrow">Đã gửi</p>
          <h2 id="daGui">Đánh giá của bạn</h2>
          {cuaToi.map((r) => <DanhGiaCuaToi key={r.MaDanhGia} r={r} />)}
        </section>
      )}
    </div>
  );
}

export function DanhGiaCuaToi({ r }: { r: NonNullable<Awaited<ReturnType<typeof api.myReviews>>>[number] }) {
  return (
    <article className="review review-inline danh-gia-cua-toi">
      <div className="title-row">
        <strong>{tenHangPhong(r.TenLoaiPhong ?? 'Kỳ nghỉ')}{r.MaDatPhong ? ` · ${r.MaDatPhong}` : ''}</strong>
        <Stars value={r.SoSao} />
      </div>
      <p>{r.NoiDung || '(Không có nhận xét)'}</p>
      <small className="muted">{ngay(r.NgayDanhGia)}{r.AnDanh ? ' · ẩn danh' : ''}</small>
      {r.PhanHoi && <div className="reply"><strong>Phản hồi từ khách sạn</strong><p>{r.PhanHoi}</p></div>}
    </article>
  );
}
