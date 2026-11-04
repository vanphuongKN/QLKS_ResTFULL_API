import { useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { catalogApi, roomApi, serviceApi, type Amenity, type Room, type RoomStatus, type RoomTypeAdmin, type RoomTypeForm, type Service, type ServiceForm } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { Modal, useXacNhan } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { useToast } from '../../components/ui/Toast';
import { anh, so, so1, tien } from '../../utils/format';
import './admin.css';

const TEN_TT_PHONG: Record<RoomStatus, string> = { Available: 'Trống', Occupied: 'Đang ở', Booked: 'Đã đặt', Cleaning: 'Đang dọn', Maintenance: 'Bảo trì' };
const CSS_TT_PHONG: Record<RoomStatus, string> = { Available: 'p-trong', Occupied: 'p-dang-o', Booked: 'p-da-dat', Cleaning: 'p-dang-don', Maintenance: 'p-bao-tri' };

// ===== Loại phòng (Admin/RoomTypes) =====
export function RoomTypesPage() {
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['room-types'], queryFn: catalogApi.roomTypes });
  const [form, setForm] = useState<RoomTypeAdmin | 'moi' | null>(null);
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deleteRoomType(ma), lamMoi: [['room-types']], thanhCong: 'Xóa loại phòng thành công!' });
  return (
    <>
      <PageHeader tieuDe="Danh sách loại phòng" phu="Quản lý tài sản">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setForm('moi')}>+ Thêm loại phòng</button>
      </PageHeader>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : (
        <section className="the-trang the-bang">
          <div className="bang-cuon">
            <table className="bang" aria-label="Danh sách loại phòng">
              <thead><tr><th>Ảnh</th><th>Mã loại</th><th>Tên loại phòng</th><th>Sức chứa</th><th>Diện tích</th><th>Giá / Ngày</th><th>Giá / Giờ</th><th>Đánh giá</th><th>Thao tác</th></tr></thead>
              <tbody>
                {!q.data?.length && <tr><td colSpan={9} className="canh-giua muted">Chưa có loại phòng nào.</td></tr>}
                {(q.data ?? []).map((rt) => (
                  <tr key={rt.MaLoai} data-loai={rt.MaLoai}>
                    <td><img className="qt-anh-nho" src={anh(rt.AnhDaiDien)} alt="" /></td>
                    <td><strong>{rt.MaLoai}</strong></td>
                    <td><strong className="chu-vang">{rt.TenLoai}</strong><div className="small muted">{[rt.LoaiGiuong, rt.HuongNhin].filter(Boolean).join(' · ')}</div></td>
                    <td className="nowrap">{rt.SoNguoiToiDa} khách</td>
                    <td className="nowrap">{rt.DienTich != null ? `${so1(rt.DienTich)} m²` : '—'}</td>
                    <td className="nowrap"><strong className="chu-vang">{tien(rt.GiaThueNgay)}</strong></td>
                    <td className="nowrap">{tien(rt.GiaThueGio)}</td>
                    <td className="nowrap">{rt.DiemDanhGia != null ? `${rt.DiemDanhGia.toFixed(1).replace('.', ',')} ★ (${rt.SoDanhGia})` : '—'}</td>
                    <td className="nowrap">
                      <button type="button" className="btn btn-outline btn-sm" title="Sửa" onClick={() => setForm(rt)}>Sửa</button>{' '}
                      <Link to={`/nhan-vien/quan-tri/loai-phong/${rt.MaLoai}/anh`} className="btn btn-outline btn-sm" title="Bộ ảnh">Ảnh</Link>{' '}
                      <button type="button" className="btn btn-outline-danger btn-sm" title="Xoá" disabled={xoa.isPending}
                        onClick={async () => { if (await xacNhan({ tieuDe: 'Xoá loại phòng?', noiDung: `Xóa loại phòng ${rt.TenLoai}?`, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(rt.MaLoai!); }}>Xoá</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
      {form && <HopLoaiPhong rt={form === 'moi' ? null : form} onDong={() => setForm(null)} />}
    </>
  );
}

function HopLoaiPhong({ rt, onDong }: { rt: RoomTypeAdmin | null; onDong: () => void }) {
  const [f, setF] = useState({
    TenLoai: rt?.TenLoai ?? '', SoNguoiToiDa: String(rt?.SoNguoiToiDa ?? ''), GiaThueNgay: rt?.GiaThueNgay != null ? String(Math.floor(rt.GiaThueNgay)) : '',
    GiaThueGio: rt?.GiaThueGio != null ? String(Math.floor(rt.GiaThueGio)) : '', DienTich: rt?.DienTich != null ? String(rt.DienTich) : '',
    LoaiGiuong: rt?.LoaiGiuong ?? '', HuongNhin: rt?.HuongNhin ?? '', MoTa: rt?.MoTa ?? '', MoTaChiTiet: rt?.MoTaChiTiet ?? ''
  });
  const dat = (p: Partial<typeof f>) => setF({ ...f, ...p });
  const r: RoomTypeForm = {
    TenLoai: f.TenLoai.trim(), SoNguoiToiDa: Number(f.SoNguoiToiDa), GiaThueNgay: Number(f.GiaThueNgay), GiaThueGio: Number(f.GiaThueGio),
    DienTich: f.DienTich ? Number(f.DienTich) : null, LoaiGiuong: f.LoaiGiuong || null, HuongNhin: f.HuongNhin || null, MoTa: f.MoTa || null, MoTaChiTiet: f.MoTaChiTiet || null
  };
  const luu = useThaoTac({
    mutationFn: () => (rt ? catalogApi.updateRoomType(rt.MaLoai!, r) : catalogApi.createRoomType(r)), lamMoi: [['room-types']],
    thanhCong: rt ? 'Cập nhật loại phòng thành công!' : 'Thêm loại phòng thành công!', onSuccess: onDong
  });
  return (
    <Modal open rong title={rt ? `Cập nhật loại phòng ${rt.MaLoai}` : 'Thêm loại phòng mới'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Loại phòng">
        <div className="form-grid">
          <label className="field span-2"><span>Tên loại phòng</span><input required maxLength={50} placeholder="VD: Phòng Deluxe Hướng Biển" value={f.TenLoai} onChange={(e) => dat({ TenLoai: e.target.value })} /></label>
          <label className="field"><span>Số người tối đa</span><input type="number" min={1} max={10} required value={f.SoNguoiToiDa} onChange={(e) => dat({ SoNguoiToiDa: e.target.value })} /></label>
          <label className="field"><span>Diện tích (m²)</span><input type="number" min={5} max={500} step={0.5} value={f.DienTich} onChange={(e) => dat({ DienTich: e.target.value })} /></label>
          <label className="field"><span>Giá thuê / Ngày (₫)</span><input type="number" min={0} required value={f.GiaThueNgay} onChange={(e) => dat({ GiaThueNgay: e.target.value })} /></label>
          <label className="field"><span>Giá thuê / Giờ (₫)</span><input type="number" min={0} required value={f.GiaThueGio} onChange={(e) => dat({ GiaThueGio: e.target.value })} /></label>
          <label className="field"><span>Loại giường</span><input maxLength={100} placeholder="VD: 1 giường đôi lớn" value={f.LoaiGiuong} onChange={(e) => dat({ LoaiGiuong: e.target.value })} /></label>
          <label className="field"><span>Hướng nhìn</span><input maxLength={100} placeholder="VD: Hướng biển" value={f.HuongNhin} onChange={(e) => dat({ HuongNhin: e.target.value })} /></label>
          <label className="field span-2"><span>Mô tả ngắn (thẻ hạng phòng, trang đặt phòng)</span><textarea rows={2} maxLength={500} value={f.MoTa} onChange={(e) => dat({ MoTa: e.target.value })} /></label>
          <label className="field span-2"><span>Mô tả chi tiết (trang chi tiết hạng phòng)</span><textarea rows={4} maxLength={4000} value={f.MoTaChiTiet} onChange={(e) => dat({ MoTaChiTiet: e.target.value })} /></label>
        </div>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>{rt ? 'Cập nhật' : 'Thêm'}</button></div>
      </form>
    </Modal>
  );
}

// Bộ ảnh hạng phòng (Admin/RoomTypeImages): tải ảnh JPG / PNG / WebP ≤ 5 MB, ảnh đầu là ảnh đại diện
export function RoomTypeImagesPage() {
  const { maLoai = '' } = useParams();
  const xacNhan = useXacNhan();
  const toast = useToast();
  const q = useQuery({ queryKey: ['room-types', 'detail', maLoai], queryFn: () => catalogApi.roomType(maLoai) });
  const tep = useRef<HTMLInputElement>(null);
  const lamMoi = [['room-types'], ['room-type', maLoai]];
  const tai = useThaoTac({ mutationFn: (f: File) => catalogApi.uploadImage(maLoai, f), lamMoi, thanhCong: 'Đã tải ảnh lên.', onSuccess: () => { if (tep.current) tep.current.value = ''; } });
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deleteImage(maLoai, ma), lamMoi, thanhCong: 'Đã xoá ảnh.' });
  if (q.isLoading) return <Loading />;
  if (q.error) return <ErrorBox message={q.error.message} />;
  if (!q.data) return (<><PageHeader tieuDe="Không tìm thấy loại phòng" /><p><Link to="/nhan-vien/quan-tri/loai-phong">← Loại phòng</Link></p></>);
  const d = q.data;
  const ds = d.HinhAnhs ?? [];
  return (
    <>
      <p className="small"><Link to="/nhan-vien/quan-tri/loai-phong">← Loại phòng</Link></p>
      <PageHeader tieuDe={`Bộ ảnh — ${d.TenLoai}`}>
        <a href={`/hang-phong/${d.MaLoai}`} target="_blank" rel="noopener" className="btn btn-outline btn-sm">Xem trang khách ↗</a>
      </PageHeader>
      <form className="the-trang qt-tai-anh" aria-label="Thêm ảnh" onSubmit={(e) => {
        e.preventDefault();
        const f = tep.current?.files?.[0];
        if (!f) { toast.loi('Vui lòng chọn ảnh cần tải lên.'); return; }
        tai.mutate(f);
      }}>
        <label className="field"><span>Thêm ảnh</span><input ref={tep} type="file" accept="image/jpeg,image/png,image/webp" required />
          <small className="muted">JPG, PNG hoặc WebP, tối đa 5 MB. Ảnh đầu tiên trong bộ ảnh là ảnh đại diện của hạng phòng.</small></label>
        <button type="submit" className="btn btn-gold" disabled={tai.isPending}>{tai.isPending ? 'Đang tải…' : 'Tải lên'}</button>
      </form>
      {!ds.length ? <EmptyState>Hạng phòng chưa có ảnh. Trang khách đang dùng {d.AnhDaiDien ? 'ảnh đại diện hiện có' : 'ảnh mặc định'}.</EmptyState> : (
        <div className="qt-luoi-anh">
          {ds.map((h, i) => (
            <figure key={h.MaHinhAnh} className="the-trang qt-anh" data-anh={h.MaHinhAnh}>
              <img src={anh(h.DuongDan)} alt={`Ảnh ${i + 1}`} />
              <figcaption>
                {i === 0 ? <span className="badge badge-gold">Ảnh đại diện</span> : <span className="muted small">#{i + 1}</span>}
                <button type="button" className="btn btn-outline-danger btn-sm" title="Xoá ảnh" disabled={xoa.isPending}
                  onClick={async () => { if (await xacNhan({ tieuDe: 'Xoá ảnh này?', nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(h.MaHinhAnh!); }}>Xoá</button>
              </figcaption>
            </figure>
          ))}
        </div>
      )}
    </>
  );
}

// ===== Phòng (Admin/Rooms): thêm / sửa / xoá, gán tiện nghi (một phòng hoặc cả hạng) =====
export function RoomsAdminPage() {
  const xacNhan = useXacNhan();
  const phong = useQuery({ queryKey: ['rooms'], queryFn: roomApi.list });
  const loai = useQuery({ queryKey: ['room-types'], queryFn: catalogApi.roomTypes });
  const ti = useQuery({ queryKey: ['amenities'], queryFn: catalogApi.amenities });
  const [form, setForm] = useState<Room | 'moi' | null>(null);
  const [gan, setGan] = useState<Room | null>(null);
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deleteRoom(ma), lamMoi: [['rooms']], thanhCong: 'Xóa phòng thành công!' });
  const tiCua = (ma?: string | null) => (ti.data ?? []).filter((a) => a.MaPhongs?.includes(ma ?? ''));
  return (
    <>
      <PageHeader tieuDe="Danh sách phòng" phu="Quản lý tài sản">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setForm('moi')}>+ Thêm phòng mới</button>
      </PageHeader>
      <ErrorBox message={phong.error?.message ?? ti.error?.message} />
      {phong.isLoading || ti.isLoading ? <Loading /> : (
        <section className="the-trang the-bang">
          <div className="bang-cuon">
            <table className="bang" aria-label="Danh sách phòng">
              <thead><tr><th>Mã phòng</th><th>Số phòng</th><th>Loại phòng</th><th>Trạng thái</th><th>Tiện nghi</th><th>Thao tác</th></tr></thead>
              <tbody>
                {!phong.data?.length && <tr><td colSpan={6} className="canh-giua muted">Chưa có phòng nào trong hệ thống.</td></tr>}
                {(phong.data ?? []).map((r) => {
                  const co = tiCua(r.MaPhong);
                  const tt = r.TrangThai ?? 'Available';
                  return (
                    <tr key={r.MaPhong} data-phong={r.MaPhong}>
                      <td><strong>{r.MaPhong}</strong></td>
                      <td>{r.SoPhong}</td>
                      <td>{r.LoaiPhong?.TenLoai ?? 'N/A'}</td>
                      <td><span className={`badge-tt ${CSS_TT_PHONG[tt]}`}>{TEN_TT_PHONG[tt].toUpperCase()}</span></td>
                      <td><div className="qt-ti">{co.length ? co.map((a) => <span key={a.MaTienIch} title={a.Icon ?? ''}>{a.TenTienIch}</span>) : <span className="muted small">—</span>}</div></td>
                      <td className="nowrap">
                        <button type="button" className="btn btn-outline btn-sm" title="Tiện nghi" onClick={() => setGan(r)}>Tiện nghi</button>{' '}
                        <button type="button" className="btn btn-outline btn-sm" title="Sửa" onClick={() => setForm(r)}>Sửa</button>{' '}
                        <button type="button" className="btn btn-outline-danger btn-sm" title="Xoá" disabled={xoa.isPending}
                          onClick={async () => { if (await xacNhan({ tieuDe: 'Xoá phòng?', noiDung: `Xóa phòng ${r.SoPhong}?`, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(r.MaPhong!); }}>Xoá</button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      )}
      {form && <HopPhong r={form === 'moi' ? null : form} loai={loai.data ?? []} onDong={() => setForm(null)} />}
      {gan && <HopTienNghi r={gan} ds={ti.data ?? []} co={tiCua(gan.MaPhong).map((a) => a.MaTienIch!)} onDong={() => setGan(null)} />}
    </>
  );
}

function HopPhong({ r, loai, onDong }: { r: Room | null; loai: RoomTypeAdmin[]; onDong: () => void }) {
  const [soPhong, setSoPhong] = useState(r?.SoPhong ?? '');
  const [maLoai, setMaLoai] = useState(r?.MaLoai ?? '');
  const luu = useThaoTac({
    mutationFn: () => (r ? catalogApi.updateRoom(r.MaPhong!, soPhong.trim(), maLoai) : catalogApi.createRoom(soPhong.trim(), maLoai)), lamMoi: [['rooms']],
    thanhCong: r ? 'Cập nhật phòng thành công!' : 'Thêm phòng thành công!', onSuccess: onDong
  });
  return (
    <Modal open title={r ? 'Cập nhật thông tin phòng' : 'Thêm phòng mới'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Phòng">
        <label className="field"><span>Số phòng</span><input required placeholder="VD: 101" value={soPhong} onChange={(e) => setSoPhong(e.target.value)} /></label>
        <label className="field"><span>Loại phòng</span>
          <select required value={maLoai} onChange={(e) => setMaLoai(e.target.value)}><option value="">-- Chọn loại --</option>{loai.map((x) => <option key={x.MaLoai} value={x.MaLoai!}>{x.TenLoai}</option>)}</select>
        </label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className={`btn ${r ? 'btn-primary' : 'btn-gold'}`} disabled={luu.isPending}>{r ? 'Cập nhật' : 'Thêm phòng'}</button></div>
      </form>
    </Modal>
  );
}

function HopTienNghi({ r, ds, co, onDong }: { r: Room; ds: Amenity[]; co: string[]; onDong: () => void }) {
  const [chon, setChon] = useState<Set<string>>(() => new Set(co));
  const [caHang, setCaHang] = useState(false);
  const luu = useThaoTac({
    mutationFn: () => catalogApi.setRoomAmenities(r.MaPhong!, [...chon], caHang), lamMoi: [['amenities'], ['rooms']],
    thanhCong: caHang ? 'Đã cập nhật tiện nghi cho mọi phòng cùng hạng.' : `Đã cập nhật tiện nghi phòng ${r.MaPhong}.`, onSuccess: onDong
  });
  const doi = (ma: string) => setChon((s) => { const n = new Set(s); if (n.has(ma)) n.delete(ma); else n.add(ma); return n; });
  return (
    <Modal open title={`Tiện nghi phòng ${r.SoPhong}`} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Tiện nghi phòng">
        {!ds.length && <p className="muted small">Chưa có tiện nghi nào. <Link to="/nhan-vien/quan-tri/tien-nghi">Thêm tiện nghi</Link></p>}
        <div className="qt-chon-ti">
          {ds.map((a) => <label key={a.MaTienIch} className="o-chon"><input type="checkbox" checked={chon.has(a.MaTienIch!)} onChange={() => doi(a.MaTienIch!)} /> {a.TenTienIch}</label>)}
        </div>
        <label className="o-chon qt-o-chon"><input type="checkbox" checked={caHang} onChange={(e) => setCaHang(e.target.checked)} /> Áp dụng cho mọi phòng cùng hạng <span className="muted">({r.LoaiPhong?.TenLoai})</span></label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu tiện nghi</button></div>
      </form>
    </Modal>
  );
}

// ===== Tiện nghi (Admin/Amenities) =====
export function AmenitiesPage() {
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['amenities'], queryFn: catalogApi.amenities });
  const phong = useQuery({ queryKey: ['rooms'], queryFn: roomApi.list });
  const [form, setForm] = useState<Amenity | 'moi' | null>(null);
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deleteAmenity(ma), lamMoi: [['amenities']], thanhCong: 'Đã xoá tiện nghi.' });
  const soPhong = new Map((phong.data ?? []).map((r) => [r.MaPhong, r.SoPhong]));
  return (
    <>
      <PageHeader tieuDe="Tiện nghi phòng" phu={<>Tiện nghi hiển thị ở trang chi tiết hạng phòng của khách. Gán cho từng phòng (hoặc cả hạng) ở trang <Link to="/nhan-vien/quan-tri/phong">Phòng</Link>.</>}>
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setForm('moi')}>+ Thêm tiện nghi</button>
      </PageHeader>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : !q.data?.length ? <EmptyState>Chưa có tiện nghi nào.</EmptyState> : (
        <div className="qt-luoi-the">
          {q.data.map((a) => {
            const ds = a.MaPhongs ?? [];
            return (
              <article key={a.MaTienIch} className="the-trang qt-ti-the" data-tien-ich={a.MaTienIch}>
                <div className="the-dau"><strong>{a.TenTienIch}</strong><small className="muted">{a.MaTienIch} · {a.Icon}</small></div>
                <p className="small muted">{ds.length ? `${ds.length} phòng: ${ds.map((m) => soPhong.get(m) ?? m).join(', ')}` : 'Chưa gán phòng nào'}</p>
                <div className="the-dau-nut">
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => setForm(a)}>Sửa</button>
                  <button type="button" className="btn btn-outline-danger btn-sm" disabled={xoa.isPending}
                    onClick={async () => { if (await xacNhan({ tieuDe: 'Xoá tiện nghi?', noiDung: `Xoá tiện nghi ${a.TenTienIch}? Tiện nghi sẽ bị gỡ khỏi ${ds.length} phòng.`, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(a.MaTienIch!); }}>Xoá</button>
                </div>
              </article>
            );
          })}
        </div>
      )}
      {form && <HopTienIch a={form === 'moi' ? null : form} onDong={() => setForm(null)} />}
    </>
  );
}

function HopTienIch({ a, onDong }: { a: Amenity | null; onDong: () => void }) {
  const [ten, setTen] = useState(a?.TenTienIch ?? '');
  const [icon, setIcon] = useState(a?.Icon ?? '');
  const luu = useThaoTac({
    mutationFn: () => (a ? catalogApi.updateAmenity(a.MaTienIch!, ten.trim(), icon.trim()) : catalogApi.createAmenity(ten.trim(), icon.trim())), lamMoi: [['amenities']],
    thanhCong: a ? 'Đã cập nhật tiện nghi.' : `Đã thêm tiện nghi "${ten.trim()}".`, onSuccess: onDong
  });
  return (
    <Modal open title={a ? `Sửa tiện nghi ${a.MaTienIch}` : 'Thêm tiện nghi'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Tiện nghi">
        <label className="field"><span>Tên tiện nghi</span><input required maxLength={50} placeholder="VD: Két sắt điện tử" value={ten} onChange={(e) => setTen(e.target.value)} /></label>
        <label className="field"><span>Biểu tượng (Font Awesome, dùng ở cổng MVC)</span>
          <input required maxLength={50} pattern="(fa[srb]? )?fa-[a-z0-9\-]+" title="VD: fa-lock hoặc fas fa-lock" placeholder="fa-lock" value={icon} onChange={(e) => setIcon(e.target.value)} />
          <small className="muted">Ví dụ: fa-wifi, fa-tv, fa-snowflake, fa-bath, fa-wine-glass, fa-lock, fa-mug-hot.</small></label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className="btn btn-gold" disabled={luu.isPending}>{a ? 'Lưu' : 'Thêm'}</button></div>
      </form>
    </Modal>
  );
}

// ===== Dịch vụ & giá (Admin/Services) =====
export function ServicesAdminPage() {
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['services'], queryFn: serviceApi.list });
  const [form, setForm] = useState<Service | 'moi' | null>(null);
  const xoa = useThaoTac({ mutationFn: (ma: string) => catalogApi.deleteService(ma), lamMoi: [['services']], thanhCong: 'Xóa dịch vụ thành công!' });
  const doi = useThaoTac({
    mutationFn: (s: Service) => catalogApi.setServiceStatus(s.MaDichVu!, s.TrangThai === 'Available' ? 'Unavailable' : 'Available'), lamMoi: [['services']],
    thanhCong: (_, s) => `Đã chuyển dịch vụ sang "${s.TrangThai === 'Available' ? 'Đã hết' : 'Sẵn sàng'}".`
  });
  return (
    <>
      <PageHeader tieuDe="Bảng giá dịch vụ" phu="Quản lý dịch vụ">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setForm('moi')}>+ Thêm dịch vụ</button>
      </PageHeader>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : !q.data?.length ? <EmptyState>Chưa có dịch vụ nào. Hãy thêm dịch vụ đầu tiên!</EmptyState> : (
        <div className="qt-luoi-the">
          {q.data.map((s) => {
            const san = s.TrangThai === 'Available';
            return (
              <article key={s.MaDichVu} className="the-trang qt-dv-the" data-dich-vu={s.MaDichVu}>
                <div className="qt-dv-nhan">
                  <span className="badge badge-gold">{s.MaDichVu}</span>
                  <span className={`badge ${san ? 'badge-ok' : 'badge-muted'}`}>{san ? 'Sẵn sàng' : 'Đã hết'}</span>
                  {s.LaMinibar && <span className="badge badge-muted">Minibar</span>}
                  {s.LaTraiNghiem && <span className="badge badge-gold" title="Hiển thị ở mục Trải nghiệm trang chủ">★ Trải nghiệm</span>}
                </div>
                <h3>{s.TenDichVu}</h3>
                <p className="small muted">Đơn vị: {s.DonViTinh}</p>
                {s.MoTa && <p className="small">{s.MoTa}</p>}
                <p className="qt-dv-gia"><strong className="chu-vang">{so(s.GiaDichVu)} ₫</strong><span className="muted small"> / {s.DonViTinh}</span></p>
                <div className="the-dau-nut">
                  <button type="button" className="btn btn-outline btn-sm" disabled={doi.isPending} onClick={() => doi.mutate(s)}>{san ? 'Chuyển sang Đã hết' : 'Chuyển sang Sẵn sàng'}</button>
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => setForm(s)}>Sửa</button>
                  <button type="button" className="btn btn-outline-danger btn-sm" disabled={xoa.isPending}
                    onClick={async () => { if (await xacNhan({ tieuDe: 'Xóa dịch vụ này?', noiDung: s.TenDichVu, nutXacNhan: 'Xoá', nguyHiem: true })) xoa.mutate(s.MaDichVu!); }}>Xoá</button>
                </div>
              </article>
            );
          })}
        </div>
      )}
      {form && <HopDichVu s={form === 'moi' ? null : form} onDong={() => setForm(null)} />}
    </>
  );
}

function HopDichVu({ s, onDong }: { s: Service | null; onDong: () => void }) {
  const [f, setF] = useState({ TenDichVu: s?.TenDichVu ?? '', GiaDichVu: s?.GiaDichVu != null ? String(Math.floor(s.GiaDichVu)) : '', DonViTinh: s?.DonViTinh ?? '', LaTraiNghiem: !!s?.LaTraiNghiem, LaMinibar: !!s?.LaMinibar, MoTa: s?.MoTa ?? '' });
  const dat = (p: Partial<typeof f>) => setF({ ...f, ...p });
  const r: ServiceForm = { ...f, TenDichVu: f.TenDichVu.trim(), DonViTinh: f.DonViTinh.trim(), GiaDichVu: Number(f.GiaDichVu), MoTa: f.MoTa || null };
  const luu = useThaoTac({
    mutationFn: () => (s ? catalogApi.updateService(s.MaDichVu!, r) : catalogApi.createService(r)), lamMoi: [['services']],
    thanhCong: s ? 'Cập nhật dịch vụ thành công!' : 'Thêm dịch vụ thành công!', onSuccess: onDong
  });
  return (
    <Modal open title={s ? 'Cập nhật dịch vụ' : 'Thêm dịch vụ mới'} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }} aria-label="Dịch vụ">
        <label className="field"><span>Tên dịch vụ</span><input required placeholder="VD: Massage, Giặt ủi..." value={f.TenDichVu} onChange={(e) => dat({ TenDichVu: e.target.value })} /></label>
        <div className="form-grid">
          <label className="field"><span>Giá (₫)</span><input type="number" min={0} required placeholder="VD: 150000" value={f.GiaDichVu} onChange={(e) => dat({ GiaDichVu: e.target.value })} /></label>
          <label className="field"><span>Đơn vị tính</span><input required placeholder="VD: lần, kg, giờ..." value={f.DonViTinh} onChange={(e) => dat({ DonViTinh: e.target.value })} /></label>
        </div>
        <label className="o-chon"><input type="checkbox" checked={f.LaTraiNghiem} onChange={(e) => dat({ LaTraiNghiem: e.target.checked })} /> Giới thiệu ở mục "Trải nghiệm" trên trang chủ</label>
        <label className="o-chon"><input type="checkbox" checked={f.LaMinibar} onChange={(e) => dat({ LaMinibar: e.target.checked })} /> Đồ minibar (buồng phòng ghi được vào bảng kê)</label>
        <label className="field"><span>Mô tả cho khách</span><textarea rows={2} maxLength={500} placeholder="Một câu giới thiệu ngắn, hiển thị trên trang chủ" value={f.MoTa} onChange={(e) => dat({ MoTa: e.target.value })} /></label>
        <div className="modal-chan"><button type="button" className="btn btn-outline" onClick={onDong}>Hủy</button><button type="submit" className={`btn ${s ? 'btn-primary' : 'btn-gold'}`} disabled={luu.isPending}>{s ? 'Cập nhật' : 'Thêm dịch vụ'}</button></div>
      </form>
    </Modal>
  );
}
