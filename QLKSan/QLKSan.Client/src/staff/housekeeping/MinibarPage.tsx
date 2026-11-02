import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { hkApi, serviceApi } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { ErrorBox, Loading } from '../../components/common';
import { PageHeader } from '../../components/ui/Page';
import { so, tien } from '../../utils/format';
import './hk.css';

// Ghi minibar phòng có khách: tiền tự vào bảng kê của khách (như Housekeeping/Minibar của MVC)
export function MinibarPage() {
  const [params] = useSearchParams();
  const hk = useQuery({ queryKey: ['hk', 'summary'], queryFn: hkApi.summary });
  const dv = useQuery({ queryKey: ['services'], queryFn: serviceApi.list });
  const [maPhong, setMaPhong] = useState(params.get('maPhong') ?? '');
  const [sl, setSl] = useState<Record<string, number>>({});
  const ghi = useThaoTac({
    mutationFn: () => hkApi.minibar(maPhong, Object.entries(sl).filter(([, n]) => n > 0).map(([MaDichVu, SoLuong]) => ({ MaDichVu, SoLuong }))),
    lamMoi: [['hk']], thanhCong: (tong) => `Đã ghi minibar ${so(tong)}đ vào bảng kê của khách.`, tienToLoi: 'Không ghi được minibar. ',
    onSuccess: () => { setSl({}); setMaPhong(''); }
  });

  if (hk.isLoading || dv.isLoading) return <Loading />;
  const phongs = (hk.data?.Phong ?? []).filter((r) => r.TrangThai === 'Occupied');
  const items = (dv.data ?? []).filter((s) => s.LaMinibar && s.TrangThai === 'Available');
  const tong = items.reduce((t, d) => t + (sl[d.MaDichVu!] ?? 0) * (d.GiaDichVu ?? 0), 0);
  const dat = (ma: string, n: number) => setSl((x) => ({ ...x, [ma]: Math.max(0, Math.min(50, Number.isFinite(n) ? Math.floor(n) : 0)) }));
  const gui = (e: FormEvent) => { e.preventDefault(); ghi.mutate(); };

  return (
    <>
      <PageHeader tieuDe="Ghi minibar" phu="Kiểm tra minibar phòng có khách và ghi số lượng đã dùng — tiền tự vào bảng kê của khách, lễ tân thấy ngay khi trả phòng." />
      <ErrorBox message={hk.error?.message ?? dv.error?.message} />
      {items.length === 0 ? (
        <div className="alert alert-info">Chưa có đồ minibar nào. Quản lý đánh dấu "Đồ minibar" ở trang Dịch vụ &amp; giá.</div>
      ) : phongs.length === 0 ? (
        <div className="alert alert-info">Không có phòng nào đang có khách.</div>
      ) : (
        <form className="the-trang minibar" onSubmit={gui}>
          <label className="field"><span>Phòng</span>
            <select value={maPhong} onChange={(e) => setMaPhong(e.target.value)} required>
              <option value="">-- Chọn phòng có khách --</option>
              {phongs.map((r) => <option key={r.MaPhong} value={r.MaPhong!}>Phòng {r.SoPhong} · tầng {r.Tang}</option>)}
            </select>
          </label>
          <div className="minibar-ds">
            {items.map((d) => {
              const n = sl[d.MaDichVu!] ?? 0;
              const ten = (d.TenDichVu ?? '').replace('Minibar - ', '');
              return (
                <div key={d.MaDichVu} className="minibar-muc">
                  <div><strong>{ten}</strong><small className="muted">{tien(d.GiaDichVu)} / {d.DonViTinh}</small></div>
                  <div className="so-luong">
                    <button type="button" className="btn btn-outline btn-sm" aria-label={`Bớt ${ten}`} onClick={() => dat(d.MaDichVu!, n - 1)}>−</button>
                    <input type="number" min={0} max={50} value={n} aria-label={`Số lượng ${ten}`} onChange={(e) => dat(d.MaDichVu!, e.target.valueAsNumber)} />
                    <button type="button" className="btn btn-outline btn-sm" aria-label={`Thêm ${ten}`} onClick={() => dat(d.MaDichVu!, n + 1)}>+</button>
                  </div>
                </div>
              );
            })}
          </div>
          <div className="minibar-chan">
            <span>Tổng: <strong className="minibar-tong">{tien(tong)}</strong></span>
            <button type="submit" className="btn btn-gold" disabled={ghi.isPending || !maPhong}>Ghi vào bảng kê</button>
          </div>
        </form>
      )}
    </>
  );
}
