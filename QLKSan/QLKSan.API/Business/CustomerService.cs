using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        public CustomerService(ApplicationDbContext context) { _context = context; }

        public async Task<KhachHang> GetCustomerByAccountIdAsync(string maDangNhap)
            => await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.LienHeDiaChis)
                .FirstOrDefaultAsync(k => k.MaDangNhap == maDangNhap);

        public async Task<KhachHang> GetCustomerByUsernameAsync(string username)
            => await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .Include(k => k.LienHeDiaChis)
                .FirstOrDefaultAsync(k => k.TaiKhoan.TenDangNhap == username);

        public async Task<IEnumerable<PhieuDatPhong>> GetBookingsByCustomerAsync(string maKhachHang)
            => await _context.PhieuDatPhongs
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.HoaDon).ThenInclude(h => h.ThanhToans)
                .Where(p => p.MaKhachHang == maKhachHang)
                .OrderByDescending(p => p.NgayDat)
                .ToListAsync();

        public async Task<IEnumerable<Phong>> GetAvailableRoomsForBookingAsync()
            => await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Include(p => p.HinhAnhs)
                .Where(p => p.TrangThai == RoomStatus.Available)
                .ToListAsync();

        public async Task<string?> CreateOnlineBookingAsync(string maKhachHang, string maPhong, int soNguoi, DateTime checkIn, DateTime checkOut, string? maKhuyenMai = null)
        {
            try
            {
                var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (room == null || room.TrangThai != RoomStatus.Available) return "Phòng không tồn tại hoặc không trống.";

                if (room.LoaiPhong != null && soNguoi > room.LoaiPhong.SoNguoiToiDa)
                {
                    return $"Số lượng khách ({soNguoi}) vượt quá sức chứa tối đa của {room.LoaiPhong.TenLoai} ({room.LoaiPhong.SoNguoiToiDa} người lớn).";
                }

                // Validate promotion if provided
                if (!string.IsNullOrEmpty(maKhuyenMai))
                {
                    var promo = await _context.KhuyenMais.FindAsync(maKhuyenMai);
                    var today = DateTime.Today;
                    if (promo == null || promo.TrangThai != PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
                    {
                        maKhuyenMai = null; // Reset if invalid or expired
                    }
                }

                var booking = new PhieuDatPhong
                {
                    MaDatPhong = "PDP" + new Random().Next(1000000, 9999999),
                    MaKhachHang = maKhachHang,
                    NgayDat = DateTime.Now,
                    NgayCheckInDuKien = checkIn,
                    NgayCheckOutDuKien = checkOut,
                    SoLuongKhach = soNguoi,
                    TrangThai = BookingStatus.Pending,
                    TienCoc = 0
                };
                _context.PhieuDatPhongs.Add(booking);

                var detail = new ChiTietDatPhong
                {
                    MaCTDP = "CT" + new Random().Next(10000000, 99999999),
                    MaDatPhong = booking.MaDatPhong,
                    MaPhong = maPhong,
                    HinhThucThue = RentalType.Daily,
                    GiaThueThucTe = room.LoaiPhong.GiaThueNgay
                };
                _context.ChiTietDatPhongs.Add(detail);

                room.TrangThai = RoomStatus.Booked;
                await _context.SaveChangesAsync();
                return null;
            }
            catch { return "Đã xảy ra lỗi hệ thống khi đặt phòng."; }
        }

        public async Task<bool> SubmitReviewAsync(string maKhachHang, string maPhong, int soSao, string noiDung, bool anDanh)
        {
            try
            {
                // Validate that the customer has actually checked out of this room
                bool hasCheckedOutBooking = await _context.PhieuDatPhongs
                    .AnyAsync(b => b.MaKhachHang == maKhachHang 
                                && b.TrangThai == BookingStatus.CheckedOut
                                && b.ChiTietDatPhongs.Any(c => c.MaPhong == maPhong));
                
                if (!hasCheckedOutBooking)
                {
                    return false;
                }

                var existingReview = await _context.DanhGias
                    .FirstOrDefaultAsync(d => d.MaKhachHang == maKhachHang && d.MaPhong == maPhong);

                if (existingReview != null)
                {
                    return false; // Do not allow reviewing again
                }
                else
                {
                    var review = new DanhGia
                    {
                        MaDanhGia = "DG" + new Random().Next(10000000, 99999999),
                        MaKhachHang = maKhachHang,
                        MaPhong = maPhong,
                        SoSao = soSao,
                        NoiDung = noiDung,
                        NgayDanhGia = DateTime.Now,
                        AnDanh = anDanh
                    };
                    _context.DanhGias.Add(review);
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<DanhGia>> GetReviewsByCustomerAsync(string maKhachHang)
            => await _context.DanhGias
                .Where(d => d.MaKhachHang == maKhachHang)
                .ToListAsync();

        public async Task<KhuyenMai?> GetPromotionAsync(string code)
            => await _context.KhuyenMais.FindAsync(code);
    }
}
