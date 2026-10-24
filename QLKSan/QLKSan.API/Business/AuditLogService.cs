using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Models.DTOs;

namespace QLKSan.Business
{
    // Tra cứu nhật ký thao tác (ghi tự động bởi AuditFilter và khi tài khoản bị tạm khoá)
    public interface IAuditLogService
    {
        Task<AuditLogPageDto> SearchAsync(DateTime? tuNgay, DateTime? denNgay, string doiTuong, string nguoiDung, string tuKhoa, int trang, int soDong);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLogPageDto> SearchAsync(DateTime? tuNgay, DateTime? denNgay, string doiTuong, string nguoiDung, string tuKhoa, int trang, int soDong)
        {
            trang = Math.Max(1, trang);
            soDong = Math.Clamp(soDong, 10, 200);
            var q = _context.NhatKyThaoTacs.AsQueryable();
            if (tuNgay != null) q = q.Where(n => n.ThoiGian >= tuNgay.Value.Date);
            if (denNgay != null) q = q.Where(n => n.ThoiGian < denNgay.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(doiTuong)) q = q.Where(n => n.DoiTuong == doiTuong);
            if (!string.IsNullOrWhiteSpace(nguoiDung)) q = q.Where(n => n.TenDangNhap == nguoiDung.Trim());
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var k = tuKhoa.Trim();
                q = q.Where(n => n.HanhDong.Contains(k) || n.MaDoiTuong.Contains(k) || n.ChiTiet.Contains(k));
            }

            var tong = await q.CountAsync();
            var rows = await q.OrderByDescending(n => n.ThoiGian).ThenByDescending(n => n.MaNhatKy)
                .Skip((trang - 1) * soDong).Take(soDong).ToListAsync();
            return new AuditLogPageDto
            {
                Trang = trang,
                SoDong = soDong,
                TongSo = tong,
                DoiTuongs = await _context.NhatKyThaoTacs.Select(n => n.DoiTuong).Where(d => d != null).Distinct().OrderBy(d => d).ToListAsync(),
                NguoiDungs = await _context.NhatKyThaoTacs.Select(n => n.TenDangNhap).Where(d => d != null).Distinct().OrderBy(d => d).ToListAsync(),
                Dong = rows.Select(n => new AuditLogDto
                {
                    MaNhatKy = n.MaNhatKy,
                    ThoiGian = n.ThoiGian,
                    TenDangNhap = n.TenDangNhap,
                    VaiTro = n.VaiTro,
                    HanhDong = n.HanhDong,
                    DoiTuong = n.DoiTuong,
                    MaDoiTuong = n.MaDoiTuong,
                    ChiTiet = n.ChiTiet,
                    DiaChiIP = n.DiaChiIP
                }).ToList()
            };
        }
    }
}
