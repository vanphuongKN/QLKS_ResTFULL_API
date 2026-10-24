using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // Đánh giá một lần lưu trú: gửi MaDatPhong (cách mới). Chỉ gửi MaPhong thì hệ thống gắn vào lần ở gần nhất chưa đánh giá
    public class ReviewRequest
    {
        [StringLength(20)]
        public string MaDatPhong { get; set; }

        [StringLength(10)]
        public string MaPhong { get; set; }

        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
        public int SoSao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        [StringLength(1000)]
        public string NoiDung { get; set; }

        public bool AnDanh { get; set; }
    }
}
