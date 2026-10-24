using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    // Khuyến mãi: quản lý (thêm / sửa / bật–tắt / xoá) và kiểm tra điều kiện áp dụng
    // (còn hạn, đang bật, hạng phòng, số đêm tối thiểu, số lượt tối đa) khi khách đặt phòng hoặc khi lập hoá đơn
    public interface IPromotionService
    {
        Task<List<PromotionDto>> GetAllAsync();
        Task<PromotionDto> CreateAsync(PromotionRequest request);
        Task<PromotionDto> UpdateAsync(string maKhuyenMai, PromotionRequest request);
        Task<PromotionDto> SetActiveAsync(string maKhuyenMai, bool kichHoat);

        // Chỉ xoá mã chưa từng dùng (đã dùng → ConflictException, nên tắt thay vì xoá)
        Task DeleteAsync(string maKhuyenMai);

        // Mã còn hiệu lực hôm nay (khách xem trước khi đặt): 404 không tồn tại, 400 hết hạn / đã tắt / hết lượt
        Task<PromotionDto> GetActiveAsync(string maKhuyenMai);

        // Kiểm tra mã cho một kỳ nghỉ cụ thể; ném BusinessException kèm lý do. maDatPhongBoQua: không đếm lượt của chính phiếu này
        Task<KhuyenMai> ValidateForStayAsync(string maKhuyenMai, string maLoai, int soDem, string maDatPhongBoQua = null);
    }

    public class PromotionService : IPromotionService
    {
        public const string PhanTram = "Phần trăm";
        private readonly ApplicationDbContext _context;

        public PromotionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PromotionDto>> GetAllAsync()
        {
            var promos = await _context.KhuyenMais.Include(k => k.LoaiPhong).OrderByDescending(k => k.NgayBatDau).ToListAsync();
            var usage = await UsageAsync(null);
            return promos.Select(k =>
            {
                var dto = k.ToDto();
                dto.SoLuotDaDung = usage.TryGetValue(k.MaKhuyenMai, out var n) ? n : 0;
                return dto;
            }).ToList();
        }

        public async Task<PromotionDto> CreateAsync(PromotionRequest request)
        {
            var ma = request.MaKhuyenMai?.Trim().ToUpperInvariant();
            if (await _context.KhuyenMais.AnyAsync(k => k.MaKhuyenMai == ma))
                throw new ConflictException($"Mã khuyến mãi '{ma}' đã tồn tại.");
            await ValidateRequestAsync(request);

            var promo = new KhuyenMai { MaKhuyenMai = ma, KichHoat = true };
            Apply(promo, request);
            _context.KhuyenMais.Add(promo);
            await _context.SaveChangesAsync();
            return await GetDtoAsync(ma);
        }

        public async Task<PromotionDto> UpdateAsync(string maKhuyenMai, PromotionRequest request)
        {
            var promo = await FindAsync(maKhuyenMai);
            await ValidateRequestAsync(request);
            Apply(promo, request);
            await _context.SaveChangesAsync();
            return await GetDtoAsync(promo.MaKhuyenMai);
        }

        public async Task<PromotionDto> SetActiveAsync(string maKhuyenMai, bool kichHoat)
        {
            var promo = await FindAsync(maKhuyenMai);
            promo.KichHoat = kichHoat;
            await _context.SaveChangesAsync();
            return await GetDtoAsync(promo.MaKhuyenMai);
        }

        public async Task DeleteAsync(string maKhuyenMai)
        {
            var promo = await FindAsync(maKhuyenMai);
            var daDung = await _context.PhieuDatPhongs.AnyAsync(p => p.MaKhuyenMai == promo.MaKhuyenMai)
                         || await _context.HoaDons.AnyAsync(h => h.MaKhuyenMai == promo.MaKhuyenMai);
            if (daDung)
                throw new ConflictException($"Mã {promo.MaKhuyenMai} đã được dùng cho phiếu đặt / hoá đơn nên không xoá được. Hãy tắt mã thay vì xoá.");
            _context.KhuyenMais.Remove(promo);
            await _context.SaveChangesAsync();
        }

        public async Task<PromotionDto> GetActiveAsync(string maKhuyenMai)
        {
            var promo = await _context.KhuyenMais.Include(k => k.LoaiPhong).FirstOrDefaultAsync(k => k.MaKhuyenMai == (maKhuyenMai ?? "").Trim())
                ?? throw new NotFoundException("Mã khuyến mãi không tồn tại.");
            if (!DangApDung(promo, DateTime.Today))
                throw new BusinessException("Mã khuyến mãi đã hết hạn hoặc chưa được kích hoạt.");
            if (promo.SoLuotToiDa != null && await CountUsageAsync(promo.MaKhuyenMai, null) >= promo.SoLuotToiDa)
                throw new BusinessException($"Mã {promo.MaKhuyenMai} đã hết lượt sử dụng.");
            return promo.ToDto();
        }

        public async Task<KhuyenMai> ValidateForStayAsync(string maKhuyenMai, string maLoai, int soDem, string maDatPhongBoQua = null)
        {
            var promo = await _context.KhuyenMais.Include(k => k.LoaiPhong).FirstOrDefaultAsync(k => k.MaKhuyenMai == (maKhuyenMai ?? "").Trim());
            if (promo == null || !DangApDung(promo, DateTime.Today))
                throw new BusinessException("Mã khuyến mãi không tồn tại, đã hết hạn hoặc chưa được kích hoạt.");
            if (promo.MaLoai != null && promo.MaLoai != maLoai)
                throw new BusinessException($"Mã {promo.MaKhuyenMai} chỉ áp dụng cho hạng {promo.LoaiPhong?.TenLoai ?? promo.MaLoai}.");
            if (promo.SoDemToiThieu != null && soDem < promo.SoDemToiThieu)
                throw new BusinessException($"Mã {promo.MaKhuyenMai} áp dụng cho kỳ nghỉ từ {promo.SoDemToiThieu} đêm.");
            if (promo.SoLuotToiDa != null && await CountUsageAsync(promo.MaKhuyenMai, maDatPhongBoQua) >= promo.SoLuotToiDa)
                throw new BusinessException($"Mã {promo.MaKhuyenMai} đã hết lượt sử dụng.");
            return promo;
        }

        // Đang áp dụng: đang bật và hôm nay nằm trong [ngày bắt đầu, ngày kết thúc]
        public static bool DangApDung(KhuyenMai k, DateTime today)
            => k.KichHoat && k.NgayBatDau.Date <= today && k.NgayKetThuc.Date >= today;

        private async Task<KhuyenMai> FindAsync(string maKhuyenMai)
            => await _context.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == maKhuyenMai)
               ?? throw new NotFoundException($"Không tìm thấy khuyến mãi '{maKhuyenMai}'.");

        private async Task<PromotionDto> GetDtoAsync(string ma)
            => (await GetAllAsync()).First(k => k.MaKhuyenMai == ma);

        private async Task ValidateRequestAsync(PromotionRequest r)
        {
            if (r.LoaiGiam != PhanTram && r.LoaiGiam != "Số tiền")
                throw new BusinessException("Loại giảm phải là \"Phần trăm\" hoặc \"Số tiền\".");
            if (r.LoaiGiam == PhanTram && (r.GiaTriGiam <= 0 || r.GiaTriGiam > 100))
                throw new BusinessException("Giảm theo phần trăm phải từ 1 đến 100%.");
            if (r.NgayKetThuc.Date < r.NgayBatDau.Date)
                throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.");
            if (!string.IsNullOrEmpty(r.MaLoai) && !await _context.LoaiPhongs.AnyAsync(l => l.MaLoai == r.MaLoai))
                throw new BusinessException($"Không tìm thấy hạng phòng '{r.MaLoai}'.");
        }

        private static void Apply(KhuyenMai k, PromotionRequest r)
        {
            k.TenKhuyenMai = r.TenKhuyenMai.Trim();
            k.LoaiGiam = r.LoaiGiam;
            k.GiaTriGiam = r.GiaTriGiam;
            k.NgayBatDau = r.NgayBatDau.Date;
            k.NgayKetThuc = r.NgayKetThuc.Date;
            k.SoLuotToiDa = r.SoLuotToiDa;
            k.SoDemToiThieu = r.SoDemToiThieu;
            k.MaLoai = string.IsNullOrEmpty(r.MaLoai) ? null : r.MaLoai;
        }

        private async Task<int> CountUsageAsync(string ma, string maDatPhongBoQua)
            => (await UsageAsync(maDatPhongBoQua)).TryGetValue(ma, out var n) ? n : 0;

        // Số lượt dùng mỗi mã = số phiếu (không tính phiếu đã huỷ) có mã, cộng hoá đơn có mã khác với mã khách chọn lúc đặt
        private async Task<Dictionary<string, int>> UsageAsync(string maDatPhongBoQua)
        {
            var phieu = await _context.PhieuDatPhongs
                .Where(p => p.MaKhuyenMai != null && p.TrangThai != BookingStatus.Cancelled && p.MaDatPhong != maDatPhongBoQua)
                .Select(p => new { p.MaDatPhong, p.MaKhuyenMai })
                .ToListAsync();
            var hoaDon = await _context.HoaDons
                .Where(h => h.MaKhuyenMai != null && h.TrangThai != InvoiceStatus.Cancelled && h.MaDatPhong != maDatPhongBoQua)
                .Select(h => new { h.MaDatPhong, h.MaKhuyenMai })
                .ToListAsync();
            return phieu.Concat(hoaDon).Distinct()
                .GroupBy(x => x.MaKhuyenMai)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MaDatPhong).Distinct().Count());
        }
    }
}
