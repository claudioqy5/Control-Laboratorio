<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const activeSessions = ref([])

const fetchActive = async () => {
  const res = await axios.get('https://bvefamurp.helifyferdigital.cloud/api/stats/active')
  activeSessions.value = res.data
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
          <th>Equipo</th>
          <th>Estudiante</th>
          <th>Hora Ingreso</th>
          <th>Estado</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="s in activeSessions" :key="s.sesionID">
          <td><span style="background: #ccfbf1; color: #0f766e; padding: 2px 8px; border-radius: 4px; font-weight: bold; border: 1px solid #5eead4;">{{ s.equipo }}</span></td>
          <td>{{ s.alumno }}</td>
          <td>{{ new Date(s.horaInicio).toLocaleTimeString() }}</td>
          <td><span style="color: #10b981; font-weight: bold;">● Conectado</span></td>
        </tr>
        <tr v-if="activeSessions.length === 0">
          <td colspan="4" style="text-align: center; padding: 2rem; color: #64748b;">No hay sesiones activas en este momento.</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
