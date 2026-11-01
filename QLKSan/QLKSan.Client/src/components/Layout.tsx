import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { laNhanVien, trangChuCua } from '../auth/roles';
import { ChatWidget } from './ChatWidget';
import { useAsync } from './common';

export function Layout() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [menu, setMenu] = useState(false);

  // Đăng xuất: về trang chủ trước rồi mới xoá phiên (đang ở trang cần đăng nhập thì không bị chuyển sang trang đăng nhập)
  const dangXuat = (location.state as { dangXuat?: boolean } | null)?.dangXuat;
  useEffect(() => {
    if (!dangXuat) return;
    if (session) logout();
    // Xoá cờ khỏi lịch sử: quay lại mục này sau khi đăng nhập lại không bị đăng xuất lần nữa
    navigate('/', { replace: true, state: null });
  }, [dangXuat, session, logout, navigate]);
  const hotel = useAsync(() => api.hotel(), []);
  const h = hotel.data;

  return (
    <>
      <a href="#noi-dung" className="skip-link">Bỏ qua tới nội dung</a>
      <header className="site-header">
        <div className="container header-inner">
          <Link to="/" className="brand">LUXE RESERVE</Link>
          <button type="button" className="menu-toggle" aria-expanded={menu} aria-controls="mainNav" onClick={() => setMenu(!menu)}>☰<span className="sr-only">Menu</span></button>
          <nav id="mainNav" className={menu ? 'open' : ''} aria-label="Điều hướng chính" onClick={() => setMenu(false)}>
            <NavLink to="/" end>Trang chủ</NavLink>
            <NavLink to="/tim-phong">Tìm phòng</NavLink>
            {session && !laNhanVien(session.vaiTro) && <NavLink to="/phieu">Phiếu của tôi</NavLink>}
            {session ? (
              <>
                {laNhanVien(session.vaiTro)
                  ? <NavLink to={trangChuCua(session.vaiTro)}>Cổng nhân viên</NavLink>
                  : <NavLink to="/tai-khoan">{session.hoTen}</NavLink>}
                <button type="button" className="link-btn" onClick={() => navigate('/', { state: { dangXuat: true } })}>Đăng xuất</button>
              </>
            ) : (
              <>
                <NavLink to="/dang-nhap">Đăng nhập</NavLink>
                <NavLink to="/dang-ky" className="btn btn-outline">Đăng ký</NavLink>
              </>
            )}
          </nav>
        </div>
      </header>

      <main id="noi-dung">
        <Outlet />
      </main>

      <footer className="site-footer">
        <div className="container footer-grid">
          <div>
            <p className="brand">{h?.Ten ?? 'LUXE RESERVE'}</p>
            <p>{h?.MoTaNgan}</p>
          </div>
          <div>
            <h2>Liên hệ</h2>
            <p>{h?.DiaChi}</p>
            <p>Hotline: {h?.Hotline} · {h?.Email}</p>
          </div>
          <div>
            <h2>Lưu trú</h2>
            <p>Nhận phòng từ {h?.GioNhanPhong} · trả phòng trước {h?.GioTraPhong}</p>
            <p>{h?.ChinhSachHuy}</p>
          </div>
        </div>
      </footer>
      <ChatWidget />
    </>
  );
}
