import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { roomApi, TRANG_THAI_PHIEU, type BookingStatus, type TimelineBooking } from '../../api/staff';
import { ErrorBox, Loading } from '../../components/common';
import { PageHeader } from '../../components/ui/Page';
import { addDays, homNay, ngay } from '../../utils/format';
import './room.css';

const THU = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
const CSS: Partial<Record<BookingStatus, string>> = { Pending: 'tl-cho', Approved: 'tl-duyet', CheckedIn: 'tl-o' };
const thuCua = (iso: string) => { const [y, m, d] = iso.split('-').map(Number); return new Date(y, m - 1, d).getDay(); };
const laCuoiTuan = (iso: string) => [5, 6].includes(thuCua(iso));
const ngayHopLe = (v: string | null) => (v && /^\d{4}-\d{2}-\d{2}$/.test(v) ? v : null);

// Thanh phiếu trên lưới: mỗi phiếu chiếm từ đêm đầu tới hết phiếu, cắt theo khung đang xem (như Room/Timeline của MVC)
export function chiaO(ngays: string[], phieu: TimelineBooking[]) {
  const ketThuc = addDays(ngays[0], ngays.length);
  const o: ({ ngay: string } | { b: TimelineBooking; span: number })[] = [];
  for (let i = 0; i < ngays.length;) {
    const d = ngays[i];
    const b = phieu.find((x) => (x.TuNgay ?? '').slice(0, 10) <= d && (x.DenNgay ?? '').slice(0, 10) > d);
    if (!b) { o.push({ ngay: d }); i++; continue; }
    const den = (b.DenNgay ?? '').slice(0, 10) < ketThuc ? (b.DenNgay ?? '').slice(0, 10) : ketThuc;
    const span = Math.max(1, Math.round((new Date(den).getTime() - new Date(d).getTime()) / 86_400_000));
    o.push({ b, span });
    i += span;
  }
  return o;
}

// Lịch phòng: mỗi hàng 1 phòng, mỗi cột 1 đêm — mặc định 14 đêm từ hôm nay
export function TimelinePage() {
  const [params, setParams] = useSearchParams();
  const tuNgay = ngayHopLe(params.get('tuNgay')) ?? homNay();
  const soNgay = Math.min(31, Math.max(7, Number(params.get('soNgay')) || 14));
  const ngays = Array.from({ length: soNgay }, (_, i) => addDays(tuNgay, i));
  const phong = useQuery({ queryKey: ['rooms'], queryFn: roomApi.list });
  const lich = useQuery({ queryKey: ['timeline', tuNgay, soNgay], queryFn: () => roomApi.timeline(tuNgay, addDays(tuNgay, soNgay - 1)) });
  const [chonNgay, setChonNgay] = useState(tuNgay);
  const [chonSo, setChonSo] = useState(soNgay);
  const toi = (tu: string, so = soNgay) => setParams({ tuNgay: tu, soNgay: String(so) });
  const xem = (e: FormEvent) => { e.preventDefault(); toi(chonNgay, chonSo); };

  const rooms = [...(phong.data ?? [])].sort((a, b) => (a.SoPhong ?? '').localeCompare(b.SoPhong ?? '', 'vi', { numeric: true }));
  return (
    <>
      <PageHeader tieuDe="Lịch phòng" phu={`${ngay(tuNgay)} – ${ngay(addDays(tuNgay, soNgay - 1))} · mỗi ô là 1 đêm`}>
        <form className="tl-dieu-khien" onSubmit={xem} aria-label="Chọn khoảng ngày">
          <button type="button" className="btn btn-outline btn-sm" aria-label="Lùi 7 ngày" onClick={() => toi(addDays(tuNgay, -7))}>‹</button>
          <input type="date" value={chonNgay} onChange={(e) => setChonNgay(e.target.value)} aria-label="Từ ngày" required />
          <select value={chonSo} onChange={(e) => setChonSo(Number(e.target.value))} aria-label="Số ngày">
            {[7, 14, 21, 31].map((n) => <option key={n} value={n}>{n} ngày</option>)}
          </select>
          <button type="submit" className="btn btn-gold btn-sm">Xem</button>
          <button type="button" className="btn btn-outline btn-sm" aria-label="Tới 7 ngày" onClick={() => toi(addDays(tuNgay, 7))}>›</button>
          <button type="button" className="btn btn-outline btn-sm" onClick={() => { setChonNgay(homNay()); setChonSo(14); setParams({}); }}>Hôm nay</button>
        </form>
      </PageHeader>
      <p className="tl-chu-thich small muted"><span className="tl-cho" />Chờ duyệt <span className="tl-duyet" />Đã duyệt <span className="tl-o" />Đang ở <span className="tl-tra" />Đã trả phòng</p>
      <ErrorBox message={phong.error?.message ?? lich.error?.message} />
      {phong.isLoading || lich.isLoading ? <Loading /> : (
        <div className="bang-cuon tl-khung">
          <table className="tl" style={{ minWidth: 130 + soNgay * 62 }}>
            <colgroup><col className="tl-cot-phong" />{ngays.map((d) => <col key={d} />)}</colgroup>
            <thead>
              <tr>
                <th scope="col" className="tl-phong">Phòng</th>
                {ngays.map((d) => <th key={d} scope="col" className={d === homNay() ? 'tl-hom-nay' : laCuoiTuan(d) ? 'tl-cuoi-tuan' : ''}>{THU[thuCua(d)]}<br />{ngay(d).slice(0, 5)}</th>)}
              </tr>
            </thead>
            <tbody>
              {rooms.map((r) => (
                <tr key={r.MaPhong} data-phong={r.SoPhong}>
                  <th scope="row" className="tl-phong"><Link to={`/nhan-vien/phong/${r.MaPhong}`}>{r.SoPhong}</Link><small>{r.LoaiPhong?.TenLoai}</small></th>
                  {chiaO(ngays, (lich.data ?? []).filter((b) => b.MaPhong === r.MaPhong).sort((a, b) => (a.TuNgay ?? '').localeCompare(b.TuNgay ?? ''))).map((o) => 'b' in o ? (
                    <td key={o.b.MaDatPhong} colSpan={o.span}>
                      <span className={`tl-thanh ${CSS[o.b.TrangThai!] ?? 'tl-tra'}`} title={`${o.b.MaDatPhong} · ${o.b.HoTenKhachHang} · ${ngay(o.b.TuNgay).slice(0, 5)} → ${ngay(o.b.DenNgay).slice(0, 5)} · ${TRANG_THAI_PHIEU[o.b.TrangThai!]}`}>
                        {o.b.HoTenKhachHang} <small>({o.b.MaDatPhong})</small>
                      </span>
                    </td>
                  ) : <td key={o.ngay} className={laCuoiTuan(o.ngay) ? 'tl-cuoi-tuan' : ''} />)}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
