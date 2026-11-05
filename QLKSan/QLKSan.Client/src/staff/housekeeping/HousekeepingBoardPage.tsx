import { useMemo, useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { hkApi, TRANG_THAI_VIEC, type HousekeepingRoom, type HousekeepingTask, type HousekeepingTaskStatus, type RoomStatus } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { useAuth } from '../../auth/AuthContext';
import { coQuyen, VAI_TRO } from '../../auth/roles';
import { ErrorBox, Loading } from '../../components/common';
import { Modal } from '../../components/ui/Dialog';
import { PageHeader } from '../../components/ui/Page';
import { ngay, tenHangPhong } from '../../utils/format';
import './hk.css';

const PHONG: Record<RoomStatus, [string, string]> = {
  Available: ['p-sach', 'Sạch'], Cleaning: ['p-ban', 'Chưa dọn'], Occupied: ['p-khach', 'Có khách'], Maintenance: ['p-bao-tri', 'Bảo trì'], Booked: ['p-sach', 'Đã đặt']
};
const UU_TIEN = (u?: number): [string, string] => (u === 1 ? ['uu-tien-1', 'Gấp'] : u === 2 ? ['uu-tien-2', 'Cao'] : u === 4 ? ['uu-tien-4', 'Sự cố'] : ['uu-tien-3', 'Thường']);
const VIEC_CSS: Record<HousekeepingTaskStatus, string> = {
  Unassigned: 'badge-warn', Assigned: 'badge-info', InProgress: 'badge-gold', Issue: 'badge-danger', AwaitingInspection: 'badge-info', Completed: 'badge-ok'
};
const coTheGiao = (t: HousekeepingTask) => t.TrangThai === 'Unassigned' || t.TrangThai === 'Assigned';
const gioNgay = (v?: string | null) => (v ? `${ngay(v).slice(0, 5)} ${v.slice(11, 16)}` : '');

// Buồng phòng (lễ tân / quản trị): kiểm tra phòng, sơ đồ theo tầng, phân công — như Housekeeping/Tasks của MVC
export function HousekeepingBoardPage() {
  const { session } = useAuth();
  const q = useQuery({ queryKey: ['hk', 'summary'], queryFn: hkApi.summary });
  const lamMoi = [['hk']];
  const [chon, setChon] = useState<string[]>([]);
  const [tang, setTang] = useState('');
  const [nvNhom, setNvNhom] = useState('');
  const [kiemTra, setKiemTra] = useState<HousekeepingTask | null>(null);

  const daDon = useThaoTac({ mutationFn: hkApi.markClean, lamMoi, thanhCong: 'Phòng đã được chuyển sang trạng thái Trống (Sẵn sàng)!', tienToLoi: 'Không thể cập nhật trạng thái phòng. ' });
  const giao = useThaoTac({ mutationFn: (v: { id: string; nv: string }) => hkApi.assign(v.id, v.nv), lamMoi, thanhCong: 'Đã giao việc dọn phòng.', tienToLoi: 'Không thể giao việc. ' });
  const giaoNhom = useThaoTac({
    mutationFn: (v: { ids: string[]; nv: string }) => hkApi.assignBulk(v.ids, v.nv), lamMoi,
    thanhCong: (n) => `Đã giao ${n} việc.`, tienToLoi: 'Không thể giao việc. ', onSuccess: () => { setChon([]); setTang(''); }
  });
  const taoViec = useThaoTac({
    mutationFn: hkApi.generateDaily, lamMoi, tienToLoi: 'Không tạo được việc. ',
    thanhCong: (r) => ((r.DonHangNgay ?? 0) + (r.ChinhTrangToi ?? 0) === 0 ? 'Việc trong ngày đã đủ, không cần tạo thêm.' : `Đã tạo ${r.DonHangNgay} việc dọn hằng ngày, ${r.ChinhTrangToi} việc chỉnh trang buổi tối.`)
  });

  const d = q.data;
  const tasks = useMemo(() => d?.CongViec ?? [], [d]);
  const tangs = useMemo(() => {
    const m = new Map<number, HousekeepingRoom[]>();
    for (const r of d?.Phong ?? []) m.set(r.Tang ?? 0, [...(m.get(r.Tang ?? 0) ?? []), r]);
    return [...m.entries()];
  }, [d]);
  if (q.isLoading) return <Loading />;
  if (!d) return <ErrorBox message={q.error?.message ?? 'Không tải được dữ liệu buồng phòng.'} />;

  const dem = (s: HousekeepingTaskStatus) => tasks.filter((t) => t.TrangThai === s).length;
  const choKiemTra = tasks.filter((t) => t.TrangThai === 'AwaitingInspection');
  const laNguoiKiemTra = coQuyen(session?.vaiTro, VAI_TRO.kiemTraPhong);
  const tangCoViec = [...new Set(tasks.filter(coTheGiao).map((t) => t.Tang ?? 0))].sort((a, b) => a - b);
  const nhanVien = d.NhanVienBuongPhong ?? [];

  const chonTang = (v: string) => {
    setTang(v);
    setChon(v === '' ? [] : tasks.filter((t) => coTheGiao(t) && String(t.Tang) === v).map((t) => t.MaPhanCong!));
  };
  const guiNhom = (e: FormEvent) => {
    e.preventDefault();
    if (chon.length && nvNhom) giaoNhom.mutate({ ids: chon, nv: nvNhom });
  };

  return (
    <>
      <PageHeader tieuDe="Công việc dọn phòng" phu={`Việc được tạo khi khách trả phòng, đổi phòng, gửi yêu cầu, sau sửa chữa, và mỗi ngày cho phòng có khách (dọn hằng ngày, chỉnh trang buổi tối); xếp theo ưu tiên khách sắp đến. Hiện có ${d.SoPhongTrong ?? 0} phòng sạch sẵn sàng đón khách.`} />
      {(d.KhachDenPhongChuaSan ?? 0) > 0 && (
        <div className="alert alert-warn" role="status"><strong>{d.KhachDenPhongChuaSan} phòng</strong> có khách đến hôm nay nhưng chưa sạch — ưu tiên giao dọn trước.</div>
      )}

      {choKiemTra.length > 0 && (
        <section className="the-trang cho-kiem-tra" aria-labelledby="ckt">
          <div className="the-dau"><h2 id="ckt">Chờ kiểm tra ({choKiemTra.length})</h2><small className="muted">Phòng đã dọn xong; kiểm tra theo danh mục trước khi phòng được bán</small></div>
          <div className="ckt-ds">
            {choKiemTra.map((t) => {
              const [css, nhan] = UU_TIEN(t.UuTien);
              return (
                <div key={t.MaPhanCong} className="ckt-muc">
                  <div><strong>Phòng {t.SoPhong}</strong> <span className={`uu-tien ${css}`}>{nhan}</span></div>
                  <small className="muted">{t.TenNhanVien} · xong lúc {t.ThoiGianHoanThanh?.slice(11, 16)}{(t.SoLanKhongDat ?? 0) > 0 ? ` · đã trả lại ${t.SoLanKhongDat} lần` : ''}</small>
                  {laNguoiKiemTra && <button type="button" className="btn btn-gold btn-sm" onClick={() => setKiemTra(t)}>Kiểm tra</button>}
                </div>
              );
            })}
          </div>
        </section>
      )}

      <div className="o-so">
        {([['Chưa giao', dem('Unassigned')], ['Đã giao', dem('Assigned')], ['Đang dọn', dem('InProgress')], ['Báo sự cố', dem('Issue')]] as const).map(([n, v]) => (
          <div key={n} className="o-so-muc"><span>{n}</span><strong>{v}</strong> <small className="muted">việc</small></div>
        ))}
      </div>

      <section className="the-trang" aria-labelledby="sdbp">
        <div className="the-dau">
          <h2 id="sdbp">Sơ đồ buồng phòng</h2>
          <div className="the-dau-nut">
            <Link to="/nhan-vien/buong-phong/minibar" className="btn btn-outline btn-sm">Ghi minibar</Link>
            <Link to="/nhan-vien/ky-thuat" className="btn btn-outline btn-sm">Kỹ thuật</Link>
            <button type="button" className="btn btn-outline btn-sm" disabled={taoViec.isPending} onClick={() => taoViec.mutate()}
              title="Dọn hằng ngày phòng có khách + chỉnh trang buổi tối (hệ thống cũng tự tạo định kỳ)">Tạo việc trong ngày</button>
          </div>
          <div className="chu-thich" aria-label="Chú thích màu">
            <span className="p-sach">Sạch</span><span className="p-ban">Chưa dọn</span><span className="p-khach">Có khách</span><span className="p-bao-tri">Bảo trì</span>
          </div>
        </div>
        {tangs.map(([so, phongs]) => (
          <div key={so} className="tang">
            <div className="tang-ten">Tầng {so}</div>
            <div className="tang-phong">
              {phongs.map((r) => {
                const [css, nhan] = PHONG[r.TrangThai ?? 'Available'];
                const v = r.ViecDangMo;
                return (
                  <div key={r.MaPhong} className={`o-phong ${css}${r.KhachDen && r.TrangThai !== 'Available' ? ' o-gap' : ''}`} data-phong={r.SoPhong}>
                    <div className="o-phong-dau"><strong>{r.SoPhong}</strong><span>{nhan}</span></div>
                    <small className="o-phong-hang">{tenHangPhong(r.TenLoaiPhong)}</small>
                    {r.KhachDen && <small className="o-the">{r.KhachDenVIP ? '★ ' : ''}Đến {r.GioDen ?? 'hôm nay'}</small>}
                    {r.KhachDiHomNay && <small className="o-the">Đi hôm nay</small>}
                    {r.SuCo && <small className="o-the chu-loi" title={r.SuCo}>{r.SuCo}</small>}
                    {v && <small className="o-viec"><span className={`badge ${VIEC_CSS[v.TrangThai!]}`}>{TRANG_THAI_VIEC[v.TrangThai!]}</span> {v.TenNhanVien ?? ''}</small>}
                    {r.TrangThai === 'Cleaning' && (
                      <button type="button" className="link-btn o-da-don" disabled={daDon.isPending} onClick={() => daDon.mutate(r.MaPhong!)} title="Lễ tân xác nhận phòng đã dọn">Xác nhận đã dọn</button>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </section>

      <section className="the-trang" aria-labelledby="pcdp">
        <div className="the-dau">
          <h2 id="pcdp">Phân công dọn phòng ({tasks.length})</h2>
          <form className="giao-nhom" onSubmit={guiNhom} aria-label="Giao nhiều việc">
            {tangCoViec.length > 0 && (
              <select value={tang} onChange={(e) => chonTang(e.target.value)} aria-label="Chọn cả tầng">
                <option value="">Chọn cả tầng…</option>
                {tangCoViec.map((t) => <option key={t} value={t}>Tầng {t}</option>)}
              </select>
            )}
            <select value={nvNhom} onChange={(e) => setNvNhom(e.target.value)} required aria-label="Nhân viên nhận việc">
              <option value="">-- Nhân viên --</option>
              {nhanVien.map((nv) => <option key={nv.MaNhanVien} value={nv.MaNhanVien!}>{nv.TenNhanVien}</option>)}
            </select>
            <button type="submit" className="btn btn-gold btn-sm" disabled={!chon.length || giaoNhom.isPending}>{chon.length ? `Giao ${chon.length} việc đã chọn` : 'Giao việc đã chọn'}</button>
          </form>
        </div>
        <div className="bang-cuon">
          <table className="bang bang-phan-cong">
            <thead><tr><th aria-label="Chọn"></th><th>Phòng</th><th>Công việc</th><th>Ưu tiên</th><th>Trạng thái</th><th>Nhân viên</th><th>Giao việc</th></tr></thead>
            <tbody>
              {tasks.length === 0 && <tr><td colSpan={7} className="bang-trong">Không có công việc dọn phòng nào đang chờ.</td></tr>}
              {tasks.map((t) => <DongPhanCong key={t.MaPhanCong} t={t} nhanVien={nhanVien} chon={chon.includes(t.MaPhanCong!)}
                onChon={(c) => setChon((x) => (c ? [...x, t.MaPhanCong!] : x.filter((y) => y !== t.MaPhanCong)))}
                onGiao={(nv) => giao.mutate({ id: t.MaPhanCong!, nv })} dangGiao={giao.isPending} />)}
            </tbody>
          </table>
        </div>
      </section>

      {kiemTra && <HopKiemTra t={kiemTra} onDong={() => setKiemTra(null)} />}
    </>
  );
}

function DongPhanCong({ t, nhanVien, chon, onChon, onGiao, dangGiao }: {
  t: HousekeepingTask; nhanVien: { MaNhanVien?: string | null; TenNhanVien?: string | null }[];
  chon: boolean; onChon: (c: boolean) => void; onGiao: (nv: string) => void; dangGiao: boolean;
}) {
  const [nv, setNv] = useState(t.MaNhanVien ?? '');
  const [css, nhan] = UU_TIEN(t.UuTien);
  const giaoDuoc = coTheGiao(t);
  return (
    <tr>
      <td>{giaoDuoc && <input type="checkbox" checked={chon} onChange={(e) => onChon(e.target.checked)} aria-label={`Chọn phòng ${t.SoPhong}`} data-tang={t.Tang} />}</td>
      <td className="nowrap"><strong>Phòng {t.SoPhong}</strong><br /><small className="muted">Tầng {t.Tang} · {t.TenLoaiPhong}</small></td>
      <td>{t.LoaiCongViec}<br /><small className="muted">Tạo lúc {gioNgay(t.ThoiGianTao)}</small>
        {t.GioDonMongMuon && <><br /><small>Khách muốn dọn: {t.GioDonMongMuon}</small></>}
        {t.GhiChu && <><br /><small className={t.TrangThai === 'Issue' ? 'chu-loi' : ''}>{t.GhiChu}</small></>}
      </td>
      <td><span className={`uu-tien ${css}`}>{nhan}</span>{t.LyDoUuTien && <><br /><small className="muted">{t.LyDoUuTien}</small></>}</td>
      <td><span className={`badge ${VIEC_CSS[t.TrangThai!]}`}>{TRANG_THAI_VIEC[t.TrangThai!]}</span></td>
      <td>{t.TenNhanVien ?? '—'}</td>
      <td>
        {giaoDuoc ? (
          <form className="giao-mot" onSubmit={(e) => { e.preventDefault(); if (nv) onGiao(nv); }}>
            <select value={nv} onChange={(e) => setNv(e.target.value)} required aria-label={`Nhân viên dọn phòng ${t.SoPhong}`}>
              <option value="">-- Chọn nhân viên --</option>
              {nhanVien.map((x) => <option key={x.MaNhanVien} value={x.MaNhanVien!}>{x.TenNhanVien}</option>)}
            </select>
            <button type="submit" className="btn btn-gold btn-sm" disabled={dangGiao}>{t.MaNhanVien == null ? 'Giao' : 'Giao lại'}</button>
          </form>
        ) : <small className="muted">{t.TrangThai === 'InProgress' ? 'Nhân viên đang dọn' : 'Chờ xử lý sự cố (đổi trạng thái phòng khi sửa xong)'}</small>}
      </td>
    </tr>
  );
}

// Kiểm tra phòng theo danh mục của hạng phòng: bỏ tích mục nào → chưa đạt, trả lại nhân viên dọn lại
function HopKiemTra({ t, onDong }: { t: HousekeepingTask; onDong: () => void }) {
  const danhMuc = t.DanhMucKiemTra ?? [];
  const [dat, setDat] = useState<string[]>(danhMuc);
  const [ghiChu, setGhiChu] = useState('');
  const chuaDat = danhMuc.filter((m) => !dat.includes(m));
  const luu = useThaoTac({
    mutationFn: () => hkApi.inspect(t.MaPhanCong!, { Dat: chuaDat.length === 0, MucChuaDat: chuaDat, GhiChu: ghiChu || null }),
    lamMoi: [['hk']], tienToLoi: 'Không lưu được kết quả kiểm tra. ', onSuccess: onDong,
    thanhCong: () => (chuaDat.length === 0 ? 'Kiểm tra đạt — phòng sẵn sàng đón khách.' : `Kiểm tra chưa đạt (${chuaDat.length} mục) — đã trả lại để dọn lại.`)
  });
  return (
    <Modal open title={`Kiểm tra phòng ${t.SoPhong}`} onClose={onDong}>
      <form className="xac-nhan" onSubmit={(e) => { e.preventDefault(); luu.mutate(); }}>
        <p className="small muted">Tích các mục đạt. Bỏ tích mục nào thì phòng được trả lại cho nhân viên dọn lại.</p>
        <fieldset className="danh-muc-kt">
          <legend className="sr-only">Danh mục kiểm tra</legend>
          {danhMuc.map((m) => (
            <label key={m} className="check"><input type="checkbox" checked={dat.includes(m)} onChange={(e) => setDat((x) => (e.target.checked ? [...x, m] : x.filter((y) => y !== m)))} /> {m}</label>
          ))}
        </fieldset>
        <label className="field"><span>Ghi chú</span><input value={ghiChu} maxLength={300} onChange={(e) => setGhiChu(e.target.value)} placeholder="VD: thay khăn tắm bị ố" /></label>
        <div className="modal-chan">
          <button type="button" className="btn btn-outline" onClick={onDong}>Đóng</button>
          <button type="submit" className={`btn ${chuaDat.length ? 'btn-danger' : 'btn-gold'}`} disabled={luu.isPending}>
            {chuaDat.length ? `Chưa đạt — trả lại dọn lại (${chuaDat.length} mục)` : 'Đạt — mở bán phòng'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
