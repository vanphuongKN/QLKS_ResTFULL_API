using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.DTOs;
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

        // Giao nhiều việc cho một nhân viên (vd: cả tầng); trả về số việc đã giao
        Task<int> AssignManyAsync(IEnumerable<string> maPhanCongs, string maNhanVien);

        // Việc kèm mức ưu tiên (khách VIP / khách đến hôm nay / yêu cầu của khách), đã sắp theo ưu tiên
        Task<List<HousekeepingTaskDto>> PrioritizeAsync(IEnumerable<PhanCongDonPhong> tasks);

        // Bảng buồng phòng: mọi phòng theo tầng, việc đang mở, khách đến / đi hôm nay
        Task<List<HousekeepingRoomDto>> GetBoardAsync();

        // laQuanLy = true: Admin/Lễ tân được thao tác trên công việc của người khác
        Task<PhanCongDonPhong> StartTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy);
        Task<PhanCongDonPhong> CompleteTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy);
        Task<PhanCongDonPhong> ReportIssueAsync(string maPhanCong, string ghiChu, string maNhanVienThucHien, bool laQuanLy);
    }
}
