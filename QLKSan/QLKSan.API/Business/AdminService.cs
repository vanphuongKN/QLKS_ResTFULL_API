using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;
using QLKSan.Models.DTOs;

namespace QLKSan.Business
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        public AdminService(ApplicationDbContext context) { _context = context; }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.HoaDons
                .Where(h => h.TrangThai == InvoiceStatus.Paid)
                .SumAsync(h => h.TongTienCanThanhToan);
        }

        public async Task<List<decimal>> GetWeeklyRevenueAsync()
        {
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = today.AddDays(-1 * diff).Date;
            var endOfWeek = startOfWeek.AddDays(7).Date;

            var invoicesThisWeek = await _context.HoaDons
                .Where(h => h.TrangThai == InvoiceStatus.Paid && h.NgayLapHoaDon >= startOfWeek && h.NgayLapHoaDon < endOfWeek)
                .ToListAsync();

            var weeklyRevenue = new decimal[7];
            foreach(var inv in invoicesThisWeek)
            {
                int dayIndex = (int)inv.NgayLapHoaDon.DayOfWeek - 1;
                if (dayIndex == -1) dayIndex = 6; // Sunday
                weeklyRevenue[dayIndex] += inv.TongTienCanThanhToan;
            }

            return weeklyRevenue.ToList();
        }

        public async Task<List<MonthlyRevenueDto>> GetYearlyRevenueHistoryAsync(int year)
        {
            var invoices = await _context.HoaDons
                .Where(h => h.TrangThai == InvoiceStatus.Paid && h.NgayLapHoaDon.Year == year)
                .ToListAsync();

            var result = new List<MonthlyRevenueDto>();

            for (int m = 1; m <= 12; m++)
            {
                var monthInvoices = invoices.Where(i => i.NgayLapHoaDon.Month == m).ToList();
                int daysInMonth = DateTime.DaysInMonth(year, m);

                var monthlyDto = new MonthlyRevenueDto
                {
                    Month = m,
                    TotalRevenue = monthInvoices.Sum(i => i.TongTienCanThanhToan)
                };

                // Create 4 weeks
                for (int w = 1; w <= 4; w++)
                {
                    int startDay = (w - 1) * 7 + 1;
                    int endDay = w == 4 ? daysInMonth : w * 7;

                    var weekInvoices = monthInvoices
                        .Where(i => i.NgayLapHoaDon.Day >= startDay && i.NgayLapHoaDon.Day <= endDay)
                        .ToList();

                    var weeklyDto = new WeeklyRevenueDto
                    {
                        WeekLabel = $"Tuần {w}",
                        StartDate = new DateTime(year, m, startDay),
                        EndDate = new DateTime(year, m, endDay),
                        TotalRevenue = weekInvoices.Sum(i => i.TongTienCanThanhToan)
                    };

                    // Create Days
                    for (int d = startDay; d <= endDay; d++)
                    {
                        var currentDate = new DateTime(year, m, d);
                        var dayTotal = weekInvoices
                            .Where(i => i.NgayLapHoaDon.Date == currentDate)
                            .Sum(i => i.TongTienCanThanhToan);

                        weeklyDto.Days.Add(new DailyRevenueDto
                        {
                            Date = currentDate,
                            Revenue = dayTotal
                        });
                    }

                    monthlyDto.Weeks.Add(weeklyDto);
                }

                result.Add(monthlyDto);
            }

            return result;
        }

        public async Task<int> GetTotalBookingsAsync()
            => await _context.PhieuDatPhongs.CountAsync();

        public async Task<int> GetTotalCustomersAsync()
            => await _context.KhachHangs.CountAsync();

        // Room CRUD
        public async Task<IEnumerable<Phong>> GetAllRoomsAsync()
            => await _context.Phongs.Include(p => p.LoaiPhong).OrderBy(p => p.SoPhong).ToListAsync();

        public async Task<IEnumerable<LoaiPhong>> GetAllRoomTypesAsync()
            => await _context.LoaiPhongs.ToListAsync();

        public async Task<string> GenerateNextRoomTypeIdAsync()
        {
            var ids = await _context.LoaiPhongs.Select(s => s.MaLoai).ToListAsync();
            int max = 0;
            foreach (var id in ids)
            {
                if (id.StartsWith("LP") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return "LP" + (max + 1).ToString("D2");
        }

        public async Task<bool> AddRoomTypeAsync(LoaiPhong roomType)
        {
            try { _context.LoaiPhongs.Add(roomType); await _context.SaveChangesAsync(); return true; }
            catch { return false; }
        }

        public async Task<bool> UpdateRoomTypeAsync(LoaiPhong roomType)
        {
            try
            {
                var existing = await _context.LoaiPhongs.FindAsync(roomType.MaLoai);
                if (existing == null) return false;
                existing.TenLoai = roomType.TenLoai;
                existing.SoNguoiToiDa = roomType.SoNguoiToiDa;
                existing.GiaThueNgay = roomType.GiaThueNgay;
                existing.GiaThueGio = roomType.GiaThueGio;
                existing.MoTa = roomType.MoTa;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteRoomTypeAsync(string maLoai)
        {
            try
            {
                var roomType = await _context.LoaiPhongs.FindAsync(maLoai);
                if (roomType == null) return false;
                
                // Prevent deletion if rooms are associated
                bool hasRooms = await _context.Phongs.AnyAsync(p => p.MaLoai == maLoai);
                if (hasRooms) return false;

                _context.LoaiPhongs.Remove(roomType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> AddRoomAsync(Phong room)
        {
            try { _context.Phongs.Add(room); await _context.SaveChangesAsync(); return true; }
            catch { return false; }
        }

        public async Task<bool> UpdateRoomAsync(Phong room)
        {
            try
            {
                var existing = await _context.Phongs.FindAsync(room.MaPhong);
                if (existing == null) return false;
                existing.SoPhong = room.SoPhong;
                existing.MaLoai = room.MaLoai;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteRoomAsync(string maPhong)
        {
            try
            {
                var room = await _context.Phongs.FindAsync(maPhong);
                if (room == null) return false;
                _context.Phongs.Remove(room);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // Service CRUD
        public async Task<IEnumerable<DichVu>> GetAllServicesAsync()
            => await _context.DichVus.ToListAsync();

        public async Task<string> GenerateNextServiceIdAsync()
        {
            var ids = await _context.DichVus.Select(s => s.MaDichVu).ToListAsync();
            int max = 0;
            foreach (var id in ids)
            {
                if (id.StartsWith("DV") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return "DV" + (max + 1).ToString("D3");
        }

        public async Task<bool> AddServiceAsync(DichVu service)
        {
            try { _context.DichVus.Add(service); await _context.SaveChangesAsync(); return true; }
            catch { return false; }
        }

        public async Task<bool> UpdateServiceAsync(DichVu service)
        {
            try
            {
                var existing = await _context.DichVus.FindAsync(service.MaDichVu);
                if (existing == null) return false;
                existing.TenDichVu = service.TenDichVu;
                existing.GiaDichVu = service.GiaDichVu;
                existing.DonViTinh = service.DonViTinh;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteServiceAsync(string maDichVu)
        {
            try
            {
                var s = await _context.DichVus.FindAsync(maDichVu);
                if (s == null) return false;
                _context.DichVus.Remove(s);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> ToggleServiceStatusAsync(string maDichVu)
        {
            try
            {
                var s = await _context.DichVus.FindAsync(maDichVu);
                if (s == null) return false;
                
                s.TrangThai = s.TrangThai == ServiceStatus.Available ? ServiceStatus.Unavailable : ServiceStatus.Available;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // Staff
        public async Task<IEnumerable<NhanVien>> GetAllStaffAsync()
            => await _context.NhanViens.Include(n => n.TaiKhoan).ToListAsync();

        public async Task<string> GenerateNextStaffIdAsync()
        {
            var ids = await _context.NhanViens.Select(s => s.MaNhanVien).ToListAsync();
            int max = 0;
            foreach (var id in ids)
            {
                if (id.StartsWith("NV") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return "NV" + (max + 1).ToString("D3");
        }

        public async Task<bool> AddStaffAsync(NhanVien staff, string tenDangNhap, string matKhau)
        {
            try
            {
                var account = new TaiKhoan
                {
                    MaDangNhap = "TK" + new Random().Next(10000000, 99999999),
                    TenDangNhap = tenDangNhap,
                    MatKhau = matKhau,
                    VaiTro = Role.Receptionist
                };
                _context.TaiKhoans.Add(account);
                staff.MaDangNhap = account.MaDangNhap;
                _context.NhanViens.Add(staff);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateStaffAsync(NhanVien staff)
        {
            try
            {
                var existing = await _context.NhanViens.FindAsync(staff.MaNhanVien);
                if (existing == null) return false;
                existing.TenNhanVien = staff.TenNhanVien;
                existing.ChucVu = staff.ChucVu;
                existing.SDT = staff.SDT;
                existing.Email = staff.Email;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteStaffAsync(string maNhanVien)
        {
            try
            {
                var staff = await _context.NhanViens.FindAsync(maNhanVien);
                if (staff == null) return false;
                
                // Remove associated account if exists
                if (!string.IsNullOrEmpty(staff.MaDangNhap))
                {
                    var account = await _context.TaiKhoans.FindAsync(staff.MaDangNhap);
                    if (account != null) _context.TaiKhoans.Remove(account);
                }

                _context.NhanViens.Remove(staff);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // Promotions
        public async Task<IEnumerable<KhuyenMai>> GetAllPromotionsAsync()
            => await _context.KhuyenMais.ToListAsync();

        public async Task<bool> AddPromotionAsync(KhuyenMai promo)
        {
            try { _context.KhuyenMais.Add(promo); await _context.SaveChangesAsync(); return true; }
            catch { return false; }
        }

        // Reviews
        public async Task<IEnumerable<DanhGia>> GetAllReviewsAsync()
            => await _context.DanhGias.Include(d => d.KhachHang).Include(d => d.Phong).OrderByDescending(d => d.NgayDanhGia).ToListAsync();
    }
}
