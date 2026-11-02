import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { hkApi, maintenanceApi, type GuestRequestStatus, type MaintenanceTicket } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { useAuth } from '../../auth/AuthContext';
import { coQuyen, VAI_TRO } from '../../auth/roles';
import { ErrorBox, Loading } from '../../components/common';
import { Modal } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { ngay } from '../../utils/format';
import './hk.css';

const LOC = [['mo', 'Đang mở'], ['xong', 'Đã xong (7 ngày)'], ['tatca', 'Tất cả (30 ngày)']] as const;
const TRANG_THAI: Record<GuestRequestStatus, [string, string]> = {
  New: ['badge-warn', 'Mới'], InProgress: ['badge-info', 'Đang sửa'], Done: ['badge-ok', 'Đã xong'], Cancelled: ['badge-muted', 'Đã huỷ']
};
const gioNgay = (v?: string | null) => (v ? `${v.slice(11, 16)} ${ngay(v).slice(0, 5)}` : '');

// Phiếu kỹ thuật (như Housekeeping/Maintenance của MVC): cả đội buồng phòng xem + mở phiếu; lễ tân / quản trị cập nhật
export function MaintenancePage() {
  const { session } = useAuth();
  const [params] = useSearchParams();
  const loc = LOC.some(([k]) => k === params.get('loc')) ? params.get('loc')! : 'mo';
  const laQuay = coQuyen(session?.vaiTro, VAI_TRO.leTan);
  const ds = useQuery({ queryKey: ['maintenance', loc], queryFn: () => maintenanceApi.list(loc === 'tatca' ? null : loc) });
  const phong = useQuery({ queryKey: ['hk', 'summary'], queryFn: hkApi.summary });
  const [moPhieu, setMoPhieu] = useState(false);
  const [capNhat, setCapNhat] = useState<{ p: MaintenanceTicket; trangThai: GuestRequestStatus; tieuDe: string } | null>(null);
  const dangSua = useThaoTac({
    mutationFn: (p: MaintenanceTicket) => maintenanceApi.update(p.MaPhieu!, 'InProgress', null), lamMoi: [['maintenance'], ['hk']],
    thanhCong: (_, p) => `Đã cập nhật phiếu kỹ thuật #${p.MaPhieu}.`, tienToLoi: 'Không cập nhật được phiếu. '
  });

  return (
    <>
      <PageHeader tieuDe="Phiếu kỹ thuật" phu="Sự cố từ buồng phòng báo, yêu cầu khách loại Kỹ thuật hoặc lễ tân ghi. Phiếu khoá phòng giữ phòng ở Bảo trì; sửa xong phiếu cuối cùng thì phòng chuyển sang Đang dọn và tự tạo việc dọn.">
        <button type="button" className="btn btn-gold btn-sm" onClick={() => setMoPhieu(true)}>+ Mở phiếu</button>
      </PageHeader>
      <nav className="loc-chip" aria-label="Lọc phiếu">
        {LOC.map(([k, n]) => <Link key={k} to={`?loc=${k}`} className={loc === k ? 'active' : ''} aria-current={loc === k ? 'page' : undefined}>{n}</Link>)}
      </nav>
      <ErrorBox message={ds.error?.message} />
      {ds.isLoading ? <Loading /> : !ds.data?.length ? (
        <EmptyState>{loc === 'mo' ? 'Không có sự cố nào đang chờ sửa.' : 'Chưa có phiếu nào.'}</EmptyState>
      ) : (
        <div className="phieu-ds">
          {ds.data.map((p) => {
            const [css, nhan] = TRANG_THAI[p.TrangThai ?? 'New'];
            const mo = p.TrangThai === 'New' || p.TrangThai === 'InProgress';
            return (
              <article key={p.MaPhieu} className={`phieu${p.MucDo === 'Khẩn' && mo ? ' phieu-khan' : ''}${mo ? '' : ' phieu-xong'}`} aria-label={`Phiếu #${p.MaPhieu}`}>
                <div className="phieu-chinh">
                  <div className="phieu-dau">
                    <strong>Phòng {p.SoPhong}</strong>
                    <span className={`badge ${css}`}>{nhan}</span>
                    {p.MucDo === 'Khẩn' && <span className="badge badge-danger">Khẩn</span>}
                    {p.KhoaPhong && <span className="badge badge-muted">Khoá phòng</span>}
                  </div>
                  <p className="phieu-mo-ta">{p.MoTa}</p>
                  <p className="phieu-meta small muted">
                    <span>#{p.MaPhieu} · {gioNgay(p.ThoiGianBao)} · {p.TenNguoiBao}</span>
                    {p.MaPhanCong && <span>Từ việc {p.MaPhanCong}</span>}
                    {p.MaYeuCau != null && <span>Yêu cầu khách #{p.MaYeuCau}</span>}
                    {p.ThoiGianXong && <span>Xong {gioNgay(p.ThoiGianXong)} · {p.TenNguoiXuLy}</span>}
                    {p.KetQua && <span className="phieu-ket-qua">{p.KetQua}</span>}
                  </p>
                </div>
                {mo && laQuay && (
                  <div className="phieu-nut">
                    {p.TrangThai === 'New' && <button type="button" className="btn btn-outline btn-sm" disabled={dangSua.isPending} onClick={() => dangSua.mutate(p)}>Đang sửa</button>}
                    <button type="button" className="btn btn-gold btn-sm" onClick={() => setCapNhat({ p, trangThai: 'Done', tieuDe: `Sửa xong phiếu #${p.MaPhieu}` })}>Sửa xong</button>
                    <button type="button" className="btn btn-outline-danger btn-sm" aria-label={`Huỷ phiếu #${p.MaPhieu}`} onClick={() => setCapNhat({ p, trangThai: 'Cancelled', tieuDe: `Huỷ phiếu #${p.MaPhieu}` })}>Huỷ</button>
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}
      {moPhieu && <HopMoPhieu phongs={phong.data?.Phong ?? []} onDong={() => setMoPhieu(false)} />}
      {capNhat && <HopCapNhat {...capNhat} onDong={() => setCapNhat(null)} />}
    </>
  );
}

function HopMoPhieu({ phongs, onDong }: { phongs: { MaPhong?: string | null; SoPhong?: string | null; TrangThai?: string }[]; onDong: () => void }) {
  const [f, setF] = useState({ MaPhong: '', MucDo: 'Thường', MoTa: '', KhoaPhong: false });
  const tao = useThaoTac({
    mutationFn: () => maintenanceApi.create({ ...f, MoTa: f.MoTa.trim() }), lamMoi: [['maintenance'], ['hk']], tienToLoi: 'Không mở được phiếu. ',
    thanhCong: (p) => `Đã mở phiếu kỹ thuật #${p.MaPhieu} cho phòng ${p.SoPhong}` + (p.KhoaPhong ? ' (phòng chuyển Bảo trì).' : '.'), onSuccess: onDong
  });
  const gui = (e: FormEvent) => { e.preventDefault(); tao.mutate(); };
  return (
    <Modal open title="Mở phiếu kỹ thuật" onClose={onDong}>
      <form className="xac-nhan" onSubmit={gui}>
        <div className="form-grid">
          <label className="field"><span>Phòng</span>
            <select value={f.MaPhong} onChange={(e) => setF({ ...f, MaPhong: e.target.value })} required>
              <option value="">-- Chọn phòng --</option>
              {phongs.map((r) => <option key={r.MaPhong} value={r.MaPhong!}>Phòng {r.SoPhong}{r.TrangThai === 'Occupied' ? ' (có khách)' : ''}</option>)}
            </select>
          </label>
          <label className="field"><span>Mức độ</span>
            <select value={f.MucDo} onChange={(e) => setF({ ...f, MucDo: e.target.value })}><option>Thường</option><option>Khẩn</option></select>
          </label>
          <label className="field span-2"><span>Mô tả sự cố</span>
            <textarea rows={3} maxLength={500} required value={f.MoTa} onChange={(e) => setF({ ...f, MoTa: e.target.value })} placeholder="VD: Điều hoà chảy nước, bóng đèn đầu giường hỏng" />
          </label>
          <label className="check span-2"><input type="checkbox" checked={f.KhoaPhong} onChange={(e) => setF({ ...f, KhoaPhong: e.target.checked })} /> Khoá phòng (chuyển Bảo trì tới khi sửa xong — không áp dụng khi phòng có khách)</label>
        </div>
        <div className="modal-chan">
          <button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button>
          <button type="submit" className="btn btn-gold" disabled={tao.isPending}>Mở phiếu</button>
        </div>
      </form>
    </Modal>
  );
}

// Sửa xong (kết quả tuỳ chọn) / huỷ (bắt buộc lý do) — như hộp thoại doneTicketModal của MVC
function HopCapNhat({ p, trangThai, tieuDe, onDong }: { p: MaintenanceTicket; trangThai: GuestRequestStatus; tieuDe: string; onDong: () => void }) {
  const [ketQua, setKetQua] = useState('');
  const luu = useThaoTac({
    mutationFn: () => maintenanceApi.update(p.MaPhieu!, trangThai, ketQua.trim() || null), lamMoi: [['maintenance'], ['hk']],
    thanhCong: `Đã cập nhật phiếu kỹ thuật #${p.MaPhieu}.`, tienToLoi: 'Không cập nhật được phiếu. ', onSuccess: onDong
  });
  return (
    <Modal open title={tieuDe} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <label className="field"><span>Kết quả / lý do{trangThai === 'Cancelled' ? ' *' : ''}</span>
          <textarea rows={2} maxLength={500} required={trangThai === 'Cancelled'} value={ketQua} onChange={(e) => setKetQua(e.target.value)} placeholder="VD: Đã thay gioăng vòi sen" autoFocus />
        </label>
        <div className="modal-chan">
          <button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button>
          <button type="submit" className="btn btn-gold" disabled={luu.isPending}>Lưu</button>
        </div>
      </form>
    </Modal>
  );
}
