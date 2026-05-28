<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const serverInfo = ref(null)
const equiposStatus = ref([])
const isLoading = ref(true)
const isUpdatingAll = ref(false)
const pollingInterval = ref(null)

const loadData = async () => {
  try {
    const [infoRes, statusRes] = await Promise.all([
      axios.get(`${API_BASE_URL}/api/update/info`),
      axios.get(`${API_BASE_URL}/api/update/status`)
    ])
    
    serverInfo.value = infoRes.data
    equiposStatus.value = statusRes.data
  } catch (error) {
    console.error('Error cargando datos de actualizaciones:', error)
  } finally {
    isLoading.value = false
  }
}

const updatePC = async (nombreRed) => {
  if (!confirm(`¿Estás seguro de enviar la actualización a ${nombreRed}?`)) return
  
  try {
    const res = await axios.post(`${API_BASE_URL}/api/update/push/${nombreRed}`)
    alert(res.data.message)
    loadData()
  } catch (error) {
    alert(error.response?.data?.message || 'Error al intentar actualizar.')
  }
}

const updateAll = async () => {
  if (!confirm('¿Estás seguro de enviar la actualización a TODAS las computadoras que no estén al día?')) return
  
  isUpdatingAll.value = true
  try {
    const res = await axios.post(`${API_BASE_URL}/api/update/push-all`)
    alert(res.data.message)
    loadData()
  } catch (error) {
    alert(error.response?.data?.message || 'Error masivo de actualización.')
  } finally {
    isUpdatingAll.value = false
  }
}

const formatDate = (dateString) => {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleString('es-PE')
}

onMounted(() => {
  loadData()
  // Refresco automático cada 30 segundos
  pollingInterval.value = setInterval(loadData, 30000)
})

onUnmounted(() => {
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
  }
})
</script>

<template>
  <div class="actualizaciones-container">
    <div class="header">
      <div>
        <h2>Actualización de Agentes</h2>
        <p class="subtitle">Gestiona la versión del software instalado en las computadoras del laboratorio.</p>
      </div>
      
      <div v-if="serverInfo" class="server-status-card">
        <div class="status-icon">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
        </div>
        <div class="status-info">
          <span class="label">Versión disponible en Servidor:</span>
          <span class="version">v{{ serverInfo.version }}</span>
          <span class="date">{{ formatDate(serverInfo.fecha) }}</span>
          <span v-if="!serverInfo.archivoDisponible" class="warning-text">
            ⚠️ Falta archivo Agent.exe en wwwroot/updates/
          </span>
        </div>
      </div>
    </div>

    <div class="actions-bar">
      <button class="btn-primary" @click="updateAll" :disabled="isUpdatingAll || (serverInfo && !serverInfo.archivoDisponible)">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
        {{ isUpdatingAll ? 'Enviando...' : 'Actualizar todas las pendientes' }}
      </button>
      <button class="btn-secondary" @click="loadData">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
        Refrescar
      </button>
    </div>

    <div v-if="isLoading" class="loading">Cargando estado de las computadoras...</div>

    <div v-else class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Computadora</th>
            <th>Versión Instalada</th>
            <th>Último Reporte</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="equipo in equiposStatus" :key="equipo.nombreRed">
            <td class="font-medium">{{ equipo.nombreRed }}</td>
            <td>v{{ equipo.versionInstalada }}</td>
            <td>{{ formatDate(equipo.fechaActualizacion) }}</td>
            <td>
              <span class="badge" :class="equipo.estado">
                <template v-if="equipo.estado === 'alDia'">✅ Al día</template>
                <template v-else-if="equipo.estado === 'pendiente'">🔄 Actualización pendiente</template>
                <template v-else-if="equipo.estado === 'disponible'">⚠️ Versión antigua</template>
                <template v-else-if="equipo.estado === 'sinSenal'">❓ Sin datos</template>
              </span>
            </td>
            <td>
              <button 
                class="btn-sm" 
                @click="updatePC(equipo.nombreRed)"
                :disabled="equipo.estado === 'alDia' || equipo.estado === 'pendiente' || (serverInfo && !serverInfo.archivoDisponible)">
                Actualizar
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.actualizaciones-container {
  padding: 1.5rem;
  max-width: 1200px;
  margin: 0 auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}

h2 {
  font-size: 1.5rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.5rem 0;
}

.subtitle {
  color: #6b7280;
  margin: 0;
}

.server-status-card {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 1rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 1px 3px rgba(0,0,0,0.05);
}

.status-icon {
  background: #f3f4f6;
  color: #4f46e5;
  padding: 0.75rem;
  border-radius: 50%;
  display: flex;
}

.status-info {
  display: flex;
  flex-direction: column;
}

.label {
  font-size: 0.75rem;
  color: #6b7280;
  text-transform: uppercase;
  font-weight: 600;
}

.version {
  font-size: 1.25rem;
  font-weight: 700;
  color: #111827;
}

.date {
  font-size: 0.875rem;
  color: #6b7280;
}

.warning-text {
  color: #dc2626;
  font-size: 0.75rem;
  font-weight: 600;
  margin-top: 0.25rem;
}

.actions-bar {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.btn-primary {
  background: #4f46e5;
  color: white;
  border: none;
  padding: 0.625rem 1.25rem;
  border-radius: 0.375rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: background-color 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background: #4338ca;
}

.btn-primary:disabled {
  background: #9ca3af;
  cursor: not-allowed;
}

.btn-secondary {
  background: white;
  color: #374151;
  border: 1px solid #d1d5db;
  padding: 0.625rem 1.25rem;
  border-radius: 0.375rem;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: background-color 0.2s;
}

.btn-secondary:hover {
  background: #f9fafb;
}

.btn-sm {
  background: white;
  border: 1px solid #d1d5db;
  padding: 0.375rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  cursor: pointer;
}

.btn-sm:hover:not(:disabled) {
  background: #f9fafb;
}

.btn-sm:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.table-container {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  overflow: hidden;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th, .data-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #e5e7eb;
}

.data-table th {
  background: #f9fafb;
  font-weight: 600;
  color: #374151;
  font-size: 0.875rem;
  text-transform: uppercase;
}

.font-medium {
  font-weight: 500;
  color: #111827;
}

.badge {
  padding: 0.25rem 0.75rem;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 600;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
}

.badge.alDia {
  background: #dcfce7;
  color: #166534;
}

.badge.pendiente {
  background: #fef08a;
  color: #854d0e;
}

.badge.disponible {
  background: #fee2e2;
  color: #991b1b;
}

.badge.sinSenal {
  background: #f3f4f6;
  color: #374151;
}

.loading {
  text-align: center;
  padding: 3rem;
  color: #6b7280;
}
</style>
