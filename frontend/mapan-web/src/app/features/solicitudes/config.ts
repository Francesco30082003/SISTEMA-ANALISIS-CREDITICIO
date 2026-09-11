import {ResourceConfig} from '../../shared/resource-page';
export const solicitudes:ResourceConfig={title:'Solicitudes',singular:'solicitud',path:'/solicitudes',id:'solicitudCreditoId',permission:'Solicitudes',paged:true,search:false,columns:['numeroSolicitud','fechaCreacion','montoSolicitado','plazoSolicitadoMeses','tasaInteresAnualPct','cuotaEstimada','totalAPagarEstimado'],fields:[
 {key:'numeroSolicitud',label:'Número de solicitud',required:true,max:50},
 {key:'sucursalId',label:'Sucursal',type:'select',required:true,source:'/organizacion/sucursales',id:'sucursalId',display:'nombre'},
 {key:'clienteId',label:'Cliente (identificación)',type:'select',required:true,source:'/clientes?page=1&size=100',id:'clienteId',display:'numeroIdentificacion'},
 {key:'productoCreditoId',label:'Producto',type:'select',required:true,source:'/productos',id:'productoCreditoId',display:'nombre'},
 {key:'montoSolicitado',label:'Monto solicitado',required:true,type:'number'},{key:'plazoSolicitadoMeses',label:'Plazo (meses)',required:true,type:'number'},
 {key:'destinoCredito',label:'Destino del crédito',max:500}]};
// Nota: la tasa y todo el desglose (capital/interés/cuota/total a pagar) los calcula siempre el backend
// a partir de la tasa vigente del producto — por eso no son campos editables aquí, solo columnas informativas.
