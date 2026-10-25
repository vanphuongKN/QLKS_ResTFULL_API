using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Models.Entities;
using QLKSan.Enums;

namespace QLKSan.Business
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;

        public RoomService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Phong>> GetAllRoomsAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.HinhAnhs)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .ToListAsync();
        }

        public async Task<Phong> GetRoomByIdAsync(string maPhong)
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.PhongTienIchs).ThenInclude(pti => pti.TienIch)
                .Include(p => p.HinhAnhs)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
        }

        public async Task UpdateRoomStatusAsync(string maPhong, RoomStatus status)
        {
            var room = await _context.Phongs.FindAsync(maPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{maPhong}'.");

            // Các trạng thái gắn với khách lưu trú chỉ được đổi qua nghiệp vụ check-in/đặt phòng, không đổi tay
            if (status == RoomStatus.Booked)
                throw new BusinessException("Trạng thái 'Đã đặt' được hệ thống tự tính từ phiếu đặt phòng, không đặt thủ công.");
            if (status == RoomStatus.Occupied)
                throw new BusinessException("Phòng chỉ chuyển sang 'Đang ở' khi lễ tân check-in cho khách.");

            bool hasGuest = await _context.ChiTietDatPhongs
                .AnyAsync(c => c.MaPhong == maPhong && c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn);
            if (hasGuest)
                throw new ConflictException("Phòng đang có khách lưu trú, không thể đổi trạng thái thủ công.");

            var trangThaiCu = room.TrangThai;
            room.TrangThai = status;

            // Đồng bộ công việc buồng phòng với trạng thái mới
            if (trangThaiCu == RoomStatus.Maintenance && status != RoomStatus.Maintenance)
                await HousekeepingTasks.ResolveIssuesAsync(_context, maPhong);
            if (status == RoomStatus.Cleaning)
                await HousekeepingTasks.EnsureOpenTaskAsync(_context, maPhong, null, HousekeepingTasks.DonTheoYeuCau);
            else
                await HousekeepingTasks.CloseOpenTasksAsync(_context, maPhong, $"Phòng đổi sang trạng thái khác thủ công {DateTime.Now:dd/MM HH:mm}");

            await _context.SaveChangesAsync();
        }
    }
}
