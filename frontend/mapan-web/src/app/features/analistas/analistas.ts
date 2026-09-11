import {Component, inject, signal} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ApiClient, errorMessage} from '../../core/api';
import {AuthService} from '../../core/auth';
import {ClientPager} from '../../shared/pager';
import {AlertFocus} from '../../shared/alert-focus';
import {Icon} from '../../shared/icon';
import {exportCsv} from '../../shared/export-csv';
import {LoadingOverlayService} from '../../shared/loading-overlay';

interface AnalistaDesempeno {
  usuarioEmpresaId: string; analista: string; sucursalId: string; sucursal: string;
  solicitudesTotal: number; solicitudesAprobadas: number; solicitudesRechazadas: number; solicitudesEnProceso: number;
  montoColocado: number; tasaAprobacionPct: number | null; prestamosEnMora: number; saldoEnMora: number;
}

@Component({
  selector: 'mapan-analistas',
  imports: [DecimalPipe, AlertFocus, Icon, FormsModule],
  template: `
<header class="page-heading">
  <div><span class="eyebrow">GESTIÓN DE EQUIPO</span><h1>Desempeño de analistas</h1><p>Para jefes de agencia y alta gerencia: cuántas solicitudes gestiona cada analista, cuánto han colocado, su tasa de aprobación y su cartera en mora.</p></div>
  <button class="ghost compact" [disabled]="loading()" (click)="exportar()"><m-icon name="download"/>Excel</button>
</header>

@if (loading()) {<p role="status">Cargando desempeño…</p>}
@if (error()) {<p role="alert" class="error" mapanAlertFocus>{{ error() }}</p><button (click)="load()">Reintentar</button>}

@if (!loading() && !error()) {
  <section class="panel">
    @if (filas().length === 0) {<p>Aún no hay solicitudes registradas para calcular desempeño.</p>}
    <div class="table-scroll">
      <table>
        <thead><tr><th>Analista</th><th>Sucursal</th><th>Solicitudes</th><th>Aprobadas</th><th>Rechazadas</th><th>En proceso</th><th>Monto colocado</th><th>Tasa de aprobación</th><th>Préstamos en mora</th></tr></thead>
        <tbody>
          @for (f of pager.items(); track f.usuarioEmpresaId) {
            <tr>
              <td><strong>{{ f.analista }}</strong></td>
              <td>{{ f.sucursal }}</td>
              <td>{{ f.solicitudesTotal }}</td>
              <td>{{ f.solicitudesAprobadas }}</td>
              <td>{{ f.solicitudesRechazadas }}</td>
              <td>{{ f.solicitudesEnProceso }}</td>
              <td>{{ f.montoColocado | number: '1.2-2' }}</td>
              <td>{{ f.tasaAprobacionPct !== null ? (f.tasaAprobacionPct + '%') : '—' }}</td>
              <td>
                @if (f.prestamosEnMora > 0) {<span class="badge" data-state="ROJO">{{ f.prestamosEnMora }} ({{ f.saldoEnMora | number: '1.2-2' }})</span>}
                @else {<span class="badge" data-state="VERDE">Al día</span>}
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
    <div class="pagination"><button class="secondary compact" [disabled]="pager.page()===1" (click)="pager.prev()">Anterior</button><span>Página {{ pager.page() }} de {{ pager.totalPages() }} · {{ pager.total() }} registros</span><button class="secondary compact" [disabled]="pager.page()===pager.totalPages()" (click)="pager.next()">Siguiente</button><label class="page-size">Ver<select [ngModel]="pager.pageSize()" (ngModelChange)="pager.setPageSize($event)"><option [ngValue]="5">5</option><option [ngValue]="10">10</option><option [ngValue]="15">15</option><option [ngValue]="20">20</option></select>registros</label></div>
  </section>
}
`,
})
export class AnalistasPage {
  auth = inject(AuthService);
  private api = inject(ApiClient);
  private loadingOverlay = inject(LoadingOverlayService);
  loading = signal(false);
  error = signal('');
  filas = signal<AnalistaDesempeno[]>([]);
  pager = new ClientPager<AnalistaDesempeno>(15);

  constructor() { this.load(); }

  load() {
    this.loading.set(true); this.error.set(''); this.loadingOverlay.show('Cargando desempeño…');
    this.api.get<AnalistaDesempeno[]>('/analistas/desempeno').subscribe({
      next: r => { this.filas.set(r); this.pager.set(r); this.loading.set(false); this.loadingOverlay.hide(); },
      error: e => { this.error.set(errorMessage(e)); this.loading.set(false); this.loadingOverlay.hide(); },
    });
  }

  exportar() {
    this.loadingOverlay.show('Generando Excel…');
    try { exportCsv('desempeno-analistas', [
      {key:'analista',label:'Analista'},{key:'sucursal',label:'Sucursal'},{key:'solicitudesTotal',label:'Solicitudes'},
      {key:'solicitudesAprobadas',label:'Aprobadas'},{key:'solicitudesRechazadas',label:'Rechazadas'},{key:'solicitudesEnProceso',label:'En proceso'},
      {key:'montoColocado',label:'Monto colocado'},{key:'tasaAprobacionPct',label:'Tasa de aprobación %'},
      {key:'prestamosEnMora',label:'Préstamos en mora'},{key:'saldoEnMora',label:'Saldo en mora'},
    ], this.filas()); } finally { this.loadingOverlay.hide(); }
  }
}
