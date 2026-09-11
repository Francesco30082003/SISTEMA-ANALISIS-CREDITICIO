import {Directive,ElementRef,inject} from '@angular/core';
// Angular recrea este nodo cada vez que el bloque "@if(error())" pasa de oculto a visible (no solo
// cuando cambia el texto), así que el constructor se dispara de nuevo cada vez que aparece una alerta
// nueva — igual que "focus()" a un input inválido en JS puro. tabindex=-1 permite enfocar un <p> que
// normalmente no es enfocable, sin meterlo en el orden de tabulación del teclado.
@Directive({selector:'[mapanAlertFocus]',host:{tabindex:'-1',style:'outline:none'}})
export class AlertFocus {
 constructor(){
  const el=inject(ElementRef).nativeElement as HTMLElement;
  queueMicrotask(()=>{el.scrollIntoView({behavior:'smooth',block:'center'});el.focus({preventScroll:true});});
 }
}
