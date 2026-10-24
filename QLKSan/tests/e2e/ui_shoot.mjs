// Chụp màn hình toàn trang + đo lỗi bố cục cho từng vai trò, qua Chrome DevTools Protocol (không cần thư viện)
// node shoot.mjs <outDir> <width> <height> [filter]
import { spawn } from 'node:child_process';
import { writeFileSync, mkdirSync } from 'node:fs';

const [outDir, W = '1440', H = '900', filter = ''] = process.argv.slice(2);
mkdirSync(outDir, { recursive: true });
const WEB = 'https://localhost:7234';
const PORT = Number(process.env.CDP_PORT || 9555); // Chrome headless đã chạy sẵn
const chrome = { kill() { } };

const sleep = ms => new Promise(r => setTimeout(r, ms));
let ws, id = 0; const pending = new Map(); const events = [];
async function connect() {
  for (let i = 0; i < 150; i++) {
    try {
      const list = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json();
      const page = list.find(t => t.type === 'page');
      if (page) { ws = new WebSocket(page.webSocketDebuggerUrl); break; }
    } catch { }
    await sleep(200);
  }
  await new Promise(r => ws.onopen = r);
  ws.onmessage = m => {
    const d = JSON.parse(m.data);
    if (d.id && pending.has(d.id)) { pending.get(d.id)(d); pending.delete(d.id); }
    else if (d.method) events.push(d);
  };
}
const send = (method, params = {}) => new Promise(r => { const i = ++id; pending.set(i, r); ws.send(JSON.stringify({ id: i, method, params })); });
const evaluate = async expr => (await send('Runtime.evaluate', { expression: expr, returnByValue: true, awaitPromise: true })).result?.result?.value;

async function go(url) {
  events.length = 0;
  await send('Page.navigate', { url });
  for (let i = 0; i < 100; i++) {
    await sleep(150);
    if (await evaluate('document.readyState') === 'complete') break;
  }
  await sleep(700); // font, chart, animation
}

const PAGES = {
  anon: ['/', '/Auth/Login', '/Auth/Register'],
  letan1: ['/Reception', '/Reception/Customers', '/Reception/BookingHistory', '/Reception/CustomerDetails/KH01', '/Reception/WalkIn',
    '/Room/Status', '/Room/Details/P201', '/Room/Timeline', '/Room/Guests', '/Service?maPhong=P201', '/Housekeeping/Tasks',
    '/Checkout/Process?maPhong=P201', '/Invoice/Print/HD01'],
  admin1: ['/Admin', '/Admin/RoomTypes', '/Admin/Rooms', '/Admin/Services', '/Admin/Staff', '/Admin/Promotions', '/Admin/Reviews', '/Admin/Pricing'],
  ketoan1: ['/Accounting/Invoices', '/Accounting/Invoice/HD04', '/Accounting/Reports'],
  buongphong1: ['/Housekeeping/MyTasks'],
  khach1: ['/', '/Customer/BookRoom', '/Customer/MyStays', '/Customer/Profile', '/Customer/Feedback'],
};

// Đo lỗi bố cục trong trang
const AUDIT = `(() => {
  const vw = document.documentElement.clientWidth;
  const out = { url: location.pathname + location.search, title: document.title, scrollW: document.documentElement.scrollWidth, vw,
    height: document.documentElement.scrollHeight, overflow: [], brokenImg: [], overlapText: [], emptyCss: [] };
  for (const el of document.querySelectorAll('body *')) {
    const cs = getComputedStyle(el);
    if (cs.display === 'none' || cs.visibility === 'hidden') continue;
    const r = el.getBoundingClientRect();
    if (r.width === 0 || r.height === 0) continue;
    // phần tử tràn ra ngoài khung nhìn ngang mà cha không cuộn
    if (r.right > vw + 2 || r.left < -2) {
      let p = el.parentElement, clipped = false;
      while (p && p !== document.body) { const pc = getComputedStyle(p); if (/(auto|scroll|hidden|clip)/.test(pc.overflowX)) { clipped = true; break; } p = p.parentElement; }
      if (!clipped) out.overflow.push((el.tagName + '.' + [...el.classList].slice(0, 3).join('.')) + ' [' + Math.round(r.left) + '→' + Math.round(r.right) + ']');
    }
    // chữ bị tràn khỏi hộp của chính nó (không có ellipsis/scroll)
    if (el.children.length === 0 && el.textContent.trim().length > 0 && el.scrollWidth > el.clientWidth + 2 && cs.overflowX === 'visible' && cs.whiteSpace === 'nowrap' && el.clientWidth > 0)
      out.overlapText.push(el.tagName + ': ' + el.textContent.trim().slice(0, 40));
  }
  for (const img of document.images) if (img.complete && img.naturalWidth === 0) out.brokenImg.push(img.getAttribute('src'));
  out.overflow = [...new Set(out.overflow)].slice(0, 15);
  out.overlapText = [...new Set(out.overlapText)].slice(0, 10);
  return out;
})()`;

await connect();
await send('Page.enable'); await send('Runtime.enable'); await send('Network.enable'); await send('Log.enable');
const mobile = Number(W) < 800;
await send('Emulation.setDeviceMetricsOverride', { width: Number(W), height: Number(H), deviceScaleFactor: 1, mobile });
const report = [];
for (const [user, paths] of Object.entries(PAGES)) {
  await send('Network.clearBrowserCookies');
  if (user !== 'anon') {
    await go(WEB + '/Auth/Login');
    await evaluate(`(() => { const f = document.querySelector('form'); f.querySelector('[name=username]').value='${user}'; f.querySelector('[name=password]').value='123456'; f.submit(); })()`);
    await sleep(1500);
  }
  for (const p of paths) {
    if (filter && !(user + p).includes(filter)) continue;
    await go(WEB + p);
    const audit = await evaluate(AUDIT);
    const errs = events.filter(e => e.method === 'Runtime.exceptionThrown').map(e => e.params.exceptionDetails.exception?.description?.split('\n')[0] || e.params.exceptionDetails.text);
    const consoleErr = events.filter(e => e.method === 'Runtime.consoleAPICalled' && e.params.type === 'error').map(e => e.params.args.map(a => a.value ?? a.description).join(' '));
    const failed = events.filter(e => e.method === 'Network.responseReceived' && e.params.response.status >= 400).map(e => e.params.response.status + ' ' + e.params.response.url.replace(WEB, ''));
    const loadFail = events.filter(e => e.method === 'Network.loadingFailed' && !e.params.canceled).map(e => e.params.errorText);
    const name = `${user}__${p.replace(/[^A-Za-z0-9]+/g, '_').replace(/^_|_$/g, '') || 'home'}`;
    const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true });
    writeFileSync(`${outDir}/${name}.png`, Buffer.from(shot.result.data, 'base64'));
    report.push({ name, user, ...audit, jsErrors: [...errs, ...consoleErr], failed: [...failed, ...loadFail] });
    console.log(`${name.padEnd(55)} h=${audit?.height} scrollW=${audit?.scrollW}/${audit?.vw} overflow=${audit?.overflow.length} img=${audit?.brokenImg.length} js=${errs.length + consoleErr.length} 4xx=${failed.length}`);
  }
}
writeFileSync(`${outDir}/_report.json`, JSON.stringify(report, null, 1));
ws.close(); chrome.kill();
process.exit(0);
