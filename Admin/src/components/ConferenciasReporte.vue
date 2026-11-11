<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'
import { API_BASE_URL } from '../config'

const props = defineProps({
  initialConferenciaId: {
    type: [Number, String],
    default: null
  }
})

const conferencias = ref([])
const selectedConferenciaId = ref(null)
const selectedMes = ref('todos')
const selectedAnio = ref('todos')
const loading = ref(false)
const loadingReport = ref(false)

const reportData = ref({
  kpis: {
    totalPreInscritos: 0,
    acreditados: 0,
    inasistencias: 0,
    espontaneos: 0,
    totalAsistentesReales: 0,
    tasaAsistencia: 0
  },
  participantes: []
})

const activeTab = ref('todos')
const searchQuery = ref('')
const filterEstamento = ref('todos')

const toast = ref({ show: false, message: '' })
let toastTimer = null

const showEditModal = ref(false)
const editForm = ref({
  idParticipacion: null,
  nombres: '',
  apellidoPaterno: '',
  apellidoMaterno: '',
  correo: ''
})
const savingEdit = ref(false)

const showToast = (message) => {
  if (toastTimer) clearTimeout(toastTimer)
  toast.value = { show: true, message }
  toastTimer = setTimeout(() => {
    toast.value.show = false
  }, 3000)
}

const mesesNombres = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
]

const availableYears = computed(() => {
  const years = new Set()
  for (const c of conferencias.value) {
    if (c.fechaHoraInicio) {
      const dt = new Date(c.fechaHoraInicio)
      if (!isNaN(dt.getTime())) {
        years.add(dt.getFullYear())
      }
    }
  }
  return Array.from(years).sort((a, b) => b - a)
})

const filteredConferenciasParaSelect = computed(() => {
  return conferencias.value.filter(c => {
    if (!c.fechaHoraInicio) return true
    const dt = new Date(c.fechaHoraInicio)
    if (isNaN(dt.getTime())) return true
    if (selectedMes.value !== 'todos' && dt.getMonth() !== Number(selectedMes.value)) {
      return false
    }
    if (selectedAnio.value !== 'todos' && dt.getFullYear() !== Number(selectedAnio.value)) {
      return false
    }
    return true
  })
})

const currentConferencia = computed(() => {
  return conferencias.value.find(c => String(c.idConferencia) === String(selectedConferenciaId.value)) || null
})

const getParticipanteEstado = (p) => {
  if (p.estadoParticipacion === 2) {
    if (p.fechaHoraInscripcion && p.fechaHoraAsistencia) {
      const tInsc = new Date(p.fechaHoraInscripcion).getTime()
      const tAsis = new Date(p.fechaHoraAsistencia).getTime()
      if (Math.abs(tInsc - tAsis) < 1000) {
        return 'Espontáneo'
      }
    }
    return 'Acreditado'
  }
  return 'Inasistencia'
}

const tabCounts = computed(() => {
  const list = reportData.value.participantes || []
  let acreditados = 0
  let espontaneos = 0
  let inasistencias = 0
  for (const p of list) {
    const st = getParticipanteEstado(p)
    if (st === 'Acreditado') acreditados++
    else if (st === 'Espontáneo') espontaneos++
    else if (st === 'Inasistencia') inasistencias++
  }
  return {
    todos: list.length,
    acreditados,
    espontaneos,
    inasistencias
  }
})

const filteredParticipantes = computed(() => {
  let list = reportData.value.participantes || []

  if (activeTab.value !== 'todos') {
    list = list.filter(p => {
      const st = getParticipanteEstado(p)
      if (activeTab.value === 'acreditados') return st === 'Acreditado'
      if (activeTab.value === 'espontaneos') return st === 'Espontáneo'
      if (activeTab.value === 'inasistencias') return st === 'Inasistencia'
      return true
    })
  }

  if (filterEstamento.value !== 'todos') {
    const f = filterEstamento.value.toLowerCase()
    list = list.filter(p => {
      const r = (p.tipoParticipante || '').toLowerCase()
      if (f === 'pregrado') {
        return r === 'pregrado' || r === 'estudiante'
      }
      return r.includes(f)
    })
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase()
    list = list.filter(p => {
      const fullName = `${p.nombres || ''} ${p.apellidoPaterno || ''} ${p.apellidoMaterno || ''}`.toLowerCase()
      const doc = (p.numeroDocumento || '').toLowerCase()
      const cod = (p.codigoURP || '').toLowerCase()
      const mail = (p.correo || '').toLowerCase()
      return fullName.includes(q) || doc.includes(q) || cod.includes(q) || mail.includes(q)
    })
  }

  return list
})

const formatDateFull = (d) => {
  if (!d) return '--'
  const dt = new Date(d)
  if (isNaN(dt.getTime())) return '--'
  return dt.toLocaleDateString('es-PE', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

const formatTimeRange = (start, end) => {
  if (!start) return '--:--'
  const s = new Date(start)
  const e = end ? new Date(end) : null
  const fmt = (dt) => dt.toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit', hour12: true }).toLowerCase()
  return e && !isNaN(e.getTime()) ? `${fmt(s)} - ${fmt(e)}` : fmt(s)
}

const formatTime12h = (d) => {
  if (!d) return '-'
  const dt = new Date(d)
  if (isNaN(dt.getTime())) return '-'
  return dt.toLocaleTimeString('es-PE', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: true
  }).toLowerCase()
}

const loadReport = async (conferenciaId) => {
  if (!conferenciaId) return
  loadingReport.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/v1/admin/conferences/${conferenciaId}/report`)
    reportData.value = {
      kpis: res.data?.kpis || {
        totalPreInscritos: 0,
        acreditados: 0,
        inasistencias: 0,
        espontaneos: 0,
        totalAsistentesReales: 0,
        tasaAsistencia: 0
      },
      participantes: Array.isArray(res.data?.participantes) ? res.data.participantes : []
    }
  } catch (err) {
    showToast('Error al cargar reporte de la conferencia')
    reportData.value = {
      kpis: {
        totalPreInscritos: 0,
        acreditados: 0,
        inasistencias: 0,
        espontaneos: 0,
        totalAsistentesReales: 0,
        tasaAsistencia: 0
      },
      participantes: []
    }
  } finally {
    loadingReport.value = false
  }
}

const loadConferencias = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/v1/admin/conferences`)
    conferencias.value = Array.isArray(res.data) ? res.data : []
    if (conferencias.value.length > 0) {
      if (props.initialConferenciaId) {
        const found = conferencias.value.find(c => String(c.idConferencia) === String(props.initialConferenciaId))
        if (found) {
          selectedConferenciaId.value = found.idConferencia
          await loadReport(found.idConferencia)
          return
        }
      }
      selectedConferenciaId.value = conferencias.value[0].idConferencia
      await loadReport(conferencias.value[0].idConferencia)
    }
  } catch (err) {
    showToast('Error al conectar con el servidor de conferencias')
  } finally {
    loading.value = false
  }
}

const refreshData = async () => {
  if (selectedConferenciaId.value) {
    await loadReport(selectedConferenciaId.value)
    showToast('Datos actualizados')
  } else {
    await loadConferencias()
  }
}

watch(selectedConferenciaId, (newId) => {
  if (newId) {
    loadReport(newId)
  }
})

watch([selectedMes, selectedAnio], () => {
  const disponibles = filteredConferenciasParaSelect.value
  if (disponibles.length > 0) {
    const stillPresent = disponibles.some(c => String(c.idConferencia) === String(selectedConferenciaId.value))
    if (!stillPresent) {
      selectedConferenciaId.value = disponibles[0].idConferencia
    }
  }
})

const empadronar = async (part) => {
  const nombre = `${part.nombres || ''} ${part.apellidoPaterno || ''}`.trim()
  if (!window.confirm(`¿Desea dar de alta a "${nombre}" en el padrón de alumnos de cómputo?`)) {
    return
  }
  try {
    const res = await axios.post(`${API_BASE_URL}/api/v1/admin/conferences/participants/${part.idParticipacion}/link-to-student`)
    part.alumnoID = res.data?.alumnoId || 1
    showToast('Participante empadronado exitosamente en Cómputo')
  } catch (err) {
    const msg = err.response?.data?.message || err.response?.data || 'Error al empadronar participante'
    showToast(typeof msg === 'string' ? msg : 'Error al empadronar participante')
  }
}

const openEditModal = (part) => {
  editForm.value = {
    idParticipacion: part.idParticipacion,
    nombres: part.nombres || '',
    apellidoPaterno: part.apellidoPaterno || '',
    apellidoMaterno: part.apellidoMaterno || '',
    correo: part.correo || ''
  }
  showEditModal.value = true
}

const saveParticipantEdit = async () => {
  if (!editForm.value.nombres.trim() || !editForm.value.apellidoPaterno.trim()) {
    showToast('Nombres y Apellido Paterno son obligatorios')
    return
  }
  savingEdit.value = true
  try {
    const payload = {
      nombres: editForm.value.nombres.trim(),
      apellidoPaterno: editForm.value.apellidoPaterno.trim(),
      apellidoMaterno: editForm.value.apellidoMaterno ? editForm.value.apellidoMaterno.trim() : null,
      correo: editForm.value.correo.trim()
    }
    await axios.put(
      `${API_BASE_URL}/api/v1/admin/conferences/${selectedConferenciaId.value}/participants/${editForm.value.idParticipacion}`,
      payload
    )
    const target = reportData.value.participantes.find(p => p.idParticipacion === editForm.value.idParticipacion)
    if (target) {
      target.nombres = payload.nombres
      target.apellidoPaterno = payload.apellidoPaterno
      target.apellidoMaterno = payload.apellidoMaterno
      target.correo = payload.correo
    }
    showEditModal.value = false
    showToast('Datos de participante actualizados correctamente')
  } catch (err) {
    showToast('Error al actualizar datos del participante')
  } finally {
    savingEdit.value = false
  }
}

const exportToExcel = () => {
  if (!currentConferencia.value) {
    showToast('Seleccione una conferencia para exportar')
    return
  }
  const lista = reportData.value.participantes || []
  if (lista.length === 0) {
    showToast('No hay participantes registrados para exportar')
    return
  }
  const rows = lista.map((p, idx) => {
    const st = getParticipanteEstado(p)
    const isEspont = st === 'Espontáneo'
    return {
      'N°': idx + 1,
      'Tipo Documento': p.tipoDocumento || 'DNI',
      'Documento': p.numeroDocumento || '',
      'Código URP': p.codigoURP || '-',
      'Apellidos y Nombres': `${p.apellidoPaterno || ''} ${p.apellidoMaterno || ''} ${p.nombres || ''}`.trim(),
      'Correo Electrónico': p.correo || '',
      'Rol / Estamento': p.tipoParticipante || 'Estudiante',
      'Ciclo': p.ciclo || '-',
      'Fecha / Hora Pre-Registro': isEspont ? '-' : formatTime12h(p.fechaHoraInscripcion),
      'Hora Marcación': p.fechaHoraAsistencia ? formatTime12h(p.fechaHoraAsistencia) : '-',
      'Estado Acreditación': st,
      'Padrón Cómputo': p.alumnoID !== null ? 'En Padrón' : 'No Registrado'
    }
  })

  const ws = XLSX.utils.json_to_sheet(rows)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Auditoria_Asistencias')
  const tituloSanitizado = (currentConferencia.value.tituloEvento || 'Conferencia')
    .replace(/[^a-zA-Z0-9_-]/g, '_')
    .substring(0, 35)
  XLSX.writeFile(wb, `Auditoria_Conferencia_${tituloSanitizado}.xlsx`)
  showToast('Archivo Excel generado y descargado correctamente')
}

onMounted(() => {
  loadConferencias()
})
</script>

<template>
  <div class="reporte-wrapper">
    <header class="header-container">
      <div class="header-left">
        <span class="header-badge-tag">AUDITORÍA DE ASISTENCIAS & ACREDITACIÓN</span>
        <h1 class="header-title">Estadísticas y Reportes de Conferencias</h1>
        <p class="header-subtitle">Cruce algorítmico de pre-inscripción vs. marcación en tiempo real con exportación oficial.</p>
      </div>
      <div class="header-actions">
        <button
          type="button"
          class="btn-refresh"
          title="Actualizar datos"
          :disabled="loading || loadingReport"
          @click="refreshData"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
          </svg>
        </button>
        <button
          type="button"
          class="btn-export-excel"
          :disabled="!currentConferencia || loadingReport"
          @click="exportToExcel"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
            <polyline points="10 9 9 9 8 9" />
          </svg>
          <span>Exportar Reporte Excel (.xlsx)</span>
        </button>
      </div>
    </header>

    <section class="selectors-card">
      <div class="selectors-grid">
        <div class="selector-group selector-mes">
          <label class="selector-label">Mes</label>
          <select v-model="selectedMes" class="selector-input">
            <option value="todos">Todos los Meses</option>
            <option v-for="(mes, idx) in mesesNombres" :key="idx" :value="idx">
              {{ mes }}
            </option>
          </select>
        </div>

        <div class="selector-group selector-anio">
          <label class="selector-label">Año</label>
          <select v-model="selectedAnio" class="selector-input">
            <option value="todos">Todos los Años</option>
            <option v-for="yr in availableYears" :key="yr" :value="yr">
              {{ yr }}
            </option>
          </select>
        </div>

        <div class="selector-group selector-conferencia">
          <label class="selector-label">Seleccionar Conferencia Médica a Auditar</label>
          <select v-model="selectedConferenciaId" class="selector-input selector-input-main">
            <option v-if="filteredConferenciasParaSelect.length === 0" :value="null" disabled>
              No hay conferencias en este período
            </option>
            <option
              v-for="conf in filteredConferenciasParaSelect"
              :key="conf.idConferencia"
              :value="conf.idConferencia"
            >
              {{ conf.tituloEvento }} ({{ conf.expositorPonente }})
            </option>
          </select>
        </div>
      </div>
    </section>

    <section v-if="currentConferencia" class="banner-card">
      <div class="banner-meta-row">
        <div class="banner-meta-left">
          <span class="meta-item">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
              <line x1="16" y1="2" x2="16" y2="6" />
              <line x1="8" y1="2" x2="8" y2="6" />
              <line x1="3" y1="10" x2="21" y2="10" />
            </svg>
            {{ formatDateFull(currentConferencia.fechaHoraInicio) }} | {{ formatTimeRange(currentConferencia.fechaHoraInicio, currentConferencia.fechaHoraFin) }}
          </span>
          <span class="meta-item">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
              <polyline points="9 22 9 12 15 12 15 22" />
            </svg>
            {{ currentConferencia.entidadEditorial }}
          </span>
          <span class="meta-item">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10" />
              <line x1="2" y1="12" x2="22" y2="12" />
              <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
            </svg>
            {{ currentConferencia.modalidad }}
          </span>
        </div>
        <div class="banner-meta-right">
          <span :class="['pill-attendance', currentConferencia.asistenciaAbierta ? 'pill-open' : 'pill-closed']">
            {{ currentConferencia.asistenciaAbierta ? 'ASISTENCIA ABIERTA' : 'ASISTENCIA CERRADA' }}
          </span>
          <span class="pill-event-status">
            {{ currentConferencia.estadoEvento || 'Programado' }}
          </span>
        </div>
      </div>
      <h2 class="banner-title">{{ currentConferencia.tituloEvento }}</h2>
      <p class="banner-speaker">
        <span class="speaker-label">Expositor:</span>
        <span class="speaker-name">{{ currentConferencia.expositorPonente }}</span>
      </p>
    </section>

    <section class="kpis-grid">
      <div class="kpi-card kpi-neutral">
        <div class="kpi-header">
          <span class="kpi-title">TOTAL PRE-INSCRITOS</span>
          <span class="kpi-icon">📋</span>
        </div>
        <div class="kpi-number">{{ reportData.kpis.totalPreInscritos }}</div>
        <div class="kpi-desc">Formulario previo</div>
      </div>

      <div class="kpi-card kpi-neutral">
        <div class="kpi-header">
          <span class="kpi-title">TOTAL ASISTENTES</span>
          <span class="kpi-icon">👥</span>
        </div>
        <div class="kpi-number">{{ reportData.kpis.totalAsistentesReales }}</div>
        <div class="kpi-desc">Marcaciones Registradas</div>
      </div>

      <div class="kpi-card kpi-green">
        <div class="kpi-header">
          <span class="kpi-title">ACREDITADOS (OFICIAL)</span>
          <span class="kpi-icon">✓</span>
        </div>
        <div class="kpi-number">{{ reportData.kpis.acreditados }}</div>
        <div class="kpi-desc">Inscritos + Asistieron</div>
      </div>

      <div class="kpi-card kpi-amber">
        <div class="kpi-header">
          <span class="kpi-title">ESPONTÁNEOS</span>
          <span class="kpi-icon">⚡</span>
        </div>
        <div class="kpi-number">{{ reportData.kpis.espontaneos }}</div>
        <div class="kpi-desc">Sin pre-inscripción</div>
      </div>

      <div class="kpi-card kpi-red">
        <div class="kpi-header">
          <span class="kpi-title">INASISTENCIAS</span>
          <span class="kpi-icon">✕</span>
        </div>
        <div class="kpi-number">{{ reportData.kpis.inasistencias }}</div>
        <div class="kpi-desc">Inscritos que faltaron</div>
      </div>

      <div class="kpi-card kpi-dark">
        <div class="kpi-header">
          <span class="kpi-title">EFECTIVIDAD</span>
          <span class="kpi-icon">📈</span>
        </div>
        <div class="kpi-number">{{ reportData.kpis.tasaAsistencia }}%</div>
        <div class="kpi-desc">Tasa de acreditación</div>
      </div>
    </section>

    <section class="table-card">
      <div class="table-controls-row">
        <div class="tabs-group">
          <button
            type="button"
            :class="['tab-btn', { active: activeTab === 'todos' }]"
            @click="activeTab = 'todos'"
          >
            Todos ({{ tabCounts.todos }})
          </button>
          <button
            type="button"
            :class="['tab-btn', { active: activeTab === 'acreditados' }]"
            @click="activeTab = 'acreditados'"
          >
            Acreditados ({{ tabCounts.acreditados }})
          </button>
          <button
            type="button"
            :class="['tab-btn', { active: activeTab === 'espontaneos' }]"
            @click="activeTab = 'espontaneos'"
          >
            Espontáneos ({{ tabCounts.espontaneos }})
          </button>
          <button
            type="button"
            :class="['tab-btn', { active: activeTab === 'inasistencias' }]"
            @click="activeTab = 'inasistencias'"
          >
            Inasistencias ({{ tabCounts.inasistencias }})
          </button>
        </div>

        <div class="filters-group">
          <div class="search-input-wrap">
            <svg class="search-svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="searchQuery"
              type="text"
              class="search-text-field"
              placeholder="Buscar por documento o nombre..."
            />
          </div>

          <select v-model="filterEstamento" class="filter-estamento-select">
            <option value="todos">Todos los Roles</option>
            <option value="Pregrado">Pregrado</option>
            <option value="Posgrado">Posgrado</option>
            <option value="Docente">Docente</option>
            <option value="Externo">Externo</option>
          </select>
        </div>
      </div>

      <div class="table-container">
        <table class="audit-table">
          <thead>
            <tr>
              <th class="th-num">N°</th>
              <th class="th-doc">DOCUMENTO</th>
              <th class="th-part">PARTICIPANTE</th>
              <th class="th-rol">ROL / ESTAMENTO</th>
              <th class="th-ciclo">CICLO</th>
              <th class="th-time">PRE-REGISTRO</th>
              <th class="th-time">HORA MARCACIÓN</th>
              <th class="th-estado">ESTADO</th>
              <th class="th-actions">ACCIONES</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loadingReport">
              <td colspan="9" class="td-empty">
                <div class="spinner-center">Cargando reporte de auditoría...</div>
              </td>
            </tr>
            <tr v-else-if="filteredParticipantes.length === 0">
              <td colspan="9" class="td-empty">
                No se encontraron participantes con los filtros seleccionados.
              </td>
            </tr>
            <tr
              v-for="(part, idx) in filteredParticipantes"
              :key="part.idParticipacion || idx"
              class="table-row"
            >
              <td class="td-num">{{ idx + 1 }}</td>
              <td class="td-doc">
                <div class="doc-cell">
                  <span class="doc-value">{{ part.codigoURP || part.numeroDocumento }}</span>
                  <span class="doc-badge">{{ part.codigoURP ? 'CODIGO_URP' : (part.tipoDocumento || 'DNI') }}</span>
                </div>
              </td>
              <td class="td-part">
                <div class="participant-cell">
                  <span class="participant-name">
                    {{ part.nombres }} {{ part.apellidoPaterno }} {{ part.apellidoMaterno || '' }}
                  </span>
                  <span class="participant-email">{{ part.correo }}</span>
                </div>
              </td>
              <td class="td-rol">
                <span class="badge-role">{{ part.tipoParticipante || 'Estudiante' }}</span>
              </td>
              <td class="td-ciclo">{{ part.ciclo || '-' }}</td>
              <td class="td-time">
                {{ getParticipanteEstado(part) === 'Espontáneo' ? '-' : formatTime12h(part.fechaHoraInscripcion) }}
              </td>
              <td class="td-time">
                {{ part.fechaHoraAsistencia ? formatTime12h(part.fechaHoraAsistencia) : '-' }}
              </td>
              <td class="td-estado">
                <span :class="['pill-status', getParticipanteEstado(part).toLowerCase()]">
                  {{ getParticipanteEstado(part) }}
                </span>
              </td>
              <td class="td-actions">
                <div class="action-items">
                  <span v-if="part.alumnoID !== null" class="badge-empadronado">
                    ✓ En Padrón
                  </span>
                  <button
                    v-else
                    type="button"
                    class="btn-empadronar"
                    @click="empadronar(part)"
                  >
                    👤+ Empadronar
                  </button>
                  <button
                    type="button"
                    class="btn-pencil"
                    title="Subsanar datos de participante"
                    @click="openEditModal(part)"
                  >
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                      <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <footer class="table-footer">
        <span class="footer-count">
          Mostrando {{ filteredParticipantes.length }} de {{ reportData.participantes?.length || 0 }} participantes
        </span>
        <span class="footer-brand">BVE-FAMURP • Sistema de Gestión de Capacitaciones</span>
      </footer>
    </section>

    <div v-if="showEditModal" class="modal-backdrop">
      <div class="modal-box">
        <header class="modal-header">
          <h3 class="modal-title">Subsanar Datos de Participante</h3>
          <button type="button" class="btn-close" @click="showEditModal = false">✕</button>
        </header>

        <section class="modal-body">
          <div class="form-row">
            <label class="form-label">Nombres</label>
            <input v-model="editForm.nombres" type="text" class="form-input" />
          </div>

          <div class="form-row-2">
            <div class="form-row">
              <label class="form-label">Apellido Paterno</label>
              <input v-model="editForm.apellidoPaterno" type="text" class="form-input" />
            </div>
            <div class="form-row">
              <label class="form-label">Apellido Materno</label>
              <input v-model="editForm.apellidoMaterno" type="text" class="form-input" />
            </div>
          </div>

          <div class="form-row">
            <label class="form-label">Correo Electrónico</label>
            <input v-model="editForm.correo" type="email" class="form-input" />
          </div>
        </section>

        <footer class="modal-footer">
          <button type="button" class="btn-cancel" @click="showEditModal = false">Cancelar</button>
          <button
            type="button"
            class="btn-save"
            :disabled="savingEdit"
            @click="saveParticipantEdit"
          >
            {{ savingEdit ? 'Guardando...' : 'Guardar Cambios' }}
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
.reporte-wrapper {
  display: flex;
  flex-direction: column;
  gap: 18px;
  width: 100%;
  box-sizing: border-box;
}

.header-container {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 16px;
}

.header-left {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.header-badge-tag {
  display: inline-block;
  align-self: flex-start;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.72rem;
  font-weight: 700;
  color: #008744;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.header-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.55rem;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.02em;
  margin: 0;
}

.header-subtitle {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.86rem;
  color: #64748b;
  margin: 0;
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
  transition: border-color 0.2s ease, color 0.2s ease;
}

.btn-refresh:hover:not(:disabled) {
  border-color: #008744;
  color: #008744;
}

.btn-refresh:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-export-excel {
  background: #008744;
  color: #ffffff;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: background 0.2s ease;
}

.btn-export-excel:hover:not(:disabled) {
  background: #007A3D;
}

.btn-export-excel:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.selectors-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 14px 18px;
}

.selectors-grid {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
}

.selector-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.selector-mes {
  flex: 0 0 170px;
}

.selector-anio {
  flex: 0 0 150px;
}

.selector-conferencia {
  flex: 1;
  min-width: 320px;
}

.selector-label {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.76rem;
  font-weight: 600;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.selector-input {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 8px 12px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.85rem;
  color: #0f172a;
  outline: none;
  transition: border-color 0.2s ease;
}

.selector-input:focus {
  border-color: #008744;
}

.selector-input-main {
  font-weight: 500;
}

.banner-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 18px 22px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.banner-meta-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
}

.banner-meta-left {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.meta-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.8rem;
  color: #64748b;
  font-weight: 500;
}

.banner-meta-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.pill-attendance {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 3px 9px;
  border-radius: 9999px;
  letter-spacing: 0.04em;
}

.pill-open {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #15803d;
}

.pill-closed {
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  color: #475569;
}

.pill-event-status {
  background: #e0f2fe;
  border: 1px solid #bae6fd;
  color: #0369a1;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 3px 9px;
  border-radius: 9999px;
}

.banner-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.28rem;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.01em;
  margin: 2px 0 0 0;
}

.banner-speaker {
  margin: 0;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.86rem;
  color: #475569;
  display: flex;
  gap: 6px;
}

.speaker-label {
  font-weight: 600;
  color: #64748b;
}

.speaker-name {
  font-weight: 600;
  color: #0f172a;
}

.kpis-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 12px;
}

.kpi-card {
  border-radius: 10px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.kpi-neutral {
  background: #ffffff;
  border: 1px solid #e2e8f0;
}

.kpi-green {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #15803d;
}

.kpi-green .kpi-number {
  color: #15803d;
}

.kpi-green .kpi-title {
  color: #166534;
}

.kpi-green .kpi-desc {
  color: #15803d;
}

.kpi-amber {
  background: #fffbeb;
  border: 1px solid #fde68a;
  color: #b45309;
}

.kpi-amber .kpi-number {
  color: #b45309;
}

.kpi-amber .kpi-title {
  color: #92400e;
}

.kpi-amber .kpi-desc {
  color: #b45309;
}

.kpi-red {
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
}

.kpi-red .kpi-number {
  color: #b91c1c;
}

.kpi-red .kpi-title {
  color: #991b1b;
}

.kpi-red .kpi-desc {
  color: #b91c1c;
}

.kpi-dark {
  background: #0f172a;
  border: 1px solid #0f172a;
  color: #ffffff;
}

.kpi-dark .kpi-title {
  color: #94a3b8;
}

.kpi-dark .kpi-number {
  color: #ffffff;
}

.kpi-dark .kpi-desc {
  color: #8cf9a9;
}

.kpi-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.kpi-title {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.68rem;
  font-weight: 700;
  color: #64748b;
  letter-spacing: 0.04em;
}

.kpi-icon {
  font-size: 0.85rem;
}

.kpi-number {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.55rem;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.2;
}

.kpi-desc {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.72rem;
  color: #64748b;
}

.table-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  overflow: hidden;
}

.table-controls-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  padding: 14px 18px;
  border-bottom: 1px solid #f1f5f9;
}

.tabs-group {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.tab-btn {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #64748b;
  padding: 6px 13px;
  border-radius: 7px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.tab-btn:hover {
  border-color: #94a3b8;
  color: #0f172a;
}

.tab-btn.active {
  background: #0f172a;
  border-color: #0f172a;
  color: #ffffff;
}

.filters-group {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.search-input-wrap {
  position: relative;
  min-width: 260px;
}

.search-svg {
  position: absolute;
  left: 10px;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
  pointer-events: none;
}

.search-text-field {
  width: 100%;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 7px;
  padding: 6px 10px 6px 32px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.8rem;
  color: #0f172a;
  outline: none;
  box-sizing: border-box;
  transition: border-color 0.2s ease;
}

.search-text-field:focus {
  border-color: #008744;
}

.filter-estamento-select {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 7px;
  padding: 6px 12px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.8rem;
  color: #0f172a;
  outline: none;
  transition: border-color 0.2s ease;
}

.filter-estamento-select:focus {
  border-color: #008744;
}

.table-container {
  width: 100%;
  overflow-x: auto;
}

.audit-table {
  width: 100%;
  border-collapse: collapse;
}

.audit-table th {
  background: transparent;
  color: #64748b;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  padding: 10px 14px;
  border-bottom: 1px solid #e2e8f0;
  text-align: left;
}

.audit-table td {
  padding: 10px 14px;
  border-bottom: 1px solid #f1f5f9;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.82rem;
  color: #0f172a;
  vertical-align: middle;
}

.table-row:hover td {
  background: #fcfcfd;
}

.th-num, .td-num {
  width: 44px;
  text-align: center;
  color: #94a3b8;
  font-weight: 600;
}

.doc-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.doc-value {
  font-weight: 600;
  color: #0f172a;
  letter-spacing: -0.01em;
}

.doc-badge {
  font-size: 0.68rem;
  color: #64748b;
  font-weight: 600;
}

.participant-cell {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.participant-name {
  font-weight: 600;
  color: #0f172a;
}

.participant-email {
  font-size: 0.74rem;
  color: #64748b;
}

.badge-role {
  background: #eff6ff;
  border: 1px solid #dbeafe;
  color: #1e40af;
  font-size: 0.72rem;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 9999px;
  white-space: nowrap;
}

.td-ciclo {
  color: #64748b;
  font-weight: 500;
}

.td-time {
  color: #475569;
  font-size: 0.8rem;
  white-space: nowrap;
}

.pill-status {
  font-size: 0.72rem;
  font-weight: 700;
  padding: 3px 9px;
  border-radius: 9999px;
  white-space: nowrap;
  letter-spacing: 0.02em;
}

.pill-status.acreditado {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #15803d;
}

.pill-status.espontáneo, .pill-status.espontaneo {
  background: #fffbeb;
  border: 1px solid #fde68a;
  color: #b45309;
}

.pill-status.inasistencia {
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #b91c1c;
}

.action-items {
  display: flex;
  align-items: center;
  gap: 8px;
}

.badge-empadronado {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #15803d;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 4px 8px;
  border-radius: 6px;
  white-space: nowrap;
}

.btn-empadronar {
  background: #008744;
  color: #ffffff;
  border: none;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.74rem;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: 6px;
  cursor: pointer;
  white-space: nowrap;
  transition: background 0.2s ease;
}

.btn-empadronar:hover {
  background: #007A3D;
}

.btn-pencil {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  padding: 4px 6px;
  border-radius: 6px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: border-color 0.2s ease, color 0.2s ease;
}

.btn-pencil:hover {
  border-color: #008744;
  color: #008744;
}

.td-empty {
  text-align: center;
  padding: 36px 14px;
  color: #64748b;
  font-weight: 500;
}

.spinner-center {
  color: #008744;
  font-weight: 600;
}

.table-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  padding: 12px 18px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
}

.footer-count {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.76rem;
  color: #64748b;
  font-weight: 500;
}

.footer-brand {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.74rem;
  color: #94a3b8;
  font-weight: 500;
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}

.modal-box {
  background: #ffffff;
  border-radius: 12px;
  width: 100%;
  max-width: 480px;
  overflow: hidden;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.15);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 18px;
  border-bottom: 1px solid #e2e8f0;
}

.modal-title {
  margin: 0;
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.05rem;
  font-weight: 700;
  color: #0f172a;
}

.btn-close {
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 1.1rem;
  cursor: pointer;
}

.btn-close:hover {
  color: #0f172a;
}

.modal-body {
  padding: 16px 18px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.form-row-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.form-label {
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.76rem;
  font-weight: 600;
  color: #475569;
}

.form-input {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 7px;
  padding: 7px 10px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.82rem;
  color: #0f172a;
  outline: none;
  transition: border-color 0.2s ease;
}

.form-input:focus {
  border-color: #008744;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 12px 18px;
  border-top: 1px solid #e2e8f0;
  background: #f8fafc;
}

.btn-cancel {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  color: #475569;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.8rem;
  font-weight: 600;
  padding: 7px 14px;
  border-radius: 7px;
  cursor: pointer;
}

.btn-cancel:hover {
  border-color: #94a3b8;
  color: #0f172a;
}

.btn-save {
  background: #008744;
  border: none;
  color: #ffffff;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.8rem;
  font-weight: 600;
  padding: 7px 16px;
  border-radius: 7px;
  cursor: pointer;
  transition: background 0.2s ease;
}

.btn-save:hover:not(:disabled) {
  background: #007A3D;
}

.btn-save:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.floating-toast {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: #0f172a;
  color: #ffffff;
  padding: 10px 18px;
  border-radius: 8px;
  font-family: 'Inter', system-ui, sans-serif;
  font-size: 0.82rem;
  font-weight: 600;
  z-index: 2000;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.2);
  animation: fadeInToast 0.2s ease;
}

@keyframes fadeInToast {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

@media (max-width: 1024px) {
  .kpis-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 640px) {
  .kpis-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .selectors-grid {
    flex-direction: column;
  }
  .selector-mes, .selector-anio, .selector-conferencia {
    flex: 1 1 100%;
    min-width: 100%;
  }
  .form-row-2 {
    grid-template-columns: 1fr;
  }
}
</style>
