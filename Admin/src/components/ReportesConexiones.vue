<script setup>
import { ref, onMounted } from 'vue'
import { API_BASE_URL } from '../config.js'

const activeTab = ref('conexiones') // 'conexiones' o 'escaneos'
const alumnosGrouped = ref([])
const totalConexiones = ref(0)
const loading = ref(true)
const fechaSeleccionada = ref(new Date().toISOString().split('T')[0]) // Hoy por defecto

// Variables para escaneos
const escaneos = ref([])
const totalEscaneos = ref(0)
const verTodosEscaneos = ref(false)

const fetchReporte = async () => {
  loading.value = true
  try {
    const response = await fetch(`${API_BASE_URL}/api/reportes/conexiones?fecha=${fechaSeleccionada.value}`)
    const data = await response.json()
    totalConexiones.value = data.totalConexiones

    // Group sessions by alumnoCodigo
    const groups = {}
    data.sesiones.forEach(sesion => {
      const codigo = sesion.alumnoCodigo || 'N/A'
      if (!groups[codigo]) {
        groups[codigo] = {
          alumnoCodigo: codigo,
          alumnoNombres: sesion.alumnoNombres,
          sesiones: []
        }
      }
      groups[codigo].sesiones.push(sesion)
    })

    // For each student, sort sessions by horaInicio descending (latest first)
    const list = Object.values(groups).map(g => {
      g.sesiones.sort((a, b) => new Date(b.horaInicio) - new Date(a.horaInicio))
      g.ultimaSesion = g.sesiones[0]
      g.demasSesiones = g.sesiones.slice(1)
      g.isExpanded = false
      return g
    })

    // Sort students: currently online first, then by latest session start time
    list.sort((a, b) => {
      const aOnline = a.ultimaSesion.estado === 'En línea' ? 1 : 0
      const bOnline = b.ultimaSesion.estado === 'En línea' ? 1 : 0
      if (aOnline !== bOnline) return bOnline - aOnline

      return new Date(b.ultimaSesion.horaInicio) - new Date(a.ultimaSesion.horaInicio)
    })

    alumnosGrouped.value = list
  } catch (error) {
    console.error("Error cargando reporte:", error)
  } finally {
    loading.value = false
  }
}

const fetchEscaneos = async () => {
  loading.value = true
  try {
    const paramFecha = verTodosEscaneos.value ? 'all' : fechaSeleccionada.value
    const response = await fetch(`${API_BASE_URL}/api/reportes/escaneos?fecha=${paramFecha}`)
    const data = await response.json()
    totalEscaneos.value = data.totalEscaneos
    escaneos.value = data.escaneos
  } catch (error) {
    console.error("Error cargando reporte de escaneos:", error)
  } finally {
    loading.value = false
  }
}

const handleDateOrTabChange = () => {
  if (activeTab.value === 'conexiones') {
    fetchReporte()
  } else {
    fetchEscaneos()
  }
}

const formatTime = (dateString) => {
  if (!dateString) return '-'
  const d = new Date(dateString)
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

const formatDateAndTime = (dateString) => {
  if (!dateString) return '-'
  const d = new Date(dateString)
  return d.toLocaleDateString([], { day: '2-digit', month: '2-digit', year: 'numeric' }) + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  handleDateOrTabChange()
})
</script>

<template>
  <div>
    <!-- Menú de Pestañas -->
    <div style="display: flex; gap: 1rem; border-bottom: 2px solid #f1f5f9; margin-bottom: 1.5rem;">
      <button 
        @click="activeTab = 'conexiones'; handleDateOrTabChange()" 
        :style="{
          padding: '0.75rem 1rem', 
          border: 'none', 
          background: 'transparent', 
          color: activeTab === 'conexiones' ? '#6366f1' : '#64748b', 
          borderBottom: activeTab === 'conexiones' ? '2px solid #6366f1' : '2px solid transparent', 
          fontWeight: '600', 
          cursor: 'pointer',
          fontSize: '0.9rem'
        }"
      >
        Reporte de Conexiones
      </button>
      <button 
        @click="activeTab = 'escaneos'; handleDateOrTabChange()" 
        :style="{
          padding: '0.75rem 1rem', 
          border: 'none', 
          background: 'transparent', 
          color: activeTab === 'escaneos' ? '#6366f1' : '#64748b', 
          borderBottom: activeTab === 'escaneos' ? '2px solid #6366f1' : '2px solid transparent', 
          fontWeight: '600', 
          cursor: 'pointer',
          fontSize: '0.9rem'
        }"
      >
        Reporte de Escaneos (Google Vision)
      </button>
    </div>

    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">
          {{ activeTab === 'conexiones' ? 'Reporte de Conexiones' : 'Reporte de Escaneos de Carné' }}
        </h2>
        <div style="color: #6b7280; font-size: 0.875rem;">
          <template v-if="activeTab === 'conexiones'">
            {{ totalConexiones }} Conexiones registradas ({{ alumnosGrouped.length }} alumnos hoy)
          </template>
          <template v-else>
            {{ totalEscaneos }} Escaneos de carné con Google Vision realizados {{ verTodosEscaneos ? 'en total' : 'en esta fecha' }}
          </template>
        </div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        <!-- Botón para ver todo el historial (solo en pestaña de escaneos) -->
        <button 
          v-if="activeTab === 'escaneos'"
          @click="verTodosEscaneos = !verTodosEscaneos; handleDateOrTabChange()"
          :style="{
            background: verTodosEscaneos ? '#6366f1' : '#f1f5f9',
            color: verTodosEscaneos ? 'white' : '#475569',
            border: '1px solid ' + (verTodosEscaneos ? '#6366f1' : '#e2e8f0'),
            cursor: 'pointer',
            borderRadius: '0.5rem',
            fontSize: '0.875rem',
            fontWeight: '600',
            padding: '0.5rem 1rem',
            transition: 'all 0.2s'
          }"
        >
          {{ verTodosEscaneos ? 'Filtrar por Fecha' : 'Ver Todo el Historial' }}
        </button>

        <label v-if="activeTab === 'conexiones' || !verTodosEscaneos" for="fecha" style="font-size: 0.875rem; font-weight: 600; color: #475569;">Filtrar por fecha:</label>
        <div v-if="activeTab === 'conexiones' || !verTodosEscaneos" style="position: relative; display: flex; gap: 0.5rem;">
          <input 
            type="date" 
            id="fecha" 
            v-model="fechaSeleccionada"
            @change="handleDateOrTabChange"
            style="padding: 0.5rem 1rem; border-radius: 0.5rem; border: 1px solid #e5e7eb; font-size: 0.875rem; color: #111827;"
          />
          <button 
            @click="handleDateOrTabChange" 
            style="background: #f1f5f9; border: 1px solid #e2e8f0; cursor: pointer; border-radius: 0.5rem; font-size: 0.875rem; font-weight: 600; color: #475569; display: flex; align-items: center; justify-content: center; padding: 0.5rem 1rem; transition: background 0.2s;"
            onmouseover="this.style.background='#e2e8f0'" onmouseout="this.style.background='#f1f5f9'"
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
    
    <template v-else>
      <!-- VISTA DE CONEXIONES -->
      <div v-if="activeTab === 'conexiones'">
        <div v-if="alumnosGrouped.length === 0" style="padding: 3rem; text-align: center; color: #94a3b8;">
          No hay conexiones registradas en esta fecha.
        </div>
        
        <table v-else class="centered-table">
          <thead>
            <tr>
              <th style="text-align: left; padding-left: 2rem;">Alumno</th>
              <th>PC / Equipo</th>
              <th>Hora Inicio</th>
              <th>Hora Fin</th>
              <th>Uso (Mins)</th>
              <th>Sesión Actual</th>
              <th>Estado</th>
            </tr>
          </thead>
          <template v-for="alumno in alumnosGrouped" :key="alumno.alumnoCodigo">
            <tr 
              :class="{ 'has-multiple': alumno.sesiones.length > 1, 'is-expanded': alumno.isExpanded }"
              @click="alumno.sesiones.length > 1 ? alumno.isExpanded = !alumno.isExpanded : null"
              :style="{ cursor: alumno.sesiones.length > 1 ? 'pointer' : 'default' }"
            >
              <td style="text-align: left; padding-left: 1.5rem;">
                <div style="display: flex; align-items: center; gap: 10px;">
                  <!-- Chevron for multiple sessions -->
                  <div 
                    v-if="alumno.sesiones.length > 1" 
                    class="chevron-icon" 
                    style="display: flex; align-items: center; justify-content: center;"
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#6366f1" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg>
                  </div>
                  <div v-else style="width: 16px;"></div> <!-- Spacer to align names -->
                  
                  <div style="flex: 1;">
                    <div style="font-weight: 600; color: #111827; line-height: 1.2;">{{ alumno.alumnoNombres }}</div>
                    <div style="display: flex; align-items: center; gap: 8px; margin-top: 4px;">
                      <span style="font-size: 0.75rem; color: #6b7280;">{{ alumno.alumnoCodigo }}</span>
                      <span v-if="alumno.sesiones.length > 1" class="multi-connection-badge">
                        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="margin-right: 2px;"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                        {{ alumno.sesiones.length }} conex.
                      </span>
                    </div>
                  </div>
                </div>
              </td>
              <td>
                <div style="display: flex; align-items: center; justify-content: center; gap: 8px;">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6366f1" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
                  <strong :title="alumno.ultimaSesion.equipoRed" style="color: #111827;">{{ alumno.ultimaSesion.equipoAlias || alumno.ultimaSesion.equipoRed }}</strong>
                </div>
              </td>
              <td style="color: #475569;">{{ formatTime(alumno.ultimaSesion.horaInicio) }}</td>
              <td style="color: #475569;">{{ formatTime(alumno.ultimaSesion.horaFin) }}</td>
              <td :style="{ color: alumno.ultimaSesion.duracionMinutos > 180 ? '#e11d48' : '#475569', fontWeight: '600' }">
                {{ alumno.ultimaSesion.duracionMinutos }} min
              </td>
              <td>
                <span style="background: #eff6ff; color: #1d4ed8; padding: 4px 8px; border-radius: 4px; font-size: 0.85rem; font-weight: 600; white-space: nowrap;">
                  Sesión {{ Math.floor((alumno.ultimaSesion.limiteDiarioSegundos || 10800) / 10800) }}
                </span>
              </td>
              <td>
                <span v-if="alumno.ultimaSesion.estado === 'En línea'" style="background: #ecfdf5; color: #059669; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600; display: inline-flex; align-items: center; gap: 6px;">
                  <span style="width: 6px; height: 6px; border-radius: 50%; background: #10b981;"></span>
                  En línea
                </span>
                <span v-else style="background: #f1f5f9; color: #64748b; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600; display: inline-flex; align-items: center; gap: 6px;">
                  Finalizado
                </span>
              </td>
            </tr>
            <!-- Nested row for other sessions -->
            <tr v-if="alumno.isExpanded" class="expanded-row">
              <td colspan="7" style="background: #f8fafc; padding: 0.75rem 1.5rem 1.25rem 3.5rem; text-align: left; border-bottom: 1px solid #e2e8f0;">
                <div class="nested-container">
                  <div style="font-size: 0.75rem; font-weight: 700; color: #4f46e5; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.75rem; display: flex; align-items: center; gap: 6px;">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>
                    Otras conexiones de este alumno hoy
                  </div>
                  <table class="nested-table">
                    <thead>
                      <tr>
                        <th style="text-align: left; padding-left: 1rem;">PC / Equipo</th>
                        <th>Hora Inicio</th>
                        <th>Hora Fin</th>
                        <th>Uso</th>
                        <th>Sesión Actual</th>
                        <th>Estado</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="ses in alumno.demasSesiones" :key="ses.sesionId">
                        <td style="text-align: left; padding-left: 1rem;">
                          <div style="display: flex; align-items: center; gap: 8px;">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#6366f1" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
                            <strong :title="ses.equipoRed" style="color: #475569;">{{ ses.equipoAlias || ses.equipoRed }}</strong>
                          </div>
                        </td>
                        <td style="color: #64748b;">{{ formatTime(ses.horaInicio) }}</td>
                        <td style="color: #64748b;">{{ formatTime(ses.horaFin) }}</td>
                        <td :style="{ color: ses.duracionMinutos > 180 ? '#e11d48' : '#64748b', fontWeight: '600' }">
                          {{ ses.duracionMinutos }} min
                        </td>
                        <td>
                          <span style="background: #eff6ff; color: #1d4ed8; padding: 2px 8px; border-radius: 4px; font-size: 0.75rem; font-weight: 600; white-space: nowrap;">
                            Sesión {{ Math.floor((ses.limiteDiarioSegundos || 10800) / 10800) }}
                          </span>
                        </td>
                        <td>
                          <span v-if="ses.estado === 'En línea'" style="background: #ecfdf5; color: #059669; padding: 2px 8px; border-radius: 4px; font-size: 0.75rem; font-weight: 600; display: inline-flex; align-items: center; gap: 4px;">
                            <span style="width: 4px; height: 4px; border-radius: 50%; background: #10b981;"></span>
                            En línea
                          </span>
                          <span v-else style="background: #f1f5f9; color: #64748b; padding: 2px 8px; border-radius: 4px; font-size: 0.75rem; font-weight: 600; display: inline-flex; align-items: center; gap: 4px;">
                            Finalizado
                          </span>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </td>
            </tr>
          </template>
        </table>
      </div>

      <!-- VISTA DE ESCANEOS -->
      <div v-else>
        <div v-if="escaneos.length === 0" style="padding: 3rem; text-align: center; color: #94a3b8;">
          No hay escaneos de carné registrados.
        </div>
        
        <table v-else class="centered-table">
          <thead>
            <tr>
              <th style="text-align: left; padding-left: 2rem;">ID de Escaneo</th>
              <th style="text-align: left;">Alumno / Detalle</th>
              <th>{{ verTodosEscaneos ? 'Fecha y Hora de Escaneo' : 'Hora de Escaneo' }}</th>
              <th>Dispositivo / Canal</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="scan in escaneos" :key="scan.scanLogId">
              <td style="text-align: left; padding-left: 2rem; font-weight: 600; color: #111827;">
                #{{ scan.scanLogId }}
              </td>
              <td style="text-align: left;">
                <div v-if="scan.isExitoso">
                  <div style="font-weight: 600; color: #111827;">{{ scan.alumnoNombre }}</div>
                  <div style="font-size: 0.75rem; color: #6b7280; margin-top: 2px;">Código: {{ scan.alumnoCodigo }}</div>
                </div>
                <div v-else style="color: #ef4444; font-size: 0.875rem; font-weight: 500; display: flex; align-items: center; gap: 6px;">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
                  <span>{{ scan.mensaje || 'Error al leer carnet' }}</span>
                  <span v-if="scan.alumnoCodigo" style="font-size: 0.75rem; color: #6b7280; font-weight: normal; margin-left: 4px;">({{ scan.alumnoCodigo }})</span>
                </div>
              </td>
              <td style="color: #475569;">
                {{ verTodosEscaneos ? formatDateAndTime(scan.fecha) : formatTime(scan.fecha) }}
              </td>
              <td style="color: #475569;">
                <div style="display: flex; align-items: center; justify-content: center; gap: 8px;">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#7c3aed" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>
                  {{ scan.realizadoPor }}
                </div>
              </td>
              <td>
                <span v-if="scan.isExitoso" style="background: #ecfdf5; color: #059669; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600; display: inline-flex; align-items: center; gap: 6px;">
                  <span style="width: 6px; height: 6px; border-radius: 50%; background: #10b981;"></span>
                  Exitoso
                </span>
                <span v-else style="background: #fef2f2; color: #ef4444; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600; display: inline-flex; align-items: center; gap: 6px;">
                  <span style="width: 6px; height: 6px; border-radius: 50%; background: #ef4444;"></span>
                  Fallido
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
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

/* Highlight row for multiple connections */
.has-multiple {
  background: rgba(99, 102, 241, 0.02);
  transition: background 0.2s ease, border-left 0.2s ease;
}

.has-multiple:hover {
  background: rgba(99, 102, 241, 0.05) !important;
}

.has-multiple td:first-child {
  position: relative;
}

/* Left indicator line for expandable rows */
.has-multiple td:first-child::before {
  content: "";
  position: absolute;
  left: 0;
  top: 6px;
  bottom: 6px;
  width: 4px;
  background: #6366f1;
  border-radius: 0 4px 4px 0;
}

/* Chevron animations */
.chevron-icon {
  transition: transform 0.2s ease;
}

.is-expanded .chevron-icon {
  transform: rotate(90deg);
}

/* Connection Badges */
.multi-connection-badge {
  background: #e0e7ff;
  color: #4f46e5;
  padding: 2px 8px;
  border-radius: 9999px;
  font-size: 0.65rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  border: 1px solid #c7d2fe;
}

/* Nested expanded content */
.nested-container {
  padding: 0.5rem 1rem;
  border-left: 2px dashed #cbd5e1;
  margin-left: 0.5rem;
}

.nested-table {
  width: 100%;
  border-collapse: collapse;
  background: transparent;
}

.nested-table th {
  background: transparent;
  border-bottom: 1px solid #e2e8f0;
  padding: 0.5rem 1rem;
  font-size: 0.7rem;
  color: #64748b;
  text-transform: uppercase;
  text-align: center;
  font-weight: 700;
  letter-spacing: 0.05em;
}

.nested-table td {
  padding: 0.6rem 1rem;
  text-align: center;
  border-bottom: 1px solid #f1f5f9;
  font-size: 0.85rem;
  color: #475569;
  background: transparent;
}

.nested-table tbody tr:hover {
  background: rgba(99, 102, 241, 0.03) !important;
}

.expanded-row td {
  border-bottom: 1px solid #e2e8f0 !important;
}
</style>
