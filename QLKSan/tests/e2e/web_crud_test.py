"""Kiểm tra các thao tác ghi qua giao diện Web (Web -> API -> DB), chỉ dùng dữ liệu tự dọn được."""
import os
from csrfhelp import with_token
import html, http.cookiejar, re, ssl, sys, urllib.parse, urllib.request, urllib.error

WEB = os.environ.get('QLKSAN_WEB', 'https://localhost:7234')
API = os.environ.get('QLKSAN_API', 'https://localhost:7001')
CTX = ssl._create_unverified_context()
results = []

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def session(user):
    jar = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX), urllib.request.HTTPCookieProcessor(jar), NoRedirect)
    req(op, 'POST', '/Auth/Login', {'username': user, 'password': '123456'})
    return op

def req(op, method, path, form=None):
    form = with_token(op, method, form)
    data = urllib.parse.urlencode(form, doseq=True).encode() if form is not None else None
    r = urllib.request.Request(WEB + path, data=data, method=method)
    try:
        with op.open(r) as resp: return resp.status, resp.headers.get('Location'), html.unescape(resp.read().decode('utf-8', 'replace'))
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), html.unescape(e.read().decode('utf-8', 'replace'))

def post_follow(op, path, form):
    """POST rồi theo redirect, trả về HTML trang đích (chứa thông báo TempData)."""
    s, loc, body = req(op, 'POST', path, form)
    if s == 302 and loc:
        s, _, body = req(op, 'GET', loc if loc.startswith('/') else urllib.parse.urlparse(loc).path)
    return s, body

def check(name, cond, info=''):
    results.append(bool(cond))
    print(f"{'OK  ' if cond else 'FAIL'} {name}{' | ' + info if info else ''}")

def alert(body):
    m = re.search(r'alert-(success|danger)[^>]*>\s*(?:<[^>]+>\s*)*([^<]+)', body)
    return (m.group(1), m.group(2).strip()) if m else (None, None)

ad = session('admin1')
lt = session('letan1')
kh = session('khach1')

print('--- Admin: loại phòng + phòng (tự dọn) ---')
s, body = post_follow(ad, '/Admin/AddRoomType', {'tenLoai': 'WEB TEST', 'soNguoiToiDa': 2, 'giaThueNgay': 100000, 'giaThueGio': 10000, 'moTa': 'x'})
check('Thêm loại phòng', alert(body) == ('success', 'Thêm loại phòng thành công!') and 'WEB TEST' in body, str(alert(body)))
ma_loai = re.search(r"(LP\d+)[^<]{0,400}WEB TEST|WEB TEST[\s\S]{0,600}?(LP\d+)", body)
ma_loai = (ma_loai.group(1) or ma_loai.group(2)) if ma_loai else None
check('Tìm được mã loại phòng vừa thêm', ma_loai is not None, str(ma_loai))
s, body = post_follow(ad, '/Admin/AddRoomType', {'tenLoai': 'X', 'soNguoiToiDa': 0, 'giaThueNgay': 1, 'giaThueGio': 1})
check('Thêm loại phòng sai (0 người) → lỗi validation từ API', alert(body) == ('danger', 'Số người tối đa phải từ 1 đến 10'), str(alert(body)))
s, body = post_follow(ad, '/Admin/AddRoom', {'soPhong': '9911', 'maLoai': ma_loai})
check('Thêm phòng 9911', alert(body)[0] == 'success' and '9911' in body, str(alert(body)))
s, body = post_follow(ad, '/Admin/AddRoom', {'soPhong': '9911', 'maLoai': ma_loai})
check('Thêm phòng trùng → lỗi 409 từ API', alert(body) == ('danger', "Phòng số '9911' đã tồn tại."), str(alert(body)))
s, body = post_follow(ad, '/Admin/DeleteRoomType', {'maLoai': ma_loai})
check('Xoá loại đang có phòng → bị chặn', alert(body)[0] == 'danger' and 'có phòng đang dùng' in alert(body)[1], str(alert(body)))
s, body = post_follow(ad, '/Admin/EditRoom', {'maPhong': 'P9911', 'soPhong': '9911', 'maLoai': ma_loai})
check('Sửa phòng', alert(body) == ('success', 'Cập nhật phòng thành công!'), str(alert(body)))

print('--- Lễ tân: đổi trạng thái + buồng phòng trên phòng test ---')
s, body = post_follow(lt, '/Room/UpdateStatus', {'maPhong': 'P9911', 'status': 'Cleaning'})
check('Đổi trạng thái → Đang dọn', alert(body) == ('success', 'Cập nhật trạng thái phòng thành công!'), str(alert(body)))
s, body = req(lt, 'GET', '/Housekeeping/Tasks')[0], req(lt, 'GET', '/Housekeeping/Tasks')[2]
check('Phòng 9911 xuất hiện trong danh sách cần dọn', '9911' in body)
s, body = post_follow(lt, '/Housekeeping/MarkClean', {'maPhong': 'P9911'})
check('Đánh dấu dọn xong', alert(body)[0] == 'success', str(alert(body)))
s, body = post_follow(lt, '/Housekeeping/MarkClean', {'maPhong': 'P9911'})
check('Đánh dấu dọn lần 2 → lỗi nghiệp vụ', alert(body)[0] == 'danger' and 'không ở trạng thái đang dọn' in alert(body)[1], str(alert(body)))

print('--- Admin: dọn dữ liệu test ---')
s, body = post_follow(ad, '/Admin/DeleteRoom', {'maPhong': 'P9911'})
check('Xoá phòng', alert(body) == ('success', 'Xóa phòng thành công!'), str(alert(body)))
s, body = post_follow(ad, '/Admin/EditRoomType', {'maLoai': ma_loai, 'tenLoai': 'WEB TEST 2', 'soNguoiToiDa': 3, 'giaThueNgay': 1, 'giaThueGio': 1})
check('Sửa loại phòng', alert(body) == ('success', 'Cập nhật loại phòng thành công!'), str(alert(body)))
s, body = post_follow(ad, '/Admin/DeleteRoomType', {'maLoai': ma_loai})
check('Xoá loại phòng', alert(body) == ('success', 'Xóa loại phòng thành công!') and 'WEB TEST' not in body, str(alert(body)))

print('--- Admin: dịch vụ (tự dọn) ---')
s, body = post_follow(ad, '/Admin/AddService', {'tenDichVu': 'WEB TEST DV', 'giaDichVu': 1000, 'donViTinh': 'Lần'})
check('Thêm dịch vụ', alert(body)[0] == 'success' and 'WEB TEST DV' in body, str(alert(body)))
ma_dv = re.findall(r'(DV\d{3})', body)
ma_dv = sorted(ma_dv)[-1] if ma_dv else None
s, body = post_follow(ad, '/Admin/SetServiceStatus', {'maDichVu': ma_dv, 'trangThai': 'Unavailable'})
check(f'Chuyển dịch vụ {ma_dv} sang "Đã hết"', alert(body)[0] == 'success' and 'Đã hết' in alert(body)[1], str(alert(body)))
s, body = post_follow(ad, '/Admin/EditService', {'maDichVu': ma_dv, 'tenDichVu': 'WEB TEST DV2', 'giaDichVu': 2000, 'donViTinh': 'Lần'})
check('Sửa dịch vụ', alert(body)[0] == 'success' and 'WEB TEST DV2' in body, str(alert(body)))
s, body = post_follow(ad, '/Admin/DeleteService', {'maDichVu': ma_dv})
check('Xoá dịch vụ', alert(body)[0] == 'success' and 'WEB TEST DV2' not in body, str(alert(body)))

print('--- Admin: nhân viên (tự dọn) ---')
s, body = post_follow(ad, '/Admin/AddStaff', {'tenNhanVien': 'X', 'chucVu': 'Lễ tân', 'sdt': '123', 'email': 'a@b.vn', 'tenDangNhap': 'x', 'matKhau': '1'})
check('Thêm NV sai SĐT + mật khẩu ngắn → lỗi validation', alert(body)[0] == 'danger' and 'Số điện thoại nhân viên không hợp lệ' in alert(body)[1], str(alert(body)))
s, body = post_follow(ad, '/Admin/AddStaff', {'tenNhanVien': 'Web Test NV', 'chucVu': 'Lễ tân', 'sdt': '0912345670', 'email': 'web@test.vn', 'tenDangNhap': 'webtest_zz', 'matKhau': '123456'})
check('Thêm nhân viên', alert(body)[0] == 'success' and 'webtest_zz' in body, str(alert(body)))
ma_nv = re.search(r'(NV\d+)[\s\S]{0,1500}?webtest_zz', body)
ma_nv = re.findall(r'NV\d+', ma_nv.group(0))[-1] if ma_nv else None
s, body = post_follow(ad, '/Admin/DeleteStaff', {'maNhanVien': ma_nv})
check(f'Xoá nhân viên {ma_nv}', alert(body)[0] == 'success' and 'webtest_zz' not in body, str(alert(body)))

print('--- Các trường hợp lỗi (không ghi dữ liệu) ---')
s, body = post_follow(lt, '/Reception/ApproveBooking', {'id': 'XXX'})
check('Duyệt phiếu không tồn tại', alert(body)[0] == 'danger' and "Không tìm thấy phiếu đặt phòng 'XXX'" in alert(body)[1], str(alert(body)))
s, body = post_follow(lt, '/Checkout/Pay', {'maHoaDon': 'HD05', 'phuongThuc': 'Tiền mặt'})
check('Thanh toán hoá đơn đã thanh toán', alert(body)[0] == 'danger' and 'đã được thanh toán' in alert(body)[1], str(alert(body)))
import datetime as _dt, json as _json
# Lấy một phòng đang có khách từ API (không gán cứng mã phòng)
_rq = urllib.request.Request(API + '/api/auth/login', data=_json.dumps({'TenDangNhap': 'letan1', 'MatKhau': '123456'}).encode(), method='POST')
_rq.add_header('Content-Type', 'application/json')
_tok = _json.loads(urllib.request.urlopen(_rq, context=CTX).read())['AccessToken']
_rq = urllib.request.Request(API + '/api/rooms?trangThai=Occupied', headers={'Authorization': 'Bearer ' + _tok})
OCC = _json.loads(urllib.request.urlopen(_rq, context=CTX).read())[0]['MaPhong']
_today, _tomorrow = _dt.date.today().isoformat(), (_dt.date.today() + _dt.timedelta(days=1)).isoformat()
s, _, body = req(lt, 'POST', '/Reception/WalkIn', {'Ho': 'A', 'Ten': 'B', 'SDT': '0912345678', 'CCCD_Passport': '123456789',
                                                   'NgayCheckIn': _today, 'NgayCheckOut': _tomorrow, 'MaPhong': OCC, 'SoNguoi': 1})
check('Walk-in vào phòng đang có khách → lỗi hiển thị trên form', s == 200 and 'Phòng không khả dụng hoặc đã được đặt' in body)
s, body = post_follow(kh, '/Customer/SubmitFeedback', {'maDatPhong': 'DP02', 'rating': 5, 'comment': 'x', 'isAnonymous': 'false'})
check('Khách đánh giá kỳ nghỉ không phải của mình', alert(body)[0] == 'danger' and 'kỳ nghỉ của mình đã trả phòng' in alert(body)[1], str(alert(body)))
s, _, body = req(kh, 'POST', '/Customer/BookRoom', {'MaPhong': OCC, 'SoNguoi': 1, 'CheckIn': _today, 'CheckOut': _tomorrow})
check('Khách đặt phòng đang có người ở (trùng lịch hôm nay) → lỗi hiển thị', s == 200 and 'đang có khách lưu trú' in body)
s, _, body = req(kh, 'GET', '/Customer/CheckPromotion?code=KM01')
import json as _j; _d = _j.loads(body); check('Kiểm tra mã KM hết hạn (JSON)', _d['success'] is False and 'hết hạn' in _d['message'], _d['message'])

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
