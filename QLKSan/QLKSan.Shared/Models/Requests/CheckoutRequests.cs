using System.Collections.Generic;
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
        // Một phương thức cho toàn bộ số tiền còn lại (dùng khi không tách thanh toán)
        [StringLength(50)]
        public string PhuongThuc { get; set; }

        // Số điểm khách đổi trừ vào hoá đơn (0 = không đổi)
        [Range(0, int.MaxValue, ErrorMessage = "Số điểm đổi không hợp lệ.")]
        public int DiemSuDung { get; set; }

        // Tách thanh toán: tối đa 3 khoản (vd: một phần tiền mặt, một phần thẻ); tổng phải bằng số tiền còn phải thu sau khi đổi điểm
        [MaxLength(3, ErrorMessage = "Tách tối đa 3 khoản thanh toán.")]
        public List<PaymentPart> CacKhoan { get; set; }
    }

    public class PaymentPart
    {
        [Required(ErrorMessage = "Vui lòng chọn phương thức cho từng khoản.")]
        [StringLength(50)]
        public string PhuongThuc { get; set; }

        [Range(1000, 10_000_000_000, ErrorMessage = "Mỗi khoản thanh toán phải từ 1.000đ.")]
        public decimal SoTien { get; set; }

        // VD: "Công ty ABC trả tiền phòng"
        [StringLength(255)]
        public string GhiChu { get; set; }
    }

    // POST /api/night-audit/close: chốt một ngày; GiuLai = phiếu chưa nhận phòng nhưng khách báo đến muộn (không huỷ)
    public class CloseDayRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày chốt.")]
        public System.DateTime? Ngay { get; set; }

        public List<string> GiuLai { get; set; } = new List<string>();

        [StringLength(500)]
        public string GhiChu { get; set; }
    }

    // POST /api/night-audit/{ngay}/reopen: quản lý mở lại ngày chốt gần nhất
    public class ReopenDayRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do mở lại.")]
        [StringLength(300)]
        public string LyDo { get; set; }
    }

    // PUT /api/invoices/{id}/e-invoice: kế toán ghi số hoá đơn điện tử đã xuất (trống = bỏ)
    public class EInvoiceRequest
    {
        [StringLength(30)]
        public string SoHoaDonDienTu { get; set; }
    }

    // PUT /api/invoices/{id}/company: thông tin xuất hoá đơn VAT cho công ty (gửi MaSoThue trống để bỏ)
    public class InvoiceCompanyRequest
    {
        [StringLength(200)]
        public string TenCongTy { get; set; }

        [RegularExpression(@"^\d{10}(-\d{3})?$", ErrorMessage = "Mã số thuế gồm 10 chữ số (chi nhánh: 10 số-3 số).")]
        public string MaSoThue { get; set; }

        [StringLength(300)]
        public string DiaChiCongTy { get; set; }

        [EmailAddress(ErrorMessage = "Email nhận hoá đơn không hợp lệ.")]
        [StringLength(100)]
        public string EmailHoaDon { get; set; }
    }
}
