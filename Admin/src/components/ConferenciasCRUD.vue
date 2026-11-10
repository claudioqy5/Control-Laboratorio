<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const emit = defineEmits(['ver-estadisticas'])

const conferencias = ref([])
const loading = ref(false)
const searchQuery = ref('')
const filterModalidad = ref('todas')
const filterEstado = ref('todos')

const showModal = ref(false)
const isEditing = ref(false)
const form = ref({
  tituloEvento: '',
  expositorPonente: '',
  entidadEditorial: 'FAMURP Conferencias',
  modalidad: 'Presencial',
  ubicacion_o_Enlace: '',
  fechaHoraInicio: '',
  fechaHoraFin: '',
  asistenciaAbierta: false,
  estadoEvento: 'Programado',
  autoPurgar30Dias: true
})

const toast = ref({ show: false, message: '' })
let toastTimer = null

const showToast = (message) => {
  if (toastTimer) clearTimeout(toastTimer)
  toast.value = { show: true, message }
  toastTimer = setTimeout(() => {
    toast.value.show = false
  }, 3000)
}

const loadConferencias = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/v1/admin/conferences`)
    conferencias.value = Array.isArray(res.data) ? res.data : []
  } catch (err) {
    showToast('Error al conectar con el servidor de conferencias')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadConferencias()
})

const monthNames = ['ENE.', 'FEB.', 'MAR.', 'ABR.', 'MAY.', 'JUN.', 'JUL.', 'AGO.', 'SET.', 'OCT.', 'NOV.', 'DIC.']

const parseDate = (d) => {
  if (!d) return new Date()
  const date = new Date(d)
  return isNaN(date.getTime()) ? new Date() : date
}

const getDay = (d) => {
  const date = parseDate(d)
  return String(date.getDate()).padStart(2, '0')
}

const getMonth = (d) => {
  const date = parseDate(d)
  return monthNames[date.getMonth()] || 'MES'
}

const getYear = (d) => {
  const date = parseDate(d)
  return date.getFullYear()
}

const formatTimeRange = (start, end) => {
  if (!start) return '--:--'
  const s = parseDate(start)
  const e = end ? parseDate(end) : null
  const fmt = (dt) => dt.toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit', hour12: true })
  return e ? `${fmt(s)} - ${fmt(e)}` : fmt(s)
}

const isConcluded = (conf) => {
  if (!conf) return false
  if (conf.estadoEvento && conf.estadoEvento.toLowerCase() === 'finalizada') return true
  if (conf.fechaHoraFin) {
    return new Date(conf.fechaHoraFin).getTime() < Date.now()
  }
  return false
}

const isEventConcluded = computed(() => {
  if (!form.value) return false
  if (form.value.estadoEvento && form.value.estadoEvento.toLowerCase() === 'finalizada') return true
  if (form.value.fechaHoraFin) {
    return new Date(form.value.fechaHoraFin).getTime() < Date.now()
  }
  return false
})

const getPillStatus = (conf) => {
  if (isConcluded(conf)) return 'FINALIZADA'
  if (conf.asistenciaAbierta || (conf.estadoEvento && conf.estadoEvento.toLowerCase() === 'en vivo')) return 'EN VIVO'
  return 'PROGRAMADO'
}

const copyInscriptionLink = async (conf) => {
  const url = conf.urlInscripcion || `${window.location.origin}/#conferencias?inscripcion=${conf.idConferencia}`
  try {
    await navigator.clipboard.writeText(url)
    showToast('Enlace de inscripción copiado al portapapeles')
  } catch {
    showToast('No se pudo copiar el enlace')
  }
}

const copyAttendanceLink = async (conf) => {
  const url = conf.urlAsistencia || `${window.location.origin}/#conferencias?asistencia=${conf.idConferencia}`
  try {
    await navigator.clipboard.writeText(url)
    showToast('Enlace de asistencia copiado al portapapeles')
  } catch {
    showToast('No se pudo copiar el enlace')
  }
}

const toggleAttendance = async (conf) => {
  try {
    const res = await axios.patch(`${API_BASE_URL}/api/v1/admin/conferences/${conf.idConferencia}/toggle-attendance`)
    conf.asistenciaAbierta = res.data?.asistenciaAbierta ?? !conf.asistenciaAbierta
    showToast(conf.asistenciaAbierta ? 'Marcación en vivo habilitada' : 'Marcación en vivo cerrada')
  } catch {
    showToast('Error al cambiar estado de marcación')
  }
}

const togglePurge = async (conf) => {
  const nuevoValor = !conf.autoPurgar30Dias
  try {
    const payload = {
      ...conf,
      autoPurgar30Dias: nuevoValor
    }
    await axios.put(`${API_BASE_URL}/api/v1/admin/conferences/${conf.idConferencia}`, payload)
    conf.autoPurgar30Dias = nuevoValor
    showToast(nuevoValor ? 'Auto-purga 30 días activada' : 'Conservación indefinida establecida')
  } catch {
    showToast('Error al modificar política de purga')
  }
}

const openCreateModal = () => {
  isEditing.value = false
  const now = new Date()
  const startIso = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
  const endDate = new Date(now.getTime() + 2 * 60 * 60 * 1000 - now.getTimezoneOffset() * 60000)
  const endIso = endDate.toISOString().slice(0, 16)

  form.value = {
    tituloEvento: '',
    expositorPonente: '',
    entidadEditorial: 'FAMURP Conferencias',
    modalidad: 'Presencial',
    ubicacion_o_Enlace: '',
    fechaHoraInicio: startIso,
    fechaHoraFin: endIso,
    asistenciaAbierta: false,
    estadoEvento: 'Programado',
    autoPurgar30Dias: true
  }
  showModal.value = true
}

const openEditModal = (conf) => {
  isEditing.value = true
  const toLocalIso = (d) => {
    if (!d) return ''
    const dt = new Date(d)
    return isNaN(dt.getTime()) ? '' : new Date(dt.getTime() - dt.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
  }

  form.value = {
    idConferencia: conf.idConferencia,
    tituloEvento: conf.tituloEvento || '',
    expositorPonente: conf.expositorPonente || '',
    entidadEditorial: conf.entidadEditorial || 'FAMURP Conferencias',
    modalidad: conf.modalidad || 'Presencial',
    ubicacion_o_Enlace: conf.ubicacion_o_Enlace || '',
    fechaHoraInicio: toLocalIso(conf.fechaHoraInicio),
    fechaHoraFin: toLocalIso(conf.fechaHoraFin),
    asistenciaAbierta: !!conf.asistenciaAbierta,
    estadoEvento: conf.estadoEvento || 'Programado',
    autoPurgar30Dias: conf.autoPurgar30Dias !== false
  }
  showModal.value = true
}

const saveConference = async () => {
  if (!form.value.tituloEvento?.trim() || !form.value.expositorPonente?.trim()) {
    showToast('Título y Expositor son obligatorios')
    return
  }
  if (!form.value.ubicacion_o_Enlace?.trim()) {
    showToast('Lugar o enlace es obligatorio')
    return
  }
  try {
    const payload = {
      ...form.value,
      fechaHoraInicio: new Date(form.value.fechaHoraInicio).toISOString(),
      fechaHoraFin: new Date(form.value.fechaHoraFin).toISOString()
    }
    if (isEditing.value) {
      await axios.put(`${API_BASE_URL}/api/v1/admin/conferences/${form.value.idConferencia}`, payload)
      showToast('Conferencia actualizada exitosamente')
    } else {
      await axios.post(`${API_BASE_URL}/api/v1/admin/conferences`, payload)
      showToast('Conferencia registrada exitosamente')
    }
    showModal.value = false
    await loadConferencias()
  } catch (err) {
    const errorData = err.response?.data
    let errMsg = 'Error al guardar conferencia'
    if (errorData?.errors) {
      errMsg = Object.values(errorData.errors).flat().join(', ')
    } else if (errorData?.title) {
      errMsg = errorData.title
    }
    showToast(errMsg)
  }
}

const deleteConference = async (id) => {
  if (!window.confirm('¿Está seguro de eliminar esta conferencia? Esta acción no se puede deshacer.')) return
  try {
    await axios.delete(`${API_BASE_URL}/api/v1/admin/conferences/${id}`)
    conferencias.value = conferencias.value.filter(c => c.idConferencia !== id)
    showToast('Conferencia eliminada correctamente')
  } catch {
    showToast('Error al eliminar conferencia')
  }
}

const filteredConferencias = computed(() => {
  return conferencias.value.filter(c => {
    if (searchQuery.value.trim()) {
      const q = searchQuery.value.toLowerCase()
      const match = (c.tituloEvento || '').toLowerCase().includes(q) ||
                    (c.expositorPonente || '').toLowerCase().includes(q) ||
                    (c.entidadEditorial || '').toLowerCase().includes(q) ||
                    (c.ubicacion_o_Enlace || '').toLowerCase().includes(q)
      if (!match) return false
    }
    if (filterModalidad.value !== 'todas') {
      if ((c.modalidad || '').toLowerCase() !== filterModalidad.value.toLowerCase()) return false
    }
    if (filterEstado.value !== 'todos') {
      const pill = getPillStatus(c)
      if (filterEstado.value === 'live' && pill !== 'EN VIVO') return false
      if (filterEstado.value === 'scheduled' && pill !== 'PROGRAMADO') return false
      if (filterEstado.value === 'finished' && pill !== 'FINALIZADA') return false
    }
    return true
  })
})
</script>

<template>
  <div class="conferencias-wrapper">
    <header class="page-header">
      <div>
        <h1 class="page-title">Gestión de Conferencias</h1>
        <p class="page-subtitle">Programación de eventos médicos, control de marcación en vivo y enlaces de acreditación</p>
      </div>
      <div class="header-actions">
        <button type="button" class="btn-refresh" @click="loadConferencias" title="Actualizar">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10" />
            <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" />
          </svg>
        </button>
        <button type="button" class="btn-create" @click="openCreateModal">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          <span>Nueva Conferencia</span>
        </button>
      </div>
    </header>

    <section class="filter-bar">
      <div class="search-box">
        <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          class="search-input"
          placeholder="Buscar por título, ponente o entidad organizadora..."
        />
      </div>
      <select v-model="filterModalidad" class="filter-select">
        <option value="todas">Todas las Modalidades</option>
        <option value="Presencial">Presencial</option>
        <option value="Virtual">Virtual</option>
        <option value="Híbrida">Híbrida</option>
      </select>
      <select v-model="filterEstado" class="filter-select">
        <option value="todos">Todos los Estados</option>
        <option value="live">En Vivo (Marcación Abierta)</option>
        <option value="scheduled">Programadas</option>
        <option value="finished">Finalizadas</option>
      </select>
    </section>

    <main class="conferences-list">
      <div v-if="loading" class="empty-message">
        Cargando eventos académicos...
      </div>
      <div v-else-if="filteredConferencias.length === 0" class="empty-message">
        No se encontraron conferencias registradas.
      </div>
      <article
        v-for="conf in filteredConferencias"
        :key="conf.idConferencia"
        class="conf-card"
      >
        <div
          class="conf-date-block"
          :class="{
            'finished-block': isConcluded(conf)
          }"
        >
          <div class="date-info">
            <div class="date-day">{{ getDay(conf.fechaHoraInicio) }}</div>
            <div class="date-month">{{ getMonth(conf.fechaHoraInicio) }}</div>
            <div class="date-year">{{ getYear(conf.fechaHoraInicio) }}</div>
          </div>
          <span
            class="status-pill"
            :class="{
              'pill-finished': isConcluded(conf),
              'pill-live': getPillStatus(conf) === 'EN VIVO',
              'pill-scheduled': getPillStatus(conf) === 'PROGRAMADO'
            }"
          >
            <span v-if="getPillStatus(conf) === 'EN VIVO'" class="pulse-dot"></span>
            <span>{{ getPillStatus(conf) }}</span>
          </span>
        </div>

        <div class="conf-body">
          <div class="conf-content-top">
            <div class="conf-meta-row">
              <div class="meta-left">
                <span class="meta-item">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                  <span>{{ formatTimeRange(conf.fechaHoraInicio, conf.fechaHoraFin) }}</span>
                </span>
                <span class="meta-bullet">•</span>
                <span class="meta-item">
                  <svg v-if="conf.modalidad === 'Virtual'" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/></svg>
                  <svg v-else width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="4" y="2" width="16" height="20" rx="2" ry="2"/><path d="M9 22v-4h6v4"/></svg>
                  <span>{{ conf.modalidad || 'Presencial' }}</span>
                </span>
                <span class="meta-bullet">•</span>
                <span class="meta-item">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>
                  <span>{{ conf.entidadEditorial || 'FAMURP Conferencias' }}</span>
                </span>
              </div>

              <div class="meta-right-aforo">
                <span class="chip-aforo chip-blue">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/></svg>
                  <span>{{ conf.totalPreInscritos ?? 0 }} Inscritos</span>
                </span>
                <span class="chip-aforo chip-green">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
                  <span>{{ conf.totalAsistentes ?? 0 }} Asistentes</span>
                </span>
              </div>
            </div>

            <h2 class="conf-title">{{ conf.tituloEvento }}</h2>
            <div class="conf-speaker">
              <span>Expositor: <strong>{{ conf.expositorPonente }}</strong></span>
            </div>
          </div>

          <div class="conf-actions-row">
            <div class="action-group-left">
              <button
                type="button"
                class="btn-action-blue"
                @click="copyInscriptionLink(conf)"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>
                <span>Link Inscripción</span>
              </button>

              <button
                type="button"
                class="btn-action-green"
                @click="copyAttendanceLink(conf)"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
                <span>Link Asistencia</span>
              </button>

              <button
                type="button"
                class="btn-action-purple"
                @click="togglePurge(conf)"
                :title="conf.autoPurgar30Dias ? 'Auto-purga a los 30 días activa' : 'Conservación indefinida de datos'"
              >
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                <span>{{ conf.autoPurgar30Dias ? 'Auto-purga 30 días' : 'Conservación Indefinida' }}</span>
              </button>

              <span v-if="isConcluded(conf)" class="badge-concluded">
                Evento Concluido
              </span>
              <label v-else class="switch-label" @click.prevent="toggleAttendance(conf)">
                <span class="switch-ui" :class="{ active: conf.asistenciaAbierta }"></span>
                <span class="switch-text" :class="{ active: conf.asistenciaAbierta }">
                  {{ conf.asistenciaAbierta ? 'Asistencia Abierta' : 'Asistencia Cerrada' }}
                </span>
              </label>
            </div>

            <div class="action-group-right">
              <button
                type="button"
                class="btn-action-dark"
                @click="emit('ver-estadisticas', conf.idConferencia)"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>
                <span>Ver Estadísticas</span>
              </button>

              <button
                type="button"
                class="icon-btn-edit"
                @click="openEditModal(conf)"
                title="Editar Conferencia"
              >
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
              </button>

              <button
                type="button"
                class="icon-btn-delete"
                @click="deleteConference(conf.idConferencia)"
                title="Eliminar Conferencia"
              >
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
              </button>
            </div>
          </div>
        </div>
      </article>
    </main>

    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal-card">
        <header class="modal-header">
          <h3 class="modal-title">{{ isEditing ? 'Editar Conferencia Médica' : 'Nueva Conferencia Médica' }}</h3>
          <button type="button" class="modal-close" @click="showModal = false">&times;</button>
        </header>

        <section class="modal-body">
          <div class="form-group">
            <label class="form-label">Título del Evento Académico</label>
            <input
              v-model="form.tituloEvento"
              type="text"
              class="form-control"
              placeholder="Ej: Simposio de Avances en Cardiología Pediátrica"
            />
          </div>

          <div class="form-grid-2">
            <div class="form-group">
              <label class="form-label">Expositor / Ponente Principal</label>
              <input
                v-model="form.expositorPonente"
                type="text"
                class="form-control"
                placeholder="Ej: Dr. Ricardo Silva Cárdenas"
              />
            </div>
            <div class="form-group">
              <label class="form-label">Entidad / Editorial Organizadora</label>
              <input
                v-model="form.entidadEditorial"
                type="text"
                class="form-control"
                placeholder="Ej: FAMURP Conferencias"
              />
            </div>
          </div>

          <div class="form-grid-2">
            <div class="form-group">
              <label class="form-label">Fecha y Hora de Inicio</label>
              <input
                v-model="form.fechaHoraInicio"
                type="datetime-local"
                class="form-control"
              />
            </div>
            <div class="form-group">
              <label class="form-label">Fecha y Hora de Fin</label>
              <input
                v-model="form.fechaHoraFin"
                type="datetime-local"
                class="form-control"
              />
            </div>
          </div>

          <div class="form-grid-2">
            <div class="form-group">
              <label class="form-label">Modalidad</label>
              <select v-model="form.modalidad" class="form-control">
                <option value="Presencial">Presencial</option>
                <option value="Virtual">Virtual</option>
                <option value="Híbrida">Híbrida</option>
              </select>
            </div>
            <div class="form-group">
              <label class="form-label">Lugar o Enlace Virtual</label>
              <input
                v-model="form.ubicacion_o_Enlace"
                type="text"
                class="form-control"
                placeholder="Auditorio Mayor o Enlace Meet/Zoom"
              />
            </div>
          </div>

          <div class="form-grid-2">
            <div class="form-group">
              <label class="form-label">Estado del Evento</label>
              <select v-model="form.estadoEvento" class="form-control" :disabled="isEventConcluded">
                <option value="Programado">Programado</option>
                <option value="En Vivo">En Vivo</option>
                <option value="Finalizada">Finalizada</option>
              </select>
              <span v-if="isEventConcluded" class="form-field-hint">Conferencia concluida por fecha límite (estado bloqueado)</span>
            </div>
            <div class="form-group-checkbox">
              <label class="inline-checkbox">
                <input v-model="form.asistenciaAbierta" type="checkbox" :disabled="isEventConcluded" />
                <span>Marcación de Asistencia Abierta</span>
              </label>
            </div>
          </div>

          <div class="purge-panel">
            <label class="purge-label">
              <input v-model="form.autoPurgar30Dias" type="checkbox" class="purge-input" />
              <div class="purge-text">
                <strong class="purge-text-title">Activar Purga Automática a 30 Días:</strong>
                <span class="purge-text-desc">Transcurridos 30 días tras finalizar el evento, las listas de participantes se eliminarán automáticamente por política de retención de datos.</span>
              </div>
            </label>
          </div>
        </section>

        <footer class="modal-footer">
          <button type="button" class="btn-cancel" @click="showModal = false">Cancelar</button>
          <button type="button" class="btn-submit" @click="saveConference">
            {{ isEditing ? 'Guardar Cambios' : 'Registrar Conferencia' }}
          </button>
        </footer>
      </div>
    </div>

    <div v-if="toast.show" class="floating-toast">
      {{ toast.message }}
    </div>
  </div>
</template>

<style scoped>
.conferencias-wrapper {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.page-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.5rem;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.02em;
  margin: 0;
}

.page-subtitle {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.875rem;
  color: #64748b;
  margin: 4px 0 0 0;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-refresh {
  background: #ffffff;
  color: #475569;
  border: 1px solid #cbd5e1;
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.btn-refresh:hover {
  background: #ffffff;
  border-color: #008744;
  color: #008744;
  transform: translateY(-1px);
}

.btn-create {
  background: #008744;
  color: #ffffff;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-create:hover {
  background: #007A3D;
  transform: translateY(-1px);
}

.filter-bar {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  align-items: center;
}

.search-box {
  flex: 1;
  min-width: 280px;
  position: relative;
}

.search-icon {
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 8px 12px 8px 38px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.875rem;
  color: #0f172a;
  background: #ffffff;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
}

.search-input:focus {
  border-color: #008744;
}

.filter-select {
  padding: 8px 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background: #ffffff;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.85rem;
  color: #334155;
  cursor: pointer;
  outline: none;
  transition: all 0.2s ease;
}

.filter-select:focus {
  border-color: #008744;
}

.conferences-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.empty-message {
  padding: 48px 16px;
  text-align: center;
  color: #64748b;
  font-size: 0.95rem;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
}

.conf-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  display: flex;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  transition: all 0.2s ease;
}

.conf-card:hover {
  border-color: #cbd5e1;
  box-shadow: 0 4px 12px -2px rgba(0, 0, 0, 0.08);
  transform: translateY(-1px);
}

.conf-date-block {
  min-width: 115px;
  width: 115px;
  background: #0f172a;
  color: #ffffff;
  padding: 20px 10px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  align-items: center;
  text-align: center;
  flex-shrink: 0;
  box-sizing: border-box;
}

.conf-date-block.finished-block {
  background: #020617;
}

.date-info {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.date-day {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 34px;
  font-weight: 800;
  line-height: 1;
  color: #ffffff;
}

.date-month {
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  margin-top: 4px;
  color: #ffffff;
}

.date-year {
  font-size: 11px;
  color: #94a3b8;
  font-weight: 600;
}

.status-pill {
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  padding: 3px 8px;
  border-radius: 9999px;
  margin-top: 10px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.status-pill.pill-live {
  background: #008744;
  color: #ffffff;
}

.status-pill.pill-scheduled {
  background: rgba(255, 255, 255, 0.16);
  color: #ffffff;
}

.status-pill.pill-finished {
  background: rgba(0, 0, 0, 0.5);
  color: #94a3b8;
  border: 1px solid #334155;
}

.pulse-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #8cf9a9;
  animation: pulseEffect 1.5s infinite;
}

@keyframes pulseEffect {
  0% { opacity: 0.4; transform: scale(0.9); }
  50% { opacity: 1; transform: scale(1.3); }
  100% { opacity: 0.4; transform: scale(0.9); }
}

.conf-body {
  padding: 18px 22px;
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 14px;
  background: #ffffff;
  box-sizing: border-box;
}

.conf-content-top {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.conf-meta-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
  font-size: 0.8rem;
  color: #475569;
  font-weight: 600;
}

.meta-left {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.meta-item {
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.meta-item svg {
  color: #008744;
}

.meta-bullet {
  color: #cbd5e1;
}

.meta-right-aforo {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.chip-aforo {
  font-size: 0.75rem;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 9999px;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.chip-aforo.chip-blue {
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #dbeafe;
}

.chip-aforo.chip-green {
  background: #ecfdf5;
  color: #047857;
  border: 1px solid #a7f3d0;
}

.conf-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.15rem;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.35;
  margin: 2px 0 0 0;
}

.conf-speaker {
  font-size: 0.85rem;
  color: #475569;
  margin: 0;
}

.conf-speaker strong {
  color: #1e293b;
}

.conf-actions-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
  border-top: 1px solid #f1f5f9;
  gap: 10px;
  flex-wrap: wrap;
}

.action-group-left {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.btn-action-blue {
  background: #0284c7;
  color: #ffffff;
  border: none;
  padding: 6px 12px;
  border-radius: 6px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-action-blue:hover {
  background: #0369a1;
  transform: translateY(-1px);
}

.btn-action-green {
  background: #008744;
  color: #ffffff;
  border: none;
  padding: 6px 12px;
  border-radius: 6px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-action-green:hover {
  background: #007A3D;
  transform: translateY(-1px);
}

.btn-action-purple {
  background: #4f46e5;
  color: #ffffff;
  border: none;
  padding: 6px 12px;
  border-radius: 6px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-action-purple:hover {
  background: #4338ca;
  transform: translateY(-1px);
}

.badge-concluded {
  background: #f1f5f9;
  color: #64748b;
  border: 1px solid #e2e8f0;
  padding: 5px 10px;
  border-radius: 6px;
  font-size: 0.78rem;
  font-weight: 600;
  display: inline-flex;
  align-items: center;
}

.switch-label {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  user-select: none;
}

.switch-ui {
  width: 36px;
  height: 20px;
  background: #cbd5e1;
  border-radius: 9999px;
  position: relative;
  transition: background-color 0.2s ease;
  display: inline-block;
}

.switch-ui::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 2px;
  width: 16px;
  height: 16px;
  background: #ffffff;
  border-radius: 50%;
  transition: transform 0.2s ease;
}

.switch-ui.active {
  background: #008744;
}

.switch-ui.active::after {
  transform: translateX(16px);
}

.switch-text {
  font-size: 0.78rem;
  font-weight: 600;
  color: #64748b;
  transition: color 0.2s ease;
}

.switch-text.active {
  color: #008744;
}

.action-group-right {
  display: flex;
  align-items: center;
  gap: 6px;
}

.btn-action-dark {
  background: #0f172a;
  color: #ffffff;
  border: none;
  padding: 6px 12px;
  border-radius: 6px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
}

.btn-action-dark:hover {
  background: #1e293b;
  transform: translateY(-1px);
}

.icon-btn-edit {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  width: 32px;
  height: 32px;
  border-radius: 6px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.icon-btn-edit:hover {
  border-color: #008744;
  color: #008744;
  transform: translateY(-1px);
}

.icon-btn-delete {
  background: #ffffff;
  border: 1px solid #fecdd3;
  color: #dc2626;
  width: 32px;
  height: 32px;
  border-radius: 6px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.icon-btn-delete:hover {
  background: #fef2f2;
  border-color: #f87171;
  transform: translateY(-1px);
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 20px;
}

.modal-card {
  background: #ffffff;
  border-radius: 16px;
  width: 100%;
  max-width: 620px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  box-sizing: border-box;
}

.modal-header {
  padding: 18px 22px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.15rem;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}

.modal-close {
  background: transparent;
  border: none;
  font-size: 1.5rem;
  color: #94a3b8;
  cursor: pointer;
  line-height: 1;
}

.modal-close:hover {
  color: #0f172a;
}

.modal-body {
  padding: 22px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: #334155;
}

.form-control {
  padding: 8px 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.875rem;
  color: #0f172a;
  background: #ffffff;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
}

.form-control:focus {
  border-color: #008744;
}

.form-control:disabled {
  background: #f1f5f9;
  color: #64748b;
  cursor: not-allowed;
  border-color: #cbd5e1;
}

.form-field-hint {
  font-size: 0.725rem;
  color: #64748b;
  margin-top: 4px;
}

.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.form-group-checkbox {
  display: flex;
  align-items: flex-end;
  padding-bottom: 6px;
}

.inline-checkbox {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 600;
  color: #334155;
}

.inline-checkbox input {
  accent-color: #008744;
}

.purge-panel {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 12px;
  box-sizing: border-box;
}

.purge-label {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  cursor: pointer;
}

.purge-input {
  accent-color: #4f46e5;
  margin-top: 3px;
}

.purge-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: 0.8rem;
  color: #475569;
}

.purge-text-title {
  color: #0f172a;
}

.modal-footer {
  padding: 14px 22px;
  border-top: 1px solid #e2e8f0;
  background: #ffffff;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  border-bottom-left-radius: 16px;
  border-bottom-right-radius: 16px;
}

.btn-cancel {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  padding: 8px 16px;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-cancel:hover {
  background: #ffffff;
  border-color: #94a3b8;
  color: #0f172a;
}

.btn-submit {
  background: #008744;
  border: none;
  color: #ffffff;
  padding: 8px 18px;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-submit:hover {
  background: #007A3D;
  transform: translateY(-1px);
}

.floating-toast {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: #0f172a;
  color: #ffffff;
  padding: 12px 20px;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.85rem;
  font-weight: 600;
  z-index: 2000;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.2);
  animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

@media (max-width: 768px) {
  .conf-card {
    flex-direction: column;
  }
  .conf-date-block {
    width: 100%;
    min-width: 100%;
    flex-direction: row;
    padding: 14px 20px;
  }
  .form-grid-2 {
    grid-template-columns: 1fr;
  }
}
</style>
