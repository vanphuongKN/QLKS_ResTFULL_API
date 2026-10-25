using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [Authorize(Roles = WebRoles.FrontDesk)]
    public class ReceptionController : Controller
    {
        private readonly IRoomApiClient _roomApi;
        private readonly IBookingApiClient _bookingApi;
        private readonly ICustomerApiClient _customerApi;
        private readonly IFrontDeskApiClient _frontDeskApi;
        private readonly IRoomTypeApiClient _roomTypeApi;

        public ReceptionController(IRoomApiClient roomApi, IBookingApiClient bookingApi, ICustomerApiClient customerApi, IFrontDeskApiClient frontDeskApi,
            IRoomTypeApiClient roomTypeApi)
        {
            _roomApi = roomApi;
            _bookingApi = bookingApi;
            _customerApi = customerApi;
            _frontDeskApi = frontDeskApi;
            _roomTypeApi = roomTypeApi;
        }

        // Màn hình đầu ca: khách đến / đi / đang ở hôm nay, phiếu sắp đến, sơ đồ phòng.
        // Tìm phiếu (search) chuyển sang trang Lịch sử đặt phòng
        [HttpGet]
        public async Task<IActionResult> Index(string search = null)
        {
            if (!string.IsNullOrWhiteSpace(search)) return RedirectToAction(nameof(BookingHistory), new { search });
            ViewBag.Rooms = await _roomApi.GetAllAsync();
            return View(await _frontDeskApi.GetTodayAsync());
        }

        // Đổi phòng đã xếp cho phiếu chưa nhận phòng: liệt kê phòng trống đủ sức chứa trong đúng khoảng ngày của phiếu
        [HttpGet]
        public async Task<IActionResult> ChangeRoom(string id)
        {
            var booking = string.IsNullOrEmpty(id) ? null : await _bookingApi.GetByIdAsync(id);
            if (booking == null) return NotFound();
            if (booking.TrangThai is Enums.BookingStatus.Pending or Enums.BookingStatus.Approved)
                ViewBag.Rooms = await _roomApi.GetAvailableAsync(booking.NgayCheckInDuKien, booking.NgayCheckOutDuKien, booking.SoLuongKhach);
            else if (booking.TrangThai == Enums.BookingStatus.CheckedIn)
            {
                // Khách đang ở: phòng phải sạch, sẵn sàng ngay và trống đến ngày trả
                var den = booking.NgayCheckOutDuKien.Date > DateTime.Today ? booking.NgayCheckOutDuKien.Date : DateTime.Today.AddDays(1);
                ViewBag.Rooms = (await _roomApi.GetAvailableAsync(DateTime.Today, den, booking.SoLuongKhach))
                    .Where(r => r.TrangThai == Enums.RoomStatus.Available).ToList();
            }
            else ViewBag.Rooms = new List<Models.DTOs.RoomDto>();
            return View(booking);
        }

        // Chưa nhận phòng: đổi phòng đã xếp. Đang ở: chuyển phòng (cần lý do, tuỳ chọn giữ giá cũ)
        [HttpPost]
        public async Task<IActionResult> ChangeRoom(string id, string maPhong, string lyDo, bool giuGiaCu = false, bool dangO = false)
        {
            try
            {
                if (dangO) await _bookingApi.MoveRoomAsync(id, new MoveRoomRequest { MaPhong = maPhong, LyDo = lyDo, GiuGiaCu = giuGiaCu });
                else await _bookingApi.ChangeRoomAsync(id, maPhong);
                TempData["SuccessMessage"] = $"Đã đổi phòng cho phiếu {id}.";
                return Redirect(Url.Action(nameof(Index)) + (dangO ? "#dango" : ""));
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể đổi phòng. " + ex.UserMessage;
                return RedirectToAction(nameof(ChangeRoom), new { id });
            }
        }

        // Duyệt phòng online
        [HttpPost]
        public async Task<IActionResult> ApproveBooking(string id)
        {
            try
            {
                await _bookingApi.ApproveAsync(id);
                TempData["SuccessMessage"] = "Đã duyệt yêu cầu đặt phòng trực tuyến.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể duyệt yêu cầu này. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Nhận phòng cho phiếu đã đặt (mã nhân viên lấy từ JWT phía API); kèm số khách thực tế và cọc thu thêm nếu có
        [HttpPost]
        public async Task<IActionResult> CheckInOnlineBooking(string id, int? soLuongKhach, decimal tienCoc = 0, string phuongThucCoc = null,
            List<string> dcHoTen = null, List<string> dcCccd = null, List<string> dcQuocTich = null)
        {
            try
            {
                var diCung = Companions(dcHoTen, dcCccd, dcQuocTich);
                var request = soLuongKhach == null && tienCoc <= 0 && diCung.Count == 0 ? null
                    : new CheckInBookingRequest { SoLuongKhach = soLuongKhach, TienCoc = tienCoc, PhuongThucCoc = phuongThucCoc, KhachDiCung = diCung.Count > 0 ? diCung : null };
                await _bookingApi.CheckInAsync(id, request);
                TempData["SuccessMessage"] = "Khách đã nhận phòng thành công.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể check-in cho đơn này. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Từ chối phiếu chờ duyệt / huỷ phiếu đã duyệt; hoanCoc: hoàn toàn bộ tiền cọc đã thu
        [HttpPost]
        public async Task<IActionResult> CancelBooking(string id, string lyDo, bool hoanCoc = false)
        {
            try
            {
                await _bookingApi.CancelAsync(id, lyDo, hoanCoc);
                TempData["SuccessMessage"] = $"Đã huỷ/từ chối phiếu đặt phòng {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể huỷ phiếu. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Thu thêm tiền cọc / đảm bảo cho phiếu chưa trả phòng
        [HttpPost]
        public async Task<IActionResult> AddDeposit(string id, decimal soTien, string phuongThuc, string ghiChu)
        {
            try
            {
                await _bookingApi.AddDepositAsync(id, new DepositRequest { SoTien = soTien, PhuongThuc = phuongThuc, GhiChu = ghiChu });
                TempData["SuccessMessage"] = $"Đã thu cọc {soTien:N0}đ cho phiếu {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể thu cọc. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // --- TRONG KỲ LƯU TRÚ ---

        // Báo giá đổi ngày (JSON cho hộp thoại gia hạn)
        [HttpGet]
        public async Task<IActionResult> StayQuote(string id, DateTime? ngayNhan, DateTime ngayTra)
        {
            try
            {
                var q = await _bookingApi.QuoteStayDatesAsync(id, ngayNhan, ngayTra);
                return Json(new { q.SoDemCu, q.SoDemMoi, q.TienPhongCu, q.TienPhongMoi, q.ChenhLech, q.ConPhong, q.LyDo });
            }
            catch (ApiException ex)
            {
                return BadRequest(new { error = ex.UserMessage });
            }
        }

        // Gia hạn / rút ngắn (đang ở) hoặc đổi ngày (chưa nhận phòng)
        [HttpPost]
        public async Task<IActionResult> ChangeStayDates(string id, DateTime? ngayNhan, DateTime ngayTra)
        {
            try
            {
                var b = await _bookingApi.ChangeStayDatesAsync(id, new StayDatesRequest { NgayNhan = ngayNhan, NgayTra = ngayTra });
                TempData["SuccessMessage"] = $"Phiếu {id}: lưu trú {b.NgayCheckInDuKien:dd/MM} → {b.NgayCheckOutDuKien:dd/MM}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể đổi ngày lưu trú. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Khai báo người đi cùng (thay toàn bộ danh sách)
        [HttpPost]
        public async Task<IActionResult> SetCompanions(string id, List<string> dcHoTen, List<string> dcCccd, List<string> dcQuocTich)
        {
            try
            {
                var list = Companions(dcHoTen, dcCccd, dcQuocTich);
                await _bookingApi.SetCompanionsAsync(id, new CompanionsRequest { KhachDiCung = list });
                TempData["SuccessMessage"] = $"Đã cập nhật {list.Count} người đi cùng cho phiếu {id}.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = "Không thể lưu người đi cùng. " + ex.UserMessage;
            }
            return RedirectToAction("Index");
        }

        // Ghép các cột form (họ tên / CCCD / quốc tịch) thành danh sách, bỏ dòng trống họ tên
        private static List<CompanionRequest> Companions(List<string> hoTen, List<string> cccd, List<string> quocTich)
        {
            var list = new List<CompanionRequest>();
            for (var i = 0; i < (hoTen?.Count ?? 0); i++)
            {
                if (string.IsNullOrWhiteSpace(hoTen[i])) continue;
                list.Add(new CompanionRequest
                {
                    HoTen = hoTen[i].Trim(),
                    CCCD_Passport = cccd != null && i < cccd.Count && !string.IsNullOrWhiteSpace(cccd[i]) ? cccd[i].Trim() : null,
                    QuocTich = quocTich != null && i < quocTich.Count && !string.IsNullOrWhiteSpace(quocTich[i]) ? quocTich[i].Trim() : null
                });
            }
            return list;
        }

        // --- ĐẶT PHÒNG QUA QUẦY (điện thoại / email / tại quầy / đại lý) ---

        [HttpGet]
        public IActionResult NewBooking()
        {
            return View(new FrontDeskBookingRequest { NguonDat = Enums.BookingSources.DienThoai, PhuongThucCoc = Enums.PaymentMethods.TienMat });
        }

        [HttpPost]
        public async Task<IActionResult> NewBooking(FrontDeskBookingRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var booking = await _bookingApi.CreateAtFrontDeskAsync(model);
                    var phong = booking.ChiTietDatPhongs.FirstOrDefault()?.Phong?.SoPhong;
                    TempData["SuccessMessage"] = $"Đã tạo phiếu {booking.MaDatPhong} cho {model.Ho} {model.Ten}: phòng {phong}, "
                        + $"{booking.NgayCheckInDuKien:dd/MM} → {booking.NgayCheckOutDuKien:dd/MM}"
                        + (booking.TienCoc > 0 ? $", đã thu cọc {booking.TienCoc:N0}đ." : ".");
                    return Redirect(Url.Action(nameof(Index)) + (booking.NgayCheckInDuKien.Date == DateTime.Today ? "#den" : "#sapden"));
                }
                catch (ApiException ex)
                {
                    ModelState.AddModelError("", ex.UserMessage);
                }
            }
            return View(model);
        }

        // Hạng phòng còn trống cho khoảng ngày (JSON cho trang đặt phòng)
        [HttpGet]
        public async Task<IActionResult> AvailableRoomTypes(DateTime checkIn, DateTime checkOut, int soNguoi = 1)
        {
            if (checkOut.Date <= checkIn.Date || checkIn.Date < DateTime.Today) return Json(Array.Empty<object>());
            try
            {
                var list = await _roomTypeApi.GetAvailableAsync(checkIn.Date, checkOut.Date, soNguoi);
                return Json(list.Select(x => new
                {
                    maLoai = x.LoaiPhong.MaLoai,
                    tenLoai = FormatHelper.TenHangPhong(x.LoaiPhong.TenLoai),
                    soNguoiToiDa = x.LoaiPhong.SoNguoiToiDa,
                    soPhongTrong = x.SoPhongTrong,
                    soDem = x.BaoGia?.SoDem ?? 0,
                    tongTien = x.BaoGia?.TongTien ?? 0,
                    giaTrungBinh = x.BaoGia?.GiaTrungBinhDem ?? x.LoaiPhong.GiaThueNgay
                }));
            }
            catch (ApiException ex)
            {
                return BadRequest(new { error = ex.UserMessage });
            }
        }

        // Tra khách cũ theo CCCD/SĐT (JSON cho form đặt phòng / nhận khách vãng lai); chỉ trả thông tin lễ tân cần
        [HttpGet]
        public async Task<IActionResult> LookupGuest(string q)
        {
            var list = await _customerApi.LookupAsync(q);
            return Json(list.Select(k =>
            {
                var lienHe = k.LienHeDiaChis.FirstOrDefault();
                return new
                {
                    maKhachHang = k.MaKhachHang,
                    ho = k.Ho,
                    ten = k.Ten,
                    cccd = k.CCCD_Passport,
                    sdt = lienHe?.SDT,
                    email = lienHe?.Email,
                    laVIP = k.LaVIP,
                    hanChe = k.HanChe,
                    lyDoHanChe = k.LyDoHanChe,
                    soThich = k.SoThich,
                    ghiChuNoiBo = k.GhiChuNoiBo,
                    hang = k.ThanhVien?.Hang,
                    soLanDatPhong = k.SoLanDatPhong
                };
            }));
        }

        // --- WALK-IN CHECK-IN (Khách vãng lai) ---

        [HttpGet]
        // maPhong: chọn sẵn phòng khi bấm "Nhận khách" từ sơ đồ phòng
        public async Task<IActionResult> WalkIn(string maPhong)
        {
            ViewBag.AvailableRooms = await _roomApi.GetAvailableAsync();
            return View(new CheckInRequest { MaPhong = maPhong });
        }

        [HttpPost]
        public async Task<IActionResult> WalkIn(CheckInRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _bookingApi.WalkInAsync(model);
                    TempData["SuccessMessage"] = "Tạo mới đặt phòng và Check-in thành công!";
                    return RedirectToAction("Index");
                }
                catch (ApiException ex)
                {
                    ModelState.AddModelError("", ex.UserMessage);
                }
            }

            ViewBag.AvailableRooms = await _roomApi.GetAvailableAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BookingHistory(string search)
        {
            ViewBag.Search = search;
            var bookings = await _bookingApi.GetAllAsync(search);
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Customers(string search, bool vip = false)
        {
            ViewBag.Search = search;
            ViewBag.Vip = vip;
            var customers = await _customerApi.GetAllAsync(search);
            return View(vip ? customers.Where(k => k.LaVIP).ToList() : customers);
        }

        // Hồ sơ khách đầy đủ: thông tin, chăm sóc (VIP, sở thích, ghi chú nội bộ, hạn chế), lịch sử kỳ nghỉ, lịch sử điểm
        [HttpGet]
        public async Task<IActionResult> CustomerDetails(string id)
        {
            var customer = string.IsNullOrEmpty(id) ? null : await _customerApi.GetByIdAsync(id);
            if (customer == null) return NotFound();
            ViewBag.Points = await _customerApi.GetPointHistoryAsync(id);
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(string maKhachHang, UpdateCustomerProfileRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(CustomerDetails), new { id = maKhachHang });
            }
            try
            {
                await _customerApi.UpdateProfileAsync(maKhachHang, model);
                TempData["SuccessMessage"] = "Đã cập nhật hồ sơ khách.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(nameof(CustomerDetails), new { id = maKhachHang });
        }

        // Chỉ quản lý (API kiểm tra quyền)
        [HttpPost]
        public async Task<IActionResult> AdjustPoints(string maKhachHang, int soDiem, string lyDo)
        {
            try
            {
                await _customerApi.AdjustPointsAsync(maKhachHang, new PointAdjustmentRequest { SoDiem = soDiem, LyDo = lyDo });
                TempData["SuccessMessage"] = $"Đã {(soDiem >= 0 ? "cộng" : "trừ")} {Math.Abs(soDiem):N0} điểm.";
            }
            catch (ApiException ex)
            {
                TempData["ErrorMessage"] = ex.UserMessage;
            }
            return RedirectToAction(nameof(CustomerDetails), new { id = maKhachHang });
        }
    }
}
