"""Kiểm tra Mức 3: giá theo mùa / cuối tuần / lễ, lịch phòng, in hoá đơn (khách tự xem hoá đơn của mình), sổ khách lưu trú + Excel.
CHỈ chạy trên database thử dựng từ DB/Quanlykhachsanc#.sql (script ghi dữ liệu: 1 lượt khách vãng lai; quy tắc giá thử được xoá ở cuối)."""
import os
from csrfhelp import with_token
import html, http.cookiejar, io, json, random, re, ssl, sys, urllib.parse, urllib.request, urllib.error, zipfile, datetime

WEB, API = os.environ.get('QLKSAN_WEB', 'https://localhost:7234'), os.environ.get('QLKSAN_API', 'https://localhost:7001')
CTX = ssl._create_unverified_context()
results = []
today = datetime.date.today()
d = lambda n: (today + datetime.timedelta(days=n)).isoformat()
# 0 = Chủ nhật ... 6 = Thứ bảy (giống cột CacThu)
thu = lambda n: ((today + datetime.timedelta(days=n)).weekday() + 1) % 7

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k): return None

def web_session():
    return urllib.request.build_opener(urllib.request.HTTPSHandler(context=CTX), urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect)

def web(op, method, path, form=None, raw=False):
    form = with_token(op, method, form)
    data = urllib.parse.urlencode(form, doseq=True).encode() if form is not None else None
    try:
        with op.open(urllib.request.Request(WEB + path, data=data, method=method)) as r:
            body = r.read()
            return r.status, r.headers.get('Location'), (body if raw else html.unescape(body.decode('utf-8', 'replace'))), r.headers
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Location'), html.unescape(e.read().decode('utf-8', 'replace')), e.headers

def web_post(op, path, form):
    s, loc, body, _ = web(op, 'POST', path, form)
    if s == 302 and loc:
        u = urllib.parse.urlparse(loc); _, _, body, _ = web(op, 'GET', u.path + ('?' + u.query if u.query else ''))
    return s, loc, body

def web_login(user, pwd='123456'):
    op = web_session()
    web(op, 'POST', '/Auth/Login', {'username': user, 'password': pwd})
    return op

def api(method, path, token=None, body=None, raw=False):
    req = urllib.request.Request(API + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header('Content-Type', 'application/json')
    if token: req.add_header('Authorization', 'Bearer ' + token)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            content = r.read()
            if raw: return r.status, content
            text = content.decode(); return r.status, json.loads(text) if text else None
    except urllib.error.HTTPError as e:
        text = e.read().decode()
        try: return e.code, json.loads(text) if text else None
        except Exception: return e.code, text

def tok(user): return api('POST', '/api/auth/login', body={'TenDangNhap': user, 'MatKhau': '123456'})[1]['AccessToken']

def alert(body):
    m = re.search(r'alert-(success|danger)[^>]*>\s*(?:<[^>]+>\s*)*([^<]+)', body)
    return (m.group(1), m.group(2).strip()) if m else (None, None)

def check(name, cond, info=''):
    results.append(bool(cond)); print(f"{'OK  ' if cond else 'FAIL'} {name}{' | ' + str(info) if info != '' else ''}")

def xlsx_text(content):
    z = zipfile.ZipFile(io.BytesIO(content))
    return ''.join(z.read(n).decode('utf-8') for n in z.namelist() if n.startswith('xl/') and n.endswith('.xml'))

A, L, BP, KT, K = tok('admin1'), tok('letan1'), tok('buongphong1'), tok('ketoan1'), tok('khach1')
adw, ltw, ktw, bpw, kw = (web_login(u) for u in ('admin1', 'letan1', 'ketoan1', 'buongphong1', 'khach1'))
quote = lambda ma, a, b: api('GET', f'/api/rooms/{ma}/quote?checkIn={d(a)}&checkOut={d(b)}', A)[1]
test_rules = []

try:
    print('===== 3.1 Quy tắc giá: dữ liệu mẫu & phân quyền =====')
    s, rules = api('GET', '/api/pricing-rules', A)
    check('Admin xem danh sách quy tắc giá → 3 quy tắc mẫu', s == 200 and {r['MaQuyTac'] for r in rules} >= {'QT001', 'QT002', 'QT003'}, [r['TenQuyTac'] for r in rules])
    qt1 = next(r for r in rules if r['MaQuyTac'] == 'QT001')
    check('  QT001 cuối tuần: đêm thứ Sáu + thứ Bảy, +20%', qt1['CacThu'] == [5, 6] and qt1['PhanTramDieuChinh'] == 20)
    check('Lễ tân xem quy tắc giá → 403', api('GET', '/api/pricing-rules', L)[0] == 403)
    check('Kế toán thêm quy tắc giá → 403', api('POST', '/api/pricing-rules', KT, {'TenQuyTac': 'x', 'PhanTramDieuChinh': 10, 'UuTien': 1})[0] == 403)
    check('Chưa đăng nhập → 401', api('GET', '/api/pricing-rules')[0] == 401)
    s, r = api('POST', '/api/pricing-rules', A, {'TenQuyTac': 'Sai', 'PhanTramDieuChinh': 500, 'UuTien': 1})
    check('Điều chỉnh +500% → 400', s == 400, r and r.get('errors'))
    s, r = api('POST', '/api/pricing-rules', A, {'TenQuyTac': 'Sai', 'TuNgay': d(5), 'DenNgay': d(2), 'PhanTramDieuChinh': 10, 'UuTien': 1})
    check('Đến ngày trước Từ ngày → 400', s == 400, r and r.get('detail'))
    s, r = api('POST', '/api/pricing-rules', A, {'TenQuyTac': 'Sai', 'MaLoai': 'LP99', 'PhanTramDieuChinh': 10, 'UuTien': 1})
    check('Loại phòng không tồn tại → 400', s == 400, r and r.get('detail'))

    print('\n===== 3.1 Báo giá từng đêm =====')
    free = api('GET', f'/api/rooms/available?checkIn={d(0)}&checkOut={d(7)}', A)[1]
    R = next(x for x in free if x['LoaiPhong']['MaLoai'] != 'LP04')
    G = R['LoaiPhong']['GiaThueNgay']
    print(f"Phòng test: {R['MaPhong']} ({R['LoaiPhong']['MaLoai']}, {G:,.0f}đ) | hôm nay {today:%d/%m/%Y} (thứ {thu(0)})")
    fri = next(n for n in range(1, 8) if thu(n) == 5)
    q = quote(R['MaPhong'], fri, fri + 1)
    check(f"Đêm thứ Sáu {d(fri)} → +20% theo QT001", q['CacDem'][0]['Gia'] == round(G * 1.2) and q['CacDem'][0]['QuyTac'] == 'Phụ thu cuối tuần', q['CacDem'][0])
    q = quote(R['MaPhong'], 26, 27)
    check('Đêm trong dịp lễ (+26) → +50% theo QT002 (ưu tiên hơn cuối tuần)', q['CacDem'][0]['Gia'] == round(G * 1.5) and q['CacDem'][0]['QuyTac'] == 'Lễ hội cuối năm', q['CacDem'][0])
    q = quote(R['MaPhong'], 0, 7)
    check('Báo giá 7 đêm: tổng = tổng giá từng đêm, có đêm cuối tuần', q['SoDem'] == 7 and q['TongTien'] == sum(n['Gia'] for n in q['CacDem'])
          and sum(1 for n in q['CacDem'] if n['QuyTac']) >= 2, f"{q['TongTien']:,.0f}, TB {q['GiaTrungBinhDem']:,.0f}/đêm")
    avail = next(x for x in api('GET', f'/api/rooms/available?checkIn={d(0)}&checkOut={d(7)}', K)[1] if x['MaPhong'] == R['MaPhong'])
    check('Tra phòng trống 7 đêm (khách) → TongTienDuKien khớp báo giá', avail['TongTienDuKien'] == q['TongTien'] and avail['GiaTrungBinhDem'] == round(q['TongTien'] / 7), avail['TongTienDuKien'])
    check('Báo giá ngày trả ≤ ngày nhận → 400', api('GET', f"/api/rooms/{R['MaPhong']}/quote?checkIn={d(3)}&checkOut={d(3)}", A)[0] == 400)
    check('Báo giá phòng không tồn tại → 404', api('GET', f"/api/rooms/P999/quote?checkIn={d(1)}&checkOut={d(2)}", A)[0] == 404)

    print('\n===== 3.1 Thêm / bật-tắt quy tắc & tính tiền khi trả phòng =====')
    s, rule = api('POST', '/api/pricing-rules', A, {'TenQuyTac': 'Thử nghiệm hôm nay', 'MaLoai': R['LoaiPhong']['MaLoai'], 'TuNgay': d(0), 'DenNgay': d(0),
                                                    'CacThu': [thu(0)], 'PhanTramDieuChinh': 40, 'UuTien': 50})
    if s == 201: test_rules.append(rule['MaQuyTac'])
    check('Thêm quy tắc +40% cho đêm nay, loại phòng test → 201', s == 201 and rule['TenLoai'] == R['LoaiPhong']['TenLoai'] and rule['CacThu'] == [thu(0)], rule and rule.get('MaQuyTac'))
    check('  Báo giá đêm nay = giá × 1,4', quote(R['MaPhong'], 0, 1)['TongTien'] == round(G * 1.4))
    s, r = api('PATCH', f"/api/pricing-rules/{rule['MaQuyTac']}", A, {'KichHoat': False})
    check('PATCH { KichHoat: false } → đêm nay không còn +40%', s == 200 and not r['KichHoat'] and quote(R['MaPhong'], 0, 1)['TongTien'] != round(G * 1.4))
    s, r = api('PATCH', f"/api/pricing-rules/{rule['MaQuyTac']}", A, {'KichHoat': False})
    check('  Gọi lại cùng body → vẫn tắt (idempotent, không đảo trạng thái)', s == 200 and not r['KichHoat'])
    api('PATCH', f"/api/pricing-rules/{rule['MaQuyTac']}", A, {'KichHoat': True})
    check('PATCH { KichHoat: true } → +40%', quote(R['MaPhong'], 0, 1)['TongTien'] == round(G * 1.4))
    check('PATCH thiếu KichHoat → 400', api('PATCH', f"/api/pricing-rules/{rule['MaQuyTac']}", A, {})[0] == 400)
    check('PATCH quy tắc không tồn tại → 404', api('PATCH', '/api/pricing-rules/QT999', A, {'KichHoat': False})[0] == 404)
    check('Endpoint /toggle cũ không còn', api('PATCH', f"/api/pricing-rules/{rule['MaQuyTac']}/toggle", A)[0] in (404, 405))

    cccd = str(random.randint(10**11, 10**12 - 1))
    web_post(ltw, '/Reception/WalkIn', {'Ho': 'Thử', 'Ten': 'Giá Mùa', 'SDT': '0912000777', 'CCCD_Passport': cccd,
                                        'NgayCheckIn': d(0), 'NgayCheckOut': d(1), 'MaPhong': R['MaPhong'], 'SoNguoi': 2})
    bk = next((x for x in api('GET', '/api/bookings/active', A)[1] if x['ChiTietDatPhongs'][0]['MaPhong'] == R['MaPhong']), None)
    check('Walk-in đêm nay → tiền cọc = 50% giá đã điều chỉnh', bk and bk['TienCoc'] == round(G * 1.4 * 0.5), bk and f"{bk['MaDatPhong']} cọc {bk['TienCoc']:,.0f}")
    web_post(ltw, '/Checkout/GenerateInvoice', {'maDatPhong': bk['MaDatPhong'], 'maKhuyenMai': '', 'maPhong': R['MaPhong']})
    inv = api('GET', f"/api/bookings/{bk['MaDatPhong']}", A)[1]['HoaDon']
    check('Hoá đơn: tiền phòng theo giá đêm đã điều chỉnh, SoDem = 1', inv['TongTienPhong'] == round(G * 1.4) and inv['SoDem'] == 1, f"{inv['TongTienPhong']:,.0f}")
    s, _, body, _ = web(ltw, 'GET', f"/Checkout/Process?maPhong={R['MaPhong']}")
    check('Trang trả phòng có nút in phiếu tạm tính', f"/Invoice/Print/{inv['MaHoaDon']}" in body)
    s, _, body, _ = web(ltw, 'GET', f"/Invoice/Print/{inv['MaHoaDon']}")
    check('Phiếu tạm tính (chưa thanh toán) in được', s == 200 and 'PHIẾU TẠM TÍNH' in body and 'Đã áp dụng giá cuối tuần / dịp lễ' in body)
    s, loc, body = web_post(ltw, '/Checkout/Pay', {'maHoaDon': inv['MaHoaDon'], 'phuongThuc': 'Tiền mặt'})
    check('Thanh toán xong → trang lễ tân có nút "In hoá đơn"', f"/Invoice/Print/{inv['MaHoaDon']}" in body, alert(body)[1])
    s, _, body, _ = web(ltw, 'GET', f"/Invoice/Print/{inv['MaHoaDon']}")
    check('Bản in hoá đơn đã thanh toán: tiêu đề, khách, tổng tiền', s == 200 and 'HOÁ ĐƠN THANH TOÁN' in body and 'Thử Giá Mùa' in body
          and any(f"{round(inv['TongTienCanThanhToan']):,.0f}".replace(',', sep) in body for sep in (',', '.')))
    web_post(ltw, '/Housekeeping/MarkClean', {'maPhong': R['MaPhong']})

    print('\n===== 3.1 Trang Bảng giá (Web) =====')
    s, _, body, _ = web(adw, 'GET', '/Admin/Pricing')
    check('Admin mở trang Bảng giá', s == 200 and 'Phụ thu cuối tuần' in body and 'Thử nghiệm hôm nay' in body)
    check('Lễ tân mở Bảng giá → chặn', web(ltw, 'GET', '/Admin/Pricing')[0] in (302, 403))
    s, loc, body = web_post(adw, '/Admin/AddPricingRule', {'TenQuyTac': 'Web thử', 'MaLoai': '', 'TuNgay': '', 'DenNgay': '',
                                                         'CacThu': ['0', '6'], 'PhanTramDieuChinh': '-10', 'UuTien': '3'})
    web_rule = next((r for r in api('GET', '/api/pricing-rules', A)[1] if r['TenQuyTac'] == 'Web thử'), None)
    if web_rule: test_rules.append(web_rule['MaQuyTac'])
    check('Thêm quy tắc qua form (chọn CN + T7, -10%, mọi loại phòng)', web_rule and web_rule['CacThu'] == [0, 6] and web_rule['PhanTramDieuChinh'] == -10 and web_rule['MaLoai'] is None, alert(body))
    s, loc, body = web_post(adw, '/Admin/AddPricingRule', {'TenQuyTac': 'Web sai', 'PhanTramDieuChinh': '500', 'UuTien': '1'})
    check('Form: điều chỉnh 500% → báo lỗi', alert(body)[0] == 'danger' and '-90%' in alert(body)[1], alert(body)[1])
    s, loc, body = web_post(adw, '/Admin/SetPricingRuleStatus', {'id': web_rule['MaQuyTac'], 'kichHoat': 'false'})
    check('Form: tắt quy tắc', alert(body)[0] == 'success' and not next(r for r in api('GET', '/api/pricing-rules', A)[1] if r['MaQuyTac'] == web_rule['MaQuyTac'])['KichHoat'])
    s, loc, body = web_post(adw, '/Admin/DeletePricingRule', {'id': web_rule['MaQuyTac']})
    check('Form: xoá quy tắc', alert(body)[0] == 'success' and all(r['MaQuyTac'] != web_rule['MaQuyTac'] for r in api('GET', '/api/pricing-rules', A)[1]))
    test_rules.remove(web_rule['MaQuyTac'])

    print('\n===== 3.1 Khách đặt phòng thấy giá theo ngày =====')
    s, _, body, _ = web(kw, 'GET', f'/Customer/BookRoom?checkIn={d(0)}&checkOut={d(7)}')
    check('Trang đặt phòng 7 đêm: hiển thị giá trung bình/đêm', s == 200 and '(trung bình)' in body)
    q7 = quote(R['MaPhong'], 0, 7)
    s, _, body, _ = web(kw, 'GET', f"/Customer/BookRoom?checkIn={d(0)}&checkOut={d(7)}&soNguoi=1&maLoai={R['LoaiPhong']['MaLoai']}")
    check('Bước xác nhận: tổng tiền và giá từng đêm khớp báo giá API', s == 200 and f"{q7['TongTien']:,.0f} ₫" in body
          and body.count('bk-line night') == 7 and all(x['QuyTac'] in body for x in q7['CacDem'] if x['QuyTac']), q7['TongTien'])

    print('\n===== 3.2 Lịch phòng =====')
    s, tl = api('GET', '/api/bookings/timeline', L)
    p2 = next((x for x in tl if x['MaDatPhong'] == 'PDP0000002'), None)
    check('Lịch phòng 14 ngày: phiếu PDP0000002 ở P101 từ +1 đến +3', s == 200 and p2 and p2['MaPhong'] == 'P101' and p2['TuNgay'][:10] == d(1) and p2['DenNgay'][:10] == d(3), p2)
    check('  Có phiếu đang ở và phiếu vừa trả hôm nay', {'CheckedIn', 'CheckedOut'} <= {x['TrangThai'] for x in tl})
    check('  Không có phiếu đã huỷ', all(x['MaDatPhong'] != 'PDP0000005' for x in tl))
    check('Xem quá 62 ngày → 400', api('GET', f'/api/bookings/timeline?tuNgay={d(0)}&denNgay={d(70)}', L)[0] == 400)
    check('Buồng phòng xem lịch phòng → 403', api('GET', '/api/bookings/timeline', BP)[0] == 403)
    s, _, body, _ = web(ltw, 'GET', '/Room/Timeline')
    check('Web: lễ tân mở Lịch phòng, có thanh PDP0000002', s == 200 and 'PDP0000002' in body and 'Lịch phòng' in body)
    s, _, body, _ = web(ktw, 'GET', f'/Room/Timeline?tuNgay={d(-7)}&soNgay=31')
    check('Web: kế toán xem lịch phòng 31 ngày từ tuần trước', s == 200 and body.count('<th class') >= 31)
    check('Web: buồng phòng mở Lịch phòng → chặn', web(bpw, 'GET', '/Room/Timeline')[0] in (302, 403))

    print('\n===== 3.3 In hoá đơn & khách xem hoá đơn của mình =====')
    s, r = api('GET', '/api/invoices/HD01', K)
    check('Khách khach1 xem hoá đơn HD01 của mình (API) → 200', s == 200 and r['HoaDon']['MaHoaDon'] == 'HD01')
    check('Khách khach1 xem hoá đơn HD03 của khách khác (API) → 403', api('GET', '/api/invoices/HD03', K)[0] == 403)
    check('Buồng phòng xem hoá đơn → 403', api('GET', '/api/invoices/HD01', BP)[0] == 403)
    s, _, body, _ = web(kw, 'GET', '/Customer/MyStays')
    check('Lịch sử kỳ nghỉ: nút hoá đơn trỏ tới bản in thật, hết alert giả', '/Invoice/Print/HD01' in body and "alert('Đang" not in body and "alert('Bắt đầu" not in body)
    s, _, body, _ = web(kw, 'GET', '/Invoice/Print/HD01')
    check('Khách mở bản in HD01 → 200, có nút quay lại Lịch sử kỳ nghỉ', s == 200 and 'HD01' in body and '/Customer/MyStays' in body and 'In / Lưu PDF' in body)
    s, loc, _, _ = web(kw, 'GET', '/Invoice/Print/HD03')
    check('Khách mở bản in hoá đơn người khác → chặn', s in (302, 403) and (loc is None or 'AccessDenied' in loc), loc)
    check('Hoá đơn không tồn tại → 404', web(ltw, 'GET', '/Invoice/Print/HD999')[0] == 404)
    s, _, body, _ = web(ktw, 'GET', '/Accounting/Invoice/HD04')
    check('Kế toán: trang chi tiết hoá đơn có nút In / PDF', '/Invoice/Print/HD04' in body)
    s, _, body, _ = web(ktw, 'GET', '/Invoice/Print/HD04')
    check('  Bản in HD04: dịch vụ, khuyến mãi KM04, VAT', s == 200 and 'KM04' in body and 'VAT' in body and 'Tiền phòng' in body)
    check('Buồng phòng mở bản in → chặn', web(bpw, 'GET', '/Invoice/Print/HD01')[0] in (302, 403))

    print('\n===== 3.4 Sổ khách lưu trú =====')
    s, guests = api('GET', f'/api/reports/guests?tuNgay={d(0)}&denNgay={d(0)}', L)
    me = next((g for g in guests if g['CCCD_Passport'] == cccd), None)
    check('Sổ lưu trú hôm nay có khách vãng lai vừa ở', s == 200 and me and me['SoPhong'] == R['SoPhong'] and me['SoLuongKhach'] == 2, me)
    check('  Có khách đang ở (PDP0000001) và không có khách chưa đến (PDP0000002)', any(g['MaDatPhong'] == 'PDP0000001' for g in guests) and all(g['MaDatPhong'] != 'PDP0000002' for g in guests))
    check('Khách hàng xem sổ lưu trú → 403', api('GET', '/api/reports/guests', K)[0] == 403)
    s, content = api('GET', f'/api/reports/guests/export?tuNgay={d(-30)}&denNgay={d(0)}', KT, raw=True)
    txt = xlsx_text(content) if s == 200 else ''
    check('Kế toán xuất Excel sổ lưu trú 30 ngày → .xlsx có CCCD khách', s == 200 and cccd in txt, f"{len(content)} bytes")
    s, _, body, _ = web(ltw, 'GET', f'/Room/Guests?tuNgay={d(0)}&denNgay={d(0)}')
    check('Web: trang Sổ khách lưu trú', s == 200 and cccd in body and 'Xuất Excel' in body)
    s, _, content, headers = web(ltw, 'GET', f'/Room/ExportGuests?tuNgay={d(0)}&denNgay={d(0)}', raw=True)
    check('Web: tải Excel sổ lưu trú', s == 200 and 'spreadsheetml' in headers.get('Content-Type', '') and cccd in xlsx_text(content))
    check('Web: khách hàng mở sổ lưu trú → chặn', web(kw, 'GET', '/Room/Guests')[0] in (302, 403))

    print('\n===== Báo cáo doanh thu dùng số đêm đã lưu =====')
    s, rev = api('GET', f'/api/reports/revenue?tuNgay={d(-40)}&denNgay={d(0)}', KT)
    check('Báo cáo doanh thu 40 ngày vẫn chạy, có hoá đơn vừa thanh toán', s == 200 and rev['SoHoaDon'] >= 7, f"{rev and rev['SoHoaDon']} HĐ")
    s, _, body, _ = web(adw, 'GET', '/Admin/Index')
    check('Sidebar admin có Bảng giá, Lịch phòng, Sổ lưu trú', all(x in body for x in ('/Admin/Pricing', '/Room/Timeline', '/Room/Guests')))
finally:
    for ma in test_rules:
        api('DELETE', f'/api/pricing-rules/{ma}', A)

print(f"\n==> {sum(results)}/{len(results)} kiểm tra đạt")
sys.exit(0 if all(results) else 1)
