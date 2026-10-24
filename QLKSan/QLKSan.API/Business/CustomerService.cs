using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomAvailabilityService _availability;

        public CustomerService(ApplicationDbContext context, IRoomAvailabilityService availability)
        {
            _context = context;
            _availability = availability;
        }

        public async Task<KhachHang> GetCustomerByAccountIdAsync(string maDangNhap)
            => await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                .FirstOrDefaultAsync(k => k.MaDangNhap == maDangNhap);

        public async Task<KhachHang> UpdateContactAsync(string maKhachHang, UpdateContactRequest request)
        {
            var khach = await _context.KhachHangs.Include(k => k.LienHeDiaChis)
                .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang)
                ?? throw new NotFoundException("Không tìm thấy hồ sơ khách hàng.");

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
            lienHe.SDT = request.SDT.Trim();
            lienHe.Email = request.Email?.Trim() ?? "";
            lienHe.DiaChi = request.DiaChi.Trim();
            lienHe.QuocGia = string.IsNullOrWhiteSpace(request.QuocGia) ? "Việt Nam" : request.QuocGia.Trim();
            await _context.SaveChangesAsync();
            return khach;
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetBookingsByCustomerAsync(string maKhachHang)
            => await _context.PhieuDatPhongs
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.HoaDon).ThenInclude(h => h.ThanhToans)
                .Where(p => p.MaKhachHang == maKhachHang)
                .OrderByDescending(p => p.NgayDat)
                .ToListAsync();

        public async Task<string> CreateOnlineBookingAsync(string maKhachHang, string maPhong, int soNguoi, DateTime checkIn, DateTime checkOut, string? maKhuyenMai = null)
        {
            checkIn = checkIn.Date;
            checkOut = checkOut.Date;
            if (checkIn < DateTime.Today)
                throw new BusinessException("Ngày nhận phòng không được ở trong quá khứ.");
            if (checkOut <= checkIn)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");

            // Mã khuyến mãi phải còn hiệu lực lúc đặt; được lưu vào phiếu để áp dụng khi trả phòng
            maKhuyenMai = string.IsNullOrWhiteSpace(maKhuyenMai) ? null : maKhuyenMai.Trim();
            if (maKhuyenMai != null)
            {
                var promo = await _context.KhuyenMais.FindAsync(maKhuyenMai);
                var today = DateTime.Today;
                if (promo == null || promo.TrangThai != PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
                    throw new BusinessException("Mã khuyến mãi không tồn tại, đã hết hạn hoặc chưa được kích hoạt.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Khoá phòng để hai khách không đặt trùng lịch cùng lúc
                await _availability.LockRoomAsync(maPhong);

                var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                    ?? throw new BusinessException("Phòng không tồn tại.");
                if (room.TrangThai == RoomStatus.Maintenance)
                    throw new ConflictException("Phòng đang bảo trì, vui lòng chọn phòng khác.");
                if (room.LoaiPhong != null && soNguoi > room.LoaiPhong.SoNguoiToiDa)
                    throw new BusinessException($"Số lượng khách ({soNguoi}) vượt quá sức chứa tối đa của {room.LoaiPhong.TenLoai} ({room.LoaiPhong.SoNguoiToiDa} người lớn).");

                await _availability.EnsureAvailableAsync(maPhong, checkIn, checkOut);

                var booking = new PhieuDatPhong
                {
                    MaDatPhong = await IdGenerator.NextAsync(_context.PhieuDatPhongs.Select(p => p.MaDatPhong), "PDP", 7),
                    MaKhachHang = maKhachHang,
                    NgayDat = DateTime.Now,
                    NgayCheckInDuKien = checkIn,
                    NgayCheckOutDuKien = checkOut,
                    SoLuongKhach = soNguoi,
                    TrangThai = BookingStatus.Pending,
                    TienCoc = 0,
                    MaKhuyenMai = maKhuyenMai
                };
                _context.PhieuDatPhongs.Add(booking);

                _context.ChiTietDatPhongs.Add(new ChiTietDatPhong
                {
                    MaCTDP = await IdGenerator.NextAsync(_context.ChiTietDatPhongs.Select(c => c.MaCTDP), "CT", 4),
                    MaDatPhong = booking.MaDatPhong,
                    MaPhong = maPhong,
                    HinhThucThue = RentalType.Daily,
                    GiaThueThucTe = room.LoaiPhong.GiaThueNgay
                });

                // Không đổi Phong.TrangThai: phòng chỉ bị giữ trong khoảng ngày của phiếu
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return booking.MaDatPhong;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<DanhGia> SubmitReviewAsync(string maKhachHang, string maPhong, int soSao, string noiDung, bool anDanh)
        {
            // Validate that the customer has actually checked out of this room
            bool hasCheckedOutBooking = await _context.PhieuDatPhongs
                .AnyAsync(b => b.MaKhachHang == maKhachHang 
                            && b.TrangThai == BookingStatus.CheckedOut
                            && b.ChiTietDatPhongs.Any(c => c.MaPhong == maPhong));
            
            if (!hasCheckedOutBooking)
                throw new BusinessException("Bạn chỉ có thể đánh giá phòng đã ở và đã trả phòng.");

            var existingReview = await _context.DanhGias
                .FirstOrDefaultAsync(d => d.MaKhachHang == maKhachHang && d.MaPhong == maPhong);

            if (existingReview != null)
                throw new ConflictException("Bạn đã đánh giá phòng này rồi.");

            var review = new DanhGia
            {
                MaDanhGia = await IdGenerator.NextAsync(_context.DanhGias.Select(d => d.MaDanhGia), "DG", 4),
                MaKhachHang = maKhachHang,
                MaPhong = maPhong,
                SoSao = soSao,
                NoiDung = noiDung,
                NgayDanhGia = DateTime.Now,
                AnDanh = anDanh
            };
            _context.DanhGias.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<IEnumerable<DanhGia>> GetReviewsByCustomerAsync(string maKhachHang)
            => await _context.DanhGias
                .Include(d => d.Phong)
                .Where(d => d.MaKhachHang == maKhachHang)
                .ToListAsync();

        public async Task<KhuyenMai> GetActivePromotionAsync(string code)
        {
            var promo = await _context.KhuyenMais.FindAsync(code?.Trim())
                ?? throw new NotFoundException("Mã khuyến mãi không tồn tại.");

            var today = DateTime.Today;
            if (promo.TrangThai != PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
                throw new BusinessException("Mã khuyến mãi đã hết hạn hoặc chưa được kích hoạt.");

            return promo;
        }
    }
}
