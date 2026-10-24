using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // Lập hoá đơn cho phiếu đặt phòng (có thể kèm mã khuyến mãi)
    public class GenerateInvoiceRequest
    {
        [Required]
        public string MaDatPhong { get; set; }

        [StringLength(10)]
        public string MaKhuyenMai { get; set; }
    }

    public class PaymentRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [StringLength(50)]
        public string PhuongThuc { get; set; }

        // Số điểm khách đổi trừ vào hoá đơn (0 = không đổi)
        [Range(0, int.MaxValue, ErrorMessage = "Số điểm đổi không hợp lệ.")]
        public int DiemSuDung { get; set; }
    }
}
