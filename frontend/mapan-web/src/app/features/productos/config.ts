import {ResourceConfig} from '../../shared/resource-page';
export const productos:ResourceConfig={title:'Productos de crédito',singular:'producto',path:'/productos',id:'productoCreditoId',permission:'Productos',paged:false,search:false,columns:['codigo','nombre','categoria','monedaCodigo','tasaInteresAnualPct','montoMinimo','montoMaximo'],fields:[
 {key:'codigo',label:'Código',required:true,max:40},{key:'nombre',label:'Nombre',required:true,max:150},{key:'descripcion',label:'Descripción',max:500},
 {key:'categoria',label:'Categoría',type:'select',options:[{value:'MICROCREDITO',label:'Microcrédito'},{value:'COMERCIAL',label:'Comercial'},{value:'CONSUMO',label:'Consumo'},{value:'OTRO',label:'Otro'}]},
 {key:'montoMinimo',label:'Monto mínimo',type:'number'},{key:'montoMaximo',label:'Monto máximo',type:'number'},
 {key:'plazoMinimoMeses',label:'Plazo mínimo (meses)',type:'number'},{key:'plazoMaximoMeses',label:'Plazo máximo (meses)',type:'number'},
 {key:'monedaCodigo',label:'Código de moneda',required:true,max:3},
 {key:'tasaInteresAnualPct',label:'Tasa de interés anual (%)',required:true,type:'number'},
 {key:'tipoTasa',label:'Tipo de tasa',type:'select',options:[{value:'FIJA',label:'Fija'},{value:'VARIABLE',label:'Variable'}]},
 {key:'vigenteDesde',label:'Tasa vigente desde',type:'date'},{key:'vigenteHasta',label:'Tasa vigente hasta',type:'date'}]};
