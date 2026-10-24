using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface IHousekeepingService
    {
        Task<IEnumerable<Phong>> GetRoomsNeedingCleaningAsync();

        // Lễ tân xác nhận phòng đã dọn (Chờ dọn dẹp → Trống), đóng công việc đang mở của phòng
        Task MarkRoomAsCleanAsync(string maPhong);
        Task<int> GetCountByStatusAsync(Enums.RoomStatus status);

        // Nhân viên buồng phòng đang làm việc (vai trò Buồng phòng hoặc chức vụ buồng phòng)
        Task<IEnumerable<NhanVien>> GetHousekeepingStaffAsync();

        // ===== Công việc dọn phòng (ném NotFoundException/BusinessException khi không hợp lệ) =====
        Task<IEnumerable<PhanCongDonPhong>> GetOpenTasksAsync();
        Task<IEnumerable<PhanCongDonPhong>> GetTasksOfStaffAsync(string maNhanVien);
        Task<PhanCongDonPhong> AssignTaskAsync(string maPhanCong, string maNhanVien);

        // laQuanLy = true: Admin/Lễ tân được thao tác trên công việc của người khác
        Task<PhanCongDonPhong> StartTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy);
        Task<PhanCongDonPhong> CompleteTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy);
        Task<PhanCongDonPhong> ReportIssueAsync(string maPhanCong, string ghiChu, string maNhanVienThucHien, bool laQuanLy);
    }
}
