using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    public class ReviewRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        public string MaPhong { get; set; }

        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
        public int SoSao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        [StringLength(1000)]
        public string NoiDung { get; set; }

        public bool AnDanh { get; set; }
    }
}
