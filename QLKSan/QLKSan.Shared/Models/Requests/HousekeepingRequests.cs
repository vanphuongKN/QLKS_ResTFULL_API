using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
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
