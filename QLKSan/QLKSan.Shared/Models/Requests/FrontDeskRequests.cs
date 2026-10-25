using System;
using System.ComponentModel.DataAnnotations;
using QLKSan.Validation;

namespace QLKSan.Models.Requests
{
    // POST /api/bookings/front-desk: lễ tân tạo đặt phòng cho ngày tới (khách gọi điện, gửi email, đến quầy, đại lý).
    // Khách được nhận diện theo CCCD/Passport: đã có hồ sơ thì dùng hồ sơ đó, chưa có thì tạo mới
    public class FrontDeskBookingRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50)]
        public string Ho { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50)]
        public string Ten { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09)")]
        public string SDT { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập CCCD/Passport.")]
        [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/Passport phải bao gồm 9 đến 12 chữ số")]
        public string CCCD_Passport { get; set; }

        // Chọn hạng phòng (tự xếp phòng trống) hoặc một phòng cụ thể
        [StringLength(10)]
        public string? MaLoai { get; set; }

        [StringLength(10)]
        public string? MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [FutureDate(ErrorMessage = "Ngày nhận phòng không được ở trong quá khứ")]
        public DateTime CheckIn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [DateGreaterThan("CheckIn", ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng")]
        public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 10, ErrorMessage = "Số lượng khách phải từ 1 đến 10.")]
        public int SoNguoi { get; set; } = 1;

        // Một trong BookingSources.QuaQuay
        [Required(ErrorMessage = "Vui lòng chọn nguồn đặt phòng.")]
        [StringLength(30)]
        public string NguonDat { get; set; }

        [StringLength(30)]
        public string? GioDenDuKien { get; set; }

        [StringLength(500, ErrorMessage = "Yêu cầu riêng tối đa 500 ký tự.")]
        public string? YeuCauDacBiet { get; set; }

        [StringLength(10)]
        public string? MaKhuyenMai { get; set; }

        // Tiền cọc / đảm bảo thu ngay (0 = không thu)
        [Range(0, 1_000_000_000, ErrorMessage = "Tiền cọc không hợp lệ.")]
        public decimal TienCoc { get; set; }

        [StringLength(50)]
        public string? PhuongThucCoc { get; set; }
    }

    // POST /api/bookings/{id}/check-in (body tuỳ chọn): số khách thực tế và tiền cọc thu thêm khi nhận phòng
    public class CheckInBookingRequest
    {
        [Range(1, 10, ErrorMessage = "Số lượng khách phải từ 1 đến 10.")]
        public int? SoLuongKhach { get; set; }

        [Range(0, 1_000_000_000, ErrorMessage = "Tiền cọc không hợp lệ.")]
        public decimal TienCoc { get; set; }

        [StringLength(50)]
        public string? PhuongThucCoc { get; set; }
    }

    // POST /api/bookings/{id}/deposits: thu thêm tiền cọc / đảm bảo cho phiếu chưa trả phòng
    public class DepositRequest
    {
        [Range(1000, 1_000_000_000, ErrorMessage = "Số tiền cọc phải từ 1.000đ.")]
        public decimal SoTien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức.")]
        [StringLength(50)]
        public string PhuongThuc { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}
