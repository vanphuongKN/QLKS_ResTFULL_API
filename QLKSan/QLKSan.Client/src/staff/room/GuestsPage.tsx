import { useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { reportApi, TRANG_THAI_PHIEU } from '../../api/staff';
import { ErrorBox, Loading } from '../../components/common';
import { NutTaiTep, PageHeader } from '../../components/ui/Page';
import { homNay, ngay, ngayGio } from '../../utils/format';
import './room.css';

const ngayHopLe = (v: string | null) => (v && /^\d{4}-\d{2}-\d{2}$/.test(v) ? v : null);

// Sổ khách lưu trú (khai báo tạm trú): khách có thời gian ở giao với khoảng ngày; mặc định hôm nay (Room/Guests của MVC)
export function GuestsPage() {
  const [params, setParams] = useSearchParams();
  let tu = ngayHopLe(params.get('tuNgay')) ?? homNay();
  let den = ngayHopLe(params.get('denNgay')) ?? tu;
  if (den < tu) [tu, den] = [den, tu];
  const q = useQuery({ queryKey: ['guests', tu, den], queryFn: () => reportApi.guests(tu, den) });
  const [f, setF] = useState({ tu, den });
  const xem = (e: FormEvent) => { e.preventDefault(); setParams({ tuNgay: f.tu, denNgay: f.den }); };

  const ds = q.data ?? [];
  const luot = ds.filter((g) => !g.LaNguoiDiCung);
  const soKhach = luot.reduce((t, g) => t + (g.SoLuongKhach ?? 0), 0);
  const nuocNgoai = ds.filter((g) => g.QuocGia && !g.QuocGia.includes('Việt Nam')).length;
  const tenTep = `SoKhachLuuTru_${tu.replace(/-/g, '')}_${den.replace(/-/g, '')}.xlsx`;

  return (
    <>
      <PageHeader tieuDe="Sổ khách lưu trú" phu={`Khách có thời gian lưu trú trong khoảng ${ngay(tu)} – ${ngay(den)}: dùng để khai báo tạm trú với công an khu vực.`}>
        <NutTaiTep path={reportApi.guestsExportPath(tu, den)} tenMacDinh={tenTep}>Xuất Excel</NutTaiTep>
      </PageHeader>
      <form className="the-trang so-luu-tru-loc" onSubmit={xem} aria-label="Lọc theo khoảng ngày">
        <label className="field"><span>Từ ngày</span><input type="date" value={f.tu} onChange={(e) => setF({ ...f, tu: e.target.value })} required /></label>
        <label className="field"><span>Đến ngày</span><input type="date" value={f.den} onChange={(e) => setF({ ...f, den: e.target.value })} required /></label>
        <button type="submit" className="btn btn-gold btn-sm">Xem</button>
        <p className="small muted so-luu-tru-dem"><strong>{luot.length}</strong> lượt lưu trú · <strong>{ds.length}</strong> / {soKhach} khách đã khai báo · <strong>{nuocNgoai}</strong> khách nước ngoài</p>
      </form>
      <ErrorBox message={q.error?.message} />
      {q.isLoading ? <Loading /> : (
        <div className="bang-cuon">
          <table className="bang so-luu-tru">
            <thead><tr><th>#</th><th>Họ tên</th><th>Ngày sinh</th><th>CCCD / Hộ chiếu</th><th>Quốc tịch</th><th>Địa chỉ / SĐT</th><th>Phòng</th><th className="canh-giua">Số khách</th><th>Ngày đến</th><th>Ngày đi</th><th>Trạng thái</th></tr></thead>
            <tbody>
              {ds.length === 0 && <tr><td colSpan={11} className="bang-trong">Không có khách lưu trú trong khoảng ngày này.</td></tr>}
              {ds.map((g, i) => (
                <tr key={i} className={g.LaNguoiDiCung ? 'khach-di-cung' : undefined}>
                  <td>{i + 1}</td>
                  <td><strong>{g.LaNguoiDiCung ? '↳ ' : ''}{g.HoTen}</strong><br /><small className="muted">{g.LaNguoiDiCung ? `Đi cùng ${g.NguoiDungTen}` : g.MaDatPhong}</small></td>
                  <td>{ngay(g.NgaySinh)}</td>
                  <td>{g.CCCD_Passport}</td>
                  <td>{g.QuocGia}</td>
                  <td><small>{g.DiaChi}<br />{g.SDT}</small></td>
                  <td><strong>{g.SoPhong}</strong></td>
                  <td className="canh-giua">{g.LaNguoiDiCung ? '' : g.SoLuongKhach}</td>
                  <td>{ngayGio(g.NgayDen)}</td>
                  <td>{g.NgayDi ? ngayGio(g.NgayDi) : <span className="muted">dự kiến {ngay(g.NgayDiDuKien)}</span>}</td>
                  <td><span className={`badge ${g.TrangThai === 'CheckedIn' ? 'badge-ok' : 'badge-muted'}`}>{TRANG_THAI_PHIEU[g.TrangThai!]}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
