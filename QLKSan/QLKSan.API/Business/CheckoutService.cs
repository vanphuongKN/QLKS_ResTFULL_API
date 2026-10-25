using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public class CheckoutService : ICheckoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly HotelPolicy _policy;
        private readonly LoyaltyPolicy _loyalty;
        private readonly IPricingService _pricing;
        private readonly IPromotionService _promotions;

        public CheckoutService(ApplicationDbContext context, IOptions<HotelPolicy> policy, IOptions<LoyaltyPolicy> loyalty, IPricingService pricing,
            IPromotionService promotions)
        {
            _context = context;
            _policy = policy.Value;
            _loyalty = loyalty.Value;
            _pricing = pricing;
            _promotions = promotions;
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

            // Mã mới nhập tại quầy: kiểm tra đủ điều kiện (bật, còn hạn, hạng phòng, số đêm, số lượt) cho kỳ nghỉ này
            var maLoai = booking.ChiTietDatPhongs.FirstOrDefault()?.Phong?.MaLoai;
            var soDem = Math.Max(1, (booking.NgayCheckOutDuKien.Date - booking.NgayCheckInDuKien.Date).Days);
            return await _promotions.ValidateForStayAsync(code, maLoai, soDem, booking.MaDatPhong);
        }

        // Tính tiền tới thời điểm trả: giá từng đêm (quy tắc giá), phụ thu, khuyến mãi, ưu đãi hạng, cọc — dùng chung cho hoá đơn và bảng kê
        private async Task<(BillingResult Result, List<NightPriceDto> CacDem, HangThanhVien Hang)> ComputeAsync(PhieuDatPhong booking, KhuyenMai promo, DateTime thoiDiemTra)
        {
            var tongTienDichVu = booking.SuDungDichVus.Sum(s => s.ThanhTien);
            var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault();
            var giaGoc = chiTiet?.GiaThueThucTe ?? 0;
            var ngayNhan = booking.NgayCheckInThucTe ?? booking.NgayCheckInDuKien;

            // Giá từng đêm theo quy tắc giá mùa/cuối tuần/lễ
            var soDem = BillingCalculator.TinhSoDem(ngayNhan, thoiDiemTra);
            var giaTungDem = await _pricing.GetNightlyPricesAsync(chiTiet?.Phong?.MaLoai, giaGoc, ngayNhan, soDem);

            // Hạng thành viên của khách (xét theo điểm trọn đời) quyết định % giảm tiền phòng
            var khach = booking.KhachHang;
            var hang = khach == null ? null : _loyalty.HangTheoDiem(khach.DiemTichLuy + khach.DiemDaDoi);

            return (BillingCalculator.Calculate(new BillingInput
            {
                GiaMotDem = giaGoc,
                GiaTungDem = giaTungDem.Select(n => n.Gia).ToList(),
                NgayNhanPhong = ngayNhan,
                ThoiDiemTraPhong = thoiDiemTra,
                TongTienDichVu = tongTienDichVu,
                LoaiGiam = promo?.LoaiGiam,
                GiaTriGiam = promo?.GiaTriGiam ?? 0,
                PhanTramGiamThanhVien = hang?.PhanTramGiamPhong ?? 0,
                TienCoc = booking.TienCoc
            }, _policy), giaTungDem, hang);
        }

        private async Task ApplyBillingAsync(PhieuDatPhong booking, HoaDon hoaDon, KhuyenMai promo, DateTime thoiDiemTra)
        {
            var (result, _, hang) = await ComputeAsync(booking, promo, thoiDiemTra);
            var tongTienDichVu = booking.SuDungDichVus.Sum(s => s.ThanhTien);

            hoaDon.TongTienPhong = result.TienPhong;
            hoaDon.SoDem = result.SoDem;
            hoaDon.TongTienDichVu = tongTienDichVu;
            hoaDon.TienPhat = result.PhuThuTraMuon;
            hoaDon.MaKhuyenMai = promo?.MaKhuyenMai;
            hoaDon.GiamGiaKhuyenMai = result.GiamGia;
            hoaDon.GiamGiaThanhVien = result.GiamGiaThanhVien;
            hoaDon.HangThanhVien = hang?.Ten;
            hoaDon.TienCoc = booking.TienCoc;
        }

        public async Task<HoaDon> ProcessPaymentAsync(string maHoaDon, string phuongThucThanhToan, string maNhanVienThu, int diemSuDung = 0, IList<PaymentPart> cacKhoan = null)
        {
            var tachKhoan = cacKhoan != null && cacKhoan.Count > 0;
            if (tachKhoan)
            {
                if (cacKhoan.Count > 3) throw new BusinessException("Tách tối đa 3 khoản thanh toán.");
                if (cacKhoan.Any(k => !PaymentMethods.TaiQuay.Contains(k.PhuongThuc)))
                    throw new BusinessException("Phương thức của mỗi khoản phải là tiền mặt, thẻ tín dụng hoặc chuyển khoản.");
                if (cacKhoan.Any(k => k.SoTien <= 0)) throw new BusinessException("Mỗi khoản thanh toán phải lớn hơn 0.");
            }
            else if (string.IsNullOrWhiteSpace(phuongThucThanhToan))
            {
                throw new BusinessException("Vui lòng chọn phương thức thanh toán.");
            }

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
                var conPhaiTra = Math.Max(0, Math.Round(hoaDon.SoTienConLai, 0));
                var khachHang = hoaDon.PhieuDatPhong.KhachHang;

                // Đổi điểm: ghi thành một khoản thanh toán "Điểm thưởng", phần còn lại thu bằng phương thức đã chọn
                decimal tienDoiDiem = 0;
                if (diemSuDung > 0)
                {
                    if (khachHang == null)
                        throw new BusinessException("Phiếu đặt không gắn với khách hàng nên không đổi điểm được.");
                    if (diemSuDung < _loyalty.DiemDoiToiThieu)
                        throw new BusinessException($"Mỗi lần đổi tối thiểu {_loyalty.DiemDoiToiThieu} điểm.");
                    if (diemSuDung > khachHang.DiemTichLuy)
                        throw new BusinessException($"Khách chỉ còn {khachHang.DiemTichLuy} điểm.");
                    tienDoiDiem = (decimal)diemSuDung * _loyalty.GiaTriMotDiem;
                    if (tienDoiDiem > conPhaiTra)
                        throw new BusinessException($"Số điểm đổi vượt quá số tiền còn phải trả (tối đa {Math.Floor(conPhaiTra / _loyalty.GiaTriMotDiem):N0} điểm).");

                    khachHang.DiemTichLuy -= diemSuDung;
                    khachHang.DiemDaDoi += diemSuDung;
                    await PointLedger.AddAsync(_context, khachHang.MaKhachHang, -diemSuDung, PointLedger.DoiDiem,
                        $"Đổi điểm thanh toán hoá đơn {maHoaDon}", maHoaDon, maNhanVienThu);
                    var traDiem = new ThanhToan
                    {
                        MaThanhToan = await IdGenerator.NextAsync(_context.ThanhToans.Select(t => t.MaThanhToan), "TT", 4),
                        MaHoaDon = maHoaDon,
                        PhuongThuc = LoyaltyPolicy.PhuongThucDiem,
                        NgayThanhToan = DateTime.Now,
                        SoTienDaTra = tienDoiDiem,
                        GhiChu = $"Đổi {diemSuDung:N0} điểm",
                        LoaiGiaoDich = PaymentType.Payment,
                        MaNhanVien = maNhanVienThu
                    };
                    _context.ThanhToans.Add(traDiem);
                    // Lưu trước để mã thanh toán / giao dịch kế tiếp không trùng
                    await _context.SaveChangesAsync();
                }
                var soTienThu = conPhaiTra - tienDoiDiem;

                // Một khoản (phương thức đã chọn) hoặc tách nhiều khoản có tổng đúng bằng số tiền còn phải thu
                var cacPhan = tachKhoan && soTienThu > 0
                    ? cacKhoan.Select(k => (k.PhuongThuc, SoTien: Math.Round(k.SoTien, 0), GhiChu: string.IsNullOrWhiteSpace(k.GhiChu) ? null : k.GhiChu.Trim())).ToList()
                    : new List<(string PhuongThuc, decimal SoTien, string GhiChu)> { (tachKhoan ? cacKhoan[0].PhuongThuc : phuongThucThanhToan, soTienThu, null) };
                if (cacPhan.Sum(k => k.SoTien) != soTienThu)
                    throw new BusinessException($"Tổng các khoản ({cacPhan.Sum(k => k.SoTien):N0}đ) phải bằng số tiền còn phải thu {soTienThu:N0}đ.");

                var thanhToans = new List<ThanhToan>();
                foreach (var (phuongThuc, soTien, ghiChu) in cacPhan)
                {
                    var thanhToan = new ThanhToan
                    {
                        MaThanhToan = await IdGenerator.NextAsync(_context.ThanhToans.Select(t => t.MaThanhToan), "TT", 4),
                        MaHoaDon = maHoaDon,
                        PhuongThuc = phuongThuc,
                        NgayThanhToan = DateTime.Now,
                        SoTienDaTra = soTien,
                        GhiChu = ghiChu ?? (hoaDon.TienCoc > 0 ? $"Đã trừ tiền cọc {hoaDon.TienCoc:N0}đ" : null),
                        LoaiGiaoDich = PaymentType.Payment,
                        MaNhanVien = maNhanVienThu
                    };
                    _context.ThanhToans.Add(thanhToan);
                    _context.LichSuGiaoDichs.Add(new LichSuGiaoDich
                    {
                        MaGiaoDich = await IdGenerator.NextAsync(_context.LichSuGiaoDichs.Select(g => g.MaGiaoDich), "GD_", 6, 50),
                        MaThanhToan = thanhToan.MaThanhToan,
                        CongThanhToan = phuongThuc,
                        SoTien = soTien,
                        TrangThaiGiaoDich = TransactionStatus.Success,
                        ThoiGianGiaoDich = DateTime.Now,
                        NoiDungChuyenKhoan = "Khách thanh toán hóa đơn " + maHoaDon
                    });
                    thanhToans.Add(thanhToan);
                    // Lưu từng khoản để mã thanh toán / giao dịch kế tiếp không trùng
                    await _context.SaveChangesAsync();
                }

                // Cọc thừa (cọc > tổng hoá đơn): ghi dòng hoàn vào sổ cọc để đối soát tiền theo ca
                var cocThua = Math.Round(-hoaDon.SoTienConLai, 0);
                if (cocThua > 0)
                {
                    var booking = hoaDon.PhieuDatPhong;
                    _context.GiaoDichCocs.Add(new GiaoDichCoc
                    {
                        MaDatPhong = booking.MaDatPhong,
                        LoaiGiaoDich = PaymentType.Refund,
                        SoTien = cocThua,
                        PhuongThuc = cacPhan[0].PhuongThuc,
                        ThoiGian = DateTime.Now,
                        MaNhanVien = maNhanVienThu,
                        GhiChu = $"Hoàn cọc thừa khi thanh toán hoá đơn {maHoaDon}"
                    });
                    booking.TienCoc -= cocThua;
                }

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
                        await HousekeepingTasks.CloseDailyTasksAsync(_context, hoaDon.MaDatPhong, "khách đã trả phòng");
                        await _context.SaveChangesAsync();
                        await HousekeepingTasks.EnsureOpenTaskAsync(_context, phong.MaPhong, hoaDon.MaDatPhong, HousekeepingTasks.DonSauTraPhong);
                    }
                }

                // Cộng điểm tích lũy theo số tiền khách thực trả (tính cả phần đã cọc, không tính phần trả bằng điểm)
                if (khachHang != null)
                {
                    int diemCongThem = (int)((hoaDon.TongTienCanThanhToan - tienDoiDiem) / _loyalty.SoTienMotDiem);
                    khachHang.DiemTichLuy += diemCongThem;
                    if (diemCongThem > 0)
                        await PointLedger.AddAsync(_context, khachHang.MaKhachHang, diemCongThem, PointLedger.TichDiem,
                            $"Thanh toán hoá đơn {maHoaDon}", maHoaDon, maNhanVienThu);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                hoaDon.ThanhToans = thanhToans;
                return hoaDon;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<FolioDto> GetFolioAsync(string maDatPhong)
        {
            var booking = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .Include(p => p.HoaDon).ThenInclude(h => h.ThanhToans)
                .Include(p => p.HoaDon).ThenInclude(h => h.KhuyenMai)
                .Include(p => p.GiaoDichCocs).ThenInclude(g => g.NhanVien)
                .Include(p => p.LichSuDoiPhongs).ThenInclude(l => l.PhongCu)
                .Include(p => p.LichSuDoiPhongs).ThenInclude(l => l.PhongMoi)
                .Include(p => p.LichSuDoiPhongs).ThenInclude(l => l.NhanVien)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
            if (booking.TrangThai is not (BookingStatus.CheckedIn or BookingStatus.CheckedOut))
                throw new ConflictException("Chỉ có bảng kê cho khách đang lưu trú hoặc đã trả phòng.");

            var hoaDon = booking.HoaDon;
            var daChot = hoaDon != null && hoaDon.TrangThai != InvoiceStatus.Unpaid;
            var thoiDiem = daChot ? (booking.NgayCheckOutThucTe ?? hoaDon.NgayLapHoaDon) : DateTime.Now;
            // Hoá đơn chưa thanh toán: mã khuyến mãi trên hoá đơn; chưa có hoá đơn: mã khách chọn khi đặt (giữ quyền lợi đã cam kết)
            var maKm = hoaDon?.MaKhuyenMai ?? booking.MaKhuyenMai;
            var promo = string.IsNullOrEmpty(maKm) ? null : hoaDon?.KhuyenMai ?? await _context.KhuyenMais.FindAsync(maKm);
            var (r, cacDem, hang) = await ComputeAsync(booking, promo, thoiDiem);
            var phong = booking.ChiTietDatPhongs.FirstOrDefault()?.Phong;

            var dto = new FolioDto
            {
                MaDatPhong = booking.MaDatPhong,
                MaHoaDon = hoaDon?.MaHoaDon,
                TrangThaiHoaDon = hoaDon?.TrangThai,
                HoTenKhachHang = booking.KhachHang == null ? "" : $"{booking.KhachHang.Ho} {booking.KhachHang.Ten}".Trim(),
                SoPhong = phong?.SoPhong,
                TenLoaiPhong = phong?.LoaiPhong?.TenLoai,
                NgayNhan = booking.NgayCheckInThucTe ?? booking.NgayCheckInDuKien,
                NgayTraDuKien = booking.NgayCheckOutDuKien,
                ThoiDiemTinh = thoiDiem,
                CacDem = cacDem,
                DichVu = booking.SuDungDichVus.OrderBy(s => s.NgaySuDung).Select(s => s.ToDto()).ToList(),
                GiaoDichCocs = booking.GiaoDichCocs.OrderBy(g => g.ThoiGian).Select(g => g.ToDto()).ToList(),
                ThanhToans = hoaDon?.ThanhToans?.OrderBy(t => t.NgayThanhToan).Select(t => t.ToDto()).ToList() ?? new List<PaymentDto>(),
                LichSuDoiPhongs = booking.ToDto().LichSuDoiPhongs
            };
            if (daChot)
            {
                // Đã thanh toán: số liệu theo hoá đơn đã lưu
                dto.SoDem = hoaDon.SoDem;
                dto.TienPhong = hoaDon.TongTienPhong;
                dto.PhuThuTraMuon = hoaDon.TienPhat;
                dto.PhanTramPhuThu = r.PhanTramPhuThu;
                dto.TongTienDichVu = hoaDon.TongTienDichVu;
                dto.MaKhuyenMai = hoaDon.MaKhuyenMai;
                dto.GiamGiaKhuyenMai = hoaDon.GiamGiaKhuyenMai;
                dto.HangThanhVien = hoaDon.HangThanhVien;
                dto.GiamGiaThanhVien = hoaDon.GiamGiaThanhVien;
                dto.TienTruocThue = hoaDon.TongTienPhong + hoaDon.TongTienDichVu + hoaDon.TienPhat - hoaDon.GiamGiaKhuyenMai - hoaDon.GiamGiaThanhVien;
                dto.ThueVAT = Math.Round(hoaDon.ThueVAT, 0);
                dto.TongCong = Math.Round(hoaDon.TongTienCanThanhToan, 0);
                dto.TienCoc = hoaDon.TienCoc;
            }
            else
            {
                dto.SoDem = r.SoDem;
                dto.TienPhong = r.TienPhong;
                dto.PhuThuTraMuon = r.PhuThuTraMuon;
                dto.PhanTramPhuThu = r.PhanTramPhuThu;
                dto.TongTienDichVu = booking.SuDungDichVus.Sum(s => s.ThanhTien);
                dto.MaKhuyenMai = promo?.MaKhuyenMai;
                dto.GiamGiaKhuyenMai = r.GiamGia;
                dto.HangThanhVien = hang?.Ten;
                dto.GiamGiaThanhVien = r.GiamGiaThanhVien;
                dto.TienTruocThue = r.TienTruocThue;
                dto.ThueVAT = Math.Round(r.ThueVAT, 0);
                dto.TongCong = Math.Round(r.TongTien, 0);
                dto.TienCoc = booking.TienCoc;
            }
            dto.DaThanhToan = dto.ThanhToans.Where(t => t.LoaiGiaoDich == PaymentType.Payment).Sum(t => t.SoTienDaTra);
            dto.ConPhaiThu = daChot ? 0 : dto.TongCong - dto.TienCoc;
            return dto;
        }

        public async Task<HoaDon> SetCompanyInfoAsync(string maHoaDon, InvoiceCompanyRequest request)
        {
            var hoaDon = await _context.HoaDons.Include(h => h.ThanhToans).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                ?? throw new NotFoundException($"Không tìm thấy hóa đơn '{maHoaDon}'.");
            if (hoaDon.TrangThai == InvoiceStatus.Cancelled)
                throw new ConflictException("Hoá đơn đã huỷ.");

            static string Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            var mst = Clean(request.MaSoThue);
            if (mst != null && Clean(request.TenCongTy) == null)
                throw new BusinessException("Vui lòng nhập tên công ty kèm mã số thuế.");
            if (mst == null && Clean(request.TenCongTy) != null)
                throw new BusinessException("Vui lòng nhập mã số thuế của công ty.");

            // Không có mã số thuế: hoá đơn cá nhân, xoá thông tin công ty
            hoaDon.MaSoThue = mst;
            hoaDon.TenCongTy = mst == null ? null : Clean(request.TenCongTy);
            hoaDon.DiaChiCongTy = mst == null ? null : Clean(request.DiaChiCongTy);
            hoaDon.EmailHoaDon = mst == null ? null : Clean(request.EmailHoaDon);
            await _context.SaveChangesAsync();
            return hoaDon;
        }
    }
}
