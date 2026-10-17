using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var room = await _context.Phongs.FindAsync(maPhong);
            if (room != null)
            {
                room.TrangThai = status;
                await _context.SaveChangesAsync();
            }
        }
    }
}
