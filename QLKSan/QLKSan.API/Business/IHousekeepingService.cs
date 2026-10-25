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
        // Phòng trống vừa dọn: chuyển "Chờ kiểm tra" (trừ khi người bấm có quyền kiểm tra — coi như đã kiểm tra đạt)
        Task<PhanCongDonPhong> CompleteTaskAsync(string maPhanCong, string maNhanVienThucHien, bool laQuanLy, bool laNguoiKiemTra = false);

        // Kiểm tra phòng: đạt → hoàn thành, phòng sẵn sàng; chưa đạt → trả lại nhân viên dọn lại (ghi mục chưa đạt)
        Task<PhanCongDonPhong> InspectAsync(string maPhanCong, bool dat, IList<string> mucChuaDat, string ghiChu, string maNguoiKiemTra);

        // Ghi đồ minibar khách đã dùng vào bảng kê của phiếu đang ở tại phòng; trả về tổng tiền vừa ghi
        Task<decimal> PostMinibarAsync(string maPhong, IList<Models.Requests.ServiceOrderItem> items, string nguoiGhi);
        Task<PhanCongDonPhong> ReportIssueAsync(string maPhanCong, string ghiChu, string maNhanVienThucHien, bool laQuanLy);
    }
}
