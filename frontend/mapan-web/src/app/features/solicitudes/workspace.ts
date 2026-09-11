import {Component,inject,signal} from '@angular/core';
import {DatePipe,DecimalPipe} from '@angular/common';
import {DomSanitizer,SafeResourceUrl} from '@angular/platform-browser';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute,Router,RouterLink} from '@angular/router';
import {firstValueFrom} from 'rxjs';
import {ApiClient,errorMessage} from '../../core/api';
import {AuthService} from '../../core/auth';
import {DialogService, Modal} from '../../shared/dialog';
import {Icon} from '../../shared/icon';
import {ClientPager} from '../../shared/pager';
import {DOCUMENT_TYPE_SUGGESTIONS} from '../../shared/document-types';
import {Combobox} from '../../shared/combobox';
import {LoadingOverlayService} from '../../shared/loading-overlay';
import {AlertFocus} from '../../shared/alert-focus';
type Row=Record<string,any>;
const PREVIEWABLE_MIME_TYPES=['image/png','image/jpeg'];
// Marca los campos que no se pudieron detectar automáticamente — el analista es quien revisa y corrige
// después (botón "Corregir"), no quien tiene que confirmar antes de que el dato cuente. El aviso al
// hacer clic en "Continuar" busca exactamente este texto para saber qué quedó sin revisar.
const ECON_DEFAULT='Sin especificar';
interface EconField{key:string;label:string;type?:string;required?:boolean;suggest?:string|string[];}
@Component({selector:'mapan-expediente',imports:[FormsModule,RouterLink,Icon,Modal,Combobox,DatePipe,DecimalPipe,AlertFocus],template:`
<header class="page-heading"><div><span class="eyebrow">EXPEDIENTE DE CRÉDITO</span><h1>{{request()['numeroSolicitud'] || 'Solicitud'}}</h1><p>Completa y revisa la información que respalda esta operación.</p></div><a routerLink="/solicitudes">Volver a solicitudes</a></header>
@if(error()){<p class="error" role="alert" mapanAlertFocus>{{error()}}</p><button class="secondary" (click)="load()">Reintentar</button>}
@if(success()){<p class="success" role="status">{{success()}}</p>}
@if(loading()){<p role="status">Cargando expediente…</p>}
@if(!loading() && request()['solicitudCreditoId']) {
<nav class="steps" aria-label="Pasos del expediente">@for(label of steps;track $index){<button class="secondary" [attr.aria-current]="step()===$index?'step':null" (click)="step.set($index)">{{$index+1}}. {{label}}</button>}</nav>
@switch(step()) {
@case(0){<section class="panel"><div class="section-title"><span class="icon-tile"><m-icon name="clients"/></span><h2>Cliente y operación</h2><span class="badge" [attr.data-state]="request()['estado']">{{request()['estado']}}</span></div>
<div class="data-card-grid">
<div class="data-card"><div class="verify-card-top"><span class="icon-tile"><m-icon name="clients"/></span><strong>Cliente</strong></div><dl class="data-card-fields"><div><dt>Nombre / razón social</dt><dd>{{client()['razonSocial'] || (client()['nombres'] || '')+' '+(client()['apellidos'] || '')}}</dd></div><div><dt>Identificación</dt><dd>{{client()['numeroIdentificacion'] || 'No disponible con tus permisos'}}</dd></div></dl></div>
<div class="data-card"><div class="verify-card-top"><span class="icon-tile"><m-icon name="folder"/></span><strong>Producto</strong></div><dl class="data-card-fields"><div><dt>Producto</dt><dd>{{product()['nombre'] || 'No disponible con tus permisos'}}</dd></div><div><dt>Destino del crédito</dt><dd>{{request()['destinoCredito'] || 'Sin registrar'}}</dd></div></dl></div>
<div class="data-card"><div class="verify-card-top"><span class="icon-tile"><m-icon name="chart"/></span><strong>Operación</strong></div><dl class="data-card-fields"><div><dt>Monto solicitado</dt><dd>{{request()['montoSolicitado']}}</dd></div><div><dt>Plazo</dt><dd>{{request()['plazoSolicitadoMeses']}} meses</dd></div><div><dt>Cuota confirmada</dt><dd>{{request()['cuotaEstimada'] ?? 'Pendiente'}}</dd></div></dl></div>
</div>
<a class="text-action" routerLink="/solicitudes"><m-icon name="back"/>Editar datos del borrador</a></section>}
@case(1){<section class="panel"><h2>Investigación del cliente</h2><p>Consulta automática de fuentes externas (buró de crédito, procesos judiciales, aval/garante) antes de solicitar documentos financieros. Ningún dato se guarda sin dejar constancia de la consulta que lo originó. Si el cliente ya trae un reporte impreso (Equifax, buró, aval), también puedes cargarlo como documento en el paso "Expediente" — MAPAN intenta leerlo automáticamente y el resultado aparece aquí igual que una consulta automática.</p>
@if(auth.can('Investigacion:Execute')){<button [disabled]="investigando()" (click)="ejecutarInvestigacion()">{{investigando()?'Consultando fuentes…':(investigacion()?'Repetir investigación':'Ejecutar investigación')}}</button>}
@if(investigando()){<p role="status" class="loading-block">Consultando buró de crédito, procesos judiciales y aval…</p>}
@if(!investigando() && investigacion();as inv){
<p class="hint">Última consulta: {{inv['ultimaEjecucion']|date:'medium'}}</p>
<div class="stat-grid">
<div class="stat-card"><span>Buró de crédito</span>@if(inv['buro'];as b){<strong>{{b['score'] ?? 'Sin historial'}}</strong><span class="badge" [attr.data-state]="b['bandaRiesgo']">{{b['bandaRiesgo'] ?? 'SIN DATOS'}}</span><p>Deuda total {{b['deudaTotal']|number:'1.2-2'}} · {{b['operacionesVencidas']}} operación(es) vencida(s) · Demanda judicial {{b['montoDemandaJudicial']|number:'1.2-2'}} · Cartera castigada {{b['montoCarteraCastigada']|number:'1.2-2'}}</p>}@else{<p>No configurado para esta cooperativa.</p>}</div>
<div class="stat-card"><span>Procesos judiciales</span>@if(inv['judicial'];as j){<strong class="badge" [attr.data-state]="j['tieneProcesos']?'ALTO':'BAJO'">{{j['tieneProcesos']?j['numeroProcesos']+' proceso(s)':'Sin procesos'}}</strong>@if(j['tieneProcesos']){<p>Materia principal: {{j['materiaPrincipal']}} · Gravedad máxima: {{j['gravedadMaxima']}}</p>}}@else{<p>No configurado para esta cooperativa.</p>}</div>
<div class="stat-card"><span>Aval / garante</span>@if(inv['aval'];as a){<strong class="badge" [attr.data-state]="a['tieneMoraComoGarante']?'ALTO':(a['esGaranteActivo']?'MEDIO':'BAJO')">{{a['esGaranteActivo']?'Garante de '+a['operacionesComoGarante']+' operación(es)':'Sin novedades'}}</strong>@if(a['esGaranteActivo']){<p>{{a['tieneMoraComoGarante']?'Con mora en operación garantizada':'Operación(es) al día'}}</p>}}@else{<p>No configurado para esta cooperativa.</p>}</div>
</div>
@for(c of inv['consultas'];track c['consultaExternaId']){@if(c['estado']==='ERROR'){<p class="error" role="alert" mapanAlertFocus>{{c['tipoConsulta']}}: la fuente no respondió ({{c['mensajeError']}}). Vuelve a intentarlo más tarde.</p>}}
<h3>Consultas registradas</h3>
@for(c of inv['consultas'];track c['consultaExternaId']){<div class="list-row"><span>{{c['tipoConsulta']}} · {{c['origen']==='CARGA_DOCUMENTO'?'Reporte cargado por el analista':c['origen']==='CONSULTA_API'?'Consulta automática':'Origen sin registrar'}}</span><span class="badge" [attr.data-state]="c['estado']==='ERROR'?'ROJO':'VERDE'">{{c['fechaInicio']|date:'short'}}</span></div>}
<p class="hint">Esta pantalla reúne la información encontrada; la evaluación contra las políticas de la cooperativa se hace en el análisis de crédito.</p>
}
@if(!investigando() && !investigacion()){<p>Todavía no se ha investigado a este cliente.</p>}
</section>}
@case(2){<section class="panel"><h2>Preevaluación</h2><p>Evalúa lo encontrado en la investigación contra las políticas de la cooperativa, antes de pedir documentos financieros.</p>
@if(auth.can('Preevaluacion:Execute')){<button [disabled]="preevaluando()||!investigacion()" (click)="ejecutarPreevaluacion()">{{preevaluando()?'Evaluando…':(preevaluacion()?'Repetir preevaluación':'Ejecutar preevaluación')}}</button>}
@if(!investigacion()){<p class="hint">Primero ejecuta la investigación del cliente (paso anterior).</p>}
@if(preevaluando()){<p role="status" class="loading-block">Evaluando políticas de preevaluación…</p>}
@if(!preevaluando() && preevaluacion();as p){
<div class="case-banner"><div><span class="eyebrow">Resultado preliminar</span><h2>{{p['resultadoPreliminar']==='APTO'?'APTO para continuar':p['resultadoPreliminar']==='REQUIERE_EXCEPCION'?'Requiere excepción':'No apto'}}</h2></div><div><span class="badge" [attr.data-state]="p['resultadoPreliminar']==='APTO'?'VERDE':p['resultadoPreliminar']==='REQUIERE_EXCEPCION'?'AMARILLO':'ROJO'">{{p['resultadoPreliminar']}}</span><small>{{p['fechaEjecucion']|date:'medium'}}</small></div></div>
@for(r of p['reglas'];track r['codigo']){@if(r['aplica']){<div class="alert-row" [attr.data-level]="r['severidad']"><div><span class="badge" [attr.data-state]="r['severidad']">{{r['accion']}}</span><h3>{{r['nombre']}}</h3><p>{{r['descripcion']}}</p></div></div>}}
@if(!aplicaAlguna(p)){<p class="hint">Ninguna política de preevaluación se activó con esta información.</p>}
@if(p['resultadoPreliminar']!=='APTO'){
<h3>Excepciones</h3>
@if(auth.can('Excepciones:Solicitar')){@if(!excepcionForm()){<button class="secondary" (click)="excepcionForm.set({motivoJustificacion:'',observacion:''})">Solicitar excepción</button>}
@if(excepcionForm();as f){<form (ngSubmit)="solicitarExcepcion()" #ef="ngForm" class="editor-surface"><label>Motivo (obligatorio)<textarea name="motivo" [(ngModel)]="f['motivoJustificacion']" required rows="2" placeholder="¿Por qué se justifica continuar a pesar de lo detectado?"></textarea></label><label>Observación<textarea name="obs" [(ngModel)]="f['observacion']" rows="2"></textarea></label><div class="actions"><button [disabled]="busy()||ef.invalid">Enviar solicitud</button><button type="button" class="secondary" (click)="excepcionForm.set(null)">Cancelar</button></div></form>}}
@for(e of excepciones();track e['excepcionId']){<div class="list-row"><div><strong>{{e['motivoJustificacion']}}</strong><p>{{e['observacion']}}</p><small>Solicitada por {{e['solicitadaPorNombre']}} · {{e['fechaSolicitud']|date:'short'}}@if(e['resueltaPorNombre']){ · Resuelta por {{e['resueltaPorNombre']}}}</small></div><div><span class="badge" [attr.data-state]="e['estado']">{{e['estado']}}</span>
@if(e['estado']==='SOLICITADA'&&auth.can('Excepciones:Aprobar')){<button class="secondary compact" (click)="resolverExcepcion(e,true)"><m-icon name="check"/>Aprobar</button><button class="ghost compact danger-text" (click)="resolverExcepcion(e,false)"><m-icon name="x"/>Rechazar</button>}</div></div>}@empty{<p>Sin excepciones solicitadas todavía.</p>}
}}
</section>}
@case(3){<section class="panel"><h2>Expediente documental</h2><p>Adjunta PDF, PNG o JPEG. Para cualquier PDF con texto (no escaneado ni foto), MAPAN intenta extraer automáticamente los campos que reconoce, sin importar cómo llames al tipo de documento; siempre debes confirmarlos en "Validación documental" antes de que cuenten. Un PDF escaneado o una foto (PNG/JPEG) todavía se valida manualmente — el reconocimiento de imágenes aún no está configurado.</p>
@if(auth.can('Documentos:Upload') && editableDocuments()) {
<label for="doc-type">Tipo de documento</label><mapan-combobox inputId="doc-type" [options]="documentTypeSuggestions" placeholder="Escribe o elige un tipo de documento" [value]="documentType" (valueChange)="documentType=$event"/>
<div class="dropzone" (dragover)="$event.preventDefault()" (drop)="drop($event)"><label for="files">Arrastra archivos aquí o selecciónalos</label><input id="files" type="file" multiple accept="application/pdf,image/png,image/jpeg" [disabled]="busy() || !documentType.trim()" (change)="files($event)"></div>
@if(busy()){<progress [value]="completed()" [max]="uploadTotal()"></progress><p role="status">{{completed()}} de {{uploadTotal()}} archivos procesados</p>}}
@if(!auth.can('Documentos:Read')){<p>No tienes permiso de consulta documental.</p>}
@for(doc of documentPager.items();track doc['solicitudDocumentoId']){<div class="list-row" [class.voided]="doc['documentoEstado']==='ANULADO'">
@if(previewUrls()[doc['documentoVersionId']];as url){<img class="doc-thumb" [src]="url" [alt]="doc['nombre']" (click)="openPreview(url)">}
<div><strong>{{doc['nombre']}}</strong><p>{{doc['tipoDocumento']}} · Versión {{doc['numeroVersion']}} · {{doc['estado']}}@if(doc['documentoEstado']==='ANULADO'){ · <span class="badge" data-state="ERROR">Eliminado</span>}</p></div><div>
@if(doc['documentoEstado']!=='ANULADO'){<button class="secondary" (click)="download(doc)">Descargar</button>
@if(auth.can('Documentos:Upload') && editableDocuments()){<label>Subir nueva versión<input type="file" accept="application/pdf,image/png,image/jpeg" [disabled]="busy()" (change)="replace($event,doc)"></label>}
@if(auth.can('Documentos:Delete') && editableDocuments()){<button class="secondary danger" (click)="voidDocument(doc)">Eliminar</button>}}
</div></div>}@empty{<p>No hay documentos adjuntos.</p>}
@if(documentPager.totalPages()>1){<div class="pagination"><button class="secondary compact" [disabled]="documentPager.page()===1" (click)="documentPager.prev()">Anterior</button><span>Página {{documentPager.page()}} de {{documentPager.totalPages()}} · {{documentPager.total()}} documentos</span><button class="secondary compact" [disabled]="documentPager.page()===documentPager.totalPages()" (click)="documentPager.next()">Siguiente</button></div>}
</section>}
@case(4){<section class="panel"><h2>Verificación</h2><p>Confirma la autenticidad de los documentos cargados y, si aplica, la relación laboral del cliente ante el IESS. Esto es independiente de la revisión de contenido que se hace en "Validación documental" — aquí se evalúa si el archivo en sí es confiable.</p>
<div class="econ-group">
<div class="section-title"><span class="icon-tile"><m-icon name="shield"/></span><h3>Documentos</h3><span class="count">{{verificacionDocs().length}}</span></div>
<div class="data-card-grid">
@for(doc of verificacionDocs();track doc['solicitudDocumentoId']){<div class="data-card">
  <div class="verify-card-top"><span class="verify-icon" [attr.data-state]="doc['estadoVerificacion']"><m-icon [name]="doc['estadoVerificacion']==='VERIFICADO'?'check':doc['estadoVerificacion']==='PENDIENTE'?'search':'alert'"/></span><strong>{{doc['nombre']}}</strong></div>
  <dl class="data-card-fields"><div><dt>Tipo</dt><dd>{{doc['tipoDocumento']}}</dd></div>@if(doc['confianzaPct']!=null){<div><dt>Confianza</dt><dd>{{doc['confianzaPct']}}%</dd></div>}</dl>
  @if(doc['motivos']?.length){<p class="hint"><m-icon name="alert"/>{{doc['motivos'].join(' · ')}}</p>}
  <div class="data-card-head"><span class="badge" [attr.data-state]="doc['estadoVerificacion']">{{doc['estadoVerificacion']}}</span>
  @if(auth.can('Verificacion:Execute')){<button class="secondary compact" [disabled]="verificando()===doc['solicitudDocumentoId']" (click)="verificarDocumento(doc)">{{verificando()===doc['solicitudDocumentoId']?'Verificando…':(doc['estadoVerificacion']==='PENDIENTE'?'Verificar':'Repetir')}}</button>}</div>
</div>}
@empty{<div class="empty-state compact"><m-icon name="folder"/><p>No hay documentos cargados todavía. Súbelos en el paso "Expediente".</p></div>}
</div>
</div>
<div class="econ-group">
<div class="section-title"><span class="icon-tile"><m-icon name="audit"/></span><h3>Verificación laboral (IESS)</h3></div>
<p class="hint">Sin API oficial de consulta disponible en este entorno; el resultado que se muestra es simulado hasta que la cooperativa configure un proveedor real.</p>
@if(auth.can('Verificacion:Execute')){<button [disabled]="laboralBusy()" (click)="ejecutarLaboral()"><m-icon name="search"/>{{laboralBusy()?'Consultando…':(laboral()?'Repetir consulta':'Consultar IESS')}}</button>}
@if(laboral();as l){<div class="stat-grid"><div class="stat-card" [class.warn]="l['estado']==='SIN_REGISTRO'||l['estado']==='CESANTE'"><span>Relación laboral</span><strong class="badge" [attr.data-state]="l['estado']">{{l['estado']}}</strong><p>{{l['empleadorRegistrado'] || 'Sin empleador registrado'}}@if(l['fechaAfiliacion']){ · Afiliado desde {{l['fechaAfiliacion']|date:'mediumDate'}}}@if(l['aporteMensual']!=null){ · Aporte {{l['aporteMensual']|number:'1.2-2'}}}</p><small class="hint">Última consulta: {{l['ultimaEjecucion']|date:'medium'}}</small></div></div>}
</div>
</section>}
@case(5){<section class="panel"><h2>Datos económicos</h2><p>Registra meses calendario completos (del primer al último día). No se convierten monedas ni se estiman ingresos. Lo que MAPAN reconoce en los documentos se guarda aquí automáticamente — revisa las tarjetas y usa "Corregir" si algo no quedó bien; no hace falta confirmar cada dato antes de que cuente.</p>
<div class="econ-group">
<div class="section-title"><span class="icon-tile"><m-icon name="clients"/></span><h3>Actividad económica y fuente de ingreso</h3><span class="count">{{rows('actividades').length}}</span></div>
<p class="hint">De dónde vive el cliente y con qué se paga esta operación, en un solo lugar.</p>
<div class="data-card-grid">
@for(actividad of rows('actividades');track actividad['actividadEconomicaId']){<div class="data-card">
  <div class="data-card-head"><strong>{{actividad['tipoActividad'] || 'Actividad económica'}}</strong>@if(auth.can("Solicitudes:Update") && request()["estado"]==="BORRADOR"){<button class="ghost compact" (click)="editEconomicCombined(actividad);openSection.set('combinado')"><m-icon name="settings"/>Corregir</button>}</div>
  <dl class="data-card-fields">@for(f of fieldsOf('actividades',actividad);track f.label){<div><dt>{{f.label}}</dt><dd>{{f.value}}</dd></div>}</dl>
  @for(fuente of fuentesDe(actividad['actividadEconomicaId']);track fuente['fuenteIngresoId']){<div class="data-card-nested"><span class="eyebrow">FUENTE DE INGRESO</span><dl class="data-card-fields">@for(f of fieldsOf('fuentes',fuente);track f.label){<div><dt>{{f.label}}</dt><dd>{{f.value}}</dd></div>}</dl></div>}
</div>}
@empty{
  @if(draftEconomicFields().length){<div class="data-card draft-card">
    <div class="data-card-head"><span class="badge" data-state="AMARILLO"><m-icon name="model"/>{{economicDraftSaving()?'Guardando automáticamente…':'Detectado — pendiente de guardar'}}</span></div>
    <dl class="data-card-fields">@for(f of draftEconomicFields();track f.label){<div><dt>{{f.label}}</dt><dd>{{f.value}}</dd></div>}</dl>
    <p class="hint"><m-icon name="model"/>Desde "{{draftEconomicFields()[0].documento}}". Se guarda solo — después usa "Corregir" en la tarjeta si algo no quedó bien.</p>
  </div>}
  @else{<div class="empty-state compact"><m-icon name="clients"/><p>Aún no se ha registrado la actividad económica de este cliente.</p></div>}
}
</div>
@if(auth.can('Solicitudes:Update') && request()['estado']==='BORRADOR') {<button class="secondary" (click)="cancelEconomicCombined();openSection.set('combinado')"><m-icon name="plus"/>Agregar actividad y fuente de ingreso</button>}
</div>
@if(openSection()==='combinado'){<mapan-modal [title]="(editingActividadId ? 'Corregir' : 'Agregar')+' actividad y fuente de ingreso'" (close)="cancelEconomicCombined();openSection.set(null)">
<form (ngSubmit)="saveEconomicCombined()" #fc="ngForm">
<div class="grid-form">
@for(field of actividadFields;track field.key){<label>{{field.label}}<input [type]="field.type || 'text'" [name]="'a_'+field.key" [(ngModel)]="actividadModel[field.key]" [required]="field.required || false" step="any">
@if(suggestionFor(field.suggest);as sug){<small class="hint"><m-icon name="model"/>Prellenado automáticamente desde "{{sug.documento}}". Verifica el valor antes de guardar.</small>}</label>}
<label class="check-row"><input type="checkbox" name="principal" [(ngModel)]="actividadModel['esPrincipal']">Actividad principal confirmada</label>
@for(field of fuenteFields;track field.key){<label>{{field.label}}<input [type]="field.type || 'text'" [name]="'f_'+field.key" [(ngModel)]="fuenteModel[field.key]" [required]="field.required || false" step="any">
@if(suggestionFor(field.suggest);as sug){<small class="hint"><m-icon name="model"/>Prellenado automáticamente desde "{{sug.documento}}". Verifica el valor antes de guardar.</small>}</label>}
<label class="check-row"><input type="checkbox" name="recurrente" [(ngModel)]="fuenteModel['esRecurrente']">Ingreso recurrente</label>
</div><div class="actions"><button [disabled]="busy() || fc.invalid">{{editingActividadId ? 'Guardar corrección' : 'Agregar actividad y fuente'}}</button><button type="button" class="secondary" (click)="cancelEconomicCombined();openSection.set(null)">Cancelar</button></div></form>
</mapan-modal>}
@for(section of sections;track section.key){<div class="econ-group">
<div class="section-title"><span class="icon-tile"><m-icon [name]="section.icon"/></span><h3>{{section.title}}</h3><span class="count">{{rows(section.key).length}}</span></div>
<div class="data-card-grid">
@for(row of rows(section.key);track rowId(row)){<div class="data-card">
  <div class="data-card-head">@if(auth.can("Solicitudes:Update") && request()["estado"]==="BORRADOR"){<button class="ghost compact" (click)="editEconomic(section.key,row);openSection.set(section.key)"><m-icon name="settings"/>Corregir</button><button class="ghost compact danger-text" type="button" title="Eliminar registro" (click)="deleteEconomic(section.key,row)"><m-icon name="trash"/></button>}</div>
  <dl class="data-card-fields">@for(f of fieldsOf(section.key,row);track f.label){<div><dt>{{f.label}}</dt><dd>{{f.value}}</dd></div>}</dl>
</div>}
@empty{<div class="empty-state compact"><m-icon [name]="section.icon"/><p>Sin registros todavía.</p></div>}
</div>
@if(auth.can('Solicitudes:Update') && request()['estado']==='BORRADOR') {<button class="secondary" (click)="startEconomic(section.key)"><m-icon name="plus"/>Agregar {{section.singular}}</button>}
@if(openSection()===section.key){<mapan-modal [title]="(editingIds[section.key] ? 'Corregir ' : 'Agregar ')+section.singular" (close)="cancelEconomic(section.key)">
<form (ngSubmit)="saveEconomic(section)" #f="ngForm"><div class="grid-form">
@for(field of section.fields;track field.key){<label>{{field.label}}<input [type]="field.type || 'text'" [name]="field.key" [(ngModel)]="models[section.key][field.key]" [required]="field.required || false" [attr.min]="field.type==='number'?0:null" step="any">
@if(suggestionFor(field.suggest);as sug){<small class="hint"><m-icon name="model"/>Prellenado automáticamente desde "{{sug.documento}}". Verifica el valor antes de guardar.</small>}</label>}
@if(section.key==='periodos'){<label>Fuente de ingreso<select name="fuente" [(ngModel)]="sourceId" required [disabled]="!!editingIds[section.key]"><option value="">Selecciona una fuente</option>@for(source of rows('fuentes');track source['fuenteIngresoId']){<option [value]="source['fuenteIngresoId']">{{source['descripcion'] || source['tipoIngreso']}}</option>}</select></label>}
@if(section.key==="actividades"){<label class="check-row"><input type="checkbox" name="principal" [(ngModel)]="models[section.key]['esPrincipal']">Actividad principal confirmada</label>}@if(section.key==="fuentes"){<label class="check-row"><input type="checkbox" name="recurrente" [(ngModel)]="models[section.key]['esRecurrente']">Ingreso recurrente</label>}@if(section.key==="obligaciones"){<label class="check-row"><input type="checkbox" name="garante" [(ngModel)]="models[section.key]['esGarante']">El cliente es garante</label>}</div><div class="actions"><button [disabled]="busy() || f.invalid">{{editingIds[section.key] ? "Guardar corrección" : "Agregar " + section.singular}}</button><button type="button" class="secondary" (click)="cancelEconomic(section.key)">Cancelar</button></div></form>
</mapan-modal>}
</div>}
</section>}
@case(6){<section class="panel"><div class="section-title"><span class="icon-tile"><m-icon name="loan"/></span><h2>Visita de negocio</h2></div><p>Verificación en campo del negocio del cliente — típicamente aplica a microcrédito y comercial.
@if(product()['categoria']==='MICROCREDITO'||product()['categoria']==='COMERCIAL'){<span class="badge" data-state="AMARILLO">Aplica para este producto ({{product()['categoria']}})</span>}
@else if(product()['categoria']){<span class="badge" data-state="SIN DATOS">No suele exigirse para {{product()['categoria']}}, pero puedes registrarla igual</span>}
La evidencia fotográfica se adjunta como documento en el paso "Expediente" (tipo "Foto de visita de negocio").</p>
@if(auth.can('VisitaNegocio:Create')){@if(!visitaForm()){<button (click)="startVisita()"><m-icon name="plus"/>Registrar nueva visita</button>}
@if(visitaForm();as f){<form (ngSubmit)="guardarVisita()" #vf="ngForm" class="editor-surface">
<div class="grid-form">
<label>Fecha de visita<input type="date" name="fechaVisita" [(ngModel)]="f['fechaVisita']" required></label>
<label>Dirección observada<input name="direccion" [(ngModel)]="f['direccionObservada']"></label>
<label class="check-row"><input type="checkbox" name="negocioExiste" [(ngModel)]="f['negocioExiste']">El negocio existe y fue verificado</label>
<label>Tipo de negocio observado<input name="tipoNegocio" [(ngModel)]="f['tipoNegocioObservado']"></label>
<label>Tiempo de funcionamiento observado<input name="tiempoFuncionamiento" [(ngModel)]="f['tiempoFuncionamientoObservado']" placeholder="Ej. 2 años"></label>
<label>Empleados observados<input type="number" name="empleados" [(ngModel)]="f['numeroEmpleadosObservado']" min="0"></label>
<label>Inventario/activos estimados<input type="number" name="inventario" [(ngModel)]="f['inventarioEstimado']" min="0" step="any"></label>
<label>Ingreso mensual estimado (observado)<input type="number" name="ingresoObs" [(ngModel)]="f['ingresoMensualEstimadoObservado']" min="0" step="any"></label>
<label>Recomendación<select name="recomendacion" [(ngModel)]="f['recomendacion']" required><option value="FAVORABLE">Favorable</option><option value="FAVORABLE_CON_OBSERVACIONES">Favorable con observaciones</option><option value="DESFAVORABLE">Desfavorable</option></select></label>
</div>
<label>Observaciones<textarea name="observaciones" [(ngModel)]="f['observaciones']" rows="3"></textarea></label>
<h3>Ubicación</h3><p class="hint">Captura la ubicación con el GPS del dispositivo, o ingresa las coordenadas manualmente si las conoces (por ejemplo, tomadas de una foto o de un mapa).</p>
<div class="grid-form">
<label>Latitud<input type="number" name="latitud" [(ngModel)]="f['latitud']" step="any" placeholder="Ej. -0.180653"></label>
<label>Longitud<input type="number" name="longitud" [(ngModel)]="f['longitud']" step="any" placeholder="Ej. -78.467834"></label>
</div>
<button type="button" class="secondary compact" (click)="capturarUbicacion()"><m-icon name="search"/>{{f['latitud']!=null?'Capturar de nuevo con GPS':'Capturar ubicación con GPS'}}</button>
@if(f['latitud']!=null && f['longitud']!=null){<iframe class="map-embed" [src]="mapsEmbedUrl(f['latitud'],f['longitud'])" loading="lazy" referrerpolicy="no-referrer-when-downgrade" title="Ubicación de la visita"></iframe>}
<div class="actions"><button [disabled]="busy() || vf.invalid">Guardar visita</button><button type="button" class="secondary" (click)="visitaForm.set(null)">Cancelar</button></div>
</form>}}
<div class="section-title"><span class="icon-tile"><m-icon name="folder"/></span><h3>Visitas registradas</h3><span class="count">{{visitaPager.total()}}</span></div>
<div class="data-card-grid">
@for(v of visitaPager.items();track v['visitaNegocioId']){<div class="data-card">
  <div class="data-card-head"><strong>{{v['fechaVisita']|date:'mediumDate'}}</strong><span class="badge" [attr.data-state]="v['recomendacion']==='FAVORABLE'?'VERDE':v['recomendacion']==='DESFAVORABLE'?'ROJO':'AMARILLO'">{{v['recomendacion']}}</span></div>
  <dl class="data-card-fields">
    <div><dt>Negocio</dt><dd>{{v['negocioExiste']?'Verificado':'NO encontrado'}}</dd></div>
    <div><dt>Dirección</dt><dd>{{v['direccionObservada'] || 'Sin registrar'}}</dd></div>
    @if(v['tipoNegocioObservado']){<div><dt>Tipo observado</dt><dd>{{v['tipoNegocioObservado']}}</dd></div>}
    @if(v['ingresoMensualEstimadoObservado']!=null){<div><dt>Ingreso observado</dt><dd>{{v['ingresoMensualEstimadoObservado']|number:'1.2-2'}}</dd></div>}
  </dl>
  <p class="hint"><m-icon name="clients"/>{{v['realizadaPorNombre']}} · {{v['fechaCreacion']|date:'short'}}</p>
  @if(v['latitud']!=null && v['longitud']!=null){<iframe class="map-embed" [src]="mapsEmbedUrl(v['latitud'],v['longitud'])" loading="lazy" referrerpolicy="no-referrer-when-downgrade" title="Ubicación de la visita"></iframe>}
</div>}
@empty{<div class="empty-state compact"><m-icon name="loan"/><p>Todavía no se ha registrado ninguna visita.</p></div>}
</div>
@if(visitaPager.totalPages()>1){<div class="pagination"><button class="secondary compact" [disabled]="visitaPager.page()===1" (click)="visitaPager.prev()">Anterior</button><span>Página {{visitaPager.page()}} de {{visitaPager.totalPages()}} · {{visitaPager.total()}} visitas</span><button class="secondary compact" [disabled]="visitaPager.page()===visitaPager.totalPages()" (click)="visitaPager.next()">Siguiente</button></div>}
</section>}
@case(7){<section class="panel"><div class="section-title"><span class="icon-tile"><m-icon name="chart"/></span><h2>Resumen antes del análisis</h2></div>
<div class="stat-grid">
<div class="stat-card"><span>Fuentes de ingreso</span><strong>{{rows('fuentes').length}}</strong></div>
<div class="stat-card"><span>Períodos registrados</span><strong>{{rows('periodos').length}}</strong></div>
<div class="stat-card"><span>Gastos mensuales</span><strong>{{sum('gastos','montoMensual')}}</strong></div>
<div class="stat-card"><span>Obligaciones (saldo)</span><strong>{{sum('obligaciones','saldoActual')}}</strong></div>
<div class="stat-card"><span>Cuotas actuales</span><strong>{{sum('obligaciones','cuotaMensual')}}</strong></div>
<div class="stat-card brand"><span>Cuota nueva</span><strong>{{request()['cuotaEstimada'] ?? 'Pendiente'}}</strong></div>
<div class="stat-card"><span>Documentos adjuntos</span><strong>{{documents().length}}</strong></div>
<div class="stat-card"><span>Visitas de negocio</span><strong>{{visitas().length}}</strong></div>
</div>
<p class="hint">Monto / plazo: {{request()['montoSolicitado']}} / {{request()['plazoSolicitadoMeses']}} meses</p>
@if(hasPreviews()){<div class="section-title"><span class="icon-tile"><m-icon name="folder"/></span><h3>Fotos y archivos adjuntos</h3></div><div class="preview-gallery">@for(doc of documents();track doc['solicitudDocumentoId']){@if(previewUrls()[doc['documentoVersionId']];as url){<figure class="preview-item"><img [src]="url" [alt]="doc['nombre']" (click)="openPreview(url)"><figcaption>{{doc['nombre']}}</figcaption></figure>}}</div>}
@if(visitas().length){<div class="section-title"><span class="icon-tile"><m-icon name="loan"/></span><h3>Visitas de negocio registradas</h3></div>
<div class="data-card-grid">@for(v of visitas();track v['visitaNegocioId']){<div class="data-card">
  <div class="data-card-head"><strong>{{v['fechaVisita']|date:'mediumDate'}}</strong><span class="badge" [attr.data-state]="v['recomendacion']==='FAVORABLE'?'VERDE':v['recomendacion']==='DESFAVORABLE'?'ROJO':'AMARILLO'">{{v['recomendacion']}}</span></div>
  <dl class="data-card-fields">
    <div><dt>Negocio</dt><dd>{{v['negocioExiste']?'Verificado':'NO encontrado'}}</dd></div>
    <div><dt>Dirección</dt><dd>{{v['direccionObservada'] || 'Sin registrar'}}</dd></div>
    @if(v['ingresoMensualEstimadoObservado']!=null){<div><dt>Ingreso observado</dt><dd>{{v['ingresoMensualEstimadoObservado']|number:'1.2-2'}}</dd></div>}
  </dl>
  <p class="hint"><m-icon name="clients"/>{{v['realizadaPorNombre']}}</p>
  @if(v['latitud']!=null && v['longitud']!=null){<iframe class="map-embed" [src]="mapsEmbedUrl(v['latitud'],v['longitud'])" loading="lazy" referrerpolicy="no-referrer-when-downgrade" title="Ubicación de la visita"></iframe>}
</div>}</div>}
<p role="status">El análisis calculará la capacidad de pago con la política vigente y estimará el riesgo con el modelo predictivo configurado (o su respaldo temporal, si tu empresa aún no tiene uno propio entrenado).</p><p>La recomendación siempre requiere revisión humana.</p>@if(auth.can('Analisis:Execute')){<button [disabled]="busy() || request()['cuotaEstimada']==null" (click)="execute()">{{busy()?'Ejecutando análisis…':'Ejecutar análisis →'}}</button>}@else{<p>Solicita el permiso de ejecución al administrador de tu empresa.</p>}@if(request()['cuotaEstimada']==null){<p class="error">Confirma la cuota estimada antes de analizar.</p>}@for(h of history();track h['analisisId']){<div class="list-row"><a [routerLink]="['/analisis',h['analisisId']]">Ver ejecución #{{h['numeroEjecucion']}}</a><span class="badge">{{h['estado']}}</span></div>}</section>}}
@if(fullscreenPreview();as url){<div class="preview-lightbox" (click)="fullscreenPreview.set(null)"><img [src]="url" alt="Vista ampliada"></div>}
<div class="actions"><button class="secondary" [disabled]="step()===0" (click)="step.set(step()-1)">Anterior</button><button [disabled]="step()===7" (click)="continueStep()">Continuar</button></div>}
`})
export class SolicitudWorkspace {
 auth=inject(AuthService);private api=inject(ApiClient);private dialog=inject(DialogService);private loadingOverlay=inject(LoadingOverlayService);private sanitizer=inject(DomSanitizer);private route=inject(ActivatedRoute);private router=inject(Router);history=signal<Row[]>([]);
 id=this.route.snapshot.paramMap.get('id')!;steps=['Cliente y crédito','Investigación','Preevaluación','Expediente','Verificación','Datos económicos','Visita de negocio','Resumen'];step=signal(0);
 request=signal<Row>({});client=signal<Row>({});product=signal<Row>({});economic=signal<Row>({});documents=signal<Row[]>([]);suggestions=signal<Row[]>([]);
 investigacion=signal<Row|null>(null);investigando=signal(false);
 preevaluacion=signal<Row|null>(null);preevaluando=signal(false);excepciones=signal<Row[]>([]);excepcionForm=signal<Row|null>(null);
 verificacionDocs=signal<Row[]>([]);verificando=signal<string|null>(null);laboral=signal<Row|null>(null);laboralBusy=signal(false);
 visitas=signal<Row[]>([]);visitaForm=signal<Row|null>(null);
 documentPager=new ClientPager<Row>(8);visitaPager=new ClientPager<Row>(5);documentTypeSuggestions=DOCUMENT_TYPE_SUGGESTIONS;
 previewUrls=signal<Record<string,string>>({});fullscreenPreview=signal<string|null>(null);
 loading=signal(false);busy=signal(false);error=signal('');success=signal('');completed=signal(0);uploadTotal=signal(1);documentType='';sourceId='';
 // Qué formulario de "Datos económicos" está abierto ahora mismo como ventana (null = ninguno);
 // 'combinado' es actividad+fuente, o la clave de una sección (periodos/gastos/obligaciones).
 openSection=signal<string|null>(null);
 // Actividad económica y fuentes de ingreso se capturan juntas en una sola tarjeta (ver
 // saveEconomicCombined) en vez de como dos listas independientes sin relación visible entre sí.
 actividadFields:EconField[]=[{key:'tipoActividad',label:'Tipo de actividad',required:true},{key:'empleadorNegocio',label:'Empleador o negocio',suggest:'empleador'},{key:'cargoActividad',label:'Cargo',suggest:'cargo'},{key:'fechaInicio',label:'Fecha de inicio',type:'date',suggest:'fecha_ingreso'}];
 fuenteFields:EconField[]=[{key:'tipoIngreso',label:'Tipo de ingreso',required:true},{key:'descripcion',label:'Descripción'},{key:'monedaCodigo',label:'Moneda (código de tres letras)',required:true}];
 actividadModel:Row={};fuenteModel:Row={};editingActividadId:string|null=null;editingFuenteId:string|null=null;economicDraftSaving=signal(false);periodoDraftSaving=signal(false);obligacionDraftSaving=signal(false);
 sections:{key:string;title:string;singular:string;icon:string;fields:EconField[]}[]=[
 {key:'periodos',title:'Períodos de ingreso',singular:'período',icon:'chart',fields:[{key:'periodoInicio',label:'Desde',type:'date',required:true,suggest:'periodo_desde'},{key:'periodoFin',label:'Hasta',type:'date',required:true,suggest:'periodo_hasta'},{key:'montoNeto',label:'Ingreso neto confirmado',type:'number',required:true,suggest:'ingreso_mensual'}]},
 {key:'gastos',title:'Gastos mensuales',singular:'gasto',icon:'download',fields:[{key:'tipoGasto',label:'Tipo de gasto',required:true},{key:'descripcion',label:'Descripción'},{key:'montoMensual',label:'Monto mensual',type:'number',required:true}]},
 {key:'obligaciones',title:'Obligaciones',singular:'obligación',icon:'loan',fields:[{key:'institucion',label:'Institución',suggest:'obligacion_institucion'},{key:'saldoActual',label:'Saldo confirmado',type:'number',suggest:['obligacion_saldo','reporte_buro_deuda_total']},{key:'cuotaMensual',label:'Cuota mensual confirmada',type:'number',suggest:['obligacion_cuota','reporte_buro_cuota_total']},{key:'estado',label:'Estado declarado',suggest:'obligacion_estado'},{key:'diasMoraActual',label:'Días de mora actual confirmados',type:'number',required:true,suggest:['obligacion_mora','reporte_buro_mora_actual_dias']},{key:'maxDiasMoraHistorico',label:'Máximo histórico de mora confirmado',type:'number',required:true}]}];
 economicIdKeys:Record<string,string>={periodos:'ingresoPeriodoId',gastos:'gastoId',obligaciones:'obligacionId'};
 editingIds:Record<string,string>={};
 models:Record<string,Row>={periodos:{},gastos:{},obligaciones:{}};
 constructor(){void this.load();}
 async load(){this.loading.set(true);this.error.set('');try{const r=await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id));this.request.set(r);this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));await this.loadDocuments();await this.loadInvestigacion();await this.loadPreevaluacion();await this.loadLaboral();await this.loadVisitas();if(this.auth.can('Analisis:Read'))this.history.set(await firstValueFrom(this.api.get<Row[]>('/analisis?solicitudId='+this.id)));if(this.auth.can('Clientes:Read'))this.client.set(await firstValueFrom(this.api.get<Row>('/clientes/'+r['clienteId'])));if(this.auth.can('Productos:Read'))this.product.set(await firstValueFrom(this.api.get<Row>('/productos/'+r['productoCreditoId'])));}catch(e){this.error.set(errorMessage(e));}finally{this.loading.set(false);}}
 async execute(){if(!await this.dialog.confirm('¿Ejecutar el análisis con los datos actuales? Se conservará una nueva ejecución histórica.',{title:'Ejecutar análisis'}))return;await this.run(async()=>{const result=await firstValueFrom(this.api.post<{analisisId:string}>('/solicitudes/'+this.id+'/analisis',{}));await this.router.navigate(['/analisis',result.analisisId]);},'Ejecutando análisis con el motor de riesgo…');}
 // Subir/reemplazar/anular un documento cambia qué hay para verificar, así que este refresco de
 // verificacionDocs va DENTRO de loadDocuments (no como paso aparte en load()) — de lo contrario el
 // paso "Verificación" se queda mostrando la lista tal como estaba al abrir el expediente, sin los
 // documentos que el analista acaba de subir.
 async loadDocuments(){if(this.auth.can('Documentos:Read')){this.documents.set(await firstValueFrom(this.api.get<Row[]>('/documentos/solicitud/'+this.id)));this.documentPager.set(this.documents());await this.loadSuggestions();void this.loadPreviews();}await this.loadVerificacion();}
 // Solo se descargan miniaturas de imágenes (PNG/JPEG) — un PDF no tiene una vista previa barata sin una
 // librería de renderizado, así que se queda con su ícono/nombre normal en la lista.
 async loadPreviews(){
  for(const doc of this.documents()){
   const versionId=doc['documentoVersionId'];
   if(!PREVIEWABLE_MIME_TYPES.includes(doc['mimeType'])||this.previewUrls()[versionId]||doc['documentoEstado']==='ANULADO')continue;
   try{const blob=await firstValueFrom(this.api.download('/documentos/version/'+versionId+'/archivo'));this.previewUrls.update(m=>({...m,[versionId]:URL.createObjectURL(blob)}));}
   catch{/* la miniatura es solo una ayuda visual — si falla, la lista sigue funcionando sin ella */}
  }
 }
 hasPreviews(){return Object.keys(this.previewUrls()).length>0;}
 openPreview(url:string){this.fullscreenPreview.set(url);}
 mapsUrl(lat:number,lng:number){return 'https://www.google.com/maps?q='+lat+','+lng;}
 // "output=embed" es el único modo de incrustar Google Maps en un <iframe> sin API key/token — Angular
 // exige marcar la URL como segura explícitamente o bloquea el binding [src] de un iframe por completo.
 mapsEmbedUrl(lat:number,lng:number):SafeResourceUrl{return this.sanitizer.bypassSecurityTrustResourceUrl('https://www.google.com/maps?q='+lat+','+lng+'&output=embed');}
 async loadSuggestions(){
  const found:Row[]=[];
  for(const doc of this.documents()){
   try{const r=await firstValueFrom(this.api.get<{extraidos:Row[]}>('/documentos/'+doc['solicitudDocumentoId']+'/revision'));for(const e of r.extraidos)found.push({...e,documento:doc['nombre']});}
   catch(e){this.error.set(errorMessage(e));}
  }
  this.suggestions.set(found);
  this.applySuggestions();
  await this.maybeAutoSaveEconomicDraft();
  await this.maybeAutoSavePeriodo();
  await this.maybeAutoSaveObligacion();
 }
 // Autollenado real: en cuanto hay una sugerencia y el campo sigue vacío, se coloca directamente
 // en el formulario. El analista la revisa y corrige al guardar; nada se guarda solo.
 applySuggestions(){
  for(const section of this.sections)for(const field of section.fields){
   if(!field.suggest)continue;
   const sug=this.suggestionFor(field.suggest);
   const current=this.models[section.key][field.key];
   if(sug!=null&&(current==null||current===''))this.models[section.key][field.key]=sug.value;
  }
  for(const [model,fields] of [[this.actividadModel,this.actividadFields],[this.fuenteModel,this.fuenteFields]] as [Row,EconField[]][])
   for(const field of fields){
    if(!field.suggest)continue;
    const sug=this.suggestionFor(field.suggest);
    if(sug!=null&&(model[field.key]==null||model[field.key]===''))model[field.key]=sug.value;
   }
 }
 // Un campo puede tener más de una fuente posible (ej. un "certificado de deuda" o, si no hay,
 // el total agregado de un reporte de buró cargado) — se usa la primera que efectivamente aparezca.
 suggestionFor(codigoCampo?:string|string[]):{value:any;display:string;documento:string}|null{
  if(!codigoCampo)return null;
  const codes=Array.isArray(codigoCampo)?codigoCampo:[codigoCampo];
  const found=this.suggestions().find(s=>codes.includes(s['codigoCampo']));
  if(!found)return null;
  const value=found['valorFecha']??found['valorNumerico']??found['valorTexto']??(found['valorBooleano']!=null?found['valorBooleano']:null);
  return value==null?null:{value,display:String(value),documento:found['documento']};
 }
 // Solo se usa como texto informativo mientras el guardado automático está en curso (ver
 // maybeAutoSaveEconomicDraft) o como respaldo si el usuario no tiene permiso para guardar — el flujo
 // normal ya no depende de esto para llenar la actividad económica.
 draftEconomicFields():{label:string;value:string;documento:string}[]{
  return [...this.actividadFields,...this.fuenteFields]
   .map(f=>{const sug=this.suggestionFor(f.suggest);return sug?{label:f.label,value:sug.display,documento:sug.documento}:null;})
   .filter((x):x is {label:string;value:string;documento:string}=>x!==null);
 }
 // El analista es responsable de revisar lo que ya se detectó (vía "Corregir"), no de aprobar antes de
 // que cuente: en cuanto hay algo detectado del documento, se guarda directo como actividad/fuente real.
 // Lo que no se detectó automáticamente queda marcado con ECON_DEFAULT para que continueStep() lo note.
 async maybeAutoSaveEconomicDraft(){
  if(this.economicDraftSaving()||this.rows('actividades').length>0)return;
  if(!this.auth.can('Solicitudes:Update')||this.request()['estado']!=='BORRADOR')return;
  const draft=this.draftEconomicFields();
  if(!draft.length)return;
  this.economicDraftSaving.set(true);
  try{
   const empleador=this.suggestionFor('empleador')?.value??ECON_DEFAULT;
   const cargo=this.suggestionFor('cargo')?.value??ECON_DEFAULT;
   const fechaInicio=this.suggestionFor('fecha_ingreso')?.value??null;
   const actividadId=await firstValueFrom(this.api.post<string>('/solicitudes/'+this.id+'/expediente/actividades',
    {tipoActividad:ECON_DEFAULT,empleadorNegocio:empleador,cargoActividad:cargo,fechaInicio,esPrincipal:true}));
   await firstValueFrom(this.api.post('/solicitudes/'+this.id+'/expediente/fuentes',
    {actividadEconomicaId:actividadId,tipoIngreso:ECON_DEFAULT,descripcion:'Fuente de ingreso detectada automáticamente',monedaCodigo:'USD',esRecurrente:true}));
   this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));
  }catch(e){this.error.set(errorMessage(e));}
  finally{this.economicDraftSaving.set(false);}
 }
 // El motor de análisis exige que TODO período sea un mes calendario completo (día 1 al último día,
 // ver AnalysisDecisions.IsMonthly) — nunca confía en la fecha "hasta" tal cual la sugiera el OCR
 // (un rol de pago quincenal puede mostrar "01/09 al 09/09"), sino que la ajusta al mes completo que
 // esa fecha sugiere. Si el mes detectado es el actual (todavía no cerró) o no hay ninguna fecha
 // sugerida, retrocede al último mes ya cerrado — nunca puede terminar hoy ni en el futuro.
 fullMonthRange(seedIso?:string|null):{desde:string;hasta:string}{
  const fmt=(d:Date)=>d.getFullYear()+'-'+String(d.getMonth()+1).padStart(2,'0')+'-'+String(d.getDate()).padStart(2,'0');
  const hoy=new Date();
  let year=hoy.getFullYear(),month=hoy.getMonth();
  if(seedIso){const seed=new Date(seedIso+'T00:00:00');year=seed.getFullYear();month=seed.getMonth();}
  if(year>hoy.getFullYear()||(year===hoy.getFullYear()&&month>=hoy.getMonth()))month-=1;
  return {desde:fmt(new Date(year,month,1)),hasta:fmt(new Date(year,month+1,0))};
 }
 // Mismo criterio que la actividad: en cuanto hay un ingreso_mensual detectado se guarda un período real
 // bajo la fuente ya creada (a lo sumo uno por fuente — el analista agrega más con "Agregar período" si
 // el documento traía varios meses).
 async maybeAutoSavePeriodo(){
  if(this.periodoDraftSaving())return;
  if(!this.auth.can('Solicitudes:Update')||this.request()['estado']!=='BORRADOR')return;
  const fuente=this.rows('fuentes')[0];
  if(!fuente)return;
  const monto=this.suggestionFor('ingreso_mensual')?.value;
  if(monto==null)return;
  if(this.rows('periodos').some(p=>p['fuenteIngresoId']===fuente['fuenteIngresoId']))return;
  this.periodoDraftSaving.set(true);
  try{
   const desde=this.suggestionFor('periodo_desde')?.value;
   const hasta=this.suggestionFor('periodo_fin')?.value;
   const {desde:periodoInicio,hasta:periodoFin}=this.fullMonthRange(desde??hasta??null);
   await firstValueFrom(this.api.post('/solicitudes/'+this.id+'/expediente/fuentes/'+fuente['fuenteIngresoId']+'/periodos',
    {periodoInicio,periodoFin,montoNeto:monto,observacion:(desde||hasta)?'Detectado automáticamente del documento (ajustado a mes calendario completo)':'Detectado automáticamente del documento'}));
   this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));
  }catch(e){this.error.set(errorMessage(e));}
  finally{this.periodoDraftSaving.set(false);}
 }
 // Estado NO puede quedar en ECON_DEFAULT: el motor de análisis financiero exige que sea uno de
 // VIGENTE/CANCELADA/CASTIGADA/REESTRUCTURADA o rechaza la ejecución completa — "VIGENTE" es el
 // supuesto conservador (la deuda sigue activa) cuando no se detectó un estado explícito.
 async maybeAutoSaveObligacion(){
  if(this.obligacionDraftSaving()||this.rows('obligaciones').length>0)return;
  if(!this.auth.can('Solicitudes:Update')||this.request()['estado']!=='BORRADOR')return;
  const institucion=this.suggestionFor('obligacion_institucion')?.value;
  const saldo=this.suggestionFor(['obligacion_saldo','reporte_buro_deuda_total'])?.value;
  const cuota=this.suggestionFor(['obligacion_cuota','reporte_buro_cuota_total'])?.value;
  if(institucion==null&&saldo==null&&cuota==null)return;
  this.obligacionDraftSaving.set(true);
  try{
   const estado=this.suggestionFor('obligacion_estado')?.value??'VIGENTE';
   const mora=this.suggestionFor(['obligacion_mora','reporte_buro_mora_actual_dias'])?.value??0;
   await firstValueFrom(this.api.post('/solicitudes/'+this.id+'/expediente/obligaciones',
    {institucion:institucion??ECON_DEFAULT,tipoObligacion:ECON_DEFAULT,saldoActual:saldo??0,cuotaMensual:cuota??0,
     estado,diasMoraActual:mora,maxDiasMoraHistorico:mora,esGarante:false}));
   this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));
  }catch(e){this.error.set(errorMessage(e));}
  finally{this.obligacionDraftSaving.set(false);}
 }
 rows(key:string):Row[]{return this.economic()[key] || [];}
 sum(key:string,field:string){const rows=this.rows(key);return rows.some(r=>r[field]==null)?'Información incompleta':rows.reduce((n,r)=>n+Number(r[field]),0);}
 editableDocuments(){return ['BORRADOR','DOCUMENTACION'].includes(this.request()['estado']);}
 fuentesDe(actividadId:string){return this.rows('fuentes').filter(f=>f['actividadEconomicaId']===actividadId);}
 editEconomicCombined(actividad:Row){
  this.actividadModel={...actividad};this.editingActividadId=actividad['actividadEconomicaId'];
  const fuente=this.fuentesDe(actividad['actividadEconomicaId'])[0];
  this.fuenteModel=fuente?{...fuente}:{};this.editingFuenteId=fuente?fuente['fuenteIngresoId']:null;
 }
 // applySuggestions() solo rellena campos vacíos, así que es seguro (y necesario) volver a
 // llamarla aquí: sin esto, el formulario se abre vacío y el aviso "Prellenado automáticamente"
 // queda mostrando una promesa que el campo no cumple.
 cancelEconomicCombined(){this.actividadModel={};this.fuenteModel={};this.editingActividadId=null;this.editingFuenteId=null;this.applySuggestions();}
 async saveEconomicCombined(){
 const wasEditing=!!this.editingActividadId;
 await this.run(async()=>{
  const aPayload={...this.actividadModel};for(const f of this.actividadFields)if(aPayload[f.key]==='')aPayload[f.key]=null;
  const aUrl='/solicitudes/'+this.id+'/expediente/actividades';
  const actividadId=this.editingActividadId
   ?await firstValueFrom(this.api.put<string>(aUrl+'/'+this.editingActividadId,aPayload))
   :await firstValueFrom(this.api.post<string>(aUrl,aPayload));
  const fPayload:Row={...this.fuenteModel,actividadEconomicaId:actividadId};for(const f of this.fuenteFields)if(fPayload[f.key]==='')fPayload[f.key]=null;
  const fUrl='/solicitudes/'+this.id+'/expediente/fuentes';
  if(this.editingFuenteId)await firstValueFrom(this.api.put(fUrl+'/'+this.editingFuenteId,fPayload));
  else await firstValueFrom(this.api.post(fUrl,fPayload));
  this.cancelEconomicCombined();
  this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));
 });
 if(this.error())return;
 this.openSection.set(null);
 // Igual que en saveEconomic: no se ofrece "agregar otra" después de corregir una ya existente.
 if(!wasEditing&&await this.dialog.confirm('¿Deseas agregar otra actividad económica o fuente de ingreso?',{title:'Registro guardado',confirmLabel:'Sí, agregar otra',cancelLabel:'No, por ahora'}))this.openSection.set('combinado');
 }
 fieldsOf(key:string,row:Row):{label:string;value:string}[]{const fields=key==='actividades'?this.actividadFields:key==='fuentes'?this.fuenteFields:this.sections.find(s=>s.key===key)!.fields;return fields.filter(f=>row[f.key]!=null&&row[f.key]!=='').map(f=>({label:f.label,value:String(row[f.key])}));}
 incompleteEconomicSectionLabels:Record<string,string>={actividades:'Actividad económica',fuentes:'Fuente de ingreso',periodos:'Períodos de ingreso',obligaciones:'Obligaciones'};
 incompleteEconomicSections(){return Object.keys(this.incompleteEconomicSectionLabels).filter(key=>this.rows(key).some(r=>Object.values(r).includes(ECON_DEFAULT))).map(key=>this.incompleteEconomicSectionLabels[key]);}
 // El "Continuar" del paso de Datos económicos avisa si algo quedó sin detectar automáticamente, pero
 // no bloquea — el analista decide si ya revisó lo suficiente o prefiere volver a corregirlo primero.
 // El mensaje nombra las secciones afectadas para que no tenga que adivinar dónde está el pendiente.
 async continueStep(){
  if(this.step()===5){
   const pending=this.incompleteEconomicSections();
   if(pending.length&&!await this.dialog.confirm('Hay datos "Sin especificar" en: '+pending.join(', ')+'. No se detectaron automáticamente del documento — corrígelos con el botón "Corregir" en la tarjeta antes de continuar.',{title:'Datos económicos sin revisar',confirmLabel:'Continuar de todas formas'}))return;
  }
  this.step.set(this.step()+1);
 }
 async run(action:()=>Promise<void>,message='Guardando…'){if(this.busy())return;this.busy.set(true);this.error.set('');this.success.set('');this.loadingOverlay.show(message);try{await action();this.success.set('Cambios guardados.');}catch(e){this.error.set(errorMessage(e));}finally{this.busy.set(false);this.loadingOverlay.hide();}}
 // Los tres tipos de registro usan nombres de id distintos, pero cada tarjeta solo trae UNO de ellos
 // — de ahí que se puedan probar los tres sin ambigüedad. Solo toma "row" y "$index" porque una
 // expresión de "track" en un @for anidado no puede leer la variable del @for exterior (section).
 rowId(row:Row){return row['ingresoPeriodoId']??row['gastoId']??row['obligacionId'];}
 editEconomic(key:string,row:Row){this.models[key]={...row};this.editingIds[key]=row[this.economicIdKeys[key]];if(key==='periodos')this.sourceId=row['fuenteIngresoId'];}
 startEconomic(key:string){this.models[key]={};delete this.editingIds[key];if(key==='periodos')this.sourceId='';this.openSection.set(key);this.applySuggestions();}
 cancelEconomic(key:string){this.openSection.set(null);this.models[key]={};delete this.editingIds[key];if(key==='periodos')this.sourceId='';}
 async deleteEconomic(key:string,row:Row){
  if(!await this.dialog.confirm('¿Eliminar este registro? No se puede deshacer — útil si se cargó por error o quedó duplicado.',{title:'Eliminar '+this.sections.find(s=>s.key===key)!.singular,confirmLabel:'Eliminar',cancelLabel:'Cancelar'}))return;
  await this.run(async()=>{
   const id=row[this.economicIdKeys[key]];
   const path=key==='periodos'?'fuentes/'+row['fuenteIngresoId']+'/periodos/'+id:key+'/'+id;
   await firstValueFrom(this.api.delete('/solicitudes/'+this.id+'/expediente/'+path));
   this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));
  },'Eliminando…');
 }
 async saveEconomic(section:{key:string,title:string,singular:string}){
 const wasEditing=!!this.editingIds[section.key];
 await this.run(async()=>{const key=section.key;const payload={...this.models[key]};for(const field of this.sections.find(s=>s.key===key)!.fields)if(payload[field.key]==='')payload[field.key]=null;const path=key==='periodos'?'fuentes/'+this.sourceId+'/periodos':key;const url='/solicitudes/'+this.id+'/expediente/'+path;await firstValueFrom(this.editingIds[key]?this.api.put(url+'/'+this.editingIds[key],payload):this.api.post(url,payload));this.models[key]={};delete this.editingIds[key];this.economic.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/expediente')));});
 if(this.error())return;
 this.openSection.set(null);
 // Solo se ofrece "agregar otro" después de crear un registro nuevo — tras una corrección, el
 // analista ya terminó con esa tarjeta y este mismo diálogo confundía y llevaba a crear duplicados
 // por accidente cuando se aceptaba sin darse cuenta de que reabría un formulario en blanco.
 if(!wasEditing&&await this.dialog.confirm('¿Deseas agregar otro registro de "'+section.title+'"?',{title:'Registro guardado',confirmLabel:'Sí, agregar otro',cancelLabel:'No, por ahora'}))this.startEconomic(section.key);
 }
 files(event:Event){const input=event.target as HTMLInputElement;void this.upload(Array.from(input.files || []));input.value='';}
 drop(event:DragEvent){event.preventDefault();if(this.auth.can('Documentos:Upload')&&this.editableDocuments())void this.upload(Array.from(event.dataTransfer?.files || []));}
 async upload(files:File[]){if(!this.documentType.trim() || !files.length)return;await this.run(async()=>{this.completed.set(0);this.uploadTotal.set(files.length);const failures:string[]=[];for(const file of files){try{const body=new FormData();body.append('archivo',file);body.append('nombre',file.name);body.append('tipo',this.documentType);await firstValueFrom(this.api.post('/documentos/solicitud/'+this.id,body));}catch(e){failures.push(file.name+': '+errorMessage(e));}this.completed.update(n=>n+1);}await this.loadDocuments();
  // Limpia el tipo apenas termina la carga (haya fallado algún archivo o no) para que el siguiente
  // documento a subir empiece en blanco — de lo contrario el analista puede etiquetar por error el
  // siguiente archivo con el tipo anterior sin darse cuenta.
  this.documentType='';
  if(failures.length)throw {error:{title:failures.join(' / ')}};});}
 async replace(event:Event,doc:Row){const input=event.target as HTMLInputElement;const file=input.files?.[0];input.value='';if(!file || !await this.dialog.confirm('¿Guardar una nueva versión conservando la anterior?',{title:'Nueva versión del documento'}))return;await this.run(async()=>{const body=new FormData();body.append('archivo',file);await firstValueFrom(this.api.post('/documentos/'+doc['solicitudDocumentoId']+'/versiones',body));await this.loadDocuments();});}
 async download(doc:Row){await this.run(async()=>{const blob=await firstValueFrom(this.api.download('/documentos/version/'+doc['documentoVersionId']+'/archivo'));const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download=doc['nombre'];a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});}
 async voidDocument(doc:Row){if(!await this.dialog.confirm('¿Eliminar "'+doc['nombre']+'"? El archivo no se destruye, pero deja de contar para el expediente.',{title:'Eliminar documento'}))return;await this.run(async()=>{await firstValueFrom(this.api.post('/documentos/'+doc['solicitudDocumentoId']+'/anular',{}));await this.loadDocuments();});}
 async loadInvestigacion(){if(!this.auth.can('Investigacion:Read'))return;try{this.investigacion.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/investigacion')));}catch(e){this.error.set(errorMessage(e));}}
 async ejecutarInvestigacion(){if(this.investigando())return;this.investigando.set(true);this.error.set('');this.loadingOverlay.show('Consultando buró, procesos judiciales y aval…');try{this.investigacion.set(await firstValueFrom(this.api.post<Row>('/solicitudes/'+this.id+'/investigacion',{})));}catch(e){this.error.set(errorMessage(e));}finally{this.investigando.set(false);this.loadingOverlay.hide();}}
 aplicaAlguna(p:Row){return (p['reglas'] as Row[]).some(r=>r['aplica']);}
 async loadPreevaluacion(){if(!this.auth.can('Preevaluacion:Read'))return;try{this.preevaluacion.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/preevaluacion')));}catch(e){this.error.set(errorMessage(e));}if(this.auth.can('Excepciones:Read'))await this.loadExcepciones();}
 async ejecutarPreevaluacion(){if(this.preevaluando())return;this.preevaluando.set(true);this.error.set('');this.loadingOverlay.show('Evaluando políticas de preevaluación…');try{this.preevaluacion.set(await firstValueFrom(this.api.post<Row>('/solicitudes/'+this.id+'/preevaluacion',{})));}catch(e){this.error.set(errorMessage(e));}finally{this.preevaluando.set(false);this.loadingOverlay.hide();}}
 async loadExcepciones(){try{this.excepciones.set(await firstValueFrom(this.api.get<Row[]>('/excepciones/solicitud/'+this.id)));}catch(e){this.error.set(errorMessage(e));}}
 async solicitarExcepcion(){await this.run(async()=>{const f=this.excepcionForm()!;await firstValueFrom(this.api.post('/excepciones',{solicitudCreditoId:this.id,reglaId:null,motivoJustificacion:f['motivoJustificacion'],observacion:f['observacion']||null,evidencia:null}));this.excepcionForm.set(null);await this.loadExcepciones();});}
 async resolverExcepcion(e:Row,aprobar:boolean){const comentario=await this.dialog.prompt(aprobar?'Comentario de aprobación (opcional):':'Motivo del rechazo (opcional):',{title:aprobar?'Aprobar excepción':'Rechazar excepción'});if(comentario===null)return;await this.run(async()=>{await firstValueFrom(this.api.post('/excepciones/'+e['excepcionId']+'/resolver',{aprobar,comentario:comentario||null}));await this.loadExcepciones();});}
 async loadVerificacion(){if(!this.auth.can('Verificacion:Read'))return;try{this.verificacionDocs.set(await firstValueFrom(this.api.get<Row[]>('/solicitudes/'+this.id+'/verificacion/documentos')));}catch(e){this.error.set(errorMessage(e));}}
 async verificarDocumento(doc:Row){if(this.verificando())return;this.verificando.set(doc['solicitudDocumentoId']);this.error.set('');this.loadingOverlay.show('Verificando autenticidad del documento…');try{const updated=await firstValueFrom(this.api.post<Row>('/solicitudes/'+this.id+'/verificacion/documentos/'+doc['solicitudDocumentoId'],{}));this.verificacionDocs.update(list=>list.map(d=>d['solicitudDocumentoId']===updated['solicitudDocumentoId']?updated:d));}catch(e){this.error.set(errorMessage(e));}finally{this.verificando.set(null);this.loadingOverlay.hide();}}
 async loadLaboral(){if(!this.auth.can('Verificacion:Read'))return;try{this.laboral.set(await firstValueFrom(this.api.get<Row>('/solicitudes/'+this.id+'/verificacion/laboral')));}catch(e){this.error.set(errorMessage(e));}}
 async ejecutarLaboral(){if(this.laboralBusy())return;this.laboralBusy.set(true);this.error.set('');this.loadingOverlay.show('Consultando relación laboral en el IESS…');try{this.laboral.set(await firstValueFrom(this.api.post<Row>('/solicitudes/'+this.id+'/verificacion/laboral',{})));}catch(e){this.error.set(errorMessage(e));}finally{this.laboralBusy.set(false);this.loadingOverlay.hide();}}
 async loadVisitas(){if(!this.auth.can('VisitaNegocio:Read'))return;try{this.visitas.set(await firstValueFrom(this.api.get<Row[]>('/solicitudes/'+this.id+'/visitas')));this.visitaPager.set(this.visitas());}catch(e){this.error.set(errorMessage(e));}}
 startVisita(){this.visitaForm.set({fechaVisita:new Date().toISOString().slice(0,10),negocioExiste:true,recomendacion:'FAVORABLE'});}
 // Geolocalización del navegador es opcional y de solo lectura para el analista — si el dispositivo/permiso
 // no está disponible, la visita se guarda igual sin coordenadas (nunca bloquea el registro de la visita).
 capturarUbicacion(){
  if(!navigator.geolocation){this.error.set('Este dispositivo no soporta geolocalización.');return;}
  navigator.geolocation.getCurrentPosition(
   pos=>this.visitaForm.update(f=>f&&({...f,latitud:pos.coords.latitude,longitud:pos.coords.longitude})),
   ()=>this.error.set('No se pudo obtener la ubicación. Verifica los permisos del navegador.'));
 }
 async guardarVisita(){await this.run(async()=>{
  const f=this.visitaForm()!;
  const payload={...f};
  for(const k of ['direccionObservada','tipoNegocioObservado','tiempoFuncionamientoObservado','observaciones'])if(payload[k]==='')payload[k]=null;
  await firstValueFrom(this.api.post('/solicitudes/'+this.id+'/visitas',payload));
  this.visitaForm.set(null);
  await this.loadVisitas();
 });}
}





