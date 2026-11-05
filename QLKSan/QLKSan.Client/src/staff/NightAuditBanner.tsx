import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { coQuyen, VAI_TRO } from '../auth/roles';
import { addDays, homNay, ngay } from '../utils/format';

// Cảnh báo còn ngày chưa chốt (như NightAuditBannerViewComponent của MVC): chỉ lễ tân / kế toán / quản trị;
// không lấy được tình trạng thì bỏ qua, không làm hỏng trang
export function NightAuditBanner() {
  const { session } = useAuth();
  const duocXem = coQuyen(session?.vaiTro, VAI_TRO.thuNgan);
  const { data } = useQuery({ queryKey: ['night-audit', 'status'], queryFn: api.nightAuditStatus, enabled: duocXem, staleTime: 60_000 });
  if (!duocXem || !data || !data.SoNgayChuaChot) return null;

  return (
    <div className="banner-chot-ngay" role="status">
      <span>
        <strong>{data.SoNgayChuaChot} ngày chưa chốt</strong> — cần chốt ngày {ngay(data.NgayCanChot)}
        {data.SoNgayChuaChot > 1 ? ` (tới ${ngay(addDays(homNay(), -1))})` : ''}.{' '}
        {data.NgayChotGanNhat ? `Lần chốt gần nhất: ${ngay(data.NgayChotGanNhat)}.` : 'Chưa chốt ngày nào.'}
      </span>
      <Link to="/nhan-vien/chot-ngay" className="btn btn-sm btn-gold">Chốt ngày →</Link>
    </div>
  );
}
