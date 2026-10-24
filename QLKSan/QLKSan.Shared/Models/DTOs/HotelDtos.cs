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
}
