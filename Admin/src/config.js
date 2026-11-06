// Prioriza la variable de entorno de Vite (VITE_API_URL).
// Si no se especifica ninguna, apunta por defecto al backend del VPS.
export const API_BASE_URL = 
  import.meta.env?.VITE_API_URL || 
  'https://bvefamurp.helifyferdigital.cloud';

