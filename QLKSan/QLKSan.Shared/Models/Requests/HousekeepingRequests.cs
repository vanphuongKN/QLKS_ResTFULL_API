using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // PUT /api/housekeeping/stays/{maDatPhong}/preferences: dặn dò buồng phòng cho khách đang ở
    public class HousekeepingPreferencesRequest
    {
        public bool KhongLamPhienHomNay { get; set; }

        [StringLength(30)]
        public string GioDonMongMuon { get; set; }

        public bool TuChoiDonHangNgay { get; set; }
    }

    // POST /api/housekeeping/tasks/{id}/inspect: kiểm tra phòng sau khi dọn
    public class InspectTaskRequest
    {
        public bool Dat { get; set; }

        // Các mục chưa đạt trong danh mục kiểm tra (bắt buộc có mục hoặc ghi chú khi chưa đạt)
        public System.Collections.Generic.List<string> MucChuaDat { get; set; } = new System.Collections.Generic.List<string>();

        [StringLength(300)]
        public string GhiChu { get; set; }
    }

    // POST /api/housekeeping/rooms/{maPhong}/minibar: ghi đồ minibar khách đã dùng vào bảng kê
    public class MinibarRequest
    {
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một món.")]
        public System.Collections.Generic.List<ServiceOrderItem> Items { get; set; } = new System.Collections.Generic.List<ServiceOrderItem>();
    }

    // POST /api/maintenance: mở phiếu kỹ thuật
    public class MaintenanceCreateRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        [StringLength(10)]
        public string MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng mô tả sự cố.")]
        [StringLength(500)]
        public string MoTa { get; set; }

        // Thường | Khẩn
        [StringLength(20)]
        public string MucDo { get; set; } = "Thường";

        // Chuyển phòng sang Bảo trì tới khi sửa xong (bỏ qua nếu phòng đang có khách)
        public bool KhoaPhong { get; set; }
    }

    // PATCH /api/maintenance/{id}: đang sửa / đã xong / huỷ
    public class MaintenanceUpdateRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public Enums.GuestRequestStatus? TrangThai { get; set; }

        [StringLength(500)]
        public string KetQua { get; set; }
    }

    // POST /api/housekeeping/tasks/assign-bulk: giao nhiều việc cùng lúc (vd: cả một tầng) cho một nhân viên
    public class BulkAssignTaskRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
        public string MaNhanVien { get; set; }

        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một việc.")]
        public System.Collections.Generic.List<string> MaPhanCongs { get; set; } = new System.Collections.Generic.List<string>();
    }

    // Lễ tân giao việc dọn phòng cho nhân viên buồng phòng
    public class AssignTaskRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn nhân viên buồng phòng.")]
        public string MaNhanVien { get; set; }
    }

    // Nhân viên buồng phòng báo sự cố (phòng chuyển sang Bảo trì)
    public class ReportIssueRequest
    {
        [Required(ErrorMessage = "Vui lòng mô tả sự cố.")]
        [StringLength(500)]
        public string GhiChu { get; set; }
    }
}
