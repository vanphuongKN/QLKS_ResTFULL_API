import { lazy, Suspense } from 'react';
import { Link, Navigate, Route, Routes, useLocation, useParams } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Loading, RequireAuth } from './components/common';
import { ChangePasswordPage, LoginPage, RegisterPage } from './pages/AuthPages';
import { BookPage } from './pages/BookPage';
import { BookingDetailPage } from './pages/BookingDetailPage';
import { BookingsPage } from './pages/BookingsPage';
import { HomePage } from './pages/HomePage';
import { ProfilePage } from './pages/ProfilePage';
import { RoomTypePage } from './pages/RoomTypePage';
import { SearchPage } from './pages/SearchPage';

// Đường dẫn cũ của trang Web MVC (vd liên kết trợ lý ảo đã gửi) → trang tương ứng của SPA
function LegacyBookRoom() {
  return <Navigate to={`/dat-phong${useLocation().search}`} replace />;
}
function LegacyRoomType() {
  return <Navigate to={`/hang-phong/${useParams().id}`} replace />;
}

// Cổng nhân viên tải riêng khi cần: khách không tải mã của cổng này
const StaffApp = lazy(() => import('./staff/StaffApp'));

export function App() {
  return (
    <Routes>
      <Route path="nhan-vien/*" element={<Suspense fallback={<Loading />}><StaffApp /></Suspense>} />
      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="tim-phong" element={<SearchPage />} />
        <Route path="hang-phong/:maLoai" element={<RoomTypePage />} />
        <Route path="dat-phong" element={<RequireAuth><BookPage /></RequireAuth>} />
        <Route path="phieu" element={<RequireAuth><BookingsPage /></RequireAuth>} />
        <Route path="phieu/:maDatPhong" element={<RequireAuth><BookingDetailPage /></RequireAuth>} />
        <Route path="tai-khoan" element={<RequireAuth><ProfilePage /></RequireAuth>} />
        <Route path="doi-mat-khau" element={<RequireAuth><div className="container"><ChangePasswordPage /></div></RequireAuth>} />
        <Route path="dang-nhap" element={<LoginPage />} />
        <Route path="dang-ky" element={<RegisterPage />} />
        <Route path="Customer/BookRoom" element={<LegacyBookRoom />} />
        <Route path="Home/RoomType/:id" element={<LegacyRoomType />} />
        <Route path="*" element={<div className="container section"><h1>Không tìm thấy trang</h1><Link to="/">← Về trang chủ</Link></div>} />
      </Route>
    </Routes>
  );
}
