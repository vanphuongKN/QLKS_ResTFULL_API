import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { laNhanVien, menuCho, NHAN_VAI_TRO } from '../auth/roles';
import { NightAuditBanner } from './NightAuditBanner';

// Khung cổng nhân viên: sidebar theo vai trò (cùng menu với MVC), thanh trên có tên + vai trò, banner chốt ngày
export function StaffLayout() {
  const { session } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [mo, setMo] = useState(false);

  // Màn hình hẹp: chuyển trang thì đóng sidebar
  useEffect(() => setMo(false), [location.pathname]);

  if (!session || !laNhanVien(session.vaiTro)) return null;
  const nhoms = menuCho(session.vaiTro);
  const dangXuat = () => navigate('/dang-nhap', { state: { dangXuatNhanVien: true } });

  return (
    <div className={`staff${mo ? ' staff-mo-menu' : ''}`}>
      <a href="#noi-dung-nv" className="skip-link">Bỏ qua tới nội dung</a>
      <aside className="staff-sidebar" id="staffSidebar" aria-label="Menu nhân viên">
        <Link to="/nhan-vien" className="staff-brand">LUXE RESERVE</Link>
        <p className="staff-sub">Cổng thông tin nhân viên</p>
        <nav className="staff-nav">
          {nhoms.map((g) => (
            <div key={g.nhom || '_'} className="staff-nhom">
              {g.nhom && <div className="staff-nhom-ten">{g.nhom}</div>}
              {g.muc.map((m) => (
                <NavLink key={m.path} to={m.path} end className="staff-muc">{m.ten}</NavLink>
              ))}
            </div>
          ))}
        </nav>
        <button type="button" className="staff-dang-xuat" onClick={dangXuat}>Đăng xuất</button>
      </aside>
      <button type="button" className="staff-phu" aria-label="Đóng menu" tabIndex={-1} onClick={() => setMo(false)} />

      <div className="staff-chinh">
        <header className="staff-top">
          <button type="button" className="staff-nut-menu" aria-expanded={mo} aria-controls="staffSidebar" onClick={() => setMo(!mo)}>☰<span className="sr-only">Menu</span></button>
          <span className="staff-top-brand">LUXE RESERVE</span>
          <div className="staff-nguoi-dung">
            <span className="staff-vai-tro">{NHAN_VAI_TRO[session.vaiTro]}</span>
            <span className="staff-ten">{session.hoTen}</span>
            <Link to="/nhan-vien/doi-mat-khau" className="staff-lien-ket">Đổi mật khẩu</Link>
            <button type="button" className="link-btn staff-lien-ket" onClick={dangXuat}>Đăng xuất</button>
          </div>
        </header>
        <main id="noi-dung-nv" className="staff-noi-dung">
          <NightAuditBanner />
          <Outlet />
        </main>
      </div>
    </div>
  );
}
