// Exporta cualquier grilla a un .csv que Excel abre nativamente — sin agregar una librería nueva.
// El BOM UTF-8 es necesario para que Excel (no solo navegadores) muestre tildes/ñ correctamente.
export interface CsvColumn { key: string; label: string; }

function csvCell(value: unknown): string {
  const text = value === null || value === undefined ? '' : String(value);
  return /[",\n]/.test(text) ? '"' + text.replaceAll('"', '""') + '"' : text;
}

export function exportCsv(filename: string, columns: CsvColumn[], rows: readonly object[]): void {
  const header = columns.map(c => csvCell(c.label)).join(',');
  const body = rows.map(row => columns.map(c => csvCell((row as Record<string, unknown>)[c.key])).join(',')).join('\n');
  const blob = new Blob(['﻿' + header + '\n' + body], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url; a.download = filename.endsWith('.csv') ? filename : filename + '.csv';
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
