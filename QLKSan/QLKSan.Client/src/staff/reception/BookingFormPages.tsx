import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { availableRoomsNow, frontDeskApi, NGUON_QUA_QUAY, PHUONG_THUC_QUAY } from '../../api/staff';
import { ErrorBox, Loading } from '../../components/common';
import { useToast } from '../../components/ui/Toast';
import { PageHeader } from '../../components/ui/Page';
import { addDays, homNay, ngay, so, soDem, tenHangPhong, tien } from '../../utils/format';
import { GuestLookup, type ThongTinKhach } from './GuestLookup';
import './reception.css';

const LAM_MOI = [['front-desk'], ['rooms'], ['timeline']];

// Đặt phòng qua quầy (Reception/NewBooking của MVC): chọn ngày → hạng còn trống (giá mùa / cuối tuần) → khách → cọc.
// Phiếu được xác nhận ngay; hệ thống xếp phòng trống đầu tiên của hạng.
export function NewBookingPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const qc = useQueryClient();
  const [ngayO, setNgayO] = useState({ CheckIn: homNay(), CheckOut: addDays(homNay(), 1), SoNguoi: 1 });
  const [khach, setKhach] = useState<ThongTinKhach>({ CCCD_Passport: '', Ho: '', Ten: '', SDT: '', Email: '' });
  const [them, setThem] = useState({ NguonDat: NGUON_QUA_QUAY[0], GioDenDuKien: '', YeuCauDacBiet: '', MaKhuyenMai: '', TienCoc: 0, PhuongThucCoc: PHUONG_THUC_QUAY[0] });
  const [maLoai, setMaLoai] = useState('');
  const [gui, setGui] = useState<{ busy?: boolean; loi?: string }>({});
  const hopLe = !!ngayO.CheckIn && !!ngayO.CheckOut && ngayO.CheckOut > ngayO.CheckIn && ngayO.CheckIn >= homNay();
  const hang = useQuery({
    queryKey: ['available-types', ngayO.CheckIn, ngayO.CheckOut, ngayO.SoNguoi], enabled: hopLe,
    queryFn: () => frontDeskApi.availableTypes(ngayO.CheckIn, ngayO.CheckOut, ngayO.SoNguoi)
  });
  const ds = hopLe ? hang.data ?? [] : [];
  const chon = ds.find((x) => x.LoaiPhong?.MaLoai === maLoai && (x.SoPhongTrong ?? 0) > 0);
  const doiNgayNhan = (v: string) => setNgayO((n) => ({ ...n, CheckIn: v, CheckOut: n.CheckOut <= v ? addDays(v, 1) : n.CheckOut }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!chon) { setGui({ loi: 'Vui lòng chọn hạng phòng còn trống.' }); return; }
    setGui({ busy: true });
    try {
      const b = await frontDeskApi.book({
        ...ngayO, ...khach, Email: khach.Email?.trim() || null, MaLoai: maLoai, MaPhong: null, NguonDat: them.NguonDat,
        GioDenDuKien: them.GioDenDuKien.trim() || null, YeuCauDacBiet: them.YeuCauDacBiet.trim() || null, MaKhuyenMai: them.MaKhuyenMai.trim() || null,
        TienCoc: them.TienCoc, PhuongThucCoc: them.PhuongThucCoc
      });
      await Promise.all(LAM_MOI.map((k) => qc.invalidateQueries({ queryKey: k })));
      toast.thanhCong(`Đã tạo phiếu ${b.MaDatPhong} cho ${khach.Ho} ${khach.Ten}: phòng ${b.ChiTietDatPhongs?.[0]?.Phong?.SoPhong}, `
        + `${ngay(b.NgayCheckInDuKien).slice(0, 5)} → ${ngay(b.NgayCheckOutDuKien).slice(0, 5)}` + ((b.TienCoc ?? 0) > 0 ? `, đã thu cọc ${so(b.TienCoc)}đ.` : '.'));
      navigate(`/nhan-vien/le-tan#${(b.NgayCheckInDuKien ?? '').slice(0, 10) === homNay() ? 'den' : 'sapden'}`);
    } catch (err) {
      setGui({ loi: (err as Error).message });
    }
  }

  return (
    <form onSubmit={submit} aria-label="Đặt phòng qua quầy">
      <PageHeader tieuDe="Đặt phòng qua quầy" phu="Khách gọi điện, gửi email, đến quầy hoặc qua đại lý. Phiếu được xác nhận ngay; phòng được giữ trong đúng khoảng ngày.">
        <Link to="/nhan-vien/le-tan" className="btn btn-outline btn-sm">Quay lại</Link>
      </PageHeader>
      <ErrorBox message={gui.loi} />
      <div className="dat-quay-luoi">
        <div>
          <section className="the-trang">
            <h2 className="quay-h">Ngày lưu trú</h2>
            <div className="ba-o">
              <label className="field"><span>Nhận phòng</span><input type="date" min={homNay()} value={ngayO.CheckIn} onChange={(e) => doiNgayNhan(e.target.value)} required /></label>
              <label className="field"><span>Trả phòng</span><input type="date" min={addDays(ngayO.CheckIn || homNay(), 1)} value={ngayO.CheckOut} onChange={(e) => setNgayO({ ...ngayO, CheckOut: e.target.value })} required /></label>
              <label className="field"><span>Số khách</span><input type="number" min={1} max={10} value={ngayO.SoNguoi} onChange={(e) => setNgayO({ ...ngayO, SoNguoi: e.target.valueAsNumber || 1 })} /></label>
            </div>
          </section>
          <section className="the-trang">
            <div className="the-dau"><h2 className="quay-h">Hạng phòng còn trống</h2><small className="muted">{hopLe && hang.data ? `${ds.filter((x) => (x.SoPhongTrong ?? 0) > 0).length} hạng còn phòng` : ''}</small></div>
            {!hopLe ? <p className="small muted">Chọn ngày nhận và trả phòng hợp lệ.</p>
              : hang.isLoading ? <p className="small muted">Đang tìm phòng trống…</p>
                : !ds.length ? <p className="small muted">Không có hạng phòng phù hợp số khách.</p> : (
                  <div className="chon-hang" role="radiogroup" aria-label="Hạng phòng">
                    {ds.map((x) => {
                      const het = !(x.SoPhongTrong ?? 0);
                      return (
                        <button key={x.LoaiPhong?.MaLoai} type="button" role="radio" aria-checked={maLoai === x.LoaiPhong?.MaLoai} disabled={het}
                          className={`hang-o${maLoai === x.LoaiPhong?.MaLoai ? ' dang-chon' : ''}${het ? ' het' : ''}`} onClick={() => setMaLoai(x.LoaiPhong!.MaLoai!)}>
                          <strong>{tenHangPhong(x.LoaiPhong?.TenLoai)}</strong>
                          <small>Tối đa {x.LoaiPhong?.SoNguoiToiDa} khách · {het ? 'hết phòng' : `còn ${x.SoPhongTrong} phòng`}</small>
                          <span>{tien(x.BaoGia?.TongTien)} / {x.BaoGia?.SoDem ?? 0} đêm</span>
                        </button>
                      );
                    })}
                  </div>
                )}
            <small className="muted">Hệ thống xếp phòng trống đầu tiên của hạng; có thể đổi phòng cụ thể trước khi khách đến.</small>
          </section>
          <section className="the-trang">
            <h2 className="quay-h">Thông tin khách</h2>
            <GuestLookup khach={khach} onDien={(k) => setKhach((x) => ({ ...x, ...k }))} />
            <FormKhach khach={khach} onDoi={setKhach} coEmail />
          </section>
        </div>

        <aside className="so-cai">
          <h2>Xác nhận đặt phòng</h2>
          <div className="sc-muc"><span>Hạng phòng</span><div><strong>{chon ? tenHangPhong(chon.LoaiPhong?.TenLoai) : 'Chưa chọn'}</strong><span>{chon ? `${tien(chon.BaoGia?.GiaTrungBinhDem ?? chon.LoaiPhong?.GiaThueNgay)} / đêm` : '—'}</span></div></div>
          <div className="sc-muc"><span>Thời gian lưu trú</span><div><strong>{chon ? `${chon.BaoGia?.SoDem} đêm` : '—'}</strong><span>{ngayO.CheckIn && ngayO.CheckOut ? `${ngay(ngayO.CheckIn)} – ${ngay(ngayO.CheckOut)}` : ''}</span></div></div>
          <div className="sc-muc"><span>Tiền phòng dự kiến</span><strong className="sc-tong">{chon ? tien(chon.BaoGia?.TongTien) : '—'}</strong>
            <small>Đã áp dụng giá mùa / cuối tuần; chưa gồm VAT, dịch vụ và ưu đãi hạng thành viên.</small></div>
          <hr />
          <div className="form-grid">
            <label className="field"><span>Nguồn đặt</span><select value={them.NguonDat} onChange={(e) => setThem({ ...them, NguonDat: e.target.value })}>{NGUON_QUA_QUAY.map((x) => <option key={x}>{x}</option>)}</select></label>
            <label className="field"><span>Giờ đến dự kiến</span><input value={them.GioDenDuKien} placeholder="VD: 14:00 – 16:00" onChange={(e) => setThem({ ...them, GioDenDuKien: e.target.value })} /></label>
            <label className="field span-2"><span>Yêu cầu riêng</span><textarea rows={2} value={them.YeuCauDacBiet} placeholder="Giường, tầng, dịp đặc biệt, đón sân bay…" onChange={(e) => setThem({ ...them, YeuCauDacBiet: e.target.value })} /></label>
            <label className="field span-2"><span>Mã khuyến mãi (tuỳ chọn)</span><input value={them.MaKhuyenMai} maxLength={10} onChange={(e) => setThem({ ...them, MaKhuyenMai: e.target.value.toUpperCase() })} /></label>
          </div>
          <div className="o-coc">
            <div className="the-dau"><span className="quay-h">Tiền cọc / đảm bảo</span>
              <button type="button" className="link-btn small" onClick={() => chon && setThem({ ...them, TienCoc: Math.round(((chon.BaoGia?.TongTien ?? 0) * 0.5) / 1000) * 1000 })}>Gợi ý 50%</button></div>
            <span className="o-ghep"><input type="number" min={0} step={1000} value={them.TienCoc} onChange={(e) => setThem({ ...them, TienCoc: e.target.valueAsNumber || 0 })} aria-label="Tiền cọc" />
              <select value={them.PhuongThucCoc} onChange={(e) => setThem({ ...them, PhuongThucCoc: e.target.value })} aria-label="Phương thức cọc">{PHUONG_THUC_QUAY.map((x) => <option key={x}>{x}</option>)}</select></span>
            <small className="muted">Để 0 nếu khách chưa đặt cọc. Cọc được trừ vào hoá đơn khi trả phòng.</small>
          </div>
          <button type="submit" className="btn btn-gold w-100" disabled={gui.busy}>{gui.busy ? 'Đang đặt…' : 'Xác nhận đặt phòng'}</button>
        </aside>
      </div>
    </form>
  );
}

function FormKhach({ khach, onDoi, coEmail }: { khach: ThongTinKhach; onDoi: (k: ThongTinKhach) => void; coEmail?: boolean }) {
  const dat = (k: keyof ThongTinKhach) => (e: React.ChangeEvent<HTMLInputElement>) => onDoi({ ...khach, [k]: e.target.value });
  return (
    <div className="form-khach">
      <label className="field"><span>CCCD / Hộ chiếu</span><input value={khach.CCCD_Passport} onChange={dat('CCCD_Passport')} inputMode="numeric" required pattern="[0-9]{9,12}" title="CCCD/Passport phải bao gồm 9 đến 12 chữ số" /></label>
      <label className="field"><span>Họ</span><input value={khach.Ho} onChange={dat('Ho')} required maxLength={50} /></label>
      <label className="field"><span>Tên</span><input value={khach.Ten} onChange={dat('Ten')} required maxLength={50} /></label>
      <label className="field"><span>Số điện thoại</span><input type="tel" value={khach.SDT} onChange={dat('SDT')} required pattern="0[35789][0-9]{8}" title="Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09)" /></label>
      {coEmail && <label className="field fk-rong"><span>Email (tuỳ chọn)</span><input type="email" value={khach.Email ?? ''} onChange={dat('Email')} maxLength={100} /></label>}
    </div>
  );
}

// Nhận phòng khách vãng lai (Reception/WalkIn của MVC): khách đến trực tiếp, nhận phòng ngay hôm nay; cọc API tính theo chính sách
export function WalkInPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const qc = useQueryClient();
  const [params] = useSearchParams();
  const phong = useQuery({ queryKey: ['rooms', 'available-now'], queryFn: availableRoomsNow });
  const [khach, setKhach] = useState<ThongTinKhach>({ CCCD_Passport: '', Ho: '', Ten: '', SDT: '' });
  const [o, setO] = useState({ SoNguoi: 1, NgayCheckIn: homNay(), NgayCheckOut: addDays(homNay(), 1), PhuongThucCoc: PHUONG_THUC_QUAY[0] });
  const [maPhong, setMaPhong] = useState(params.get('maPhong') ?? '');
  const [gui, setGui] = useState<{ busy?: boolean; loi?: string }>({});
  const ds = phong.data ?? [];
  const chon = ds.find((r) => r.MaPhong === maPhong);
  const toiDa = chon?.LoaiPhong?.SoNguoiToiDa;
  const dem = o.NgayCheckOut > o.NgayCheckIn ? soDem(o.NgayCheckIn, o.NgayCheckOut) : 0;
  const coc = chon && dem ? dem * (chon.LoaiPhong?.GiaThueNgay ?? 0) * 0.5 : 0;
  const chonPhong = (ma: string) => {
    setMaPhong(ma);
    const r = ds.find((x) => x.MaPhong === ma);
    if (r?.LoaiPhong?.SoNguoiToiDa && o.SoNguoi > r.LoaiPhong.SoNguoiToiDa) setO({ ...o, SoNguoi: r.LoaiPhong.SoNguoiToiDa });
  };

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!maPhong) { setGui({ loi: 'Vui lòng chọn phòng.' }); return; }
    setGui({ busy: true });
    try {
      await frontDeskApi.walkIn({ ...khach, ...o, MaPhong: maPhong });
      await Promise.all(LAM_MOI.map((k) => qc.invalidateQueries({ queryKey: k })));
      toast.thanhCong('Tạo mới đặt phòng và Check-in thành công!');
      navigate('/nhan-vien/le-tan');
    } catch (err) {
      setGui({ loi: (err as Error).message });
    }
  }

  return (
    <form onSubmit={submit} aria-label="Nhận phòng khách vãng lai">
      <PageHeader tieuDe="Nhận phòng khách vãng lai" phu="Khách đến trực tiếp, nhận phòng ngay hôm nay. Vui lòng kiểm tra CCCD / hộ chiếu trước khi xác nhận.">
        <Link to="/nhan-vien/le-tan" className="btn btn-outline btn-sm">Quay lại</Link>
        <button type="submit" className="btn btn-gold btn-sm" disabled={gui.busy}>Xác nhận nhận phòng</button>
      </PageHeader>
      <ErrorBox message={gui.loi} />
      <div className="dat-quay-luoi">
        <div>
          <section className="the-trang">
            <h2 className="quay-h">Xác nhận danh tính khách (CCCD/Passport)</h2>
            <GuestLookup khach={khach} onDien={(k) => setKhach((x) => ({ ...x, ...k }))} />
            <FormKhach khach={khach} onDoi={setKhach} />
            <div className="ba-o">
              <label className="field"><span>Số lượng khách</span><input type="number" min={1} max={toiDa ?? 10} value={o.SoNguoi} onChange={(e) => setO({ ...o, SoNguoi: e.target.valueAsNumber || 1 })} />
                {toiDa && <small className="field-loi">Tối đa: {toiDa} người</small>}</label>
              <label className="field"><span>Ngày Check-in</span><input type="date" min={homNay()} value={o.NgayCheckIn} required
                onChange={(e) => setO({ ...o, NgayCheckIn: e.target.value, NgayCheckOut: o.NgayCheckOut <= e.target.value ? addDays(e.target.value, 1) : o.NgayCheckOut })} /></label>
              <label className="field"><span>Ngày Check-out</span><input type="date" min={addDays(o.NgayCheckIn || homNay(), 1)} value={o.NgayCheckOut} required onChange={(e) => setO({ ...o, NgayCheckOut: e.target.value })} /></label>
            </div>
          </section>
          <section className="the-trang">
            <div className="the-dau"><h2 className="quay-h">Chọn phòng trống</h2><small className="muted">{ds.length} phòng</small></div>
            {phong.isLoading ? <Loading /> : !ds.length ? <p className="small muted canh-giua">Không có phòng nào trống lúc này.</p> : (
              <div className="chon-phong-vl" role="radiogroup" aria-label="Phòng trống">
                {ds.map((r) => (
                  <button key={r.MaPhong} type="button" role="radio" aria-checked={maPhong === r.MaPhong} className={`pvl${maPhong === r.MaPhong ? ' dang-chon' : ''}`} onClick={() => chonPhong(r.MaPhong!)} data-phong={r.MaPhong}>
                    <small>{r.LoaiPhong?.TenLoai ?? 'Standard'}</small><strong>{r.SoPhong}</strong><small>{so(r.LoaiPhong?.GiaThueNgay)} ₫/đêm</small>
                  </button>
                ))}
              </div>
            )}
          </section>
        </div>
        <aside className="so-cai">
          <h2>Chi tiết thanh toán</h2>
          <p className="small muted">Ước tính theo giá niêm yết, giá cuối tuần / lễ tính khi lập hoá đơn</p>
          <div className="sc-muc"><span>Loại phòng</span><div><strong>{chon ? `Phòng ${chon.SoPhong}` : 'Chưa chọn'}</strong><span>{chon ? `${so(chon.LoaiPhong?.GiaThueNgay)} ₫ / đêm` : '—'}</span></div></div>
          <div className="sc-muc"><span>Thời gian lưu trú</span><div><strong>{chon && dem ? `${dem} đêm` : '—'}</strong><span>{chon && dem ? `${ngay(o.NgayCheckIn)} – ${ngay(o.NgayCheckOut)}` : ''}</span></div></div>
          <div className="sc-muc"><span>Tiền cọc yêu cầu (50%)</span>
            <div><strong className="sc-tong">{tien(coc)}</strong>
              <select value={o.PhuongThucCoc} onChange={(e) => setO({ ...o, PhuongThucCoc: e.target.value })} aria-label="Phương thức thu cọc">{PHUONG_THUC_QUAY.map((x) => <option key={x}>{x}</option>)}</select></div></div>
          <button type="submit" className="btn btn-gold w-100" disabled={gui.busy}>Xác nhận nhận phòng</button>
        </aside>
      </div>
    </form>
  );
}
