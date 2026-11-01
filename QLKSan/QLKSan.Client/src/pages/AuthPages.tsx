import { useEffect, useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { trangChuCua, type Role } from '../auth/roles';
import { ErrorBox } from '../components/common';
import { duongQuayLai } from '../utils/links';

const quayLai = (params: URLSearchParams) => duongQuayLai(params.get('quayLai'));
// Sau đăng nhập: trang đang cần (quayLai) nếu có, không thì trang làm việc của vai trò (như Auth/Portal của MVC)
const sauDangNhap = (params: URLSearchParams, role: Role) => duongQuayLai(params.get('quayLai'), trangChuCua(role));

export function LoginPage() {
  const { session, login, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [params] = useSearchParams();
  const [ten, setTen] = useState('');
  const [mk, setMk] = useState('');
  const [st, setSt] = useState<{ busy?: boolean; error?: string }>({});

  // Cổng nhân viên đăng xuất bằng cách chuyển tới đây kèm cờ riêng (tránh trang đang mở tự chuyển sang trang đăng nhập có quayLai;
  // không dùng cờ dangXuat vì khung trang khách xử lý cờ đó bằng cách về trang chủ)
  const dangXuat = (location.state as { dangXuatNhanVien?: boolean } | null)?.dangXuatNhanVien;
  useEffect(() => {
    if (!dangXuat) return;
    if (session) logout();
    navigate(location.pathname, { replace: true, state: null });
  }, [dangXuat, session, logout, navigate, location.pathname]);

  if (session && !dangXuat) return <Navigate to={sauDangNhap(params, session.vaiTro)} replace />;

  async function submit(e: FormEvent) {
    e.preventDefault();
    setSt({ busy: true });
    try {
      const s = await login(ten.trim(), mk);
      navigate(sauDangNhap(params, s.vaiTro), { replace: true });
    } catch (err) {
      setSt({ error: (err as Error).message });
    }
  }

  return (
    <div className="container section auth">
      <form className="card-form" onSubmit={submit}>
        <h1>Đăng nhập</h1>
        <label><span>Tên đăng nhập</span><input value={ten} required autoComplete="username" onChange={(e) => setTen(e.target.value)} /></label>
        <label><span>Mật khẩu</span><input type="password" value={mk} required autoComplete="current-password" onChange={(e) => setMk(e.target.value)} /></label>
        <ErrorBox message={st.error} />
        <button type="submit" className="btn btn-gold w-100" disabled={st.busy}>{st.busy ? 'Đang đăng nhập…' : 'Đăng nhập'}</button>
        <p className="muted small">Chưa có tài khoản? <Link to={`/dang-ky?quayLai=${encodeURIComponent(quayLai(params))}`}>Đăng ký thành viên</Link></p>
      </form>
    </div>
  );
}

export function RegisterPage() {
  const { session, register } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [f, setF] = useState({ Ho: '', Ten: '', SDT: '', Email: '', CCCD_Passport: '', NgaySinh: '', DiaChi: '', TenDangNhap: '', MatKhau: '', XacNhanMatKhau: '' });
  const [st, setSt] = useState<{ busy?: boolean; error?: string }>({});
  if (session) return <Navigate to={quayLai(params)} replace />;
  const set = (k: keyof typeof f) => (e: React.ChangeEvent<HTMLInputElement>) => setF({ ...f, [k]: e.target.value });

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (f.MatKhau !== f.XacNhanMatKhau) return setSt({ error: 'Mật khẩu xác nhận không khớp.' });
    setSt({ busy: true });
    try {
      await register({ ...f, Email: f.Email || null, NgaySinh: f.NgaySinh || undefined });
      navigate(quayLai(params), { replace: true });
    } catch (err) {
      setSt({ error: (err as Error).message });
    }
  }

  return (
    <div className="container section auth">
      <form className="card-form wide" onSubmit={submit}>
        <h1>Đăng ký thành viên</h1>
        <p className="muted small">Tích điểm cho mỗi kỳ nghỉ và nhận ưu đãi theo hạng thành viên.</p>
        <div className="form-grid">
          <label><span>Họ</span><input value={f.Ho} required onChange={set('Ho')} /></label>
          <label><span>Tên</span><input value={f.Ten} required onChange={set('Ten')} /></label>
          <label><span>Số điện thoại</span><input value={f.SDT} required inputMode="tel" onChange={set('SDT')} /></label>
          <label><span>Email</span><input type="email" value={f.Email} onChange={set('Email')} /></label>
          <label><span>CCCD / Hộ chiếu</span><input value={f.CCCD_Passport} required onChange={set('CCCD_Passport')} /></label>
          <label><span>Ngày sinh</span><input type="date" value={f.NgaySinh} onChange={set('NgaySinh')} /></label>
          <label className="span-2"><span>Địa chỉ</span><input value={f.DiaChi} required onChange={set('DiaChi')} /></label>
          <label><span>Tên đăng nhập</span><input value={f.TenDangNhap} required autoComplete="username" onChange={set('TenDangNhap')} /></label>
          <span />
          <label><span>Mật khẩu</span><input type="password" value={f.MatKhau} required autoComplete="new-password" onChange={set('MatKhau')} /></label>
          <label><span>Nhập lại mật khẩu</span><input type="password" value={f.XacNhanMatKhau} required autoComplete="new-password" onChange={set('XacNhanMatKhau')} /></label>
        </div>
        <ErrorBox message={st.error} />
        <button type="submit" className="btn btn-gold w-100" disabled={st.busy}>{st.busy ? 'Đang đăng ký…' : 'Đăng ký'}</button>
        <p className="muted small">Đã có tài khoản? <Link to={`/dang-nhap?quayLai=${encodeURIComponent(quayLai(params))}`}>Đăng nhập</Link></p>
      </form>
    </div>
  );
}

// Đổi mật khẩu cho người đang đăng nhập (khách và nhân viên)
export function ChangePasswordPage() {
  const [f, setF] = useState({ MatKhauCu: '', MatKhauMoi: '', XacNhanMatKhauMoi: '' });
  const [st, setSt] = useState<{ busy?: boolean; error?: string; ok?: boolean }>({});
  const set = (k: keyof typeof f) => (e: React.ChangeEvent<HTMLInputElement>) => setF({ ...f, [k]: e.target.value });

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (f.MatKhauMoi !== f.XacNhanMatKhauMoi) return setSt({ error: 'Mật khẩu xác nhận không khớp.' });
    setSt({ busy: true });
    try {
      await api.changePassword(f);
      setF({ MatKhauCu: '', MatKhauMoi: '', XacNhanMatKhauMoi: '' });
      setSt({ ok: true });
    } catch (err) {
      setSt({ error: (err as Error).message });
    }
  }

  return (
    <div className="section auth">
      <form className="card-form" onSubmit={submit}>
        <h1>Đổi mật khẩu</h1>
        <label><span>Mật khẩu hiện tại</span><input type="password" value={f.MatKhauCu} required autoComplete="current-password" onChange={set('MatKhauCu')} /></label>
        <label><span>Mật khẩu mới</span><input type="password" value={f.MatKhauMoi} required minLength={6} autoComplete="new-password" onChange={set('MatKhauMoi')} /></label>
        <label><span>Nhập lại mật khẩu mới</span><input type="password" value={f.XacNhanMatKhauMoi} required autoComplete="new-password" onChange={set('XacNhanMatKhauMoi')} /></label>
        <ErrorBox message={st.error} />
        {st.ok && <div className="alert alert-ok" role="status">Đã đổi mật khẩu. Lần đăng nhập sau hãy dùng mật khẩu mới.</div>}
        <button type="submit" className="btn btn-gold w-100" disabled={st.busy}>{st.busy ? 'Đang lưu…' : 'Đổi mật khẩu'}</button>
      </form>
    </div>
  );
}
