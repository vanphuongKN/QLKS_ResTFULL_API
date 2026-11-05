import { Link } from 'react-router-dom';
import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import { ErrorBox, Loading, useAsync } from '../components/common';
import { ngay } from '../utils/format';

export function ProfilePage() {
  const [lan, setLan] = useState(0);
  const me = useAsync(() => Promise.all([api.me(), api.myPoints()]), [lan]);
  const [form, setForm] = useState({ SDT: '', Email: '', DiaChi: '', QuocGia: '' });
  const [gui, setGui] = useState<{ busy?: boolean; error?: string; ok?: boolean }>({});

  const kh = me.data?.[0];
  useEffect(() => {
    const lh = kh?.LienHeDiaChis?.[0];
    if (lh) setForm({ SDT: lh.SDT ?? '', Email: lh.Email ?? '', DiaChi: lh.DiaChi ?? '', QuocGia: lh.QuocGia ?? '' });
  }, [kh]);

  if (me.loading && !me.data) return <div className="container section"><Loading /></div>;
  if (me.error || !kh) return <div className="container section"><ErrorBox message={me.error} /></div>;
  const tv = kh.ThanhVien;
  const diem = me.data![1];
  const tienDo = tv?.MocHangKeTiep ? Math.min(100, Math.round(((tv.DiemXetHang ?? 0) / tv.MocHangKeTiep) * 100)) : 100;

  async function luu(e: FormEvent) {
    e.preventDefault();
    setGui({ busy: true });
    try {
      await api.updateContact({ ...form, SoThich: kh!.SoThich });
      setGui({ ok: true });
      setLan((x) => x + 1);
    } catch (err) {
      setGui({ error: (err as Error).message });
    }
  }

  return (
    <div className="container section narrow">
      <div className="title-row"><h1>Tài khoản</h1><Link to="/doi-mat-khau" className="small">Đổi mật khẩu</Link></div>
      <section className="card member">
        <p className="eyebrow">{tv?.Hang ?? 'Thành viên'}</p>
        <h2>{kh.HoTen}</h2>
        <p><strong>{(kh.DiemTichLuy ?? 0).toLocaleString('vi-VN')}</strong> điểm khả dụng · {kh.SoLanDatPhong} lần lưu trú</p>
        {tv?.HangKeTiep ? (
          <>
            <div className="progress" role="progressbar" aria-valuenow={tienDo} aria-valuemin={0} aria-valuemax={100} aria-label="Tiến độ lên hạng"><span style={{ width: `${tienDo}%` }} /></div>
            <p className="muted small">Còn {(tv.DiemLenHang ?? 0).toLocaleString('vi-VN')} điểm để lên {tv.HangKeTiep}</p>
          </>
        ) : <p className="muted small">Quý khách đang ở hạng cao nhất.</p>}
        {(tv?.QuyenLoi?.length ?? 0) > 0 && <ul className="perks">{tv!.QuyenLoi!.map((q) => <li key={q}>{q}</li>)}</ul>}
      </section>

      <form className="card-form" onSubmit={luu}>
        <h2>Thông tin liên hệ</h2>
        <label><span>Số điện thoại</span><input value={form.SDT} required onChange={(e) => setForm({ ...form, SDT: e.target.value })} /></label>
        <label><span>Email</span><input type="email" value={form.Email} onChange={(e) => setForm({ ...form, Email: e.target.value })} /></label>
        <label><span>Địa chỉ</span><input value={form.DiaChi} onChange={(e) => setForm({ ...form, DiaChi: e.target.value })} /></label>
        <label><span>Quốc gia</span><input value={form.QuocGia} onChange={(e) => setForm({ ...form, QuocGia: e.target.value })} /></label>
        <ErrorBox message={gui.error} />
        {gui.ok && <div className="alert alert-ok" role="status">Đã lưu thông tin liên hệ.</div>}
        <button type="submit" className="btn btn-gold" disabled={gui.busy}>Lưu</button>
      </form>

      <section className="card">
        <h2>Lịch sử điểm</h2>
        {diem.length === 0 ? <p className="muted">Chưa có giao dịch điểm.</p> : (
          <table className="table">
            <thead><tr><th>Ngày</th><th>Nội dung</th><th className="num">Điểm</th></tr></thead>
            <tbody>
              {diem.map((d) => (
                <tr key={d.MaLichSu}><td>{ngay(d.ThoiGian)}</td><td>{d.NoiDung}</td><td className={`num ${(d.SoDiem ?? 0) < 0 ? 'neg' : 'pos'}`}>{(d.SoDiem ?? 0) > 0 ? '+' : ''}{d.SoDiem}</td></tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
