import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorBox, Loading } from '../components/common';
import { TrangIn } from '../components/ui/Page';
import { ngay, ngayGio, so, tenHangPhong } from '../utils/format';
import './invoice-print.css';

const TRANG_THAI_HD: Record<string, string> = { Unpaid: 'Chưa thanh toán', Paid: 'Đã thanh toán', Cancelled: 'Đã hủy' };

// Bản in hoá đơn (trình duyệt "In" / "Lưu thành PDF"), cùng nội dung với Invoice/Print của MVC.
// Thu ngân / kế toán in mọi hoá đơn; khách chỉ mở được hoá đơn của mình (API kiểm tra quyền sở hữu, trả 403).
export function InvoicePrintPage() {
  const { maHoaDon = '' } = useParams();
  const { session } = useAuth();
  const navigate = useNavigate();
  const q = useQuery({ queryKey: ['invoice', maHoaDon], queryFn: () => api.invoice(maHoaDon) });
  const laKhach = session?.vaiTro === 'Customer';
  const quayLai = laKhach ? () => navigate('/phieu') : undefined;

  if (q.isLoading) return <Loading />;
  if (q.error || !q.data?.HoaDon) {
    return (
      <TrangIn tieuDe="Hoá đơn" onQuayLai={quayLai} nhanQuayLai={laKhach ? '← Lịch sử kỳ nghỉ' : undefined}>
        <ErrorBox message={q.error?.message ?? 'Không tìm thấy hoá đơn.'} />
      </TrangIn>
    );
  }

  const d = q.data;
  const h = d.HoaDon!;
  const b = d.PhieuDatPhong;
  const room = b?.ChiTietDatPhongs?.[0];
  const tenHang = tenHangPhong(room?.Phong?.LoaiPhong?.TenLoai);
  const daThanhToan = h.TrangThai === 'Paid';
  const tamTinh = (h.TongTienPhong ?? 0) + (h.TongTienDichVu ?? 0) + (h.TienPhat ?? 0) - (h.GiamGiaKhuyenMai ?? 0) - (h.GiamGiaThanhVien ?? 0);
  const coGiaRiengTungDem = (h.SoDem ?? 0) > 0 && !!room && h.TongTienPhong !== (room.GiaThueThucTe ?? 0) * (h.SoDem ?? 0);
  let stt = 1;

  return (
    <TrangIn tieuDe={`Hoá đơn ${h.MaHoaDon}`} onQuayLai={quayLai} nhanQuayLai={laKhach ? '← Lịch sử kỳ nghỉ' : undefined}>
      <div className="hd">
        <div className="hd-dau">
          <div>
            <div className="hd-brand">LUXE RESERVE</div>
            <div className="muted">Khách sạn &amp; Khu nghỉ dưỡng</div>
          </div>
          <div className="hd-so">
            <div><strong>Số hoá đơn:</strong> {h.MaHoaDon}</div>
            <div><strong>Ngày lập:</strong> {ngayGio(h.NgayLapHoaDon)}</div>
            <span className={`hd-dau-moc${daThanhToan ? '' : ' nhap'}`}>{daThanhToan ? 'ĐÃ THANH TOÁN' : (TRANG_THAI_HD[h.TrangThai ?? ''] ?? '').toUpperCase()}</span>
          </div>
        </div>
        <h1>{daThanhToan ? 'HOÁ ĐƠN THANH TOÁN' : 'PHIẾU TẠM TÍNH'}</h1>
        <div className="hd-phu muted">Phiếu đặt phòng {b?.MaDatPhong}</div>

        <div className="hd-tt">
          <div><span>Khách hàng:</span> <strong>{b?.KhachHang?.HoTen}</strong></div>
          <div><span>Phòng:</span> <strong>{room?.Phong?.SoPhong}</strong> — {tenHang}</div>
          <div><span>CCCD/Hộ chiếu:</span> {b?.KhachHang?.CCCD_Passport}</div>
          <div><span>Số khách:</span> {b?.SoLuongKhach}</div>
          <div><span>Điện thoại:</span> {b?.KhachHang?.SDT}</div>
          <div><span>Nhận phòng:</span> {b?.NgayCheckInThucTe ? ngayGio(b.NgayCheckInThucTe) : ngay(b?.NgayCheckInDuKien)}</div>
          <div><span>Email:</span> {b?.KhachHang?.Email}</div>
          <div><span>Trả phòng:</span> {b?.NgayCheckOutThucTe ? ngayGio(b.NgayCheckOutThucTe) : ngay(b?.NgayCheckOutDuKien)}</div>
        </div>
        {h.MaSoThue && (
          <div className="hd-tt">
            <div><span>Đơn vị mua hàng:</span> <strong>{h.TenCongTy}</strong></div>
            <div><span>Mã số thuế:</span> <strong>{h.MaSoThue}</strong></div>
            <div><span>Địa chỉ:</span> {h.DiaChiCongTy}</div>
            <div><span>Email nhận hoá đơn:</span> {h.EmailHoaDon}</div>
          </div>
        )}

        <table className="hd-bang">
          <thead><tr><th className="hd-stt">#</th><th>Nội dung</th><th className="so">Số lượng</th><th className="so">Đơn giá</th><th className="so">Thành tiền (₫)</th></tr></thead>
          <tbody>
            <tr>
              <td>{stt++}</td>
              <td>Tiền phòng {room?.Phong?.SoPhong} ({tenHang}){coGiaRiengTungDem && <><br /><small className="muted">Đã áp dụng giá cuối tuần / dịp lễ theo từng đêm</small></>}</td>
              <td className="so">{h.SoDem} đêm</td>
              <td className="so">{room ? so(room.GiaThueThucTe) : ''}</td>
              <td className="so">{so(h.TongTienPhong)}</td>
            </tr>
            {(b?.SuDungDichVus ?? []).map((s) => (
              <tr key={s.MaSDDV}>
                <td>{stt++}</td>
                <td>{s.TenDichVu} <small className="muted">({ngay(s.NgaySuDung).slice(0, 5)})</small></td>
                <td className="so">{s.SoLuong} {s.DonViTinh}</td>
                <td className="so">{so(s.GiaThucTe)}</td>
                <td className="so">{so(s.ThanhTien)}</td>
              </tr>
            ))}
            {(h.TienPhat ?? 0) > 0 && (
              <tr><td>{stt++}</td><td>Phụ thu trả phòng muộn ({h.PhanTramPhuThu}% giá phòng)</td><td className="so">1</td><td className="so">{so(h.TienPhat)}</td><td className="so">{so(h.TienPhat)}</td></tr>
            )}
          </tbody>
        </table>

        <table className="hd-bang hd-tong">
          <tbody>
            <tr><td>Tiền phòng</td><td className="so">{so(h.TongTienPhong)}</td></tr>
            <tr><td>Tiền dịch vụ</td><td className="so">{so(h.TongTienDichVu)}</td></tr>
            {(h.TienPhat ?? 0) > 0 && <tr><td>Phụ thu trả muộn</td><td className="so">{so(h.TienPhat)}</td></tr>}
            {(h.GiamGiaKhuyenMai ?? 0) > 0 && <tr><td>Khuyến mãi ({h.MaKhuyenMai})</td><td className="so">-{so(h.GiamGiaKhuyenMai)}</td></tr>}
            {(h.GiamGiaThanhVien ?? 0) > 0 && <tr><td>Ưu đãi {h.HangThanhVien}</td><td className="so">-{so(h.GiamGiaThanhVien)}</td></tr>}
            <tr><td>Cộng tiền hàng</td><td className="so">{so(tamTinh)}</td></tr>
            <tr><td>Thuế VAT (8%)</td><td className="so">{so(h.ThueVAT)}</td></tr>
            <tr className="hd-tong-cong"><td>TỔNG CỘNG</td><td className="so">{so(h.TongTienCanThanhToan)} ₫</td></tr>
            {(h.TienCoc ?? 0) > 0 && <tr><td>Đã đặt cọc</td><td className="so">-{so(h.TienCoc)}</td></tr>}
            {daThanhToan ? (
              <>
                <tr><td>Đã thanh toán (gồm cọc)</td><td className="so">{so((d.DaThu ?? 0) + (h.TienCoc ?? 0))}</td></tr>
                {(d.DaHoan ?? 0) > 0 && <tr><td>Đã hoàn lại khách</td><td className="so">-{so(d.DaHoan)}</td></tr>}
              </>
            ) : (
              <tr><td><strong>{(h.SoTienConLai ?? 0) < 0 ? 'Hoàn lại khách' : 'Còn phải thu'}</strong></td><td className="so"><strong>{so(Math.abs(h.SoTienConLai ?? 0))}</strong></td></tr>
            )}
          </tbody>
        </table>

        {(d.GiaoDich ?? []).length > 0 && (
          <table className="hd-bang">
            <thead><tr><th>Mã GD</th><th>Loại</th><th>Thời gian</th><th>Phương thức</th><th className="so">Số tiền (₫)</th></tr></thead>
            <tbody>
              {d.GiaoDich!.map((t) => (
                <tr key={t.MaThanhToan}>
                  <td>{t.MaThanhToan}</td>
                  <td>{t.LoaiGiaoDich === 'Refund' ? 'Hoàn' : 'Thu'}</td>
                  <td>{ngayGio(t.NgayThanhToan)}</td>
                  <td>{t.PhuongThuc}</td>
                  <td className="so">{t.LoaiGiaoDich === 'Refund' ? '-' : ''}{so(t.SoTienDaTra)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <div className="hd-ky">
          <div><p>Khách hàng</p><span className="muted">(Ký, ghi rõ họ tên)</span><br /><br />{b?.KhachHang?.HoTen}</div>
          <div><p>Người lập hoá đơn</p><span className="muted">(Ký, ghi rõ họ tên)</span><br /><br />{d.TenNhanVienLap}</div>
        </div>
        <div className="hd-chan muted">Cảm ơn Quý khách đã lựa chọn LUXE RESERVE. Hẹn gặp lại!</div>
      </div>
    </TrangIn>
  );
}
