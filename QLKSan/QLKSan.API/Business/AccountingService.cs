using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Extensions;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class AccountingService : IAccountingService
    {
        private readonly ApplicationDbContext _context;

        public AccountingService(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<HoaDon> InvoiceQuery()
            => _context.HoaDons
                .Include(h => h.NhanVienLap)
                .Include(h => h.ThanhToans)
                .Include(h => h.PhieuDatPhong).ThenInclude(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(h => h.PhieuDatPhong).ThenInclude(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(h => h.PhieuDatPhong).ThenInclude(p => p.SuDungDichVus).ThenInclude(s => s.DichVu);

        private static decimal DaThu(HoaDon h) => h.ThanhToans?.Where(t => t.LoaiGiaoDich == PaymentType.Payment).Sum(t => t.SoTienDaTra) ?? 0;
        private static decimal DaHoan(HoaDon h) => h.ThanhToans?.Where(t => t.LoaiGiaoDich == PaymentType.Refund).Sum(t => t.SoTienDaTra) ?? 0;

        // Phần khách trả bằng điểm thưởng: không hoàn thành tiền
        private static decimal TraBangDiem(HoaDon h) => h.ThanhToans?
            .Where(t => t.LoaiGiaoDich == PaymentType.Payment && t.PhuongThuc == LoyaltyPolicy.PhuongThucDiem).Sum(t => t.SoTienDaTra) ?? 0;

        // Ngày ghi nhận doanh thu: ngày thanh toán (hoặc ngày lập nếu không có giao dịch thu)
        private static DateTime NgayGhiNhan(HoaDon h)
            => h.ThanhToans?.Where(t => t.LoaiGiaoDich == PaymentType.Payment).Select(t => (DateTime?)t.NgayThanhToan).Min() ?? h.NgayLapHoaDon;

        public async Task<List<InvoiceListItemDto>> GetInvoicesAsync(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search)
        {
            var query = InvoiceQuery();
            if (tuNgay != null) query = query.Where(h => h.NgayLapHoaDon >= tuNgay.Value.Date);
            if (denNgay != null) query = query.Where(h => h.NgayLapHoaDon < denNgay.Value.Date.AddDays(1));
            if (trangThai != null) query = query.Where(h => h.TrangThai == trangThai.Value);

            var invoices = await query.OrderByDescending(h => h.NgayLapHoaDon).ToListAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                invoices = invoices.Where(h =>
                        h.MaHoaDon.ContainsIgnoreCaseAndAccents(keyword)
                     || h.MaDatPhong.ContainsIgnoreCaseAndAccents(keyword)
                     || $"{h.PhieuDatPhong?.KhachHang?.Ho} {h.PhieuDatPhong?.KhachHang?.Ten}".ContainsIgnoreCaseAndAccents(keyword))
                    .ToList();
            }

            return invoices.Select(h => new InvoiceListItemDto
            {
                MaHoaDon = h.MaHoaDon,
                MaDatPhong = h.MaDatPhong,
                HoTenKhachHang = $"{h.PhieuDatPhong?.KhachHang?.Ho} {h.PhieuDatPhong?.KhachHang?.Ten}".Trim(),
                SoPhong = h.PhieuDatPhong?.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.SoPhong,
                NgayLapHoaDon = h.NgayLapHoaDon,
                TongTienCanThanhToan = h.TongTienCanThanhToan,
                TienCoc = h.TienCoc,
                DaThu = DaThu(h),
                DaHoan = DaHoan(h),
                TrangThai = h.TrangThai,
                TenNhanVienLap = h.NhanVienLap?.TenNhanVien,
                PhuongThuc = h.ThanhToans?.Where(t => t.LoaiGiaoDich == PaymentType.Payment).OrderBy(t => t.NgayThanhToan).Select(t => t.PhuongThuc).FirstOrDefault()
            }).ToList();
        }

        public async Task<InvoiceDetailDto> GetInvoiceDetailAsync(string maHoaDon)
        {
            var h = await InvoiceQuery().FirstOrDefaultAsync(x => x.MaHoaDon == maHoaDon)
                ?? throw new NotFoundException($"Không tìm thấy hóa đơn '{maHoaDon}'.");

            var booking = h.PhieuDatPhong.ToDto();
            var daThu = DaThu(h);
            var daHoan = DaHoan(h);
            return new InvoiceDetailDto
            {
                HoaDon = booking.HoaDon ?? h.ToDto(),
                PhieuDatPhong = booking,
                TenNhanVienLap = h.NhanVienLap?.TenNhanVien,
                DaThu = daThu,
                DaHoan = daHoan,
                CoTheHoan = h.TrangThai == InvoiceStatus.Paid ? Math.Max(0, Math.Round(h.TongTienCanThanhToan, 0) - TraBangDiem(h) - daHoan) : 0,
                GiaoDich = h.ThanhToans.OrderBy(t => t.NgayThanhToan).Select(t => t.ToDto()).ToList()
            };
        }

        public async Task<InvoiceDetailDto> RefundAsync(string maHoaDon, decimal soTien, string phuongThuc, string lyDo, string maNhanVien)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var h = await _context.HoaDons
                    .Include(x => x.ThanhToans)
                    .Include(x => x.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                    .FirstOrDefaultAsync(x => x.MaHoaDon == maHoaDon)
                    ?? throw new NotFoundException($"Không tìm thấy hóa đơn '{maHoaDon}'.");
                if (h.TrangThai != InvoiceStatus.Paid)
                    throw new ConflictException("Chỉ hoàn tiền cho hoá đơn đã thanh toán.");

                // Tổng đã thu (gồm cả tiền cọc) = tổng giá trị hoá đơn; không hoàn quá phần còn lại, không hoàn phần trả bằng điểm
                var coTheHoan = Math.Round(h.TongTienCanThanhToan, 0) - TraBangDiem(h) - DaHoan(h);
                if (soTien > coTheHoan)
                    throw new BusinessException($"Số tiền hoàn vượt quá số tiền còn có thể hoàn ({coTheHoan:N0}đ).");

                var refund = new ThanhToan
                {
                    MaThanhToan = await IdGenerator.NextAsync(_context.ThanhToans.Select(t => t.MaThanhToan), "TT", 4),
                    MaHoaDon = maHoaDon,
                    PhuongThuc = phuongThuc,
                    NgayThanhToan = DateTime.Now,
                    SoTienDaTra = soTien,
                    GhiChu = $"Hoàn tiền: {lyDo}",
                    LoaiGiaoDich = PaymentType.Refund,
                    MaNhanVien = maNhanVien
                };
                _context.ThanhToans.Add(refund);

                _context.LichSuGiaoDichs.Add(new LichSuGiaoDich
                {
                    MaGiaoDich = await IdGenerator.NextAsync(_context.LichSuGiaoDichs.Select(g => g.MaGiaoDich), "GD_", 6, 50),
                    MaThanhToan = refund.MaThanhToan,
                    CongThanhToan = phuongThuc,
                    SoTien = soTien,
                    TrangThaiGiaoDich = TransactionStatus.Success,
                    ThoiGianGiaoDich = DateTime.Now,
                    NoiDungChuyenKhoan = $"Hoàn tiền hóa đơn {maHoaDon}"
                });

                // Trừ lại điểm tích luỹ tương ứng số tiền hoàn (1 điểm / 10.000đ)
                var khachHang = h.PhieuDatPhong?.KhachHang;
                if (khachHang != null)
                {
                    var truoc = khachHang.DiemTichLuy;
                    khachHang.DiemTichLuy = Math.Max(0, khachHang.DiemTichLuy - (int)(soTien / 10000));
                    if (khachHang.DiemTichLuy != truoc)
                        await PointLedger.AddAsync(_context, khachHang.MaKhachHang, khachHang.DiemTichLuy - truoc, PointLedger.HoanTien,
                            $"Hoàn tiền hoá đơn {maHoaDon}: {lyDo}", maHoaDon, maNhanVien);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await GetInvoiceDetailAsync(maHoaDon);
        }

        public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime tuNgay, DateTime denNgay)
        {
            var from = tuNgay.Date;
            var to = denNgay.Date;
            if (to < from) throw new BusinessException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");

            var paidInvoices = (await InvoiceQuery().Where(h => h.TrangThai == InvoiceStatus.Paid).ToListAsync())
                .Where(h => NgayGhiNhan(h).Date >= from && NgayGhiNhan(h).Date <= to)
                .ToList();

            var transactions = await _context.ThanhToans
                .Include(t => t.NhanVien)
                .Where(t => t.NgayThanhToan >= from && t.NgayThanhToan < to.AddDays(1))
                .ToListAsync();
            var payments = transactions.Where(t => t.LoaiGiaoDich == PaymentType.Payment).ToList();
            var refunds = transactions.Where(t => t.LoaiGiaoDich == PaymentType.Refund).ToList();

            var report = new RevenueReportDto
            {
                TuNgay = from,
                DenNgay = to,
                SoHoaDon = paidInvoices.Count,
                TongDoanhThu = Math.Round(paidInvoices.Sum(h => h.TongTienCanThanhToan), 0),
                TongHoanTien = refunds.Sum(t => t.SoTienDaTra),
                TienPhong = paidInvoices.Sum(h => h.TongTienPhong),
                TienDichVu = paidInvoices.Sum(h => h.TongTienDichVu),
                PhuThu = paidInvoices.Sum(h => h.TienPhat),
                GiamGia = paidInvoices.Sum(h => h.GiamGiaKhuyenMai),
                ThueVAT = Math.Round(paidInvoices.Sum(h => h.ThueVAT), 0)
            };
            report.DoanhThuThuan = report.TongDoanhThu - report.TongHoanTien;

            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var day = d;
                var invoicesOfDay = paidInvoices.Where(h => NgayGhiNhan(h).Date == day).ToList();
                report.TheoNgay.Add(new RevenueByDayDto
                {
                    Ngay = day,
                    SoHoaDon = invoicesOfDay.Count,
                    DoanhThu = Math.Round(invoicesOfDay.Sum(h => h.TongTienCanThanhToan), 0),
                    HoanTien = refunds.Where(t => t.NgayThanhToan.Date == day).Sum(t => t.SoTienDaTra)
                });
            }

            report.TheoLoaiPhong = paidInvoices
                .GroupBy(h => h.PhieuDatPhong?.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.LoaiPhong?.TenLoai ?? "Không rõ")
                .Select(g => new RevenueByRoomTypeDto
                {
                    TenLoai = g.Key,
                    SoLuot = g.Count(),
                    SoDem = g.Sum(h => h.SoDem),
                    DoanhThuPhong = g.Sum(h => h.TongTienPhong)
                })
                .OrderByDescending(x => x.DoanhThuPhong)
                .ToList();

            report.TheoDichVu = paidInvoices
                .SelectMany(h => h.PhieuDatPhong?.SuDungDichVus ?? Enumerable.Empty<SuDungDichVu>())
                .GroupBy(s => s.DichVu?.TenDichVu ?? s.MaDichVu)
                .Select(g => new RevenueByServiceDto { TenDichVu = g.Key, SoLuong = g.Sum(s => s.SoLuong), DoanhThu = g.Sum(s => s.ThanhTien) })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();

            report.TheoPhuongThuc = payments
                .GroupBy(t => t.PhuongThuc ?? "Không rõ")
                .Select(g => new RevenueByMethodDto { PhuongThuc = g.Key, SoGiaoDich = g.Count(), SoTien = g.Sum(t => t.SoTienDaTra) })
                .OrderByDescending(x => x.SoTien)
                .ToList();
            var tienCoc = paidInvoices.Sum(h => h.TienCoc);
            if (tienCoc > 0)
                report.TheoPhuongThuc.Add(new RevenueByMethodDto { PhuongThuc = "Tiền cọc (thu khi nhận phòng)", SoGiaoDich = paidInvoices.Count(h => h.TienCoc > 0), SoTien = tienCoc });
            foreach (var g in refunds.GroupBy(t => t.PhuongThuc ?? "Không rõ"))
                report.TheoPhuongThuc.Add(new RevenueByMethodDto { PhuongThuc = $"Hoàn tiền ({g.Key})", SoGiaoDich = g.Count(), SoTien = -g.Sum(t => t.SoTienDaTra) });

            report.TheoNhanVien = payments
                .GroupBy(t => new { t.MaNhanVien, Ten = t.NhanVien?.TenNhanVien })
                .Select(g => new RevenueByStaffDto
                {
                    MaNhanVien = g.Key.MaNhanVien,
                    TenNhanVien = g.Key.Ten ?? "Không rõ",
                    SoGiaoDich = g.Count(),
                    SoTienThu = g.Sum(t => t.SoTienDaTra)
                })
                .OrderByDescending(x => x.SoTienThu)
                .ToList();

            return report;
        }

        public async Task<OccupancyReportDto> GetOccupancyReportAsync(DateTime tuNgay, DateTime denNgay)
        {
            var from = tuNgay.Date;
            var to = denNgay.Date;
            if (to < from) throw new BusinessException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
            var today = DateTime.Today;

            var tongSoPhong = await _context.Phongs.CountAsync();
            var stays = await _context.ChiTietDatPhongs
                .Where(c => c.PhieuDatPhong.NgayCheckInThucTe != null
                    && (c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn || c.PhieuDatPhong.TrangThai == BookingStatus.CheckedOut))
                .Select(c => new
                {
                    c.MaPhong,
                    NhanPhong = c.PhieuDatPhong.NgayCheckInThucTe.Value,
                    TraPhong = c.PhieuDatPhong.NgayCheckOutThucTe,
                    TraDuKien = c.PhieuDatPhong.NgayCheckOutDuKien,
                    c.PhieuDatPhong.TrangThai
                })
                .ToListAsync();

            // Mỗi đêm của khoảng [ngày nhận, ngày trả) tính là 1 đêm phòng; ở trong ngày tính 1 đêm
            var nights = stays.Select(s =>
            {
                var start = s.NhanPhong.Date;
                var end = s.TrangThai == BookingStatus.CheckedOut && s.TraPhong != null
                    ? s.TraPhong.Value.Date
                    : (s.TraDuKien.Date > today ? s.TraDuKien.Date : today.AddDays(1));
                if (end <= start) end = start.AddDays(1);
                return new { s.MaPhong, Start = start, End = end };
            }).ToList();

            var report = new OccupancyReportDto { TuNgay = from, DenNgay = to, TongSoPhong = tongSoPhong };
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var day = d;
                var occupied = nights.Where(n => n.Start <= day && day < n.End).Select(n => n.MaPhong).Distinct().Count();
                report.TheoNgay.Add(new OccupancyByDayDto
                {
                    Ngay = day,
                    SoPhongCoKhach = occupied,
                    TyLe = tongSoPhong == 0 ? 0 : Math.Round(occupied * 100m / tongSoPhong, 1)
                });
            }
            report.TongDemPhongDaBan = report.TheoNgay.Sum(x => x.SoPhongCoKhach);
            report.TyLeTrungBinh = report.TheoNgay.Count == 0 ? 0 : Math.Round(report.TheoNgay.Average(x => x.TyLe), 1);
            return report;
        }

        public async Task<List<GuestRegisterDto>> GetGuestRegisterAsync(DateTime tuNgay, DateTime denNgay)
        {
            var from = tuNgay.Date;
            var toExclusive = denNgay.Date.AddDays(1);
            if (toExclusive <= from) throw new BusinessException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");

            var stays = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                .Include(p => p.KhachDiCungs)
                .Where(p => p.NgayCheckInThucTe != null
                         && (p.TrangThai == BookingStatus.CheckedIn || p.TrangThai == BookingStatus.CheckedOut)
                         && p.NgayCheckInThucTe < toExclusive
                         && (p.NgayCheckOutThucTe == null || p.NgayCheckOutThucTe >= from))
                .OrderBy(p => p.NgayCheckInThucTe)
                .ToListAsync();

            return stays.SelectMany(p =>
            {
                var lienHe = p.KhachHang?.LienHeDiaChis?.FirstOrDefault();
                var dungTen = new GuestRegisterDto
                {
                    MaDatPhong = p.MaDatPhong,
                    HoTen = $"{p.KhachHang?.Ho} {p.KhachHang?.Ten}".Trim(),
                    NgaySinh = p.KhachHang?.NgaySinh,
                    CCCD_Passport = p.KhachHang?.CCCD_Passport,
                    QuocGia = lienHe?.QuocGia ?? "Việt Nam",
                    DiaChi = lienHe?.DiaChi,
                    SDT = lienHe?.SDT,
                    SoPhong = p.ChiTietDatPhongs?.FirstOrDefault()?.Phong?.SoPhong,
                    SoLuongKhach = p.SoLuongKhach,
                    NgayDen = p.NgayCheckInThucTe.Value,
                    NgayDi = p.NgayCheckOutThucTe,
                    NgayDiDuKien = p.NgayCheckOutDuKien,
                    TrangThai = p.TrangThai
                };
                var diCung = (p.KhachDiCungs ?? new List<KhachDiCung>()).OrderBy(k => k.MaKhachDiCung).Select(k => new GuestRegisterDto
                {
                    MaDatPhong = p.MaDatPhong,
                    HoTen = k.HoTen,
                    CCCD_Passport = k.CCCD_Passport,
                    QuocGia = k.QuocTich ?? dungTen.QuocGia,
                    SoPhong = dungTen.SoPhong,
                    SoLuongKhach = p.SoLuongKhach,
                    NgayDen = dungTen.NgayDen,
                    NgayDi = dungTen.NgayDi,
                    NgayDiDuKien = dungTen.NgayDiDuKien,
                    TrangThai = p.TrangThai,
                    LaNguoiDiCung = true,
                    NguoiDungTen = dungTen.HoTen
                });
                return new[] { dungTen }.Concat(diCung);
            }).ToList();
        }

        public async Task<byte[]> ExportGuestRegisterExcelAsync(DateTime tuNgay, DateTime denNgay)
        {
            var guests = await GetGuestRegisterAsync(tuNgay, denNgay);
            using var workbook = new XLWorkbook();
            AddTable(workbook, "Sổ khách lưu trú",
                new[] { "STT", "Họ và tên", "Ngày sinh", "Số CCCD/Hộ chiếu", "Quốc tịch", "Địa chỉ", "Số điện thoại", "Phòng", "Số khách", "Ngày đến", "Ngày đi", "Ghi chú" },
                guests.Select((g, i) => new object[]
                {
                    i + 1, g.HoTen, g.NgaySinh, g.CCCD_Passport, g.QuocGia, g.DiaChi, g.SDT, g.SoPhong, g.SoLuongKhach,
                    g.NgayDen, g.NgayDi ?? g.NgayDiDuKien,
                    string.Join("; ", new[] { g.LaNguoiDiCung ? $"Đi cùng {g.NguoiDungTen}" : null, g.NgayDi == null ? "Đang lưu trú (ngày đi dự kiến)" : null }.Where(x => x != null))
                }), "#,##0");
            workbook.Worksheet(1).Column(3).Style.DateFormat.Format = "dd/MM/yyyy";
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportRevenueExcelAsync(DateTime tuNgay, DateTime denNgay)
        {
            var report = await GetRevenueReportAsync(tuNgay, denNgay);
            var occupancy = await GetOccupancyReportAsync(tuNgay, denNgay);
            var invoices = (await GetInvoicesAsync(null, null, InvoiceStatus.Paid, null))
                .Where(i => i.NgayLapHoaDon.Date >= report.TuNgay && i.NgayLapHoaDon.Date <= report.DenNgay)
                .ToList();

            using var workbook = new XLWorkbook();
            const string money = "#,##0";

            var ws = workbook.Worksheets.Add("Tổng quan");
            ws.Cell(1, 1).Value = $"BÁO CÁO DOANH THU {report.TuNgay:dd/MM/yyyy} - {report.DenNgay:dd/MM/yyyy}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            var rows = new (string Label, object Value)[]
            {
                ("Số hoá đơn đã thanh toán", report.SoHoaDon),
                ("Tiền phòng", report.TienPhong),
                ("Tiền dịch vụ", report.TienDichVu),
                ("Phụ thu trả muộn", report.PhuThu),
                ("Giảm giá khuyến mãi", -report.GiamGia),
                ("Thuế VAT (8%)", report.ThueVAT),
                ("Tổng doanh thu", report.TongDoanhThu),
                ("Hoàn tiền", -report.TongHoanTien),
                ("Doanh thu thuần", report.DoanhThuThuan),
                ("Công suất phòng trung bình (%)", occupancy.TyLeTrungBinh),
                ("Đêm phòng đã bán", occupancy.TongDemPhongDaBan)
            };
            for (int i = 0; i < rows.Length; i++)
            {
                ws.Cell(i + 3, 1).Value = rows[i].Label;
                ws.Cell(i + 3, 2).Value = XLCellValue.FromObject(rows[i].Value);
                ws.Cell(i + 3, 2).Style.NumberFormat.Format = money;
            }
            ws.Range(9, 1, 9, 2).Style.Font.Bold = true;
            ws.Range(11, 1, 11, 2).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            AddTable(workbook, "Theo ngày", new[] { "Ngày", "Số hoá đơn", "Doanh thu", "Hoàn tiền" },
                report.TheoNgay.Select(x => new object[] { x.Ngay, x.SoHoaDon, x.DoanhThu, x.HoanTien }), money);
            AddTable(workbook, "Theo loại phòng", new[] { "Loại phòng", "Số lượt", "Số đêm", "Doanh thu phòng" },
                report.TheoLoaiPhong.Select(x => new object[] { x.TenLoai, x.SoLuot, x.SoDem, x.DoanhThuPhong }), money);
            AddTable(workbook, "Theo dịch vụ", new[] { "Dịch vụ", "Số lượng", "Doanh thu" },
                report.TheoDichVu.Select(x => new object[] { x.TenDichVu, x.SoLuong, x.DoanhThu }), money);
            AddTable(workbook, "Theo phương thức", new[] { "Phương thức", "Số giao dịch", "Số tiền" },
                report.TheoPhuongThuc.Select(x => new object[] { x.PhuongThuc, x.SoGiaoDich, x.SoTien }), money);
            AddTable(workbook, "Theo nhân viên thu", new[] { "Nhân viên", "Số giao dịch", "Số tiền thu" },
                report.TheoNhanVien.Select(x => new object[] { x.TenNhanVien, x.SoGiaoDich, x.SoTienThu }), money);
            AddTable(workbook, "Hoá đơn", new[] { "Mã HĐ", "Mã phiếu", "Khách hàng", "Phòng", "Ngày lập", "Tổng tiền", "Tiền cọc", "Đã thu", "Đã hoàn", "Phương thức", "Người lập" },
                invoices.Select(x => new object[] { x.MaHoaDon, x.MaDatPhong, x.HoTenKhachHang, x.SoPhong, x.NgayLapHoaDon, x.TongTienCanThanhToan, x.TienCoc, x.DaThu, x.DaHoan, x.PhuongThuc, x.TenNhanVienLap }), money);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void AddTable(XLWorkbook workbook, string name, string[] headers, IEnumerable<object[]> data, string moneyFormat)
        {
            var ws = workbook.Worksheets.Add(name);
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(1, c + 1).Value = headers[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
                ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0A192F");
                ws.Cell(1, c + 1).Style.Font.FontColor = XLColor.White;
            }
            int r = 2;
            foreach (var row in data)
            {
                for (int c = 0; c < row.Length; c++)
                {
                    var cell = ws.Cell(r, c + 1);
                    cell.Value = XLCellValue.FromObject(row[c]);
                    if (row[c] is decimal) cell.Style.NumberFormat.Format = moneyFormat;
                    if (row[c] is DateTime) cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }
                r++;
            }
            ws.Columns().AdjustToContents();
        }
    }
}
