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
    // Hồ sơ khách phía khách sạn: thông tin cá nhân, VIP, sở thích, ghi chú nội bộ, hạn chế đặt online,
    // điều chỉnh điểm thủ công và lịch sử điểm
    public interface ICustomerProfileService
    {
        Task<KhachHang> UpdateProfileAsync(string maKhachHang, UpdateCustomerProfileRequest request);
        Task<LichSuDiem> AdjustPointsAsync(string maKhachHang, int soDiem, string lyDo, string maNhanVien);
        Task<List<LichSuDiem>> GetPointHistoryAsync(string maKhachHang);
    }

    public class CustomerProfileService : ICustomerProfileService
    {
        private readonly ApplicationDbContext _context;

        public CustomerProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<KhachHang> UpdateProfileAsync(string maKhachHang, UpdateCustomerProfileRequest r)
        {
            var khach = await _context.KhachHangs.Include(k => k.LienHeDiaChis)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang)
                ?? throw new NotFoundException($"Không tìm thấy khách hàng '{maKhachHang}'.");

            var cccd = r.CCCD_Passport.Trim();
            if (await _context.KhachHangs.AnyAsync(k => k.CCCD_Passport == cccd && k.MaKhachHang != maKhachHang))
                throw new ConflictException($"Số CCCD/Passport {cccd} đã thuộc hồ sơ khách khác.");
            if (r.HanChe && string.IsNullOrWhiteSpace(r.LyDoHanChe))
                throw new BusinessException("Vui lòng ghi lý do hạn chế đặt phòng.");

            khach.Ho = r.Ho.Trim();
            khach.Ten = r.Ten.Trim();
            khach.NgaySinh = r.NgaySinh.Date;
            khach.CCCD_Passport = cccd;
            khach.LaVIP = r.LaVIP;
            khach.SoThich = Clean(r.SoThich);
            khach.GhiChuNoiBo = Clean(r.GhiChuNoiBo);
            khach.HanChe = r.HanChe;
            khach.LyDoHanChe = r.HanChe ? Clean(r.LyDoHanChe) : null;

            var lienHe = khach.LienHeDiaChis.OrderBy(l => l.MaLienHe).FirstOrDefault();
            if (lienHe == null)
            {
                lienHe = new LienHeDiaChi
                {
                    MaLienHe = await IdGenerator.NextAsync(_context.LienHeDiaChis.Select(l => l.MaLienHe), "LH", 4),
                    MaKhachHang = maKhachHang
                };
                _context.LienHeDiaChis.Add(lienHe);
            }
            lienHe.SDT = r.SDT.Trim();
            lienHe.Email = r.Email?.Trim() ?? "";
            lienHe.DiaChi = r.DiaChi?.Trim() ?? "";
            lienHe.QuocGia = string.IsNullOrWhiteSpace(r.QuocGia) ? "Việt Nam" : r.QuocGia.Trim();

            await _context.SaveChangesAsync();
            return khach;
        }

        public async Task<LichSuDiem> AdjustPointsAsync(string maKhachHang, int soDiem, string lyDo, string maNhanVien)
        {
            if (soDiem == 0) throw new BusinessException("Số điểm điều chỉnh phải khác 0.");
            var khach = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang)
                ?? throw new NotFoundException($"Không tìm thấy khách hàng '{maKhachHang}'.");
            if (khach.DiemTichLuy + soDiem < 0)
                throw new BusinessException($"Khách chỉ còn {khach.DiemTichLuy} điểm, không trừ được {-soDiem} điểm.");

            khach.DiemTichLuy += soDiem;
            var dong = await PointLedger.AddAsync(_context, maKhachHang, soDiem, PointLedger.DieuChinh, lyDo.Trim(), null, maNhanVien);
            await _context.SaveChangesAsync();
            await _context.Entry(dong).Reference(l => l.NhanVien).LoadAsync();
            return dong;
        }

        public async Task<List<LichSuDiem>> GetPointHistoryAsync(string maKhachHang)
        {
            if (!await _context.KhachHangs.AnyAsync(k => k.MaKhachHang == maKhachHang))
                throw new NotFoundException($"Không tìm thấy khách hàng '{maKhachHang}'.");
            return await _context.LichSuDiems.Include(l => l.NhanVien)
                .Where(l => l.MaKhachHang == maKhachHang)
                .OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu)
                .ToListAsync();
        }

        private static string Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    // Ghi một dòng lịch sử điểm (chưa SaveChanges). Mọi thay đổi DiemTichLuy đều phải đi kèm một dòng để
    // tổng lịch sử luôn bằng số điểm hiện có.
    public static class PointLedger
    {
        public const string TichDiem = "Tích điểm";
        public const string DoiDiem = "Đổi điểm";
        public const string HoanTien = "Hoàn tiền";
        public const string DieuChinh = "Điều chỉnh";
        public const string SoDuDauKy = "Số dư đầu kỳ";

        public static async Task<LichSuDiem> AddAsync(ApplicationDbContext context, string maKhachHang, int soDiem, string loai,
            string noiDung, string maHoaDon, string maNhanVien)
        {
            // Mã kế tiếp tính cả các dòng vừa thêm nhưng chưa lưu (nhiều dòng trong cùng một giao dịch)
            var ids = (await context.LichSuDiems.Select(l => l.MaLichSu).ToListAsync())
                .Concat(context.LichSuDiems.Local.Select(l => l.MaLichSu));
            var max = ids.Where(i => i.StartsWith("LSD") && i.Length > 3 && i.Substring(3).All(char.IsDigit))
                .Select(i => long.Parse(i.Substring(3))).DefaultIfEmpty(0).Max();
            var dong = new LichSuDiem
            {
                MaLichSu = "LSD" + (max + 1).ToString("D6"),
                MaKhachHang = maKhachHang,
                ThoiGian = DateTime.Now,
                SoDiem = soDiem,
                Loai = loai,
                NoiDung = noiDung,
                MaHoaDon = maHoaDon,
                MaNhanVien = maNhanVien
            };
            context.LichSuDiems.Add(dong);
            return dong;
        }
    }
}
