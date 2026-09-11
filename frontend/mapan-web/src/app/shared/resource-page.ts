import {Component,inject,signal} from '@angular/core';
import {DatePipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute,Router,RouterLink} from '@angular/router';
import {ApiClient,errorMessage} from '../core/api';
import {AuthService} from '../core/auth';
import {DialogService, Modal} from './dialog';
import {LoadingOverlayService} from './loading-overlay';
import {AlertFocus} from './alert-focus';
import {Icon} from './icon';
import {exportCsv} from './export-csv';
type Value=string|number|null;
type Row=Record<string,Value>;
export interface Field {key:string;label:string;type?:'number'|'date'|'select';required?:boolean;max?:number;options?:{value:string;label:string}[];source?:string;id?:string;display?:string;}
export interface ResourceConfig {title:string;singular:string;path:string;id:string;permission:string;paged:boolean;search:boolean;fields:Field[];columns:string[];}
@Component({selector:'mapan-resource-page',imports:[FormsModule,RouterLink,Modal,AlertFocus,Icon,DatePipe],template:`
  <header class="page-heading"><div><span class="eyebrow">GESTIÓN</span><h1>{{config.title}}</h1></div>
  @if(auth.can(config.permission+':Create')){<button (click)="create()">Nuevo {{config.singular}}</button>}</header>
  @if(error()){<p class="error" role="alert" mapanAlertFocus>{{error()}}</p>}
  @if(success()){<p class="success" role="status">{{success()}}</p>}
  @if(editing()) {
    <mapan-modal [title]="(selectedId()?'Editar':'Crear')+' '+config.singular" (close)="editing.set(false)">
    <form (ngSubmit)="save()" #form="ngForm"><div class="grid-form">
      @for(field of config.fields;track field.key){<div><label [for]="field.key">{{field.label}}</label>
      @if(field.key==="clienteId"){<input aria-label="Buscar cliente por identificación" placeholder="Buscar por identificación o nombre" [ngModel]="clientSearch" (ngModelChange)="clientSearch=$event" [ngModelOptions]="{standalone:true}"><button type="button" class="secondary" (click)="searchClients()">Buscar cliente</button><a routerLink="/clientes">Crear cliente</a>}@switch(field.type){
        @case('number'){<input type="number" step="any" [id]="field.key" [name]="field.key" [(ngModel)]="model[field.key]" [required]="field.required??false">}
        @case('date'){<input type="date" [id]="field.key" [name]="field.key" [(ngModel)]="model[field.key]">}
        @case('select'){<select [id]="field.key" [name]="field.key" [(ngModel)]="model[field.key]" [required]="field.required??false"><option value="">Seleccionar…</option>@for(option of options()[field.key]??field.options??[];track option.value){<option [value]="option.value">{{option.label}}</option>}</select>}
        @default{<input [id]="field.key" [name]="field.key" [(ngModel)]="model[field.key]" [required]="field.required??false" [attr.maxlength]="field.max??null">}
      }</div>}
    </div><div class="actions"><button [disabled]="busy()||form.invalid">{{busy()?'Guardando…':'Guardar'}}</button><button type="button" class="secondary" (click)="editing.set(false)">Cancelar</button></div></form></mapan-modal>
  }
  @if(detail()){<mapan-modal title="Detalle" (close)="detail.set(null)"><dl>@for(field of config.fields;track field.key){<dt>{{field.label}}</dt><dd>{{displayValue(field,detail()![field.key])}}</dd>}</dl><p>Estado: {{detail()!['estado']}}</p><button class="secondary" (click)="detail.set(null)">Cerrar</button>
  @if(history().length){<h3>Solicitudes históricas</h3>@for(item of history();track item['solicitudCreditoId']){<div class="list-row">{{item['numeroSolicitud']}} <span>{{item['estado']}}</span></div>}
  }</mapan-modal>}
  <section class="panel"><div class="toolbar">
    @if(config.search){<input aria-label="Buscar por identificación o nombre" placeholder="Identificación o nombre" [(ngModel)]="search" (keyup.enter)="searchNow()"><button class="secondary" [disabled]="loading()" (click)="searchNow()">Buscar</button>}
    <button class="ghost compact" [disabled]="loading()" (click)="exportar()"><m-icon name="download"/>Excel</button>
    <button class="secondary" [disabled]="loading()" (click)="load()">Actualizar</button>
  </div>
  @if(loading()){<p role="status">Cargando registros…</p>}@else{
  @if(items().length===0){<p>No hay registros para esta consulta.</p>}@else{
  <div class="table-scroll"><table><thead><tr>@for(column of config.columns;track column){<th>{{label(column)}}</th>}<th>Estado</th><th>Acciones</th></tr></thead><tbody>
  @for(row of items();track row[config.id]){<tr>@for(column of config.columns;track column){<td>@if(isDateColumn(column)){{{row[column] ? (row[column]+'' | date:'dd/MM/yyyy HH:mm') : '—'}}}@else{ {{row[column]??'—'}} }</td>}<td><span class="badge">{{row['estado']}}</span></td><td>
    <button class="secondary" [disabled]="busy()" (click)="show(row)">Ver</button>
    @if(config.path==='/solicitudes'){<a [routerLink]="['/solicitudes',row[config.id],'expediente']">Abrir expediente</a>}
    @if(auth.can(config.permission+':Update')&&(config.path!=='/solicitudes'||row['estado']==='BORRADOR')){<button class="secondary" [disabled]="busy()" (click)="edit(row)">Editar</button>}
    @if(config.path==='/clientes'&&auth.can('Clientes:Inactivate')&&row['estado']==='ACTIVO'){<button class="secondary" [disabled]="busy()" (click)="action(row,'inactivar')">Inactivar</button>}
    @if(config.path==='/productos'&&auth.can('Productos:Update')){<button class="secondary" [disabled]="busy()" (click)="action(row,row['estado']==='ACTIVO'?'inactivar':'activar')">{{row['estado']==='ACTIVO'?'Inactivar':'Activar'}}</button>}
    @if(config.path==='/solicitudes'&&auth.can('Solicitudes:Send')&&row['estado']==='BORRADOR'){<button class="secondary" [disabled]="busy()" (click)="action(row,'enviar-documentacion')">Enviar a documentación</button>}
  </td></tr>}
  </tbody></table></div>}}
  @if(config.paged){<div class="pagination"><button class="secondary" [disabled]="page()===1||loading()" (click)="changePage(-1)">Anterior</button><span>Página {{page()}} · {{total()}} registros</span><button class="secondary" [disabled]="page()*size()>=total()||loading()" (click)="changePage(1)">Siguiente</button><label class="page-size">Ver<select [ngModel]="size()" (ngModelChange)="size.set($event);page.set(1);load()"><option [ngValue]="5">5</option><option [ngValue]="10">10</option><option [ngValue]="20">20</option><option [ngValue]="50">50</option></select>registros</label></div>}
  </section>`})
export class ResourcePage {
  private router=inject(Router);
  private newAnalysis=inject(ActivatedRoute).snapshot.data['newAnalysis']===true;
  config=inject(ActivatedRoute).snapshot.data['resource'] as ResourceConfig;
  auth=inject(AuthService);private api=inject(ApiClient);private dialog=inject(DialogService);private loadingOverlay=inject(LoadingOverlayService);
  items=signal<Row[]>([]);loading=signal(false);busy=signal(false);error=signal('');success=signal('');editing=signal(false);
  page=signal(1);size=signal(20);total=signal(0);selectedId=signal<string|null>(null);detail=signal<Row|null>(null);history=signal<Row[]>([]);
  options=signal<Record<string,{value:string;label:string}[]>>({});search='';clientSearch='';model:Row={};
  constructor(){this.load();if(this.newAnalysis)this.create();}
  private static readonly ColumnLabels:Record<string,string>={fechaCreacion:'Creada',fechaDesembolso:'Desembolso',fechaVencimiento:'Vencimiento'};
  label(key:string){return this.config.fields.find(f=>f.key===key)?.label??ResourcePage.ColumnLabels[key]??key;}
  isDateColumn(key:string){return key.startsWith('fecha');}
  displayValue(field:Field,value:Value){return field.source?(this.options()[field.key]?.find(o=>o.value===value)?.label??'Consulta el expediente para ver el detalle'):value??'Sin dato';}
  searchClients(){this.api.get<{items:Row[]}>('/clientes?page=1&size=100&search='+encodeURIComponent(this.clientSearch)).subscribe({next:r=>this.options.update(o=>({...o,clienteId:r.items.map(row=>({value:String(row['clienteId']),label:String(row['numeroIdentificacion'])+' · '+String(row['razonSocial']??row['nombres']??'')}))})),error:e=>this.error.set(errorMessage(e))});}
  searchNow(){this.page.set(1);this.load();}
  changePage(delta:number){this.page.update(p=>p+delta);this.load();}
  load(){this.loading.set(true);this.error.set('');this.loadingOverlay.show('Cargando registros…');const query=this.config.paged?'?page='+this.page()+'&size='+this.size()+(this.config.search?'&search='+encodeURIComponent(this.search):''):'?soloActivos=false';
    this.api.get<Row[]|{items:Row[];total:number}>(this.config.path+query).subscribe({next:r=>{this.items.set(Array.isArray(r)?r:r.items);this.total.set(Array.isArray(r)?r.length:r.total);this.loading.set(false);this.loadingOverlay.hide();},error:e=>{this.error.set(errorMessage(e));this.loading.set(false);this.loadingOverlay.hide();}});}
  create(){this.model={};this.selectedId.set(null);this.editing.set(true);this.loadOptions();}
  edit(row:Row){this.model={};for(const field of this.config.fields)this.model[field.key]=row[field.key]??null;this.selectedId.set(String(row[this.config.id]));this.editing.set(true);this.loadOptions();}
  private loadOptions(){for(const f of this.config.fields.filter(f=>f.source))this.api.get<Row[]|{items:Row[]}>(f.source!).subscribe({next:r=>{const rows=Array.isArray(r)?r:r.items;this.options.update(o=>({...o,[f.key]:rows.map(row=>({value:String(row[f.id!]),label:String(row[f.display!]??row[f.id!])}))}));},error:e=>this.error.set(errorMessage(e))});}
  save(){this.busy.set(true);this.error.set('');this.loadingOverlay.show('Guardando…');const payload:Row={};for(const field of this.config.fields)payload[field.key]=this.model[field.key]===''?null:this.model[field.key]??null;
    const id=this.selectedId();const request=id?this.api.put(this.config.path+'/'+id+(this.config.path==='/solicitudes'?'/borrador':''),payload):this.api.post(this.config.path,payload);
    request.subscribe({next:r=>{this.busy.set(false);this.loadingOverlay.hide();this.editing.set(false);this.success.set('Registro guardado.');if(this.newAnalysis){const created=r as Row;void this.router.navigate(['/solicitudes',created['solicitudCreditoId'],'expediente']);}else this.load();},error:e=>{this.busy.set(false);this.loadingOverlay.hide();this.error.set(errorMessage(e));}});}
  show(row:Row){this.loadOptions();this.detail.set(row);this.history.set([]);if(this.config.path==='/clientes'&&this.auth.can('Solicitudes:Read'))this.api.get<{items:Row[]}>('/solicitudes?clienteId='+row[this.config.id]+'&page=1&size=100').subscribe({next:r=>this.history.set(r.items),error:e=>this.error.set(errorMessage(e))});}
  exportar(){this.loadingOverlay.show('Generando Excel…');try{exportCsv(this.config.path.replace('/',''),[...this.config.columns.map(key=>({key,label:this.label(key)})),{key:'estado',label:'Estado'}],this.items());}finally{this.loadingOverlay.hide();}}
  async action(row:Row,action:string){if(!await this.dialog.confirm('¿Confirmas la operación '+action.replaceAll('-',' ')+'?',{title:'Confirmar operación'}))return;this.busy.set(true);this.loadingOverlay.show('Procesando…');this.api.post(this.config.path+'/'+row[this.config.id]+'/'+action,{}).subscribe({next:()=>{this.busy.set(false);this.loadingOverlay.hide();this.success.set('Operación completada.');this.load();},error:e=>{this.busy.set(false);this.loadingOverlay.hide();this.error.set(errorMessage(e));}});}
}

