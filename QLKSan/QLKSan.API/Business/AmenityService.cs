using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    // Danh mục tiện nghi (Wifi, Minibar…) và gán tiện nghi cho từng phòng / cả hạng phòng
    public interface IAmenityService
    {
        Task<List<TienIch>> GetAllAsync();
        Task<TienIch> CreateAsync(AmenityRequest request);
        Task<TienIch> UpdateAsync(string maTienIch, AmenityRequest request);
        Task DeleteAsync(string maTienIch);

        // Đặt lại danh sách tiện nghi của phòng; apDungCaHang = áp cho mọi phòng cùng hạng. Trả về số phòng đã cập nhật
        Task<int> SetRoomAmenitiesAsync(string maPhong, List<string> maTienIchs, bool apDungCaHang);
    }

    public class AmenityService : IAmenityService
    {
        private readonly ApplicationDbContext _context;

        public AmenityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TienIch>> GetAllAsync()
            => await _context.TienIchs.Include(t => t.PhongTienIchs).OrderBy(t => t.TenTienIch).ToListAsync();

        public async Task<TienIch> CreateAsync(AmenityRequest request)
        {
            var ten = request.TenTienIch.Trim();
            if (await _context.TienIchs.AnyAsync(t => t.TenTienIch == ten))
                throw new ConflictException($"Tiện nghi \"{ten}\" đã có.");
            var tienIch = new TienIch
            {
                MaTienIch = await IdGenerator.NextAsync(_context.TienIchs.Select(t => t.MaTienIch), "TI", 2),
                TenTienIch = ten,
                Icon = request.Icon.Trim(),
                PhongTienIchs = new List<PhongTienIch>()
            };
            _context.TienIchs.Add(tienIch);
            await _context.SaveChangesAsync();
            return tienIch;
        }

        public async Task<TienIch> UpdateAsync(string maTienIch, AmenityRequest request)
        {
            var tienIch = await FindAsync(maTienIch);
            var ten = request.TenTienIch.Trim();
            if (await _context.TienIchs.AnyAsync(t => t.TenTienIch == ten && t.MaTienIch != maTienIch))
                throw new ConflictException($"Tiện nghi \"{ten}\" đã có.");
            tienIch.TenTienIch = ten;
            tienIch.Icon = request.Icon.Trim();
            await _context.SaveChangesAsync();
            return tienIch;
        }

        // Xoá tiện nghi gỡ luôn khỏi các phòng đang có
        public async Task DeleteAsync(string maTienIch)
        {
            var tienIch = await FindAsync(maTienIch);
            _context.PhongTienIchs.RemoveRange(tienIch.PhongTienIchs);
            _context.TienIchs.Remove(tienIch);
            await _context.SaveChangesAsync();
        }

        public async Task<int> SetRoomAmenitiesAsync(string maPhong, List<string> maTienIchs, bool apDungCaHang)
        {
            var phong = await _context.Phongs.FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{maPhong}'.");
            var ma = (maTienIchs ?? new()).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
            var hopLe = await _context.TienIchs.Where(t => ma.Contains(t.MaTienIch)).Select(t => t.MaTienIch).ToListAsync();
            var sai = ma.Except(hopLe).ToList();
            if (sai.Count > 0) throw new BusinessException($"Không tìm thấy tiện nghi: {string.Join(", ", sai)}.");

            var phongs = apDungCaHang
                ? await _context.Phongs.Where(p => p.MaLoai == phong.MaLoai).Select(p => p.MaPhong).ToListAsync()
                : new List<string> { maPhong };
            _context.PhongTienIchs.RemoveRange(await _context.PhongTienIchs.Where(x => phongs.Contains(x.MaPhong)).ToListAsync());
            foreach (var p in phongs)
                foreach (var m in ma)
                    _context.PhongTienIchs.Add(new PhongTienIch { MaPhong = p, MaTienIch = m });
            await _context.SaveChangesAsync();
            return phongs.Count;
        }

        private async Task<TienIch> FindAsync(string maTienIch)
            => await _context.TienIchs.Include(t => t.PhongTienIchs).FirstOrDefaultAsync(t => t.MaTienIch == maTienIch)
               ?? throw new NotFoundException($"Không tìm thấy tiện nghi '{maTienIch}'.");
    }
}
