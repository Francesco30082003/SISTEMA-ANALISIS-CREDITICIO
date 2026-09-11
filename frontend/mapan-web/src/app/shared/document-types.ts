// Catálogo único de tipos de documento sugeridos (autocompletado, no una lista cerrada — el campo real
// sigue siendo texto libre en el backend). Se define una sola vez aquí para que agregar/ajustar un tipo
// no implique tocar el marcado de cada lugar que lo usa. El rediseño visual de cómo se presenta esta
// lista queda para la fase de rediseño UX/UI integral — esto solo corrige dónde vive el dato.
export const DOCUMENT_TYPE_SUGGESTIONS: readonly string[] = [
  'Cédula',
  'RUC',
  'Certificado de ingresos',
  'Certificado de deuda',
  'Rol de pagos',
  'Reporte de buró de crédito',
  'Reporte Equifax',
  'Reporte de Aval',
  'Foto de visita de negocio',
  'Foto del negocio',
];
