import { Fragment, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../api/client';
import { ErrorBox, Loading, useAsync } from '../components/common';
import { anh, homNay, ngay, soDem, tien } from '../utils/format';

const KHUNG_GIO = ['Trước 12:00', '12:00 – 14:00', '14:00 – 18:00', '18:00 – 22:00', 'Sau 22:00'];

// Xác nhận đặt phòng (đã điền sẵn từ tìm phòng, trang hạng phòng hoặc trợ lý ảo)
export function BookPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [params] = useSearchParams();
  const maLoai = params.get('maLoai') ?? '';
  const checkIn = params.get('checkIn') ?? '';
  const checkOut = params.get('checkOut') ?? '';
  const soNguoi = Number(params.get('soNguoi') ?? 2) || 2;
  const hopLe = !!maLoai && checkIn >= homNay() && checkOut > checkIn;
  const kq = useAsync(() => (hopLe ? api.available(checkIn, checkOut, soNguoi) : Promise.resolve([])), [maLoai, checkIn, checkOut, soNguoi]);
  const hang = kq.data?.find((x) => x.LoaiPhong?.MaLoai === maLoai);

  const [gio, setGio] = useState('14:00 – 18:00');
  const [yeuCau, setYeuCau] = useState('');
  const [maKm, setMaKm] = useState('');
  const [km, setKm] = useState<{ ok: boolean; thongBao: string; giam: number } | null>(null);
  const [gui, setGui] = useState<{ busy?: boolean; error?: string }>({});

  if (!hopLe) return (
    <div className="container section narrow">
      <h1>Đặt phòng</h1>
      <p className="alert alert-warn">Thông tin đặt phòng chưa đầy đủ hoặc ngày không hợp lệ.</p>
      <Link to="/tim-phong" className="btn btn-gold">Tìm phòng</Link>
    </div>
  );
  if (kq.loading) return <div className="container section"><Loading /></div>;

  async function submit(e: FormEvent) {
    e.preventDefault();
    setGui({ busy: true });
    try {
      const b = await api.book({
        MaLoai: maLoai, SoNguoi: soNguoi, CheckIn: checkIn, CheckOut: checkOut,
        GioDenDuKien: gio, YeuCauDacBiet: yeuCau.trim() || null, MaKhuyenMai: maKm.trim() || null
      });
      await qc.invalidateQueries({ queryKey: ['me'] });
      navigate(`/phieu/${b.MaDatPhong}`, { state: { moi: true } });
    } catch (err) {
      setGui({ error: (err as Error).message });
    }
  }

  const dem = soDem(checkIn, checkOut);
  const tongGoc = hang?.BaoGia?.TongTien ?? 0;

  // Mã ưu đãi chỉ để xem trước số tiền giảm (như Customer/CheckPromotion của MVC); API áp dụng lại khi lập hoá đơn
  async function apDungMa() {
    const ma = maKm.trim();
    if (!ma) return setKm({ ok: false, thongBao: 'Vui lòng nhập mã ưu đãi.', giam: 0 });
    try {
      const p = await api.promotion(ma);
      const giam = p.LoaiGiam === 'Phần trăm' ? (tongGoc * (p.GiaTriGiam ?? 0)) / 100 : p.GiaTriGiam ?? 0;
      setKm({ ok: true, thongBao: `Áp dụng thành công: ${p.TenKhuyenMai}!`, giam: Math.min(giam, tongGoc) });
    } catch (err) {
      const e = err as ApiError;
      setKm({ ok: false, thongBao: e.status === 404 || e.status === 400 ? e.message : 'Không kiểm tra được mã, vui lòng thử lại.', giam: 0 });
    }
  }
  return (
    <div className="container section narrow">
      <h1>Xác nhận đặt phòng</h1>
      <ErrorBox message={kq.error} />
      {!hang || !hang.SoPhongTrong ? (
        <>
          <p className="alert alert-warn">Hạng phòng này đã hết cho {ngay(checkIn)} – {ngay(checkOut)}.</p>
          <Link to={`/tim-phong?checkIn=${checkIn}&checkOut=${checkOut}&soNguoi=${soNguoi}`} className="btn btn-gold">Xem hạng phòng khác</Link>
        </>
      ) : (
        <div className="book-grid">
          <aside className="summary">
            <img src={anh(hang.LoaiPhong?.AnhDaiDien)} alt="" />
            <h2>{hang.LoaiPhong?.TenLoai}</h2>
            <dl>
              <dt>Nhận phòng</dt><dd>{ngay(checkIn)}</dd>
              <dt>Trả phòng</dt><dd>{ngay(checkOut)}</dd>
              <dt>Số đêm · khách</dt><dd>{dem} đêm · {soNguoi} khách</dd>
              {hang.BaoGia?.CacDem?.map((d) => <Fragment key={d.Dem}><dt>Đêm {ngay(d.Dem)}</dt><dd>{tien(d.Gia)}{d.QuyTac ? ` · ${d.QuyTac}` : ''}</dd></Fragment>)}
            </dl>
            {km?.giam ? <p className="dong-giam"><span>Mã ưu đãi {maKm.trim()}</span><span>−{tien(km.giam)}</span></p> : null}
            <p className="quote-total">{tien(tongGoc - (km?.giam ?? 0))}</p>
            <p className="muted small">Tạm tính tiền phòng, chưa gồm ưu đãi thành viên, khuyến mãi và VAT. Thanh toán khi trả phòng.</p>
          </aside>
          <form className="card-form" onSubmit={submit}>
            <label><span>Giờ đến dự kiến</span>
              <select value={gio} onChange={(e) => setGio(e.target.value)}>{KHUNG_GIO.map((g) => <option key={g}>{g}</option>)}</select>
            </label>
            <label><span>Yêu cầu riêng (không bắt buộc)</span>
              <textarea rows={3} maxLength={500} value={yeuCau} onChange={(e) => setYeuCau(e.target.value)} placeholder="Ví dụ: phòng tầng cao, giường phụ, kỷ niệm ngày cưới…" />
            </label>
            <div className="ma-uu-dai">
              <label><span>Mã ưu đãi (không bắt buộc)</span>
                <input value={maKm} maxLength={20} placeholder="Nhập mã (nếu có)" onChange={(e) => { setMaKm(e.target.value.toUpperCase()); setKm(null); }} />
              </label>
              <button type="button" className="btn btn-outline" onClick={apDungMa}>Áp dụng</button>
            </div>
            {km && <p className={`small ${km.ok ? 'chu-ok' : 'chu-loi'}`} role="status">{km.thongBao}</p>}
            <ErrorBox message={gui.error} />
            <button type="submit" className="btn btn-gold w-100" disabled={gui.busy}>{gui.busy ? 'Đang đặt…' : 'Xác nhận đặt phòng'}</button>
            <p className="muted small">Huỷ miễn phí trước ngày nhận phòng.</p>
          </form>
        </div>
      )}
    </div>
  );
}
