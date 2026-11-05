import { useState, type FormEvent } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorBox, Loading, StatusBadge, useAsync } from '../components/common';
import { homNay, ngay, soDem, tenHangPhong, tien } from '../utils/format';
import { DanhGiaCuaToi } from './FeedbackPage';

export function BookingDetailPage() {
  const { maDatPhong = '' } = useParams();
  const location = useLocation();
  const qc = useQueryClient();
  const [lan, setLanRaw] = useState(0);
  // Sau huỷ / đánh giá: tải lại phiếu và làm mới danh sách kỳ nghỉ, đánh giá đã lưu đệm
  const setLan = (f: (x: number) => number) => { void qc.invalidateQueries({ queryKey: ['me'] }); setLanRaw(f); };
  const phieu = useAsync(() => Promise.all([api.booking(maDatPhong), api.myReviews()]), [maDatPhong, lan]);
  const [huy, setHuy] = useState<{ open?: boolean; lyDo: string; busy?: boolean; error?: string; done?: boolean }>({ lyDo: '' });
  const [dg, setDg] = useState<{ soSao: number; noiDung: string; anDanh: boolean; busy?: boolean; error?: string; done?: boolean }>({ soSao: 5, noiDung: '', anDanh: false });

  if (phieu.loading && !phieu.data) return <div className="container section"><Loading /></div>;
  if (phieu.error || !phieu.data) return <div className="container section"><ErrorBox message={phieu.error ?? 'Không tìm thấy phiếu.'} /><Link to="/phieu">← Phiếu của tôi</Link></div>;
  const [b, reviews] = phieu.data;
  const phong = b.ChiTietDatPhongs?.[0]?.Phong;
  const hd = b.HoaDon;
  const duocHuy = (b.TrangThai === 'Pending' || b.TrangThai === 'Approved') && homNay() < (b.NgayCheckInDuKien ?? '').slice(0, 10);
  const daDanhGia = reviews.some((r) => r.MaDatPhong === b.MaDatPhong);
  const duocDanhGia = (b.TrangThai === 'CheckedOut' || b.TrangThai === 'Completed') && !daDanhGia;

  async function guiHuy(e: FormEvent) {
    e.preventDefault();
    setHuy((h) => ({ ...h, busy: true, error: undefined }));
    try {
      await api.cancel(maDatPhong, huy.lyDo.trim() || 'Khách tự huỷ trên trang web');
      setHuy({ lyDo: '', done: true });
      setLan((x) => x + 1);
    } catch (err) {
      setHuy((h) => ({ ...h, busy: false, error: (err as Error).message }));
    }
  }

  async function guiDanhGia(e: FormEvent) {
    e.preventDefault();
    setDg((d) => ({ ...d, busy: true, error: undefined }));
    try {
      await api.review({ MaDatPhong: b.MaDatPhong, MaPhong: phong?.MaPhong, SoSao: dg.soSao, NoiDung: dg.noiDung.trim(), AnDanh: dg.anDanh });
      setDg((d) => ({ ...d, busy: false, done: true }));
      setLan((x) => x + 1);
    } catch (err) {
      setDg((d) => ({ ...d, busy: false, error: (err as Error).message }));
    }
  }

  return (
    <div className="container section narrow">
      <p className="breadcrumb"><Link to="/phieu">Phiếu của tôi</Link> / {b.MaDatPhong}</p>
      {(location.state as { moi?: boolean } | null)?.moi && <div className="alert alert-ok" role="status">Đặt phòng thành công! Mã phiếu của quý khách là <strong>{b.MaDatPhong}</strong>.</div>}
      {huy.done && <div className="alert alert-ok" role="status">Đã huỷ phiếu {b.MaDatPhong}.</div>}
      {(dg.done || (location.state as { daDanhGia?: boolean } | null)?.daDanhGia) && <div className="alert alert-ok" role="status">Cảm ơn bạn đã chia sẻ cảm nhận về kỳ nghỉ!</div>}
      <div className="title-row">
        <h1>{phong?.LoaiPhong?.TenLoai ? tenHangPhong(phong.LoaiPhong.TenLoai) : 'Phiếu đặt phòng'}</h1>
        <StatusBadge status={b.TrangThai} />
      </div>
      <dl className="detail-list">
        <dt>Mã phiếu</dt><dd>{b.MaDatPhong}</dd>
        <dt>Ngày nhận – trả</dt><dd>{ngay(b.NgayCheckInDuKien)} – {ngay(b.NgayCheckOutDuKien)} ({soDem((b.NgayCheckInDuKien ?? '').slice(0, 10), (b.NgayCheckOutDuKien ?? '').slice(0, 10))} đêm)</dd>
        <dt>Số khách</dt><dd>{b.SoLuongKhach}</dd>
        {phong?.SoPhong && <><dt>Phòng</dt><dd>{phong.SoPhong}</dd></>}
        {b.GioDenDuKien && <><dt>Giờ đến dự kiến</dt><dd>{b.GioDenDuKien}</dd></>}
        {b.YeuCauDacBiet && <><dt>Yêu cầu riêng</dt><dd>{b.YeuCauDacBiet}</dd></>}
        {(b.TienCoc ?? 0) > 0 && <><dt>Tiền cọc</dt><dd>{tien(b.TienCoc)}</dd></>}
        {b.MaKhuyenMai && <><dt>Mã khuyến mãi</dt><dd>{b.MaKhuyenMai}</dd></>}
        <dt>Ngày đặt</dt><dd>{ngay(b.NgayDat)}</dd>
      </dl>

      {hd && (
        <section className="card">
          <h2>Hoá đơn {hd.MaHoaDon}</h2>
          <dl className="detail-list">
            <dt>Tiền phòng</dt><dd>{tien(hd.TongTienPhong)}</dd>
            {(hd.TongTienDichVu ?? 0) > 0 && <><dt>Dịch vụ</dt><dd>{tien(hd.TongTienDichVu)}</dd></>}
            {(hd.TienPhat ?? 0) > 0 && <><dt>Phụ thu</dt><dd>{tien(hd.TienPhat)}</dd></>}
            {(hd.GiamGiaKhuyenMai ?? 0) > 0 && <><dt>Khuyến mãi</dt><dd>−{tien(hd.GiamGiaKhuyenMai)}</dd></>}
            {(hd.GiamGiaThanhVien ?? 0) > 0 && <><dt>Ưu đãi {hd.HangThanhVien}</dt><dd>−{tien(hd.GiamGiaThanhVien)}</dd></>}
            <dt>VAT</dt><dd>{tien(hd.ThueVAT)}</dd>
            <dt>Tổng cộng</dt><dd><strong>{tien(hd.TongTienCanThanhToan)}</strong></dd>
          </dl>
          <p className="muted small">{hd.TrangThai === 'Paid' ? 'Đã thanh toán.' : 'Chưa thanh toán.'}</p>
          <a href={`/in/hoa-don/${hd.MaHoaDon}`} target="_blank" rel="noopener" className="btn btn-outline w-100">Xem hoá đơn</a>
        </section>
      )}
      {reviews.filter((r) => r.MaDatPhong === b.MaDatPhong).map((r) => (
        <section key={r.MaDanhGia} aria-label="Đánh giá của bạn"><h2>Đánh giá của bạn</h2><DanhGiaCuaToi r={r} /></section>
      ))}

      {duocHuy && !huy.open && <button type="button" className="btn btn-outline-danger" onClick={() => setHuy({ lyDo: '', open: true })}>Huỷ phiếu</button>}
      {duocHuy && huy.open && (
        <form className="card-form" onSubmit={guiHuy}>
          <h2>Huỷ phiếu {b.MaDatPhong}</h2>
          <p className="muted small">Huỷ miễn phí trước ngày nhận phòng.{(b.TienCoc ?? 0) > 0 ? ' Tiền cọc được xử lý theo chính sách của khách sạn, lễ tân sẽ liên hệ quý khách.' : ''}</p>
          <label><span>Lý do (không bắt buộc)</span><input value={huy.lyDo} maxLength={300} onChange={(e) => setHuy((h) => ({ ...h, lyDo: e.target.value }))} /></label>
          <ErrorBox message={huy.error} />
          <div className="row-actions">
            <button type="submit" className="btn btn-danger" disabled={huy.busy}>Xác nhận huỷ</button>
            <button type="button" className="btn btn-outline" onClick={() => setHuy({ lyDo: '' })}>Giữ phiếu</button>
          </div>
        </form>
      )}

      {duocDanhGia && !dg.done && (
        <form className="card-form" onSubmit={guiDanhGia}>
          <h2>Đánh giá kỳ nghỉ</h2>
          <fieldset className="star-input">
            <legend>Số sao</legend>
            {[1, 2, 3, 4, 5].map((n) => (
              <label key={n}><input type="radio" name="soSao" value={n} checked={dg.soSao === n} onChange={() => setDg((d) => ({ ...d, soSao: n }))} />{'★'.repeat(n)}</label>
            ))}
          </fieldset>
          <label><span>Nhận xét</span><textarea rows={3} required maxLength={1000} value={dg.noiDung} onChange={(e) => setDg((d) => ({ ...d, noiDung: e.target.value }))} /></label>
          <label className="check"><input type="checkbox" checked={dg.anDanh} onChange={(e) => setDg((d) => ({ ...d, anDanh: e.target.checked }))} /> Ẩn danh</label>
          <ErrorBox message={dg.error} />
          <button type="submit" className="btn btn-gold" disabled={dg.busy}>Gửi đánh giá</button>
        </form>
      )}
    </div>
  );
}
