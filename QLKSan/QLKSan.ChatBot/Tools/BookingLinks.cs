using Microsoft.Extensions.Options;
using QLKSan.ChatBot.Common;
using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Tools
{
    // Liên kết tới trang Web: bot không tự đặt phòng, chỉ đưa khách tới trang đặt phòng đã điền sẵn để khách tự xác nhận
    public class BookingLinks
    {
        private readonly string _web;

        public BookingLinks(IOptions<ChatBotOptions> options)
        {
            _web = options.Value.WebBaseUrl.TrimEnd('/');
        }

        public ChatLinkDto DatPhong(string maLoai, string tenLoai, DateTime checkIn, DateTime checkOut, int soNguoi) => new ChatLinkDto
        {
            Nhan = $"Đặt {tenLoai}",
            Url = $"{_web}/Customer/BookRoom?maLoai={Uri.EscapeDataString(maLoai)}&checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}&soNguoi={soNguoi}"
        };

        public ChatLinkDto HangPhong(string maLoai, string tenLoai) => new ChatLinkDto
        {
            Nhan = $"Xem {tenLoai}",
            Url = $"{_web}/Home/RoomType/{Uri.EscapeDataString(maLoai)}"
        };
    }
}
