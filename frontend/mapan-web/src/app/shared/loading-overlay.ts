import {Component, Injectable, computed, inject, signal} from '@angular/core';

// Contador, no booleano: si dos acciones se superponen (poco común pero posible, p.ej. guardar mientras
// una carga de sugerencias sigue en curso) el overlay no debe desaparecer hasta que TODAS terminen.
@Injectable({providedIn: 'root'})
export class LoadingOverlayService {
  private count = signal(0);
  message = signal('Procesando…');
  visible = computed(() => this.count() > 0);

  show(message = 'Procesando…') { this.message.set(message); this.count.update(n => n + 1); }
  hide() { this.count.update(n => Math.max(0, n - 1)); }

  async wrap<T>(promise: Promise<T>, message?: string): Promise<T> {
    this.show(message);
    try { return await promise; } finally { this.hide(); }
  }
}

@Component({
  selector: 'mapan-loading-overlay',
  template: `
@if (loading.visible()) {
<div class="loading-overlay" role="status" aria-live="polite">
  <div class="loading-overlay-card">
    <span class="brand-symbol">M</span>
    <div class="loading-spinner" aria-hidden="true"></div>
    <p>{{ loading.message() }}</p>
  </div>
</div>
}
`,
})
export class LoadingOverlayHost {
  loading = inject(LoadingOverlayService);
}
