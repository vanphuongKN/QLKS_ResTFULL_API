using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class CheckoutService : ICheckoutService
    {
        private readonly ApplicationDbContext _context;

        public CheckoutService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PhieuDatPhong> GetBookingDetailsByRoomAsync(string maPhong)
        {
            var detail = await _context.ChiTietDatPhongs
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.HoaDon).ThenInclude(h => h.KhuyenMai)
                .Include(c => c.Phong)
                .Where(c => c.MaPhong == maPhong && c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn)
                .OrderByDescending(c => c.PhieuDatPhong.NgayCheckInThucTe)
                .FirstOrDefaultAsync();

            var booking = detail?.PhieuDatPhong;
            if (booking != null && booking.HoaDon != null && booking.HoaDon.TrangThai == InvoiceStatus.Unpaid)
            {
                // Auto-recalculate unpaid invoice totals in real-time
                decimal tongTienPhong = 0;
                var days = (booking.NgayCheckOutDuKien.Date - booking.NgayCheckInDuKien.Date).Days;
                if (days <= 0) days = 1;

                var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault();
                if (chiTiet != null)
                {
                    tongTienPhong = chiTiet.GiaThueThucTe * days;
                }

                decimal tongTienDichVu = booking.SuDungDichVus.Sum(s => s.ThanhTien);

                // Fetch and calculate promotion discount
                string? maKhuyenMai = booking.HoaDon.MaKhuyenMai;
                decimal giamGiaKhuyenMai = 0;
                if (!string.IsNullOrEmpty(maKhuyenMai))
                {
                    var promo = booking.HoaDon.KhuyenMai ?? await _context.KhuyenMais.FindAsync(maKhuyenMai);
                    if (promo != null)
                    {
                        if (promo.LoaiGiam == "Phần trăm")
                        {
                            giamGiaKhuyenMai = tongTienPhong * (promo.GiaTriGiam / 100m);
                        }
                        else
                        {
                            giamGiaKhuyenMai = promo.GiaTriGiam;
                        }
                        giamGiaKhuyenMai = Math.Min(tongTienPhong + tongTienDichVu, giamGiaKhuyenMai);
                    }
                }

                booking.HoaDon.TongTienPhong = tongTienPhong;
                booking.HoaDon.TongTienDichVu = tongTienDichVu;
                booking.HoaDon.MaKhuyenMai = maKhuyenMai;
                booking.HoaDon.GiamGiaKhuyenMai = giamGiaKhuyenMai;
                // ThueVAT và TongTienCanThanhToan là computed columns trong SQL, không cần gán

                await _context.SaveChangesAsync();
            }

            return booking;
        }

        public async Task<HoaDon> GenerateInvoiceAsync(string maDatPhong, string maNhanVienLap, string? maKhuyenMai = null)
        {
            var booking = await _context.PhieuDatPhongs
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                .Include(p => p.SuDungDichVus)
                .Include(p => p.KhachHang)
                .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong);

            if (booking == null) return null;

            // Check if a promotion code is specified
            if (maKhuyenMai != null)
            {
                // Will be applied to the HoaDon, not to PhieuDatPhong
            }

            // Check if invoice already exists for this booking
            var existingInvoice = await _context.HoaDons
                .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong);

            // Tính tiền phòng
            decimal tongTienPhong = 0;
            var days = (booking.NgayCheckOutDuKien.Date - booking.NgayCheckInDuKien.Date).Days;
            if (days <= 0) days = 1;

            var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault();
            if (chiTiet != null)
            {
                tongTienPhong = chiTiet.GiaThueThucTe * days;
            }

            // Tính tiền dịch vụ
            decimal tongTienDichVu = booking.SuDungDichVus.Sum(s => s.ThanhTien);

            // Fetch and calculate promotion discount
            string? activePromoCode = maKhuyenMai?.Trim();
            decimal giamGiaKhuyenMai = 0;
            if (!string.IsNullOrEmpty(activePromoCode))
            {
                var promo = await _context.KhuyenMais.FindAsync(activePromoCode);
                var today = DateTime.Today;
                if (promo != null && promo.TrangThai == PromotionStatus.Active && promo.NgayBatDau <= today && promo.NgayKetThuc >= today)
                {
                    if (promo.LoaiGiam == "Phần trăm")
                    {
                        giamGiaKhuyenMai = tongTienPhong * (promo.GiaTriGiam / 100m);
                    }
                    else
                    {
                        giamGiaKhuyenMai = promo.GiaTriGiam;
                    }
                    giamGiaKhuyenMai = Math.Min(tongTienPhong + tongTienDichVu, giamGiaKhuyenMai);
                }
                else
                {
                    throw new ArgumentException("Mã khuyến mãi không tồn tại, đã hết hạn hoặc chưa được kích hoạt.");
                }
            }

            decimal subtotal = tongTienPhong + tongTienDichVu;
            decimal preTaxNet = Math.Max(0, subtotal - giamGiaKhuyenMai);
            decimal thueVat = preTaxNet * 0.08m;
            decimal tongTienCanThanhToan = Math.Max(0, preTaxNet + thueVat - booking.TienCoc);

            if (existingInvoice != null)
            {
                // Update unpaid invoice totals with latest calculations
                if (existingInvoice.TrangThai == InvoiceStatus.Unpaid)
                {
                    existingInvoice.TongTienPhong = tongTienPhong;
                    existingInvoice.TongTienDichVu = tongTienDichVu;
                    existingInvoice.MaKhuyenMai = activePromoCode;
                    existingInvoice.GiamGiaKhuyenMai = giamGiaKhuyenMai;
                    existingInvoice.TienPhat = 0;
                    // ThueVAT và TongTienCanThanhToan là computed columns trong SQL, không cần gán
                    
                    await _context.SaveChangesAsync();
                }
                existingInvoice.PhieuDatPhong = booking;
                return existingInvoice;
            }

            var hoaDon = new HoaDon
            {
                MaHoaDon = "HD" + new Random().Next(10000000, 99999999).ToString(),
                MaDatPhong = maDatPhong,
                MaNhanVienLap = maNhanVienLap,
                NgayLapHoaDon = DateTime.Now,
                TongTienPhong = tongTienPhong,
                TongTienDichVu = tongTienDichVu,
                MaKhuyenMai = activePromoCode,
                GiamGiaKhuyenMai = giamGiaKhuyenMai,
                TienPhat = 0,
                // ThueVAT và TongTienCanThanhToan là computed columns trong SQL, sẽ tự tính
                TrangThai = InvoiceStatus.Unpaid
            };

            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            hoaDon.PhieuDatPhong = booking;
            return hoaDon;
        }

        public async Task<bool> ProcessPaymentAsync(string maHoaDon, string phuongThucThanhToan)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hoaDon = await _context.HoaDons
                    .Include(h => h.PhieuDatPhong)
                    .ThenInclude(p => p.ChiTietDatPhongs)
                    .Include(h => h.PhieuDatPhong)
                    .ThenInclude(p => p.KhachHang)
                    .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

                if (hoaDon == null || hoaDon.TrangThai == InvoiceStatus.Paid) return false;

                // Create Payment
                var thanhToan = new ThanhToan
                {
                    MaThanhToan = "TT" + new Random().Next(10000000, 99999999).ToString(),
                    MaHoaDon = maHoaDon,
                    PhuongThuc = phuongThucThanhToan,
                    NgayThanhToan = DateTime.Now,
                    SoTienDaTra = hoaDon.TongTienCanThanhToan
                };
                _context.ThanhToans.Add(thanhToan);

                // Create Transaction History
                var lichSuGiaoDich = new LichSuGiaoDich
                {
                    MaGiaoDich = "GD_" + new Random().Next(100000, 999999).ToString(),
                    MaThanhToan = thanhToan.MaThanhToan,
                    CongThanhToan = phuongThucThanhToan,
                    SoTien = hoaDon.TongTienCanThanhToan,
                    TrangThaiGiaoDich = TransactionStatus.Success,
                    ThoiGianGiaoDich = DateTime.Now,
                    NoiDungChuyenKhoan = "Khách thanh toán hóa đơn " + maHoaDon
                };
                _context.LichSuGiaoDichs.Add(lichSuGiaoDich);

                // Update Invoice
                hoaDon.TrangThai = InvoiceStatus.Paid;

                // Update Booking
                hoaDon.PhieuDatPhong.TrangThai = BookingStatus.CheckedOut;
                hoaDon.PhieuDatPhong.NgayCheckOutThucTe = DateTime.Now;

                // Cập nhật trạng thái phòng bằng code (Phòng ngừa Trigger không chạy)
                var chiTiet = hoaDon.PhieuDatPhong.ChiTietDatPhongs.FirstOrDefault();
                if (chiTiet != null)
                {
                    var phong = await _context.Phongs.FindAsync(chiTiet.MaPhong);
                    if (phong != null)
                    {
                        phong.TrangThai = RoomStatus.Cleaning;
                    }
                }

                // Cộng điểm tích lũy: 1 điểm cho mỗi 10,000 VNĐ thanh toán
                if (hoaDon.PhieuDatPhong.KhachHang != null)
                {
                    int diemCongThem = (int)(hoaDon.TongTienCanThanhToan / 10000);
                    hoaDon.PhieuDatPhong.KhachHang.DiemTichLuy += diemCongThem;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
