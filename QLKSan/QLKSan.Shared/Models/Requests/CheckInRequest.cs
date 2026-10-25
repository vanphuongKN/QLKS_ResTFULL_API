using System;
using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    public class CheckInRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50)]
        public string Ho { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50)]
        public string Ten { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(10)]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09)")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập CCCD/Passport.")]
        [StringLength(12, MinimumLength = 9)]
        [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/Passport phải bao gồm 9 đến 12 chữ số")]
        public string CCCD_Passport { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime NgayCheckIn { get; set; } = DateTime.Now;

        [Required]
        [DataType(DataType.Date)]
        public DateTime NgayCheckOut { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        public string MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng khách.")]
        [Range(1, 10, ErrorMessage = "Số lượng khách phải từ 1 đến 10.")]
        public int SoNguoi { get; set; } = 1;

        // Phương thức thu tiền cọc (cọc tính tự động theo chính sách)
        [StringLength(50)]
        public string PhuongThucCoc { get; set; } = "Tiền mặt";
    }
}
