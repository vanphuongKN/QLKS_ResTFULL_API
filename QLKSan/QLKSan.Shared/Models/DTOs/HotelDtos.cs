using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    // GET /api/hotel: thông tin liên hệ + chính sách lưu trú hiển thị cho khách (footer, trang đặt phòng)
    public class HotelInfoDto
    {
        public string Ten { get; set; }
        public string MoTaNgan { get; set; }
        public string DiaChi { get; set; }
        public string Hotline { get; set; }
        public string Email { get; set; }
        public string BanDoUrl { get; set; }

        // "14:00" / "12:00"
        public string GioNhanPhong { get; set; }
        public string GioTraPhong { get; set; }

        // Mốc phụ thu trả phòng muộn; sau mốc cuối cùng tính thêm 1 đêm
        public List<LateCheckoutFeeDto> PhuThuTraMuon { get; set; } = new List<LateCheckoutFeeDto>();

        // Mô tả chính sách huỷ đúng với quy tắc nghiệp vụ của API
        public string ChinhSachHuy { get; set; }

        // Chương trình thành viên: cách tích / đổi điểm và quyền lợi từng hạng
        public LoyaltyProgramDto ThanhVien { get; set; }
    }

    public class LoyaltyProgramDto
    {
        public int SoTienMotDiem { get; set; }
        public int GiaTriMotDiem { get; set; }
        public int DiemDoiToiThieu { get; set; }
        public List<MembershipTierDto> Hangs { get; set; } = new List<MembershipTierDto>();
    }

    public class MembershipTierDto
    {
        public string Ten { get; set; }
        public int DiemToiThieu { get; set; }
        public int PhanTramGiamPhong { get; set; }
        public List<string> QuyenLoi { get; set; } = new List<string>();
    }

    public class LateCheckoutFeeDto
    {
        // Trả phòng đến (bao gồm) giờ này: phụ thu PhanTram % giá 1 đêm
        public string DenGio { get; set; }
        public int PhanTram { get; set; }
    }

    // GET /api/settings: cấu hình nghiệp vụ quản lý sửa được (các PUT dùng lại từng phần làm body)
    public class HotelSettingsDto
    {
        public HotelInfoSettingsDto ThongTin { get; set; } = new HotelInfoSettingsDto();
        public HotelPolicySettingsDto ChinhSach { get; set; } = new HotelPolicySettingsDto();
        public LoyaltySettingsDto ThanhVien { get; set; } = new LoyaltySettingsDto();

        // true = đang dùng giá trị quản lý đã lưu; false = giá trị mặc định trong appsettings.json
        public bool ThongTinDaTuyChinh { get; set; }
        public bool ChinhSachDaTuyChinh { get; set; }
        public bool ThanhVienDaTuyChinh { get; set; }
        public DateTime? CapNhatLuc { get; set; }
    }

    public class HotelInfoSettingsDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khách sạn.")]
        [StringLength(100)]
        public string Ten { get; set; }

        [StringLength(300)]
        public string MoTaNgan { get; set; }

        [StringLength(255)]
        public string DiaChi { get; set; }

        [StringLength(30)]
        public string Hotline { get; set; }

        [EmailAddress(ErrorMessage = "Email sai định dạng.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Url(ErrorMessage = "Liên kết bản đồ phải là địa chỉ web (http/https).")]
        [StringLength(500)]
        public string BanDoUrl { get; set; }
    }

    public class HotelPolicySettingsDto
    {
        [Range(0, 23, ErrorMessage = "Giờ nhận phòng từ 0 đến 23.")]
        public int GioNhanPhong { get; set; }

        [Range(0, 23, ErrorMessage = "Giờ trả phòng từ 0 đến 23.")]
        public int GioTraPhong { get; set; }

        // Trả phòng đến (bao gồm) DenGio giờ: phụ thu PhanTram % giá 1 đêm; sau mốc cuối tính thêm 1 đêm
        public List<LateCheckoutRuleDto> PhuThuTraMuon { get; set; } = new List<LateCheckoutRuleDto>();

        public bool TuDongXacNhanDatPhong { get; set; }

        [Range(0, 100, ErrorMessage = "Tiền cọc từ 0 đến 100%.")]
        public int PhanTramTienCocVangLai { get; set; }

        public bool TuDongHuyKhachKhongDen { get; set; }

        [Range(5, 1440, ErrorMessage = "Chu kỳ kiểm tra từ 5 đến 1440 phút.")]
        public int ChuKyKiemTraKhongDenPhut { get; set; }
    }

    public class LateCheckoutRuleDto
    {
        public int DenGio { get; set; }
        public int PhanTram { get; set; }
    }

    public class LoyaltySettingsDto
    {
        [Range(1000, 100000000, ErrorMessage = "Số tiền cho 1 điểm phải từ 1.000đ.")]
        public int SoTienMotDiem { get; set; }

        [Range(1, 1000000, ErrorMessage = "Giá trị 1 điểm phải từ 1đ.")]
        public int GiaTriMotDiem { get; set; }

        [Range(1, 1000000, ErrorMessage = "Số điểm đổi tối thiểu phải từ 1.")]
        public int DiemDoiToiThieu { get; set; }

        public List<MembershipTierDto> Hangs { get; set; } = new List<MembershipTierDto>();
    }
}
