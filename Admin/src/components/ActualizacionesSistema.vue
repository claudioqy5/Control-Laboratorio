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
  <div>
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Actualización de Agentes</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">Gestiona la versión del software instalado en las computadoras del laboratorio.</div>
      </div>
      
      <div style="display: flex; gap: 1rem; align-items: center;">
        <div v-if="serverInfo" style="background: white; border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 0.5rem 1rem; display: flex; align-items: center; gap: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
          <div style="background: #f3f4f6; color: #4f46e5; padding: 0.5rem; border-radius: 50%; display: flex;">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
          </div>
          <div style="display: flex; flex-direction: column;">
            <span style="font-size: 0.7rem; color: #6b7280; text-transform: uppercase; font-weight: 600;">Versión en Servidor:</span>
            <div style="display: flex; align-items: baseline; gap: 8px;">
              <span style="font-size: 1.1rem; font-weight: 700; color: #111827;">v{{ serverInfo.version }}</span>
              <span style="font-size: 0.75rem; color: #9ca3af;">{{ formatDate(serverInfo.fecha) }}</span>
            </div>
            <span v-if="!serverInfo.archivoDisponible" style="color: #dc2626; font-size: 0.7rem; font-weight: 600; margin-top: 2px;">
              ⚠️ Falta Agent.exe en wwwroot/updates/
            </span>
          </div>
        </div>
      
        <button class="btn" style="background: #ffffff; color: #374151; border: 1px solid #d1d5db; font-weight: 700; padding: 0.5rem 1rem; border-radius: 0.5rem; display: flex; align-items: center; gap: 8px;" @click="loadData">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
          Refrescar
        </button>
        <button class="btn btn-primary" style="display: flex; align-items: center; gap: 8px;" @click="updateAll" :disabled="isUpdatingAll || (serverInfo && !serverInfo.archivoDisponible)">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
          {{ isUpdatingAll ? 'Enviando...' : 'Actualizar pendientes' }}
        </button>
      </div>
    </div>

    <div v-if="isLoading" style="text-align: center; padding: 3rem; color: #6b7280;">Cargando estado de las computadoras...</div>

    <table v-else class="centered-table">
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
          <td><strong style="color: #111827;">{{ equipo.nombreRed }}</strong></td>
          <td style="color: #6b7280; font-weight: 600;">v{{ equipo.versionInstalada }}</td>
          <td>{{ formatDate(equipo.fechaActualizacion) }}</td>
          <td>
            <span :style="{ 
              background: equipo.estado === 'alDia' ? '#f0fdf4' : equipo.estado === 'pendiente' ? '#fefce8' : equipo.estado === 'disponible' ? '#fef2f2' : '#f3f4f6', 
              color: equipo.estado === 'alDia' ? '#166534' : equipo.estado === 'pendiente' ? '#854d0e' : equipo.estado === 'disponible' ? '#991b1b' : '#374151', 
              padding: '4px 8px', borderRadius: '4px', fontSize: '0.85rem', fontWeight: '600' 
            }">
              <template v-if="equipo.estado === 'alDia'">✅ Al día</template>
              <template v-else-if="equipo.estado === 'pendiente'">🔄 Pendiente</template>
              <template v-else-if="equipo.estado === 'disponible'">⚠️ Antiguo</template>
              <template v-else-if="equipo.estado === 'sinSenal'">❓ Sin datos</template>
            </span>
          </td>
          <td>
            <button 
              class="btn" 
              style="background: #ffffff; color: #4f46e5; border: 1px solid #4f46e5; font-weight: 600; padding: 0.3rem 0.8rem; border-radius: 0.4rem; font-size: 0.8rem;"
              @click="updatePC(equipo.nombreRed)"
              :disabled="equipo.estado === 'alDia' || equipo.estado === 'pendiente' || (serverInfo && !serverInfo.archivoDisponible)"
              :style="{ opacity: (equipo.estado === 'alDia' || equipo.estado === 'pendiente' || (serverInfo && !serverInfo.archivoDisponible)) ? '0.5' : '1', cursor: (equipo.estado === 'alDia' || equipo.estado === 'pendiente' || (serverInfo && !serverInfo.archivoDisponible)) ? 'not-allowed' : 'pointer' }">
              Actualizar
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.centered-table th, .centered-table td {
  text-align: center;
}
</style>
