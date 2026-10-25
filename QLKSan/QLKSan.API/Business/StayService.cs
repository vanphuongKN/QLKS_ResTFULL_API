using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    // Thao tác trong kỳ lưu trú: gia hạn / rút ngắn / đổi ngày, đổi phòng khi đang ở, huỷ dịch vụ gọi nhầm, người đi cùng
    public interface IStayService
    {
        // Báo giá khoảng ngày mới (không lưu); ngayNhan chỉ dùng cho phiếu chưa nhận phòng
        Task<StayChangeQuoteDto> QuoteStayChangeAsync(string maDatPhong, DateTime? ngayNhan, DateTime ngayTra);
        Task ChangeStayDatesAsync(string maDatPhong, DateTime? ngayNhan, DateTime ngayTra, string nguoiThucHien);

        // Phòng mới phải sạch, trống đến ngày trả; phòng cũ chuyển sang Đang dọn (tạo việc buồng phòng)
        Task MoveRoomAsync(string maDatPhong, MoveRoomRequest request, string maNhanVien, string nguoiThucHien);

        Task VoidServiceAsync(string maDatPhong, string maSDDV, int soLuong, string lyDo, string nguoiThucHien);
        Task SetCompanionsAsync(string maDatPhong, IList<CompanionRequest> khachDiCung);
    }

    public class StayService : IStayService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoomAvailabilityService _availability;
        private readonly IPricingService _pricing;

        public StayService(ApplicationDbContext context, IRoomAvailabilityService availability, IPricingService pricing)
        {
            _context = context;
            _availability = availability;
            _pricing = pricing;
        }

        private async Task<PhieuDatPhong> LoadAsync(string maDatPhong)
            => await _context.PhieuDatPhongs
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaDatPhong == maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");

        // Ngày mới hợp lệ: đang ở → giữ ngày nhận thực tế, ngày trả từ hôm nay; chưa nhận → nhận từ hôm nay
        private static (DateTime Nhan, DateTime Tra) ResolveDates(PhieuDatPhong booking, DateTime? ngayNhan, DateTime ngayTra)
        {
            var today = DateTime.Today;
            DateTime nhan;
            if (booking.TrangThai == BookingStatus.CheckedIn)
            {
                nhan = (booking.NgayCheckInThucTe ?? booking.NgayCheckInDuKien).Date;
                if (ngayTra.Date < today)
                    throw new BusinessException("Ngày trả phòng mới không được trước hôm nay.");
            }
            else if (booking.TrangThai is BookingStatus.Pending or BookingStatus.Approved)
            {
                nhan = (ngayNhan ?? booking.NgayCheckInDuKien).Date;
                if (nhan < today)
                    throw new BusinessException("Ngày nhận phòng không được ở trong quá khứ.");
            }
            else
            {
                throw new ConflictException("Chỉ đổi ngày cho phiếu chưa nhận phòng hoặc đang lưu trú.");
            }
            if (ngayTra.Date <= nhan)
                throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng (tối thiểu 1 đêm).");
            return (nhan, ngayTra.Date);
        }

        public async Task<StayChangeQuoteDto> QuoteStayChangeAsync(string maDatPhong, DateTime? ngayNhan, DateTime ngayTra)
        {
            var booking = await LoadAsync(maDatPhong);
            var (nhan, tra) = ResolveDates(booking, ngayNhan, ngayTra);
            var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault()
                ?? throw new BusinessException("Phiếu đặt không gắn với phòng nào.");
            var maLoai = chiTiet.Phong?.MaLoai;

            var nhanCu = (booking.NgayCheckInThucTe ?? booking.NgayCheckInDuKien).Date;
            var soDemCu = BillingCalculator.TinhSoDem(nhanCu, booking.NgayCheckOutDuKien);
            var soDemMoi = BillingCalculator.TinhSoDem(nhan, tra);
            var giaCu = await _pricing.GetNightlyPricesAsync(maLoai, chiTiet.GiaThueThucTe, nhanCu, soDemCu);
            var giaMoi = await _pricing.GetNightlyPricesAsync(maLoai, chiTiet.GiaThueThucTe, nhan, soDemMoi);

            var dto = new StayChangeQuoteDto
            {
                MaDatPhong = maDatPhong,
                NgayNhanCu = nhanCu,
                NgayTraCu = booking.NgayCheckOutDuKien.Date,
                NgayNhanMoi = nhan,
                NgayTraMoi = tra,
                SoDemCu = soDemCu,
                SoDemMoi = soDemMoi,
                TienPhongCu = giaCu.Sum(n => n.Gia),
                TienPhongMoi = giaMoi.Sum(n => n.Gia),
                CacDem = giaMoi,
                ConPhong = true
            };
            dto.ChenhLech = dto.TienPhongMoi - dto.TienPhongCu;
            try
            {
                await _availability.EnsureAvailableAsync(chiTiet.MaPhong, nhan, tra, maDatPhong);
            }
            catch (ConflictException ex)
            {
                dto.ConPhong = false;
                dto.LyDo = ex.Message;
            }
            return dto;
        }

        public async Task ChangeStayDatesAsync(string maDatPhong, DateTime? ngayNhan, DateTime ngayTra, string nguoiThucHien)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await LoadAsync(maDatPhong);
                var (nhan, tra) = ResolveDates(booking, ngayNhan, ngayTra);
                var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault()
                    ?? throw new BusinessException("Phiếu đặt không gắn với phòng nào.");

                var nhanCu = (booking.NgayCheckInThucTe ?? booking.NgayCheckInDuKien).Date;
                var traCu = booking.NgayCheckOutDuKien.Date;
                if (nhan == nhanCu && tra == traCu) return;

                // Khoá phòng rồi kiểm tra trùng lịch, bỏ qua chính phiếu này
                await _availability.LockRoomAsync(chiTiet.MaPhong);
                await _availability.EnsureAvailableAsync(chiTiet.MaPhong, nhan, tra, maDatPhong);

                var hanhDong = booking.TrangThai == BookingStatus.CheckedIn ? (tra > traCu ? "Gia hạn" : "Rút ngắn") : "Đổi ngày";
                if (booking.TrangThai != BookingStatus.CheckedIn) booking.NgayCheckInDuKien = nhan;
                booking.NgayCheckOutDuKien = tra;
                booking.GhiChu = BookingNotes.Append(booking.GhiChu,
                    $"[{hanhDong} {DateTime.Now:dd/MM/yyyy HH:mm} - {nguoiThucHien}] {nhanCu:dd/MM}–{traCu:dd/MM} → {nhan:dd/MM}–{tra:dd/MM}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task MoveRoomAsync(string maDatPhong, MoveRoomRequest request, string maNhanVien, string nguoiThucHien)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await LoadAsync(maDatPhong);
                if (booking.TrangThai != BookingStatus.CheckedIn)
                    throw new ConflictException("Chỉ đổi phòng theo cách này cho khách đang lưu trú (phiếu chưa nhận phòng dùng chức năng đổi phòng đã xếp).");
                var chiTiet = booking.ChiTietDatPhongs.FirstOrDefault()
                    ?? throw new BusinessException("Phiếu đặt không gắn với phòng nào.");
                if (chiTiet.MaPhong == request.MaPhong)
                    throw new BusinessException("Khách đang ở phòng này.");

                await _availability.LockRoomAsync(request.MaPhong);
                var moi = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == request.MaPhong)
                    ?? throw new NotFoundException($"Không tìm thấy phòng '{request.MaPhong}'.");
                if (moi.TrangThai != RoomStatus.Available)
                    throw new ConflictException($"Phòng {moi.SoPhong} chưa sẵn sàng (phải trống và đã dọn).");
                if (moi.LoaiPhong != null && booking.SoLuongKhach > moi.LoaiPhong.SoNguoiToiDa)
                    throw new BusinessException($"Phòng {moi.SoPhong} chỉ chứa tối đa {moi.LoaiPhong.SoNguoiToiDa} khách.");
                var today = DateTime.Today;
                await _availability.EnsureAvailableAsync(moi.MaPhong, today,
                    RoomAvailabilityService.NgayTraHieuLuc(booking.TrangThai, booking.NgayCheckOutDuKien, today));

                // Phòng cũ cần dọn lại trước khi đón khách khác
                var cu = chiTiet.Phong;
                cu.TrangThai = RoomStatus.Cleaning;
                await HousekeepingTasks.EnsureOpenTaskAsync(_context, cu.MaPhong, maDatPhong, HousekeepingTasks.DonSauDoiPhong);

                var giaMoi = request.GiuGiaCu ? chiTiet.GiaThueThucTe : moi.LoaiPhong.GiaThueNgay;
                _context.LichSuDoiPhongs.Add(new LichSuDoiPhong
                {
                    MaDatPhong = maDatPhong,
                    TuPhong = cu.MaPhong,
                    DenPhong = moi.MaPhong,
                    ThoiGian = DateTime.Now,
                    LyDo = request.LyDo.Trim(),
                    GiaCu = chiTiet.GiaThueThucTe,
                    GiaMoi = giaMoi,
                    MaNhanVien = maNhanVien
                });
                booking.GhiChu = BookingNotes.Append(booking.GhiChu,
                    $"[Đổi phòng {DateTime.Now:dd/MM/yyyy HH:mm} - {nguoiThucHien}] {cu.SoPhong} → {moi.SoPhong}: {request.LyDo.Trim()}"
                    + (request.GiuGiaCu ? " (giữ giá cũ)" : ""));

                chiTiet.MaPhong = moi.MaPhong;
                chiTiet.GiaThueThucTe = giaMoi;
                moi.TrangThai = RoomStatus.Occupied;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task VoidServiceAsync(string maDatPhong, string maSDDV, int soLuong, string lyDo, string nguoiThucHien)
        {
            var dong = await _context.SuDungDichVus
                .Include(s => s.DichVu)
                .Include(s => s.PhieuDatPhong).ThenInclude(p => p.HoaDon)
                .FirstOrDefaultAsync(s => s.MaSDDV == maSDDV && s.MaDatPhong == maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy dịch vụ '{maSDDV}' của phiếu {maDatPhong}.");
            var booking = dong.PhieuDatPhong;
            if (booking.TrangThai != BookingStatus.CheckedIn)
                throw new ConflictException("Chỉ huỷ dịch vụ của khách đang lưu trú.");
            if (booking.HoaDon != null && booking.HoaDon.TrangThai == InvoiceStatus.Paid)
                throw new ConflictException("Hoá đơn đã thanh toán, không huỷ dịch vụ được.");
            if (soLuong < 1 || soLuong > dong.SoLuong)
                throw new BusinessException($"Số lượng huỷ phải từ 1 đến {dong.SoLuong}.");

            var ten = dong.DichVu?.TenDichVu ?? dong.MaDichVu;
            if (soLuong == dong.SoLuong) _context.SuDungDichVus.Remove(dong);
            else dong.SoLuong -= soLuong;
            booking.GhiChu = BookingNotes.Append(booking.GhiChu,
                $"[Huỷ dịch vụ {DateTime.Now:dd/MM/yyyy HH:mm} - {nguoiThucHien}] {ten} × {soLuong}: {lyDo?.Trim()}");
            // Hoá đơn chưa thanh toán được tính lại khi lễ tân mở màn hình trả phòng
            await _context.SaveChangesAsync();
        }

        public async Task SetCompanionsAsync(string maDatPhong, IList<CompanionRequest> khachDiCung)
        {
            var booking = await LoadAsync(maDatPhong);
            if (booking.TrangThai is not (BookingStatus.Pending or BookingStatus.Approved or BookingStatus.CheckedIn))
                throw new ConflictException("Chỉ khai báo người đi cùng cho phiếu chưa trả phòng.");
            await ApplyCompanionsAsync(_context, booking, khachDiCung);
            await _context.SaveChangesAsync();
        }

        // Thay danh sách người đi cùng; số khách của phiếu tăng theo nếu cần (không vượt sức chứa phòng). Không tự SaveChanges
        internal static async Task ApplyCompanionsAsync(ApplicationDbContext context, PhieuDatPhong booking, IList<CompanionRequest> khachDiCung)
        {
            khachDiCung ??= new List<CompanionRequest>();
            var sucChua = booking.ChiTietDatPhongs.FirstOrDefault()?.Phong?.LoaiPhong?.SoNguoiToiDa ?? 10;
            if (khachDiCung.Count + 1 > sucChua)
                throw new BusinessException($"Phòng chỉ chứa tối đa {sucChua} khách (gồm người đứng tên).");
            if (khachDiCung.Count + 1 > booking.SoLuongKhach) booking.SoLuongKhach = khachDiCung.Count + 1;

            context.KhachDiCungs.RemoveRange(await context.KhachDiCungs.Where(k => k.MaDatPhong == booking.MaDatPhong).ToListAsync());
            foreach (var k in khachDiCung)
            {
                context.KhachDiCungs.Add(new KhachDiCung
                {
                    MaDatPhong = booking.MaDatPhong,
                    HoTen = k.HoTen.Trim(),
                    CCCD_Passport = string.IsNullOrWhiteSpace(k.CCCD_Passport) ? null : k.CCCD_Passport.Trim().ToUpperInvariant(),
                    QuocTich = string.IsNullOrWhiteSpace(k.QuocTich) ? null : k.QuocTich.Trim()
                });
            }
        }
    }
}
