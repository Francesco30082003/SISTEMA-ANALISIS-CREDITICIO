import {Component, computed, inject, signal} from '@angular/core';
import {DecimalPipe, DatePipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ApiClient, errorMessage} from '../../core/api';
import {AuthService} from '../../core/auth';
import {ClientPager} from '../../shared/pager';
import {AlertFocus} from '../../shared/alert-focus';
import {Icon} from '../../shared/icon';
import {exportCsv} from '../../shared/export-csv';
import {LoadingOverlayService} from '../../shared/loading-overlay';

interface MoraCliente {
  prestamoId: string; numeroPrestamo: string; clienteId: string; clienteNombre: string;
  telefono: string | null; correo: string | null; sucursal: string; analista: string;
  diasMora: number; banda: string; saldoCapital: number; cuotaExigible: number; fechaCorte: string;
}

const BANDAS = ['90+', '60-89', '30-59', '15-29', '1-14'];

@Component({
  selector: 'mapan-mora',
  imports: [DecimalPipe, DatePipe, AlertFocus, Icon, FormsModule],
  template: `
<header class="page-heading">
  <div><span class="eyebrow">CARTERA</span><h1>Clientes en mora</h1><p>Visión consolidada para alta gerencia y jefes de agencia: quién está en mora, en qué sucursal, con qué analista, y cómo contactarlo.</p></div>
  <div class="toolbar">
  <button class="ghost compact" [disabled]="loading()" (click)="exportar()"><m-icon name="download"/>Excel</button>
  @if (auth.can('Mora:Notificar')) {<button class="primary-link" [disabled]="enviando()" (click)="enviarAlerta()">{{ enviando() ? 'Enviando…' : 'Enviar alerta a gerencia' }}</button>}
  </div>
</header>

@if (mensaje()) {<p role="status" class="success">{{ mensaje() }}</p>}
@if (loading()) {<p role="status">Cargando cartera en mora…</p>}
@if (error()) {<p role="alert" class="error" mapanAlertFocus>{{ error() }}</p><button (click)="load()">Reintentar</button>}

@if (!loading() && !error()) {
  <section class="panel">
    <div class="mora-resumen">
      @for (banda of bandas; track banda) {
        <div class="mora-stat" [class]="'banda-' + banda">
          <strong>{{ porBanda()[banda] ?? 0 }}</strong>
          <span>{{ banda }} días</span>
        </div>
      }
      <div class="mora-stat total"><strong>{{ saldoTotal() | number: '1.2-2' }}</strong><span>Saldo total en mora (USD)</span></div>
    </div>

    @if (clientes().length === 0) {<p>No hay clientes en mora registrados.</p>}
    @for (c of pager.items(); track c.prestamoId) {
      <div class="list-row mora-row">
        <div>
          <strong>{{ c.clienteNombre }}</strong>
          <small>{{ c.sucursal }} · analista {{ c.analista }} · préstamo {{ c.numeroPrestamo }}</small>
        </div>
        <span class="badge" [class]="'banda-' + c.banda">{{ c.diasMora }} días ({{ c.banda }})</span>
        <span>Saldo: {{ c.saldoCapital | number: '1.2-2' }}</span>
        <span class="hint">Último corte: {{ c.fechaCorte | date: 'dd/MM/yyyy' }}</span>
        <div class="mora-contacto">
          @if (c.telefono) {<span>{{ c.telefono }}</span><span class="contact-links"><a class="whatsapp" [href]="waLink(c.telefono)" target="_blank" rel="noopener" title="Escribir por WhatsApp"><m-icon name="whatsapp"/></a><a class="phone" [href]="'tel:' + c.telefono" title="Llamar"><m-icon name="phone"/></a></span>} @else {<span>Sin teléfono</span>}
        </div>
      </div>
    }
    <div class="pagination"><button class="secondary compact" [disabled]="pager.page()===1" (click)="pager.prev()">Anterior</button><span>Página {{ pager.page() }} de {{ pager.totalPages() }} · {{ pager.total() }} registros</span><button class="secondary compact" [disabled]="pager.page()===pager.totalPages()" (click)="pager.next()">Siguiente</button><label class="page-size">Ver<select [ngModel]="pager.pageSize()" (ngModelChange)="pager.setPageSize($event)"><option [ngValue]="5">5</option><option [ngValue]="10">10</option><option [ngValue]="15">15</option><option [ngValue]="20">20</option></select>registros</label></div>
  </section>
}
`,
})
export class MoraPage {
  auth = inject(AuthService);
  private api = inject(ApiClient);
  private loadingOverlay = inject(LoadingOverlayService);
  loading = signal(false);
  error = signal('');
  mensaje = signal('');
  enviando = signal(false);
  clientes = signal<MoraCliente[]>([]);
  bandas = BANDAS;
  pager = new ClientPager<MoraCliente>(15);

  porBanda = computed(() => {
    const counts: Record<string, number> = {};
    for (const c of this.clientes()) counts[c.banda] = (counts[c.banda] ?? 0) + 1;
    return counts;
  });
  saldoTotal = computed(() => this.clientes().reduce((sum, c) => sum + c.saldoCapital, 0));

  constructor() { this.load(); }

  load() {
    this.loading.set(true); this.error.set(''); this.loadingOverlay.show('Cargando cartera en mora…');
    this.api.get<MoraCliente[]>('/mora').subscribe({
      next: r => { this.clientes.set(r); this.pager.set(r); this.loading.set(false); this.loadingOverlay.hide(); },
      error: e => { this.error.set(errorMessage(e)); this.loading.set(false); this.loadingOverlay.hide(); },
    });
  }

  // Números de Ecuador se registran en formato local (09XXXXXXXX); wa.me exige el formato
  // internacional sin el 0 inicial (+593 9XXXXXXXX). El enlace "tel:" en cambio funciona bien con
  // el número local tal cual, así que no hace falta transformarlo para ese caso.
  waLink(telefono: string) { return 'https://wa.me/593' + telefono.replace(/^0/, '').replace(/\D/g, ''); }
  exportar() {
    this.loadingOverlay.show('Generando Excel…');
    try { exportCsv('mora', [
      {key:'clienteNombre',label:'Cliente'},{key:'sucursal',label:'Sucursal'},{key:'analista',label:'Analista'},
      {key:'numeroPrestamo',label:'Préstamo'},{key:'diasMora',label:'Días de mora'},{key:'banda',label:'Banda'},
      {key:'saldoCapital',label:'Saldo capital'},{key:'cuotaExigible',label:'Cuota exigible'},{key:'fechaCorte',label:'Último corte'},{key:'telefono',label:'Teléfono'},
    ], this.clientes()); } finally { this.loadingOverlay.hide(); }
  }

  enviarAlerta() {
    this.enviando.set(true); this.mensaje.set(''); this.loadingOverlay.show('Enviando alerta…');
    this.api.post<number>('/mora/alertas', {}).subscribe({
      next: n => { this.enviando.set(false); this.loadingOverlay.hide(); this.mensaje.set(n > 0 ? `Alerta enviada a ${n} persona(s) de alta gerencia.` : 'No hay clientes en mora crítica (30+ días) o no hay destinatarios configurados.'); },
      error: e => { this.enviando.set(false); this.loadingOverlay.hide(); this.error.set(errorMessage(e)); },
    });
  }
}
