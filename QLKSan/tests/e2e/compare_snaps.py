import difflib, os, re, sys
A, B = sys.argv[1], sys.argv[2]

# Bỏ các giá trị thay đổi theo mỗi lần tải trang: antiforgery token, giờ hiện tại, "x phút trước", mã phiếu tạm ngẫu nhiên
NOISE = [(r'\[value=CfDJ8[^\]]*\]', '[antiforgery]'), (r'^\d\d:\d\d [AP]M$', '[giờ]'),
         (r'Thêm -?\d+ phút trước', 'Thêm N phút trước'), (r'#PR-\d+', '#PR-N')]
def clean(lines):
    out = []
    for l in lines:
        for pat, rep in NOISE: l = re.sub(pat, rep, l)
        out.append(l)
    return out
same = diff = 0
for name in sorted(os.listdir(A)):
    if not name.endswith('.txt'): continue
    a = clean(open(os.path.join(A, name), encoding='utf-8').read().splitlines())
    pb = os.path.join(B, name)
    b = clean(open(pb, encoding='utf-8').read().splitlines()) if os.path.exists(pb) else ['(không có)']
    if a == b:
        same += 1
        continue
    diff += 1
    print(f'===== KHÁC: {name}')
    for line in list(difflib.unified_diff(a, b, 'trước', 'sau', n=0, lineterm=''))[2:40]:
        print('   ' + line[:220])
print(f'\n==> {same} trang giống hệt, {diff} trang khác')
