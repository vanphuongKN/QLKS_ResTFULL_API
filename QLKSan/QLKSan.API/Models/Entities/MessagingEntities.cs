using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Entities
{
    // Outbox: sự kiện ghi cùng giao dịch với nghiệp vụ, tác vụ nền đẩy lên RabbitMQ sau
    // (broker tạm dừng không làm mất sự kiện, cũng không làm hỏng thao tác của người dùng)
    public class SuKienCho
    {
        [Key]
        public Guid MaSuKien { get; set; }

        // Routing key, vd booking.created
        [StringLength(50)]
        public string Loai { get; set; }

        // Nội dung sự kiện (JSON)
        public string NoiDung { get; set; }

        public DateTime ThoiGianTao { get; set; } = DateTime.Now;
        public DateTime? ThoiGianGui { get; set; }
        public int SoLanThu { get; set; }

        [StringLength(500)]
        public string? LoiCuoi { get; set; }
    }
}
