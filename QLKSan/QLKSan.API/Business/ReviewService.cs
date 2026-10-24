using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Quản lý đánh giá phía khách sạn: lọc, phản hồi công khai, ẩn đánh giá không phù hợp.
    // Đánh giá bị ẩn không hiện trên trang khách nhưng vẫn được tính vào điểm trung bình (tránh làm đẹp điểm số).
    public interface IReviewService
    {
        Task<List<DanhGia>> GetAllAsync(int? soSao, bool? chuaPhanHoi, bool? an);
        Task<DanhGia> ReplyAsync(string maDanhGia, string noiDung, string maNhanVien);
        Task<DanhGia> DeleteReplyAsync(string maDanhGia);
        Task<DanhGia> SetHiddenAsync(string maDanhGia, bool an);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<DanhGia> Query()
            => _context.DanhGias
                .Include(d => d.KhachHang)
                .Include(d => d.Phong).ThenInclude(p => p.LoaiPhong)
                .Include(d => d.NhanVienPhanHoi);

        public async Task<List<DanhGia>> GetAllAsync(int? soSao, bool? chuaPhanHoi, bool? an)
        {
            var q = Query();
            if (soSao is >= 1 and <= 5) q = q.Where(d => d.SoSao == soSao);
            if (chuaPhanHoi == true) q = q.Where(d => d.PhanHoi == null);
            if (an != null) q = q.Where(d => d.An == an);
            return await q.OrderByDescending(d => d.NgayDanhGia).ToListAsync();
        }

        public async Task<DanhGia> ReplyAsync(string maDanhGia, string noiDung, string maNhanVien)
        {
            var review = await FindAsync(maDanhGia);
            review.PhanHoi = noiDung.Trim();
            review.NgayPhanHoi = DateTime.Now;
            review.MaNhanVienPhanHoi = maNhanVien;
            await _context.SaveChangesAsync();
            return await FindAsync(maDanhGia);
        }

        public async Task<DanhGia> DeleteReplyAsync(string maDanhGia)
        {
            var review = await FindAsync(maDanhGia);
            review.PhanHoi = null;
            review.NgayPhanHoi = null;
            review.MaNhanVienPhanHoi = null;
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<DanhGia> SetHiddenAsync(string maDanhGia, bool an)
        {
            var review = await FindAsync(maDanhGia);
            review.An = an;
            await _context.SaveChangesAsync();
            return review;
        }

        private async Task<DanhGia> FindAsync(string maDanhGia)
            => await Query().FirstOrDefaultAsync(d => d.MaDanhGia == maDanhGia)
               ?? throw new NotFoundException($"Không tìm thấy đánh giá '{maDanhGia}'.");
    }
}
