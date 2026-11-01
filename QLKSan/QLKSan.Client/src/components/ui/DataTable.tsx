import { useMemo, useState, type ReactNode } from 'react';
import './ui.css';

export interface Cot<T> {
  key: string;
  tieuDe: string;
  hien: (row: T) => ReactNode;
  // Giá trị để sắp xếp; không có thì cột không sắp xếp được
  sapXep?: (row: T) => string | number | null | undefined;
  canh?: 'trai' | 'phai' | 'giua';
}

interface Props<T> {
  cot: Cot<T>[];
  dong: T[] | undefined;
  khoa: (row: T) => string | number;
  trong?: string;
  chuThich?: string;
  sapXepMacDinh?: { key: string; giam?: boolean };
}

// Bảng dữ liệu dùng chung: sắp xếp phía trình duyệt, cuộn ngang trên màn hình hẹp, caption cho trình đọc màn hình
export function DataTable<T>({ cot, dong, khoa, trong = 'Không có dữ liệu.', chuThich, sapXepMacDinh }: Props<T>) {
  const [sx, setSx] = useState(sapXepMacDinh ?? null);

  const ds = useMemo(() => {
    const c = sx && cot.find((x) => x.key === sx.key);
    if (!dong || !c?.sapXep) return dong ?? [];
    const lay = c.sapXep;
    return [...dong].sort((a, b) => {
      const va = lay(a), vb = lay(b);
      if (va == null && vb == null) return 0;
      if (va == null) return 1;
      if (vb == null) return -1;
      const r = typeof va === 'number' && typeof vb === 'number' ? va - vb : String(va).localeCompare(String(vb), 'vi');
      return sx!.giam ? -r : r;
    });
  }, [dong, cot, sx]);

  const bam = (c: Cot<T>) => setSx((s) => (s?.key === c.key ? { key: c.key, giam: !s.giam } : { key: c.key }));

  return (
    <div className="bang-cuon">
      <table className="bang">
        {chuThich && <caption className="sr-only">{chuThich}</caption>}
        <thead>
          <tr>
            {cot.map((c) => {
              const dangSx = sx?.key === c.key;
              return (
                <th key={c.key} scope="col" className={c.canh ? `canh-${c.canh}` : undefined}
                  aria-sort={dangSx ? (sx!.giam ? 'descending' : 'ascending') : undefined}>
                  {c.sapXep ? (
                    <button type="button" className="th-sx" onClick={() => bam(c)}>
                      {c.tieuDe}<span aria-hidden="true">{dangSx ? (sx!.giam ? ' ▼' : ' ▲') : ' ↕'}</span>
                    </button>
                  ) : c.tieuDe}
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody>
          {ds.length === 0 ? (
            <tr><td colSpan={cot.length} className="bang-trong">{trong}</td></tr>
          ) : ds.map((r) => (
            <tr key={khoa(r)}>
              {cot.map((c) => <td key={c.key} className={c.canh ? `canh-${c.canh}` : undefined}>{c.hien(r)}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
