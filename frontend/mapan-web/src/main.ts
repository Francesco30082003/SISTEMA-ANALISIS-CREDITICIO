import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { runtimeConfig } from './app/core/config';
async function start() {
  const response = await fetch('/config.json', {cache:'no-store'});
  if (!response.ok) throw new Error('Configuración no disponible');
  const config = await response.json();
  const url = new URL(config.apiBaseUrl, location.origin);
  if (!['http:','https:'].includes(url.protocol) || url.username || url.password) throw new Error('URL API inválida');
  runtimeConfig.apiBaseUrl = url.toString().replace(/\/$/,'');
  await bootstrapApplication(App, appConfig);
}
start().catch(() => { document.body.textContent = 'No se pudo iniciar MAPAN. Verifique la configuración del servicio.'; });
