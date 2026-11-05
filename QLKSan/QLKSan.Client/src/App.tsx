import { lazy, Suspense } from 'react';
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Loading, RequireAuth } from './components/common';
import { ChangePasswordPage, LoginPage, RegisterPage } from './pages/AuthPages';
import { AboutPage, DiningPage, GalleryPage, MembershipPage, SpaPage } from './pages/ContentPages';
import { FeedbackPage } from './pages/FeedbackPage';
import { InvoicePrintPage } from './pages/InvoicePrintPage';
import { BookPage } from './pages/BookPage';
import { BookingDetailPage } from './pages/BookingDetailPage';
import { BookingsPage } from './pages/BookingsPage';
import { HomePage } from './pages/HomePage';
import { ProfilePage } from './pages/ProfilePage';
import { RoomTypePage } from './pages/RoomTypePage';
import { SearchPage } from './pages/SearchPage';
import { duongDanMoi } from './utils/links';

// Đường dẫn không có trong SPA: nếu là đường dẫn cũ của cổng MVC (bookmark, email, trợ lý ảo) thì chuyển sang trang tương ứng
function KhongTimThay() {
  const { pathname, search } = useLocation();
  const moi = duongDanMoi(pathname, search);
  if (moi) return <Navigate to={moi} replace />;
  return <div className="container section"><h1>Không tìm thấy trang</h1><Link to="/">← Về trang chủ</Link></div>;
}

// Cổng nhân viên tải riêng khi cần: khách không tải mã của cổng này
const StaffApp = lazy(() => import('./staff/StaffApp'));

export function App() {
  return (
    <Routes>
      <Route path="nhan-vien/*" element={<Suspense fallback={<Loading />}><StaffApp /></Suspense>} />
      {/* Bản in hoá đơn: ngoài khung trang; khách xem hoá đơn của mình, thu ngân / kế toán xem mọi hoá đơn (API kiểm tra) */}
      <Route path="in/hoa-don/:maHoaDon" element={<RequireAuth roles={['Customer', 'Admin', 'Receptionist', 'Accountant']}><InvoicePrintPage /></RequireAuth>} />
      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="tim-phong" element={<SearchPage />} />
        <Route path="hang-phong/:maLoai" element={<RoomTypePage />} />
        <Route path="dat-phong" element={<RequireAuth><BookPage /></RequireAuth>} />
        <Route path="phieu" element={<RequireAuth><BookingsPage /></RequireAuth>} />
        <Route path="phieu/:maDatPhong" element={<RequireAuth><BookingDetailPage /></RequireAuth>} />
        <Route path="tai-khoan" element={<RequireAuth><ProfilePage /></RequireAuth>} />
        <Route path="doi-mat-khau" element={<RequireAuth><div className="container"><ChangePasswordPage /></div></RequireAuth>} />
        <Route path="danh-gia" element={<RequireAuth><FeedbackPage /></RequireAuth>} />
        <Route path="cau-chuyen" element={<AboutPage />} />
        <Route path="am-thuc" element={<DiningPage />} />
        <Route path="spa" element={<SpaPage />} />
        <Route path="thu-vien" element={<GalleryPage />} />
        <Route path="dac-quyen" element={<MembershipPage />} />
        <Route path="dang-nhap" element={<LoginPage />} />
        <Route path="dang-ky" element={<RegisterPage />} />
        <Route path="*" element={<KhongTimThay />} />
      </Route>
    </Routes>
  );
}
