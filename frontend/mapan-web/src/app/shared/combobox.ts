import {Component, ElementRef, EventEmitter, HostListener, Input, Output, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';

// Reemplaza <input list="..."><datalist> nativo: el datalist del navegador se ve distinto en cada
// navegador, no respeta el sistema de diseño, y — el problema que reportó el usuario — al escribir
// filtra tan agresivo que "esconde" el resto de opciones mapeadas. Este combobox siempre deja ver la
// lista completa (con el texto escrito resaltando coincidencias por substring, no por prefijo), permite
// seleccionar con clic o teclado, y también admite un valor libre que no esté en la lista.
@Component({
  selector: 'mapan-combobox',
  imports: [FormsModule],
  template: `
<div class="combobox" [class.open]="open()">
  <input [id]="inputId" [(ngModel)]="text" (ngModelChange)="onType($event)" (focus)="onFocus()"
    (keydown.arrowdown)="move(1,$event)" (keydown.arrowup)="move(-1,$event)" (keydown.enter)="onEnter($event)" (keydown.escape)="close()"
    [placeholder]="placeholder" autocomplete="off" role="combobox" aria-autocomplete="list" [attr.aria-expanded]="open()">
  @if (open()) {
    <ul class="combobox-panel" role="listbox">
      @for (opt of visible(); track opt; let i = $index) {
        <li role="option" [attr.aria-selected]="i===active()" [class.active]="i===active()" (mousedown)="pick(opt)">{{ opt }}</li>
      } @empty {
        <li class="combobox-empty">Sin coincidencias — se guardará "{{ text }}" tal como lo escribiste.</li>
      }
    </ul>
  }
</div>
`,
})
export class Combobox {
  @Input() options: readonly string[] = [];
  @Input() placeholder = '';
  @Input() inputId = '';
  @Input() set value(v: string) { this.text = v ?? ''; }
  @Output() valueChange = new EventEmitter<string>();
  private host = inject(ElementRef<HTMLElement>);

  text = '';
  open = signal(false);
  active = signal(0);

  visible() {
    const q = this.text.trim().toLowerCase();
    const matches = q ? this.options.filter(o => o.toLowerCase().includes(q)) : this.options;
    return matches.length || q ? matches : this.options;
  }
  onFocus() { this.open.set(true); this.active.set(0); }
  onType(v: string) { this.text = v; this.valueChange.emit(v); this.open.set(true); this.active.set(0); }
  move(delta: number, ev: Event) { if (!this.open()) { this.open.set(true); return; } ev.preventDefault(); const n = this.visible().length; if (!n) return; this.active.update(a => (a + delta + n) % n); }
  onEnter(ev: Event) { if (!this.open()) return; const opts = this.visible(); if (opts[this.active()]) { ev.preventDefault(); this.pick(opts[this.active()]); } }
  pick(opt: string) { this.text = opt; this.valueChange.emit(opt); this.close(); }
  close() { this.open.set(false); }
  @HostListener('document:click', ['$event']) onDocClick(ev: MouseEvent) { if (!this.host.nativeElement.contains(ev.target)) this.close(); }
}
