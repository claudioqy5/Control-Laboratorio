<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const props = defineProps({
  currentUser: {
    type: Object,
    default: () => null
  }
})

const items = ref([])
const loading = ref(false)
const kpis = ref({
  totalCustodia: 0,
  totalDevueltos: 0,
  totalVencidos: 0,
  totalBajas: 0,
  totalRegistrados: 0
})

const searchQuery = ref('')
const filterEstado = ref('todos')
const filterCategoria = ref('todas')
const filterUbicacion = ref('todas')

const usuarios = ref([])
const alumnosCache = ref({})

const modalNewOpen = ref(false)
const modalDevolverOpen = ref(false)
const modalEmpadronarOpen = ref(false)
const modalActaOpen = ref(false)
const modalBajaConfirmOpen = ref(false)

const selectedItemForActa = ref(null)
const selectedItemForDevolver = ref(null)
const selectedItemForBaja = ref(null)

const verificandoPadron = ref(false)
const alumnoValidado = ref(null)
const padronErrorMsg = ref('')

const formNew = reactive({
  titulo: '',
  categoria: 1,
  descripcion: '',
  lugarHallazgo: '',
  fechaHoraHallazgo: '',
  idUsuarioHallazgo: 1
})

const devFechaDevolucion = ref('')
const devPersonalEntrega = ref(1)
const devCodigoURP = ref('')
const devObservaciones = ref('')
const guardandoDevolucion = ref(false)
const guardandoBaja = ref(false)
const guardandoNuevo = ref(false)

const empadronarForm = reactive({
  codigo: '',
  dni: '',
  nombres: '',
  apellidoPaterno: '',
  apellidoMaterno: '',
  carrera: 'Medicina Humana',
  telefono: '',
  correoInstitucional: '',
  correoPersonal: ''
})
const empadronarError = ref('')
const empadronando = ref(false)

const toast = ref({ show: false, message: '' })
let toastTimer = null

const showToast = (message) => {
  if (toastTimer) clearTimeout(toastTimer)
  toast.value = { show: true, message }
  toastTimer = setTimeout(() => {
    toast.value.show = false
  }, 3500)
}

const currentUserId = computed(() => {
  return props.currentUser?.idUsuarioAdmin || props.currentUser?.IdUsuarioAdmin || 1
})

const currentUserName = computed(() => {
  return props.currentUser?.nombres || props.currentUser?.username || 'Lic. Claudio Quezada'
})

const loadUsuarios = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/usuarios`)
    if (Array.isArray(res.data) && res.data.length > 0) {
      usuarios.value = res.data
      return
    }
  } catch {
  }

  const defaultId = currentUserId.value
  const defaultUser = props.currentUser?.username || 'cquezada'
  usuarios.value = [
    { idUsuarioAdmin: defaultId, username: defaultUser, nombres: currentUserName.value },
    { idUsuarioAdmin: 2, username: 'rquispe', nombres: 'Rosa Quispe (Biblioteca)' },
    { idUsuarioAdmin: 3, username: 'admin_lab', nombres: 'Soporte Técnico Cómputo' },
    { idUsuarioAdmin: 4, username: 'jcorrea', nombres: 'Joshue Correa (SuperAdmin)' }
  ]
}

const loadKpis = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/objetos-perdidos/kpis`)
    if (res.data) {
      kpis.value = {
        totalCustodia: res.data.totalCustodia || 0,
        totalDevueltos: res.data.totalDevueltos || 0,
        totalVencidos: res.data.totalVencidos || 0,
        totalBajas: res.data.totalBajas || 0,
        totalRegistrados: res.data.totalRegistrados || 0
      }
    }
  } catch {
  }
}

const loadItems = async () => {
  loading.value = true
  try {
    const catParam = filterCategoria.value !== 'todas' ? filterCategoria.value : ''
    const res = await axios.get(`${API_BASE_URL}/api/admin/objetos-perdidos`, {
      params: {
        tab: filterEstado.value,
        categoria: catParam,
        search: searchQuery.value.trim()
      }
    })
    items.value = Array.isArray(res.data) ? res.data : []

    items.value.forEach(item => {
      if (item.codigoURPReceptor && !alumnosCache.value[item.codigoURPReceptor]) {
        resolverAlumno(item.codigoURPReceptor)
      }
    })
  } catch {
    showToast('Error al conectar con la bitácora de objetos perdidos')
  } finally {
    loading.value = false
  }
}

const resolverAlumno = async (codigo) => {
  if (!codigo || alumnosCache.value[codigo]) return
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/objetos-perdidos/verificar-alumno/${encodeURIComponent(codigo)}`)
    if (res.data && res.data.existe) {
      alumnosCache.value[codigo] = res.data
    }
  } catch {
  }
}

const filteredItems = computed(() => {
  if (filterUbicacion.value === 'todas') {
    return items.value
  }
  const term = filterUbicacion.value.toLowerCase()
  return items.value.filter(item => (item.lugarHallazgo || '').toLowerCase().includes(term))
})

const getDaysInCustody = (fechaStr) => {
  if (!fechaStr) return 0
  const fecha = new Date(fechaStr)
  const now = new Date()
  const diffMs = now - fecha
  return Math.max(0, Math.floor(diffMs / (1000 * 60 * 60 * 24)))
}

const isItemExpired = (item) => {
  if (item.estadoObjeto === 2 || item.estadoObjeto === 4) return false
  if (item.esVencido !== undefined) return Boolean(item.esVencido)
  return getDaysInCustody(item.fechaHoraHallazgo) > 30
}

const formatDateTime = (isoStr) => {
  if (!isoStr) return '-'
  const d = new Date(isoStr)
  if (isNaN(d.getTime())) return isoStr
  const day = String(d.getDate()).padStart(2, '0')
  const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Set', 'Oct', 'Nov', 'Dic']
  const month = months[d.getMonth()]
  const year = d.getFullYear()
  let hours = d.getHours()
  const minutes = String(d.getMinutes()).padStart(2, '0')
  const ampm = hours >= 12 ? 'p. m.' : 'a. m.'
  hours = hours % 12 || 12
  return `${day} ${month} ${year}, ${hours}:${minutes} ${ampm}`
}

const getCategoryKey = (cat) => {
  const val = Number(cat)
  if (val === 1 || cat === 'Electrónica') return 'electronica'
  if (val === 2 || cat === 'Documentos') return 'documentos'
  if (val === 3 || cat === 'Estudio') return 'estudio'
  if (val === 4 || cat === 'Prendas') return 'prendas'
  return 'otros'
}

const getCategoryLabel = (cat) => {
  const val = Number(cat)
  if (val === 1 || cat === 'Electrónica') return 'Electrónica'
  if (val === 2 || cat === 'Documentos') return 'Documentos'
  if (val === 3 || cat === 'Estudio') return 'Estudio'
  if (val === 4 || cat === 'Prendas') return 'Prendas'
  return 'Otros'
}

const openNewModal = () => {
  const now = new Date()
  const localIso = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
  formNew.titulo = ''
  formNew.categoria = 1
  formNew.descripcion = ''
  formNew.lugarHallazgo = ''
  formNew.fechaHoraHallazgo = localIso
  formNew.idUsuarioHallazgo = currentUserId.value
  modalNewOpen.value = true
}

const handleSaveNew = async () => {
  if (!formNew.titulo.trim() || !formNew.descripcion.trim() || !formNew.lugarHallazgo.trim()) {
    showToast('Por favor complete todos los campos obligatorios')
    return
  }

  guardandoNuevo.value = true
  try {
    const payload = {
      titulo: formNew.titulo.trim(),
      categoria: Number(formNew.categoria),
      descripcion: formNew.descripcion.trim(),
      lugarHallazgo: formNew.lugarHallazgo.trim(),
      fechaHoraHallazgo: new Date(formNew.fechaHoraHallazgo).toISOString(),
      idUsuarioHallazgo: Number(formNew.idUsuarioHallazgo || currentUserId.value)
    }

    const res = await axios.post(`${API_BASE_URL}/api/admin/objetos-perdidos`, payload)
    modalNewOpen.value = false
    await loadItems()
    await loadKpis()
    const nuevoId = res.data?.idObjeto || ''
    showToast(`Objeto registrado en custodia exitosamente ${nuevoId ? `#${nuevoId}` : ''}`)
  } catch {
    showToast('Error al registrar el objeto en custodia')
  } finally {
    guardandoNuevo.value = false
  }
}

const openDevolverModal = (item) => {
  selectedItemForDevolver.value = item
  devCodigoURP.value = ''
  alumnoValidado.value = null
  padronErrorMsg.value = ''
  devObservaciones.value = ''
  const now = new Date()
  devFechaDevolucion.value = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
  devPersonalEntrega.value = currentUserId.value
  modalDevolverOpen.value = true
}

const verificarEnPadron = async () => {
  const cod = devCodigoURP.value.trim()
  if (!cod) {
    padronErrorMsg.value = 'Por favor ingrese el Código Universitario URP para verificar.'
    alumnoValidado.value = null
    return
  }

  verificandoPadron.value = true
  padronErrorMsg.value = ''
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/objetos-perdidos/verificar-alumno/${encodeURIComponent(cod)}`)
    if (res.data && res.data.existe) {
      alumnoValidado.value = res.data
      alumnosCache.value[res.data.codigoUniversitario] = res.data
      padronErrorMsg.value = ''
      showToast(`Estudiante ${res.data.nombres} validado en el sistema`)
    } else {
      alumnoValidado.value = null
      padronErrorMsg.value = 'Estudiante no figura en el sistema'
    }
  } catch {
    alumnoValidado.value = null
    padronErrorMsg.value = 'Estudiante no figura en el sistema'
  } finally {
    verificandoPadron.value = false
  }
}

const abrirModalEmpadronar = () => {
  const cod = devCodigoURP.value.trim()
  empadronarForm.codigo = cod
  empadronarForm.dni = ''
  empadronarForm.nombres = ''
  empadronarForm.apellidoPaterno = ''
  empadronarForm.apellidoMaterno = ''
  empadronarForm.carrera = 'Medicina Humana'
  empadronarForm.telefono = ''
  empadronarForm.correoInstitucional = cod ? `${cod}@urp.edu.pe` : ''
  empadronarForm.correoPersonal = ''
  empadronarError.value = ''
  modalEmpadronarOpen.value = true
}

const actualizarCorreoSugerido = () => {
  const cod = empadronarForm.codigo.trim()
  if (cod) {
    empadronarForm.correoInstitucional = `${cod}@urp.edu.pe`
  }
}

const guardarNuevoAlumno = async () => {
  if (!empadronarForm.codigo.trim() || !empadronarForm.dni.trim() || !empadronarForm.nombres.trim() || !empadronarForm.apellidoPaterno.trim() || !empadronarForm.apellidoMaterno.trim() || !empadronarForm.carrera.trim()) {
    empadronarError.value = 'Por favor complete todos los campos obligatorios (*).'
    return
  }

  empadronando.value = true
  empadronarError.value = ''
  try {
    const payload = {
      codigoUniversitario: empadronarForm.codigo.trim(),
      dni: empadronarForm.dni.trim(),
      nombres: empadronarForm.nombres.trim(),
      apellidoPaterno: empadronarForm.apellidoPaterno.trim(),
      apellidoMaterno: empadronarForm.apellidoMaterno.trim(),
      carrera: empadronarForm.carrera.trim(),
      telefono: empadronarForm.telefono.trim(),
      correoInstitucional: empadronarForm.correoInstitucional.trim() || `${empadronarForm.codigo.trim()}@urp.edu.pe`,
      correoPersonal: empadronarForm.correoPersonal.trim(),
      estado: true
    }

    const res = await axios.post(`${API_BASE_URL}/api/alumnos`, payload)
    modalEmpadronarOpen.value = false
    const alumnoCreado = res.data || payload
    const nombreCompleto = `${alumnoCreado.nombres || payload.nombres} ${alumnoCreado.apellidoPaterno || payload.apellidoPaterno} ${alumnoCreado.apellidoMaterno || payload.apellidoMaterno}`.trim()

    alumnoValidado.value = {
      existe: true,
      alumnoId: alumnoCreado.alumnoID || alumnoCreado.AlumnoID || 0,
      codigoUniversitario: payload.codigoUniversitario,
      dni: payload.dni,
      nombres: nombreCompleto,
      carrera: payload.carrera,
      correoInstitucional: payload.correoInstitucional
    }

    alumnosCache.value[payload.codigoUniversitario] = alumnoValidado.value
    devCodigoURP.value = payload.codigoUniversitario
    padronErrorMsg.value = ''
    showToast(`Usuario ${payload.nombres} ${payload.apellidoPaterno} registrado en el padrón`)
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al empadronar al nuevo estudiante'
    empadronarError.value = msg
  } finally {
    empadronando.value = false
  }
}

const handleSaveDevolucion = async () => {
  if (!selectedItemForDevolver.value || !alumnoValidado.value) {
    showToast('Debe validar o empadronar al estudiante antes de confirmar la entrega')
    return
  }

  guardandoDevolucion.value = true
  try {
    const itemId = selectedItemForDevolver.value.idObjeto
    const payload = {
      codigoUniversitario: alumnoValidado.value.codigoUniversitario,
      idUsuarioEntrega: Number(devPersonalEntrega.value || currentUserId.value),
      observaciones: devObservaciones.value.trim() || null
    }

    const res = await axios.post(`${API_BASE_URL}/api/admin/objetos-perdidos/${itemId}/devolver`, payload)
    const itemActualizado = res.data?.objeto || {
      ...selectedItemForDevolver.value,
      estadoObjeto: 2,
      fechaHoraDevolucion: new Date().toISOString(),
      codigoURPReceptor: alumnoValidado.value.codigoUniversitario,
      documentoReceptor: alumnoValidado.value.dni,
      observacionesEntrega: payload.observaciones
    }

    modalDevolverOpen.value = false
    await loadItems()
    await loadKpis()
    showToast(`Devolución confirmada para el objeto #${itemId}`)
    openActa(itemActualizado)
  } catch {
    showToast('Error al confirmar la devolución del objeto')
  } finally {
    guardandoDevolucion.value = false
  }
}

const openConfirmarBaja = (item) => {
  selectedItemForBaja.value = item
  modalBajaConfirmOpen.value = true
}

const ejecutarBajaDefinitiva = async () => {
  if (!selectedItemForBaja.value) return
  guardandoBaja.value = true
  try {
    const itemId = selectedItemForBaja.value.idObjeto
    await axios.post(`${API_BASE_URL}/api/admin/objetos-perdidos/${itemId}/dar-de-baja`)
    modalBajaConfirmOpen.value = false
    await loadItems()
    await loadKpis()
    showToast(`Objeto #${itemId} registrado como Dispuesto / Baja Definitiva`)
  } catch {
    showToast('Error al asentar la baja definitiva del objeto')
  } finally {
    guardandoBaja.value = false
  }
}

const openActa = (item) => {
  selectedItemForActa.value = item
  modalActaOpen.value = true
}

const imprimirActa = () => {
  window.print()
}

const exportToCSV = () => {
  if (items.value.length === 0) {
    showToast('No hay registros disponibles para exportar')
    return
  }

  let csv = 'ID,Titulo,Categoria,Fecha Hallazgo,Lugar Hallazgo,Estado,Receptor Alumno,DNI,Codigo URP,Fecha Devolucion\n'
  items.value.forEach(i => {
    const nomAlumno = alumnosCache.value[i.codigoURPReceptor]?.nombres || (i.codigoURPReceptor ? 'Estudiante URP' : '-')
    const dni = i.documentoReceptor || '-'
    const cod = i.codigoURPReceptor || '-'
    const fDev = i.fechaHoraDevolucion ? formatDateTime(i.fechaHoraDevolucion) : '-'
    const fHal = i.fechaHoraHallazgo ? formatDateTime(i.fechaHoraHallazgo) : '-'
    const est = i.estadoObjeto === 2 ? 'Devuelto' : (i.estadoObjeto === 4 ? 'Baja Definitiva' : (isItemExpired(i) ? 'Vencido' : 'En Custodia'))
    csv += `${i.idObjeto},"${i.titulo.replace(/"/g, '""')}","${i.categoriaNombre || getCategoryLabel(i.categoria)}","${fHal}","${(i.lugarHallazgo || '').replace(/"/g, '""')}","${est}","${nomAlumno}","${dni}","${cod}","${fDev}"\n`
  })

  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = `bitacora_objetos_perdidos_${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  showToast('Descargando archivo CSV de bitácora institucional')
}

onMounted(async () => {
  await loadUsuarios()
  await Promise.all([loadKpis(), loadItems()])
})
</script>

<template>
  <div class="content-area">
    <header class="header-section">
      <div>
        <h1 class="page-title">Objetos Perdidos &amp; Devoluciones</h1>
        <p class="page-subtitle">Trazabilidad y custodia institucional de pertenencias encontradas en la facultad</p>
      </div>
      <div class="header-actions">
        <button type="button" class="btn btn-secondary" @click="exportToCSV">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
            <polyline points="7 10 12 15 17 10" />
            <line x1="12" y1="15" x2="12" y2="3" />
          </svg>
          Exportar Bitácora
        </button>
        <button type="button" class="btn btn-primary" @click="openNewModal">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          + Registrar Hallazgo
        </button>
      </div>
    </header>

    <section class="kpi-grid">
      <article class="kpi-card">
        <div class="kpi-icon custodia">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
            <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
            <line x1="12" y1="22.08" x2="12" y2="12" />
          </svg>
        </div>
        <div class="kpi-info">
          <span class="kpi-label">En Custodia Activa</span>
          <span class="kpi-value">{{ kpis.totalCustodia }}</span>
          <span class="kpi-subtext">Plazo vigente (≤ 30 días)</span>
        </div>
      </article>

      <article class="kpi-card">
        <div class="kpi-icon devueltos">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
            <polyline points="22 4 12 14.01 9 11.01" />
          </svg>
        </div>
        <div class="kpi-info">
          <span class="kpi-label">Devueltos a Alumnos</span>
          <span class="kpi-value">{{ kpis.totalDevueltos }}</span>
          <span class="kpi-subtext">Acreditados con DNI y código</span>
        </div>
      </article>

      <article class="kpi-card">
        <div class="kpi-icon vencido">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <circle cx="12" cy="16" r="1.2" fill="currentColor" stroke="none" />
          </svg>
        </div>
        <div class="kpi-info">
          <span class="kpi-label">Plazo Vencido (&gt; 30 d)</span>
          <span class="kpi-value">{{ kpis.totalVencidos }}</span>
          <span class="kpi-subtext">Pendiente de disposición final</span>
        </div>
      </article>

      <article class="kpi-card">
        <div class="kpi-icon total">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
          </svg>
        </div>
        <div class="kpi-info">
          <span class="kpi-label">Total en Bitácora</span>
          <span class="kpi-value">{{ kpis.totalRegistrados }}</span>
          <span class="kpi-subtext">Histórico general</span>
        </div>
      </article>
    </section>

    <div class="open-filter-row">
      <div class="open-search-wrap">
        <svg class="open-search-svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          class="open-search-input"
          placeholder="Buscar por DNI, código URP, nombre del objeto, ubicación o receptor..."
          @input="loadItems"
        />
      </div>

      <div class="open-filters-right">
        <select v-model="filterEstado" class="open-select" @change="loadItems">
          <option value="todos">Todos los Estados</option>
          <option value="custodia">En Custodia Activa (≤ 30 d)</option>
          <option value="devueltos">Devueltos a Alumnos</option>
          <option value="vencidos">Plazo Vencido (&gt; 30 d)</option>
          <option value="bajas">Baja / Dispuestos</option>
        </select>

        <select v-model="filterCategoria" class="open-select" @change="loadItems">
          <option value="todas">Todas las Categorías</option>
          <option value="1">Electrónica &amp; Accesorios</option>
          <option value="2">Documentos &amp; Carnés</option>
          <option value="3">Material de Estudio / Libros</option>
          <option value="4">Prendas &amp; Accesorios</option>
          <option value="5">Otros Objetos</option>
        </select>

        <select v-model="filterUbicacion" class="open-select">
          <option value="todas">Todas las Ubicaciones</option>
          <option value="Laboratorio">Laboratorios de Cómputo</option>
          <option value="Biblioteca">Salas de Biblioteca</option>
          <option value="Hemeroteca">Hemeroteca</option>
          <option value="Cubículos">Cubículos de Estudio</option>
        </select>
      </div>
    </div>

    <div class="table-responsive">
      <table class="free-table">
        <thead>
          <tr>
            <th class="col-num">Nº</th>
            <th style="width: 28%;">Objeto / Categoría</th>
            <th style="width: 22%;">Hallazgo &amp; Ubicación</th>
            <th style="width: 16%;">Estado</th>
            <th style="width: 20%;">Entrega a Alumno</th>
            <th style="width: 14%; text-align: right;">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="6" class="empty-state">
              Cargando bitácora de objetos perdidos...
            </td>
          </tr>
          <tr v-else-if="filteredItems.length === 0">
            <td colspan="6" class="empty-state">
              No se encontraron registros que coincidan con los criterios de búsqueda.
            </td>
          </tr>
          <tr v-for="(item, index) in filteredItems" :key="item.idObjeto">
            <td class="col-num">{{ index + 1 }}</td>
            <td>
              <div class="item-cell">
                <div class="item-icon-box">
                  <svg v-if="getCategoryKey(item.categoria) === 'electronica'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
                    <line x1="8" y1="21" x2="16" y2="21" />
                    <line x1="12" y1="17" x2="12" y2="21" />
                  </svg>
                  <svg v-else-if="getCategoryKey(item.categoria) === 'documentos'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                    <line x1="16" y1="2" x2="16" y2="6" />
                    <line x1="8" y1="2" x2="8" y2="6" />
                    <line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                  <svg v-else-if="getCategoryKey(item.categoria) === 'estudio'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
                    <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
                  </svg>
                  <svg v-else-if="getCategoryKey(item.categoria) === 'prendas'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M20.38 3.46L16 2a4 4 0 0 1-8 0L3.62 3.46a2 2 0 0 0-1.34 2.23l.58 3.47a1 1 0 0 0 .99.84H6v10c0 1.1.9 2 2 2h8a2 2 0 0 0 2-2V10h2.15a1 1 0 0 0 .99-.84l.58-3.47a2 2 0 0 0-1.34-2.23z" />
                  </svg>
                  <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
                    <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
                    <line x1="12" y1="22.08" x2="12" y2="12" />
                  </svg>
                </div>
                <div>
                  <span class="item-title">{{ item.titulo }}</span>
                  <span class="badge-category">{{ item.categoriaNombre || getCategoryLabel(item.categoria) }}</span>
                </div>
              </div>
            </td>
            <td>
              <span class="meta-date">{{ formatDateTime(item.fechaHoraHallazgo) }}</span>
              <span class="meta-sub">Lugar: {{ item.lugarHallazgo }}</span>
              <span class="meta-sub">Recepcionó: {{ item.nombrePersonalHallazgo }}</span>
            </td>
            <td>
              <template v-if="item.estadoObjeto === 2">
                <span class="badge badge-devuelto">
                  <span class="badge-dot"></span>Devuelto
                </span>
                <div class="estado-sub-block">
                  <span class="date-cell">{{ formatDateTime(item.fechaHoraDevolucion) }}</span>
                  <span class="conformidad-text">✓ Entregado conforme</span>
                </div>
              </template>
              <template v-else-if="item.estadoObjeto === 4">
                <span class="badge badge-dispuesto">
                  <span class="badge-dot"></span>Baja Definitiva
                </span>
                <div class="estado-sub-block">
                  <span class="date-cell">{{ formatDateTime(item.fechaBaja) }}</span>
                  <span class="baja-subtext">Baja formal asentada</span>
                </div>
              </template>
              <template v-else-if="isItemExpired(item)">
                <span class="badge badge-vencido">
                  <span class="badge-dot"></span>Plazo Vencido
                </span>
                <div class="estado-sub-block">
                  <span class="overdue-text">⚠️ Vencido hace {{ Math.max(1, getDaysInCustody(item.fechaHoraHallazgo) - 30) }} d</span>
                </div>
              </template>
              <template v-else>
                <span class="badge badge-custodia">
                  <span class="badge-dot"></span>En Custodia
                </span>
                <div class="estado-sub-block">
                  <span class="remaining-text">⏳ Quedan {{ Math.max(0, 30 - getDaysInCustody(item.fechaHoraHallazgo)) }} día(s)</span>
                </div>
              </template>
            </td>
            <td>
              <div v-if="item.estadoObjeto === 2" class="receptor-block">
                <span class="receptor-name">{{ alumnosCache[item.codigoURPReceptor]?.nombres || 'Estudiante Validado' }}</span>
                <div class="receptor-ids">
                  <span v-if="item.documentoReceptor">DNI: <strong class="pill-id">{{ item.documentoReceptor }}</strong></span>
                  <span v-if="item.codigoURPReceptor">Cód: <strong class="pill-id">{{ item.codigoURPReceptor }}</strong></span>
                </div>
                <span v-if="item.nombrePersonalEntrega" class="meta-sub">Entregó: {{ item.nombrePersonalEntrega }}</span>
              </div>
              <span v-else-if="item.estadoObjeto === 4" class="text-muted-sm">
                Baja física asentada formalmente
              </span>
              <div v-else-if="isItemExpired(item)">
                <span class="text-danger-sm">Superó plazo de 30 días.</span>
                <span class="text-muted-xs">Listo para baja física o entrega extemporánea</span>
              </div>
              <div v-else>
                <span class="text-muted-sm">En espera de reclamo institucional</span>
                <span class="text-muted-xs">Plazo fijo de custodia: 30 días</span>
              </div>
            </td>
            <td>
              <div class="actions-cell">
                <button
                  v-if="item.estadoObjeto === 2 || item.estadoObjeto === 4"
                  type="button"
                  class="btn btn-sm btn-secondary"
                  @click="openActa(item)"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                    <polyline points="14 2 14 8 20 8" />
                    <line x1="16" y1="13" x2="8" y2="13" />
                    <line x1="16" y1="17" x2="8" y2="17" />
                  </svg>
                  Ver Ficha
                </button>
                <template v-else>
                  <button
                    type="button"
                    class="icon-btn-baja"
                    title="Dar de baja definitiva"
                    @click="openConfirmarBaja(item)"
                  >
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <circle cx="12" cy="12" r="10" />
                      <line x1="15" y1="9" x2="9" y2="15" />
                      <line x1="9" y1="9" x2="15" y2="15" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-action-devolver"
                    title="Devolución a alumno"
                    @click="openDevolverModal(item)"
                  >
                    Devolver
                  </button>
                </template>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <footer class="free-footer">
      <span>Mostrando {{ filteredItems.length }} de {{ items.length }} registros</span>
    </footer>

    <div class="modal-overlay" :class="{ active: modalNewOpen }">
      <div class="modal-card">
        <div class="modal-header">
          <h2 class="modal-title">Registrar Objeto Encontrado</h2>
          <button type="button" class="modal-close" @click="modalNewOpen = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
        <form @submit.prevent="handleSaveNew">
          <div class="modal-body">
            <div class="form-group-row">
              <div class="form-group">
                <label class="form-label">Categoría del Objeto <span class="req">*</span></label>
                <select v-model.number="formNew.categoria" class="form-control" required>
                  <option :value="1">Electrónica &amp; Accesorios</option>
                  <option :value="2">Documentos &amp; Carnés</option>
                  <option :value="3">Material de Estudio / Libros</option>
                  <option :value="4">Prendas &amp; Accesorios</option>
                  <option :value="5">Otros</option>
                </select>
              </div>
              <div class="form-group">
                <label class="form-label">Personal Administrativo Receptor <span class="req">*</span></label>
                <select v-model.number="formNew.idUsuarioHallazgo" class="form-control" required>
                  <option v-for="u in usuarios" :key="u.idUsuarioAdmin" :value="u.idUsuarioAdmin">
                    {{ u.username }} • {{ u.nombres }}
                  </option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">Nombre / Denominación del Objeto <span class="req">*</span></label>
              <input
                v-model="formNew.titulo"
                type="text"
                class="form-control"
                placeholder="Ej. Calculadora Científica Casio FX-991LAX"
                required
              />
            </div>

            <div class="form-group">
              <label class="form-label">Descripción Detallada &amp; Señas Particulares <span class="req">*</span></label>
              <textarea
                v-model="formNew.descripcion"
                class="form-control"
                placeholder="Color, detalles distintivos, stickers, funda, marcas de uso..."
                required
              ></textarea>
            </div>

            <div class="form-group-row">
              <div class="form-group">
                <label class="form-label">Lugar de Hallazgo <span class="req">*</span></label>
                <input
                  v-model="formNew.lugarHallazgo"
                  type="text"
                  class="form-control"
                  placeholder="Ej. Laboratorio Cómputo 2 - Mesa 4"
                  required
                />
              </div>
              <div class="form-group">
                <label class="form-label">Fecha y Hora de Hallazgo <span class="req">*</span></label>
                <input
                  v-model="formNew.fechaHoraHallazgo"
                  type="datetime-local"
                  class="form-control"
                  required
                />
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="modalNewOpen = false">Cancelar</button>
            <button type="submit" class="btn btn-primary" :disabled="guardandoNuevo">
              {{ guardandoNuevo ? 'Guardando...' : 'Guardar en Custodia' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <div class="modal-overlay" :class="{ active: modalDevolverOpen }">
      <div class="modal-card">
        <div class="modal-header">
          <h2 class="modal-title">Registrar Devolución a Alumno</h2>
          <button type="button" class="modal-close" @click="modalDevolverOpen = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
        <form @submit.prevent="handleSaveDevolucion">
          <div v-if="selectedItemForDevolver" class="modal-body">
            <div class="item-banner">
              <div class="item-icon-box">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
                </svg>
              </div>
              <div class="item-banner-info">
                <div class="item-banner-title">{{ selectedItemForDevolver.titulo }}</div>
                <div class="item-banner-desc">
                  Registro #{{ selectedItemForDevolver.idObjeto }} • Hallado el {{ formatDateTime(selectedItemForDevolver.fechaHoraHallazgo) }} en {{ selectedItemForDevolver.lugarHallazgo }}
                </div>
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">Código Universitario URP <span class="req">*</span></label>
              <div class="input-verify-group">
                <input
                  v-model="devCodigoURP"
                  type="text"
                  class="form-control"
                  placeholder="Ej. 202211070"
                  maxlength="12"
                  required
                  @keydown.enter.prevent="verificarEnPadron"
                />
                <button
                  type="button"
                  class="btn btn-secondary btn-sm"
                  :disabled="verificandoPadron"
                  @click="verificarEnPadron"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8" />
                    <line x1="21" y1="21" x2="16.65" y2="16.65" />
                  </svg>
                  {{ verificandoPadron ? '...' : 'Verificar' }}
                </button>
              </div>
            </div>

            <div v-if="alumnoValidado" class="status-padron-box status-padron-success">
              <div class="status-padron-content">
                <div class="status-padron-title">
                  ✓ Estudiante Validado: {{ alumnoValidado.nombres }}
                </div>
                <div class="status-padron-meta">
                  DNI: <strong>{{ alumnoValidado.dni }}</strong> • Código: <strong>{{ alumnoValidado.codigoUniversitario }}</strong> • {{ alumnoValidado.carrera }}
                </div>
              </div>
              <span class="badge-validado">VALIDADO</span>
            </div>

            <div v-else-if="padronErrorMsg" class="status-padron-box status-padron-warning">
              <div class="status-warning-content">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="12" />
                  <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                <span>{{ padronErrorMsg }}</span>
              </div>
              <button type="button" class="btn btn-sm btn-primary" @click="abrirModalEmpadronar">
                + Empadronar Ahora
              </button>
            </div>

            <div class="form-group-row">
              <div class="form-group">
                <label class="form-label">Fecha y Hora de Entrega <span class="req">*</span></label>
                <input
                  v-model="devFechaDevolucion"
                  type="datetime-local"
                  class="form-control"
                  required
                />
              </div>
              <div class="form-group">
                <label class="form-label">Personal Administrativo que Entrega <span class="req">*</span></label>
                <select v-model.number="devPersonalEntrega" class="form-control" required>
                  <option v-for="u in usuarios" :key="u.idUsuarioAdmin" :value="u.idUsuarioAdmin">
                    {{ u.username }} • {{ u.nombres }}
                  </option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">Observaciones de Entrega y Conformidad</label>
              <textarea
                v-model="devObservaciones"
                class="form-control"
                placeholder="Ej. El estudiante presentó DNI original coincidente y describió con precisión los detalles del objeto."
              ></textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="modalDevolverOpen = false">Cancelar</button>
            <button
              type="submit"
              class="btn btn-primary"
              :disabled="!alumnoValidado || guardandoDevolucion"
            >
              {{ guardandoDevolucion ? 'Confirmando...' : 'Confirmar' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <div class="modal-overlay modal-high" :class="{ active: modalEmpadronarOpen }">
      <div class="modal-card modal-card-lg">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon-badge">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                <circle cx="8.5" cy="7" r="4" />
                <line x1="20" y1="8" x2="20" y2="14" />
                <line x1="23" y1="11" x2="17" y2="11" />
              </svg>
            </div>
            <div>
              <h3 class="modal-title-custom">Nuevo Usuario</h3>
              <span class="modal-subtitle-custom">Padrón Institucional de Estudiantes ControlLaboratorio</span>
            </div>
          </div>
          <button type="button" class="close-btn" @click="modalEmpadronarOpen = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <form @submit.prevent="guardarNuevoAlumno">
          <div class="modal-body modal-scroll-body">
            <div v-if="empadronarError" class="alert-error-box">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>{{ empadronarError }}</span>
            </div>

            <div class="form-grid-2">
              <div class="form-field-vertical">
                <label class="label-premium">Código Universitario / ID *</label>
                <input
                  v-model="empadronarForm.codigo"
                  type="text"
                  placeholder="Ej. 20212345"
                  class="premium-input"
                  required
                  @input="actualizarCorreoSugerido"
                />
              </div>
              <div class="form-field-vertical">
                <label class="label-premium">DNI *</label>
                <input
                  v-model="empadronarForm.dni"
                  type="text"
                  placeholder="Nro. Documento"
                  class="premium-input"
                  maxlength="12"
                  required
                />
              </div>
            </div>

            <div class="form-field-vertical">
              <label class="label-premium">Nombres *</label>
              <input
                v-model="empadronarForm.nombres"
                type="text"
                placeholder="Nombres completos"
                class="premium-input"
                required
              />
            </div>

            <div class="form-grid-2">
              <div class="form-field-vertical">
                <label class="label-premium">Apellido Paterno *</label>
                <input
                  v-model="empadronarForm.apellidoPaterno"
                  type="text"
                  placeholder="Paterno"
                  class="premium-input"
                  required
                />
              </div>
              <div class="form-field-vertical">
                <label class="label-premium">Apellido Materno *</label>
                <input
                  v-model="empadronarForm.apellidoMaterno"
                  type="text"
                  placeholder="Materno"
                  class="premium-input"
                  required
                />
              </div>
            </div>

            <div class="form-grid-2">
              <div class="form-field-vertical">
                <label class="label-premium">Carrera / Cargo *</label>
                <select v-model="empadronarForm.carrera" class="premium-input select-pointer" required>
                  <option value="Medicina Humana">Medicina Humana</option>
                  <option value="Ingeniería Informática">Ingeniería Informática</option>
                  <option value="Ingeniería Industrial">Ingeniería Industrial</option>
                  <option value="Ingeniería Civil">Ingeniería Civil</option>
                  <option value="Ingeniería Electrónica">Ingeniería Electrónica</option>
                  <option value="Ingeniería Mecatrónica">Ingeniería Mecatrónica</option>
                  <option value="Administración y Gerencia">Administración y Gerencia</option>
                  <option value="Administración de Negocios Globales">Administración de Negocios Globales</option>
                  <option value="Contabilidad y Finanzas">Contabilidad y Finanzas</option>
                  <option value="Economía">Economía</option>
                  <option value="Marketing Global y Administración Comercial">Marketing Global y Administración Comercial</option>
                  <option value="Turismo, Hotelería y Gastronomía">Turismo, Hotelería y Gastronomía</option>
                  <option value="Arquitectura y Urbanismo">Arquitectura y Urbanismo</option>
                  <option value="Psicología">Psicología</option>
                  <option value="Biología">Biología</option>
                  <option value="Medicina Veterinaria">Medicina Veterinaria</option>
                  <option value="Derecho y Ciencia Política">Derecho y Ciencia Política</option>
                  <option value="Traducción e Interpretación">Traducción e Interpretación</option>
                </select>
              </div>
              <div class="form-field-vertical">
                <label class="label-premium">Teléfono</label>
                <input
                  v-model="empadronarForm.telefono"
                  type="text"
                  placeholder="Opcional"
                  class="premium-input"
                />
              </div>
            </div>

            <div class="form-grid-2">
              <div class="form-field-vertical">
                <label class="label-premium">Correo Institucional</label>
                <input
                  v-model="empadronarForm.correoInstitucional"
                  type="email"
                  placeholder="usuario@urp.edu.pe"
                  class="premium-input"
                />
              </div>
              <div class="form-field-vertical">
                <label class="label-premium">Correo Personal</label>
                <input
                  v-model="empadronarForm.correoPersonal"
                  type="email"
                  placeholder="usuario@gmail.com"
                  class="premium-input"
                />
              </div>
            </div>
          </div>

          <div class="modal-footer modal-footer-custom">
            <button type="button" class="btn btn-secondary" @click="modalEmpadronarOpen = false">Cancelar</button>
            <button type="submit" class="btn btn-primary" :disabled="empadronando">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" />
                <polyline points="17 21 17 13 7 13 7 21" />
                <polyline points="7 3 7 8 15 8" />
              </svg>
              <span>{{ empadronando ? 'Guardando...' : 'Guardar Datos' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <div class="modal-overlay" :class="{ active: modalActaOpen }">
      <div class="modal-card modal-card-acta">
        <div class="modal-header">
          <h2 class="modal-title">Acta Oficial de Devolución</h2>
          <div class="header-modal-actions">
            <button type="button" class="btn btn-secondary btn-sm" @click="imprimirActa">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="6 9 6 2 18 2 18 9" />
                <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
                <rect x="6" y="14" width="12" height="8" />
              </svg>
              Imprimir Acta
            </button>
            <button type="button" class="modal-close" @click="modalActaOpen = false">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </div>
        </div>
        <div v-if="selectedItemForActa" class="modal-body">
          <div class="acta-sheet">
            <div class="acta-header">
              <div class="acta-facultad">Universidad Ricardo Palma • Facultad de Medicina Humana</div>
              <div class="acta-subfacultad">
                Biblioteca Virtual y Especializada (BVE) • Centro de Cómputo
              </div>
              <div class="acta-folio">REGISTRO DE CUSTODIA Y ENTREGA: OBJETO #{{ selectedItemForActa.idObjeto }}</div>
              <h3 class="acta-doc-title">
                {{ selectedItemForActa.estadoObjeto === 2 ? 'Acta de Conformidad de Devolución de Pertenencia' : (selectedItemForActa.estadoObjeto === 4 ? 'Acta de Disposición y Baja Definitiva' : 'Ficha de Registro y Custodia') }}
              </h3>
            </div>

            <div class="acta-section-title">1. Datos del Objeto Encontrado</div>
            <div class="acta-grid">
              <div><span class="acta-key">Denominación:</span><span class="acta-val">{{ selectedItemForActa.titulo }}</span></div>
              <div><span class="acta-key">Categoría:</span><span class="acta-val">{{ selectedItemForActa.categoriaNombre || getCategoryLabel(selectedItemForActa.categoria) }}</span></div>
              <div class="acta-row-full"><span class="acta-key">Descripción detallada:</span><span class="acta-val">{{ selectedItemForActa.descripcion }}</span></div>
              <div><span class="acta-key">Fecha de Hallazgo:</span><span class="acta-val">{{ formatDateTime(selectedItemForActa.fechaHoraHallazgo) }}</span></div>
              <div><span class="acta-key">Lugar de Hallazgo:</span><span class="acta-val">{{ selectedItemForActa.lugarHallazgo }}</span></div>
              <div class="acta-row-full"><span class="acta-key">Personal que Recepcionó:</span><span class="acta-val">{{ selectedItemForActa.nombrePersonalHallazgo }}</span></div>
            </div>

            <template v-if="selectedItemForActa.estadoObjeto === 2">
              <div class="acta-section-title acta-sec-gap">2. Datos del Estudiante Propietario</div>
              <div class="acta-grid">
                <div><span class="acta-key">Nombres y Apellidos:</span><span class="acta-val">{{ alumnosCache[selectedItemForActa.codigoURPReceptor]?.nombres || 'Estudiante URP' }}</span></div>
                <div><span class="acta-key">Código Universitario:</span><span class="acta-val acta-code-highlight">{{ selectedItemForActa.codigoURPReceptor }}</span></div>
                <div><span class="acta-key">DNI / Documento:</span><span class="acta-val">{{ selectedItemForActa.documentoReceptor }}</span></div>
                <div><span class="acta-key">Fecha y Hora de Entrega:</span><span class="acta-val">{{ formatDateTime(selectedItemForActa.fechaHoraDevolucion) }}</span></div>
                <div class="acta-row-full"><span class="acta-key">Personal que Efectuó Entrega:</span><span class="acta-val">{{ selectedItemForActa.nombrePersonalEntrega }}</span></div>
                <div class="acta-row-full"><span class="acta-key">Observaciones de Entrega:</span><span class="acta-val">{{ selectedItemForActa.observacionesEntrega || 'Entrega física conforme acreditada con documento.' }}</span></div>
              </div>
            </template>
            <div v-else-if="selectedItemForActa.estadoObjeto === 4" class="acta-banner-baja">
              <strong>Estado: DISPOSICIÓN / BAJA DEFINITIVA.</strong> Objeto dado de baja y dispuesto conforme a la normativa institucional de la facultad tras superar el periodo máximo de 30 días de custodia sin reclamo. Fecha de asiento: {{ formatDateTime(selectedItemForActa.fechaBaja) }}.
            </div>
            <div v-else class="acta-banner-custodia">
              <strong>Estado: EN CUSTODIA ACTIVA.</strong> Pertenencia en resguardo en recepción de cómputo a la espera de reclamo institucional por parte del estudiante acreditando código URP y DNI.
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="modalActaOpen = false">Cerrar</button>
        </div>
      </div>
    </div>

    <div class="modal-overlay" :class="{ active: modalBajaConfirmOpen }">
      <div class="modal-card modal-card-sm">
        <div class="modal-header">
          <h2 class="modal-title">Confirmar Baja Definitiva</h2>
          <button type="button" class="modal-close" @click="modalBajaConfirmOpen = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
        <div v-if="selectedItemForBaja" class="modal-body">
          <p class="text-baja-confirm">
            ¿Desea asentar la disposición y baja definitiva del objeto <strong>#{{ selectedItemForBaja.idObjeto }}</strong> ({{ selectedItemForBaja.titulo }})?
          </p>
          <p class="text-baja-sub">
            Esta acción registrará la baja física formal en la bitácora institucional tras superar el periodo reglamentario de custodia.
          </p>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="modalBajaConfirmOpen = false">Cancelar</button>
          <button type="button" class="btn btn-danger" :disabled="guardandoBaja" @click="ejecutarBajaDefinitiva">
            {{ guardandoBaja ? 'Procesando...' : 'Asentar Baja Definitiva' }}
          </button>
        </div>
      </div>
    </div>

    <div class="toast" :class="{ show: toast.show }">
      <svg class="toast-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
        <polyline points="22 4 12 14.01 9 11.01" />
      </svg>
      <span>{{ toast.message }}</span>
    </div>
  </div>
</template>

<style scoped>
.content-area {
  padding: 1.5rem 2.25rem 3rem 2.25rem;
  width: 100%;
  box-sizing: border-box;
  background: #FFFFFF;
  color: #111827;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
  font-size: 13.5px;
}

.header-section {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 1.5rem;
  gap: 1.5rem;
}

.page-title {
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-size: 1.6rem;
  font-weight: 800;
  color: #0F172A;
  letter-spacing: -0.02em;
}

.page-subtitle {
  color: #64748B;
  font-size: 0.9rem;
  margin-top: 4px;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 0.6rem 1.1rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  text-decoration: none;
  font-family: inherit;
}

.btn-primary {
  background: #008744;
  color: #FFFFFF;
  border-color: #008744;
}

.btn-primary:hover:not(:disabled) {
  background: #007A3D;
  border-color: #007A3D;
  box-shadow: 0 4px 12px rgba(0, 135, 68, 0.25);
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: #FFFFFF;
  border-color: #CBD5E1;
  color: #334155;
}

.btn-secondary:hover {
  background: #F1F5F9;
  color: #0F172A;
}

.btn-danger {
  background: #DC2626;
  color: #FFFFFF;
  border-color: #DC2626;
}

.btn-danger:hover:not(:disabled) {
  background: #B91C1C;
  border-color: #B91C1C;
}

.btn-danger:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-sm {
  padding: 0.4rem 0.8rem;
  font-size: 0.8rem;
  border-radius: 6px;
}

.btn-action-devolver {
  background: #008744;
  color: #FFFFFF;
  border: 1px solid #008744;
}

.btn-action-devolver:hover {
  background: #007A3D;
  border-color: #007A3D;
}

.icon-btn-baja {
  background: transparent;
  border: 1px solid #FECDD3;
  color: #E11D48;
  width: 30px;
  height: 30px;
  border-radius: 6px;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  padding: 0;
  flex-shrink: 0;
}

.icon-btn-baja:hover {
  background: #FEE2E2;
  border-color: #FDA4AF;
  color: #BE123C;
}

.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1.25rem;
  margin-bottom: 1.75rem;
}

.kpi-card {
  background: #FFFFFF;
  border: 1px solid #E2E8F0;
  border-radius: 12px;
  padding: 1.25rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  transition: transform 0.2s, box-shadow 0.2s;
}

.kpi-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.06);
}

.kpi-icon {
  width: 46px;
  height: 46px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.kpi-icon.custodia {
  background: #FEF3C7;
  color: #D97706;
}

.kpi-icon.devueltos {
  background: #E8F5E9;
  color: #008744;
}

.kpi-icon.vencido {
  background: #FEE2E2;
  color: #991B1B;
}

.kpi-icon.total {
  background: #F1F5F9;
  color: #475569;
}

.kpi-info {
  display: flex;
  flex-direction: column;
}

.kpi-label {
  font-size: 0.76rem;
  font-weight: 700;
  color: #64748B;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.kpi-value {
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-size: 1.6rem;
  font-weight: 800;
  color: #0F172A;
  line-height: 1.2;
  margin-top: 2px;
}

.kpi-subtext {
  font-size: 0.74rem;
  color: #94A3B8;
  margin-top: 2px;
}

.open-filter-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 1.25rem;
  flex-wrap: wrap;
}

.open-search-wrap {
  position: relative;
  flex: 1;
  max-width: 420px;
}

.open-search-svg {
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  color: #94A3B8;
  pointer-events: none;
}

.open-search-input {
  width: 100%;
  padding: 0.6rem 0.85rem 0.6rem 2.35rem;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  font-size: 0.85rem;
  color: #111827;
  background: #FFFFFF;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
  box-sizing: border-box;
}

.open-search-input:focus {
  border-color: #008744;
  box-shadow: 0 0 0 3px rgba(0, 135, 68, 0.12);
}

.open-filters-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.open-select {
  padding: 0.55rem 0.85rem;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  font-size: 0.84rem;
  color: #334155;
  background: #FFFFFF;
  outline: none;
  cursor: pointer;
  transition: border-color 0.2s;
}

.open-select:focus {
  border-color: #008744;
}

.table-responsive {
  width: 100%;
  overflow-x: auto;
}

.free-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  background: #FFFFFF;
}

.free-table th {
  padding: 1rem 0.75rem;
  color: #6B7280;
  border-bottom: 1px solid #E5E7EB;
  font-weight: 500;
  font-size: 0.85rem;
  background: transparent;
  white-space: nowrap;
}

.free-table td {
  padding: 1.05rem 0.75rem;
  border-bottom: 1px solid #F3F4F6;
  font-size: 0.875rem;
  color: #111827;
  vertical-align: middle;
}

.free-table tr:hover td {
  background: #F9FAFB;
}

.free-table tr:last-child td {
  border-bottom: 1px solid #E5E7EB;
}

.col-num {
  width: 44px;
  text-align: center;
  color: #6B7280;
  font-weight: 600;
}

.item-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.item-icon-box {
  width: 38px;
  height: 38px;
  border-radius: 8px;
  background: #F1F5F9;
  color: #475569;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.item-title {
  font-weight: 600;
  color: #0F172A;
  display: block;
}

.badge-category {
  background: #F1F5F9;
  color: #475569;
  font-size: 0.72rem;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-weight: 500;
  display: inline-block;
  margin-top: 3px;
}

.meta-date {
  font-size: 0.82rem;
  color: #0F172A;
  font-weight: 500;
  display: block;
}

.meta-sub {
  font-size: 0.74rem;
  color: #64748B;
  display: block;
  margin-top: 2px;
}

.receptor-block {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.receptor-name {
  font-weight: 600;
  color: #0F172A;
  font-size: 0.83rem;
}

.receptor-ids {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.74rem;
  color: #475569;
}

.pill-id {
  background: #E8F5E9;
  color: #008744;
  padding: 1px 5px;
  border-radius: 4px;
  font-weight: 600;
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0.3rem 0.65rem;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.02em;
}

.badge-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.badge-custodia {
  background: #FEF3C7;
  color: #92400E;
}

.badge-custodia .badge-dot {
  background: #D97706;
}

.badge-devuelto {
  background: #DCFCE7;
  color: #166534;
}

.badge-devuelto .badge-dot {
  background: #16A34A;
}

.badge-vencido {
  background: #FEE2E2;
  color: #991B1B;
}

.badge-vencido .badge-dot {
  background: #DC2626;
}

.badge-dispuesto {
  background: #F1F5F9;
  color: #475569;
}

.badge-dispuesto .badge-dot {
  background: #64748B;
}

.estado-sub-block {
  margin-top: 4px;
}

.date-cell {
  color: #4B5563;
  font-size: 0.78rem;
}

.conformidad-text {
  color: #166534;
  font-size: 0.72rem;
  font-weight: 500;
  margin-top: 2px;
  display: block;
}

.baja-subtext {
  color: #64748B;
  font-size: 0.72rem;
  font-weight: 500;
  margin-top: 2px;
  display: block;
}

.remaining-text {
  color: #059669;
  font-size: 0.74rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 3px;
  margin-top: 3px;
}

.overdue-text {
  color: #E11D48;
  font-size: 0.74rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 3px;
  margin-top: 3px;
}

.actions-cell {
  display: flex;
  align-items: center;
  gap: 6px;
  justify-content: flex-end;
  white-space: nowrap;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #6B7280;
}

.free-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 0.5rem 0.5rem 0.5rem;
  color: #6B7280;
  font-size: 0.85rem;
}

.text-muted-sm {
  color: #64748B;
  font-size: 0.8rem;
  display: block;
}

.text-muted-xs {
  color: #94A3B8;
  font-size: 0.74rem;
  display: block;
}

.text-danger-sm {
  color: #991B1B;
  font-size: 0.8rem;
  font-weight: 500;
  display: block;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s ease;
}

.modal-overlay.active {
  opacity: 1;
  pointer-events: auto;
}

.modal-high {
  z-index: 1050;
}

.modal-card {
  background: #FFFFFF;
  border-radius: 14px;
  max-width: 620px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  display: flex;
  flex-direction: column;
}

.modal-card-sm {
  max-width: 500px;
}

.modal-card-lg {
  max-width: 650px;
}

.modal-card-acta {
  max-width: 680px;
}

.modal-header {
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #E2E8F0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #FFFFFF;
  position: sticky;
  top: 0;
  z-index: 10;
}

.modal-title {
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-size: 1.15rem;
  font-weight: 700;
  color: #0F172A;
}

.modal-close {
  background: transparent;
  border: none;
  color: #64748B;
  cursor: pointer;
  width: 32px;
  height: 32px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}

.modal-close:hover {
  background: #F1F5F9;
  color: #0F172A;
}

.modal-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}

.item-banner {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  padding: 0.85rem 1rem;
  display: flex;
  align-items: center;
  gap: 12px;
}

.item-banner-info {
  flex: 1;
}

.item-banner-title {
  font-weight: 700;
  color: #0F172A;
  font-size: 0.92rem;
}

.item-banner-desc {
  font-size: 0.78rem;
  color: #64748B;
  margin-top: 2px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-group-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: #334155;
}

.form-label .req {
  color: #DC2626;
  margin-left: 2px;
}

.input-verify-group {
  display: flex;
  gap: 8px;
  align-items: center;
}

.input-verify-group .form-control {
  flex: 1;
}

.form-control {
  padding: 0.6rem 0.85rem;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  font-size: 0.875rem;
  color: #0F172A;
  background: #FFFFFF;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
  font-family: inherit;
  box-sizing: border-box;
}

.form-control:focus {
  border-color: #008744;
  box-shadow: 0 0 0 3px rgba(0, 135, 68, 0.12);
}

textarea.form-control {
  resize: vertical;
  min-height: 70px;
}

.status-padron-box {
  margin-top: 4px;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.status-padron-success {
  background: #E8F5E9;
  color: #008744;
  border: 1px solid #A7F3D0;
}

.status-padron-warning {
  background: #FEF3C7;
  color: #92400E;
  border: 1px solid #FCD34D;
}

.status-padron-content {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.status-padron-title {
  font-weight: 700;
  color: #166534;
  font-size: 0.88rem;
}

.status-padron-meta {
  font-size: 0.78rem;
  color: #15803D;
}

.badge-validado {
  font-size: 0.72rem;
  color: #008744;
  font-weight: 700;
  background: #DCFCE7;
  padding: 3px 8px;
  border-radius: 4px;
  border: 1px solid #86EFAC;
}

.status-warning-content {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
}

.modal-footer {
  padding: 1rem 1.5rem;
  border-top: 1px solid #E2E8F0;
  background: #F8FAFC;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.modal-title-wrapper {
  display: flex;
  align-items: center;
  gap: 12px;
}

.modal-icon-badge {
  background: #E0E7FF;
  color: #4F46E5;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.modal-title-custom {
  margin: 0;
  color: #0F172A;
  font-size: 1.2rem;
  font-weight: 800;
  font-family: 'Plus Jakarta Sans', sans-serif;
}

.modal-subtitle-custom {
  font-size: 0.78rem;
  color: #64748B;
}

.close-btn {
  background: transparent;
  border: none;
  color: #94A3B8;
  cursor: pointer;
  padding: 6px;
  border-radius: 6px;
  transition: all 0.15s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-btn:hover {
  background: #E2E8F0;
  color: #0F172A;
}

.modal-scroll-body {
  padding: 1.5rem 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.15rem;
  max-height: 75vh;
  overflow-y: auto;
}

.alert-error-box {
  background: #FEF2F2;
  border: 1px solid #FECACA;
  color: #991B1B;
  padding: 10px 14px;
  border-radius: 8px;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
}

.form-field-vertical {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.label-premium {
  font-size: 0.75rem;
  font-weight: 700;
  color: #64748B;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.premium-input {
  padding: 10px 14px;
  background: #F8FAFC;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  color: #0F172A;
  font-size: 0.88rem;
  font-family: inherit;
  transition: all 0.2s;
  width: 100%;
  box-sizing: border-box;
  outline: none;
}

.premium-input:focus {
  border-color: #008744;
  background: #FFFFFF;
  box-shadow: 0 0 0 3px rgba(0, 135, 68, 0.12);
}

.select-pointer {
  cursor: pointer;
}

.modal-footer-custom {
  padding: 1.25rem 2rem;
  border-top: 1px solid #F1F5F9;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  background: #F8FAFC;
}

.header-modal-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.acta-sheet {
  border: 2px dashed #CBD5E1;
  border-radius: 10px;
  padding: 1.5rem;
  background: #FFFFFF;
}

.acta-header {
  text-align: center;
  border-bottom: 2px solid #008744;
  padding-bottom: 1rem;
  margin-bottom: 1.25rem;
}

.acta-facultad {
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-size: 0.82rem;
  font-weight: 700;
  color: #008744;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.acta-subfacultad {
  font-weight: 600;
  color: #334155;
  font-size: 0.9rem;
  margin-top: 2px;
}

.acta-folio {
  font-size: 0.78rem;
  color: #64748B;
  margin-top: 4px;
}

.acta-doc-title {
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-size: 1.15rem;
  font-weight: 800;
  color: #0F172A;
  margin-top: 6px;
}

.acta-section-title {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #008744;
  margin-bottom: 8px;
}

.acta-sec-gap {
  margin-top: 1.25rem;
}

.acta-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px 16px;
  font-size: 0.82rem;
  margin-bottom: 1.25rem;
}

.acta-row-full {
  grid-column: span 2;
}

.acta-key {
  color: #64748B;
  font-weight: 500;
}

.acta-val {
  color: #0F172A;
  font-weight: 600;
  margin-left: 6px;
}

.acta-code-highlight {
  color: #008744;
  font-weight: 700;
}

.acta-banner-baja {
  background: #F1F5F9;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  padding: 0.85rem;
  margin-top: 1rem;
  font-size: 0.82rem;
  color: #334155;
}

.acta-banner-custodia {
  background: #FEF3C7;
  border: 1px solid #FCD34D;
  border-radius: 6px;
  padding: 0.85rem;
  margin-top: 1rem;
  font-size: 0.82rem;
  color: #92400E;
}

.text-baja-confirm {
  font-size: 0.92rem;
  color: #0F172A;
  line-height: 1.5;
}

.text-baja-sub {
  font-size: 0.82rem;
  color: #64748B;
  line-height: 1.4;
}

.toast {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: #0F172A;
  color: #FFFFFF;
  padding: 0.85rem 1.25rem;
  border-radius: 8px;
  font-size: 0.86rem;
  font-weight: 500;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.2);
  display: flex;
  align-items: center;
  gap: 10px;
  z-index: 2000;
  transform: translateY(100px);
  opacity: 0;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}

.toast.show {
  transform: translateY(0);
  opacity: 1;
}

.toast-icon {
  color: #00A859;
}

@media print {
  body * {
    visibility: hidden;
  }
  .acta-sheet, .acta-sheet * {
    visibility: visible;
  }
  .acta-sheet {
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    border: none;
  }
}

@media (max-width: 1024px) {
  .kpi-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .form-group-row,
  .form-grid-2 {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .header-section {
    flex-direction: column;
    align-items: stretch;
  }
  .header-actions {
    justify-content: flex-start;
  }
  .kpi-grid {
    grid-template-columns: 1fr;
  }
  .acta-grid {
    grid-template-columns: 1fr;
  }
  .acta-row-full {
    grid-column: span 1;
  }
}
</style>
