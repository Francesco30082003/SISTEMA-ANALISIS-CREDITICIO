import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpInterceptorFn } from '@angular/common/http';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom, catchError, throwError } from 'rxjs';
import { runtimeConfig } from './config';
export interface Membership {usuarioEmpresaId:string; empresaId:string; codigo:string; nombreLegal:string; sucursalPredeterminadaId:string|null;}
export interface Session {identity:{usuarioId:string;nombreUsuario:string;membership:Membership;roles:string[];permisos:string[]};allowedOperations:string[];}
interface AuthResponse {accessToken:string|null;selectionToken:string|null;expiresAt:string;empresas:Membership[];}
const SESSION_WARNING_MS=5*60*1000;
@Injectable({providedIn:'root'})
export class AuthService {
  private http=inject(HttpClient);
  token=signal<string|null>(null);
  selectionToken=signal<string|null>(null);
  memberships=signal<Membership[]>([]);
  session=signal<Session|null>(null);
  sessionWarningSecondsLeft=signal<number|null>(null);
  sessionWarningDismissed=signal(false);
  private expiryAtMs:number|null=null;
  private warnTimer:ReturnType<typeof setTimeout>|undefined;
  private countdownInterval:ReturnType<typeof setInterval>|undefined;
  can(operation:string) {return this.session()?.allowedOperations.includes(operation)??false;}
  async login(usuario:string,password:string) {
    this.clear();
    await this.accept(await firstValueFrom(this.http.post<AuthResponse>(runtimeConfig.apiBaseUrl+'/auth/login',{usuario,password})));
  }
  async select(empresaId:string) {
    await this.accept(await firstValueFrom(this.http.post<AuthResponse>(runtimeConfig.apiBaseUrl+'/auth/select-empresa',{selectionToken:this.selectionToken(),empresaId})));
  }
  dismissWarning() {this.sessionWarningDismissed.set(true);}
  private async accept(response:AuthResponse) {
    this.memberships.set(response.empresas); this.selectionToken.set(response.selectionToken);
    if(response.accessToken) {
      this.token.set(response.accessToken);
      try {this.session.set(await firstValueFrom(this.http.get<Session>(runtimeConfig.apiBaseUrl+'/auth/me')));}
      catch(error) {this.clear();throw error;}
      this.scheduleExpiry(Date.parse(response.expiresAt));
    }
  }
  private scheduleExpiry(expiryAtMs:number) {
    clearTimeout(this.warnTimer); clearInterval(this.countdownInterval);
    this.expiryAtMs=expiryAtMs; this.sessionWarningSecondsLeft.set(null); this.sessionWarningDismissed.set(false);
    const untilWarning=expiryAtMs-SESSION_WARNING_MS-Date.now();
    this.warnTimer=setTimeout(()=>this.startCountdown(),Math.max(0,untilWarning));
  }
  private startCountdown() {
    this.tickCountdown();
    this.countdownInterval=setInterval(()=>this.tickCountdown(),1000);
  }
  private tickCountdown() {
    if(this.expiryAtMs===null) return;
    const remainingMs=this.expiryAtMs-Date.now();
    if(remainingMs<=0) {clearInterval(this.countdownInterval);this.clear();return;}
    this.sessionWarningSecondsLeft.set(Math.round(remainingMs/1000));
  }
  clear() {
    clearTimeout(this.warnTimer);clearInterval(this.countdownInterval);
    this.expiryAtMs=null;this.sessionWarningSecondsLeft.set(null);this.sessionWarningDismissed.set(false);
    this.token.set(null);this.session.set(null);this.selectionToken.set(null);this.memberships.set([]);
  }
}
export const authInterceptor:HttpInterceptorFn=(request,next)=>{
  const auth=inject(AuthService);
  const api=runtimeConfig.apiBaseUrl;
  const belongs=request.url===api||request.url.startsWith(api+'/');
  const authorized=belongs&&auth.token()?request.clone({setHeaders:{Authorization:'Bearer '+auth.token()}}):request;
  return next(authorized).pipe(catchError(error=>{if(belongs&&error.status===401)auth.clear();return throwError(()=>error);}));
};
export const authGuard:CanActivateFn=()=>inject(AuthService).token()?true:inject(Router).createUrlTree(['/login']);
export const permissionGuard:CanActivateFn=(route)=>inject(AuthService).can(route.data['operation'])?true:inject(Router).createUrlTree(['/']);

