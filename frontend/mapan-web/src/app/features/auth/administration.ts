import {Component,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {firstValueFrom} from 'rxjs';
import {ApiClient,errorMessage} from '../../core/api';
import {DialogService} from '../../shared/dialog';
import {AlertFocus} from '../../shared/alert-focus';
type Row=Record<string,string>;
@Component({selector:'mapan-security',imports:[FormsModule,AlertFocus],template:`
<header class="page-heading"><div><span class="eyebrow">ADMINISTRACIÓN</span><h1>Usuarios, roles y permisos</h1><p>Administra el acceso a la empresa activa.</p></div></header>
@if(error()){<p role="alert" class="error" mapanAlertFocus>{{error()}}</p><button (click)="load()">Reintentar</button>}@if(success()){<p role="status" class="success">{{success()}}</p>}@if(loading()){<p role="status">Cargando accesos…</p>}
<section class="panel"><h2>Usuarios</h2>@for(u of users();track u['usuarioEmpresaId']){<div class="list-row"><span>{{u['nombres']}} {{u['apellidos']}} · {{u['nombreUsuario']}}</span><button class="secondary" [disabled]="busy()" (click)="assign('usuarios',u['usuarioEmpresaId'],u['nombreUsuario'])">Editar roles</button></div>}@empty{<p>No hay usuarios disponibles.</p>}
<details><summary>Crear usuario</summary><form (ngSubmit)="createUser()" #uf="ngForm"><div class="grid-form">@for(field of userFields;track field.key){<label>{{field.label}}<input [name]="field.key" [(ngModel)]="user[field.key]" [type]="field.type" required [minlength]="field.key==='password'?12:1" [maxlength]="field.max" autocomplete="off"></label>}</div><button [disabled]="busy() || uf.invalid">Crear usuario</button></form></details></section>
<section class="panel"><h2>Roles</h2>@for(r of roles();track r['rolId']){<div class="list-row"><span>{{r['nombre']}} · {{r['estado']}}</span><button class="secondary" [disabled]="busy()" (click)="assign('roles',r['rolId'],r['nombre'])">Editar permisos</button></div>}@empty{<p>No hay roles configurados.</p>}
<details><summary>Crear rol</summary><form (ngSubmit)="createRole()" #rf="ngForm"><div class="grid-form"><label>Código<input name="code" [(ngModel)]="role.codigo" required maxlength="50"></label><label>Nombre<input name="name" [(ngModel)]="role.nombre" required maxlength="100"></label><label>Descripción<input name="description" [(ngModel)]="role.descripcion" maxlength="300"></label></div><button [disabled]="busy() || rf.invalid">Crear rol</button></form></details></section>
@if(target()){<section class="panel"><h2>Accesos de {{target()!.name}}</h2><p>Guardar sustituye la asignación actual por la selección. Los cambios se reflejan en una nueva sesión.</p>@for(option of choices();track option.id){<label class="check-row"><input type="checkbox" [checked]="selected().includes(option.id)" (change)="toggle(option.id)">{{option.name}}</label>}<div class="actions"><button [disabled]="busy()" (click)="saveAssignments()">Guardar asignación</button><button class="secondary" (click)="target.set(null)">Cancelar</button></div></section>}
`})
export class SecurityAdministration {
 private api=inject(ApiClient);private dialog=inject(DialogService);users=signal<Row[]>([]);roles=signal<Row[]>([]);permissions=signal<Row[]>([]);loading=signal(false);busy=signal(false);error=signal('');success=signal('');
 target=signal<{kind:string;id:string;name:string}|null>(null);selected=signal<string[]>([]);choices=signal<{id:string;name:string}[]>([]);
 user:Row={};role={codigo:'',nombre:'',descripcion:''};userFields=[{key:'nombreUsuario',label:'Usuario',type:'text',max:100},{key:'correo',label:'Correo',type:'email',max:200},{key:'nombres',label:'Nombres',type:'text',max:120},{key:'apellidos',label:'Apellidos',type:'text',max:120},{key:'password',label:'Contraseña inicial (mínimo 12 caracteres)',type:'password',max:1024}];
 constructor(){void this.load();}
 async load(){this.loading.set(true);try{this.users.set(await firstValueFrom(this.api.get<Row[]>('/seguridad/usuarios')));this.roles.set(await firstValueFrom(this.api.get<Row[]>('/seguridad/roles')));this.permissions.set(await firstValueFrom(this.api.get<Row[]>('/seguridad/permisos')));}catch(e){this.error.set(errorMessage(e));}finally{this.loading.set(false);}}
 async run(action:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');this.success.set('');try{await action();this.success.set('Operación completada.');}catch(e){this.error.set(errorMessage(e));}finally{this.busy.set(false);}}
 async createUser(){await this.run(async()=>{try{await firstValueFrom(this.api.post('/seguridad/usuarios',this.user));this.user={};await this.load();}finally{this.user['password']='';}});}
 async createRole(){await this.run(async()=>{await firstValueFrom(this.api.post('/seguridad/roles',this.role));this.role={codigo:'',nombre:'',descripcion:''};await this.load();});}
 async assign(kind:string,id:string,name:string){await this.run(async()=>{this.target.set(null);this.selected.set(await firstValueFrom(this.api.get<string[]>('/seguridad/'+kind+'/'+id+'/'+(kind==='usuarios'?'roles':'permisos'))));this.choices.set(kind==='usuarios'?this.roles().filter(r=>r['estado']==='ACTIVO').map(r=>({id:r['rolId'],name:r['nombre']})):this.permissions().map(p=>({id:p['permisoId'],name:p['nombre']+' · '+p['codigo']})));this.target.set({kind,id,name});});}
 toggle(id:string){this.selected.update(ids=>ids.includes(id)?ids.filter(x=>x!==id):[...ids,id]);}
 async saveAssignments(){const t=this.target();if(!t||!await this.dialog.confirm('¿Confirmas la asignación de accesos para '+t.name+'?',{title:'Guardar accesos'}))return;await this.run(async()=>{await firstValueFrom(this.api.put('/seguridad/'+t.kind+'/'+t.id+'/'+(t.kind==='usuarios'?'roles':'permisos'),this.selected()));this.target.set(null);});}
}
