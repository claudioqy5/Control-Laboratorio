<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios, { API_BASE_URL } from '../config'

const cursos = ref([])
const loading = ref(false)
const searchQuery = ref('')
const selectedCiclo = ref('')
const currentPage = ref(1)
const pageSize = 10

const showModal = ref(false)
const currentCurso = ref({
  idCurso: null,
  codigoCurso: '',
  nombreCurso: '',
  ciclo: 1,
  estadoActivo: true
})

const errors = ref({
  codigoCurso: '',
  nombreCurso: '',
  ciclo: ''
})
const validationError = ref('')

const showDeleteModal = ref(false)
const cursoToDelete = ref(null)
const confirmForceDelete = ref(false)

const toast = ref({ show: false, message: '', type: 'success' })
let toastTimer = null

const showToast = (message, type = 'success') => {
  if (toastTimer) clearTimeout(toastTimer)
  toast.value = { show: true, message, type }
  toastTimer = setTimeout(() => {
    toast.value.show = false
  }, 3500)
}

const loadCursos = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/cursos`)
    cursos.value = Array.isArray(res.data) ? res.data : []
  } catch (err) {
    showToast('Error al conectar con el servidor para obtener las asignaturas.', 'error')
  } finally {
    loading.value = false
  }
}

const filteredCursos = computed(() => {
  let list = [...cursos.value]

  if (selectedCiclo.value) {
    const cNum = Number(selectedCiclo.value)
    list = list.filter(c => c.ciclo === cNum)
  }

  if (searchQuery.value.trim()) {
    const term = searchQuery.value.trim().toLowerCase()
    list = list.filter(c =>
      c.nombreCurso?.toLowerCase().includes(term) ||
      (c.codigoCurso && c.codigoCurso.toLowerCase().includes(term))
    )
  }

  return list
})

const totalPages = computed(() => {
  return Math.ceil(filteredCursos.value.length / pageSize) || 1
})

const paginatedCursos = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredCursos.value.slice(start, start + pageSize)
})

const prevPage = () => {
  if (currentPage.value > 1) currentPage.value--
}

const nextPage = () => {
  if (currentPage.value < totalPages.value) currentPage.value++
}

watch(showModal, (newVal) => {
  if (!newVal) {
    errors.value = {
      codigoCurso: '',
      nombreCurso: '',
      ciclo: ''
    }
    validationError.value = ''
  }
})

const openCreateModal = () => {
  currentCurso.value = {
    idCurso: null,
    codigoCurso: '',
    nombreCurso: '',
    ciclo: 1,
    estadoActivo: true
  }
  showModal.value = true
}

const openEditModal = (curso) => {
  currentCurso.value = {
    idCurso: curso.idCurso,
    codigoCurso: curso.codigoCurso || '',
    nombreCurso: curso.nombreCurso || '',
    ciclo: curso.ciclo || 1,
    estadoActivo: curso.estadoActivo ?? true
  }
  showModal.value = true
}

const saveCurso = async () => {
  errors.value = {
    codigoCurso: '',
    nombreCurso: '',
    ciclo: ''
  }
  validationError.value = ''

  let hasErrors = false

  if (!currentCurso.value.nombreCurso?.trim()) {
    errors.value.nombreCurso = 'El nombre oficial de la asignatura es obligatorio.'
    hasErrors = true
  }

  if (currentCurso.value.ciclo < 1 || currentCurso.value.ciclo > 14) {
    errors.value.ciclo = 'El ciclo académico debe estar comprendido entre 1 y 14.'
    hasErrors = true
  }

  if (hasErrors) return

  const payload = {
    codigoCurso: currentCurso.value.codigoCurso?.trim() || null,
    nombreCurso: currentCurso.value.nombreCurso.trim(),
    ciclo: Number(currentCurso.value.ciclo),
    estadoActivo: Boolean(currentCurso.value.estadoActivo)
  }

  try {
    if (currentCurso.value.idCurso) {
      await axios.put(`${API_BASE_URL}/api/admin/cursos/${currentCurso.value.idCurso}`, payload)
      showToast('Asignatura actualizada exitosamente.')
    } else {
      await axios.post(`${API_BASE_URL}/api/admin/cursos`, payload)
      showToast('Asignatura registrada exitosamente.')
    }
    showModal.value = false
    await loadCursos()
  } catch (err) {
    const errorMsg = err.response?.data?.message || 'Error al guardar la asignatura.'
    validationError.value = errorMsg
    showToast(errorMsg, 'error')
  }
}

const toggleActivo = async (curso) => {
  const previousState = curso.estadoActivo
  curso.estadoActivo = !previousState
  try {
    await axios.patch(`${API_BASE_URL}/api/admin/cursos/${curso.idCurso}/toggle-activo`)
    showToast(`Asignatura ${curso.estadoActivo ? 'activada' : 'desactivada'} correctamente.`)
  } catch (err) {
    curso.estadoActivo = previousState
    showToast('Error al cambiar el estado de la asignatura.', 'error')
  }
}

const promptDelete = (curso) => {
  cursoToDelete.value = { ...curso }
  confirmForceDelete.value = false
  showDeleteModal.value = true
}

const deleteCurso = async (force = false) => {
  if (!cursoToDelete.value) return

  try {
    await axios.delete(`${API_BASE_URL}/api/admin/cursos/${cursoToDelete.value.idCurso}`, {
      params: { force }
    })
    showToast('Asignatura eliminada correctamente.')
    showDeleteModal.value = false
    cursoToDelete.value = null
    await loadCursos()

    if (paginatedCursos.value.length === 0 && currentPage.value > 1) {
      currentPage.value--
    }
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al eliminar la asignatura.'
    showToast(msg, 'error')
  }
}

const deactivateFromDeleteModal = async () => {
  if (!cursoToDelete.value) return

  try {
    await axios.patch(`${API_BASE_URL}/api/admin/cursos/${cursoToDelete.value.idCurso}/toggle-activo`)
    showToast('Asignatura desactivada exitosamente para preservar su integridad referencial.')
    showDeleteModal.value = false
    cursoToDelete.value = null
    await loadCursos()
  } catch (err) {
    showToast('Error al desactivar la asignatura.', 'error')
  }
}

onMounted(() => {
  loadCursos()
})
</script>

<template>
  <div>
    <div class="crud-header">
      <div>        
        <h2 class="title">Malla Curricular de Medicina</h2>
        <div class="subtitle">{{ cursos.length }} Asignaturas Registradas</div>
      </div>
      <div class="header-actions">
        <select v-model="selectedCiclo" @change="currentPage = 1" class="cycle-filter">
          <option value="">Todos los Ciclos</option>
          <option v-for="ciclo in 14" :key="ciclo" :value="ciclo">Ciclo {{ ciclo }}</option>
        </select>

        <div class="search-box">
          <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input
            type="text"
            v-model="searchQuery"
            @input="currentPage = 1"
            placeholder="Buscar por código o nombre..."
            class="search-input"
          >
        </div>

        <button class="btn btn-primary" @click="openCreateModal">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
          Nueva Asignatura
        </button>
      </div>
    </div>

    <table class="centered-table">
      <thead>
        <tr>
          <th style="width: 70px;">Nº</th>
          <th style="width: 140px;">Código</th>
          <th style="text-align: left; padding-left: 1.5rem;">Nombre Oficial de la Asignatura</th>
          <th style="width: 90px;">Ciclo</th>
          <th style="width: 170px;">Bases Asociadas</th>
          <th style="width: 110px;">Estado</th>
          <th style="width: 120px;">Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading">
          <td colspan="7" class="empty-cell">Cargando malla curricular...</td>
        </tr>
        <tr v-else-if="paginatedCursos.length === 0">
          <td colspan="7" class="empty-cell">No se encontraron asignaturas registradas.</td>
        </tr>
        <tr v-for="(c, idx) in paginatedCursos" :key="c.idCurso">
          <td class="col-index">{{ (currentPage - 1) * pageSize + idx + 1 }}</td>
          <td>
            <span v-if="c.codigoCurso" class="code-badge">{{ c.codigoCurso }}</span>
            <span v-else class="empty-badge">-</span>
          </td>
          <td style="text-align: left; padding-left: 1.5rem;">
            <strong class="course-name">{{ c.nombreCurso }}</strong>
          </td>
          <td>
            <span class="cycle-pill">{{ c.ciclo }}</span>
          </td>
          <td>
            <span
              v-if="c.totalBasesAsociadas > 0"
              class="resources-count-badge badge-active"
              :title="c.basesAsociadas?.join(', ') || ''"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><ellipse cx="12" cy="5" rx="9" ry="3"></ellipse><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"></path><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"></path></svg>
              {{ c.totalBasesAsociadas }} {{ c.totalBasesAsociadas === 1 ? 'base' : 'bases' }}
            </span>
            <span v-else class="resources-count-badge badge-empty">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><ellipse cx="12" cy="5" rx="9" ry="3"></ellipse><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"></path><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"></path></svg>
              0 bases
            </span>
          </td>
          <td>
            <label class="switch" :title="c.estadoActivo ? 'Asignatura activa' : 'Asignatura desactivada'">
              <input type="checkbox" :checked="c.estadoActivo" @change="toggleActivo(c)">
              <span class="slider"></span>
            </label>
          </td>
          <td>
            <button class="icon-btn edit-btn" @click="openEditModal(c)" title="Editar Asignatura">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </button>
            <button class="icon-btn delete-btn" @click="promptDelete(c)" title="Eliminar Asignatura">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <div class="pagination-footer">
      <span class="pagination-info">
        Mostrando {{ filteredCursos.length === 0 ? 0 : (currentPage - 1) * pageSize + 1 }} a {{ Math.min(currentPage * pageSize, filteredCursos.length) }} de {{ filteredCursos.length }} registros
      </span>
      <div class="pagination-controls">
        <button class="btn btn-page" @click="prevPage" :disabled="currentPage === 1">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="15 18 9 12 15 6"></polyline></svg>
          Anterior
        </button>
        <span class="pagination-pages">
          Página {{ currentPage }} de {{ totalPages }}
        </span>
        <button class="btn btn-page" @click="nextPage" :disabled="currentPage === totalPages">
          Siguiente
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="9 18 15 12 9 6"></polyline></svg>
        </button>
      </div>
    </div>

    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal-card">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
            </div>
            <h3>{{ currentCurso.idCurso ? 'Editar Asignatura' : 'Nueva Asignatura' }}</h3>
          </div>
          <button class="close-btn" @click="showModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>
        
        <div class="modal-body">
          <div v-if="validationError" class="validation-banner">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="flex-shrink: 0;">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{{ validationError }}</span>
          </div>

          <div class="input-group">
            <label>Código de Asignatura (Opcional)</label>
            <input
              v-model="currentCurso.codigoCurso"
              placeholder="Ej. MH-0101"
              maxlength="20"
              class="premium-input"
            >
          </div>
          
          <div class="input-group">
            <label :class="{ 'error-label': errors.nombreCurso }">Nombre Oficial de la Asignatura *</label>
            <input
              v-model="currentCurso.nombreCurso"
              placeholder="Ej. Anatomía Humana I"
              maxlength="150"
              class="premium-input"
              :class="{ 'invalid': errors.nombreCurso }"
            >
            <span v-if="errors.nombreCurso" class="field-error-message">{{ errors.nombreCurso }}</span>
          </div>

          <div class="input-group">
            <label :class="{ 'error-label': errors.ciclo }">Ciclo Académico *</label>
            <select v-model.number="currentCurso.ciclo" class="premium-input" :class="{ 'invalid': errors.ciclo }">
              <option v-for="ciclo in 14" :key="ciclo" :value="ciclo">Ciclo {{ ciclo }}</option>
            </select>
            <span v-if="errors.ciclo" class="field-error-message">{{ errors.ciclo }}</span>
          </div>

          <div class="input-group switch-row">
            <div class="switch-field">
              <label class="switch">
                <input type="checkbox" v-model="currentCurso.estadoActivo">
                <span class="slider"></span>
              </label>
              <span class="switch-label">Asignatura activa en la malla curricular</span>
            </div>
          </div>
        </div>
        
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Cancelar</button>
          <button class="btn btn-primary" @click="saveCurso">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path><polyline points="17 21 17 13 7 13 7 21"></polyline><polyline points="7 3 7 8 15 8"></polyline></svg>
            Guardar Asignatura
          </button>
        </div>
      </div>
    </div>

    <div v-if="showDeleteModal && cursoToDelete" class="modal-backdrop" @click.self="showDeleteModal = false">
      <div class="modal-card">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon" :class="cursoToDelete.totalBasesAsociadas > 0 ? 'icon-warning' : 'icon-danger'">
              <svg v-if="cursoToDelete.totalBasesAsociadas > 0" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>
              <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
            </div>
            <h3>{{ cursoToDelete.totalBasesAsociadas > 0 ? 'Protección Referencial: Asignatura Vinculada' : 'Confirmar Eliminación' }}</h3>
          </div>
          <button class="close-btn" @click="showDeleteModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>

        <div class="modal-body">
          <div v-if="cursoToDelete.totalBasesAsociadas === 0">
            <p class="delete-confirm-text">
              ¿Está seguro de eliminar permanentemente la asignatura
              <strong>"{{ cursoToDelete.nombreCurso }}"</strong> (Ciclo {{ cursoToDelete.ciclo }})?
            </p>
            <p class="delete-warning-subtext">Esta acción es irreversible y retirará el registro de la malla curricular.</p>
          </div>

          <div v-else>
            <div class="validation-banner banner-warning">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="flex-shrink: 0;">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="12" y1="8" x2="12" y2="12"></line>
                <line x1="12" y1="16" x2="12.01" y2="16"></line>
              </svg>
              <span>Esta asignatura está vinculada a {{ cursoToDelete.totalBasesAsociadas }} bases de datos médicas.</span>
            </div>

            <div class="linked-resources-box">
              <div class="linked-resources-title">Bases de datos médicas asociadas:</div>
              <ul class="linked-resources-list">
                <li v-for="(base, bIdx) in cursoToDelete.basesAsociadas" :key="bIdx">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><ellipse cx="12" cy="5" rx="9" ry="3"></ellipse><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"></path><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"></path></svg>
                  <span>{{ base }}</span>
                </li>
              </ul>
            </div>

            <div class="recommendation-card">
              <div class="recommendation-header">
                <strong>Alternativa Recomendada:</strong>
                <span>Desactivar en lugar de eliminar</span>
              </div>
              <p class="recommendation-desc">
                Desactivar oculta la asignatura de las vistas públicas sin romper los vínculos con las bases de datos médicas asociadas.
              </p>
              <button class="btn btn-recommend" @click="deactivateFromDeleteModal">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18.36 6.64a9 9 0 1 1-12.73 0"></path><line x1="12" y1="2" x2="12" y2="12"></line></svg>
                Desactivar Asignatura (Recomendado)
              </button>
            </div>

            <div class="force-delete-section">
              <label class="force-checkbox-label">
                <input type="checkbox" v-model="confirmForceDelete">
                <span>Entiendo los riesgos y confirmo desvincular forzosamente esta asignatura de todas las bases asociadas y eliminarla en su totalidad.</span>
              </label>
              <button
                class="btn btn-danger-force"
                :disabled="!confirmForceDelete"
                @click="deleteCurso(true)"
              >
                Desvincular de todas las bases y Eliminar
              </button>
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showDeleteModal = false">Cancelar</button>
          <button
            v-if="cursoToDelete.totalBasesAsociadas === 0"
            class="btn btn-danger"
            @click="deleteCurso(false)"
          >
            Eliminar Asignatura
          </button>
        </div>
      </div>
    </div>

    <transition name="toast-fade">
      <div v-if="toast.show" class="toast-notification" :class="toast.type === 'error' ? 'toast-error' : 'toast-success'">
        <span class="toast-dot"></span>
        <span>{{ toast.message }}</span>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.crud-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 2rem;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.title {
  color: #111827;
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.25rem 0;
}

.subtitle {
  color: #6b7280;
  font-size: 0.875rem;
}

.header-actions {
  display: flex;
  gap: 0.875rem;
  align-items: center;
  flex-wrap: wrap;
}

.cycle-filter {
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
  background: #ffffff;
  color: #111827;
  font-size: 0.875rem;
  outline: none;
  cursor: pointer;
}

.search-box {
  position: relative;
}

.search-icon {
  position: absolute;
  left: 10px;
  top: 50%;
  transform: translateY(-50%);
  color: #9ca3af;
}

.search-input {
  padding: 0.5rem 1rem 0.5rem 2rem;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
  width: 280px;
  font-size: 0.875rem;
  color: #111827;
  outline: none;
  background: #ffffff;
}

.search-input:focus,
.cycle-filter:focus {
  border-color: #9f1239;
}

.btn {
  padding: 0.55rem 1.1rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 1px solid transparent;
  transition: all 0.2s ease;
}

.btn-primary {
  background: #9f1239;
  border-color: #9f1239;
  color: #ffffff;
}

.btn-primary:hover {
  background: #881337;
  border-color: #881337;
}

.btn-secondary {
  background: #ffffff;
  border-color: #e2e8f0;
  color: #475569;
}

.btn-secondary:hover {
  background: #f1f5f9;
}

.btn-danger {
  background: #ef4444;
  border-color: #ef4444;
  color: #ffffff;
}

.btn-danger:hover {
  background: #dc2626;
}

.centered-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  background: #ffffff;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
}

.centered-table th,
.centered-table td {
  padding: 0.9rem 1rem;
  text-align: center;
  border-bottom: 1px solid #f1f5f9;
}

.centered-table th {
  background: #f8fafc;
  color: #475569;
  font-size: 0.78rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.col-index {
  color: #6b7280;
  font-weight: 600;
}

.code-badge {
  background: #f1f5f9;
  color: #475569;
  padding: 4px 8px;
  border-radius: 6px;
  font-size: 0.8rem;
  font-family: monospace;
  border: 1px solid #e2e8f0;
  font-weight: 600;
}

.empty-badge {
  color: #cbd5e1;
  font-weight: 600;
}

.course-name {
  color: #111827;
  font-weight: 600;
  font-size: 0.925rem;
}

.cycle-pill {
  display: inline-block;
  min-width: 28px;
  padding: 3px 8px;
  border-radius: 9999px;
  background: #f1f5f9;
  color: #334155;
  font-weight: 700;
  font-size: 0.8rem;
}

.resources-count-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 9999px;
  font-size: 0.8rem;
  font-weight: 600;
}

.badge-active {
  background: #eff6ff;
  color: #1e40af;
  border: 1px solid #bfdbfe;
}

.badge-empty {
  background: #f8fafc;
  color: #94a3b8;
  border: 1px dashed #cbd5e1;
}

.empty-cell {
  text-align: center;
  color: #9ca3af;
  padding: 3rem;
}

.icon-btn {
  background: transparent;
  border: none;
  padding: 6px;
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.edit-btn {
  color: #6366f1;
  margin-right: 6px;
}

.edit-btn:hover {
  background: #e0e7ff;
}

.delete-btn {
  color: #ef4444;
}

.delete-btn:hover {
  background: #fee2e2;
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
  background-color: #cbd5e1;
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
  background-color: #ffffff;
  transition: 0.2s ease;
  border-radius: 50%;
}

input:checked + .slider {
  background-color: #008744;
}

input:checked + .slider::before {
  transform: translateX(16px);
}

.pagination-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 1.5rem;
  background: #ffffff;
  padding: 1rem;
  border-radius: 12px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
}

.pagination-info {
  color: #64748b;
  font-size: 0.875rem;
  font-weight: 500;
}

.pagination-controls {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.btn-page {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  color: #475569;
}

.btn-page:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.pagination-pages {
  color: #111827;
  font-size: 0.875rem;
  font-weight: 600;
  padding: 0 0.5rem;
}

.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: fadeIn 0.2s ease-out;
}

.modal-card {
  width: 100%;
  max-width: 520px;
  max-height: 95vh;
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  display: flex;
  flex-direction: column;
  animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  overflow: hidden;
}

.modal-header {
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #f1f5f9;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #f8fafc;
}

.modal-title-wrapper {
  display: flex;
  align-items: center;
  gap: 12px;
}

.modal-title-wrapper h3 {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 800;
  color: #0f172a;
}

.modal-icon {
  background: #e0e7ff;
  color: #4f46e5;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.icon-warning {
  background: #fef3c7;
  color: #b45309;
}

.icon-danger {
  background: #fee2e2;
  color: #dc2626;
}

.close-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 6px;
  border-radius: 8px;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-btn:hover {
  background: #e2e8f0;
  color: #475569;
}

.modal-body {
  padding: 1.5rem 2rem;
  overflow-y: auto;
}

.validation-banner {
  background: #fef2f2;
  border: 1px solid #fee2e2;
  border-radius: 12px;
  padding: 0.75rem 1rem;
  color: #991b1b;
  font-size: 0.85rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 1.25rem;
}

.banner-warning {
  background: #fffbeb;
  border-color: #fef3c7;
  color: #92400e;
}

.input-group {
  margin-bottom: 1.25rem;
  display: flex;
  flex-direction: column;
}

.input-group label {
  font-size: 0.78rem;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 6px;
}

.error-label {
  color: #ef4444;
}

.premium-input {
  padding: 0.75rem 1rem;
  border: 1.5px solid #e2e8f0;
  border-radius: 10px;
  font-size: 0.95rem;
  color: #1e293b;
  outline: none;
  transition: all 0.2s ease-in-out;
  background: #f8fafc;
}

.premium-input:focus {
  border-color: #9f1239;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(159, 18, 57, 0.1);
}

.premium-input.invalid {
  border-color: #fca5a5;
  background: #fff8f8;
}

.field-error-message {
  font-size: 0.75rem;
  font-weight: 600;
  color: #ef4444;
  margin-top: 4px;
}

.switch-row {
  margin-top: 0.5rem;
}

.switch-field {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.switch-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #334155;
}

.modal-footer {
  padding: 1.25rem 2rem;
  border-top: 1px solid #f1f5f9;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  background: #f8fafc;
}

.delete-confirm-text {
  font-size: 0.95rem;
  color: #1e293b;
  line-height: 1.5;
  margin: 0 0 0.5rem 0;
}

.delete-warning-subtext {
  font-size: 0.85rem;
  color: #64748b;
  margin: 0;
}

.linked-resources-box {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 1rem;
  margin-bottom: 1.25rem;
}

.linked-resources-title {
  font-size: 0.8rem;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 0.5rem;
}

.linked-resources-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 140px;
  overflow-y: auto;
}

.linked-resources-list li {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.875rem;
  color: #1e293b;
  font-weight: 500;
}

.recommendation-card {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  border-radius: 12px;
  padding: 1rem;
  margin-bottom: 1.25rem;
}

.recommendation-header {
  display: flex;
  gap: 6px;
  align-items: baseline;
  font-size: 0.875rem;
  color: #166534;
  margin-bottom: 0.25rem;
}

.recommendation-desc {
  font-size: 0.8rem;
  color: #15803d;
  margin: 0 0 0.75rem 0;
  line-height: 1.4;
}

.btn-recommend {
  background: #008744;
  border-color: #008744;
  color: #ffffff;
  width: 100%;
  justify-content: center;
}

.btn-recommend:hover {
  background: #007a3d;
  border-color: #007a3d;
}

.force-delete-section {
  border-top: 1px solid #fee2e2;
  padding-top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.force-checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  font-size: 0.8rem;
  color: #991b1b;
  font-weight: 600;
  cursor: pointer;
  line-height: 1.35;
}

.btn-danger-force {
  background: #dc2626;
  border-color: #dc2626;
  color: #ffffff;
  justify-content: center;
}

.btn-danger-force:disabled {
  opacity: 0.45;
  cursor: not-allowed;
  background: #94a3b8;
  border-color: #94a3b8;
}

.btn-danger-force:not(:disabled):hover {
  background: #b91c1c;
  border-color: #b91c1c;
}

.toast-notification {
  position: fixed;
  bottom: 24px;
  right: 24px;
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 12px 18px;
  border-radius: 12px;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3);
  font-size: 0.875rem;
  font-weight: 600;
  z-index: 1000;
}

.toast-success {
  background: #0f172a;
  color: #ffffff;
  border-left: 4px solid #00a859;
}

.toast-error {
  background: #0f172a;
  color: #ffffff;
  border-left: 4px solid #ef4444;
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

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slideUp {
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}
</style>
