using System.Text.Json;
using System.Text.Json.Nodes;
using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Tools
{
    // Công cụ cho mô hình ngôn ngữ (tool calling): chỉ đọc dữ liệu công khai qua Gateway.
    // Không có công cụ đặt / huỷ phòng: tìm phòng trống trả kèm liên kết đặt phòng điền sẵn để khách tự xác nhận trên Web.
    public class HotelToolbox
    {
        private static readonly JsonSerializerOptions Json = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private readonly HotelDataClient _data;
        private readonly BookingLinks _links;

        public HotelToolbox(HotelDataClient data, BookingLinks links)
        {
            _data = data;
            _links = links;
        }

        // Khai báo công cụ theo JSON Schema (định dạng chung của các API tool calling)
        public static IReadOnlyList<(string Ten, string MoTa, JsonObject ThamSo)> Definitions { get; } = new List<(string, string, JsonObject)>
        {
            ("thong_tin_khach_san", "Tên, địa chỉ, hotline, email, giờ nhận/trả phòng, phụ thu trả muộn, chính sách huỷ và chương trình thành viên của khách sạn.", Schema()),
            ("danh_sach_hang_phong", "Các hạng phòng: mã, tên, giá từ (một đêm, giá gốc), số khách tối đa, diện tích, hướng nhìn, loại giường, điểm đánh giá.", Schema()),
            ("tim_phong_trong", "Tìm hạng phòng còn trống và báo giá cho kỳ nghỉ (giá theo mùa / cuối tuần, chưa gồm ưu đãi thành viên và VAT). Kết quả tự kèm nút đặt phòng điền sẵn cho khách.",
                Schema(("check_in", "string", "Ngày nhận phòng, định dạng yyyy-MM-dd"), ("check_out", "string", "Ngày trả phòng, định dạng yyyy-MM-dd, sau ngày nhận"), ("so_nguoi", "integer", "Số khách, mặc định 2"))),
            ("danh_sach_dich_vu", "Các dịch vụ đang phục vụ (spa, ăn uống, đưa đón...) và giá.", Schema()),
            ("danh_gia_noi_bat", "Một số đánh giá tốt gần đây của khách lưu trú.", Schema())
        };

        // Trả về kết quả dạng JSON cho mô hình và các liên kết sẽ hiển thị thành nút cho khách
        public async Task<(string Ketqua, List<ChatLinkDto> LienKet)> ExecuteAsync(string ten, string thamSoJson, CancellationToken ct)
        {
            var lienKet = new List<ChatLinkDto>();
            object ketQua;
            try
            {
                var args = string.IsNullOrWhiteSpace(thamSoJson) ? new JsonObject() : JsonNode.Parse(thamSoJson)?.AsObject() ?? new JsonObject();
                switch (ten)
                {
                    case "thong_tin_khach_san":
                        var h = await _data.GetHotelAsync(ct);
                        ketQua = new
                        {
                            ten = h.Ten, dia_chi = h.DiaChi, hotline = h.Hotline, email = h.Email,
                            gio_nhan_phong = h.GioNhanPhong, gio_tra_phong = h.GioTraPhong,
                            phu_thu_tra_muon = h.PhuThuTraMuon.Select(p => new { den_gio = p.DenGio, phan_tram_gia_dem = p.PhanTram }),
                            chinh_sach_huy = h.ChinhSachHuy,
                            thanh_vien = new
                            {
                                so_tien_mot_diem = h.ThanhVien?.SoTienMotDiem, gia_tri_mot_diem = h.ThanhVien?.GiaTriMotDiem,
                                hang = h.ThanhVien?.Hangs.Select(x => new { ten = x.Ten, diem_toi_thieu = x.DiemToiThieu, quyen_loi = x.QuyenLoi })
                            }
                        };
                        break;
                    case "danh_sach_hang_phong":
                        ketQua = (await _data.GetRoomTypesAsync(ct)).OrderBy(l => l.GiaThueNgay).Select(l => new
                        {
                            ma_loai = l.MaLoai, ten = l.TenLoai, gia_tu_moi_dem = l.GiaThueNgay, so_khach_toi_da = l.SoNguoiToiDa,
                            dien_tich_m2 = l.DienTich, huong_nhin = l.HuongNhin, loai_giuong = l.LoaiGiuong, diem_danh_gia = l.DiemDanhGia, so_danh_gia = l.SoDanhGia
                        });
                        break;
                    case "tim_phong_trong":
                        if (!DateTime.TryParse(args["check_in"]?.ToString(), out var checkIn) || !DateTime.TryParse(args["check_out"]?.ToString(), out var checkOut))
                        {
                            ketQua = new { loi = "Cần check_in và check_out dạng yyyy-MM-dd." };
                            break;
                        }
                        checkIn = checkIn.Date;
                        checkOut = checkOut.Date;
                        var soNguoi = args["so_nguoi"] is JsonValue v && v.TryGetValue<int>(out var n) && n > 0 ? n : 2;
                        if (checkIn < DateTime.Today) { ketQua = new { loi = "Ngày nhận phòng đã qua." }; break; }
                        if (checkOut <= checkIn || (checkOut - checkIn).Days > 30) { ketQua = new { loi = "Ngày trả phải sau ngày nhận và kỳ nghỉ tối đa 30 đêm." }; break; }
                        var trong = (await _data.FindAvailableAsync(checkIn, checkOut, soNguoi, ct)).Where(x => x.SoPhongTrong > 0)
                            .OrderBy(x => x.BaoGia?.TongTien ?? x.LoaiPhong.GiaThueNgay).ToList();
                        lienKet.AddRange(trong.Take(4).Select(x => _links.DatPhong(x.LoaiPhong.MaLoai, x.LoaiPhong.TenLoai, checkIn, checkOut, soNguoi)));
                        ketQua = new
                        {
                            check_in = checkIn.ToString("yyyy-MM-dd"), check_out = checkOut.ToString("yyyy-MM-dd"), so_dem = (checkOut - checkIn).Days, so_nguoi = soNguoi,
                            hang_phong_con_trong = trong.Select(x => new
                            {
                                ma_loai = x.LoaiPhong.MaLoai, ten = x.LoaiPhong.TenLoai, so_phong_trong = x.SoPhongTrong,
                                tong_tien = x.BaoGia?.TongTien, gia_trung_binh_dem = x.BaoGia?.GiaTrungBinhDem
                            }),
                            ghi_chu = trong.Count == 0 ? "Hết phòng phù hợp." : "Nút đặt phòng điền sẵn đã được gửi kèm cho khách."
                        };
                        break;
                    case "danh_sach_dich_vu":
                        ketQua = (await _data.GetServicesAsync(ct)).Select(d => new { ten = d.TenDichVu, gia = d.GiaDichVu, don_vi = d.DonViTinh, mo_ta = d.MoTa });
                        break;
                    case "danh_gia_noi_bat":
                        ketQua = (await _data.GetFeaturedReviewsAsync(ct)).Select(r => new { so_sao = r.SoSao, noi_dung = r.NoiDung, khach = r.TenHienThi, hang_phong = r.TenLoaiPhong });
                        break;
                    default:
                        ketQua = new { loi = $"Không có công cụ {ten}." };
                        break;
                }
            }
            catch (HttpRequestException)
            {
                ketQua = new { loi = "Tạm thời không lấy được dữ liệu khách sạn." };
            }
            catch (JsonException)
            {
                ketQua = new { loi = "Tham số công cụ không phải JSON hợp lệ." };
            }
            return (JsonSerializer.Serialize(ketQua, Json), lienKet);
        }

        private static JsonObject Schema(params (string Ten, string Kieu, string MoTa)[] thamSo)
        {
            var props = new JsonObject();
            foreach (var p in thamSo) props[p.Ten] = new JsonObject { ["type"] = p.Kieu, ["description"] = p.MoTa };
            var schema = new JsonObject { ["type"] = "object", ["properties"] = props };
            var batBuoc = thamSo.Where(p => p.Ten != "so_nguoi").Select(p => (JsonNode)p.Ten).ToArray();
            if (batBuoc.Length > 0) schema["required"] = new JsonArray(batBuoc);
            return schema;
        }
    }
}
