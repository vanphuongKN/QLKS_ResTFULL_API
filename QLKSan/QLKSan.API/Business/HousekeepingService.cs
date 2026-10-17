using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class HousekeepingService : IHousekeepingService
    {
        private readonly ApplicationDbContext _context;

        public HousekeepingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Phong>> GetRoomsNeedingCleaningAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                .Where(p => p.TrangThai == RoomStatus.Cleaning)
                .ToListAsync();
        }

        public async Task<IEnumerable<Phong>> GetAllRoomsWithStatusAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<bool> MarkRoomAsCleanAsync(string maPhong)
        {
            var room = await _context.Phongs.FindAsync(maPhong);
            if (room == null || room.TrangThai != RoomStatus.Cleaning)
                return false;

            room.TrangThai = RoomStatus.Available;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCountByStatusAsync(RoomStatus status)
        {
            return await _context.Phongs.CountAsync(p => p.TrangThai == status);
        }

        public async Task<Phong> GetAttentionRoomAsync()
        {
            // Find a room in cleaning status that is VIP or Suite first
            var room = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                .FirstOrDefaultAsync(p => p.TrangThai == RoomStatus.Cleaning && 
                    (p.LoaiPhong.TenLoai.Contains("Suite") || p.LoaiPhong.TenLoai.Contains("President") || p.LoaiPhong.TenLoai.Contains("VIP")));

            if (room == null)
            {
                // Fallback to any room needing cleaning
                room = await _context.Phongs
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                    .FirstOrDefaultAsync(p => p.TrangThai == RoomStatus.Cleaning);
            }
            return room;
        }

        public async Task<int> GetVipCountAsync()
        {
            // Count premium/Suite room bookings that are approved or checked-in
            return await _context.PhieuDatPhongs
                .CountAsync(p => (p.TrangThai == BookingStatus.Approved || p.TrangThai == BookingStatus.CheckedIn) && 
                    p.ChiTietDatPhongs.Any(c => c.Phong.LoaiPhong.TenLoai.Contains("Suite") || c.Phong.LoaiPhong.TenLoai.Contains("President") || c.Phong.LoaiPhong.TenLoai.Contains("VIP")));
        }

        public async Task<IEnumerable<NhanVien>> GetHousekeepingStaffAsync()
        {
            var staff = await _context.NhanViens.ToListAsync();
            var hkStaff = staff.Where(s => s.ChucVu.Contains("Dọn") || s.ChucVu.Contains("Buồng") || s.ChucVu.Contains("Phòng") || s.ChucVu.Contains("Vệ sinh")).ToList();
            if (!hkStaff.Any())
            {
                hkStaff = staff.Take(3).ToList();
            }
            return hkStaff;
        }
    }
}
