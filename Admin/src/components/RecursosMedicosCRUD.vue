<script setup>
import { ref, computed, onMounted } from 'vue'
import axios, { API_BASE_URL } from '../config'

const resources = ref([])
const allCursos = ref([])
const stats = ref({
  totalRecursos: 0,
  totalSuscripcion: 0,
  totalDestacados: 0,
  totalCursosVinculados: 0
})
const loading = ref(false)
const searchQuery = ref('')
const filterLicencia = ref('todas')
const filterEstado = ref('todos')

const currentPage = ref(1)
const pageSize = ref(10)

const toast = ref({ show: false, message: '', type: 'success' })
let toastTimer = null

const showToast = (message, type = 'success') => {
  if (toastTimer) clearTimeout(toastTimer)
  toast.value = { show: true, message, type }
  toastTimer = setTimeout(() => {
    toast.value.show = false
  }, 3500)
}

const getImageUrl = (path) => {
  if (!path) return ''
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('data:')) {
    return path
  }
  return `${API_BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`
}

const extractYoutubeId = (urlOrId) => {
  if (!urlOrId) return ''
  const trimmed = urlOrId.trim()
  const match = trimmed.match(/(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/))([\w-]{11})/i)
  return match ? match[1] : trimmed
}

const loadStats = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/resources/stats`)
    if (res?.data) {
      stats.value = res.data
    }
  } catch {
  }
}

const loadCursos = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/resources/cursos`)
    allCursos.value = Array.isArray(res.data) ? res.data : []
  } catch {
  }
}

const loadResources = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/resources`)
    resources.value = Array.isArray(res.data) ? res.data : []
  } catch {
    showToast('Error al conectar con el servidor para obtener las bases de datos.', 'error')
  } finally {
    loading.value = false
  }
}

const refreshData = async () => {
  await Promise.all([loadStats(), loadResources(), loadCursos()])
}

onMounted(() => {
  refreshData()
})

const filteredResources = computed(() => {
  let list = [...resources.value]

  if (searchQuery.value.trim()) {
    const term = searchQuery.value.trim().toLowerCase()
    list = list.filter(r =>
      r.nombreRecurso?.toLowerCase().includes(term) ||
      r.descripcionClinica?.toLowerCase().includes(term) ||
      r.cursos?.some(c => c.nombreCurso?.toLowerCase().includes(term))
    )
  }

  if (filterLicencia.value === 'suscripcion') {
    list = list.filter(r => r.esSuscripcion)
  } else if (filterLicencia.value === 'abierto') {
    list = list.filter(r => !r.esSuscripcion)
  }

  if (filterEstado.value === 'activo') {
    list = list.filter(r => r.estadoActivo)
  } else if (filterEstado.value === 'inactivo') {
    list = list.filter(r => !r.estadoActivo)
  }

  return list
})

const totalPages = computed(() => {
  return Math.max(1, Math.ceil(filteredResources.value.length / pageSize.value))
})

const paginatedResources = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return filteredResources.value.slice(start, start + pageSize.value)
})

const formatAllCursosTooltip = (cursos) => {
  if (!cursos || cursos.length === 0) return 'Sin asignaturas vinculadas'
  return cursos.map(c => `Ciclo ${c.ciclo}: ${c.nombreCurso}`).join('\n')
}

const handleToggleActive = async (resource) => {
  try {
    const res = await axios.patch(`${API_BASE_URL}/api/admin/resources/${resource.idBaseDatos}/toggle-activo`)
    resource.estadoActivo = res.data.estadoActivo
    resource.mostrarEnHexagonos = res.data.mostrarEnHexagonos
    await loadStats()
    showToast(
      resource.estadoActivo ? 'Base de datos activada en el catálogo.' : 'Base de datos desactivada del catálogo.',
      'success'
    )
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al cambiar estado activo.'
    showToast(msg, 'error')
  }
}

const handleToggleMobile = async (resource) => {
  try {
    const res = await axios.patch(`${API_BASE_URL}/api/admin/resources/${resource.idBaseDatos}/toggle-mobile`)
    resource.tieneAppMovil = res.data.tieneAppMovil
    showToast(
      resource.tieneAppMovil ? 'Compatibilidad con App Móvil habilitada.' : 'Compatibilidad con App Móvil deshabilitada.',
      'success'
    )
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al actualizar app móvil.'
    showToast(msg, 'error')
  }
}

const showSwapModal = ref(false)
const incomingResource = ref(null)
const selectedReplacementId = ref(null)
const swapping = ref(false)

const currentHexagonResources = computed(() => {
  return resources.value.filter(r => r.mostrarEnHexagonos && r.estadoActivo)
})

const handleToggleDestacado = async (resource) => {
  if (resource.mostrarEnHexagonos) {
    try {
      const res = await axios.patch(`${API_BASE_URL}/api/admin/resources/${resource.idBaseDatos}/toggle-destacado`)
      resource.mostrarEnHexagonos = res.data.mostrarEnHexagonos
      await loadStats()
      showToast('Base de datos retirada de la matriz hexagonal.', 'success')
    } catch (err) {
      const msg = err.response?.data?.message || 'Error al retirar de la matriz hexagonal.'
      showToast(msg, 'error')
    }
    return
  }

  if (!resource.estadoActivo) {
    showToast('No puedes destacar un recurso inactivo. Actívalo primero.', 'error')
    return
  }

  const activeFeaturedCount = resources.value.filter(r => r.mostrarEnHexagonos && r.estadoActivo).length
  if (activeFeaturedCount >= 15) {
    incomingResource.value = resource
    selectedReplacementId.value = currentHexagonResources.value[0]?.idBaseDatos || null
    showSwapModal.value = true
    return
  }

  try {
    const res = await axios.patch(`${API_BASE_URL}/api/admin/resources/${resource.idBaseDatos}/toggle-destacado`)
    resource.mostrarEnHexagonos = res.data.mostrarEnHexagonos
    await loadStats()
    showToast('Base de datos agregada a la matriz hexagonal.', 'success')
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al destacar en la matriz hexagonal.'
    showToast(msg, 'error')
  }
}

const handleConfirmSwap = async () => {
  if (!incomingResource.value || !selectedReplacementId.value) return
  swapping.value = true
  try {
    await axios.patch(`${API_BASE_URL}/api/admin/resources/${incomingResource.value.idBaseDatos}/swap-destacado`, {
      idReemplazo: Number(selectedReplacementId.value)
    })
    showToast('Intercambio en la matriz hexagonal realizado exitosamente.', 'success')
    showSwapModal.value = false
    incomingResource.value = null
    selectedReplacementId.value = null
    await refreshData()
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al reemplazar recurso en la matriz hexagonal.'
    showToast(msg, 'error')
  } finally {
    swapping.value = false
  }
}

const showVideoModal = ref(false)
const activeVideoId = ref('')
const activeVideoTitle = ref('')

const handleOpenVideo = (resource) => {
  if (!resource.youtubeVideoId) return
  activeVideoId.value = extractYoutubeId(resource.youtubeVideoId)
  activeVideoTitle.value = resource.nombreRecurso
  showVideoModal.value = true
}

const handleCloseVideo = () => {
  showVideoModal.value = false
  activeVideoId.value = ''
  activeVideoTitle.value = ''
}

const showDeleteModal = ref(false)
const resourceToDelete = ref(null)
const eliminando = ref(false)

const handleConfirmDelete = (resource) => {
  resourceToDelete.value = resource
  showDeleteModal.value = true
}

const handleCancelDelete = () => {
  if (eliminando.value) return
  showDeleteModal.value = false
  resourceToDelete.value = null
}

const handleExecuteDelete = async () => {
  if (!resourceToDelete.value) return
  eliminando.value = true
  try {
    await axios.delete(`${API_BASE_URL}/api/admin/resources/${resourceToDelete.value.idBaseDatos}`)
    showToast('Base de datos eliminada correctamente.', 'success')
    showDeleteModal.value = false
    resourceToDelete.value = null
    await refreshData()
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al eliminar la base de datos.'
    showToast(msg, 'error')
  } finally {
    eliminando.value = false
  }
}

const showFormModal = ref(false)
const isEditing = ref(false)
const editingId = ref(null)
const guardando = ref(false)

const form = ref({
  nombreRecurso: '',
  descripcionClinica: '',
  esSuscripcion: true,
  tieneAppMovil: false,
  urlExterno: '',
  mostrarEnHexagonos: false,
  youtubeVideoId: '',
  selectedCursos: []
})

const formErrors = ref({
  nombreRecurso: '',
  descripcionClinica: '',
  image: ''
})

const fileInputRef = ref(null)
const selectedFile = ref(null)
const imagePreviewUrl = ref('')
const selectedCiclo = ref(1)

const cursosDelCiclo = computed(() => {
  return allCursos.value.filter(c => c.ciclo === Number(selectedCiclo.value))
})

const selectedCursosDetalle = computed(() => {
  const ids = new Set(form.value.selectedCursos)
  return allCursos.value.filter(c => ids.has(c.idCurso))
})

const resetForm = () => {
  form.value = {
    nombreRecurso: '',
    descripcionClinica: '',
    esSuscripcion: true,
    tieneAppMovil: false,
    urlExterno: '',
    mostrarEnHexagonos: false,
    youtubeVideoId: '',
    selectedCursos: []
  }
  selectedFile.value = null
  if (imagePreviewUrl.value) {
    URL.revokeObjectURL(imagePreviewUrl.value)
    imagePreviewUrl.value = ''
  }
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
  formErrors.value = {
    nombreRecurso: '',
    descripcionClinica: '',
    image: ''
  }
  selectedCiclo.value = 1
  isEditing.value = false
  editingId.value = null
}

const handleOpenCreateModal = () => {
  resetForm()
  showFormModal.value = true
}

const handleOpenEditModal = (resource) => {
  resetForm()
  isEditing.value = true
  editingId.value = resource.idBaseDatos
  form.value = {
    nombreRecurso: resource.nombreRecurso || '',
    descripcionClinica: resource.descripcionClinica || '',
    esSuscripcion: Boolean(resource.esSuscripcion),
    tieneAppMovil: Boolean(resource.tieneAppMovil),
    urlExterno: resource.urlExterno || '',
    mostrarEnHexagonos: Boolean(resource.mostrarEnHexagonos),
    youtubeVideoId: resource.youtubeVideoId || '',
    selectedCursos: resource.cursos ? resource.cursos.map(c => c.idCurso) : []
  }
  if (resource.logotipoUrl) {
    imagePreviewUrl.value = getImageUrl(resource.logotipoUrl)
  }
  showFormModal.value = true
}

const handleCloseFormModal = () => {
  if (guardando.value) return
  showFormModal.value = false
  resetForm()
}

const handleFileSelect = (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  processSelectedFile(file)
}

const processSelectedFile = (file) => {
  formErrors.value.image = ''
  const maxBytes = 5 * 1024 * 1024
  if (file.size > maxBytes) {
    formErrors.value.image = 'La imagen excede el límite máximo de 5 MB.'
    return
  }

  const validExtensions = ['.webp', '.jpg', '.jpeg', '.png']
  const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase()
  if (!validExtensions.includes(ext)) {
    formErrors.value.image = 'Solo se permiten extensiones .webp, .jpg, .jpeg y .png.'
    return
  }

  selectedFile.value = file
  if (imagePreviewUrl.value && imagePreviewUrl.value.startsWith('blob:')) {
    URL.revokeObjectURL(imagePreviewUrl.value)
  }
  imagePreviewUrl.value = URL.createObjectURL(file)
}

const handleDropFile = (event) => {
  const file = event.dataTransfer?.files?.[0]
  if (file) {
    processSelectedFile(file)
  }
}

const toggleCursoSelection = (idCurso) => {
  const idx = form.value.selectedCursos.indexOf(idCurso)
  if (idx >= 0) {
    form.value.selectedCursos.splice(idx, 1)
  } else {
    form.value.selectedCursos.push(idCurso)
  }
}

const removeCursoSelection = (idCurso) => {
  const idx = form.value.selectedCursos.indexOf(idCurso)
  if (idx >= 0) {
    form.value.selectedCursos.splice(idx, 1)
  }
}

const handleSubmitForm = async () => {
  formErrors.value = {
    nombreRecurso: '',
    descripcionClinica: '',
    image: ''
  }

  let hasErrors = false

  if (!form.value.nombreRecurso.trim()) {
    formErrors.value.nombreRecurso = 'El nombre de la base de datos es obligatorio.'
    hasErrors = true
  }

  if (!form.value.descripcionClinica.trim()) {
    formErrors.value.descripcionClinica = 'La descripción clínica es obligatoria.'
    hasErrors = true
  }

  if (!isEditing.value && !selectedFile.value) {
    formErrors.value.image = 'Debes subir un archivo de logotipo para la base de datos.'
    hasErrors = true
  }

  if (hasErrors) return

  guardando.value = true
  try {
    const formData = new FormData()
    formData.append('NombreRecurso', form.value.nombreRecurso.trim())
    formData.append('DescripcionClinica', form.value.descripcionClinica.trim())
    formData.append('EsSuscripcion', String(form.value.esSuscripcion))
    formData.append('TieneAppMovil', String(form.value.tieneAppMovil))
    formData.append('MostrarEnHexagonos', String(form.value.mostrarEnHexagonos))

    if (form.value.urlExterno?.trim()) {
      formData.append('UrlExterno', form.value.urlExterno.trim())
    }

    if (form.value.youtubeVideoId?.trim()) {
      formData.append('YoutubeVideoId', form.value.youtubeVideoId.trim())
    }

    if (selectedFile.value) {
      formData.append('Image', selectedFile.value)
    }

    formData.append('CursoIdsRaw', form.value.selectedCursos.join(','))

    if (isEditing.value) {
      await axios.put(`${API_BASE_URL}/api/admin/resources/${editingId.value}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      showToast('Base de datos actualizada correctamente.', 'success')
    } else {
      await axios.post(`${API_BASE_URL}/api/admin/resources`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      showToast('Base de datos registrada exitosamente.', 'success')
    }

    showFormModal.value = false
    resetForm()
    await refreshData()
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al guardar la base de datos.'
    showToast(msg, 'error')
  } finally {
    guardando.value = false
  }
}
</script>

<template>
  <div class="resources-view">
    <div class="header-section">
      <div class="header-left">
        <h2 class="header-title">Directorio de Bases de Datos Médicas</h2>
        <p class="header-subtitle">
          Gestión del catálogo científico biomédico, vinculación curricular por ciclos y matriz hexagonal de la FAMURP.
        </p>
      </div>

      <div class="header-right">
        <button
          type="button"
          class="btn-primary-add"
          @click="handleOpenCreateModal"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          <span>+ Nueva Base de Datos</span>
        </button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <div class="stat-icon-wrap stat-icon-green">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <ellipse cx="12" cy="5" rx="9" ry="3" />
            <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
            <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
          </svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Bases Registradas</span>
          <span class="stat-value">{{ stats.totalRecursos }}</span>
        </div>
      </article>

      <article class="stat-card">
        <div class="stat-icon-wrap stat-icon-blue">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Suscripción URP</span>
          <span class="stat-value">{{ stats.totalSuscripcion }}</span>
        </div>
      </article>

      <article class="stat-card">
        <div class="stat-icon-wrap stat-icon-amber">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
          </svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Matriz Hexagonal</span>
          <span class="stat-value">{{ stats.totalDestacados }} / 15</span>
        </div>
      </article>

      <article class="stat-card">
        <div class="stat-icon-wrap stat-icon-violet">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z" />
            <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z" />
          </svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Cursos Vinculados</span>
          <span class="stat-value">{{ stats.totalCursosVinculados }}</span>
        </div>
      </article>
    </div>

    <div class="toolbar-section">
      <div class="search-wrap">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="2">
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          class="search-input"
          placeholder="Buscar por nombre, descripción o asignatura..."
        />
        <button
          v-if="searchQuery"
          type="button"
          class="search-clear-btn"
          @click="searchQuery = ''"
        >
          &times;
        </button>
      </div>

      <div class="filters-row">
        <select v-model="filterLicencia" class="select-filter">
          <option value="todas">Todas las Licencias</option>
          <option value="suscripcion">Suscripción URP</option>
          <option value="abierto">Acceso Abierto</option>
        </select>

        <select v-model="filterEstado" class="select-filter">
          <option value="todos">Todos los Estados</option>
          <option value="activo">Solo Activas</option>
          <option value="inactivo">Solo Inactivas</option>
        </select>
      </div>
    </div>

    <div class="table-container">
      <div v-if="loading" class="state-loading">
        <div class="spinner"></div>
        <span>Cargando bases de datos médicas...</span>
      </div>

      <div v-else-if="filteredResources.length === 0" class="state-empty">
        <div class="empty-icon-wrap">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#94A3B8" stroke-width="1.8">
            <ellipse cx="12" cy="5" rx="9" ry="3" />
            <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
            <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
          </svg>
        </div>
        <h3 class="empty-title">No se encontraron bases de datos</h3>
        <p class="empty-desc">
          Intenta ajustar los términos de búsqueda o registra una nueva base médica en el sistema.
        </p>
      </div>

      <div v-else class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th class="th-num">Nº</th>
              <th class="th-resource">Recurso Biomédico</th>
              <th class="th-courses">Asignaturas Clave</th>
              <th class="th-license">Licencia</th>
              <th class="th-center">App Móvil</th>
              <th class="th-center">Video Inducción</th>
              <th class="th-center">Matriz Hexagonal</th>
              <th class="th-center">Estado</th>
              <th class="th-actions">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(res, index) in paginatedResources"
              :key="res.idBaseDatos"
              :class="{ 'row-inactive': !res.estadoActivo }"
            >
              <td class="td-num">
                {{ (currentPage - 1) * pageSize + index + 1 }}
              </td>

              <td class="td-resource">
                <div class="resource-cell">
                  <div class="logo-box">
                    <img
                      :src="getImageUrl(res.logotipoUrl)"
                      :alt="res.nombreRecurso"
                      class="resource-logo-img"
                    />
                  </div>
                  <div class="resource-info">
                    <span class="resource-name">{{ res.nombreRecurso }}</span>
                    <span class="resource-desc" :title="res.descripcionClinica">
                      {{ res.descripcionClinica }}
                    </span>
                  </div>
                </div>
              </td>

              <td class="td-courses">
                <div v-if="res.cursos && res.cursos.length > 0" class="chips-container">
                  <span
                    v-for="c in res.cursos.slice(0, 2)"
                    :key="c.idCurso"
                    class="course-chip"
                  >
                    C{{ c.ciclo }}: {{ c.nombreCurso }}
                  </span>

                  <span
                    v-if="res.cursos.length > 2"
                    class="course-chip-more"
                    :title="formatAllCursosTooltip(res.cursos)"
                  >
                    +{{ res.cursos.length - 2 }} más
                  </span>
                </div>
                <span v-else class="text-muted">Sin cursos</span>
              </td>

              <td class="td-license">
                <span
                  :class="['badge-license', res.esSuscripcion ? 'badge-subscription' : 'badge-open-access']"
                >
                  {{ res.esSuscripcion ? 'Suscripción URP' : 'Acceso Abierto' }}
                </span>
              </td>

              <td class="td-center">
                <label class="switch">
                  <input
                    type="checkbox"
                    :checked="res.tieneAppMovil"
                    @change="handleToggleMobile(res)"
                  />
                  <span class="slider"></span>
                </label>
              </td>

              <td class="td-center">
                <button
                  v-if="res.youtubeVideoId"
                  type="button"
                  class="btn-video-play"
                  title="Ver video de inducción oficial"
                  @click="handleOpenVideo(res)"
                >
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor">
                    <polygon points="5 3 19 12 5 21 5 3" />
                  </svg>
                  <span>Video</span>
                </button>
                <span v-else class="text-muted">—</span>
              </td>

              <td class="td-center">
                <button
                  type="button"
                  :class="['star-btn', res.mostrarEnHexagonos ? 'star-active' : 'star-inactive']"
                  :title="res.mostrarEnHexagonos ? 'Destacada en portada (Haz clic para retirar)' : 'No destacada (Haz clic para incorporar)'"
                  @click="handleToggleDestacado(res)"
                >
                  <svg width="18" height="18" viewBox="0 0 24 24" :fill="res.mostrarEnHexagonos ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="2">
                    <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
                  </svg>
                </button>
              </td>

              <td class="td-center">
                <label class="switch">
                  <input
                    type="checkbox"
                    :checked="res.estadoActivo"
                    @change="handleToggleActive(res)"
                  />
                  <span class="slider"></span>
                </label>
              </td>

              <td class="td-actions">
                <div class="actions-group">
                  <button
                    type="button"
                    class="icon-btn edit-btn"
                    title="Editar base de datos"
                    @click="handleOpenEditModal(res)"
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                      <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                    </svg>
                  </button>

                  <button
                    type="button"
                    class="icon-btn delete-btn"
                    title="Eliminar base de datos"
                    @click="handleConfirmDelete(res)"
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="3 6 5 6 21 6" />
                      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                      <line x1="10" y1="11" x2="10" y2="17" />
                      <line x1="14" y1="11" x2="14" y2="17" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="filteredResources.length > 0" class="pagination-bar">
        <span class="pagination-info">
          Mostrando {{ (currentPage - 1) * pageSize + 1 }} - {{ Math.min(currentPage * pageSize, filteredResources.length) }} de {{ filteredResources.length }} registros
        </span>

        <div class="pagination-controls">
          <button
            type="button"
            class="pagination-btn"
            :disabled="currentPage <= 1"
            @click="currentPage--"
          >
            Anterior
          </button>
          <span class="pagination-current">Pág. {{ currentPage }} de {{ totalPages }}</span>
          <button
            type="button"
            class="pagination-btn"
            :disabled="currentPage >= totalPages"
            @click="currentPage++"
          >
            Siguiente
          </button>
        </div>
      </div>
    </div>

    <div :class="['modal-overlay', { active: showFormModal }]">
      <div class="modal-card modal-card-lg">
        <div class="modal-header">
          <div>
            <h3 class="modal-title">{{ isEditing ? 'Editar Base de Datos Médica' : 'Nueva Base de Datos Médica' }}</h3>
            <p class="modal-subtitle">Configura la ficha técnica, licenciamiento y vinculación con la malla curricular.</p>
          </div>
          <button type="button" class="modal-close-btn" @click="handleCloseFormModal">
            &times;
          </button>
        </div>

        <form class="modal-body" @submit.prevent="handleSubmitForm">
          <div class="form-grid-top">
            <div class="form-group dropzone-group">
              <label class="form-label">
                Logotipo Oficial
                <span v-if="!isEditing" class="required-star">*</span>
              </label>

              <div
                class="dropzone-area"
                @click="fileInputRef?.click()"
                @dragover.prevent
                @drop.prevent="handleDropFile"
              >
                <input
                  ref="fileInputRef"
                  type="file"
                  accept=".webp,.jpg,.jpeg,.png"
                  class="hidden-file-input"
                  @change="handleFileSelect"
                />

                <div v-if="imagePreviewUrl" class="dropzone-preview">
                  <img :src="imagePreviewUrl" alt="Preview" class="preview-img" />
                  <span class="dropzone-change-text">Haz clic para cambiar imagen</span>
                </div>

                <div v-else class="dropzone-prompt">
                  <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#64748B" stroke-width="1.8">
                    <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                    <circle cx="8.5" cy="8.5" r="1.5" />
                    <polyline points="21 15 16 10 5 21" />
                  </svg>
                  <span class="dropzone-title">Arrastra el logo o haz clic para subir</span>
                  <span class="dropzone-hint">Formatos: .webp, .jpg, .png (Máx. 5 MB)</span>
                </div>
              </div>
              <span v-if="formErrors.image" class="error-msg">{{ formErrors.image }}</span>
            </div>

            <div class="form-fields-column">
              <div class="form-group">
                <label class="form-label">
                  Nombre del Recurso
                  <span class="required-star">*</span>
                </label>
                <input
                  v-model="form.nombreRecurso"
                  type="text"
                  class="form-input"
                  placeholder="Ej: UpToDate, ClinicalKey, PubMed..."
                  maxlength="150"
                />
                <span v-if="formErrors.nombreRecurso" class="error-msg">{{ formErrors.nombreRecurso }}</span>
              </div>

              <div class="form-group">
                <label class="form-label">
                  Descripción Clínica y Utilidad Académica
                  <span class="required-star">*</span>
                  <span class="char-counter">({{ form.descripcionClinica.length }}/1000)</span>
                </label>
                <textarea
                  v-model="form.descripcionClinica"
                  rows="3"
                  class="form-textarea"
                  placeholder="Describe el contenido, alcance clínico y valor para el aprendizaje del estudiante..."
                  maxlength="1000"
                ></textarea>
                <span v-if="formErrors.descripcionClinica" class="error-msg">{{ formErrors.descripcionClinica }}</span>
              </div>

              <div class="switches-row">
                <label class="switch-field">
                  <span class="switch-field-label">Suscripción URP</span>
                  <label class="switch">
                    <input v-model="form.esSuscripcion" type="checkbox" />
                    <span class="slider"></span>
                  </label>
                </label>

                <label class="switch-field">
                  <span class="switch-field-label">App Móvil</span>
                  <label class="switch">
                    <input v-model="form.tieneAppMovil" type="checkbox" />
                    <span class="slider"></span>
                  </label>
                </label>

                <label class="switch-field">
                  <span class="switch-field-label">Matriz Hexagonal</span>
                  <label class="switch">
                    <input v-model="form.mostrarEnHexagonos" type="checkbox" />
                    <span class="slider"></span>
                  </label>
                </label>
              </div>

              <div class="form-grid-2col">
                <div class="form-group">
                  <label class="form-label">Enlace Directo Externo</label>
                  <input
                    v-model="form.urlExterno"
                    type="url"
                    class="form-input"
                    placeholder="https://..."
                    maxlength="500"
                  />
                </div>

                <div class="form-group">
                  <label class="form-label">Video de Inducción (YouTube)</label>
                  <input
                    v-model="form.youtubeVideoId"
                    type="text"
                    class="form-input"
                    placeholder="Enlace o ID de YouTube (Ej: dQw4w9WgXcQ)"
                    maxlength="150"
                  />
                </div>
              </div>
            </div>
          </div>

          <div class="curricular-section">
            <div class="curricular-header">
              <span class="curricular-title">Malla Curricular: Asignaturas Vinculadas</span>
              <span class="curricular-desc">Selecciona las materias oficiales de medicina humana recomendadas para este recurso.</span>
            </div>

            <div class="curricular-workspace">
              <div class="curricular-left-panel">
                <div class="ciclo-selector-bar">
                  <label class="ciclo-label">Ciclo Académico:</label>
                  <select v-model="selectedCiclo" class="ciclo-select">
                    <option v-for="ciclo in 14" :key="ciclo" :value="ciclo">
                      Ciclo {{ ciclo }}
                    </option>
                  </select>
                </div>

                <div class="cursos-checkbox-list">
                  <label
                    v-for="curso in cursosDelCiclo"
                    :key="curso.idCurso"
                    class="curso-checkbox-item"
                  >
                    <input
                      type="checkbox"
                      :checked="form.selectedCursos.includes(curso.idCurso)"
                      @change="toggleCursoSelection(curso.idCurso)"
                    />
                    <span class="curso-checkbox-name">
                      {{ curso.nombreCurso }}
                      <small v-if="curso.codigoCurso" class="curso-code">({{ curso.codigoCurso }})</small>
                    </span>
                  </label>
                </div>
              </div>

              <div class="curricular-right-panel">
                <div class="selected-header">
                  <span class="selected-title">Asignaturas Seleccionadas</span>
                  <span class="selected-count">({{ form.selectedCursos.length }})</span>
                </div>

                <div class="selected-chips-box">
                  <div v-if="selectedCursosDetalle.length === 0" class="no-selection-hint">
                    No has seleccionado ninguna asignatura. Utiliza el selector de la izquierda para vincular materias.
                  </div>
                  <div v-else class="selected-chips-grid">
                    <span
                      v-for="curso in selectedCursosDetalle"
                      :key="curso.idCurso"
                      class="selected-chip-badge"
                    >
                      <span class="chip-ciclo">C{{ curso.ciclo }}</span>
                      <span class="chip-text">{{ curso.nombreCurso }}</span>
                      <button
                        type="button"
                        class="chip-remove-btn"
                        title="Quitar asignatura"
                        @click="removeCursoSelection(curso.idCurso)"
                      >
                        &times;
                      </button>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button
              type="button"
              class="btn-secondary"
              :disabled="guardando"
              @click="handleCloseFormModal"
            >
              Cancelar
            </button>
            <button
              type="submit"
              class="btn-primary"
              :disabled="guardando"
            >
              <div v-if="guardando" class="spinner-sm"></div>
              <span>{{ guardando ? 'Guardando...' : (isEditing ? 'Guardar Cambios' : 'Registrar Base de Datos') }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <div :class="['modal-overlay', { active: showSwapModal }]">
      <div class="modal-card modal-card-sm">
        <div class="modal-header">
          <div class="header-icon-title">
            <div class="icon-swap-warning">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#D97706" stroke-width="2">
                <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
              </svg>
            </div>
            <div>
              <h3 class="modal-title">Reemplazo en Matriz Hexagonal</h3>
              <p class="modal-subtitle">Capacidad máxima alcanzada (15 / 15)</p>
            </div>
          </div>
          <button type="button" class="modal-close-btn" @click="showSwapModal = false">
            &times;
          </button>
        </div>

        <div class="modal-body">
          <p class="swap-instruction-text">
            La matriz hexagonal cuenta con sus 15 cupos ocupados. Selecciona cuál de las 15 bases actuales deseas reemplazar por
            <strong>{{ incomingResource?.nombreRecurso }}</strong>:
          </p>

          <div class="form-group">
            <label class="form-label">Base de datos a reemplazar:</label>
            <select v-model="selectedReplacementId" class="form-select">
              <option
                v-for="r in currentHexagonResources"
                :key="r.idBaseDatos"
                :value="r.idBaseDatos"
              >
                {{ r.nombreRecurso }} ({{ r.esSuscripcion ? 'Suscripción' : 'Acceso Abierto' }})
              </option>
            </select>
          </div>
        </div>

        <div class="modal-footer">
          <button
            type="button"
            class="btn-secondary"
            :disabled="swapping"
            @click="showSwapModal = false"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="btn-primary"
            :disabled="swapping || !selectedReplacementId"
            @click="handleConfirmSwap"
          >
            <div v-if="swapping" class="spinner-sm"></div>
            <span>{{ swapping ? 'Reemplazando...' : 'Confirmar Reemplazo' }}</span>
          </button>
        </div>
      </div>
    </div>

    <div :class="['modal-overlay modal-overlay-lightbox', { active: showVideoModal }]">
      <div class="modal-card modal-card-video">
        <div class="video-modal-header">
          <span class="video-modal-title">{{ activeVideoTitle }} - Inducción Oficial</span>
          <button type="button" class="video-close-btn" @click="handleCloseVideo">
            &times;
          </button>
        </div>
        <div class="video-container-iframe">
          <iframe
            v-if="showVideoModal && activeVideoId"
            :src="`https://www.youtube.com/embed/${activeVideoId}?autoplay=1&rel=0`"
            title="Video Tutorial"
            frameborder="0"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowfullscreen
            class="video-iframe"
          ></iframe>
        </div>
      </div>
    </div>

    <div :class="['modal-overlay', { active: showDeleteModal }]">
      <div class="modal-card modal-card-sm">
        <div class="modal-header">
          <h3 class="modal-title">Confirmar Eliminación</h3>
          <button type="button" class="modal-close-btn" @click="handleCancelDelete">
            &times;
          </button>
        </div>

        <div class="modal-body">
          <p class="delete-msg">
            ¿Estás seguro de eliminar permanentemente la base de datos
            <strong>{{ resourceToDelete?.nombreRecurso }}</strong>?
          </p>
          <p class="delete-submsg">
            Esta acción desvinculará sus asignaturas y eliminará su archivo de logotipo físico del servidor.
          </p>
        </div>

        <div class="modal-footer">
          <button
            type="button"
            class="btn-secondary"
            :disabled="eliminando"
            @click="handleCancelDelete"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="btn-danger"
            :disabled="eliminando"
            @click="handleExecuteDelete"
          >
            <div v-if="eliminando" class="spinner-sm"></div>
            <span>{{ eliminando ? 'Eliminando...' : 'Eliminar Definitivamente' }}</span>
          </button>
        </div>
      </div>
    </div>

    <transition name="toast-fade">
      <div v-if="toast.show" :class="['toast-notification', `toast-${toast.type}`]">
        <span class="toast-dot"></span>
        <span class="toast-msg">{{ toast.message }}</span>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.resources-view {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 1.5rem;
  max-width: 1400px;
  margin: 0 auto;
}

.header-section {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.header-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.5rem;
  font-weight: 800;
  color: #0F172A;
  margin: 0;
}

.header-subtitle {
  font-size: 0.88rem;
  color: #64748B;
  margin: 0.25rem 0 0 0;
}

.btn-primary-add {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  background: #00A859;
  color: #FFFFFF;
  border: none;
  border-radius: 8px;
  padding: 0.65rem 1.15rem;
  font-size: 0.88rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s, transform 0.15s;
}

.btn-primary-add:hover {
  background: #008744;
  transform: translateY(-1px);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: #FFFFFF;
  border: 1px solid #E2E8F0;
  border-radius: 10px;
  padding: 1rem 1.25rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.stat-icon-wrap {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-icon-green {
  background: rgba(0, 168, 89, 0.12);
  color: #008744;
}

.stat-icon-blue {
  background: rgba(2, 132, 199, 0.12);
  color: #0284C7;
}

.stat-icon-amber {
  background: rgba(217, 119, 6, 0.12);
  color: #D97706;
}

.stat-icon-violet {
  background: rgba(124, 58, 237, 0.12);
  color: #7C3AED;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 0.76rem;
  font-weight: 600;
  text-transform: uppercase;
  color: #64748B;
  letter-spacing: 0.025em;
}

.stat-value {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.35rem;
  font-weight: 800;
  color: #0F172A;
  margin-top: 2px;
}

.toolbar-section {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.search-wrap {
  position: relative;
  flex: 1;
  min-width: 260px;
  max-width: 480px;
  display: flex;
  align-items: center;
}

.search-wrap svg {
  position: absolute;
  left: 12px;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: 0.6rem 2.2rem 0.6rem 2.4rem;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  font-size: 0.88rem;
  background: #FFFFFF;
  color: #0F172A;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.search-input:focus {
  border-color: #008744;
  box-shadow: 0 0 0 3px rgba(0, 135, 68, 0.15);
}

.search-clear-btn {
  position: absolute;
  right: 8px;
  background: transparent;
  border: none;
  font-size: 1.1rem;
  color: #94A3B8;
  cursor: pointer;
  padding: 4px;
}

.filters-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.select-filter {
  padding: 0.55rem 0.85rem;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  background: #FFFFFF;
  font-size: 0.83rem;
  color: #334155;
  outline: none;
  cursor: pointer;
}

.table-container {
  background: #FFFFFF;
  border: 1px solid #E2E8F0;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.state-loading,
.state-empty {
  padding: 3.5rem 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 0.75rem;
  color: #64748B;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #E2E8F0;
  border-top-color: #008744;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.spinner-sm {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-top-color: #FFFFFF;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.empty-icon-wrap {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: #F1F5F9;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-title {
  font-size: 1.05rem;
  font-weight: 700;
  color: #0F172A;
  margin: 0;
}

.empty-desc {
  font-size: 0.85rem;
  max-width: 400px;
  margin: 0;
}

.table-wrapper {
  overflow-x: auto;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.86rem;
}

.data-table thead th {
  background: #F8FAFC;
  padding: 0.85rem 1rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  border-bottom: 1px solid #E2E8F0;
  white-space: nowrap;
}

.data-table tbody td {
  padding: 0.85rem 1rem;
  border-bottom: 1px solid #F1F5F9;
  color: #334155;
  vertical-align: middle;
}

.row-inactive {
  background: #FAFBFD;
  opacity: 0.7;
}

.th-num, .td-num {
  width: 44px;
  text-align: center;
  color: #94A3B8;
  font-weight: 600;
}

.th-resource {
  min-width: 240px;
}

.resource-cell {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.logo-box {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  border: 1px solid #E2E8F0;
  background: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  flex-shrink: 0;
}

.resource-logo-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  padding: 4px;
}

.resource-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.resource-name {
  font-weight: 700;
  color: #0F172A;
  font-size: 0.92rem;
}

.resource-desc {
  font-size: 0.78rem;
  color: #64748B;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 280px;
}

.th-courses {
  min-width: 220px;
}

.chips-container {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px;
}

.course-chip {
  background: #F1F5F9;
  color: #334155;
  border: 1px solid #E2E8F0;
  border-radius: 4px;
  font-size: 0.72rem;
  font-weight: 600;
  padding: 2px 6px;
  white-space: nowrap;
}

.course-chip-more {
  background: #E2E8F0;
  color: #0F172A;
  border-radius: 4px;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 2px 6px;
  cursor: help;
}

.badge-license {
  display: inline-block;
  padding: 3px 8px;
  border-radius: 9999px;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.badge-subscription {
  background: rgba(2, 132, 199, 0.12);
  color: #0284C7;
}

.badge-open-access {
  background: rgba(0, 168, 89, 0.12);
  color: #008744;
}

.th-center, .td-center {
  text-align: center;
  white-space: nowrap;
}

.btn-video-play {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: rgba(220, 38, 38, 0.1);
  color: #DC2626;
  border: 1px solid rgba(220, 38, 38, 0.2);
  border-radius: 6px;
  padding: 4px 8px;
  font-size: 0.74rem;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn-video-play:hover {
  background: rgba(220, 38, 38, 0.18);
}

.star-btn {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 4px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.15s, color 0.15s;
}

.star-btn:hover {
  transform: scale(1.15);
}

.star-active {
  color: #F59E0B;
}

.star-inactive {
  color: #CBD5E1;
}

.star-inactive:hover {
  color: #94A3B8;
}

.switch {
  position: relative;
  display: inline-block;
  width: 38px;
  height: 22px;
}

.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #CBD5E1;
  transition: 0.2s ease;
  border-radius: 9999px;
}

.slider::before {
  position: absolute;
  content: "";
  height: 16px;
  width: 16px;
  left: 3px;
  bottom: 3px;
  background-color: #FFFFFF;
  transition: 0.2s ease;
  border-radius: 50%;
}

input:checked + .slider {
  background-color: #008744;
}

input:checked + .slider::before {
  transform: translateX(16px);
}

.th-actions, .td-actions {
  text-align: right;
  width: 90px;
}

.actions-group {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.icon-btn {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 6px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: background-color 0.15s;
}

.edit-btn {
  color: #6366F1;
}

.edit-btn:hover {
  background: rgba(99, 102, 241, 0.12);
}

.delete-btn {
  color: #EF4444;
}

.delete-btn:hover {
  background: rgba(239, 68, 68, 0.12);
}

.text-muted {
  color: #94A3B8;
  font-size: 0.78rem;
}

.pagination-bar {
  padding: 0.85rem 1.25rem;
  border-top: 1px solid #E2E8F0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.75rem;
  background: #F8FAFC;
}

.pagination-info {
  font-size: 0.8rem;
  color: #64748B;
}

.pagination-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.pagination-btn {
  padding: 0.4rem 0.85rem;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  background: #FFFFFF;
  font-size: 0.8rem;
  font-weight: 600;
  color: #334155;
  cursor: pointer;
  transition: background-color 0.15s;
}

.pagination-btn:hover:not(:disabled) {
  background: #F1F5F9;
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.pagination-current {
  font-size: 0.8rem;
  font-weight: 600;
  color: #475569;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  z-index: 999;
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

.modal-card {
  background: #FFFFFF;
  border-radius: 12px;
  width: 100%;
  max-width: 580px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  display: flex;
  flex-direction: column;
}

.modal-card-sm {
  max-width: 480px;
}

.modal-card-lg {
  max-width: 900px;
}

.modal-header {
  padding: 1.15rem 1.4rem;
  border-bottom: 1px solid #E2E8F0;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  position: sticky;
  top: 0;
  background: #FFFFFF;
  z-index: 10;
}

.modal-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 1.15rem;
  font-weight: 700;
  color: #0F172A;
  margin: 0;
}

.modal-subtitle {
  font-size: 0.8rem;
  color: #64748B;
  margin: 0.25rem 0 0 0;
}

.modal-close-btn {
  background: transparent;
  border: none;
  color: #94A3B8;
  cursor: pointer;
  font-size: 1.4rem;
  line-height: 1;
  padding: 2px 6px;
  border-radius: 6px;
}

.modal-close-btn:hover {
  color: #0F172A;
  background: #F1F5F9;
}

.modal-body {
  padding: 1.4rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-grid-top {
  display: grid;
  grid-template-columns: 240px 1fr;
  gap: 1.25rem;
}

@media (max-width: 768px) {
  .form-grid-top {
    grid-template-columns: 1fr;
  }
}

.dropzone-group {
  display: flex;
  flex-direction: column;
}

.dropzone-area {
  border: 2px dashed #CBD5E1;
  border-radius: 8px;
  height: 220px;
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  cursor: pointer;
  background: #F8FAFC;
  overflow: hidden;
  position: relative;
  transition: border-color 0.15s, background-color 0.15s;
}

.dropzone-area:hover {
  border-color: #008744;
  background: rgba(0, 135, 68, 0.04);
}

.hidden-file-input {
  display: none;
}

.dropzone-prompt {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  padding: 1rem;
}

.dropzone-title {
  font-size: 0.8rem;
  font-weight: 600;
  color: #334155;
}

.dropzone-hint {
  font-size: 0.72rem;
  color: #94A3B8;
}

.dropzone-preview {
  width: 100%;
  height: 100%;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #FFFFFF;
}

.preview-img {
  max-width: 90%;
  max-height: 80%;
  object-fit: contain;
}

.dropzone-change-text {
  position: absolute;
  bottom: 6px;
  background: rgba(15, 23, 42, 0.75);
  color: #FFFFFF;
  font-size: 0.7rem;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 4px;
}

.form-fields-column {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.form-label {
  font-size: 0.82rem;
  font-weight: 600;
  color: #334155;
  display: flex;
  align-items: center;
  gap: 4px;
}

.char-counter {
  font-size: 0.74rem;
  font-weight: 500;
  color: #94A3B8;
  margin-left: auto;
}

.required-star {
  color: #EF4444;
}

.form-input, .form-textarea, .form-select {
  padding: 0.55rem 0.75rem;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  font-size: 0.85rem;
  color: #0F172A;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.form-input:focus, .form-textarea:focus, .form-select:focus {
  border-color: #008744;
  box-shadow: 0 0 0 3px rgba(0, 135, 68, 0.15);
}

.switches-row {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
  padding: 0.5rem 0;
}

.switch-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.switch-field-label {
  font-size: 0.82rem;
  font-weight: 600;
  color: #334155;
}

.form-grid-2col {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.85rem;
}

@media (max-width: 640px) {
  .form-grid-2col {
    grid-template-columns: 1fr;
  }
}

.error-msg {
  font-size: 0.75rem;
  color: #EF4444;
}

.curricular-section {
  border-top: 1px solid #E2E8F0;
  padding-top: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.curricular-header {
  display: flex;
  flex-direction: column;
}

.curricular-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 0.96rem;
  font-weight: 700;
  color: #0F172A;
}

.curricular-desc {
  font-size: 0.78rem;
  color: #64748B;
}

.curricular-workspace {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  height: 280px;
}

@media (max-width: 768px) {
  .curricular-workspace {
    grid-template-columns: 1fr;
    height: auto;
  }
}

.curricular-left-panel, .curricular-right-panel {
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  background: #F8FAFC;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.ciclo-selector-bar {
  padding: 0.6rem 0.85rem;
  background: #FFFFFF;
  border-bottom: 1px solid #E2E8F0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.ciclo-label {
  font-size: 0.78rem;
  font-weight: 700;
  color: #334155;
}

.ciclo-select {
  padding: 0.35rem 0.6rem;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  font-size: 0.78rem;
  font-weight: 600;
  background: #FFFFFF;
  color: #0F172A;
  outline: none;
}

.cursos-checkbox-list {
  padding: 0.6rem;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
}

.curso-checkbox-item {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 5px 8px;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.15s;
}

.curso-checkbox-item:hover {
  background: #E2E8F0;
}

.curso-checkbox-name {
  font-size: 0.78rem;
  color: #1E293B;
  line-height: 1.3;
}

.curso-code {
  color: #64748B;
  font-size: 0.72rem;
  margin-left: 4px;
}

.selected-header {
  padding: 0.6rem 0.85rem;
  background: #FFFFFF;
  border-bottom: 1px solid #E2E8F0;
  display: flex;
  align-items: center;
  gap: 4px;
}

.selected-title {
  font-size: 0.78rem;
  font-weight: 700;
  color: #334155;
}

.selected-count {
  font-size: 0.78rem;
  font-weight: 700;
  color: #008744;
}

.selected-chips-box {
  padding: 0.75rem;
  overflow-y: auto;
  flex: 1;
  background: #FFFFFF;
}

.no-selection-hint {
  font-size: 0.76rem;
  color: #94A3B8;
  text-align: center;
  padding: 2rem 1rem;
}

.selected-chips-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.selected-chip-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  background: rgba(0, 135, 68, 0.08);
  border: 1px solid rgba(0, 135, 68, 0.25);
  border-radius: 6px;
  padding: 3px 8px;
  font-size: 0.74rem;
  color: #0F172A;
}

.chip-ciclo {
  font-weight: 800;
  color: #008744;
  font-size: 0.7rem;
}

.chip-text {
  font-weight: 600;
}

.chip-remove-btn {
  background: transparent;
  border: none;
  font-size: 0.95rem;
  line-height: 1;
  color: #94A3B8;
  cursor: pointer;
  padding: 0 2px;
}

.chip-remove-btn:hover {
  color: #EF4444;
}

.modal-footer {
  padding: 1rem 1.4rem;
  border-top: 1px solid #E2E8F0;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  background: #F8FAFC;
}

.btn-secondary {
  padding: 0.55rem 1rem;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  background: #FFFFFF;
  font-size: 0.84rem;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn-secondary:hover:not(:disabled) {
  background: #F1F5F9;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0.55rem 1.25rem;
  border: none;
  border-radius: 6px;
  background: #00A859;
  font-size: 0.84rem;
  font-weight: 700;
  color: #FFFFFF;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn-primary:hover:not(:disabled) {
  background: #008744;
}

.btn-danger {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0.55rem 1.25rem;
  border: none;
  border-radius: 6px;
  background: #EF4444;
  font-size: 0.84rem;
  font-weight: 700;
  color: #FFFFFF;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn-danger:hover:not(:disabled) {
  background: #DC2626;
}

.header-icon-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.icon-swap-warning {
  width: 38px;
  height: 38px;
  border-radius: 8px;
  background: rgba(217, 119, 6, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.swap-instruction-text {
  font-size: 0.85rem;
  color: #334155;
  line-height: 1.45;
  margin: 0;
}

.modal-card-video {
  max-width: 800px;
  background: #0F172A;
  color: #FFFFFF;
  overflow: hidden;
}

.video-modal-header {
  padding: 0.85rem 1.25rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #0F172A;
}

.video-modal-title {
  font-family: 'Plus Jakarta Sans', system-ui, sans-serif;
  font-size: 0.95rem;
  font-weight: 700;
  color: #FFFFFF;
}

.video-close-btn {
  background: transparent;
  border: none;
  color: #94A3B8;
  font-size: 1.4rem;
  line-height: 1;
  cursor: pointer;
}

.video-close-btn:hover {
  color: #FFFFFF;
}

.video-container-iframe {
  position: relative;
  padding-bottom: 56.25%;
  height: 0;
  overflow: hidden;
  background: #000000;
}

.video-iframe {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
}

.delete-msg {
  font-size: 0.92rem;
  color: #0F172A;
  margin: 0;
}

.delete-submsg {
  font-size: 0.8rem;
  color: #64748B;
  margin: 0.35rem 0 0 0;
}

.toast-notification {
  position: fixed;
  bottom: 24px;
  right: 24px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 0.75rem 1.25rem;
  border-radius: 8px;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
  font-size: 0.85rem;
  font-weight: 600;
  z-index: 1000;
}

.toast-success {
  background: #0F172A;
  color: #FFFFFF;
  border-left: 4px solid #00A859;
}

.toast-error {
  background: #0F172A;
  color: #FFFFFF;
  border-left: 4px solid #EF4444;
}

.toast-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: currentColor;
}

.toast-fade-enter-active,
.toast-fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translateY(10px);
}
</style>
