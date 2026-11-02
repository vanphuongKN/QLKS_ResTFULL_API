import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { hkApi, TRANG_THAI_VIEC, type HousekeepingTask } from '../../api/staff';
import { useThaoTac } from '../../api/query';
import { useAuth } from '../../auth/AuthContext';
import { ErrorBox, Loading } from '../../components/common';
import { useXacNhan } from '../../components/ui/Dialog';
import { EmptyState, PageHeader } from '../../components/ui/Page';
import { ngay } from '../../utils/format';
import './hk.css';

const gio = (v?: string | null) => (v ? v.slice(11, 16) : '—');
const gioNgay = (v?: string | null) => (v ? `${v.slice(11, 16)} ${ngay(v).slice(0, 5)}` : '—');
const NHAN_DANG_LAM: Partial<Record<string, [string, string]>> = {
  Assigned: ['badge-info', 'Chưa bắt đầu'], InProgress: ['badge-warn', 'Đang dọn'], Issue: ['badge-danger', 'Đã báo sự cố']
};

// Việc của tôi (nhân viên buồng phòng), như Housekeeping/MyTasks của MVC
export function MyTasksPage() {
  const { session } = useAuth();
  const xacNhan = useXacNhan();
  const q = useQuery({ queryKey: ['hk', 'mine'], queryFn: hkApi.myTasks });
  const lamMoi = [['hk']];
  const batDau = useThaoTac({ mutationFn: hkApi.start, lamMoi, thanhCong: 'Đã bắt đầu dọn phòng.', tienToLoi: 'Không thể bắt đầu. ' });
  const xong = useThaoTac({ mutationFn: hkApi.complete, lamMoi, thanhCong: 'Đã báo dọn xong, chờ kiểm tra phòng.', tienToLoi: 'Không thể hoàn thành. ' });
  const dnd = useThaoTac({ mutationFn: hkApi.skipDnd, lamMoi, thanhCong: 'Đã ghi khách không làm phiền, bỏ qua việc hôm nay.', tienToLoi: 'Không thể cập nhật. ' });
  const suCo = useThaoTac({
    mutationFn: (v: { id: string; ghiChu: string }) => hkApi.reportIssue(v.id, v.ghiChu), lamMoi,
    thanhCong: 'Đã báo sự cố, mở phiếu kỹ thuật và chuyển phòng sang bảo trì.', tienToLoi: 'Không thể báo sự cố. '
  });
  const dangBan = batDau.isPending || xong.isPending || dnd.isPending || suCo.isPending;

  if (q.isLoading) return <Loading />;
  const ds = q.data ?? [];
  const daXong = ds.filter((t) => t.TrangThai === 'Completed' || t.TrangThai === 'AwaitingInspection');
  const dangLam = ds.filter((t) => !daXong.includes(t));

  const baoKhongLamPhien = async (t: HousekeepingTask) => {
    if (await xacNhan({ tieuDe: `Phòng ${t.SoPhong} treo biển Không làm phiền?`, noiDung: 'Việc hôm nay của phòng này sẽ được bỏ qua.', nutXacNhan: 'Bỏ qua việc hôm nay' })) dnd.mutate(t.MaPhanCong!);
  };
  const baoSuCo = async (t: HousekeepingTask) => {
    const kq = await xacNhan({ tieuDe: `Báo sự cố phòng ${t.SoPhong}`, nhanLyDo: 'Mô tả sự cố (vd: vòi sen rò nước, điều hoà không lạnh)', lyDoBatBuoc: true, nutXacNhan: 'Báo sự cố', nguyHiem: true });
    if (kq) suCo.mutate({ id: t.MaPhanCong!, ghiChu: kq.lyDo });
  };

  return (
    <>
      <PageHeader tieuDe="Việc của tôi hôm nay" phu={<><strong>{session?.hoTen}</strong> · {dangLam.length} việc cần làm · {daXong.length} việc đã xong hôm nay</>}>
        <Link to="/nhan-vien/buong-phong/minibar" className="btn btn-outline btn-sm">Ghi minibar</Link>
        <Link to="/nhan-vien/ky-thuat" className="btn btn-outline btn-sm">Kỹ thuật</Link>
      </PageHeader>
      <ErrorBox message={q.error?.message} />

      {dangLam.length === 0 ? (
        <EmptyState>Chưa có việc nào được giao. Lễ tân sẽ giao việc khi có phòng cần dọn.</EmptyState>
      ) : (
        <div className="viec-luoi">
          {dangLam.map((t) => {
            const [css, nhan] = NHAN_DANG_LAM[t.TrangThai ?? ''] ?? ['badge-muted', TRANG_THAI_VIEC[t.TrangThai!]];
            const coTheLam = t.TrangThai === 'Assigned' || t.TrangThai === 'InProgress';
            return (
              <article key={t.MaPhanCong} className={`viec${t.UuTien === 1 ? ' viec-gap' : t.UuTien === 2 ? ' viec-cao' : ''}`} aria-label={`Phòng ${t.SoPhong}`}>
                <div className="viec-dau">
                  <span className={`badge ${css}`}>{nhan}</span>
                  {(t.UuTien ?? 3) <= 2 && <span className={`uu-tien uu-tien-${t.UuTien}`}>{t.UuTien === 1 ? 'Gấp' : 'Ưu tiên'}</span>}
                </div>
                <h2>Phòng {t.SoPhong} <small>· tầng {t.Tang}</small></h2>
                <p className="viec-hang">{t.TenLoaiPhong}</p>
                {(t.SoLanKhongDat ?? 0) > 0 && t.KetQuaKiemTra && <p className="chu-loi small">Dọn lại — {t.KetQuaKiemTra}</p>}
                {t.LyDoUuTien && <p className={`small ${t.UuTien === 1 ? 'chu-loi' : ''}`}><strong>{t.LyDoUuTien}</strong></p>}
                <p className="small muted">{t.LoaiCongViec}</p>
                {t.GioDonMongMuon && <p className="small"><strong>Khách muốn dọn: {t.GioDonMongMuon}</strong></p>}
                <p className="small muted">Giao lúc {gioNgay(t.ThoiGianGiao)}{t.ThoiGianBatDau && ` · bắt đầu ${gio(t.ThoiGianBatDau)}`}</p>
                {t.GhiChu && <p className={`small ${t.TrangThai === 'Issue' ? 'chu-loi' : ''}`}>{t.GhiChu}</p>}
                <div className="viec-nut">
                  {t.TrangThai === 'Assigned' && <button type="button" className="btn btn-outline btn-sm" disabled={dangBan} onClick={() => batDau.mutate(t.MaPhanCong!)}>Bắt đầu</button>}
                  {coTheLam && <button type="button" className="btn btn-gold btn-sm" disabled={dangBan} onClick={() => xong.mutate(t.MaPhanCong!)}>Dọn xong</button>}
                  {coTheLam && (t.LoaiCongViec === 'Dọn hằng ngày' || t.LoaiCongViec === 'Chỉnh trang buổi tối') &&
                    <button type="button" className="btn btn-outline btn-sm w-100" disabled={dangBan} onClick={() => baoKhongLamPhien(t)}>Khách treo biển Không làm phiền</button>}
                  {coTheLam && <button type="button" className="btn btn-outline-danger btn-sm w-100" disabled={dangBan} onClick={() => baoSuCo(t)}>Báo sự cố</button>}
                </div>
              </article>
            );
          })}
        </div>
      )}

      {daXong.length > 0 && (
        <section className="the-trang" aria-labelledby="daXongTieuDe">
          <h2 id="daXongTieuDe">Đã hoàn thành hôm nay</h2>
          <div className="bang-cuon">
            <table className="bang">
              <thead><tr><th>Phòng</th><th>Công việc</th><th>Bắt đầu</th><th>Hoàn thành</th><th>Kiểm tra</th></tr></thead>
              <tbody>
                {daXong.map((t) => (
                  <tr key={t.MaPhanCong}>
                    <td>Phòng {t.SoPhong}</td>
                    <td>{t.LoaiCongViec}</td>
                    <td>{gio(t.ThoiGianBatDau)}</td>
                    <td>{gio(t.ThoiGianHoanThanh)}</td>
                    <td>{t.TrangThai === 'AwaitingInspection' ? <span className="badge badge-info">Chờ kiểm tra</span>
                      : t.KetQuaKiemTra ? <span className="badge badge-ok" title={t.TenNguoiKiemTra ?? ''}>{t.KetQuaKiemTra}</span> : <span className="muted">—</span>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </>
  );
}
