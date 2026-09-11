import {signal,computed} from '@angular/core';

// Paginador del lado del cliente: para listas que el backend ya entrega completas (mora, cosechas,
// desempeño de analistas, y las listas internas del expediente) — evita cambiar cada endpoint a
// paginado en el servidor solo para poder mostrar la grilla en páginas más cómodas de revisar.
export class ClientPager<T> {
  private all=signal<T[]>([]);
  page=signal(1);
  pageSize;
  constructor(initialPageSize=10){this.pageSize=signal(initialPageSize);}
  set(items:T[]){this.all.set(items);if(this.page()>this.totalPages())this.page.set(1);}
  setPageSize(n:number){this.pageSize.set(n);this.page.set(1);}
  total=computed(()=>this.all().length);
  totalPages=computed(()=>Math.max(1,Math.ceil(this.total()/this.pageSize())));
  items=computed(()=>{const start=(this.page()-1)*this.pageSize();return this.all().slice(start,start+this.pageSize());});
  next(){if(this.page()<this.totalPages())this.page.update(p=>p+1);}
  prev(){if(this.page()>1)this.page.update(p=>p-1);}
}
