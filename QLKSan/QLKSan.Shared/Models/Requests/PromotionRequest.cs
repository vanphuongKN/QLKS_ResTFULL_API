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

        // Điều kiện áp dụng, để trống = không giới hạn
        [Range(1, 100000, ErrorMessage = "Số lượt tối đa phải từ 1 trở lên.")]
        public int? SoLuotToiDa { get; set; }

        [Range(1, 60, ErrorMessage = "Số đêm tối thiểu phải từ 1 đến 60.")]
        public int? SoDemToiThieu { get; set; }

        [StringLength(10)]
        public string MaLoai { get; set; }

        // Không có TrangThai: cột này do CSDL tự tính theo ngày bắt đầu/kết thúc
    }

    // PATCH /api/promotions/{id}: bật / tắt mã { "KichHoat": true | false }
    public class UpdatePromotionStatusRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public bool? KichHoat { get; set; }
    }
}
