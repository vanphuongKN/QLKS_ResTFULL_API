using System;
using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string MatKhau { get; set; }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50)]
        public string Ho { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50)]
        public string Ten { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09).")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập CCCD/Passport.")]
        [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/Passport phải bao gồm 9 đến 12 chữ số.")]
        public string CCCD_Passport { get; set; }

        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [EmailAddress(ErrorMessage = "Email sai định dạng.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(255)]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string MatKhau { get; set; }

        [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhau { get; set; }
    }

    // POST /api/auth/change-password: người dùng đang đăng nhập tự đổi mật khẩu
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        public string MatKhauMoi { get; set; }

        [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhauMoi { get; set; }
    }

    // PUT /api/customers/me/contact: khách tự cập nhật liên hệ (họ tên, CCCD do lễ tân quản lý)
    public class UpdateContactRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09).")]
        public string SDT { get; set; }

        [EmailAddress(ErrorMessage = "Email sai định dạng.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(255)]
        public string DiaChi { get; set; }

        [StringLength(50)]
        public string QuocGia { get; set; }

        // Sở thích lưu trú (gối, tầng, dị ứng…); null = giữ nguyên
        [StringLength(500, ErrorMessage = "Sở thích tối đa 500 ký tự.")]
        public string SoThich { get; set; }
    }

    // PUT /api/customers/{id}: lễ tân / quản lý cập nhật hồ sơ khách (gồm VIP, sở thích, ghi chú nội bộ, hạn chế)
    public class UpdateCustomerProfileRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50)]
        public string Ho { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50)]
        public string Ten { get; set; }

        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập CCCD/Passport.")]
        [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/Passport phải gồm 9 đến 12 chữ số.")]
        public string CCCD_Passport { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09).")]
        public string SDT { get; set; }

        [EmailAddress(ErrorMessage = "Email sai định dạng.")]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(255)]
        public string DiaChi { get; set; }

        [StringLength(50)]
        public string QuocGia { get; set; }

        public bool LaVIP { get; set; }

        [StringLength(500, ErrorMessage = "Sở thích tối đa 500 ký tự.")]
        public string SoThich { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú nội bộ tối đa 1000 ký tự.")]
        public string GhiChuNoiBo { get; set; }

        public bool HanChe { get; set; }

        [StringLength(255)]
        public string LyDoHanChe { get; set; }
    }

    // POST /api/customers/{id}/point-adjustments: quản lý cộng / trừ điểm thủ công (bù đắp, sửa sai) kèm lý do
    public class PointAdjustmentRequest
    {
        [Range(-1000000, 1000000, ErrorMessage = "Số điểm điều chỉnh không hợp lệ.")]
        public int SoDiem { get; set; }

        [Required(ErrorMessage = "Vui lòng ghi lý do điều chỉnh.")]
        [StringLength(255)]
        public string LyDo { get; set; }
    }
}
