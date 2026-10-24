using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using QLKSan.Validation;

namespace QLKSan.Models.Requests
{
    public class PricingRuleRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên quy tắc giá.")]
        [StringLength(100)]
        public string TenQuyTac { get; set; }

        // Để trống = mọi loại phòng
        [StringLength(10)]
        public string MaLoai { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TuNgay { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DenNgay { get; set; }

        // 0 = Chủ nhật ... 6 = Thứ bảy; để trống = mọi đêm
        public List<int> CacThu { get; set; } = new List<int>();

        [Range(-90, 300, ErrorMessage = "Mức điều chỉnh phải từ -90% đến +300%.")]
        public decimal PhanTramDieuChinh { get; set; }

        [Range(1, 100, ErrorMessage = "Độ ưu tiên phải từ 1 đến 100.")]
        public int UuTien { get; set; } = 1;
    }

    // PATCH /api/pricing-rules/{id}: bật / tắt quy tắc với giá trị tường minh
    public class UpdatePricingRuleStatusRequest
    {
        [Required(ErrorMessage = "Vui lòng cho biết bật hay tắt quy tắc.")]
        public bool? KichHoat { get; set; }
    }
}
