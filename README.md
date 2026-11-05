# LUXE RESERVE — Hệ thống quản lý khách sạn

Hệ thống quản lý khách sạn theo mô hình **RESTful API + ứng dụng Web** trên .NET 10 và SQL Server. Giao diện chính là ứng dụng React (SPA) cho cả khách và nhân viên; ứng dụng MVC cũ giữ làm dự phòng. Hệ thống mở rộng theo hướng microservice: API Gateway (YARP), service thông báo nhận sự kiện qua RabbitMQ, trợ lý ảo (chatbot) cho khách.

- **Khách hàng** xem hạng phòng, đặt phòng trực tuyến theo hạng và theo ngày, theo dõi kỳ nghỉ, đánh giá, tích và đổi điểm thành viên.
- **Nhân viên** vận hành theo vai trò: lễ tân, buồng phòng, kế toán và quản trị.

## Kiến trúc

```
Trình duyệt ──► QLKSan.Client (React SPA: khách + /nhan-vien) ──/api (cùng nguồn)──┐
Trình duyệt ──► QLKSan.Web (MVC dự phòng, cookie) ──HTTP + JWT──► QLKSan.Gateway (YARP) ──► QLKSan.API (REST) ──EF Core──► SQL Server
   (widget chat)                                          giới hạn tần suất  │         │ outbox (SuKienCho)
                                                         /api/chat/* ──► QLKSan.ChatBot ──(dữ liệu công khai qua Gateway)
                                                                          SQLite riêng · RuleBased / OpenAI
                                                                                      ▼
                                              QLKSan.Notification ◄── RabbitMQ (exchange qlksan.su-kien)
                                              gửi email cho khách
Hợp đồng dùng chung: QLKSan.Shared (DTO, Request, Enum, Validation, sự kiện)
```

Sơ đồ chi tiết (thành phần, luồng đặt phòng → email, luồng trợ lý ảo, giới hạn tần suất, cổng dev / Docker): [docs/KIEN-TRUC.md](docs/KIEN-TRUC.md).

| Project | Vai trò |
|---|---|
| `QLKSan/QLKSan.API` | Project **duy nhất** truy cập CSDL. Nghiệp vụ nằm trong `Business/`, controller mỏng. Xác thực bằng JWT. Lỗi trả về theo chuẩn ProblemDetails: 400 dữ liệu sai, 404 không tìm thấy, 409 sai trạng thái. |
| `QLKSan/QLKSan.Web` | Ứng dụng MVC cho khách và nhân viên, **giữ làm dự phòng** (vẫn build và chạy được, đủ chức năng). Chỉ gọi API (qua Gateway) bằng `ApiClients/`, không truy cập CSDL. Đăng nhập bằng cookie và giữ JWT để gọi API. |
| `QLKSan/QLKSan.Client` | **Giao diện chính**: React + TypeScript (Vite), đủ chức năng như cổng MVC. **Trang khách** (đủ chức năng như cổng MVC): trang chủ, Câu chuyện, Ẩm thực, Spa, Thư viện ảnh, Đặc quyền thành viên, hạng phòng, tìm phòng trống, đặt phòng (kiểm tra mã ưu đãi), kỳ nghỉ của tôi (huỷ, xem / in hoá đơn), đánh giá kỳ nghỉ, hồ sơ thành viên, đổi mật khẩu, trợ lý ảo. **Cổng nhân viên** `/nhan-vien` (tải riêng): đăng nhập theo vai trò, menu và phân quyền giống cổng MVC, cảnh báo chốt ngày; đã có đủ phần buồng phòng (phân công, kiểm tra phòng theo danh mục, việc của tôi, ghi minibar, phiếu kỹ thuật) và lễ tân / thu ngân (quầy lễ tân, nhận phòng, đổi phòng, đổi ngày, đặt phòng qua quầy, khách vãng lai, yêu cầu khách, giao ca, phòng nghỉ, lịch phòng, sổ lưu trú, hồ sơ khách hàng, lịch sử đặt phòng, gọi dịch vụ, trả phòng: lập hoá đơn, đổi điểm, tách thanh toán, xuất VAT công ty), kế toán (tổng quan kỳ so với kỳ trước, hoá đơn, hoàn tiền, ghi số hoá đơn VAT điện tử, đối soát giao ca / sổ cọc, báo cáo doanh thu + xuất Excel) và chốt ngày (xem trước, khách không đến, chốt, bản in, quản lý mở lại), quản trị (bảng điều khiển, nhân sự, cấu hình khách sạn, nhật ký thao tác, loại phòng + bộ ảnh, phòng + tiện nghi, dịch vụ & giá, bảng giá theo mùa, khuyến mãi, đánh giá). Đường dẫn cũ của cổng MVC (bookmark, email, liên kết trợ lý ảo, vd `/Reception`, `/Checkout/Process?maPhong=…`) tự chuyển sang trang React tương ứng. Gọi API qua Gateway cùng nguồn (Vite proxy khi dev, nginx + CSP khi chạy Docker), kiểu dữ liệu sinh từ OpenAPI (`npm run gen:api`), dữ liệu quản lý bằng TanStack Query. |
| `QLKSan/QLKSan.Gateway` | Cổng vào duy nhất (YARP): chuyển `/api/*`, `/uploads/*` tới API lõi theo mục `ReverseProxy` trong `appsettings.json`; giới hạn tần suất theo người dùng (JWT) hoặc theo IP, đăng nhập / đăng ký chặt hơn (trả 429). Không chứa nghiệp vụ. |
| `QLKSan/QLKSan.ChatBot` | Trợ lý ảo cho khách (`/api/chat/conversations`): tìm phòng trống và báo giá, giờ nhận/trả phòng, chính sách, dịch vụ, đánh giá. Provider chọn bằng `ChatBot:Provider`: `RuleBased` (mặc định, không cần khoá) hoặc `OpenAI` (tool calling; lỗi thì tự dùng RuleBased). Chỉ gọi endpoint công khai của API lõi qua Gateway; không đặt phòng mà trả liên kết đặt phòng điền sẵn. Lịch sử hội thoại lưu ở SQLite riêng. |
| `QLKSan/QLKSan.Notification` | Service thông báo: nhận sự kiện phiếu đặt phòng (đặt mới, huỷ, nhận phòng) từ RabbitMQ, gửi email cho khách qua SMTP hoặc ghi file `.eml` khi chưa cấu hình SMTP. Mỗi sự kiện chỉ xử lý một lần. |
| `QLKSan/QLKSan.Shared` | Hợp đồng dữ liệu dùng chung: `Models/DTOs`, `Models/Requests`, `Models/Events` (sự kiện RabbitMQ), `Enums`, `Validation`. |
| `QLKSan/QLKSan.Tests` | Unit test xUnit gồm hai nhóm. (1) Phần tính toán thuần: tiền phòng, phụ thu, khuyến mãi, ưu đãi thành viên, giá theo mùa, kiểm tra phòng trống, đọc ngày ở tiếng Việt. (2) Các service mở rộng, dùng API / OpenAI / upstream giả lập: trợ lý ảo, định tuyến và giới hạn tần suất của Gateway, nội dung email của Notification. |
| `DB/Quanlykhachsanc#.sql` | Script tạo CSDL và dữ liệu mẫu. |

Quy ước REST:
- Các thao tác nghiệp vụ dùng `POST /resource/{id}/hanh-dong`, đặt tên kebab-case (ví dụ `approve`, `check-in`, `change-room`).
- Đổi trạng thái hoặc cờ dùng `PATCH /resource/{id}` kèm body tường minh.

Danh sách endpoint xem tại [docs/API.md](docs/API.md). Khi chạy ở môi trường Development, API có Swagger tại `https://localhost:7001/swagger`.

## Yêu cầu

- .NET SDK 10
- Node.js 22 (cho giao diện chính React `QLKSan.Client`)
- SQL Server 2019 trở lên (đăng nhập `sa` hoặc sửa chuỗi kết nối)
- Visual Studio 2022 17.14+ / VS Code, hoặc chỉ cần dòng lệnh

## Cài đặt

1. **Tạo CSDL.** Mở `DB/Quanlykhachsanc#.sql` trong SSMS rồi chạy (file UTF-8 có BOM, đã `SET QUOTED_IDENTIFIER ON`). Script tạo database `Quanlykhachsanc#` cùng dữ liệu mẫu.
2. **Chuỗi kết nối và khoá JWT** khi phát triển nằm trong `QLKSan/QLKSan.API/appsettings.Development.json`. `appsettings.json` để trống hai giá trị này; môi trường thật đặt bằng biến môi trường `ConnectionStrings__DefaultConnection`, `Jwt__Key` hoặc `dotnet user-secrets`. Thiếu, hoặc môi trường thật vẫn dùng khoá dev, thì API dừng ngay khi khởi động kèm thông báo.
3. **Tự nâng cấp CSDL.** Mỗi lần API khởi động, `Data/DatabaseSchemaFixer.cs` tự bổ sung cột/bảng còn thiếu. Các bước có thể chạy lặp lại an toàn. Vì vậy CSDL tạo từ script cũ vẫn dùng được. Nên sao lưu database trước khi chạy phiên bản mới.

## Chạy hệ thống

Giao diện chính là **React** (`http://localhost:5173`): trang khách và cổng nhân viên `/nhan-vien`. React chỉ là giao diện, dữ liệu lấy qua **API** (`https://localhost:7001`) và **Gateway** (`https://localhost:7000`), nên hai service này phải đang chạy. Cổng MVC (`https://localhost:7234`) giữ làm dự phòng.

**Cách 1 — bấm đúp `QLKSan/run.bat` (dễ nhất)**

1. Bấm đúp `QLKSan/run.bat`. File này build solution rồi mở mỗi service một cửa sổ: API, Gateway, ChatBot, Web MVC (dự phòng) và giao diện React. Lần đầu nó tự chạy `npm install` cho React (mất vài phút).
2. Khoảng 10 giây sau trình duyệt tự mở `http://localhost:5173`.
3. Tắt hệ thống: đóng các cửa sổ vừa mở.

Cần Node.js 22. Máy chưa có Node.js thì `run.bat` mở cổng MVC thay cho React.

**Cách 2 — Visual Studio + Terminal**

1. Mở `QLKSan/QLKSan.slnx`, đặt Multiple startup projects = `QLKSan.API` + `QLKSan.Gateway` + `QLKSan.ChatBot` (thêm `QLKSan.Web` nếu cần cổng MVC dự phòng), profile `https`, rồi bấm **Start**.
2. Visual Studio không chạy được phần React (đây là dự án Node, không phải .NET). Mở **View → Terminal** và chạy:

   ```bash
   cd QLKSan\QLKSan.Client
   npm install      # chỉ lần đầu
   npm run dev      # http://localhost:5173
   ```

3. Thấy dòng `Local: http://localhost:5173/` thì mở địa chỉ đó trên trình duyệt. Tắt React bằng `Ctrl + C` trong Terminal.

**Vào hệ thống:**

| Trang | Địa chỉ |
|---|---|
| Trang khách | `http://localhost:5173` |
| Cổng nhân viên | `http://localhost:5173/nhan-vien` (đăng nhập ở trang đăng nhập chung bằng tài khoản mẫu bên dưới) |
| Cổng MVC dự phòng | `https://localhost:7234` |

Đường dẫn cũ của cổng MVC gõ vào React (vd `/Reception`, `/Checkout/Process?maPhong=P201`, `/Invoice/Print/HD03`) tự chuyển sang trang mới tương ứng. Trang React báo "không kết nối được máy chủ" nghĩa là API hoặc Gateway chưa chạy.

**Trợ lý ảo dùng OpenAI (tuỳ chọn):** mặc định trợ lý trả lời theo quy tắc (`RuleBased`). Để dùng OpenAI, đặt khoá bằng user-secrets của `QLKSan.ChatBot` (không ghi vào `appsettings.json`):

```bash
cd QLKSan/QLKSan.ChatBot
dotnet user-secrets init
dotnet user-secrets set "ChatBot:OpenAI:ApiKey" "<khoá OpenAI>"
dotnet user-secrets set "ChatBot:Provider" "OpenAI"
```

Model đặt ở `ChatBot:OpenAI:Model`. OpenAI lỗi, hết hạn mức hoặc mất mạng thì câu trả lời tự chuyển sang RuleBased (`Provider` = `RuleBased (dự phòng)`).

**Email thông báo (tuỳ chọn khi phát triển):** cần RabbitMQ, ví dụ `docker compose up -d rabbitmq` trong `QLKSan/` (hoặc cài RabbitMQ trên máy). Sau đó bật `Messaging:Enabled` = `true` cho API (nếu dùng RabbitMQ của compose thì sửa thêm `Messaging:RabbitMqUri`) và chạy `QLKSan.Notification`. Email được ghi vào `QLKSan.Notification/mail/`. Khi `Messaging:Enabled` = `false` (mặc định), API không ghi sự kiện và không cần RabbitMQ.

**Dòng lệnh:**

```bash
cd QLKSan
dotnet run --project QLKSan.API --launch-profile https
dotnet run --project QLKSan.Gateway --launch-profile https
dotnet run --project QLKSan.ChatBot --launch-profile https
dotnet run --project QLKSan.Web --launch-profile https
```

**Giao diện React bằng dòng lệnh (QLKSan.Client):** cần API + Gateway (+ ChatBot) đang chạy, rồi:

```bash
cd QLKSan/QLKSan.Client
npm install
npm run dev               # http://localhost:5173, /api chuyển tới Gateway https://localhost:7000
```

Đổi Gateway bằng biến `VITE_GATEWAY`. Cổng nhân viên: `http://localhost:5173/nhan-vien`, đăng nhập bằng tài khoản nhân viên ở trang đăng nhập chung. Liên kết đặt phòng của trợ lý ảo trỏ tới giao diện React (`ChatBot:WebBaseUrl`, mặc định `http://localhost:5173/`). Sinh lại kiểu dữ liệu khi API thay đổi: `npm run gen:api` (đọc `https://localhost:7001/openapi/v1.json`, đổi bằng `API_OPENAPI_URL`). Build: `npm run build` → `dist/`.

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
dotnet test QLKSan.Tests          # không cần CSDL, RabbitMQ hay khoá OpenAI

cd QLKSan.Client
npm test                          # Vitest cho trang React
```

Phạm vi từng nhóm kiểm thử xem tại [docs/KIEN-TRUC.md](docs/KIEN-TRUC.md#kiểm-thử).

## Chạy bằng Docker

Cần Docker Desktop. Trong thư mục `QLKSan/`:

```bash
cp .env.example .env      # đổi SA_PASSWORD, JWT_KEY, RABBITMQ_PASSWORD (tuỳ chọn: CHATBOT_PROVIDER, OPENAI_API_KEY)
docker compose up -d --build
```

- Giao diện chính (React: khách + cổng nhân viên `/nhan-vien`): http://localhost:7082 · Web MVC (dự phòng): http://localhost:7081 · Gateway (API): http://localhost:7080/api/health · RabbitMQ: http://localhost:15672 (tài khoản `qlksan`) · SQL Server: `localhost,14330` (tài khoản `sa`).
- API lõi, ChatBot và service thông báo không mở cổng ra ngoài: mọi yêu cầu đi qua Gateway. API lõi bật outbox (`Messaging:Enabled`) nên email đặt phòng / huỷ / nhận phòng được gửi tới khách. Không đặt `SMTP_HOST` thì email nằm trong volume `mail`.
- Lần đầu, dịch vụ `db-init` tạo database từ `DB/Quanlykhachsanc#.sql`. Các lần sau giữ nguyên dữ liệu; API tự nâng cấp cấu trúc khi khởi động.
- Dữ liệu SQL Server, RabbitMQ, ảnh tải lên, email, hội thoại trợ lý ảo và khoá cookie đăng nhập nằm trong volume (`sqldata`, `rabbitmq`, `uploads`, `mail`, `chatdata`, `webkeys`), nên vẫn còn khi khởi động lại. `docker compose down -v` xoá toàn bộ.
- Container chạy theo múi giờ `Asia/Ho_Chi_Minh`, vì nghiệp vụ nhận/trả phòng và chốt ngày dùng giờ địa phương.
- Web gọi Gateway qua địa chỉ nội bộ `http://gateway:8080`. Ảnh tải lên được trình duyệt tải qua `ApiSettings:PublicUrl`.

## Triển khai (publish)

```bash
cd QLKSan
dotnet publish QLKSan.API -c Release -o publish/api
dotnet publish QLKSan.Gateway -c Release -o publish/gateway
dotnet publish QLKSan.Notification -c Release -o publish/notification
dotnet publish QLKSan.ChatBot -c Release -o publish/chatbot
dotnet publish QLKSan.Web -c Release -o publish/web
```

Khi triển khai:
- Sửa `appsettings.json` (hoặc biến môi trường) cho từng ứng dụng:
  - API: `ConnectionStrings__DefaultConnection`, `Jwt__Key`; `Messaging__Enabled`, `Messaging__RabbitMqUri` để gửi sự kiện; `ForwardedHeaders__MangTinCay__0` = mạng của Gateway (CIDR);
  - Gateway: `Jwt__Key` (giống API), `ReverseProxy__Clusters__core__Destinations__api__Address` = địa chỉ API lõi, `RateLimit__MoiPhut`, `RateLimit__DangNhapMoiPhut`, `Gateway__MangTinCay__0` = mạng của Web;
  - Notification: `RabbitMq__Uri`, `Email__Smtp__*` (hoặc `Email__ThuMucLuu`);
  - ChatBot: `ChatBot__ApiBaseUrl` = Gateway, `ChatBot__WebBaseUrl` = trang Web, `ChatBot__Provider`, `ChatBot__OpenAI__ApiKey`, `ConnectionStrings__ChatDb`; Gateway: `ReverseProxy__Clusters__chatbot__Destinations__chatbot__Address`, `RateLimit__ChatMoiPhut`;
  - Web: `ApiSettings__BaseUrl` trỏ tới Gateway; `ApiSettings__PublicUrl` nếu trình duyệt truy cập API qua địa chỉ khác; `DataProtection__KeysPath` để lưu khoá cookie đăng nhập.
- Chạy sau IIS / reverse proxy với HTTPS.
- Cấp quyền ghi thư mục `wwwroot/uploads` cho tiến trình API.
