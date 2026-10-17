using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;
using QLKSan.ViewModels;
using QLKSan.Extensions;

namespace QLKSan.Business
{
    public class ReceptionService : IReceptionService
    {
        private readonly ApplicationDbContext _context;

        public ReceptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Phong>> GetAllRoomsAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.SoPhong)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetOnlineBookingsAsync()
        {
            return await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                .Where(p => p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved)
                .OrderBy(p => p.NgayCheckInDuKien)
                .ToListAsync();
        }

        public async Task<bool> ApproveOnlineBookingAsync(string maDatPhong)
        {
            var booking = await _context.PhieuDatPhongs.FindAsync(maDatPhong);
            if (booking == null || booking.TrangThai != BookingStatus.Pending) return false;

            booking.TrangThai = BookingStatus.Approved;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckInOnlineBookingAsync(string maDatPhong, string maNhanVien)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.PhieuDatPhongs
                    .Include(p => p.ChiTietDatPhongs)
                    .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong);

                if (booking == null || (booking.TrangThai != BookingStatus.Pending && booking.TrangThai != BookingStatus.Approved)) 
                    return false;

                booking.TrangThai = BookingStatus.CheckedIn;
                booking.NgayCheckInThucTe = DateTime.Now;
                booking.MaNhanVien = maNhanVien;

                var maPhong = booking.ChiTietDatPhongs.FirstOrDefault()?.MaPhong;
                if (!string.IsNullOrEmpty(maPhong))
                {
                    var room = await _context.Phongs.FindAsync(maPhong);
                    if (room != null) room.TrangThai = RoomStatus.Occupied;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine("CheckIn Error: " + ex.ToString());
                throw; // Re-throw để hiển thị trang lỗi chi tiết trên trình duyệt trong môi trường Dev
            }
        }

        public async Task<IEnumerable<Phong>> GetAvailableRoomsAsync()
        {
            return await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai == RoomStatus.Available)
                .ToListAsync();
        }

        public async Task<KhachHang> GetCustomerByCccdAsync(string cccd)
        {
            return await _context.KhachHangs.FirstOrDefaultAsync(k => k.CCCD_Passport == cccd);
        }

        public async Task<TaiKhoan> GetAccountByUsernameAsync(string username)
        {
            return await _context.TaiKhoans
                .Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);
        }

        public async Task<string> ProcessCheckInAsync(CheckInViewModel model, string maNhanVien)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Resolve Customer
                var customer = await GetCustomerByCccdAsync(model.CCCD_Passport);
                if (customer == null)
                {
                    var rnd = new Random();
                    string maKH = "KH" + rnd.Next(10, 99).ToString("D2");
                    // Ensure unique
                    while (await _context.KhachHangs.AnyAsync(k => k.MaKhachHang == maKH))
                    {
                        maKH = "KH" + rnd.Next(100, 999).ToString();
                    }

                    customer = new KhachHang
                    {
                        MaKhachHang = maKH,
                        Ho = model.Ho,
                        Ten = model.Ten,
                        CCCD_Passport = model.CCCD_Passport,
                        NgaySinh = DateTime.Now.AddYears(-25)
                    };
                    _context.KhachHangs.Add(customer);

                    string maLH = "LH" + rnd.Next(10, 99).ToString("D2");
                    while (await _context.LienHeDiaChis.AnyAsync(l => l.MaLienHe == maLH))
                    {
                        maLH = "LH" + rnd.Next(100, 999).ToString();
                    }

                    var contact = new LienHeDiaChi
                    {
                        MaLienHe = maLH,
                        MaKhachHang = customer.MaKhachHang,
                        SDT = model.SDT
                    };
                    _context.LienHeDiaChis.Add(contact);
                    
                    // Save customer first
                    await _context.SaveChangesAsync();
                }

                // 2. Resolve Room
                var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == model.MaPhong);
                if (room == null || room.TrangThai != RoomStatus.Available)
                {
                    await transaction.RollbackAsync();
                    return "Phòng không khả dụng hoặc đã được đặt.";
                }

                if (room.LoaiPhong != null && model.SoNguoi > room.LoaiPhong.SoNguoiToiDa)
                {
                    await transaction.RollbackAsync();
                    return $"Số lượng khách ({model.SoNguoi}) vượt quá sức chứa tối đa của {room.LoaiPhong.TenLoai} ({room.LoaiPhong.SoNguoiToiDa} người lớn).";
                }

                // 3. Create Booking (PhieuDatPhong)
                var rnd2 = new Random();
                string maDP = "DP" + rnd2.Next(10, 99).ToString("D2");
                while (await _context.PhieuDatPhongs.AnyAsync(p => p.MaDatPhong == maDP))
                {
                    maDP = "DP" + rnd2.Next(100, 999).ToString();
                }

                var tienCoc = ((model.NgayCheckOut - model.NgayCheckIn).Days) * room.LoaiPhong.GiaThueNgay * 0.5m;
                if (tienCoc <= 0) tienCoc = room.LoaiPhong.GiaThueNgay * 0.5m;

                var booking = new PhieuDatPhong
                {
                    MaDatPhong = maDP,
                    MaKhachHang = customer.MaKhachHang,
                    MaNhanVien = maNhanVien,
                    NgayDat = DateTime.Now,
                    NgayCheckInDuKien = model.NgayCheckIn,
                    NgayCheckOutDuKien = model.NgayCheckOut,
                    NgayCheckInThucTe = DateTime.Now,
                    SoLuongKhach = model.SoNguoi,
                    TrangThai = BookingStatus.CheckedIn,
                    TienCoc = tienCoc
                };
                _context.PhieuDatPhongs.Add(booking);

                // 4. Create Booking Detail (ChiTietDatPhong)
                string maCT = "CT" + rnd2.Next(10, 99).ToString("D2");
                while (await _context.ChiTietDatPhongs.AnyAsync(c => c.MaCTDP == maCT))
                {
                    maCT = "CT" + rnd2.Next(100, 999).ToString();
                }

                var bookingDetail = new ChiTietDatPhong
                {
                    MaCTDP = maCT,
                    MaDatPhong = booking.MaDatPhong,
                    MaPhong = room.MaPhong,
                    HinhThucThue = RentalType.Daily,
                    GiaThueThucTe = room.LoaiPhong.GiaThueNgay
                };
                _context.ChiTietDatPhongs.Add(bookingDetail);

                await _context.SaveChangesAsync();

                // 5. Update Room Status (trigger may handle this, but do it manually as backup)
                room.TrangThai = RoomStatus.Occupied;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return null; // null = success
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Return the actual error
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return $"Lỗi: {innerMsg}";
            }
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetAllBookingsAsync(string search = null)
        {
            var query = _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.HoaDon)
                .AsQueryable();

            var result = await query.OrderByDescending(p => p.NgayDat).ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                result = result.Where(p => 
                    (p.MaDatPhong?.ContainsIgnoreCaseAndAccents(search) ?? false) ||
                    ((p.KhachHang.Ho + " " + p.KhachHang.Ten).ContainsIgnoreCaseAndAccents(search)) ||
                    p.ChiTietDatPhongs.Any(c => c.Phong.SoPhong.ContainsIgnoreCaseAndAccents(search)))
                .ToList();
            }

            return result;
        }

        public async Task<IEnumerable<KhachHang>> GetAllCustomersAsync(string search = null)
        {
            var query = _context.KhachHangs
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                .AsQueryable();

            var result = await query.OrderBy(k => k.Ten).ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                result = result.Where(k => 
                    ((k.Ho + " " + k.Ten).ContainsIgnoreCaseAndAccents(search)) ||
                    (k.CCCD_Passport?.ContainsIgnoreCaseAndAccents(search) ?? false) ||
                    k.LienHeDiaChis.Any(l => l.SDT.ContainsIgnoreCaseAndAccents(search)))
                .ToList();
            }

            return result;
        }

        public async Task<KhachHang> GetCustomerDetailsAsync(string id)
        {
            return await _context.KhachHangs
                .Include(k => k.LienHeDiaChis)
                .Include(k => k.PhieuDatPhongs)
                    .ThenInclude(p => p.ChiTietDatPhongs)
                        .ThenInclude(c => c.Phong)
                            .ThenInclude(r => r.LoaiPhong)
                .FirstOrDefaultAsync(k => k.MaKhachHang == id);
        }
    }
}
