import {inject,Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {runtimeConfig} from './config';
@Injectable({providedIn:'root'})
export class ApiClient {
  private http=inject(HttpClient);
  get<T>(path:string) {return this.http.get<T>(runtimeConfig.apiBaseUrl+path);}
  download(path:string) {return this.http.get(runtimeConfig.apiBaseUrl+path,{responseType:'blob'});}
  delete(path:string) {return this.http.delete(runtimeConfig.apiBaseUrl+path);}
  post<T>(path:string,body:unknown) {return this.http.post<T>(runtimeConfig.apiBaseUrl+path,body);}
  put<T>(path:string,body:unknown) {return this.http.put<T>(runtimeConfig.apiBaseUrl+path,body);}
  // Angular sends a raw string body as Content-Type: text/plain, unstringified — ASP.NET's
  // [FromBody] string action needs actual JSON (a quoted string) with Content-Type: application/json.
  postRaw<T>(path:string,value:unknown) {return this.http.post<T>(runtimeConfig.apiBaseUrl+path,JSON.stringify(value),{headers:{'Content-Type':'application/json'}});}
}
export function errorMessage(error:unknown):string {
  const value=error as {status?:number;error?:{title?:string}};
  return value.error?.title??(value.status===0?'No se pudo conectar con el servicio.':'No se pudo completar la operación.');
}
