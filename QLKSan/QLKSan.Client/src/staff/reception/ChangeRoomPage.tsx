import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { availableRooms, bookingApi, type Room } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { useXacNhan } from '../../components/ui/Dialog';
import { PageHeader } from '../../components/ui/Page';
import { addDays, homNay, ngay, ngayGio, tenHangPhong, tien } from '../../utils/format';
import './reception.css';

// Đổi phòng (Reception/ChangeRoom của MVC): chưa nhận phòng → đổi phòng đã xếp; đang ở → chuyển phòng (cần lý do, tuỳ chọn giữ giá cũ)
export function ChangeRoomPage() {
  const { maDatPhong = '' } = useParams();
  const navigate = useNavigate();
  const xacNhan = useXacNhan();
  const phieu = useQuery({ queryKey: ['booking', maDatPhong], queryFn: () => bookingApi.get(maDatPhong) });
  const b = phieu.data;
  const choPhep = b?.TrangThai === 'Pending' || b?.TrangThai === 'Approved';
  const dangO = b?.TrangThai === 'CheckedIn';
  // Đang ở: phòng phải sạch, sẵn sàng ngay và trống tới ngày trả
  const denNgay = b ? ((b.NgayCheckOutDuKien ?? '').slice(0, 10) > homNay() ? (b.NgayCheckOutDuKien ?? '').slice(0, 10) : addDays(homNay(), 1)) : '';
  const phong = useQuery({
    queryKey: ['rooms', 'available', maDatPhong, b?.TrangThai],
    enabled: !!b && (choPhep || dangO),
    queryFn: async () => dangO
      ? (await availableRooms(homNay(), denNgay, b!.SoLuongKhach ?? 1)).filter((r) => r.TrangThai === 'Available')
      : availableRooms((b!.NgayCheckInDuKien ?? '').slice(0, 10), (b!.NgayCheckOutDuKien ?? '').slice(0, 10), b!.SoLuongKhach ?? 1)
  });
  const [chon, setChon] = useState('');
  const [lyDo, setLyDo] = useState('');
  const [giuGia, setGiuGia] = useState(true);
  const xong = () => navigate(`/nhan-vien/le-tan${dangO ? '#dango' : ''}`);
  const doi = useThaoTac({
    mutationFn: async (maPhong: string) => { if (dangO) await bookingApi.moveRoom(maDatPhong, maPhong, lyDo.trim(), giuGia); else await bookingApi.changeRoom(maDatPhong, maPhong); },
    lamMoi: [['front-desk'], ['rooms'], ['booking'], ['hk']], thanhCong: `Đã đổi phòng cho phiếu ${maDatPhong}.`, tienToLoi: 'Không thể đổi phòng. ', onSuccess: xong
  });

  if (phieu.isLoading) return <Loading />;
  if (!b) return <><ErrorBox message={phieu.error?.message ?? 'Không tìm thấy phiếu.'} /><Link to="/nhan-vien/le-tan">← Lễ tân</Link></>;
  const hienTai = b.ChiTietDatPhongs?.[0];
  const khac = (phong.data ?? []).filter((r) => r.MaPhong !== hienTai?.MaPhong)
    .sort((x, y) => Number(x.LoaiPhong?.MaLoai !== hienTai?.Phong?.MaLoai) - Number(y.LoaiPhong?.MaLoai !== hienTai?.Phong?.MaLoai) || (x.SoPhong ?? '').localeCompare(y.SoPhong ?? '', 'vi', { numeric: true }));
  const cungHang = (r: Room) => r.MaLoai === hienTai?.Phong?.MaLoai;
  const chonPhongNay = async (r: Room) => {
    if (await xacNhan({ tieuDe: `Chuyển phiếu ${maDatPhong} sang phòng ${r.SoPhong}?`, nutXacNhan: 'Chọn phòng này' })) doi.mutate(r.MaPhong!);
  };
  const chuyen = (e: FormEvent) => { e.preventDefault(); if (chon) doi.mutate(chon); };

  return (
    <>
      <PageHeader tieuDe={`${dangO ? 'Đổi phòng khi đang ở' : 'Đổi phòng'} — Phiếu #${b.MaDatPhong}`}
        phu={dangO ? 'Phòng mới phải trống, đã dọn và trống đến ngày trả. Phòng cũ chuyển sang Đang dọn và tự tạo việc cho buồng phòng.' : undefined}>
        <Link to="/nhan-vien/le-tan" className="btn btn-outline btn-sm">← Lễ tân</Link>
      </PageHeader>
      <section className="the-trang doi-phong-tt">
        <div><span>Khách</span><strong>{b.KhachHang?.Ho} {b.KhachHang?.Ten}</strong></div>
        <div><span>Lưu trú</span><strong>{ngay(b.NgayCheckInDuKien)} → {ngay(b.NgayCheckOutDuKien)}</strong></div>
        <div><span>Số khách</span><strong>{b.SoLuongKhach}</strong></div>
        <div><span>Phòng hiện tại</span><strong>{hienTai?.Phong?.SoPhong ?? '—'} · {hienTai?.Phong?.LoaiPhong?.TenLoai}</strong> <small className="muted">({tien(hienTai?.GiaThueThucTe)}/đêm)</small></div>
        {(b.LichSuDoiPhongs ?? []).map((mv, i) => (
          <p key={i} className="small muted dp-rong">{ngayGio(mv.ThoiGian).slice(0, 5)} {mv.ThoiGian?.slice(11, 16)}: phòng {mv.SoPhongCu} → {mv.SoPhongMoi} — {mv.LyDo} ({mv.TenNhanVien})</p>
        ))}
        {b.YeuCauDacBiet && <div className="dp-rong"><span>Yêu cầu của khách</span>{b.YeuCauDacBiet}</div>}
      </section>

      {!choPhep && !dangO ? <div className="alert alert-warn">Phiếu đã trả phòng hoặc đã huỷ, không đổi phòng được.</div>
        : phong.isLoading ? <Loading />
          : khac.length === 0 ? <div className="alert alert-info">Không còn phòng trống khác đủ sức chứa trong khoảng ngày này{dangO ? ' (phòng phải đã dọn xong)' : ''}.</div>
            : dangO ? (
              <form className="the-trang" onSubmit={chuyen} aria-label="Chuyển phòng">
                <div className="chon-phong-luoi" role="radiogroup" aria-label="Phòng mới">
                  {khac.map((r) => (
                    <label key={r.MaPhong} className={`chon-phong${chon === r.MaPhong ? ' dang-chon' : ''}`}>
                      <input type="radio" name="maPhong" value={r.MaPhong!} checked={chon === r.MaPhong} onChange={() => setChon(r.MaPhong!)} required />
                      <span><strong>Phòng {r.SoPhong}</strong>{cungHang(r) && <span className="badge badge-gold">cùng hạng</span>}
                        <small>{tenHangPhong(r.LoaiPhong?.TenLoai)} · tối đa {r.LoaiPhong?.SoNguoiToiDa} khách · {tien(r.LoaiPhong?.GiaThueNgay)}/đêm</small></span>
                    </label>
                  ))}
                </div>
                <div className="form-grid">
                  <label className="field"><span>Lý do đổi phòng</span><input value={lyDo} maxLength={255} required placeholder="VD: Điều hoà hỏng, khách muốn tầng cao…" onChange={(e) => setLyDo(e.target.value)} /></label>
                  <div>
                    <label className="check"><input type="checkbox" checked={giuGia} onChange={(e) => setGiuGia(e.target.checked)} /> Giữ giá phòng cũ ({tien(hienTai?.GiaThueThucTe)}/đêm)</label>
                    <small className="muted">Bỏ chọn để tính theo giá hạng phòng mới cho cả kỳ nghỉ.</small>
                  </div>
                </div>
                <div className="modal-chan"><button type="submit" className="btn btn-gold" disabled={doi.isPending}>Chuyển phòng</button></div>
              </form>
            ) : (
              <div className="bang-cuon">
                <table className="bang">
                  <thead><tr><th>Phòng</th><th>Hạng phòng</th><th>Sức chứa</th><th>Giá / đêm</th><th aria-label="Chọn" /></tr></thead>
                  <tbody>
                    {khac.map((r) => (
                      <tr key={r.MaPhong}>
                        <td><strong>{r.SoPhong}</strong></td>
                        <td>{r.LoaiPhong?.TenLoai} {cungHang(r) && <span className="badge badge-gold">cùng hạng</span>}</td>
                        <td>{r.LoaiPhong?.SoNguoiToiDa} khách</td>
                        <td>{tien(r.LoaiPhong?.GiaThueNgay)}</td>
                        <td className="canh-phai"><button type="button" className="btn btn-gold btn-sm" disabled={doi.isPending} onClick={() => chonPhongNay(r)}>Chọn phòng này</button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
    </>
  );
}
