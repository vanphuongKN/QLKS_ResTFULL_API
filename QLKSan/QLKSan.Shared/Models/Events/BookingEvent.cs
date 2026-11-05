using System;

namespace QLKSan.Models.Events
{
    // Tên exchange / hàng đợi RabbitMQ dùng chung giữa API lõi (phát) và các service nhận
    public static class MessagingNames
    {
        public const string Exchange = "qlksan.su-kien";
        public const string HangDoiThongBao = "qlksan.thong-bao";
        public const string KhoaDatPhong = "booking.*";
    }

    // Loại sự kiện = routing key
    public static class BookingEventTypes
    {
        public const string DatPhongMoi = "booking.created";
        public const string HuyPhieu = "booking.cancelled";
        public const string NhanPhong = "booking.checked-in";
    }

    // Sự kiện phiếu đặt phòng: mang sẵn thông tin để service nhận không phải gọi lại API lõi
    public class BookingEvent
    {
        public Guid MaSuKien { get; set; }
        public string Loai { get; set; }
        public DateTime ThoiDiem { get; set; }
        public string MaDatPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public string Email { get; set; }
        public string TenLoaiPhong { get; set; }
        public string SoPhong { get; set; }
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public int SoLuongKhach { get; set; }
        public decimal TienCoc { get; set; }
        public string NguonDat { get; set; }

        // Lý do huỷ (huỷ phiếu, khách không đến khi chốt ngày)
        public string LyDo { get; set; }
    }
}
