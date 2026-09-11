import {Component,inject,signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {ApiClient,errorMessage} from '../../core/api';
import {AuthService} from '../../core/auth';
import {Icon} from '../../shared/icon';
import {AlertFocus} from '../../shared/alert-focus';
type Row=Record<string,any>;
@Component({selector:'mapan-dashboard',imports:[RouterLink,Icon,AlertFocus],template:`
  <header class="page-heading"><div><span class="eyebrow">BANDEJA DE TRABAJO</span><h1>Tu trabajo de hoy</h1><p>Retoma un expediente o comienza una nueva operación de crédito.</p></div>@if(auth.can('Solicitudes:Create')){<a class="primary-link" routerLink="/nuevo-analisis"><m-icon name="plus"/>Nuevo análisis</a>}</header>
  @if(showStats()) {
    <div class="stat-grid">
      @if(auth.can('Solicitudes:Read')){<a class="stat-card brand" routerLink="/solicitudes"><strong>{{solicitudesTotal()}}</strong><span>Solicitudes en la empresa</span></a>}
      @if(auth.can('Workflow:Read')){<a class="stat-card" [class.warn]="aprobacionesPendientes()>0" routerLink="/aprobaciones"><strong>{{aprobacionesPendientes()}}</strong><span>Aprobaciones esperando tu firma</span></a>}
      @if(auth.can('Alertas:Read')){<a class="stat-card" [class.warn]="alertasPendientes()>0" routerLink="/alertas"><strong>{{alertasPendientes()}}</strong><span>Alertas sin revisar</span></a>}
      @if(auth.can('Mora:Read')){<a class="stat-card" [class.warn]="moraTotal()>0" routerLink="/mora"><strong>{{moraTotal()}}</strong><span>Clientes en mora</span></a>}
      @if(auth.can('Analistas:Read')){<a class="stat-card" routerLink="/analistas"><strong>{{analistasTotal()}}</strong><span>Analistas activos</span></a>}
    </div>
  }
  @if(pendientes().length){<section class="panel"><div class="section-title"><span class="icon-tile"><m-icon name="check"/></span><h2>Pendiente de tu atención</h2><span class="count">{{pendientes().length}}</span></div>
    @for(p of pendientes();track p.key){<div class="list-row"><div><strong>{{p.titulo}}</strong><p>{{p.detalle}}</p></div><a class="text-action" [routerLink]="p.link"><m-icon name="arrow"/>Revisar</a></div>}
  </section>}
  @if(auth.can('Solicitudes:Read')) {
    <section class="panel"><h2>Solicitudes recientes</h2><p>Los borradores admiten captura económica; los expedientes en documentación admiten carga de archivos.</p>
      @if(loading()){<p role="status">Cargando solicitudes…</p>}
      @if(error()){<p role="alert" class="error" mapanAlertFocus>{{error()}}</p><button (click)="load()">Reintentar</button>}
      @if(!loading()&&!error()&&items().length===0){<p>No hay solicitudes registradas.</p>}
      @for(item of items();track item.solicitudCreditoId){<div class="list-row"><a [routerLink]="['/solicitudes',item.solicitudCreditoId,'expediente']">{{item.numeroSolicitud}} · Abrir expediente</a><span class="badge" [attr.data-state]="item.estado">{{item.estado}}</span></div>}
    </section>
  } @else {<section class="panel"><h2>Acceso disponible</h2><p>No tienes permiso para consultar solicitudes. Contacta al administrador de tu empresa si necesitas acceso.</p></section>}
`})
export class Dashboard {
  auth=inject(AuthService);private api=inject(ApiClient);loading=signal(false);error=signal('');
  items=signal<{solicitudCreditoId:string;numeroSolicitud:string;estado:string}[]>([]);
  solicitudesTotal=signal(0);moraTotal=signal(0);analistasTotal=signal(0);alertasPendientes=signal(0);aprobacionesPendientes=signal(0);
  aprobacionesInbox=signal<Row[]>([]);alertasInbox=signal<Row[]>([]);
  showStats(){return this.auth.can('Solicitudes:Read')||this.auth.can('Mora:Read')||this.auth.can('Analistas:Read')||this.auth.can('Alertas:Read')||this.auth.can('Workflow:Read');}
  // Une lo más urgente de dos bandejas separadas (aprobaciones y alertas) en una sola lista accionable,
  // en vez de obligar al usuario a visitar cada página para saber si tiene algo pendiente.
  pendientes(){
    const rows:{key:string;titulo:string;detalle:string;link:any[]}[]=[];
    for(const a of this.aprobacionesInbox().slice(0,5))
      rows.push({key:'ap-'+a['aprobacionPasoId'],titulo:a['solicitud']+' · '+a['nombrePaso'],detalle:'Recomendación: '+(a['recomendacion']??'—')+' · rol '+a['rol'],link:['/analisis',a['analisisId']]});
    for(const al of this.alertasInbox().slice(0,5))
      rows.push({key:'al-'+al['alertaId'],titulo:al['titulo'],detalle:al['descripcion']??'',link:['/analisis',al['analisisId']]});
    return rows;
  }
  constructor(){
    if(this.auth.can('Solicitudes:Read'))this.load();
    if(this.auth.can('Mora:Read'))this.api.get<unknown[]>('/mora').subscribe({next:r=>this.moraTotal.set(r.length),error:()=>{}});
    if(this.auth.can('Analistas:Read'))this.api.get<unknown[]>('/analistas/desempeno').subscribe({next:r=>this.analistasTotal.set(r.length),error:()=>{}});
    if(this.auth.can('Workflow:Read'))this.api.get<Row[]>('/workflow').subscribe({next:r=>{this.aprobacionesInbox.set(r);this.aprobacionesPendientes.set(r.length);},error:()=>{}});
    if(this.auth.can('Alertas:Read'))this.api.get<Row[]>('/alertas').subscribe({next:r=>{const pendientes=r.filter(a=>!a['resuelta']);this.alertasInbox.set(pendientes);this.alertasPendientes.set(pendientes.length);},error:()=>{}});
  }
  load(){this.loading.set(true);this.error.set('');this.api.get<{items:{solicitudCreditoId:string;numeroSolicitud:string;estado:string}[];total:number}>('/solicitudes?page=1&size=5').subscribe({next:r=>{this.items.set(r.items);this.solicitudesTotal.set(r.total);this.loading.set(false);},error:e=>{this.error.set(errorMessage(e));this.loading.set(false);}});}
}
