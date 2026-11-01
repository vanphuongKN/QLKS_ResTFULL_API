# LUXE RESERVE — Hệ thống quản lý khách sạn

Hệ thống quản lý khách sạn theo mô hình **RESTful API + ứng dụng Web (MVC)** trên .NET 10 và SQL Server.

- **Khách hàng** xem hạng phòng, đặt phòng trực tuyến theo hạng và theo ngày, theo dõi kỳ nghỉ, đánh giá, tích và đổi điểm thành viên.
- **Nhân viên** vận hành theo vai trò: lễ tân, buồng phòng, kế toán và quản trị.

## Kiến trúc

```
Trình duyệt ──► QLKSan.Web (MVC, cookie)  ──HTTP + JWT──►  QLKSan.API (REST)  ──EF Core──►  SQL Server
                    │                                          │
                    └──────────── QLKSan.Shared (DTO, Request, Enum, Validation) ───────────────┘
```

| Project | Vai trò |
|---|---|
| `QLKSan/QLKSan.API` | Project **duy nhất** truy cập CSDL. Nghiệp vụ nằm trong `Business/`, controller mỏng. Xác thực bằng JWT. Lỗi trả về theo chuẩn ProblemDetails: 400 dữ liệu sai, 404 không tìm thấy, 409 sai trạng thái. |
| `QLKSan/QLKSan.Web` | Ứng dụng MVC cho khách và nhân viên. Chỉ gọi API qua `ApiClients/`, không truy cập CSDL. Đăng nhập bằng cookie và giữ JWT để gọi API. |
| `QLKSan/QLKSan.Shared` | Hợp đồng dữ liệu dùng chung: `Models/DTOs`, `Models/Requests`, `Enums`, `Validation`. |
| `QLKSan/QLKSan.Tests` | Unit test xUnit cho phần tính toán thuần: tiền phòng, phụ thu, khuyến mãi, ưu đãi thành viên, giá theo mùa, kiểm tra phòng trống. |
| `DB/Quanlykhachsanc#.sql` | Script tạo CSDL và dữ liệu mẫu. |

Quy ước REST:
- Các thao tác nghiệp vụ dùng `POST /resource/{id}/hanh-dong`, đặt tên kebab-case (ví dụ `approve`, `check-in`, `change-room`).
- Đổi trạng thái hoặc cờ dùng `PATCH /resource/{id}` kèm body tường minh.

Danh sách endpoint xem tại [docs/API.md](docs/API.md). Khi chạy ở môi trường Development, API có Swagger tại `https://localhost:7001/swagger`.

## Yêu cầu

- .NET SDK 10
- SQL Server 2019 trở lên (đăng nhập `sa` hoặc sửa chuỗi kết nối)
- Visual Studio 2022 17.14+ / VS Code, hoặc chỉ cần dòng lệnh

## Cài đặt

1. **Tạo CSDL.** Mở `DB/Quanlykhachsanc#.sql` trong SSMS rồi chạy (file UTF-8 có BOM, đã `SET QUOTED_IDENTIFIER ON`). Script tạo database `Quanlykhachsanc#` cùng dữ liệu mẫu.
2. **Chuỗi kết nối và khoá JWT** khi phát triển nằm trong `QLKSan/QLKSan.API/appsettings.Development.json`. `appsettings.json` để trống hai giá trị này; môi trường thật đặt bằng biến môi trường `ConnectionStrings__DefaultConnection`, `Jwt__Key` hoặc `dotnet user-secrets`. Thiếu, hoặc môi trường thật vẫn dùng khoá dev, thì API dừng ngay khi khởi động kèm thông báo.
3. **Tự nâng cấp CSDL.** Mỗi lần API khởi động, `Data/DatabaseSchemaFixer.cs` tự bổ sung cột/bảng còn thiếu. Các bước có thể chạy lặp lại an toàn. Vì vậy CSDL tạo từ script cũ vẫn dùng được. Nên sao lưu database trước khi chạy phiên bản mới.

## Chạy hệ thống

**Cách nhanh:** chạy `QLKSan/run.bat`. File này build solution, mở API (`https://localhost:7001`) và Web (`https://localhost:7234`), rồi mở trình duyệt.

**Visual Studio:** mở `QLKSan/QLKSan.slnx` và đặt Multiple startup projects = `QLKSan.API` + `QLKSan.Web` (profile `https`).

**Dòng lệnh:**

```bash
cd QLKSan
dotnet run --project QLKSan.API --launch-profile https
dotnet run --project QLKSan.Web --launch-profile https
```

> Không mở trực tiếp `bin\...\QLKSan.Web.exe`: khi đó ứng dụng chạy ở môi trường Production, sẽ không có CSS/JS và không biết địa chỉ API.

### Tài khoản mẫu (mật khẩu `123456`)

| Vai trò | Tài khoản |
|---|---|
| Quản trị | `admin1`, `admin2` |
| Lễ tân | `letan1`, `letan2`, `letan3` |
| Buồng phòng | `buongphong1` |
| Kế toán | `ketoan1` |
| Khách hàng | `khach1` … `khach5` |

## Cấu hình nghiệp vụ

Quản lý sửa trên web tại **Quản trị → Cấu hình khách sạn**. Thay đổi được lưu vào bảng `CauHinh` và có hiệu lực ngay. Mục nào chưa từng lưu thì dùng giá trị mặc định trong `QLKSan.API/appsettings.json`:

| Mục | Nội dung |
|---|---|
| `HotelInfo` | Tên, mô tả, địa chỉ, hotline, email, bản đồ. Hiển thị ở footer và trang khách. **Các giá trị hiện tại là dữ liệu minh hoạ, cần thay bằng thông tin thật.** |
| `HotelPolicy` | Giờ nhận/trả phòng, mốc phụ thu trả muộn, tự xác nhận phiếu online (`TuDongXacNhanDatPhong`), % cọc khách vãng lai. |
| `NightAudit` | `NgayBatDau`: ngày đầu tiên phải chốt ngày (chỉ sửa trong `appsettings.json`); các ngày trước mốc này không cần chốt. |
| `Loyalty` | Chương trình thành viên: số tiền cho 1 điểm, giá trị 1 điểm khi đổi, số điểm đổi tối thiểu, các hạng (điểm tối thiểu, % giảm tiền phòng, quyền lợi). |
| `Jwt` | Khoá ký và thời hạn token. **Đổi `Key` khi triển khai thật.** |

Phía Web chỉ cần `ApiSettings:BaseUrl` trong `QLKSan.Web/appsettings.json`.

## Ảnh

- **Ảnh mẫu** nằm ở `QLKSan.Web/wwwroot/images`.
- **Ảnh hạng phòng do quản trị tải lên** (Quản trị → Loại phòng → nút *Bộ ảnh*) được API lưu ở `QLKSan.API/wwwroot/uploads/room-types/<mã loại>/` và phục vụ tại `<API>/uploads/...`. Thư mục này đã được đưa vào `.gitignore`. Khi triển khai cần sao lưu cùng database.
- Ảnh đầu tiên trong bộ ảnh là ảnh đại diện của hạng phòng.

## Chức năng chính

**Khách hàng**
- Trang chủ, chi tiết hạng phòng (bộ ảnh, tiện nghi, đánh giá) và các trang Câu chuyện, Ẩm thực, Spa, Thư viện ảnh, Đặc quyền thành viên.
- Đặt phòng 3 bước:
  - xem phòng trống và giá từng đêm mà không cần đăng nhập;
  - đặt theo hạng phòng, hệ thống tự xếp phòng;
  - ghi yêu cầu riêng và giờ đến dự kiến;
  - nhập mã ưu đãi;
  - phiếu được xác nhận ngay nếu còn phòng.
- Trang chi tiết phiếu: tiến trình, đếm ngược ngày đến, giá từng đêm, hoá đơn, huỷ trước ngày nhận phòng.
- Đánh giá theo từng kỳ nghỉ đã trả phòng.
- Hồ sơ: sửa thông tin liên hệ, đổi mật khẩu, xem hạng thành viên và điểm.
- Khách từng ở dạng vãng lai có thể đăng ký tài khoản và liên kết với hồ sơ cũ (đối chiếu CCCD và số điện thoại).

**Nhân viên**
- **Lễ tân:**
  - màn hình đầu ca: khách đến / đi (kể cả quá hạn trả) / đang ở hôm nay, phiếu sắp đến, phòng trống tối nay, việc cần làm, bàn giao từ ca trước;
  - đặt phòng qua quầy cho ngày tới (điện thoại, email, tại quầy, đại lý), tra khách cũ theo CCCD / SĐT với cảnh báo VIP, hạn chế, sở thích;
  - duyệt, đổi phòng, đổi ngày, huỷ (hoàn hoặc giữ cọc); nhận phòng kèm số khách thực tế, cọc và người đi cùng; nhận khách vãng lai;
  - trong kỳ lưu trú: gia hạn / rút ngắn có báo giá, đổi phòng khi đang ở, gọi và huỷ dịch vụ gọi nhầm, thu thêm cọc (sổ cọc ghi phương thức và người thu);
  - sổ yêu cầu khách (báo thức, đặt xe, nhắn gửi, đồ thất lạc…), giao ca có đối soát tiền mặt và ghi chú bàn giao;
  - lịch phòng, sổ khách lưu trú gồm cả người đi cùng (xuất Excel), giao việc buồng phòng.
- **Thu ngân:**
  - bảng kê lưu trú từng đêm trước khi chốt; lập hoá đơn theo số đêm thực tế, giá theo mùa/cuối tuần/lễ, phụ thu trả muộn, khuyến mãi, ưu đãi hạng thành viên và trừ tiền cọc;
  - nhận thanh toán: đổi điểm, tách tối đa 3 khoản theo phương thức, thông tin xuất hoá đơn VAT công ty.
- **Buồng phòng:**
  - sơ đồ buồng phòng theo tầng, việc xếp ưu tiên theo khách sắp đến (VIP, giờ đến), giao việc theo nhóm / cả tầng;
  - dọn sau trả phòng / đổi phòng / sửa chữa, dọn hằng ngày phòng có khách, chỉnh trang buổi tối (cấu hình `Housekeeping` trong `appsettings.json`), "Không làm phiền" và giờ dọn khách muốn;
  - nhân viên dọn xong → quản lý / lễ tân kiểm tra theo danh mục của hạng phòng (chưa đạt thì trả lại dọn lại);
  - ghi minibar vào bảng kê của khách, phiếu kỹ thuật cho sự cố (khoá phòng tới khi sửa xong), yêu cầu khách loại Buồng phòng / Kỹ thuật tự chuyển thành việc.
- **Chốt ngày (kế toán hoặc lễ tân):** chốt lần lượt từng ngày đã qua; ghi nhận tiền phòng theo đêm và cọc giữ lại thành doanh thu khác, huỷ phiếu khách không đến (giữ cọc, bỏ đánh dấu cho khách báo đến muộn), lưu số liệu ngày và bản in; cảnh báo trên trang lễ tân / kế toán / quản lý khi còn ngày chưa chốt; quản lý mở lại ngày gần nhất kèm lý do.
- **Kế toán:** tổng quan kỳ so với kỳ trước, đối soát giao ca và sổ cọc, hoá đơn VAT công ty, danh sách hoá đơn (cọc trừ vào hoá đơn và cọc thừa đã hoàn tách riêng, đủ các phương thức khi tách thanh toán), hoàn tiền (phần đã trả bằng điểm không được hoàn), báo cáo doanh thu và công suất có ưu đãi hạng thành viên (xuất Excel).
- **Quản trị:** loại phòng (thông tin và bộ ảnh), phòng, dịch vụ (đánh dấu mục "Trải nghiệm"), nhân viên, khuyến mãi, bảng giá theo mùa, đánh giá.

## Kiểm thử

```bash
cd QLKSan
dotnet test QLKSan.Tests
```

## Chạy bằng Docker

Cần Docker Desktop. Trong thư mục `QLKSan/`:

```bash
cp .env.example .env      # đổi SA_PASSWORD và JWT_KEY
docker compose up -d --build
```

- Web: http://localhost:7081 · API: http://localhost:7080/api/health · SQL Server: `localhost,14330` (tài khoản `sa`).
- Lần đầu, dịch vụ `db-init` tạo database từ `DB/Quanlykhachsanc#.sql`. Các lần sau giữ nguyên dữ liệu; API tự nâng cấp cấu trúc khi khởi động.
- Dữ liệu SQL Server, ảnh tải lên và khoá cookie đăng nhập nằm trong volume (`sqldata`, `uploads`, `webkeys`), nên vẫn còn khi khởi động lại. `docker compose down -v` xoá toàn bộ.
- Container chạy theo múi giờ `Asia/Ho_Chi_Minh`, vì nghiệp vụ nhận/trả phòng và chốt ngày dùng giờ địa phương.
- Web gọi API qua địa chỉ nội bộ `http://api:8080`. Ảnh tải lên được trình duyệt tải qua `ApiSettings:PublicUrl`.

## Triển khai (publish)

```bash
cd QLKSan
dotnet publish QLKSan.API -c Release -o publish/api
dotnet publish QLKSan.Web -c Release -o publish/web
```

Khi triển khai:
- Sửa `appsettings.json` (hoặc biến môi trường) cho từng ứng dụng:
  - API: `ConnectionStrings__DefaultConnection`, `Jwt__Key`;
  - Web: `ApiSettings__BaseUrl` trỏ tới địa chỉ API thật; `ApiSettings__PublicUrl` nếu trình duyệt truy cập API qua địa chỉ khác; `DataProtection__KeysPath` để lưu khoá cookie đăng nhập.
- Chạy sau IIS / reverse proxy với HTTPS.
- Cấp quyền ghi thư mục `wwwroot/uploads` cho tiến trình API.
