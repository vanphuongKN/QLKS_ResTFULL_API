import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { checkoutApi, customerApi, frontDeskApi, PHUONG_THUC_QUAY, type Customer, type Folio, type Invoice, type StaffBooking } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { useToast } from '../../components/ui/Toast';
import { ngay, so, tenHangPhong, tien } from '../../utils/format';
import './reception.css';

const THU = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
const thuNgay = (v?: string) => { if (!v) return ''; const [y, m, d] = v.slice(0, 10).split('-').map(Number); return `${THU[new Date(y, m - 1, d).getDay()]}, ${v.slice(8, 10)}/${v.slice(5, 7)}`; };
const gioNgay = (v?: string | null) => (v ? `${v.slice(11, 16)} ${v.slice(8, 10)}/${v.slice(5, 7)}` : '');
const MO_TA_PT: Record<string, string> = { 'Tiền mặt': 'Tiền mặt tại quầy', 'Thẻ tín dụng': 'Visa, Mastercard, Amex', 'Chuyển khoản': 'Ngân hàng hoặc kỹ thuật số' };
const LAM_MOI = [['checkout'], ['bookings'], ['front-desk'], ['rooms'], ['customers'], ['night-audit']];

// Trả phòng (Checkout/Process của MVC): bảng kê lưu trú, lập hoá đơn (mã KM), thanh toán (đổi điểm, tách tối đa 3 khoản), VAT công ty
export function CheckoutPage() {
  const [params, setParams] = useSearchParams();
  const maPhong = params.get('maPhong') ?? '';
  const stay = useQuery({ queryKey: ['checkout', maPhong], queryFn: () => checkoutApi.currentStay(maPhong), enabled: !!maPhong });
  const b = stay.data;
  const folio = useQuery({ queryKey: ['checkout', 'folio', b?.MaDatPhong], queryFn: () => checkoutApi.folio(b!.MaDatPhong!), enabled: !!b?.MaDatPhong });
  const kh = useQuery({ queryKey: ['customers', 'detail', b?.MaKhachHang], queryFn: () => customerApi.get(b!.MaKhachHang!), enabled: !!b?.MaKhachHang });

  return (
    <>
      <PageHeader tieuDe="Trả phòng khách hàng" phu="Quản lý thanh toán cuối cùng và quy trình rời phòng." />
      {!maPhong ? <ChonPhong onChon={(ma) => setParams({ maPhong: ma })} />
        : stay.isLoading || folio.isLoading ? <Loading />
        : stay.error ? <ErrorBox message={stay.error.message} />
        : !b ? <div className="alert alert-danger" role="alert">Không tìm thấy phòng hoặc phòng chưa check-in.</div>
        : (
          <div className="giao-ca-luoi tp-luoi">
            <div>
              <ErrorBox message={folio.error?.message} />
              {folio.data && <BangKe f={folio.data} b={b} />}
            </div>
            <div>
              {b.HoaDon ? <SoCai b={b} hd={b.HoaDon} kh={kh.data ?? null} maPhong={maPhong} /> : <LapHoaDon b={b} maPhong={maPhong} />}
            </div>
          </div>
        )}
    </>
  );
}

// Không có ?maPhong=: chọn nhanh phòng đang có khách
function ChonPhong({ onChon }: { onChon: (maPhong: string) => void }) {
  const o = useQuery({ queryKey: ['bookings', 'active'], queryFn: frontDeskApi.activeStays });
  const ds = (o.data ?? []).map((s) => ({ p: s.ChiTietDatPhongs?.[0]?.Phong, s })).filter((x) => x.p?.MaPhong)
    .sort((a, b) => (a.p!.SoPhong ?? '').localeCompare(b.p!.SoPhong ?? '', 'vi', { numeric: true }));
  if (o.isLoading) return <Loading />;
  if (!ds.length) return <EmptyState>Chưa có khách nào đang lưu trú.</EmptyState>;
  return (
    <section className="the-trang">
      <div className="the-dau"><h2>Chọn phòng đang có khách</h2></div>
      <div className="the-dau-nut">
        {ds.map(({ p, s }) => <button key={p!.MaPhong} type="button" className="btn btn-outline btn-sm" onClick={() => onChon(p!.MaPhong!)}>Phòng {p!.SoPhong} · {s.KhachHang?.Ho} {s.KhachHang?.Ten}</button>)}
      </div>
    </section>
  );
}

function BangKe({ f, b }: { f: Folio; b: StaffBooking }) {
  const daTra = f.TrangThaiHoaDon === 'Paid';
  const soDiCung = b.KhachDiCungs?.length ?? 0;
  return (
    <section className="the-trang" aria-label="Bảng kê lưu trú">
      <div className="the-dau">
        <div>
          <p className="eyebrow">Bảng kê lưu trú</p>
          <h2>{f.HoTenKhachHang}</h2>
          <div className="small muted">#{f.MaDatPhong} · Phòng {f.SoPhong} ({tenHangPhong(f.TenLoaiPhong)}) · CCCD {b.KhachHang?.CCCD_Passport}</div>
        </div>
        <span className={`badge ${daTra ? 'badge-muted' : 'badge-ok'}`}>{daTra ? 'ĐÃ THANH TOÁN' : 'ĐANG LƯU TRÚ'}</span>
      </div>
      <div className="tp-meta">
        <div><span>Nhận phòng</span><strong>{gioNgay(f.NgayNhan).slice(0, 5)} {ngay(f.NgayNhan)}</strong></div>
        <div><span>Trả dự kiến</span><strong>{ngay(f.NgayTraDuKien)}</strong></div>
        <div><span>Tính tới</span><strong>{gioNgay(f.ThoiDiemTinh).slice(0, 5)} {ngay(f.ThoiDiemTinh)}</strong></div>
        <div><span>Số khách</span><strong>{b.SoLuongKhach}{soDiCung > 0 ? ` (${soDiCung} người đi cùng)` : ''}</strong></div>
      </div>
      <h3 className="quay-h tp-h">Tiền phòng từng đêm</h3>
      <div className="bang-cuon">
        <table className="bang" aria-label="Tiền phòng từng đêm">
          <thead><tr><th>Đêm</th><th>Ghi chú</th><th className="canh-phai">Giá</th></tr></thead>
          <tbody>
            {(f.CacDem ?? []).map((n) => (
              <tr key={n.Dem}>
                <td className="nowrap">{thuNgay(n.Dem)}</td>
                <td className="small muted">{n.QuyTac ? `${n.QuyTac} (${(n.PhanTramDieuChinh ?? 0) > 0 ? '+' : ''}${Number((n.PhanTramDieuChinh ?? 0).toFixed(1))}%)` : 'Giá niêm yết'}</td>
                <td className="canh-phai nowrap">{tien(n.Gia)}</td>
              </tr>
            ))}
            {(f.PhuThuTraMuon ?? 0) > 0 && <tr className="chu-loi"><td>Phụ thu</td><td className="small">Trả phòng muộn ({f.PhanTramPhuThu}% giá 1 đêm)</td><td className="canh-phai nowrap">{tien(f.PhuThuTraMuon)}</td></tr>}
          </tbody>
          <tfoot><tr><td colSpan={2}>Cộng tiền phòng ({f.SoDem} đêm)</td><td className="canh-phai nowrap">{tien((f.TienPhong ?? 0) + (f.PhuThuTraMuon ?? 0))}</td></tr></tfoot>
        </table>
      </div>
      <h3 className="quay-h tp-h">Dịch vụ đã sử dụng</h3>
      <div className="bang-cuon">
        <table className="bang" aria-label="Dịch vụ đã sử dụng">
          <thead><tr><th>Dịch vụ</th><th>Thời gian</th><th className="canh-phai">SL</th><th className="canh-phai">Thành tiền</th></tr></thead>
          <tbody>
            {!f.DichVu?.length ? <tr><td colSpan={4} className="canh-giua muted">Không sử dụng dịch vụ phát sinh.</td></tr>
              : f.DichVu.map((s) => <tr key={s.MaSDDV}><td>{s.TenDichVu}</td><td className="small muted nowrap">{gioNgay(s.NgaySuDung)}</td><td className="canh-phai">{s.SoLuong}</td><td className="canh-phai nowrap">{tien(s.ThanhTien)}</td></tr>)}
          </tbody>
          <tfoot><tr><td colSpan={3}>Cộng dịch vụ</td><td className="canh-phai nowrap">{tien(f.TongTienDichVu)}</td></tr></tfoot>
        </table>
      </div>
      <div className="tp-tong" aria-label="Tổng kết bảng kê">
        {(f.GiamGiaKhuyenMai ?? 0) > 0 && <div className="chu-ok"><span>Khuyến mãi {f.MaKhuyenMai}</span><strong>-{tien(f.GiamGiaKhuyenMai)}</strong></div>}
        {(f.GiamGiaThanhVien ?? 0) > 0 && <div className="chu-ok"><span>Ưu đãi {f.HangThanhVien}</span><strong>-{tien(f.GiamGiaThanhVien)}</strong></div>}
        <div><span>Cộng trước thuế</span><strong>{tien(f.TienTruocThue)}</strong></div>
        <div><span>Thuế VAT (8%)</span><strong>{tien(f.ThueVAT)}</strong></div>
        <div className="tp-tong-cong"><span>Tổng cộng</span><strong>{tien(f.TongCong)}</strong></div>
        {(f.GiaoDichCocs ?? []).map((g) => {
          const hoan = g.LoaiGiaoDich === 'Refund';
          return <div key={g.MaGiaoDichCoc} className={`small ${hoan ? 'chu-loi' : 'muted'}`}><span>{hoan ? 'Hoàn cọc' : 'Cọc'} {gioNgay(g.ThoiGian).slice(6)} {gioNgay(g.ThoiGian).slice(0, 5)} · {g.PhuongThuc}{g.GhiChu ? ` · ${g.GhiChu}` : ''}</span><strong>{hoan ? '+' : '-'}{tien(g.SoTien)}</strong></div>;
        })}
        {(f.ThanhToans ?? []).map((t) => <div key={t.MaThanhToan} className="small muted"><span>Đã thu {gioNgay(t.NgayThanhToan).slice(6)} {gioNgay(t.NgayThanhToan).slice(0, 5)} · {t.PhuongThuc}{t.GhiChu ? ` · ${t.GhiChu}` : ''}</span><strong>-{tien(t.SoTienDaTra)}</strong></div>)}
        {!daTra && <div className="tp-con"><span>{(f.ConPhaiThu ?? 0) >= 0 ? 'Còn phải thu (tạm tính)' : 'Cọc thừa — hoàn lại khách'}</span><strong>{tien(Math.abs(f.ConPhaiThu ?? 0))}</strong></div>}
      </div>
      {!!f.LichSuDoiPhongs?.length && (
        <div className="small muted tp-doi-phong">
          {f.LichSuDoiPhongs.map((mv, i) => <div key={i}>⇄ {gioNgay(mv.ThoiGian).slice(6)} {gioNgay(mv.ThoiGian).slice(0, 5)}: đổi phòng {mv.SoPhongCu} → {mv.SoPhongMoi} ({mv.LyDo}){mv.GiaMoi === mv.GiaCu ? ', giữ giá cũ' : ''}</div>)}
        </div>
      )}
    </section>
  );
}

function LapHoaDon({ b, maPhong }: { b: StaffBooking; maPhong: string }) {
  const [km, setKm] = useState(b.MaKhuyenMai ?? '');
  const lap = useThaoTac({
    mutationFn: () => checkoutApi.generate(b.MaDatPhong!, km.trim() || null), lamMoi: [['checkout', maPhong], ['checkout']],
    thanhCong: 'Đã tạo hóa đơn thành công!', tienToLoi: 'Lỗi khi tạo hóa đơn: '
  });
  return (
    <section className="so-cai" aria-label="Tiến hành xuất hoá đơn">
      <h2>Tiến hành xuất hóa đơn</h2>
      <p className="muted">Vui lòng kiểm tra kỹ dịch vụ trước khi xuất hóa đơn. Hóa đơn đã xuất không thể thay đổi thông tin dịch vụ.</p>
      <form className="chot-ca" onSubmit={(e) => { e.preventDefault(); lap.mutate(); }}>
        <label className="field"><span>Mã khuyến mãi (nếu có)</span><input value={km} placeholder="Nhập mã khuyến mãi..." onChange={(e) => setKm(e.target.value)} /></label>
        <button type="submit" className="btn btn-outline w-100" disabled={lap.isPending}>Chốt dữ liệu &amp; tạo hóa đơn</button>
      </form>
    </section>
  );
}

function SoCai({ b, hd, kh, maPhong }: { b: StaffBooking; hd: Invoice; kh: Customer | null; maPhong: string }) {
  const chuaTra = hd.TrangThai === 'Unpaid';
  const con = hd.SoTienConLai ?? 0;
  return (
    <section className="so-cai tp-so-cai" aria-label={`Hoá đơn ${hd.MaHoaDon}`}>
      <div className="the-dau">
        <h2>Hóa đơn cuối (#{hd.MaHoaDon})</h2>
        <a className="btn btn-outline btn-sm" href={`/in/hoa-don/${hd.MaHoaDon}`} target="_blank" rel="noopener" title="In phiếu tạm tính">In</a>
      </div>
      <div className="tp-tong">
        <div><span className="muted">Tiền phòng ({hd.SoDem} đêm thực tế)</span><strong>{tien(hd.TongTienPhong)}</strong></div>
        {(hd.TienPhat ?? 0) > 0 && <div className="chu-loi"><span>Phụ thu trả phòng muộn ({hd.PhanTramPhuThu}%)</span><strong>{tien(hd.TienPhat)}</strong></div>}
        <div><span className="muted">Tiền dịch vụ</span><strong>{tien(hd.TongTienDichVu)}</strong></div>
        {(hd.GiamGiaKhuyenMai ?? 0) > 0 && <div className="chu-ok"><span>Khuyến mãi {hd.MaKhuyenMai ? `(${hd.MaKhuyenMai})` : ''}</span><strong>-{tien(hd.GiamGiaKhuyenMai)}</strong></div>}
        {(hd.GiamGiaThanhVien ?? 0) > 0 && <div className="chu-ok"><span>Ưu đãi {hd.HangThanhVien}</span><strong>-{tien(hd.GiamGiaThanhVien)}</strong></div>}
        <div><span className="muted">Thuế VAT (8%)</span><strong>{tien(hd.ThueVAT)}</strong></div>
        <div><span className="muted">Tổng giá trị hoá đơn</span><strong>{tien(hd.TongTienCanThanhToan)}</strong></div>
        {(hd.TienCoc ?? 0) > 0 && <div><span className="muted">Tiền đặt cọc đã trừ</span><strong className="chu-ok">-{tien(hd.TienCoc)}</strong></div>}
      </div>
      <hr />
      <div className="sc-muc" data-con-lai={Math.round(con)}>
        <span>{con >= 0 ? 'Còn phải thanh toán' : 'Tiền cọc thừa — hoàn lại khách'}</span>
        <div className={`sc-tong ${con >= 0 ? 'chu-loi' : 'chu-ok'}`}>{tien(Math.abs(con))}</div>
      </div>
      {chuaTra ? (
        <>
          <ApDungKm b={b} hd={hd} maPhong={maPhong} />
          <ThanhToan hd={hd} kh={kh} />
        </>
      ) : <div className="alert alert-ok canh-giua">ĐÃ THANH TOÁN</div>}
      <XuatVat hd={hd} maPhong={maPhong} />
    </section>
  );
}

function ApDungKm({ b, hd, maPhong }: { b: StaffBooking; hd: Invoice; maPhong: string }) {
  const [km, setKm] = useState(hd.MaKhuyenMai ?? '');
  const ap = useThaoTac({
    mutationFn: () => checkoutApi.generate(b.MaDatPhong!, km.trim() || null), lamMoi: [['checkout', maPhong], ['checkout']],
    thanhCong: 'Đã cập nhật mã khuyến mãi thành công!', tienToLoi: 'Lỗi: '
  });
  return (
    <form className="o-coc" onSubmit={(e) => { e.preventDefault(); ap.mutate(); }} aria-label="Mã khuyến mãi">
      <span className="small"><strong>MÃ KHUYẾN MÃI</strong></span>
      <div className="o-ghep">
        <input value={km} placeholder="Nhập mã ưu đãi..." aria-label="Mã ưu đãi" onChange={(e) => setKm(e.target.value)} />
        <button type="submit" className="btn btn-gold btn-sm" disabled={ap.isPending}>Áp dụng</button>
      </div>
    </form>
  );
}

function ThanhToan({ hd, kh }: { hd: Invoice; kh: Customer | null }) {
  const navigate = useNavigate();
  const toast = useToast();
  const tv = kh?.ThanhVien;
  const [pt, setPt] = useState(PHUONG_THUC_QUAY[0]);
  const [diem, setDiem] = useState(0);
  const [tach, setTach] = useState(false);
  const [khoan, setKhoan] = useState(() => [0, 1, 2].map(() => ({ PhuongThuc: PHUONG_THUC_QUAY[0], SoTien: '', GhiChu: '' })));

  const conPhaiTra = Math.max(0, Math.round(hd.SoTienConLai ?? 0));
  const giaTriDiem = tv?.GiaTriMotDiem ?? 0;
  const diemToiDa = kh && tv && giaTriDiem > 0 ? Math.min(kh.DiemTichLuy ?? 0, Math.floor(conPhaiTra / giaTriDiem)) : 0;
  const canThu = Math.round(hd.SoTienConLai ?? 0) - Math.max(0, diem) * giaTriDiem;
  const daChia = khoan.reduce((t, k) => t + Math.max(0, Number(k.SoTien) || 0), 0);
  const lech = canThu - daChia;

  const tra = useThaoTac({
    mutationFn: () => checkoutApi.pay(hd.MaHoaDon!, pt, Math.max(0, diem),
      tach ? khoan.filter((k) => Number(k.SoTien) > 0).map((k) => ({ PhuongThuc: k.PhuongThuc, SoTien: Number(k.SoTien), GhiChu: k.GhiChu.trim() || null })) : null),
    lamMoi: LAM_MOI, thanhCong: 'Thanh toán và trả phòng thành công!', tienToLoi: 'Thanh toán thất bại. ',
    onSuccess: () => navigate('/nhan-vien/le-tan', { state: { inHoaDon: hd.MaHoaDon } })
  });
  const gui = () => {
    if (tach && lech !== 0) { toast.loi('Tổng các khoản phải bằng số tiền cần thu.'); return; }
    tra.mutate();
  };
  const batTach = (on: boolean) => {
    setTach(on);
    if (on && !khoan[0].SoTien) setKhoan((ks) => ks.map((k, i) => (i === 0 ? { ...k, SoTien: String(canThu) } : k)));
  };

  return (
    <form className="chot-ca" onSubmit={(e) => { e.preventDefault(); gui(); }} aria-label="Thanh toán">
      <fieldset className={`tp-pt${tach ? ' tp-mo' : ''}`}>
        <legend className="small"><strong>PHƯƠNG THỨC THANH TOÁN</strong></legend>
        {PHUONG_THUC_QUAY.map((x) => (
          <label key={x} className={`tp-pt-the${pt === x ? ' dang-chon' : ''}`}>
            <input type="radio" name="phuongThuc" value={x} checked={pt === x} onChange={() => setPt(x)} />
            <span><strong>{x}</strong><small className="muted">{MO_TA_PT[x]}</small></span>
          </label>
        ))}
      </fieldset>
      {kh && tv && (
        <div className="o-coc tp-diem">
          <div className="the-dau"><strong className="small">♛ {tv.Hang}</strong><span className="small">Hiện có <strong>{so(kh.DiemTichLuy)}</strong> điểm</span></div>
          {diemToiDa >= (tv.DiemDoiToiThieu ?? 0) ? (
            <>
              <label className="field"><span>Đổi điểm (1 điểm = {so(giaTriDiem)} ₫, tối thiểu {tv.DiemDoiToiThieu})</span>
                <span className="o-ghep">
                  <input type="number" min={0} max={diemToiDa} step={1} value={diem} onChange={(e) => setDiem(Math.max(0, parseInt(e.target.value || '0', 10)))} aria-label="Số điểm đổi" />
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => setDiem(diemToiDa)}>Tối đa</button>
                </span>
              </label>
              <small className="muted">Trừ vào hoá đơn: <strong id="tp-diem-quy-doi">{tien(Math.max(0, diem) * giaTriDiem)}</strong> — phần còn lại thu bằng phương thức đã chọn.</small>
            </>
          ) : <small className="muted">Chưa đủ điểm để đổi (tối thiểu {tv.DiemDoiToiThieu} điểm).</small>}
        </div>
      )}
      {(hd.SoTienConLai ?? 0) > 0 && (
        <div className="o-coc">
          <label className="o-chon"><input type="checkbox" checked={tach} onChange={(e) => batTach(e.target.checked)} /> <strong className="small">Tách thanh toán (tối đa 3 khoản)</strong></label>
          {tach && (
            <>
              {khoan.map((k, i) => (
                <div key={i} className="tp-khoan">
                  <select value={k.PhuongThuc} aria-label={`Phương thức khoản ${i + 1}`} onChange={(e) => setKhoan(khoan.map((x, j) => (j === i ? { ...x, PhuongThuc: e.target.value } : x)))}>
                    {PHUONG_THUC_QUAY.map((x) => <option key={x}>{x}</option>)}
                  </select>
                  <input type="number" min={0} step={1000} placeholder="Số tiền" aria-label={`Số tiền khoản ${i + 1}`} value={k.SoTien} onChange={(e) => setKhoan(khoan.map((x, j) => (j === i ? { ...x, SoTien: e.target.value } : x)))} />
                  <input maxLength={255} placeholder="Ghi chú (VD: công ty trả)" aria-label={`Ghi chú khoản ${i + 1}`} value={k.GhiChu} onChange={(e) => setKhoan(khoan.map((x, j) => (j === i ? { ...x, GhiChu: e.target.value } : x)))} />
                </div>
              ))}
              <small id="tp-chia" className={lech === 0 ? 'chu-ok' : 'chu-loi'}>Đã chia {so(daChia)} ₫ / cần thu {so(canThu)} ₫{lech !== 0 ? ` — còn lệch ${so(lech)} ₫` : ' ✓'}</small>
            </>
          )}
        </div>
      )}
      <button type="submit" className="btn btn-gold w-100" disabled={tra.isPending}>Xác nhận thanh toán &amp; trả phòng</button>
    </form>
  );
}

function XuatVat({ hd, maPhong }: { hd: Invoice; maPhong: string }) {
  const [f, setF] = useState({ TenCongTy: hd.TenCongTy ?? '', MaSoThue: hd.MaSoThue ?? '', EmailHoaDon: hd.EmailHoaDon ?? '', DiaChiCongTy: hd.DiaChiCongTy ?? '' });
  const luu = useThaoTac({
    mutationFn: () => checkoutApi.setCompany(hd.MaHoaDon!, { TenCongTy: f.TenCongTy.trim() || null, MaSoThue: f.MaSoThue.trim() || null, EmailHoaDon: f.EmailHoaDon.trim() || null, DiaChiCongTy: f.DiaChiCongTy.trim() || null }),
    lamMoi: [['checkout', maPhong]], tienToLoi: 'Không lưu được thông tin xuất hoá đơn. ',
    thanhCong: (h) => (h?.MaSoThue ? `Đã lưu thông tin xuất hoá đơn cho ${h.TenCongTy}.` : 'Hoá đơn xuất cho cá nhân.')
  });
  return (
    <details className="tp-vat" open={!!hd.MaSoThue}>
      <summary>Xuất hoá đơn VAT cho công ty{hd.MaSoThue && <span className="small chu-ok"> · MST {hd.MaSoThue}</span>}</summary>
      <form className="tp-vat-form" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <input className="tp-rong" maxLength={200} placeholder="Tên công ty" aria-label="Tên công ty" value={f.TenCongTy} onChange={(e) => setF({ ...f, TenCongTy: e.target.value })} />
        <input maxLength={20} placeholder="Mã số thuế" aria-label="Mã số thuế" value={f.MaSoThue} onChange={(e) => setF({ ...f, MaSoThue: e.target.value })} />
        <input type="email" maxLength={100} placeholder="Email nhận hoá đơn" aria-label="Email nhận hoá đơn" value={f.EmailHoaDon} onChange={(e) => setF({ ...f, EmailHoaDon: e.target.value })} />
        <input className="tp-rong" maxLength={300} placeholder="Địa chỉ công ty" aria-label="Địa chỉ công ty" value={f.DiaChiCongTy} onChange={(e) => setF({ ...f, DiaChiCongTy: e.target.value })} />
        <div className="tp-rong the-dau"><small className="muted">Để trống mã số thuế: hoá đơn cá nhân.</small><button type="submit" className="btn btn-outline btn-sm" disabled={luu.isPending}>Lưu</button></div>
      </form>
    </details>
  );
}

