import type { ComponentType } from 'react';
import { Link, Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { cacTrangNhanVien, trangChuCua, type StaffRole } from '../auth/roles';
import { RequireAuth } from '../components/common';
import { PageHeader } from '../components/ui/Page';
import { MVC_URL } from '../config';
import { ChangePasswordPage } from '../pages/AuthPages';
import { StaffLayout } from './StaffLayout';
import './staff.css';

const MOI_NHAN_VIEN: StaffRole[] = ['Admin', 'Receptionist', 'Housekeeping', 'Accountant'];

// Trang đã chuyển sang React, theo đường dẫn menu (các bước R2–R5 thêm vào đây)
const DA_CHUYEN: Record<string, ComponentType> = {};

// Trang chưa chuyển: chỉ ra bước sẽ chuyển và mở trang tương ứng trên cổng MVC (chạy song song)
function ChuaChuyen({ ten, mvc, buoc }: { ten: string; mvc: string; buoc: string | null }) {
  return (
    <>
      <PageHeader tieuDe={ten} />
      <div className="chua-chuyen">
        <p>Trang này sẽ được chuyển sang giao diện mới ở bước <strong>{buoc}</strong>.</p>
        <p>Trong thời gian chạy song song, vui lòng dùng trang tương ứng trên cổng nhân viên hiện tại (cần đăng nhập riêng):</p>
        <p><a className="btn btn-primary btn-sm" href={MVC_URL + mvc} target="_blank" rel="noopener noreferrer">Mở «{ten}» trên cổng MVC ↗</a></p>
      </div>
    </>
  );
}

function VeTrangChu() {
  const { session } = useAuth();
  return <Navigate to={trangChuCua(session?.vaiTro)} replace />;
}

function KhongTimThay() {
  return (
    <>
      <PageHeader tieuDe="Không tìm thấy trang" />
      <p><Link to="/nhan-vien">← Về trang làm việc</Link></p>
    </>
  );
}

// Cổng nhân viên /nhan-vien/*: tải riêng (lazy) nên khách không phải tải mã của cổng này
export default function StaffApp() {
  return (
    <Routes>
      <Route element={<RequireAuth roles={MOI_NHAN_VIEN}><StaffLayout /></RequireAuth>}>
        <Route index element={<VeTrangChu />} />
        {cacTrangNhanVien().map((t) => {
          const Trang = DA_CHUYEN[t.path];
          return (
            <Route key={t.path} path={t.path.replace('/nhan-vien/', '')}
              element={<RequireAuth roles={t.roles}>{Trang ? <Trang /> : <ChuaChuyen ten={t.ten} mvc={t.mvc} buoc={t.buoc} />}</RequireAuth>} />
          );
        })}
        <Route path="doi-mat-khau" element={<ChangePasswordPage />} />
        <Route path="*" element={<KhongTimThay />} />
      </Route>
    </Routes>
  );
}
