using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QLKSan.ChatBot.Business;
using QLKSan.ChatBot.Tools;
using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Providers
{
    // Trả lời theo từ khoá (không cần khoá API, không tốn phí): nhận diện ý định, đọc ngày ở / số khách,
    // lấy dữ liệu công khai qua Gateway. Dùng làm mặc định và làm dự phòng khi provider LLM lỗi.
    public class RuleBasedProvider : ILlmProvider
    {
        public const string TenProvider = "RuleBased";
        private const int SoKhachMacDinh = 2;
        private const int SoDemToiDa = 30;
        private static readonly CultureInfo Vi = new CultureInfo("vi-VN");

        public static readonly List<string> GoiYMacDinh = new()
        {
            "Cuối tuần này còn phòng cho 2 người không?",
            "Bảng giá các hạng phòng",
            "Giờ nhận và trả phòng?",
            "Khách sạn có những dịch vụ gì?"
        };

        private readonly HotelDataClient _data;
        private readonly BookingLinks _links;

        public RuleBasedProvider(HotelDataClient data, BookingLinks links)
        {
            _data = data;
            _links = links;
        }

        public string Ten => TenProvider;

        public async Task<BotReply> ReplyAsync(IReadOnlyList<ChatTurn> lichSu, string cauHoi, CancellationToken ct)
        {
            var s = StayParser.Normalize(cauHoi);
            var today = DateTime.Today;
            var slots = StayParser.Parse(cauHoi, today);

            if (Co(s, @"\bcam on\b|\bthank")) return new BotReply { NoiDung = "Rất hân hạnh được hỗ trợ quý khách. Quý khách cần thêm thông tin gì cứ nhắn cho tôi nhé.", GoiY = GoiYMacDinh };

            var hoiPhong = Co(s, @"\bphong trong\b|\bcon phong\b|\bdat phong\b|\bdat cho\b|\bbook|\bcon cho\b|\bkiem tra phong\b|\bthue phong\b");
            var chuDeKhac = Co(s, @"\bgio\b|\bhuy\b|\bchinh sach\b|\bphu thu\b|\bdich vu\b|\bspa\b|\ban sang\b|\bdanh gia\b|\breview|\bdia chi\b|\blien he\b|\bhotline\b|\bthanh vien\b|\btich diem\b");
            // Không nói "phòng" nhưng có ngày ở rõ ràng (ngày dạng số, hoặc kèm đêm / người / khách) vẫn hiểu là hỏi phòng;
            // "hôm nay thời tiết sao" thì không
            var tinHieuO = Co(s, @"\b\d{1,2}\s*[/.\-]\s*\d{1,2}\b|\bthang \d|\bdem\b|\bnguoi\b|\bkhach\b|\bphong\b");
            // Câu trước đã hỏi phòng, câu này chỉ bổ sung ngày / số khách
            var truoc = lichSu.Where(t => t.VaiTro == ChatRoles.Khach).Reverse().Select(t => StayParser.Parse(t.NoiDung, today)).ToList();
            if (hoiPhong || (!chuDeKhac && tinHieuO && (slots.CoNgay || (slots.SoNguoi != null && truoc.Any(t => t.CoNgay)))))
            {
                var ngay = slots.CoNgay ? slots : truoc.FirstOrDefault(t => t.CoNgay);
                var soNguoi = slots.SoNguoi ?? truoc.Select(t => t.SoNguoi).FirstOrDefault(n => n != null);
                return await PhongTrongAsync(ngay?.CheckIn, ngay?.CheckOut, soNguoi, ct);
            }

            if (Co(s, @"\bgio\b.*\b(nhan|tra)\b|\b(nhan|tra) phong\b|\bcheck ?-?(in|out)\b|\bhuy\b|\bchinh sach\b|\bphu thu\b|\btra muon\b"))
                return await ChinhSachAsync(ct);
            if (Co(s, @"\bthanh vien\b|\btich diem\b|\bdiem thuong\b|\bdoi diem\b|\bhang (dong|bac|vang|kim cuong)\b"))
                return await ThanhVienAsync(ct);
            if (Co(s, @"\bdich vu\b|\bspa\b|\ban sang\b|\bbuffet\b|\bdua don\b|\bxe\b|\bgiat\b|\bmassage\b|\bnha hang\b|\btien ich\b"))
                return await DichVuAsync(ct);
            if (Co(s, @"\bdanh gia\b|\breview|\bnhan xet\b|\bco tot khong\b"))
                return await DanhGiaAsync(ct);
            if (Co(s, @"\bdia chi\b|\bo dau\b|\blien he\b|\bhotline\b|\bso dien thoai\b|\bsdt\b|\bemail\b|\bban do\b|\bduong di\b"))
                return await LienHeAsync(ct);
            if (Co(s, @"\bgia\b(?! dinh)|\bbang gia\b|\bbao nhieu tien\b|\bhang phong\b|\bloai phong\b|\bphong nao\b|\bcac phong\b"))
                return await HangPhongAsync(ct);
            if (Co(s, @"^(xin )?chao\b|\bhello\b|^hi\b|^alo\b"))
                return await ChaoAsync(ct);

            var hotel = await _data.GetHotelAsync(ct);
            return new BotReply
            {
                NoiDung = "Xin lỗi, tôi chưa hiểu câu hỏi này. Tôi có thể giúp quý khách tìm phòng trống, xem bảng giá, giờ nhận/trả phòng, dịch vụ và đánh giá của khách."
                    + (string.IsNullOrWhiteSpace(hotel?.Hotline) ? "" : $" Cần hỗ trợ trực tiếp, quý khách vui lòng gọi hotline {hotel.Hotline}."),
                GoiY = GoiYMacDinh
            };
        }

        private async Task<BotReply> ChaoAsync(CancellationToken ct)
        {
            var hotel = await _data.GetHotelAsync(ct);
            return new BotReply
            {
                NoiDung = $"Xin chào quý khách, tôi là trợ lý ảo của {hotel?.Ten ?? "khách sạn"}. Tôi có thể giúp tìm phòng trống, báo giá và giải đáp chính sách lưu trú.",
                GoiY = GoiYMacDinh
            };
        }

        private async Task<BotReply> PhongTrongAsync(DateTime? checkIn, DateTime? checkOut, int? soNguoi, CancellationToken ct)
        {
            if (checkIn == null || checkOut == null)
                return new BotReply
                {
                    NoiDung = "Quý khách muốn nhận phòng ngày nào và ở mấy đêm? Ví dụ: \"20/11 đến 22/11, 2 người\" hoặc \"ngày mai 2 đêm\".",
                    GoiY = new List<string> { "Ngày mai 1 đêm, 2 người", "Cuối tuần này 2 người", "Hôm nay 1 đêm, 1 người" }
                };
            if (checkIn < DateTime.Today)
                return new BotReply { NoiDung = $"Ngày nhận phòng {checkIn:dd/MM/yyyy} đã qua. Quý khách vui lòng chọn từ hôm nay trở đi." };
            var soDem = (checkOut.Value - checkIn.Value).Days;
            if (soDem > SoDemToiDa)
                return new BotReply { NoiDung = $"Tôi chỉ tra được kỳ nghỉ tối đa {SoDemToiDa} đêm. Với kỳ nghỉ dài, quý khách vui lòng liên hệ lễ tân để được báo giá riêng." };

            var khach = soNguoi ?? SoKhachMacDinh;
            var ketQua = (await _data.FindAvailableAsync(checkIn.Value, checkOut.Value, khach, ct))
                .Where(x => x.SoPhongTrong > 0).OrderBy(x => x.BaoGia?.TongTien ?? x.LoaiPhong.GiaThueNgay).ToList();
            var ky = $"{checkIn:dd/MM} – {checkOut:dd/MM/yyyy} ({soDem} đêm, {khach} khách{(soNguoi == null ? ", quý khách có thể nhắn số khách khác" : "")})";
            if (ketQua.Count == 0)
            {
                var hotel = await _data.GetHotelAsync(ct);
                return new BotReply
                {
                    NoiDung = $"Rất tiếc, {ky} hiện đã hết phòng phù hợp. Quý khách thử ngày khác"
                        + (string.IsNullOrWhiteSpace(hotel?.Hotline) ? "." : $" hoặc gọi hotline {hotel.Hotline} để được hỗ trợ."),
                    GoiY = new List<string> { "Cuối tuần này 2 người", "Bảng giá các hạng phòng" }
                };
            }

            var sb = new StringBuilder($"Kỳ nghỉ {ky}, hiện còn:");
            foreach (var x in ketQua)
            {
                var tong = x.BaoGia?.TongTien ?? x.LoaiPhong.GiaThueNgay * soDem;
                sb.Append($"\n• {x.LoaiPhong.TenLoai}: còn {x.SoPhongTrong} phòng, tổng {Tien(tong)}")
                  .Append(soDem > 1 ? $" (trung bình {Tien(Math.Round(tong / soDem, 0))}/đêm)" : "");
            }
            sb.Append("\nGiá đã theo mùa / cuối tuần, chưa gồm ưu đãi thành viên và VAT. Bấm nút bên dưới để đặt — ngày và số khách đã được điền sẵn.");
            return new BotReply
            {
                NoiDung = sb.ToString(),
                LienKet = ketQua.Take(4).Select(x => _links.DatPhong(x.LoaiPhong.MaLoai, x.LoaiPhong.TenLoai, checkIn.Value, checkOut.Value, khach)).ToList(),
                GoiY = new List<string> { "Giờ nhận và trả phòng?", "Chính sách huỷ phòng?" }
            };
        }

        private async Task<BotReply> HangPhongAsync(CancellationToken ct)
        {
            var loai = (await _data.GetRoomTypesAsync(ct)).OrderBy(l => l.GiaThueNgay).ToList();
            var sb = new StringBuilder("Các hạng phòng của khách sạn:");
            foreach (var l in loai)
            {
                sb.Append($"\n• {l.TenLoai}: từ {Tien(l.GiaThueNgay)}/đêm, tối đa {l.SoNguoiToiDa} khách");
                if (l.DienTich != null) sb.Append($", {l.DienTich:0.#} m²");
                if (!string.IsNullOrWhiteSpace(l.HuongNhin)) sb.Append($", {l.HuongNhin}");
                if (l.SoDanhGia > 0 && l.DiemDanhGia != null) sb.Append($", ★ {l.DiemDanhGia:0.0} ({l.SoDanhGia} đánh giá)");
            }
            sb.Append("\nGiá có thể thay đổi theo mùa và cuối tuần. Quý khách nhắn ngày ở để tôi báo giá chính xác.");
            return new BotReply
            {
                NoiDung = sb.ToString(),
                LienKet = loai.Take(4).Select(l => _links.HangPhong(l.MaLoai, l.TenLoai)).ToList(),
                GoiY = new List<string> { "Cuối tuần này còn phòng cho 2 người không?", "Ngày mai 1 đêm, 2 người" }
            };
        }

        private async Task<BotReply> ChinhSachAsync(CancellationToken ct)
        {
            var h = await _data.GetHotelAsync(ct);
            var sb = new StringBuilder($"Giờ nhận phòng từ {h.GioNhanPhong}, trả phòng trước {h.GioTraPhong}.");
            if (h.PhuThuTraMuon.Any())
                sb.Append(" Trả phòng muộn có phụ thu: ").Append(string.Join(", ", h.PhuThuTraMuon.Select(p => $"đến {p.DenGio} thêm {p.PhanTram}% giá một đêm"))).Append('.');
            if (!string.IsNullOrWhiteSpace(h.ChinhSachHuy)) sb.Append($"\nChính sách huỷ: {h.ChinhSachHuy}");
            return new BotReply { NoiDung = sb.ToString(), GoiY = new List<string> { "Cuối tuần này còn phòng cho 2 người không?", "Chương trình thành viên" } };
        }

        private async Task<BotReply> ThanhVienAsync(CancellationToken ct)
        {
            var tv = (await _data.GetHotelAsync(ct)).ThanhVien;
            var sb = new StringBuilder($"Chương trình thành viên: tích 1 điểm cho mỗi {Tien(tv.SoTienMotDiem)}, 1 điểm trị giá {Tien(tv.GiaTriMotDiem)} khi đổi (tối thiểu {tv.DiemDoiToiThieu} điểm).");
            foreach (var hang in tv.Hangs)
                sb.Append($"\n• {hang.Ten} (từ {hang.DiemToiThieu.ToString("N0", Vi)} điểm): {string.Join("; ", hang.QuyenLoi)}");
            return new BotReply { NoiDung = sb.ToString(), GoiY = GoiYMacDinh };
        }

        private async Task<BotReply> DichVuAsync(CancellationToken ct)
        {
            var dv = (await _data.GetServicesAsync(ct)).OrderByDescending(d => d.LaTraiNghiem).ThenBy(d => d.TenDichVu).Take(10).ToList();
            if (dv.Count == 0) return new BotReply { NoiDung = "Hiện chưa có dịch vụ nào đang phục vụ. Quý khách vui lòng liên hệ lễ tân để biết thêm.", GoiY = GoiYMacDinh };
            var sb = new StringBuilder("Dịch vụ đang phục vụ (đặt tại quầy lễ tân hoặc gọi từ phòng):");
            foreach (var d in dv) sb.Append($"\n• {d.TenDichVu}: {Tien(d.GiaDichVu)}{(string.IsNullOrWhiteSpace(d.DonViTinh) ? "" : "/" + d.DonViTinh)}");
            return new BotReply { NoiDung = sb.ToString(), GoiY = GoiYMacDinh };
        }

        private async Task<BotReply> DanhGiaAsync(CancellationToken ct)
        {
            var loai = await _data.GetRoomTypesAsync(ct);
            var soDanhGia = loai.Sum(l => l.SoDanhGia);
            var sb = new StringBuilder();
            if (soDanhGia > 0)
                sb.Append($"Khách lưu trú chấm trung bình ★ {loai.Where(l => l.DiemDanhGia != null).Sum(l => l.DiemDanhGia.Value * l.SoDanhGia) / soDanhGia:0.0}/5 ({soDanhGia} đánh giá).");
            foreach (var r in (await _data.GetFeaturedReviewsAsync(ct)).Take(3))
                sb.Append($"\n• {new string('★', Math.Clamp(r.SoSao, 1, 5))} \"{r.NoiDung}\" — {r.TenHienThi ?? "Khách lưu trú"}{(string.IsNullOrWhiteSpace(r.TenLoaiPhong) ? "" : $", {r.TenLoaiPhong}")}");
            return new BotReply
            {
                NoiDung = sb.Length == 0 ? "Hiện chưa có đánh giá nào được công bố." : sb.ToString().TrimStart('\n'),
                GoiY = new List<string> { "Bảng giá các hạng phòng", "Cuối tuần này còn phòng cho 2 người không?" }
            };
        }

        private async Task<BotReply> LienHeAsync(CancellationToken ct)
        {
            var h = await _data.GetHotelAsync(ct);
            var reply = new BotReply
            {
                NoiDung = $"{h.Ten} — {h.DiaChi}. Hotline: {h.Hotline}" + (string.IsNullOrWhiteSpace(h.Email) ? "." : $", email: {h.Email}."),
                GoiY = GoiYMacDinh
            };
            if (!string.IsNullOrWhiteSpace(h.BanDoUrl)) reply.LienKet.Add(new ChatLinkDto { Nhan = "Xem bản đồ", Url = h.BanDoUrl });
            return reply;
        }

        private static bool Co(string s, string pattern) => Regex.IsMatch(s, pattern);

        private static string Tien(decimal v) => v.ToString("N0", Vi) + " ₫";
    }
}
