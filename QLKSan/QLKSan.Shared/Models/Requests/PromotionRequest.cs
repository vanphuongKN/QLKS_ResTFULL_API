using System;
using System.ComponentModel.DataAnnotations;
using QLKSan.Validation;

namespace QLKSan.Models.Requests
{
    public class PromotionRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
        [StringLength(10)]
        public string MaKhuyenMai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khuyến mãi.")]
        [StringLength(100)]
        public string TenKhuyenMai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm.")]
        [StringLength(20)]
        public string LoaiGiam { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm không được âm")]
        public decimal GiaTriGiam { get; set; }

        [DataType(DataType.Date)]
        public DateTime NgayBatDau { get; set; }

        [DataType(DataType.Date)]
        [DateGreaterThan("NgayBatDau", ErrorMessage = "Ngày kết thúc phải sau ngày bắt đầu")]
        public DateTime NgayKetThuc { get; set; }

        // Không có TrangThai: cột này do CSDL tự tính theo ngày bắt đầu/kết thúc
    }
}
