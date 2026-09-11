import {Component,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {DatePipe} from '@angular/common';
import {firstValueFrom} from 'rxjs';
import {ApiClient,errorMessage} from '../../core/api';
import {AuthService} from '../../core/auth';
import {DialogService} from '../../shared/dialog';
import {Icon} from '../../shared/icon';
import {AlertFocus} from '../../shared/alert-focus';
type Row=Record<string,any>;
const TRANSITIONS:Record<string,string[]>={BORRADOR:['ENTRENADO','RETIRADO'],ENTRENADO:['VALIDADO','RETIRADO'],VALIDADO:['PRODUCCION','SHADOW','RETIRADO'],SHADOW:['PRODUCCION','RETIRADO'],PRODUCCION:['RETIRADO'],RETIRADO:[]};
const LABELS:Record<string,string>={ENTRENADO:'Marcar entrenado',VALIDADO:'Marcar validado',PRODUCCION:'Promover a producción',SHADOW:'Enviar a shadow',RETIRADO:'Retirar'};
@Component({selector:'mapan-models',imports:[FormsModule,DatePipe,Icon,AlertFocus],template:`
<header class="page-heading"><div><span class="eyebrow">RIESGO / MODELOS PREDICTIVOS</span><h1>Modelos predictivos</h1><p>Registra aquí el modelo que entrenaste aparte con datos reales. MAPAN no entrena ni fabrica probabilidades.</p></div>@if(canManage()){<button (click)="modelForm.set({})"><m-icon name="plus"/>Nuevo modelo</button>}</header>
@if(error()){<div class="error" role="alert" mapanAlertFocus>{{error()}} <button class="secondary" (click)="load()">Reintentar</button></div>}@if(success()){<p class="success" role="status">{{success()}}</p>}@if(loading()){<p role="status" class="loading-block">Cargando modelos…</p>}
@if(modelForm();as m){<section class="panel"><h2>{{m['modeloId']?'Editar modelo':'Nuevo modelo'}}</h2><form (ngSubmit)="saveModel()" #mf="ngForm"><div class="grid-form"><label>Código<input name="code" [(ngModel)]="m['codigo']" maxlength="100" required></label><label>Nombre<input name="name" [(ngModel)]="m['nombre']" maxlength="200" required></label><label>Objetivo<input name="goal" [(ngModel)]="m['objetivo']" maxlength="200" required placeholder="Ej. Probabilidad de incumplimiento a 12 meses"></label><label>Descripción<textarea name="description" [(ngModel)]="m['descripcion']"></textarea></label></div><div class="actions"><button [disabled]="busy()||mf.invalid"><m-icon name="check"/>Guardar modelo</button><button type="button" class="secondary" (click)="modelForm.set(null)">Cancelar</button></div></form></section>}
<div class="policy-layout"><section class="panel"><h2>Modelos de la empresa</h2>@for(m of models();track m['modeloId']){<button class="select-row" [class.selected]="selected()?.['modelo']['modeloId']===m['modeloId']" (click)="select(m['modeloId'])"><span><strong>{{m['nombre']}}</strong><small>{{m['codigo']}} · {{m['estado']}}</small></span><m-icon name="arrow"/></button>}@empty{<div class="empty-state"><m-icon name="model"/><p>Aún no hay modelos registrados.</p></div>}</section>
<div>@if(selected();as s){<section class="panel"><div class="section-title"><h2>{{s['modelo']['nombre']}}</h2>@if(canManage()){<button class="secondary compact" (click)="modelForm.set({...s['modelo']})">Editar</button>}</div><p>{{s['modelo']['objetivo']}}</p>
@if(canManage()&&s['modelo']['estado']!=='ARCHIVADO'){<div class="actions">@if(s['modelo']['estado']!=='ACTIVO'){<button [disabled]="busy()" (click)="modelState('ACTIVO')">Activar</button>}@if(s['modelo']['estado']!=='INACTIVO'){<button class="secondary" [disabled]="busy()" (click)="modelState('INACTIVO')">Inactivar</button>}<button class="secondary" [disabled]="busy()" (click)="modelState('ARCHIVADO')">Archivar</button></div>}
@if(prediccionActiva(s);as activa){<p class="success" role="status"><m-icon name="check"/>Configurado: cada análisis calculará una predicción real usando {{activa['algoritmo']==='PENDIENTE_ENTRENAMIENTO'?'el respaldo temporal con IA (Claude), mientras no subas tu propio modelo entrenado':'la versión '+activa['numeroVersion']}}.</p>}
@else{<p class="error" role="alert"><m-icon name="alert"/>No configurado todavía: el modelo debe estar ACTIVO y tener una versión en PRODUCCIÓN para que el análisis calcule una predicción. Sin eso, el análisis sigue funcionando solo con las reglas de la política.</p>}
<details class="inline-note-details"><summary><m-icon name="model"/>¿Qué es una "versión" y para qué sirve?</summary><p>Cada vez que entrenas el modelo de nuevo con datos más recientes, registras esa nueva versión aquí — así conservas el historial y puedes comparar métricas (ROC AUC, precisión, etc.) entre una versión y otra. Solo <strong>una versión en estado PRODUCCIÓN</strong> genera predicciones reales; las demás quedan como borrador, en validación, en pruebas paralelas (SHADOW) o retiradas, sin afectar los análisis en curso. Si todavía no tienes un modelo propio entrenado, MAPAN usa un respaldo temporal con IA para que puedas probar el flujo completo mientras acumulas datos.</p></details>
<div class="version-list">@for(v of s['versiones'];track v['modeloVersionId']){<button class="select-row" [class.selected]="version()?.['modeloVersionId']===v['modeloVersionId']" (click)="selectVersion(v)"><strong>Versión {{v['numeroVersion']}} · {{v['algoritmo']==='PENDIENTE_ENTRENAMIENTO'?'Respaldo temporal (IA)':v['algoritmo']}}</strong><span>{{v['fechaCreacion'] | date:'dd/MM/yyyy'}}</span><span class="badge" [attr.data-state]="v['estado']">{{v['estado']}}</span></button>}@empty{<p>Todavía no hay versiones registradas para este modelo.</p>}</div>
@if(canManage()){<button class="secondary" (click)="newVersion()"><m-icon name="plus"/>Registrar versión entrenada</button>}
@if(version();as v){<div class="editor-surface"><div class="section-title"><h3>Versión {{v['numeroVersion']}}</h3><span class="badge" [attr.data-state]="v['estado']">{{v['estado']}}</span></div>
@if(canManage()&&v['modeloVersionId']&&transitions(v['estado']).length){<div class="actions">@for(next of transitions(v['estado']);track next){<button [class.secondary]="next==='RETIRADO'" [disabled]="busy()" (click)="versionState(next)">{{label(next)}}</button>}</div>}
@if(canManage()&&isEditableVersion(v)){<form (ngSubmit)="saveVersion()" #vf="ngForm"><div class="grid-form">
<label>Algoritmo<input name="algoritmo" [(ngModel)]="v['algoritmo']" maxlength="100" required></label>
<label>Descripción<input name="descripcion" [(ngModel)]="v['descripcion']"></label>
<label>Ubicación del artefacto (artefacto_uri)<input name="artefactoUri" [(ngModel)]="v['artefactoUri']" placeholder="Ruta o URI accesible por services/ml-service"></label>
<label>Orden de variables (esquema_caracteristicas, JSON)<textarea name="esquema" [(ngModel)]="esquemaInput" required rows="3" placeholder='["ingreso_mensual","gastos_mensuales", ...]'></textarea></label>
<label>Hiperparámetros (JSON, opcional)<textarea name="hiperparametros" [(ngModel)]="hiperparametrosInput" rows="2"></textarea></label>
<label>Umbral de decisión (0 a 1)<input type="number" min="0" max="1" step="any" name="umbral" [(ngModel)]="v['umbralDecision']"></label>
<label>ROC AUC<input type="number" min="0" max="1" step="any" name="roc" [(ngModel)]="v['rocAuc']"></label>
<label>Precisión<input type="number" min="0" max="1" step="any" name="precision" [(ngModel)]="v['precisionScore']"></label>
<label>Recall<input type="number" min="0" max="1" step="any" name="recall" [(ngModel)]="v['recallScore']"></label>
<label>F1<input type="number" min="0" max="1" step="any" name="f1" [(ngModel)]="v['f1Score']"></label>
<label>Exactitud<input type="number" min="0" max="1" step="any" name="accuracy" [(ngModel)]="v['accuracyScore']"></label>
<label>Datos desde<input type="date" name="desde" [(ngModel)]="v['fechaDatosDesde']"></label>
<label>Datos hasta<input type="date" name="hasta" [(ngModel)]="v['fechaDatosHasta']"></label>
<label>Cantidad de registros<input type="number" min="0" name="registros" [(ngModel)]="v['cantidadRegistros']"></label>
<label>Positivos<input type="number" min="0" name="positivos" [(ngModel)]="v['cantidadPositivos']"></label>
<label>Negativos<input type="number" min="0" name="negativos" [(ngModel)]="v['cantidadNegativos']"></label>
</div><div class="actions"><button [disabled]="busy()||vf.invalid"><m-icon name="check"/>Guardar versión</button></div></form>}
@if(!isEditableVersion(v)){<p class="inline-note"><m-icon name="shield"/>Esta versión ya está en uso o retirada; sus datos quedan fijos. Registra una versión nueva para reemplazarla.</p>}
</div>}
</section>}</div></div>
`})
export class ModelsPage {
 auth=inject(AuthService);private api=inject(ApiClient);private dialog=inject(DialogService);models=signal<Row[]>([]);selected=signal<Row|null>(null);version=signal<Row|null>(null);modelForm=signal<Row|null>(null);error=signal('');success=signal('');loading=signal(false);busy=signal(false);esquemaInput='';hiperparametrosInput='';
 constructor(){void this.load();}
 canManage(){return this.auth.can('Modelos:Manage');}
 transitions(state:string){return TRANSITIONS[state]||[];}
 label(state:string){return LABELS[state]||state;}
 isEditableVersion(v:Row){return v['estado']==='BORRADOR'||v['estado']==='ENTRENADO'||v['estado']==='VALIDADO';}
 prediccionActiva(s:Row):Row|null{if(s['modelo']['estado']!=='ACTIVO')return null;return (s['versiones']as Row[]).find(v=>v['estado']==='PRODUCCION')??null;}
 async load(){this.loading.set(true);try{this.models.set(await firstValueFrom(this.api.get<Row[]>('/modelos')));}catch(e){this.error.set(errorMessage(e));}finally{this.loading.set(false);}}
 async run(action:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');this.success.set('');try{await action();this.success.set('Cambios guardados.');}catch(e){this.error.set(errorMessage(e));}finally{this.busy.set(false);}}
 async select(id:string){await this.run(async()=>{this.selected.set(await firstValueFrom(this.api.get<Row>('/modelos/'+id)));this.version.set(null);});}
 async refresh(){const id=this.selected()!['modelo']['modeloId'];this.selected.set(await firstValueFrom(this.api.get<Row>('/modelos/'+id)));}
 async saveModel(){await this.run(async()=>{const m=this.modelForm()!;await firstValueFrom(m['modeloId']?this.api.put('/modelos/'+m['modeloId'],m):this.api.post('/modelos',m));this.modelForm.set(null);await this.load();if(m['modeloId'])await this.refresh();});}
 async modelState(state:string){if(!await this.dialog.confirm('¿Confirmas cambiar este modelo a '+state+'?',{title:'Cambiar estado del modelo'}))return;await this.run(async()=>{await firstValueFrom(this.api.postRaw('/modelos/'+this.selected()!['modelo']['modeloId']+'/estado',state));await this.refresh();await this.load();});}
 selectVersion(v:Row){this.version.set({...v});this.esquemaInput=v['esquemaCaracteristicas']?JSON.stringify(JSON.parse(v['esquemaCaracteristicas']),null,0):'';this.hiperparametrosInput=v['hiperparametros']?JSON.stringify(JSON.parse(v['hiperparametros']),null,0):'';}
 newVersion(){this.version.set({modeloVersionId:null,numeroVersion:'—',estado:'BORRADOR',algoritmo:''});this.esquemaInput='';this.hiperparametrosInput='';}
 async saveVersion(){await this.run(async()=>{
  let esquema:unknown;try{esquema=JSON.parse(this.esquemaInput);}catch{throw {error:{title:'El orden de variables debe ser JSON válido, ej. ["ingreso_mensual","monto_solicitado"].'}};}
  if(!Array.isArray(esquema)||esquema.length===0)throw {error:{title:'El orden de variables debe ser un arreglo no vacío.'}};
  let hiperparametros:string|null=null;if(this.hiperparametrosInput.trim()){try{JSON.parse(this.hiperparametrosInput);}catch{throw {error:{title:'Los hiperparámetros deben ser JSON válido.'}};}hiperparametros=this.hiperparametrosInput;}
  const v=this.version()!;const payload={...v,esquemaCaracteristicas:JSON.stringify(esquema),hiperparametros};
  const id=this.selected()!['modelo']['modeloId'];
  if(v['modeloVersionId'])await firstValueFrom(this.api.put('/modelos/versiones/'+v['modeloVersionId'],payload));
  else await firstValueFrom(this.api.post('/modelos/'+id+'/versiones',payload));
  await this.refresh();this.version.set(null);
 });}
 async versionState(state:string){if(!await this.dialog.confirm('¿Confirmas cambiar esta versión a '+state+'?',{title:'Cambiar estado de la versión'}))return;await this.run(async()=>{await firstValueFrom(this.api.postRaw('/modelos/versiones/'+this.version()!['modeloVersionId']+'/estado',state));await this.refresh();const updated=this.selected()!['versiones'].find((x:Row)=>x['modeloVersionId']===this.version()!['modeloVersionId']);this.version.set(updated?{...updated}:null);});}
}
