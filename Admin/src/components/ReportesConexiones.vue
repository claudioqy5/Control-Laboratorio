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
  <div>
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Reporte de Conexiones</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">{{ totalConexiones }} Conexiones registradas</div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        <label for="fecha" style="font-size: 0.875rem; font-weight: 600; color: #475569;">Filtrar por fecha:</label>
        <div style="position: relative; display: flex; gap: 0.5rem;">
          <input 
            type="date" 
            id="fecha" 
            v-model="fechaSeleccionada"
            @change="fetchReporte"
            style="padding: 0.5rem 1rem; border-radius: 0.5rem; border: 1px solid #e5e7eb; font-size: 0.875rem; color: #111827;"
          />
          <button 
            @click="fetchReporte" 
            style="background: #f1f5f9; hover:bg-gray-200 text-gray-700 px-3 py-2 border: none; cursor: pointer; rounded-lg; border-radius: 0.5rem; text-sm font-medium transition-colors flex items-center gap-2; display: flex; align-items: center; justify-content: center; padding: 0.5rem 1rem;"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 6px;"><path d="M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8"/><path d="M21 3v5h-5"/></svg>
            Actualizar
          </button>
        </div>
      </div>
    </div>

    <div v-if="loading" style="padding: 3rem; text-align: center; color: #94a3b8;">
      Cargando datos...
    </div>
    
    <div v-else-if="sesiones.length === 0" style="padding: 3rem; text-align: center; color: #94a3b8;">
      No hay conexiones registradas en esta fecha.
    </div>
    
    <table v-else class="centered-table">
      <thead>
        <tr>
          <th>Alumno</th>
          <th>PC / Equipo</th>
          <th>Hora Inicio</th>
          <th>Hora Fin</th>
          <th>Uso (Mins)</th>
          <th>Estado</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="sesion in sesiones" :key="sesion.sesionId">
          <td>
            <div style="font-weight: 600; color: #111827;">{{ sesion.alumnoNombres }}</div>
            <div style="font-size: 0.75rem; color: #6b7280;">{{ sesion.alumnoCodigo }}</div>
          </td>
          <td>
            <div style="display: flex; align-items: center; justify-content: center; gap: 8px;">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6366f1" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
              <strong style="color: #111827;">{{ sesion.equipoRed }}</strong>
            </div>
          </td>
          <td style="color: #475569;">{{ formatTime(sesion.horaInicio) }}</td>
          <td style="color: #475569;">{{ formatTime(sesion.horaFin) }}</td>
          <td :style="{ color: sesion.duracionMinutos > 180 ? '#e11d48' : '#475569', fontWeight: '600' }">
            {{ sesion.duracionMinutos }} min
          </td>
          <td>
            <span v-if="sesion.estado === 'En línea'" style="background: #ecfdf5; color: #059669; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600; display: inline-flex; align-items: center; gap: 6px;">
              <span style="width: 6px; height: 6px; border-radius: 50%; background: #10b981;"></span>
              En línea
            </span>
            <span v-else style="background: #f1f5f9; color: #64748b; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600; display: inline-flex; align-items: center; gap: 6px;">
              Finalizado
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.centered-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03);
}

.centered-table th {
  background: #f8fafc;
  padding: 1rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: center;
  border-bottom: 2px solid #e2e8f0;
}

.centered-table td {
  padding: 1rem;
  text-align: center;
  border-bottom: 1px solid #f1f5f9;
  color: #1e293b;
  font-size: 0.9rem;
}

.centered-table tbody tr:hover {
  background: #f8fafc;
}
</style>
