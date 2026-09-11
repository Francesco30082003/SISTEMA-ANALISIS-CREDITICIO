import {Component,effect,inject,signal} from '@angular/core';
import {Icon} from './shared/icon';
import {DialogHost} from './shared/dialog';
import {LoadingOverlayHost} from './shared/loading-overlay';
import {SessionExpiryModal} from './shared/session-expiry-modal';
import {ApiClient} from './core/api';
import {Router,RouterLink,RouterLinkActive,RouterOutlet} from '@angular/router';
import {AuthService} from './core/auth';
@Component({selector:'app-root',imports:[RouterOutlet,RouterLink,RouterLinkActive,Icon,DialogHost,LoadingOverlayHost,SessionExpiryModal],templateUrl:'./app.html',styleUrl:'./app.css'})
export class App {
  auth=inject(AuthService);private router=inject(Router);
  mobileMenu=signal(false);hasWorkflow=signal(false);private api=inject(ApiClient);
  groups=[{name:'TRABAJO',links:[{path:'/nuevo-analisis',label:'Nuevo análisis',icon:'plus',permission:'Solicitudes:Create'},{path:'/solicitudes',label:'Solicitudes',icon:'folder',permission:'Solicitudes:Read'},{path:'/clientes',label:'Clientes',icon:'clients',permission:'Clientes:Read'}]},
  {name:'RIESGO',links:[{path:'/analisis',label:'Análisis',icon:'chart',permission:'Analisis:Read'},{path:'/alertas',label:'Alertas',icon:'alert',permission:'Alertas:Read'},{path:'/politicas',label:'Políticas',icon:'shield',permission:'Politicas:Read'}]},
  {name:'CARTERA',links:[{path:'/prestamos',label:'Préstamos',icon:'loan',permission:'Prestamos:Read'},{path:'/mora',label:'Mora',icon:'alert',permission:'Mora:Read'},{path:'/cosechas',label:'Cosechas',icon:'chart',permission:'Cosechas:Read'}]},
  {name:'EQUIPO',links:[{path:'/analistas',label:'Desempeño de analistas',icon:'chart',permission:'Analistas:Read'}]},
  {name:'ADMINISTRACIÓN',links:[{path:'/productos',label:'Productos',icon:'folder',permission:'Productos:Read'},{path:'/administracion/seguridad',label:'Usuarios y permisos',icon:'settings',permission:'Seguridad:Manage'},{path:'/flujos',label:'Flujos',icon:'flow',permission:'Workflow:Read'},{path:'/integraciones',label:'Integraciones',icon:'plug',permission:'Integraciones:Read'},{path:'/modelos',label:'Modelos',icon:'model',permission:'Modelos:Read'}]},
  {name:'CONTROL',links:[{path:'/auditoria',label:'Auditoría',icon:'audit',permission:'Auditoria:Read'}]}];
  visible(links:{permission:string}[]){return links.some(x=>this.auth.can(x.permission));}
  constructor(){effect(()=>{if(!this.auth.token()&&!this.auth.selectionToken())void this.router.navigateByUrl('/login');if(this.auth.can('Workflow:Read'))this.api.get<{rutas:{activa:boolean}[]}>('/workflow/rutas').subscribe({next:r=>this.hasWorkflow.set(r.rutas.some(x=>x.activa)),error:()=>this.hasWorkflow.set(false)});else this.hasWorkflow.set(false);});}
  logout(){this.auth.clear();void this.router.navigateByUrl('/login');}
}
