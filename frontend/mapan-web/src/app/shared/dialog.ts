import {Component, EventEmitter, Injectable, Input, Output, effect, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';

export interface ConfirmOptions {title?: string; confirmLabel?: string; cancelLabel?: string; tone?: 'default' | 'danger';}
export interface PromptOptions extends ConfirmOptions {placeholder?: string; required?: boolean;}

type DialogState =
  | {kind: 'confirm'; message: string; options: ConfirmOptions; resolve: (value: boolean) => void}
  | {kind: 'prompt'; message: string; options: PromptOptions; resolve: (value: string | null) => void}
  | null;

@Injectable({providedIn: 'root'})
export class DialogService {
  state = signal<DialogState>(null);

  confirm(message: string, options: ConfirmOptions = {}): Promise<boolean> {
    return new Promise(resolve => this.state.set({kind: 'confirm', message, options, resolve}));
  }

  prompt(message: string, options: PromptOptions = {}): Promise<string | null> {
    return new Promise(resolve => this.state.set({kind: 'prompt', message, options, resolve}));
  }

  respond(value: boolean | string | null) {
    const current = this.state();
    if (!current) return;
    this.state.set(null);
    if (current.kind === 'confirm') current.resolve(value === true);
    else current.resolve(typeof value === 'string' ? value : null);
  }
}

@Component({
  selector: 'mapan-dialog-host',
  imports: [FormsModule],
  template: `
@if (dialog.state(); as s) {
<div class="dialog-backdrop" (click)="dialog.respond(null)" (keydown.escape)="dialog.respond(null)">
  <div class="dialog-box" role="alertdialog" aria-modal="true" (click)="$event.stopPropagation()">
    @if (s.options.title) {<h2>{{ s.options.title }}</h2>}
    <p>{{ s.message }}</p>
    @if (s.kind === 'prompt') {
      <textarea rows="3" [(ngModel)]="value" [placeholder]="s.options.placeholder || ''" autofocus></textarea>
    }
    <div class="dialog-actions">
      <button class="secondary" (click)="dialog.respond(null)">{{ s.options.cancelLabel || 'Cancelar' }}</button>
      <button [class.danger]="s.options.tone === 'danger'"
              [disabled]="s.kind === 'prompt' && s.options.required === true && !value.trim()"
              (click)="submit(s)">{{ s.options.confirmLabel || (s.kind === 'prompt' ? 'Enviar' : 'Confirmar') }}</button>
    </div>
  </div>
</div>
}
`,
})
export class DialogHost {
  dialog = inject(DialogService);
  value = '';
  constructor() {
    effect(() => { if (this.dialog.state()) this.value = ''; });
  }
  submit(s: NonNullable<DialogState>) {
    this.dialog.respond(s.kind === 'prompt' ? this.value : true);
  }
}

// Contenedor de modal genérico para contenido arbitrario (formularios, detalle de un registro, etc.) —
// a diferencia de DialogService (confirm/prompt con Promise), esto es una ventana real que una página
// coloca en su propio template con `@if(condición){<mapan-modal (close)="...">...</mapan-modal>}`, para
// que "abrir X" sea visiblemente una ventana emergente y no un panel que aparece más abajo en la página.
@Component({
  selector: 'mapan-modal',
  template: `
<div class="dialog-backdrop" (click)="onBackdropClick()" (keydown.escape)="close.emit()">
  <div class="dialog-box modal-box" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
    @if (title) {<h2>{{ title }}</h2>}
    <ng-content></ng-content>
  </div>
</div>
`,
})
export class Modal {
  @Input() title = '';
  @Input() dismissible = true;
  @Output() close = new EventEmitter<void>();
  onBackdropClick() { if (this.dismissible) this.close.emit(); }
}
