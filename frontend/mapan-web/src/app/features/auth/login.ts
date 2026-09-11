import {Component,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Router} from '@angular/router';
import {AuthService} from '../../core/auth';
import {ApiClient,errorMessage} from '../../core/api';
import {AlertFocus} from '../../shared/alert-focus';

type Mode='login'|'forgot'|'reset';

@Component({selector:'mapan-login',imports:[FormsModule,AlertFocus],template:`
  <svg class="skyline" viewBox="0 0 1440 320" preserveAspectRatio="xMidYMax slice" aria-hidden="true" focusable="false">
    <rect x="40" y="150" width="120" height="150" />
    <rect x="55" y="130" width="14" height="20" /><rect x="80" y="130" width="14" height="20" /><rect x="105" y="130" width="14" height="20" /><rect x="130" y="130" width="14" height="20" />
    <polygon points="40,150 100,100 160,150" />
    <rect x="55" y="160" width="10" height="24" /><rect x="80" y="160" width="10" height="24" /><rect x="105" y="160" width="10" height="24" /><rect x="130" y="160" width="10" height="24" />
    <rect x="55" y="200" width="10" height="24" /><rect x="80" y="200" width="10" height="24" /><rect x="105" y="200" width="10" height="24" /><rect x="130" y="200" width="10" height="24" />
    <rect x="220" y="90" width="90" height="210" />
    <rect x="235" y="110" width="12" height="14" /><rect x="258" y="110" width="12" height="14" /><rect x="281" y="110" width="12" height="14" />
    <rect x="235" y="140" width="12" height="14" /><rect x="258" y="140" width="12" height="14" /><rect x="281" y="140" width="12" height="14" />
    <rect x="235" y="170" width="12" height="14" /><rect x="258" y="170" width="12" height="14" /><rect x="281" y="170" width="12" height="14" />
    <rect x="235" y="200" width="12" height="14" /><rect x="258" y="200" width="12" height="14" /><rect x="281" y="200" width="12" height="14" />
    <rect x="330" y="170" width="130" height="130" />
    <polygon points="330,170 395,115 460,170" />
    <rect x="350" y="180" width="12" height="34" /><rect x="378" y="180" width="12" height="34" /><rect x="406" y="180" width="12" height="34" /><rect x="434" y="180" width="12" height="34" />
    <rect x="1000" y="150" width="120" height="150" />
    <polygon points="1000,150 1060,100 1120,150" />
    <rect x="1015" y="160" width="10" height="24" /><rect x="1040" y="160" width="10" height="24" /><rect x="1065" y="160" width="10" height="24" /><rect x="1090" y="160" width="10" height="24" />
    <rect x="1015" y="200" width="10" height="24" /><rect x="1040" y="200" width="10" height="24" /><rect x="1065" y="200" width="10" height="24" /><rect x="1090" y="200" width="10" height="24" />
    <rect x="1160" y="90" width="90" height="210" />
    <rect x="1175" y="110" width="12" height="14" /><rect x="1198" y="110" width="12" height="14" /><rect x="1221" y="110" width="12" height="14" />
    <rect x="1175" y="140" width="12" height="14" /><rect x="1198" y="140" width="12" height="14" /><rect x="1221" y="140" width="12" height="14" />
    <rect x="1175" y="170" width="12" height="14" /><rect x="1198" y="170" width="12" height="14" /><rect x="1221" y="170" width="12" height="14" />
    <rect x="1275" y="170" width="130" height="130" />
    <polygon points="1275,170 1340,115 1405,170" />
    <rect x="1295" y="180" width="12" height="34" /><rect x="1323" y="180" width="12" height="34" /><rect x="1351" y="180" width="12" height="34" /><rect x="1379" y="180" width="12" height="34" />
    <rect x="0" y="298" width="1440" height="4" />
  </svg>
  <section class="login panel">
    <span class="eyebrow">MAPAN · ACCESO</span>
    @switch(mode()) {
      @case('login') {
        <h1>{{auth.selectionToken()?'Selecciona tu empresa':'Iniciar sesión'}}</h1>
        @if(auth.selectionToken()) {
          <p>Elige la empresa en la que vas a trabajar.</p>
          @for(company of auth.memberships();track company.empresaId) {
            <button class="company" [disabled]="busy()" (click)="select(company.empresaId)">{{company.nombreLegal}} <span>{{company.codigo}}</span></button>
          }
          <button class="secondary" (click)="auth.clear()">Volver al acceso</button>
        } @else {
          <form (ngSubmit)="login()">
            <label for="usuario">Usuario o correo</label><input id="usuario" name="usuario" [(ngModel)]="usuario" autocomplete="username" required maxlength="200">
            <label for="password">Contraseña</label><input id="password" name="password" [(ngModel)]="password" type="password" autocomplete="current-password" required maxlength="1024">
            <button [disabled]="busy()||!usuario||!password">{{busy()?'Verificando…':'Ingresar'}}</button>
          </form>
          <p class="forgot-link"><a href="javascript:void(0)" (click)="goForgot()">¿Olvidaste tu contraseña?</a></p>
        }
      }
      @case('forgot') {
        <h1>Recuperar contraseña</h1>
        <p>Escribe tu usuario o correo. Si existe una cuenta activa, te enviaremos un código de 6 dígitos.</p>
        <form (ngSubmit)="requestCode()">
          <label for="forgot-id">Usuario o correo</label><input id="forgot-id" name="forgotId" [(ngModel)]="resetIdentifier" required maxlength="200">
          <button [disabled]="busy()||!resetIdentifier">{{busy()?'Enviando…':'Enviar código'}}</button>
        </form>
        <p class="forgot-link"><a href="javascript:void(0)" (click)="goLogin()">Volver al acceso</a></p>
      }
      @case('reset') {
        <h1>Ingresa el código</h1>
        <p>Enviamos un código a la cuenta asociada a <strong>{{resetIdentifier}}</strong>. Expira en 15 minutos.</p>
        <form (ngSubmit)="resetPassword()">
          <label for="code">Código de 6 dígitos</label><input id="code" name="code" [(ngModel)]="code" required maxlength="6" inputmode="numeric" autocomplete="one-time-code">
          <label for="new-password">Nueva contraseña</label><input id="new-password" name="newPassword" type="password" [(ngModel)]="newPassword" required minlength="12" maxlength="1024" autocomplete="new-password">
          <button [disabled]="busy()||!code||!newPassword">{{busy()?'Guardando…':'Restablecer contraseña'}}</button>
        </form>
        <p class="forgot-link"><a href="javascript:void(0)" (click)="requestCode()">Reenviar código</a> · <a href="javascript:void(0)" (click)="goLogin()">Volver al acceso</a></p>
      }
    }
    @if(success()){<p role="status" class="success">{{success()}}</p>}
    @if(error()){<p role="alert" class="error" mapanAlertFocus>{{error()}}</p>}
  </section>`})
export class Login {
  auth=inject(AuthService);private router=inject(Router);private api=inject(ApiClient);
  usuario='';password='';busy=signal(false);error=signal('');success=signal('');
  mode=signal<Mode>('login');resetIdentifier='';code='';newPassword='';
  async login(){this.busy.set(true);this.error.set('');try{await this.auth.login(this.usuario,this.password);this.navigate();}catch(e){this.error.set(errorMessage(e));}finally{this.password='';this.busy.set(false);}}
  async select(id:string){this.busy.set(true);this.error.set('');try{await this.auth.select(id);this.navigate();}catch(e){this.error.set(errorMessage(e));}finally{this.busy.set(false);}}
  private navigate(){if(this.auth.token())void this.router.navigateByUrl('/');}
  goForgot(){this.mode.set('forgot');this.error.set('');this.success.set('');this.resetIdentifier=this.usuario;}
  goLogin(){this.mode.set('login');this.error.set('');this.success.set('');this.code='';this.newPassword='';}
  requestCode(){
    this.busy.set(true);this.error.set('');this.success.set('');
    this.api.post('/auth/olvide-clave',{identificador:this.resetIdentifier}).subscribe({
      next:()=>{this.busy.set(false);this.mode.set('reset');this.success.set('Si la cuenta existe, revisa tu correo. Puede tardar unos minutos.');},
      error:e=>{this.busy.set(false);this.error.set(errorMessage(e));},
    });
  }
  resetPassword(){
    this.busy.set(true);this.error.set('');this.success.set('');
    this.api.post('/auth/restablecer-clave',{identificador:this.resetIdentifier,codigo:this.code,nuevaClave:this.newPassword}).subscribe({
      next:()=>{this.busy.set(false);this.code='';this.newPassword='';this.goLogin();this.success.set('Contraseña actualizada. Ya puedes iniciar sesión.');},
      error:e=>{this.busy.set(false);this.error.set(errorMessage(e));},
    });
  }
}
