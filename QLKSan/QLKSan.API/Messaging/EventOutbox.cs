using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Data;
using QLKSan.Models.Entities;
using QLKSan.Models.Events;

namespace QLKSan.Messaging
{
    // Ghi sự kiện vào outbox trong DbContext hiện tại; người gọi SaveChanges cùng thay đổi nghiệp vụ
    public interface IEventOutbox
    {
        // Phiếu phải đã có trong CSDL (gọi sau lần SaveChanges tạo phiếu, trước khi commit)
        Task AddBookingEventAsync(string loai, string maDatPhong, string lyDo = null);
    }

    public class EventOutbox : IEventOutbox
    {
        private readonly ApplicationDbContext _context;
        private readonly MessagingOptions _options;

        public EventOutbox(ApplicationDbContext context, IOptions<MessagingOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public async Task AddBookingEventAsync(string loai, string maDatPhong, string lyDo = null)
        {
            if (!_options.Enabled) return;

            // Phiếu đang được theo dõi trả về với giá trị hiện tại (kể cả thay đổi chưa lưu)
            var p = await _context.PhieuDatPhongs
                .Include(x => x.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(x => x.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .FirstAsync(x => x.MaDatPhong == maDatPhong);
            var phong = p.ChiTietDatPhongs.FirstOrDefault()?.Phong;
            var su = new BookingEvent
            {
                MaSuKien = Guid.NewGuid(),
                Loai = loai,
                ThoiDiem = DateTime.Now,
                MaDatPhong = p.MaDatPhong,
                HoTenKhachHang = p.KhachHang == null ? "" : $"{p.KhachHang.Ho} {p.KhachHang.Ten}".Trim(),
                Email = p.KhachHang?.LienHeDiaChis?.Select(l => l.Email).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
                TenLoaiPhong = phong?.LoaiPhong?.TenLoai,
                SoPhong = phong?.SoPhong,
                NgayNhan = p.NgayCheckInDuKien.Date,
                NgayTra = p.NgayCheckOutDuKien.Date,
                SoLuongKhach = p.SoLuongKhach,
                TienCoc = p.TienCoc,
                NguonDat = p.NguonDat,
                LyDo = lyDo
            };
            _context.SuKienChos.Add(new SuKienCho
            {
                MaSuKien = su.MaSuKien,
                Loai = loai,
                NoiDung = JsonSerializer.Serialize(su),
                ThoiGianTao = su.ThoiDiem
            });
        }
    }
}
