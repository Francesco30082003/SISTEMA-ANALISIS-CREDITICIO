import {Component, inject} from '@angular/core';
import {AuthService} from '../core/auth';

@Component({
  selector: 'mapan-session-expiry-modal',
  template: `
@if (!auth.sessionWarningDismissed() && auth.sessionWarningSecondsLeft(); as seconds) {
<div class="dialog-backdrop">
  <div class="dialog-box" role="alertdialog" aria-modal="true">
    <h2>Tu sesión está por expirar</h2>
    <p>Por seguridad, tu sesión se cerrará en <strong>{{ minutes(seconds) }}:{{ secs(seconds) }}</strong> minutos.</p>
    <div class="dialog-actions">
      <button class="secondary" (click)="auth.dismissWarning()">Seguir trabajando</button>
      <button class="danger" (click)="auth.clear()">Cerrar sesión ahora</button>
    </div>
  </div>
</div>
}
`,
})
export class SessionExpiryModal {
  auth = inject(AuthService);
  minutes(seconds: number) { return Math.floor(seconds / 60); }
  secs(seconds: number) { return (seconds % 60).toString().padStart(2, '0'); }
}
