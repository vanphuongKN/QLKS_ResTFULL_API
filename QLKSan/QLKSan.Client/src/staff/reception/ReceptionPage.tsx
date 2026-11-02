import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { bookingApi, frontDeskApi, roomApi, type FrontDeskStay, type RoomStatus } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { ngay, ngayGio, tenHangPhong, tien } from '../../utils/format';
import { HopPhieu, type LoaiHop } from './StayDialogs';
import './reception.css';

const THU = ['Chủ nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
const thuCua = (iso?: string | null) => { const [y, m, d] = (iso ?? '').slice(0, 10).split('-').map(Number); return THU[new Date(y, m - 1, d).getDay()]; };
const PHONG: Record<RoomStatus, [string, string]> = {
  Available: ['p-trong', 'Trống'], Occupied: ['p-dang-o', 'Đang ở'], Booked: ['p-da-dat', 'Đã đặt'], Maintenance: ['p-bao-tri', 'Bảo trì'], Cleaning: ['p-dang-don', 'Đang dọn']
};
type Tab = 'den' | 'di' | 'dango' | 'sapden';
const KHOA_TAB = 'fd-tab';
const laTab = (v: string | null | undefined): v is Tab => v === 'den' || v === 'di' || v === 'dango' || v === 'sapden';
// Lý do chưa nhận phòng được: phòng chưa sẵn sàng → cảnh báo và gợi ý đổi phòng
const phongChuaSanSang = (k: FrontDeskStay) =>
  k.TrangThaiPhong === 'Cleaning' ? 'Phòng đang dọn' : k.TrangThaiPhong === 'Occupied' ? 'Phòng còn khách ở' : k.TrangThaiPhong === 'Maintenance' ? 'Phòng đang bảo trì' : null;

// Quầy lễ tân (Reception/Index của MVC): khách đến / đi / đang ở / sắp đến, việc cần làm, sơ đồ phòng
export function ReceptionPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const q = useQuery({ queryKey: ['front-desk', 'today'], queryFn: frontDeskApi.today, refetchInterval: 60_000 });
  const rooms = useQuery({ queryKey: ['rooms'], queryFn: roomApi.list });
  const [tab, setTab] = useState<Tab>(() => {
    const h = location.hash.slice(1);
    if (laTab(h)) return h;
    try { const s = localStorage.getItem(KHOA_TAB); if (laTab(s)) return s; } catch { /* không có localStorage */ }
    return 'den';
  });
  const [hop, setHop] = useState<{ loai: LoaiHop; k: FrontDeskStay; dangO: boolean } | null>(null);
  const [locPhong, setLocPhong] = useState<RoomStatus | 'all'>('all');
  const [tim, setTim] = useState('');
  const tabsRef = useRef<HTMLDivElement>(null);
  const duyet = useThaoTac({ mutationFn: bookingApi.approve, lamMoi: [['front-desk'], ['rooms']], thanhCong: 'Đã duyệt yêu cầu đặt phòng trực tuyến.', tienToLoi: 'Không thể duyệt yêu cầu này. ' });

  useEffect(() => { const h = location.hash.slice(1); if (laTab(h)) setTab(h); }, [location.hash]);
  const chonTab = (t: Tab, cuon = false) => {
    setTab(t);
    try { localStorage.setItem(KHOA_TAB, t); } catch { /* bỏ qua */ }
    if (cuon) tabsRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  if (q.isLoading) return <Loading />;
  const d = q.data;
  if (!d) return <ErrorBox message={q.error?.message ?? 'Không tải được dữ liệu quầy lễ tân.'} />;
  const khachDi = d.KhachDi ?? [];
  const quaHan = khachDi.filter((k) => k.QuaHanTra).length;
  const ds: Record<Tab, [string, FrontDeskStay[], string]> = {
    den: ['Khách đến', d.KhachDen ?? [], 'Không có khách đến hôm nay.'],
    di: ['Khách đi', khachDi, 'Không có khách trả phòng hôm nay.'],
    dango: ['Đang lưu trú', d.DangO ?? [], 'Chưa có khách nào đang lưu trú.'],
    sapden: ['Sắp đến', d.SapDen ?? [], 'Không có phiếu nào sắp đến.']
  };
  const viec: [number, string, string, 'tab' | 'link', string][] = [
    [d.PhieuChoDuyet ?? 0, 'Phiếu đặt chờ duyệt', 'sapden', 'tab', (d.PhieuChoDuyet ?? 0) > 0 ? 'warn' : ''],
    [d.YeuCauDangMo ?? 0, 'Yêu cầu khách đang mở', '/nhan-vien/yeu-cau', 'link', (d.YeuCauSapToi ?? []).some((y) => y.QuaHen) ? 'danger' : (d.YeuCauDangMo ?? 0) > 0 ? 'warn' : ''],
    [quaHan, 'Khách quá hạn trả phòng', 'di', 'tab', quaHan > 0 ? 'danger' : ''],
    [d.SuCoBuongPhong ?? 0, 'Sự cố khi dọn phòng', '/nhan-vien/buong-phong', 'link', (d.SuCoBuongPhong ?? 0) > 0 ? 'danger' : ''],
    [d.PhongDangDon ?? 0, 'Phòng đang dọn', '/nhan-vien/buong-phong', 'link', ''],
    [d.ViecBuongPhongChuaXong ?? 0, 'Việc buồng phòng chưa xong', '/nhan-vien/buong-phong', 'link', ''],
    [d.PhongBaoTri ?? 0, 'Phòng đang bảo trì', '/nhan-vien/phong', 'link', (d.PhongBaoTri ?? 0) > 0 ? 'danger' : '']
  ];
  const phongs = rooms.data ?? [];
  const timPhieu = (e: FormEvent) => { e.preventDefault(); if (tim.trim()) navigate(`/nhan-vien/lich-su-dat-phong?search=${encodeURIComponent(tim.trim())}`); };
  const moHop = (loai: LoaiHop, k: FrontDeskStay, dangO: boolean) => setHop({ loai, k, dangO });

  const dongPhieu = (k: FrontDeskStay, t: Tab) => {
    const chuaNhan = k.TrangThai === 'Pending' || k.TrangThai === 'Approved';
    const canhBao = chuaNhan && t === 'den' ? phongChuaSanSang(k) : null;
    const dangO = k.TrangThai === 'CheckedIn';
    return (
      <div key={k.MaDatPhong} className={`phieu-dong${k.DaXuLy ? ' da-xu-ly' : ''}${k.QuaHanTra ? ' qua-han' : ''}${k.HanChe ? ' han-che' : ''}`} data-phieu={k.MaDatPhong}>
        <div className="pd-phong"><strong>{k.SoPhong ?? '—'}</strong><small>{tenHangPhong(k.TenLoaiPhong)}</small></div>
        <div className="pd-chinh">
          <div className="pd-khach">
            {k.MaKhachHang ? <Link to={`/nhan-vien/khach-hang/${k.MaKhachHang}`}>{k.HoTenKhachHang}</Link> : <span>{k.HoTenKhachHang}</span>}
            {k.LaVIP && <span className="the-vip">VIP</span>}
            {k.HanChe && <span className="the-han-che" title={k.LyDoHanChe ?? ''}>Hạn chế</span>}
          </div>
          <div className="pd-meta">
            <span>#{k.MaDatPhong}</span>
            <span>{ngay(k.NgayNhan).slice(0, 5)} → {ngay(k.NgayTra).slice(0, 5)} · {k.SoDem} đêm</span>
            <span>{k.SoKhach} khách</span>
            {k.SDT && <span>{k.SDT}</span>}
          </div>
          <div className="pd-ghi-chu">
            {k.HanChe && k.LyDoHanChe && <span className="gc-do">{k.LyDoHanChe}</span>}
            {k.GioDenDuKien && chuaNhan && <span>Đến {k.GioDenDuKien}</span>}
            {k.YeuCauDacBiet && <span className="gc-vang" title={k.YeuCauDacBiet}>{k.YeuCauDacBiet}</span>}
            {k.SoThich && <span title={k.SoThich}>♥ {k.SoThich}</span>}
            {k.MaKhuyenMai && <span>KM {k.MaKhuyenMai}</span>}
            {(k.TienCoc ?? 0) > 0 && <span>Cọc {tien(k.TienCoc)}</span>}
            {k.NguonDat && k.NguonDat !== 'Online' && <span className="gc-nguon">{k.NguonDat}</span>}
            {k.KhongLamPhien && <span className="gc-do">Không làm phiền hôm nay</span>}
            {k.GioDonMongMuon && <span>Dọn: {k.GioDonMongMuon}</span>}
            {k.TuChoiDonHangNgay && <span>Không dọn hằng ngày</span>}
            {(k.KhachDiCung?.length ?? 0) > 0 && <span title={k.KhachDiCung!.map((x) => x.HoTen).join(', ')}>{k.KhachDiCung!.length} người đi cùng</span>}
            {canhBao && <span className="gc-do">{canhBao} — đổi phòng khác hoặc chờ buồng phòng</span>}
          </div>
        </div>
        <div className="pd-thao-tac">
          {t === 'den' && k.DaXuLy ? <span className="badge badge-ok">Đã nhận {k.NgayCheckInThucTe?.slice(11, 16)}</span>
            : t === 'di' && k.DaXuLy ? <span className="badge badge-muted">Đã trả {k.NgayCheckOutThucTe?.slice(11, 16)}</span>
              : k.QuaHanTra ? <span className="badge badge-danger">Quá hạn trả</span>
                : chuaNhan ? <span className={`badge ${k.TrangThai === 'Pending' ? 'badge-warn' : 'badge-info'}`}>{k.TrangThai === 'Pending' ? 'Chờ duyệt' : 'Đã xác nhận'}</span> : null}
          <div className="pd-nut">
            {chuaNhan && (
              <>
                {k.TrangThai === 'Pending' && <button type="button" className="btn btn-outline btn-sm" disabled={duyet.isPending} onClick={() => duyet.mutate(k.MaDatPhong!)}>Duyệt</button>}
                {t === 'den' && <button type="button" className="btn btn-gold btn-sm" disabled={!!canhBao} onClick={() => moHop('nhanPhong', k, false)}>Nhận phòng</button>}
                <Link to={`/nhan-vien/le-tan/doi-phong/${k.MaDatPhong}`} className={`btn btn-sm ${canhBao ? 'btn-outline-danger' : 'btn-outline'}`}>Đổi phòng</Link>
                <MenuPhieu k={k} dangO={false} onMo={moHop} />
                <button type="button" className="btn btn-outline-danger btn-sm" aria-label={k.TrangThai === 'Pending' ? `Từ chối phiếu ${k.MaDatPhong}` : `Huỷ phiếu ${k.MaDatPhong}`}
                  title={k.TrangThai === 'Pending' ? 'Từ chối' : 'Huỷ phiếu'} onClick={() => moHop('huy', k, false)}>×</button>
              </>
            )}
            {dangO && (
              <>
                <Link to={`/nhan-vien/dich-vu?maPhong=${k.MaPhong}`} className="btn btn-outline btn-sm">Dịch vụ</Link>
                <Link to={`/nhan-vien/tra-phong?maPhong=${k.MaPhong}`} className={`btn btn-sm ${t === 'di' ? 'btn-gold' : 'btn-outline'}`}>Trả phòng</Link>
                <MenuPhieu k={k} dangO onMo={moHop} />
              </>
            )}
          </div>
        </div>
      </div>
    );
  };

  return (
    <>
      <div className="trang-dau">
        <div>
          <p className="eyebrow">Quầy lễ tân</p>
          <h1>{thuCua(d.Ngay)}, {ngay(d.Ngay)}</h1>
          <p className="muted">Cập nhật lúc {ngayGio(d.NgayCapNhat).slice(11)} · {d.TongSoPhong} phòng</p>
        </div>
        <div className="trang-dau-nut">
          <form role="search" onSubmit={timPhieu} className="tim-phieu">
            <input type="search" value={tim} onChange={(e) => setTim(e.target.value)} placeholder="Tìm phiếu đặt phòng…" title="Mã phiếu, tên khách hoặc số phòng" aria-label="Tìm phiếu đặt phòng" />
          </form>
          <Link to="/nhan-vien/dat-phong-quay" className="btn btn-outline btn-sm">Đặt phòng</Link>
          <Link to="/nhan-vien/vang-lai" className="btn btn-gold btn-sm">Nhận khách vãng lai</Link>
        </div>
      </div>

      <div className="kpi-luoi">
        <button type="button" className="kpi kpi-toi" onClick={() => chonTab('den', true)}><span>Khách đến</span><strong>{d.SoKhachDen}</strong><small>Đã nhận {d.SoKhachDenDaNhan} · còn {(d.SoKhachDen ?? 0) - (d.SoKhachDenDaNhan ?? 0)}</small></button>
        <button type="button" className="kpi" onClick={() => chonTab('di', true)}><span>Khách đi</span><strong>{d.SoKhachDi}</strong><small>Đã trả {d.SoKhachDiDaTra} · còn {(d.SoKhachDi ?? 0) - (d.SoKhachDiDaTra ?? 0)}{quaHan > 0 ? ` · ${quaHan} quá hạn` : ''}</small></button>
        <button type="button" className="kpi" onClick={() => chonTab('dango', true)}><span>Đang lưu trú</span><strong>{d.DangO?.length ?? 0}</strong><small>{d.PhongCoKhach} / {d.TongSoPhong} phòng có khách</small></button>
        <a href="#so-do-phong" className="kpi"><span>Trống tối nay</span><strong>{d.PhongTrong}</strong><small>Đang dọn {d.PhongDangDon} · bảo trì {d.PhongBaoTri}</small></a>
      </div>

      <div className="quay-luoi">
        <section className="the-trang quay-ds" ref={tabsRef} aria-label="Danh sách phiếu hôm nay">
          <div className="quay-tabs" role="tablist">
            {(Object.keys(ds) as Tab[]).map((t) => (
              <button key={t} type="button" role="tab" id={`tab-${t}`} aria-selected={tab === t} aria-controls={`panel-${t}`} className={tab === t ? 'active' : ''} onClick={() => chonTab(t)}>
                {ds[t][0]} <span>{ds[t][1].length}</span>
              </button>
            ))}
          </div>
          <div role="tabpanel" id={`panel-${tab}`} aria-labelledby={`tab-${tab}`} data-panel={tab}>
            {ds[tab][1].length === 0 ? <p className="muted small quay-trong">{ds[tab][2]}</p>
              : tab === 'sapden' ? nhomTheoNgay(ds.sapden[1]).map(([n, g]) => (
                <div key={n}>
                  <div className="quay-ngay">{thuCua(n)}, {ngay(n)} <span>{g.length} phiếu</span></div>
                  {g.map((k) => dongPhieu(k, tab))}
                </div>
              )) : ds[tab][1].map((k) => dongPhieu(k, tab))}
          </div>
        </section>

        <aside>
          <section className="the-trang">
            <h2 className="quay-h">Việc cần làm</h2>
            <div className="viec-can-lam">
              {viec.map(([so, ten, toi, kieu, muc]) => kieu === 'tab'
                ? <button key={ten} type="button" className={`vcl ${so === 0 ? 'vcl-0' : muc}`} onClick={() => chonTab(toi as Tab, true)}><span>{ten}</span><strong>{so}</strong></button>
                : <Link key={ten} to={toi} className={`vcl ${so === 0 ? 'vcl-0' : muc}`}><span>{ten}</span><strong>{so}</strong></Link>)}
            </div>
          </section>
          {d.CaTruoc?.GhiChuBanGiao && (
            <section className="the-trang ban-giao">
              <div className="the-dau"><h2 className="quay-h">Bàn giao từ ca trước</h2><Link to="/nhan-vien/giao-ca" className="small">Giao ca</Link></div>
              <p className="small muted">{d.CaTruoc.TenNhanVien} · chốt lúc {d.CaTruoc.KetThuc?.slice(11, 16)} {ngay(d.CaTruoc.KetThuc).slice(0, 5)}</p>
              <p className="ban-giao-ghi-chu">{d.CaTruoc.GhiChuBanGiao}</p>
            </section>
          )}
          {(d.YeuCauSapToi?.length ?? 0) > 0 && (
            <section className="the-trang">
              <div className="the-dau"><h2 className="quay-h">Yêu cầu khách cần làm</h2><Link to="/nhan-vien/yeu-cau" className="small">Tất cả ({d.YeuCauDangMo})</Link></div>
              {d.YeuCauSapToi!.map((y) => (
                <div key={y.MaYeuCau} className={`yc-nho${y.QuaHen ? ' qua-han' : ''}`}>
                  <span className="yc-gio">{y.ThoiGianHen?.slice(11, 16) ?? '—'}{y.ThoiGianHen && y.ThoiGianHen.slice(0, 10) !== d.Ngay?.slice(0, 10) && <small>{ngay(y.ThoiGianHen).slice(0, 5)}</small>}</span>
                  <span><strong>{y.Loai}</strong>{y.SoPhong ? ` · P${y.SoPhong}` : ''} — {y.NoiDung}</span>
                </div>
              ))}
            </section>
          )}
          <section className="the-trang">
            <h2 className="quay-h">Tra cứu nhanh</h2>
            <div className="tra-cuu">
              <Link to="/nhan-vien/lich-phong">Lịch phòng</Link>
              <Link to="/nhan-vien/lich-su-dat-phong">Lịch sử đặt phòng</Link>
              <Link to="/nhan-vien/khach-hang">Khách hàng</Link>
              <Link to="/nhan-vien/so-luu-tru">Sổ lưu trú</Link>
              <Link to="/nhan-vien/yeu-cau">Yêu cầu khách</Link>
              <Link to="/nhan-vien/giao-ca">Giao ca</Link>
            </div>
          </section>
        </aside>
      </div>

      <section className="the-trang" id="so-do-phong" aria-labelledby="sdp">
        <div className="the-dau">
          <h2 id="sdp" className="quay-h">Sơ đồ phòng</h2>
          <div className="loc-chip" role="group" aria-label="Lọc sơ đồ phòng">
            <button type="button" className={locPhong === 'all' ? 'active' : ''} aria-pressed={locPhong === 'all'} onClick={() => setLocPhong('all')}>Tất cả <span>{phongs.length}</span></button>
            {(['Available', 'Occupied', 'Cleaning', 'Booked', 'Maintenance'] as RoomStatus[]).map((t) => (
              <button key={t} type="button" className={locPhong === t ? 'active' : ''} aria-pressed={locPhong === t} onClick={() => setLocPhong(t)}>{PHONG[t][1]} <span>{phongs.filter((r) => r.TrangThai === t).length}</span></button>
            ))}
          </div>
        </div>
        <div className="so-do-luoi">
          {phongs.filter((r) => locPhong === 'all' || r.TrangThai === locPhong).map((r) => {
            const [css, nhan] = PHONG[r.TrangThai ?? 'Available'];
            return (
              <div key={r.MaPhong} className={`sdp-o ${css}`} data-trang-thai={r.TrangThai}>
                <span className={`badge-tt ${css}`}>{nhan}</span>
                <strong className="sdp-so">{r.SoPhong}</strong>
                <small className="sdp-hang">{tenHangPhong(r.LoaiPhong?.TenLoai)}</small>
                {r.TrangThai === 'Occupied' && r.DangLuuTru ? <small className="sdp-khach" title={r.DangLuuTru.KhachHang?.HoTen ?? ''}>{r.DangLuuTru.KhachHang?.HoTen}</small>
                  : r.DatPhongHienTai ? <small className="sdp-khach">Khách đến {ngay(r.DatPhongHienTai.NgayCheckInDuKien).slice(0, 5)}</small> : null}
                <div className="sdp-nut">
                  {r.TrangThai === 'Occupied' ? (
                    <><Link to={`/nhan-vien/dich-vu?maPhong=${r.MaPhong}`} className="btn btn-outline btn-sm">Dịch vụ</Link><Link to={`/nhan-vien/tra-phong?maPhong=${r.MaPhong}`} className="btn btn-gold btn-sm">Trả phòng</Link></>
                  ) : r.TrangThai === 'Available' ? <Link to={`/nhan-vien/vang-lai?maPhong=${r.MaPhong}`} className="btn btn-outline btn-sm">Nhận khách</Link>
                    : <Link to={`/nhan-vien/phong/${r.MaPhong}`} className="btn btn-outline btn-sm">Chi tiết</Link>}
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {hop && <HopPhieu loai={hop.loai} k={hop.k} dangO={hop.dangO} onDong={() => setHop(null)} />}
    </>
  );
}

function nhomTheoNgay(ds: FrontDeskStay[]): [string, FrontDeskStay[]][] {
  const m = new Map<string, FrontDeskStay[]>();
  for (const k of ds) { const n = (k.NgayNhan ?? '').slice(0, 10); m.set(n, [...(m.get(n) ?? []), k]); }
  return [...m.entries()];
}

// Menu thao tác phụ của một phiếu: đổi ngày / gia hạn, đổi phòng (đang ở), người đi cùng, thu cọc, yêu cầu khách, dặn dò buồng phòng
function MenuPhieu({ k, dangO, onMo }: { k: FrontDeskStay; dangO: boolean; onMo: (l: LoaiHop, k: FrontDeskStay, dangO: boolean) => void }) {
  const [mo, setMo] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!mo) return;
    const dong = (e: MouseEvent | KeyboardEvent) => {
      if (e instanceof KeyboardEvent ? e.key === 'Escape' : !ref.current?.contains(e.target as Node)) setMo(false);
    };
    document.addEventListener('mousedown', dong);
    document.addEventListener('keydown', dong);
    return () => { document.removeEventListener('mousedown', dong); document.removeEventListener('keydown', dong); };
  }, [mo]);
  const chon = (l: LoaiHop) => { setMo(false); onMo(l, k, dangO); };
  return (
    <div className="menu-phieu" ref={ref}>
      <button type="button" className="btn btn-outline btn-sm" aria-haspopup="menu" aria-expanded={mo} aria-label={`Thao tác khác phiếu ${k.MaDatPhong}`} title="Thao tác khác" onClick={() => setMo(!mo)}>⋯</button>
      {mo && (
        <div className="menu-phieu-ds" role="menu">
          <button type="button" role="menuitem" onClick={() => chon('doiNgay')}>{dangO ? 'Gia hạn / rút ngắn' : 'Đổi ngày lưu trú'}</button>
          {dangO && <Link role="menuitem" to={`/nhan-vien/le-tan/doi-phong/${k.MaDatPhong}`}>Đổi phòng khi đang ở</Link>}
          <button type="button" role="menuitem" onClick={() => chon('diCung')}>Người đi cùng</button>
          <button type="button" role="menuitem" onClick={() => chon('thuCoc')}>Thu cọc</button>
          {dangO && <Link role="menuitem" to={`/nhan-vien/yeu-cau?maDatPhong=${k.MaDatPhong}`}>Ghi yêu cầu của khách</Link>}
          {dangO && <button type="button" role="menuitem" onClick={() => chon('donPhong')}>Dặn dò buồng phòng</button>}
        </div>
      )}
    </div>
  );
}
