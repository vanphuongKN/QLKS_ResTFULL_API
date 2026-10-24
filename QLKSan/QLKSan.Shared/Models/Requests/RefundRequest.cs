using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // Kế toán hoàn tiền cho hoá đơn đã thanh toán
    public class RefundRequest
    {
        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền hoàn phải từ 1.000đ trở lên.")]
        public decimal SoTien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức hoàn tiền.")]
        [StringLength(50)]
        public string PhuongThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do hoàn tiền.")]
        [StringLength(200)]
        public string LyDo { get; set; }
    }
}
