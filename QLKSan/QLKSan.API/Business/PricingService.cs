using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public interface IPricingService
    {
        Task<List<QuyTacGia>> GetRulesAsync();
        Task<QuyTacGia> AddRuleAsync(PricingRuleRequest request);
        Task<QuyTacGia> UpdateRuleAsync(string maQuyTac, PricingRuleRequest request);
        Task<QuyTacGia> SetRuleActiveAsync(string maQuyTac, bool kichHoat);
        Task DeleteRuleAsync(string maQuyTac);

        // Giá từng đêm của 1 loại phòng (dùng khi lập hoá đơn, tính cọc, báo giá)
        Task<List<NightPriceDto>> GetNightlyPricesAsync(string maLoai, decimal giaGoc, DateTime ngayNhan, int soDem);
        Task<PriceQuoteDto> QuoteAsync(string maPhong, DateTime checkIn, DateTime checkOut);

        // Báo giá cho nhiều phòng cùng khoảng ngày (danh sách phòng trống), nạp quy tắc giá một lần; key = mã phòng
        Task<Dictionary<string, PriceQuoteDto>> QuoteRoomsAsync(IEnumerable<Phong> rooms, DateTime checkIn, DateTime checkOut);
    }

    public class PricingService : IPricingService
    {
        private readonly ApplicationDbContext _context;

        public PricingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<QuyTacGia>> GetRulesAsync()
            => await _context.QuyTacGias.Include(q => q.LoaiPhong)
                .OrderByDescending(q => q.KichHoat).ThenByDescending(q => q.UuTien).ThenBy(q => q.MaQuyTac)
                .ToListAsync();

        private async Task ValidateRuleAsync(PricingRuleRequest request)
        {
            if (request.TuNgay != null && request.DenNgay != null && request.DenNgay.Value.Date < request.TuNgay.Value.Date)
                throw new BusinessException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
            if (!string.IsNullOrEmpty(request.MaLoai) && !await _context.LoaiPhongs.AnyAsync(l => l.MaLoai == request.MaLoai))
                throw new BusinessException($"Loại phòng '{request.MaLoai}' không tồn tại.");
            if (request.PhanTramDieuChinh == 0)
                throw new BusinessException("Mức điều chỉnh phải khác 0%.");
        }

        // Sửa quy tắc (giữ nguyên trạng thái bật/tắt); hoá đơn đã lập không bị ảnh hưởng
        public async Task<QuyTacGia> UpdateRuleAsync(string maQuyTac, PricingRuleRequest request)
        {
            var rule = await _context.QuyTacGias.FindAsync(maQuyTac)
                ?? throw new NotFoundException($"Không tìm thấy quy tắc giá '{maQuyTac}'.");
            await ValidateRuleAsync(request);
            rule.TenQuyTac = request.TenQuyTac.Trim();
            rule.MaLoai = string.IsNullOrWhiteSpace(request.MaLoai) ? null : request.MaLoai;
            rule.TuNgay = request.TuNgay?.Date;
            rule.DenNgay = request.DenNgay?.Date;
            rule.CacThu = PricingCalculator.FormatThu(request.CacThu);
            rule.PhanTramDieuChinh = request.PhanTramDieuChinh;
            rule.UuTien = request.UuTien;
            await _context.SaveChangesAsync();
            await _context.Entry(rule).Reference(q => q.LoaiPhong).LoadAsync();
            return rule;
        }

        public async Task<QuyTacGia> AddRuleAsync(PricingRuleRequest request)
        {
            await ValidateRuleAsync(request);

            var rule = new QuyTacGia
            {
                MaQuyTac = await IdGenerator.NextAsync(_context.QuyTacGias.Select(q => q.MaQuyTac), "QT", 3),
                TenQuyTac = request.TenQuyTac.Trim(),
                MaLoai = string.IsNullOrWhiteSpace(request.MaLoai) ? null : request.MaLoai,
                TuNgay = request.TuNgay?.Date,
                DenNgay = request.DenNgay?.Date,
                CacThu = PricingCalculator.FormatThu(request.CacThu),
                PhanTramDieuChinh = request.PhanTramDieuChinh,
                UuTien = request.UuTien,
                KichHoat = true
            };
            _context.QuyTacGias.Add(rule);
            await _context.SaveChangesAsync();
            // Nạp tên loại phòng cho kết quả trả về
            await _context.Entry(rule).Reference(q => q.LoaiPhong).LoadAsync();
            return rule;
        }

        public async Task<QuyTacGia> SetRuleActiveAsync(string maQuyTac, bool kichHoat)
        {
            var rule = await _context.QuyTacGias.FindAsync(maQuyTac)
                ?? throw new NotFoundException($"Không tìm thấy quy tắc giá '{maQuyTac}'.");
            rule.KichHoat = kichHoat;
            await _context.SaveChangesAsync();
            await _context.Entry(rule).Reference(q => q.LoaiPhong).LoadAsync();
            return rule;
        }

        public async Task DeleteRuleAsync(string maQuyTac)
        {
            var rule = await _context.QuyTacGias.FindAsync(maQuyTac)
                ?? throw new NotFoundException($"Không tìm thấy quy tắc giá '{maQuyTac}'.");
            _context.QuyTacGias.Remove(rule);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NightPriceDto>> GetNightlyPricesAsync(string maLoai, decimal giaGoc, DateTime ngayNhan, int soDem)
        {
            var rules = await _context.QuyTacGias.Where(q => q.KichHoat).ToListAsync();
            return PricingCalculator.PriceNights(giaGoc, maLoai, ngayNhan, soDem, rules);
        }

        public async Task<PriceQuoteDto> QuoteAsync(string maPhong, DateTime checkIn, DateTime checkOut)
        {
            if (checkOut.Date <= checkIn.Date)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");
            var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{maPhong}'.");

            var rules = await _context.QuyTacGias.Where(q => q.KichHoat).ToListAsync();
            return BuildQuote(room, checkIn, checkOut, rules);
        }

        public async Task<Dictionary<string, PriceQuoteDto>> QuoteRoomsAsync(IEnumerable<Phong> rooms, DateTime checkIn, DateTime checkOut)
        {
            if (checkOut.Date <= checkIn.Date)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");
            var rules = await _context.QuyTacGias.Where(q => q.KichHoat).ToListAsync();
            return rooms.ToDictionary(p => p.MaPhong, p => BuildQuote(p, checkIn, checkOut, rules));
        }

        // Giá từng đêm của phòng (cần LoaiPhong đã được nạp) cho khoảng [checkIn, checkOut)
        private static PriceQuoteDto BuildQuote(Phong room, DateTime checkIn, DateTime checkOut, List<QuyTacGia> rules)
        {
            var giaGoc = room.LoaiPhong?.GiaThueNgay ?? 0;
            var soDem = (checkOut.Date - checkIn.Date).Days;
            var nights = PricingCalculator.PriceNights(giaGoc, room.MaLoai, checkIn.Date, soDem, rules);
            var tong = nights.Sum(n => n.Gia);
            return new PriceQuoteDto
            {
                MaPhong = room.MaPhong,
                GiaGoc = giaGoc,
                SoDem = soDem,
                TongTien = tong,
                GiaTrungBinhDem = Math.Round(tong / soDem, 0),
                CacDem = nights
            };
        }
    }
}
