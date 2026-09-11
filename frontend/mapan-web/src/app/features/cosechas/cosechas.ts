import {Component, inject, signal} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ApiClient, errorMessage} from '../../core/api';
import {ClientPager} from '../../shared/pager';
import {AlertFocus} from '../../shared/alert-focus';
import {Icon} from '../../shared/icon';
import {exportCsv} from '../../shared/export-csv';
import {LoadingOverlayService} from '../../shared/loading-overlay';

interface CosechaPunto {
  mob: number; prestamosActivos: number; prestamosEnMora30: number;
  saldoActivo: number; saldoEnMora30: number; tasaMora30Pct: number | null;
}
interface Cosecha { cosecha: string; prestamosOriginados: number; montoOriginado: number; puntos: CosechaPunto[]; }
interface Option { id: string; nombre: string; }

@Component({
  selector: 'mapan-cosechas',
  imports: [DecimalPipe, FormsModule, AlertFocus, Icon],
  template: `
<header class="page-heading">
  <div><span class="eyebrow">CARTERA</span><h1>Análisis de cosechas</h1><p>Compara cómo se deteriora cada mes de originación (cosecha) a medida que pasan los meses — la técnica estándar del sector financiero para detectar si una añada de créditos está saliendo peor que las anteriores.</p></div>
  <button class="ghost compact" [disabled]="loading()" (click)="exportar()"><m-icon name="download"/>Excel</button>
</header>

<section class="panel filters">
  <label>Producto<select [disabled]="loading()" [(ngModel)]="productoId" (ngModelChange)="load()"><option value="">Todos</option>@for(p of productos();track p.id){<option [value]="p.id">{{p.nombre}}</option>}</select></label>
  <label>Sucursal<select [disabled]="loading()" [(ngModel)]="sucursalId" (ngModelChange)="load()"><option value="">Todas</option>@for(s of sucursales();track s.id){<option [value]="s.id">{{s.nombre}}</option>}</select></label>
</section>

@if (loading()) {<p role="status">Calculando cosechas…</p>}
@if (error()) {<p role="alert" class="error" mapanAlertFocus>{{ error() }}</p><button (click)="load()">Reintentar</button>}

@if (!loading() && !error()) {
  <section class="panel">
    @if (cosechas().length === 0) {<p>No hay préstamos desembolsados que coincidan con el filtro.</p>}
    @if (cosechas().length > 0) {
      <p class="hint">Cada fila es una cosecha (mes de desembolso); cada columna es MOB (meses desde el desembolso). El color muestra el % de préstamos de esa cosecha en mora 30+ días en ese momento de su vida — compara diagonalmente hacia abajo para ver si las cosechas recientes se deterioran más rápido que las antiguas.</p>
      <div class="table-scroll">
        <table class="cosecha-table">
          <thead><tr><th>Cosecha</th><th>Originados</th><th>Monto</th>@for(m of mobColumns();track m){<th>MOB {{m}}</th>}</tr></thead>
          <tbody>
            @for (c of pager.items(); track c.cosecha) {
              <tr>
                <td><strong>{{ c.cosecha }}</strong></td>
                <td>{{ c.prestamosOriginados }}</td>
                <td>{{ c.montoOriginado | number: '1.2-2' }}</td>
                @for (m of mobColumns(); track m) {
                  <td>
                    @if (puntoDe(c, m); as pt) {
                      <span class="badge" [attr.data-state]="nivel(pt.tasaMora30Pct)">{{ pt.tasaMora30Pct }}%</span>
                      <small class="hint">{{ pt.prestamosEnMora30 }}/{{ pt.prestamosActivos }}</small>
                    } @else { <span class="hint">—</span> }
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="pagination"><button class="secondary compact" [disabled]="pager.page()===1" (click)="pager.prev()">Anterior</button><span>Página {{ pager.page() }} de {{ pager.totalPages() }} · {{ pager.total() }} cosechas</span><button class="secondary compact" [disabled]="pager.page()===pager.totalPages()" (click)="pager.next()">Siguiente</button><label class="page-size">Ver<select [ngModel]="pager.pageSize()" (ngModelChange)="pager.setPageSize($event)"><option [ngValue]="6">6</option><option [ngValue]="12">12</option><option [ngValue]="20">20</option></select>registros</label></div>
    }
  </section>
}
`,
})
export class CosechasPage {
  private api = inject(ApiClient);
  private loadingOverlay = inject(LoadingOverlayService);
  loading = signal(false);
  error = signal('');
  cosechas = signal<Cosecha[]>([]);
  productos = signal<Option[]>([]);
  sucursales = signal<Option[]>([]);
  productoId = '';
  sucursalId = '';
  pager = new ClientPager<Cosecha>(12);

  constructor() {
    this.api.get<any[]>('/productos').subscribe({next: r => this.productos.set(r.map(p => ({id: p.productoCreditoId, nombre: p.nombre})))});
    this.api.get<any[]>('/organizacion/sucursales').subscribe({next: r => this.sucursales.set(r.map(s => ({id: s.sucursalId, nombre: s.nombre})))});
    this.load();
  }

  mobColumns() {
    const max = this.cosechas().reduce((m, c) => Math.max(m, ...c.puntos.map(p => p.mob), 0), 0);
    return Array.from({length: Math.min(max, 24) + 1}, (_, i) => i);
  }
  puntoDe(c: Cosecha, mob: number) { return c.puntos.find(p => p.mob === mob) ?? null; }
  nivel(tasa: number | null) { if (tasa == null) return 'SIN DATOS'; if (tasa >= 15) return 'ALTO'; if (tasa >= 5) return 'MEDIO'; return 'BAJO'; }

  load() {
    this.loading.set(true); this.error.set(''); this.loadingOverlay.show('Calculando cosechas…');
    let path = '/cosechas';
    const params: string[] = [];
    if (this.productoId) params.push('productoCreditoId=' + this.productoId);
    if (this.sucursalId) params.push('sucursalId=' + this.sucursalId);
    if (params.length) path += '?' + params.join('&');
    this.api.get<Cosecha[]>(path).subscribe({
      next: r => { this.cosechas.set(r); this.pager.set(r); this.loading.set(false); this.loadingOverlay.hide(); },
      error: e => { this.error.set(errorMessage(e)); this.loading.set(false); this.loadingOverlay.hide(); },
    });
  }

  // La grilla en pantalla es una matriz (cosecha x MOB); el CSV se exporta "largo" (una fila por
  // punto cosecha+MOB) porque el número de columnas MOB varía según los datos y un CSV no soporta
  // encabezados dinámicos tan bien como una tabla HTML.
  exportar() {
    this.loadingOverlay.show('Generando Excel…');
    try {
      const filas = this.cosechas().flatMap(c => c.puntos.map(p => ({
        cosecha: c.cosecha, prestamosOriginados: c.prestamosOriginados, montoOriginado: c.montoOriginado,
        mob: p.mob, prestamosActivos: p.prestamosActivos, prestamosEnMora30: p.prestamosEnMora30,
        saldoActivo: p.saldoActivo, saldoEnMora30: p.saldoEnMora30, tasaMora30Pct: p.tasaMora30Pct,
      })));
      exportCsv('cosechas', [
        {key:'cosecha',label:'Cosecha'},{key:'prestamosOriginados',label:'Originados'},{key:'montoOriginado',label:'Monto originado'},
        {key:'mob',label:'MOB'},{key:'prestamosActivos',label:'Préstamos activos'},{key:'prestamosEnMora30',label:'En mora 30+'},
        {key:'saldoActivo',label:'Saldo activo'},{key:'saldoEnMora30',label:'Saldo en mora 30+'},{key:'tasaMora30Pct',label:'Tasa mora 30+ %'},
      ], filas);
    } finally { this.loadingOverlay.hide(); }
  }
}
