<script setup>
import { ref, onMounted } from 'vue'
import { API_BASE_URL } from '../config.js'

const sesiones = ref([])
const totalConexiones = ref(0)
const loading = ref(true)
const fechaSeleccionada = ref(new Date().toISOString().split('T')[0]) // Hoy por defecto

const fetchReporte = async () => {
  loading.value = true
  try {
    const response = await fetch(`${API_BASE_URL}/api/reportes/conexiones?fecha=${fechaSeleccionada.value}`)
    const data = await response.json()
    sesiones.value = data.sesiones
    totalConexiones.value = data.totalConexiones
  } catch (error) {
    console.error("Error cargando reporte:", error)
  } finally {
    loading.value = false
  }
}

const formatTime = (dateString) => {
  if (!dateString) return '-'
  const d = new Date(dateString)
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  fetchReporte()
})
</script>

<template>
  <div class="p-6">
    <div class="flex justify-between items-center mb-6">
      <div>
        <h2 class="text-2xl font-bold text-gray-800">Reporte de Conexiones</h2>
        <p class="text-gray-500">Historial de acceso al laboratorio</p>
      </div>
      <div class="flex gap-4 items-center">
        <label for="fecha" class="text-sm font-medium text-gray-700">Filtrar por fecha:</label>
        <input 
          type="date" 
          id="fecha" 
          v-model="fechaSeleccionada"
          @change="fetchReporte"
          class="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500"
        />
        <button 
          @click="fetchReporte" 
          class="bg-gray-100 hover:bg-gray-200 text-gray-700 px-3 py-2 rounded-lg text-sm font-medium transition-colors flex items-center gap-2"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8"/><path d="M21 3v5h-5"/></svg>
          Actualizar
        </button>
      </div>
    </div>

    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="p-4 border-b border-gray-100 bg-gray-50 flex justify-between items-center">
        <span class="font-medium text-gray-700">Total de conexiones en la fecha: <span class="text-rose-600 font-bold">{{ totalConexiones }}</span></span>
      </div>
      
      <div v-if="loading" class="p-12 text-center text-gray-400">
        <svg class="animate-spin h-8 w-8 mx-auto mb-4 text-gray-300" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        Cargando datos...
      </div>
      
      <div v-else-if="sesiones.length === 0" class="p-12 text-center text-gray-500">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" class="mx-auto mb-4 text-gray-300"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
        No hay conexiones registradas en esta fecha.
      </div>
      
      <table v-else class="w-full text-left border-collapse">
        <thead>
          <tr class="bg-gray-50 text-gray-500 text-xs uppercase tracking-wider">
            <th class="px-6 py-4 font-medium border-b border-gray-100">Alumno</th>
            <th class="px-6 py-4 font-medium border-b border-gray-100">PC / Equipo</th>
            <th class="px-6 py-4 font-medium border-b border-gray-100">Hora Inicio</th>
            <th class="px-6 py-4 font-medium border-b border-gray-100">Hora Fin</th>
            <th class="px-6 py-4 font-medium border-b border-gray-100">Uso (Mins)</th>
            <th class="px-6 py-4 font-medium border-b border-gray-100 text-right">Estado</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-50 text-sm">
          <tr v-for="sesion in sesiones" :key="sesion.sesionId" class="hover:bg-gray-50 transition-colors">
            <td class="px-6 py-4">
              <div class="font-medium text-gray-900">{{ sesion.alumnoNombres }}</div>
              <div class="text-xs text-gray-500">{{ sesion.alumnoCodigo }}</div>
            </td>
            <td class="px-6 py-4">
              <div class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-blue-50 text-blue-700 text-xs font-semibold">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
                {{ sesion.equipoRed }}
              </div>
            </td>
            <td class="px-6 py-4 text-gray-600 font-mono text-xs">{{ formatTime(sesion.horaInicio) }}</td>
            <td class="px-6 py-4 text-gray-600 font-mono text-xs">{{ formatTime(sesion.horaFin) }}</td>
            <td class="px-6 py-4 font-medium" :class="sesion.duracionMinutos > 180 ? 'text-rose-600' : 'text-gray-700'">
              {{ sesion.duracionMinutos }} min
            </td>
            <td class="px-6 py-4 text-right">
              <span v-if="sesion.estado === 'En línea'" class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-50 text-emerald-700 text-xs font-medium">
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
                En línea
              </span>
              <span v-else class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-gray-100 text-gray-600 text-xs font-medium">
                Finalizado
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
/* Tailwind classes assume config setup, using arbitrary values where needed */
</style>
