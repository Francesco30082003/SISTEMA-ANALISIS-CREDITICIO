import { Routes } from '@angular/router';
import {authGuard,permissionGuard} from './core/auth';
import {clientes} from './features/clientes/config';
import {productos} from './features/productos/config';
import {solicitudes} from './features/solicitudes/config';
export const routes:Routes=[
 {path:'login',loadComponent:()=>import('./features/auth/login').then(m=>m.Login)},
 {path:'politicas',canActivate:[authGuard,permissionGuard],data:{operation:'Politicas:Read'},loadComponent:()=>import('./features/policies/policies').then(m=>m.PoliciesPage)},
 {path:'modelos',canActivate:[authGuard,permissionGuard],data:{operation:'Modelos:Read'},loadComponent:()=>import('./features/models/models').then(m=>m.ModelsPage)},
 {path:'mora',canActivate:[authGuard,permissionGuard],data:{operation:'Mora:Read'},loadComponent:()=>import('./features/mora/mora').then(m=>m.MoraPage)},
 {path:'analistas',canActivate:[authGuard,permissionGuard],data:{operation:'Analistas:Read'},loadComponent:()=>import('./features/analistas/analistas').then(m=>m.AnalistasPage)},
 {path:'cosechas',canActivate:[authGuard,permissionGuard],data:{operation:'Cosechas:Read'},loadComponent:()=>import('./features/cosechas/cosechas').then(m=>m.CosechasPage)},
 ...['analisis','analisis/:id'].map(path=>({path,canActivate:[authGuard,permissionGuard],data:{operation:'Analisis:Read'},loadComponent:()=>import('./features/analysis/analysis').then(m=>m.AnalysisPage)})),
 ...[{path:'alertas',area:'alertas',operation:'Alertas:Read'},{path:'auditoria',area:'auditoria',operation:'Auditoria:Read'},{path:'prestamos',area:'prestamos',operation:'Prestamos:Read'},{path:'prestamos/:id',area:'desempeno',operation:'Prestamos:Read'},{path:'aprobaciones',area:'workflow',operation:'Workflow:Read'},{path:'flujos',area:'rutas',operation:'Workflow:Read'},{path:'integraciones',area:'integraciones',operation:'Integraciones:Read'}].map(config=>({path:config.path,canActivate:[authGuard,permissionGuard],data:config,loadComponent:()=>import('./features/operations/operations').then(m=>m.OperationsPage)})),
 {path:'administracion/seguridad',canActivate:[authGuard,permissionGuard],data:{operation:'Seguridad:Manage'},loadComponent:()=>import('./features/auth/administration').then(m=>m.SecurityAdministration)},
 {path:'nuevo-analisis',canActivate:[authGuard,permissionGuard],data:{operation:'Solicitudes:Create'},loadComponent:()=>import('./features/solicitudes/new-analysis').then(m=>m.NewAnalysisPage)},
 {path:'solicitudes/:id/expediente',canActivate:[authGuard,permissionGuard],data:{operation:'Solicitudes:Read'},loadComponent:()=>import('./features/solicitudes/workspace').then(m=>m.SolicitudWorkspace)},
 {path:'',canActivate:[authGuard],loadComponent:()=>import('./features/dashboard/dashboard').then(m=>m.Dashboard)},
 ...[clientes,productos,solicitudes].map(resource=>({path:resource.path.slice(1),canActivate:[authGuard,permissionGuard],data:{operation:resource.permission+':Read',resource},loadComponent:()=>import('./shared/resource-page').then(m=>m.ResourcePage)})),
 {path:'**',redirectTo:''}
];

