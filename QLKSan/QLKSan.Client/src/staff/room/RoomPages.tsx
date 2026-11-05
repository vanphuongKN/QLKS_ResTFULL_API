import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { roomApi, type Room, type RoomDetail, type RoomStatus } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { useAuth } from '../../auth/AuthContext';
import { coQuyen, VAI_TRO } from '../../auth/roles';
import { ErrorBox, Loading } from '../../components/common';
import { ngay, ngayGio, tien } from '../../utils/format';
import './room.css';

const NHAN: Record<RoomStatus, [string, string]> = {
  Available: ['p-trong', 'Trống'], Occupied: ['p-dang-o', 'Đang ở'], Booked: ['p-da-dat', 'Đã đặt'], Cleaning: ['p-dang-don', 'Đang dọn'], Maintenance: ['p-bao-tri', 'Bảo trì']
};
const LOC: [RoomStatus | 'all', string][] = [['all', 'Tất cả phòng'], ['Available', 'Trống'], ['Occupied', 'Đang ở'], ['Booked', 'Đã đặt'], ['Cleaning', 'Đang dọn'], ['Maintenance', 'Bảo trì']];

// Ảnh phòng: ảnh riêng của phòng, không có thì ảnh mẫu theo hạng (cùng quy tắc với Views/Room của MVC)
export function anhPhong(r: { HinhAnhs?: { DuongDanAnh?: string | null }[] | null; LoaiPhong?: { TenLoai?: string | null } | null }) {
  const rieng = r.HinhAnhs?.[0]?.DuongDanAnh;
  if (rieng) return rieng;
  const t = (r.LoaiPhong?.TenLoai ?? '').toLowerCase();
  if (['presidential', 'vip', 'suite', 'sut'].some((x) => t.includes(x))) return '/images/room_presidential.png';
  if (['deluxe', 'dlx', 'superior', 'sup', 'ocean'].some((x) => t.includes(x))) return '/images/room_ocean.png';
  return '/images/room_garden.png';
}
// Ảnh riêng của phòng lỗi (vd đường dẫn trong dữ liệu mẫu không có tệp) → dùng ảnh mẫu theo hạng phòng
export function AnhPhong({ r, alt = '' }: { r: Parameters<typeof anhPhong>[0]; alt?: string }) {
  const duPhong = anhPhong({ LoaiPhong: r.LoaiPhong });
  return <img src={anhPhong(r)} alt={alt} onError={(e) => { if (!e.currentTarget.src.endsWith(duPhong)) e.currentTarget.src = duPhong; }} />;
}
const tenKhach = (k?: { Ho?: string | null; Ten?: string | null } | null) => (k ? `${k.Ho ?? ''} ${k.Ten ?? ''}`.trim() : '');

// Phòng nghỉ: tổng quan trạng thái từng phòng, lọc theo trạng thái, tìm theo số phòng / hạng / tên khách (Room/Status của MVC)
export function RoomStatusPage() {
  const q = useQuery({ queryKey: ['rooms'], queryFn: roomApi.list });
  const navigate = useNavigate();
  const [loc, setLoc] = useState<RoomStatus | 'all'>('all');
  const [tim, setTim] = useState('');
  if (q.isLoading) return <Loading />;
  const ds = q.data ?? [];
  const dangO = ds.filter((r) => r.TrangThai === 'Occupied').length;
  const khachCua = (r: Room) => (r.TrangThai === 'Occupied' ? tenKhach(r.DangLuuTru?.KhachHang) || 'Khách lưu trú' : r.TrangThai === 'Booked' ? tenKhach(r.DatPhongHienTai?.KhachHang) || 'Khách đã đặt' : '');
  const t = tim.trim().toLowerCase();
  const hien = ds.filter((r) => (loc === 'all' || r.TrangThai === loc)
    && (!t || [r.SoPhong, r.LoaiPhong?.TenLoai, khachCua(r)].some((x) => (x ?? '').toLowerCase().includes(t))));

  return (
    <>
      <section className="phong-hero">
        <h1>Tổng quan trạng thái phòng</h1>
        <p>Tình trạng từng phòng theo thời gian thực: phòng trống, đang có khách, sắp có khách đến, đang dọn và đang bảo trì.</p>
        <div className="phong-hero-so">
          <div><span>Công suất phòng</span><strong>{ds.length ? Math.floor((dangO * 100) / ds.length) : 0}%</strong></div>
          <div><span>Tổng số phòng</span><strong>{ds.length}</strong></div>
        </div>
      </section>
      <ErrorBox message={q.error?.message} />
      <div className="loc-chip" role="group" aria-label="Lọc theo trạng thái">
        {LOC.map(([k, n]) => <button key={k} type="button" className={loc === k ? 'active' : ''} aria-pressed={loc === k} onClick={() => setLoc(k)}>{n}</button>)}
      </div>
      <label className="o-tim"><span className="sr-only">Tìm phòng</span><input type="search" value={tim} onChange={(e) => setTim(e.target.value)} placeholder="Tìm phòng..." /></label>
      {ds.length === 0 ? <p className="muted">Chưa có phòng nào.</p> : (
        <div className="the-phong-luoi">
          {hien.map((r) => {
            const [css, nhan] = NHAN[r.TrangThai ?? 'Available'];
            const o = r.DangLuuTru, d = r.DatPhongHienTai;
            return (
              <article key={r.MaPhong} className={`the-phong ${css}`} data-trang-thai={r.TrangThai}>
                <Link to={`/nhan-vien/phong/${r.MaPhong}`} className="the-phong-link" aria-label={`Phòng ${r.SoPhong} — ${nhan}`}>
                  <div className="the-phong-dau">
                    <div><span className="the-phong-hang">{r.LoaiPhong?.TenLoai}</span><div className="the-phong-so">Phòng {r.SoPhong}</div></div>
                    <span className={`badge-tt ${css}`}>{nhan}</span>
                  </div>
                  <AnhPhong r={r} />
                  {r.TrangThai === 'Available' && <p className="small muted">Sẵn sàng nhận phòng</p>}
                  {r.TrangThai === 'Occupied' && (
                    <>
                      <p className="small"><strong>{khachCua(r)}</strong></p>
                      <p className="small muted">{o?.NgayCheckInThucTe ? ngay(o.NgayCheckInThucTe).slice(0, 5) : '--'} - {o ? ngay(o.NgayCheckOutDuKien).slice(0, 5) : '--'}</p>
                      <p className="the-phong-dv small"><span className="muted">Dịch vụ đã gọi</span><strong>{o?.SuDungDichVus?.length ?? 0} mục</strong></p>
                    </>
                  )}
                  {r.TrangThai === 'Booked' && (
                    <>
                      <p className="small"><strong>{khachCua(r)}</strong></p>
                      <p className="small muted">Dự kiến nhận phòng: {d ? ngay(d.NgayCheckInDuKien) : '—'}</p>
                    </>
                  )}
                  {r.TrangThai === 'Cleaning' && <p className="small muted">Chờ buồng phòng dọn xong trước khi đón khách mới</p>}
                  {r.TrangThai === 'Maintenance' && <p className="small muted">Tạm ngưng nhận khách cho đến khi khắc phục xong</p>}
                </Link>
                {r.TrangThai === 'Booked' && <button type="button" className="btn btn-outline btn-sm w-100" onClick={() => navigate('/nhan-vien/lich-phong')}>Xem lịch phòng</button>}
              </article>
            );
          })}
        </div>
      )}
    </>
  );
}

const TIEU_DE: Record<RoomStatus, string> = {
  Available: 'TRỐNG - SẴN SÀNG ĐÓN KHÁCH', Occupied: 'ĐANG LƯU TRÚ', Booked: 'ĐÃ ĐƯỢC ĐẶT TRƯỚC', Cleaning: 'ĐANG DỌN DẸP', Maintenance: 'ĐANG BẢO TRÌ'
};

// Chi tiết phòng: thông số, tiện ích, khách đang ở / đặt trước, dịch vụ đã gọi, đổi trạng thái thủ công (Room/Details của MVC)
export function RoomDetailPage() {
  const { maPhong = '' } = useParams();
  const { session } = useAuth();
  const q = useQuery({ queryKey: ['rooms', maPhong], queryFn: () => roomApi.get(maPhong) });
  const doi = useThaoTac({
    mutationFn: (tt: RoomStatus) => roomApi.setStatus(maPhong, tt), lamMoi: [['rooms'], ['hk']],
    thanhCong: 'Cập nhật trạng thái phòng thành công!', tienToLoi: 'Có lỗi xảy ra: '
  });
  if (q.isLoading) return <Loading />;
  if (!q.data) return <><ErrorBox message={q.error?.message ?? 'Không tìm thấy phòng.'} /><Link to="/nhan-vien/phong">← Quay lại tổng quan phòng</Link></>;

  const r: RoomDetail = q.data;
  const tt = r.TrangThai ?? 'Available';
  const [css] = NHAN[tt];
  const coQuyenDoi = coQuyen(session?.vaiTro, VAI_TRO.leTan);
  const o = r.DangLuuTru, d = r.DatPhongHienTai;
  const nut = (trangThai: RoomStatus, nhan: string, lop: string) => (
    <button type="button" className={`btn btn-sm ${lop}`} disabled={doi.isPending} onClick={() => doi.mutate(trangThai)}>{nhan}</button>
  );

  return (
    <div className="chi-tiet-phong">
      <div className="trang-dau">
        <div>
          <Link to="/nhan-vien/phong" className="small">← Quay lại tổng quan phòng</Link>
          <h1>Hồ sơ chi tiết Phòng {r.SoPhong}</h1>
        </div>
        <span className={`badge-tt ${css} badge-to`}>{TIEU_DE[tt]}</span>
      </div>

      <div className="ctp-luoi">
        <div>
          <figure className="ctp-anh">
            <AnhPhong r={r} alt={`Phòng ${r.SoPhong}`} />
            <figcaption><span>{r.LoaiPhong?.TenLoai}</span><strong>Phòng Hạng {r.SoPhong}</strong></figcaption>
          </figure>
          <section className="the-trang">
            <h2>Thông số kỹ thuật phòng</h2>
            <dl className="ctp-thong-so">
              <dt>Hạng phòng</dt><dd>{r.LoaiPhong?.TenLoai}</dd>
              <dt>Sức chứa tối đa</dt><dd>{r.LoaiPhong?.SoNguoiToiDa} Người lớn</dd>
              <dt>Đơn giá ngày</dt><dd className="chu-vang">{tien(r.LoaiPhong?.GiaThueNgay)} / đêm</dd>
              <dt>Đơn giá giờ</dt><dd>{tien(r.LoaiPhong?.GiaThueGio)} / giờ</dd>
            </dl>
            <p className="small muted">{r.LoaiPhong?.MoTa || 'Hệ thống phòng lưu trú sang trọng được trang bị đầy đủ vật phẩm chăm sóc cá nhân cao cấp, điều hòa không khí, smart TV, wifi tốc độ cao và dịch vụ dọn phòng 24/7.'}</p>
          </section>
          <section className="the-trang">
            <h2>Trang thiết bị &amp; Tiện ích</h2>
            <div className="ctp-tien-ich">
              {(r.TienIchs?.length ? r.TienIchs.map((x) => x.TenTienIch ?? '') : ['Wifi siêu tốc', 'Điều hòa', 'Smart TV', 'Bồn tắm', 'Mini bar']).map((x) => <span key={x}>{x}</span>)}
            </div>
          </section>
        </div>

        <div>
          {tt === 'Occupied' && o ? (
            <>
              <section className={`the-trang ctp-vien ${css}`}>
                <div className="the-dau"><h2>Thông tin khách đang ở</h2><span className={`badge-tt ${css}`}>Đang ở</span></div>
                <dl className="ctp-khach">
                  <div><dt>Tên khách hàng</dt><dd><strong>{tenKhach(o.KhachHang)}</strong></dd></div>
                  <div><dt>Mã đặt phòng</dt><dd>{o.MaDatPhong}</dd></div>
                  <div><dt>Thời gian Check-in</dt><dd>{ngayGio(o.NgayCheckInThucTe ?? o.NgayCheckInDuKien)}</dd></div>
                  <div><dt>Dự kiến Check-out</dt><dd>{ngay(o.NgayCheckOutDuKien)}</dd></div>
                  <div><dt>Liên hệ khách hàng</dt><dd>{o.KhachHang?.SDT ?? 'Chưa có SĐT'} - {o.KhachHang?.Email ?? 'Chưa có Email'}</dd></div>
                  <div><dt>Thời gian lưu trú</dt><dd className="chu-vang"><strong>{soDemLuuTru(o.NgayCheckInThucTe ?? o.NgayCheckInDuKien, o.NgayCheckOutDuKien)} đêm</strong></dd></div>
                </dl>
              </section>
              <section className="the-trang">
                <div className="the-dau">
                  <h2>Dịch vụ đã gọi trong kỳ lưu trú</h2>
                  <Link to={`/nhan-vien/dich-vu?maPhong=${r.MaPhong}`} className="btn btn-outline btn-sm">+ Thêm dịch vụ</Link>
                </div>
                {o.SuDungDichVus?.length ? (
                  <div className="bang-cuon">
                    <table className="bang ctp-dv">
                      <thead><tr><th>Dịch vụ</th><th className="canh-giua">Số lượng</th><th className="canh-phai">Đơn giá</th><th className="canh-phai">Tổng tiền</th></tr></thead>
                      <tbody>
                        {o.SuDungDichVus.map((s) => (
                          <tr key={s.MaSDDV}><td>{s.TenDichVu}</td><td className="canh-giua">{s.SoLuong}</td><td className="canh-phai">{tien(s.GiaThucTe)}</td><td className="canh-phai"><strong>{tien(s.ThanhTien)}</strong></td></tr>
                        ))}
                        <tr className="ctp-tong"><td colSpan={3} className="canh-phai">TỔNG TIỀN DỊCH VỤ:</td><td className="canh-phai">{tien(o.SuDungDichVus.reduce((t, s) => t + (s.ThanhTien ?? 0), 0))}</td></tr>
                      </tbody>
                    </table>
                  </div>
                ) : <p className="small muted canh-giua">Chưa có dịch vụ nào được gọi trong đợt lưu trú này.</p>}
              </section>
              <section className="the-trang">
                <h2>Quy trình Trả phòng &amp; Thanh toán</h2>
                <p className="small muted">Tiền phòng tính theo số đêm thực tế (giá cuối tuần / lễ nếu có), cộng tiền dịch vụ đã dùng, trừ khuyến mãi và tiền cọc.</p>
                <Link to={`/nhan-vien/tra-phong?maPhong=${r.MaPhong}`} className="btn btn-gold w-100">Tiến hành thanh toán &amp; xuất hoá đơn</Link>
              </section>
            </>
          ) : tt === 'Booked' && d ? (
            <section className={`the-trang ctp-vien ${css}`}>
              <h2>Lịch trình đặt trước hoạt động</h2>
              <dl className="ctp-khach">
                <div><dt>Tên khách hàng</dt><dd><strong>{tenKhach(d.KhachHang)}</strong></dd></div>
                <div><dt>Mã đặt phòng</dt><dd>{d.MaDatPhong}</dd></div>
                <div><dt>Dự kiến Check-in</dt><dd>{ngay(d.NgayCheckInDuKien)}</dd></div>
                <div><dt>Dự kiến Check-out</dt><dd>{ngay(d.NgayCheckOutDuKien)}</dd></div>
                <div className="ctp-rong"><dt>Liên hệ khách hàng</dt><dd>{d.KhachHang?.SDT ?? 'Chưa có SĐT'} - {d.KhachHang?.Email ?? 'Chưa có Email'}</dd></div>
              </dl>
              <Link to="/nhan-vien/le-tan" className="btn btn-gold w-100">Nhận phòng cho khách (check-in)</Link>
            </section>
          ) : (
            <section className={`the-trang ctp-vien ${css}`}>
              <h2>Trạng thái hiện tại: {tt === 'Available' ? 'Trống' : 'Chờ dọn dẹp'}</h2>
              {tt === 'Available' ? (
                <div className="canh-giua">
                  <h3>Phòng sẵn sàng tiếp đón khách mới</h3>
                  <p className="small muted">Phòng đã được bộ phận Buồng phòng dọn dẹp tiêu chuẩn và kiểm tra an toàn hệ thống điện nước đầy đủ.</p>
                  <Link to="/nhan-vien/le-tan" className="btn btn-gold">Tạo phiếu đặt phòng / nhận phòng ngay</Link>
                </div>
              ) : (
                <div className="canh-giua">
                  <h3>Phòng đang chờ được làm sạch vệ sinh</h3>
                  <p className="small muted">Khách vừa trả phòng hoặc bộ phận quản lý yêu cầu dọn dẹp định kỳ trước khi bàn giao cho phòng trống.</p>
                  {coQuyenDoi && nut('Available', 'Xác nhận đã dọn xong (chuyển sang trống)', 'btn-gold')}
                </div>
              )}
            </section>
          )}

          <section className="the-trang">
            <h2>Bảng điều khiển trạng thái</h2>
            {tt === 'Occupied' || tt === 'Booked' ? (
              <div className="alert alert-warn"><strong>Trạng thái đã khóa tự động</strong><br />Phòng đang có khách hoặc đã được đặt nên trạng thái tự cập nhật theo nhận phòng / trả phòng.</div>
            ) : !coQuyenDoi ? null : tt === 'Available' ? (
              <><p className="small muted">Phòng đang trống. Bạn có thể thay đổi trạng thái nếu phòng cần dọn dẹp hoặc xảy ra sự cố cần bảo trì.</p>
                <div className="row-actions">{nut('Cleaning', 'Yêu cầu dọn dẹp', 'btn-outline')}{nut('Maintenance', 'Báo bảo trì', 'btn-outline')}</div></>
            ) : tt === 'Cleaning' ? (
              <><p className="small muted">Nếu phát hiện hỏng hóc trong quá trình dọn dẹp, bạn có thể chuyển phòng sang trạng thái Bảo trì.</p>
                {nut('Maintenance', 'Báo cáo hỏng hóc & bảo trì', 'btn-outline w-100')}</>
            ) : (
              <><p className="small muted">Phòng đang được bảo trì. Sau khi sửa chữa xong, vui lòng chuyển về trạng thái Trống hoặc Dọn dẹp.</p>
                <div className="row-actions">{nut('Available', 'Hoàn tất (trống)', 'btn-outline')}{nut('Cleaning', 'Cần dọn dẹp lại', 'btn-outline')}</div></>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}

// Số đêm lưu trú: làm tròn lên, tối thiểu 1 (như MVC: Ceiling((checkout - checkin).TotalDays))
export function soDemLuuTru(tu?: string | null, den?: string | null) {
  if (!tu || !den) return 1;
  const ms = new Date(den).getTime() - new Date(tu).getTime();
  return Math.max(1, Math.ceil(ms / 86_400_000));
}
