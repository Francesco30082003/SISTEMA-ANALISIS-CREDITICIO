import {ResourceConfig} from '../../shared/resource-page';
export const clientes:ResourceConfig={title:'Clientes',singular:'cliente',path:'/clientes',id:'clienteId',permission:'Clientes',paged:true,search:true,columns:['numeroIdentificacion','nombres','apellidos','razonSocial','telefono'],fields:[
 {key:'tipoPersona',label:'Tipo de persona',type:'select',required:true,options:[{value:'NATURAL',label:'Natural'},{value:'JURIDICA',label:'Jurídica'}]},
 {key:'tipoIdentificacion',label:'Tipo de identificación',required:true,max:20},{key:'numeroIdentificacion',label:'Identificación',required:true,max:30},
 {key:'nombres',label:'Nombres',max:150},{key:'apellidos',label:'Apellidos',max:150},{key:'razonSocial',label:'Razón social',max:200},{key:'fechaNacimiento',label:'Fecha de nacimiento',type:'date'},
 {key:'telefono',label:'Teléfono',max:30},{key:'correo',label:'Correo',max:200}]};
