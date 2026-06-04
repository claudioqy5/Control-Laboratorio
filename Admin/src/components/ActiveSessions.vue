<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const activeSessions = ref([])

const fetchActive = async () => {
  const res = await axios.get(`${API_BASE_URL}/api/stats/active`)
  activeSessions.value = res.data
}

const formatTime12h = (isoString) => {
  if (!isoString) return ''
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}

onMounted(() => {
  fetchActive()
  // Refresh every 30 seconds
  setInterval(fetchActive, 30000)
})
</script>

<template>
  <div class="card">
    <h2>Estudiantes en Línea</h2>
    <p style="color: #64748b; margin-bottom: 1rem;">Monitoreo en tiempo real de equipos ocupados.</p>

    <table>
      <thead>
        <tr>
          <th style="width: 50px;">#</th>
          <th>Equipo</th>
          <th>Alias</th>
          <th>Estudiante</th>
          <th>Hora Ingreso</th>
          <th>Sesión Actual</th>
          <th>Estado</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(s, index) in activeSessions" :key="s.sesionID">
          <td style="color: #94a3b8; font-weight: 600;">{{ index + 1 }}</td>
          <td><span style="background: #ccfbf1; color: #0f766e; padding: 2px 8px; border-radius: 4px; font-weight: bold; border: 1px solid #5eead4;">{{ s.equipo }}</span></td>
          <td>
            <span v-if="s.alias" style="background: #f1f5f9; color: #475569; padding: 2px 8px; border-radius: 4px; font-size: 0.85rem; font-weight: 600; border: 1px solid #cbd5e1;">
              {{ s.alias }}
            </span>
            <span v-else style="color: #cbd5e1; font-style: italic;">—</span>
          </td>
          <td>{{ s.alumno }}</td>
          <td>{{ formatTime12h(s.horaInicio) }}</td>
          <td>
            <span style="background: #eff6ff; color: #1d4ed8; padding: 4px 8px; border-radius: 4px; font-size: 0.85rem; font-weight: 600; white-space: nowrap;">
              Sesión {{ Math.floor((s.limiteDiarioSegundos || 10800) / 10800) }}
            </span>
          </td>
          <td><span style="color: #10b981; font-weight: bold;">● Conectado</span></td>
        </tr>
        <tr v-if="activeSessions.length === 0">
          <td colspan="7" style="text-align: center; padding: 2rem; color: #64748b;">No hay sesiones activas en este momento.</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
