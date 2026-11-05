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
  // Chuyển trang thì về đầu trang; có #neo (vd /#hang-phong) thì cuộn tới mục đó
  useEffect(() => {
    if (!location.hash) { window.scrollTo(0, 0); return; }
    const t = window.setTimeout(() => document.getElementById(decodeURIComponent(location.hash.slice(1)))?.scrollIntoView({ behavior: 'smooth' }), 60);
    return () => window.clearTimeout(t);
  }, [location.pathname, location.hash]);
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
            {/* Cùng menu với _CustomerLayout của MVC: khách đã đăng nhập thấy các trang của mình, còn lại thấy trang giới thiệu */}
            <NavLink to="/" end>Trang chủ</NavLink>
            {session?.vaiTro === 'Customer' ? (
              <>
                <NavLink to="/tim-phong">Đặt phòng</NavLink>
                <NavLink to="/phieu">Kỳ nghỉ</NavLink>
                <NavLink to="/danh-gia">Đánh giá</NavLink>
                <NavLink to="/tai-khoan" end>Hồ sơ</NavLink>
              </>
            ) : (
              <>
                <NavLink to="/cau-chuyen">Câu chuyện</NavLink>
                <Link to="/#hang-phong">Hạng phòng</Link>
                <NavLink to="/am-thuc">Ẩm thực</NavLink>
                <NavLink to="/spa">Spa</NavLink>
                <NavLink to="/thu-vien">Thư viện</NavLink>
                <NavLink to="/dac-quyen">Đặc quyền</NavLink>
              </>
            )}
            <span className="nav-tach" aria-hidden="true" />
            {session ? (
              <>
                {laNhanVien(session.vaiTro)
                  ? <NavLink to={trangChuCua(session.vaiTro)}>Cổng nhân viên</NavLink>
                  : <Link to="/tai-khoan" className="nav-ten">{session.hoTen}</Link>}
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
