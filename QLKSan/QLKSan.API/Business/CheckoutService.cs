using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class CheckoutService : ICheckoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly HotelPolicy _policy;
        private readonly IPricingService _pricing;

        public CheckoutService(ApplicationDbContext context, IOptions<HotelPolicy> policy, IPricingService pricing)
        {
            _context = context;
            _policy = policy.Value;
            _pricing = pricing;
        }

        public async Task<PhieuDatPhong> GetBookingDetailsByRoomAsync(string maPhong)
        {
            var detail = await _context.ChiTietDatPhongs
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .Include(c => c.PhieuDatPhong).ThenInclude(p => p.HoaDon).ThenInclude(h => h.KhuyenMai)
                .Include(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Where(c => c.MaPhong == maPhong && c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn)
                .OrderByDescending(c => c.PhieuDatPhong.NgayCheckInThucTe)
                .FirstOrDefaultAsync();

            var booking = detail?.PhieuDatPhong;
            if (booking != null && booking.HoaDon != null && booking.HoaDon.TrangThai == InvoiceStatus.Unpaid)
            {
                // Hoá đơn chưa thanh toán: tính lại theo thời điểm hiện tại (số đêm, phụ thu trả muộn, dịch vụ mới gọi)
                var promo = booking.HoaDon.KhuyenMai
                    ?? (string.IsNullOrEmpty(booking.HoaDon.MaKhuyenMai) ? null : await _context.KhuyenMais.FindAsync(booking.HoaDon.MaKhuyenMai));
                await ApplyBillingAsync(booking, booking.HoaDon, promo, DateTime.Now);
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

            if (booking == null)
                throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");

            var existingInvoice = await _context.HoaDons
                .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong);
            if (existingInvoice != null && existingInvoice.TrangThai != InvoiceStatus.Unpaid)
            {
                // Hoá đơn đã thanh toán/huỷ thì giữ nguyên
                existingInvoice.PhieuDatPhong = booking;
                return existingInvoice;
            }

            if (booking.TrangThai != BookingStatus.CheckedIn)
                throw new ConflictException("Chỉ lập hoá đơn cho khách đang lưu trú (đã check-in).");

            var promo = await ResolvePromotionAsync(booking, maKhuyenMai);

            var hoaDon = existingInvoice ?? new HoaDon
            {
                MaHoaDon = await IdGenerator.NextAsync(_context.HoaDons.Select(h => h.MaHoaDon), "HD", 4),
                MaDatPhong = maDatPhong,
                MaNhanVienLap = maNhanVienLap,
                NgayLapHoaDon = DateTime.Now,
                TrangThai = InvoiceStatus.Unpaid
            };
            await ApplyBillingAsync(booking, hoaDon, promo, DateTime.Now);
            // ThueVAT, TongTienCanThanhToan, SoTienConLai là computed columns trong SQL, sẽ tự tính

            if (existingInvoice == null) _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();

            hoaDon.PhieuDatPhong = booking;
            return hoaDon;
        }

        // Mã nhân viên nhập khi lập hoá đơn phải còn hiệu lực; nếu để trống thì dùng mã khách đã chọn lúc đặt online
        // (giữ quyền lợi đã cam kết với khách, không kiểm tra hạn lại)
        private async Task<KhuyenMai> ResolvePromotionAsync(PhieuDatPhong booking, string maKhuyenMai)
        {
            var code = maKhuyenMai?.Trim();
            if (string.IsNullOrEmpty(code) || string.Equals(code, booking.MaKhuyenMai, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrEmpty(booking.MaKhuyenMai) ? null : await _context.KhuyenMais.FindAsync(booking.MaKhuyenMai);
            }

            var promo = await _context.KhuyenMais.FindAsync(code);
            var today = DateTime.Today;
            if (promo == null || promo.TrangThai != PromotionStatus.Active || promo.NgayBatDau > today || promo.NgayKetThuc < today)
                throw new BusinessException("Mã khuyến mãi không tồn tại, đã hết hạn hoặc chưa được kích hoạt.");
            return promo;
        }

        private async Task ApplyBillingAsync(PhieuDatPhong booking, HoaDon hoaDon, KhuyenMai promo, DateTime thoiDiemTra)
        {
            var tongTienDichVu = booking.SuDungDichVus.Sum(s => s.ThanhTien);
            var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault();
            var giaGoc = chiTiet?.GiaThueThucTe ?? 0;
            var ngayNhan = booking.NgayCheckInThucTe ?? booking.NgayCheckInDuKien;

            // Giá từng đêm theo quy tắc giá mùa/cuối tuần/lễ
            var soDem = BillingCalculator.TinhSoDem(ngayNhan, thoiDiemTra);
            var giaTungDem = await _pricing.GetNightlyPricesAsync(chiTiet?.Phong?.MaLoai, giaGoc, ngayNhan, soDem);

            var result = BillingCalculator.Calculate(new BillingInput
            {
                GiaMotDem = giaGoc,
                GiaTungDem = giaTungDem.Select(n => n.Gia).ToList(),
                NgayNhanPhong = ngayNhan,
                ThoiDiemTraPhong = thoiDiemTra,
                TongTienDichVu = tongTienDichVu,
                LoaiGiam = promo?.LoaiGiam,
                GiaTriGiam = promo?.GiaTriGiam ?? 0,
                TienCoc = booking.TienCoc
            }, _policy);

            hoaDon.TongTienPhong = result.TienPhong;
            hoaDon.SoDem = result.SoDem;
            hoaDon.TongTienDichVu = tongTienDichVu;
            hoaDon.TienPhat = result.PhuThuTraMuon;
            hoaDon.MaKhuyenMai = promo?.MaKhuyenMai;
            hoaDon.GiamGiaKhuyenMai = result.GiamGia;
            hoaDon.TienCoc = booking.TienCoc;
        }

        public async Task<HoaDon> ProcessPaymentAsync(string maHoaDon, string phuongThucThanhToan, string maNhanVienThu)
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

                if (hoaDon == null)
                    throw new NotFoundException($"Không tìm thấy hóa đơn '{maHoaDon}'.");
                if (hoaDon.TrangThai == InvoiceStatus.Paid)
                    throw new ConflictException("Hóa đơn này đã được thanh toán.");

                // Khách trả phần còn lại sau khi trừ tiền cọc (cọc thừa thì không thu thêm)
                var soTienThu = Math.Max(0, hoaDon.SoTienConLai);

                var thanhToan = new ThanhToan
                {
                    MaThanhToan = await IdGenerator.NextAsync(_context.ThanhToans.Select(t => t.MaThanhToan), "TT", 4),
                    MaHoaDon = maHoaDon,
                    PhuongThuc = phuongThucThanhToan,
                    NgayThanhToan = DateTime.Now,
                    SoTienDaTra = soTienThu,
                    GhiChu = hoaDon.TienCoc > 0 ? $"Đã trừ tiền cọc {hoaDon.TienCoc:N0}đ" : null,
                    LoaiGiaoDich = PaymentType.Payment,
                    MaNhanVien = maNhanVienThu
                };
                _context.ThanhToans.Add(thanhToan);

                // Create Transaction History
                var lichSuGiaoDich = new LichSuGiaoDich
                {
                    MaGiaoDich = await IdGenerator.NextAsync(_context.LichSuGiaoDichs.Select(g => g.MaGiaoDich), "GD_", 6, 50),
                    MaThanhToan = thanhToan.MaThanhToan,
                    CongThanhToan = phuongThucThanhToan,
                    SoTien = soTienThu,
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
                        // Tạo công việc dọn phòng để lễ tân giao cho nhân viên buồng phòng
                        await HousekeepingTasks.EnsureOpenTaskAsync(_context, phong.MaPhong, hoaDon.MaDatPhong, HousekeepingTasks.DonSauTraPhong);
                    }
                }

                // Cộng điểm tích lũy: 1 điểm cho mỗi 10,000 VNĐ giá trị hoá đơn (tính cả phần đã cọc)
                if (hoaDon.PhieuDatPhong.KhachHang != null)
                {
                    int diemCongThem = (int)(hoaDon.TongTienCanThanhToan / 10000);
                    hoaDon.PhieuDatPhong.KhachHang.DiemTichLuy += diemCongThem;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                hoaDon.ThanhToans = new List<ThanhToan> { thanhToan };
                return hoaDon;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
