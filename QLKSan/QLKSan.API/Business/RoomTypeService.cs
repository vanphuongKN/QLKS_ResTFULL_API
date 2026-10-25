using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;

namespace QLKSan.Business
{
    // Hạng phòng phía khách: danh sách kèm điểm đánh giá, chi tiết (ảnh, tiện nghi, đánh giá), hạng còn trống theo ngày
    public interface IRoomTypeService
    {
        Task<List<RoomTypeDto>> GetAllAsync();

        // Ném NotFoundException nếu không có hạng phòng
        Task<RoomTypeDetailDto> GetDetailAsync(string maLoai);

        // Hạng còn ít nhất 1 phòng trống trong [checkIn, checkOut) và đủ sức chứa, kèm báo giá từng đêm (rẻ trước)
        Task<List<RoomTypeAvailabilityDto>> GetAvailableAsync(DateTime checkIn, DateTime checkOut, int? soNguoi);

        Task<List<ReviewDto>> GetReviewsAsync(string maLoai);

        // Đánh giá tốt gần đây (từ 4 sao, có nội dung) để giới thiệu trên trang chủ
        Task<List<ReviewDto>> GetFeaturedReviewsAsync(int soLuong);
    }

    public class RoomTypeService : IRoomTypeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomAvailabilityService _availability;
        private readonly IPricingService _pricing;

        public RoomTypeService(ApplicationDbContext context, IRoomAvailabilityService availability, IPricingService pricing)
        {
            _context = context;
            _availability = availability;
            _pricing = pricing;
        }

        // Điểm trung bình và số đánh giá theo hạng phòng (đánh giá gắn với phòng thuộc hạng)
        private async Task<Dictionary<string, (double Diem, int So)>> RatingsAsync()
        {
            var rows = await _context.DanhGias
                .GroupBy(d => d.Phong.MaLoai)
                .Select(g => new { MaLoai = g.Key, Diem = g.Average(d => (double)d.SoSao), So = g.Count() })
                .ToListAsync();
            return rows.ToDictionary(r => r.MaLoai, r => (Math.Round(r.Diem, 1), r.So));
        }

        private static void ApplyRating(RoomTypeDto dto, Dictionary<string, (double Diem, int So)> ratings)
        {
            if (ratings.TryGetValue(dto.MaLoai, out var r))
            {
                dto.DiemDanhGia = r.Diem;
                dto.SoDanhGia = r.So;
            }
        }

        public async Task<List<RoomTypeDto>> GetAllAsync()
        {
            var ratings = await RatingsAsync();
            var types = await _context.LoaiPhongs.ToListAsync();
            return types.Select(l =>
            {
                var dto = l.ToDto();
                ApplyRating(dto, ratings);
                return dto;
            }).ToList();
        }

        public async Task<RoomTypeDetailDto> GetDetailAsync(string maLoai)
        {
            var loai = await _context.LoaiPhongs.Include(l => l.HinhAnhs).FirstOrDefaultAsync(l => l.MaLoai == maLoai)
                ?? throw new NotFoundException($"Không tìm thấy hạng phòng '{maLoai}'.");

            var tienIchs = await _context.PhongTienIchs
                .Where(t => t.Phong.MaLoai == maLoai)
                .Select(t => t.TienIch)
                .Distinct()
                .OrderBy(t => t.TenTienIch)
                .ToListAsync();

            var reviews = await GetReviewsAsync(maLoai);
            var ratings = await RatingsAsync();
            var basic = loai.ToDto();
            var detail = new RoomTypeDetailDto
            {
                MaLoai = basic.MaLoai,
                TenLoai = basic.TenLoai,
                SoNguoiToiDa = basic.SoNguoiToiDa,
                GiaThueNgay = basic.GiaThueNgay,
                GiaThueGio = basic.GiaThueGio,
                MoTa = basic.MoTa,
                DienTich = basic.DienTich,
                LoaiGiuong = basic.LoaiGiuong,
                HuongNhin = basic.HuongNhin,
                MoTaChiTiet = basic.MoTaChiTiet,
                AnhDaiDien = basic.AnhDaiDien,
                HinhAnhs = loai.HinhAnhs.OrderBy(h => h.ThuTu).ThenBy(h => h.MaHinhAnh).Select(h => h.ToDto()).ToList(),
                TienIchs = tienIchs.Select(t => t.ToDto()).ToList(),
                DanhGiaGanDay = reviews.Take(6).ToList()
            };
            ApplyRating(detail, ratings);
            return detail;
        }

        public async Task<List<RoomTypeAvailabilityDto>> GetAvailableAsync(DateTime checkIn, DateTime checkOut, int? soNguoi)
        {
            var from = checkIn.Date;
            var to = checkOut.Date;
            if (to <= from)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");

            var rooms = await _availability.GetAvailableRoomsAsync(from, to, soNguoi);
            var byType = rooms.Where(p => p.LoaiPhong != null).GroupBy(p => p.MaLoai).ToList();

            // Giá theo hạng phòng nên chỉ cần báo giá 1 phòng đại diện mỗi hạng
            var quotes = await _pricing.QuoteRoomsAsync(byType.Select(g => g.First()), from, to);
            var ratings = await RatingsAsync();

            return byType.Select(g =>
            {
                var dto = g.First().LoaiPhong.ToDto();
                ApplyRating(dto, ratings);
                return new RoomTypeAvailabilityDto
                {
                    LoaiPhong = dto,
                    SoPhongTrong = g.Count(),
                    BaoGia = quotes[g.First().MaPhong]
                };
            })
            .OrderBy(x => x.BaoGia.TongTien)
            .ToList();
        }

        public async Task<List<ReviewDto>> GetReviewsAsync(string maLoai)
            => PublicReviews(await PublicQuery().Where(d => d.Phong.MaLoai == maLoai).ToListAsync());

        public async Task<List<ReviewDto>> GetFeaturedReviewsAsync(int soLuong)
            => PublicReviews(await PublicQuery().Where(d => d.SoSao >= 4 && d.NoiDung != null && d.NoiDung != "").Take(soLuong).ToListAsync());

        private IQueryable<Models.Entities.DanhGia> PublicQuery()
            => _context.DanhGias
                .Include(d => d.KhachHang)
                .Include(d => d.Phong).ThenInclude(p => p.LoaiPhong)
                .Where(d => !d.An)
                .OrderByDescending(d => d.NgayDanhGia);

        // Danh sách công khai: không trả mã / họ tên đầy đủ của khách
        private static List<ReviewDto> PublicReviews(IEnumerable<Models.Entities.DanhGia> reviews)
            => reviews.Select(d =>
            {
                var dto = d.ToDto();
                dto.MaKhachHang = null;
                dto.HoTenKhachHang = null;
                return dto;
            }).ToList();
    }
}
