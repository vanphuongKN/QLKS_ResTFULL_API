using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
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
