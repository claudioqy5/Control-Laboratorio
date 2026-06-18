<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

// Estado del componente
const categorias = ref([])
const searchQuery = ref('')
const showModal = ref(false)
const currentPage = ref(1)
const pageSize = 10

const currentCategoria = ref({
  categoriaID: null,
  codigo: '',
  nombre: '',
  descripcion: ''
})

const toast = ref({ show: false, message: '', type: 'success' })

const showToast = (message, type = 'success') => {
  toast.value = { show: true, message, type }
  setTimeout(() => {
    toast.value.show = false
  }, 4000)
}

const errors = ref({
  codigo: '',
  nombre: ''
})
const validationError = ref('')

// Cargar datos desde el API
const loadCategorias = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/Categorias`)
    categorias.value = res.data
  } catch (err) {
    console.error("Error al cargar categorías:", err)
    showToast('Error al conectar con el servidor', 'error')
  }
}

const filteredCategorias = computed(() => {
  let result = categorias.value

  if (searchQuery.value) {
    const queryWords = searchQuery.value.toLowerCase().split(/\s+/).filter(Boolean)
    result = result.filter(categoria => {
      const catString = `${categoria.nombre} ${categoria.codigo} ${categoria.descripcion || ''}`.toLowerCase()
      return queryWords.every(word => catString.includes(word))
    })
  }
  return result
})

const totalPages = computed(() => {
  return Math.ceil(filteredCategorias.value.length / pageSize) || 1
})

const paginatedCategorias = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  const end = start + pageSize
  return filteredCategorias.value.slice(start, end)
})

const nextPage = () => {
  if (currentPage.value < totalPages.value) currentPage.value++
}

const prevPage = () => {
  if (currentPage.value > 1) currentPage.value--
}

watch(showModal, (newVal) => {
  if (!newVal) {
    errors.value = {
      codigo: '',
      nombre: ''
    }
    validationError.value = ''
  }
})

const saveCategoria = async () => {
  errors.value = {
    codigo: '',
    nombre: ''
  }
  validationError.value = ''

  let hasErrors = false

  if (!currentCategoria.value.codigo?.trim()) {
    errors.value.codigo = 'El código es obligatorio.'
    hasErrors = true
  }
  if (!currentCategoria.value.nombre?.trim()) {
    errors.value.nombre = 'El nombre es obligatorio.'
    hasErrors = true
  }

  if (hasErrors) {
    validationError.value = 'Por favor, corrija los campos marcados en rojo con sus respectivos errores.'
    return
  }

  try {
    const isNew = !currentCategoria.value.categoriaID
    let savedId = currentCategoria.value.categoriaID

    const payload = { ...currentCategoria.value }
    if (isNew) {
      payload.categoriaID = 0
    }

    if (isNew) {
      await axios.post(`${API_BASE_URL}/api/Categorias`, payload)
      showToast('Categoría registrada exitosamente.', 'success')
    } else {
      await axios.put(`${API_BASE_URL}/api/Categorias/${savedId}`, payload)
      showToast('Categoría actualizada correctamente.', 'success')
    }

    await loadCategorias()
    showModal.value = false
  } catch (error) {
    console.error("Error saving categoria:", error)
    if (error.response && error.response.data) {
      if (error.response.data.mensaje) {
        validationError.value = error.response.data.mensaje
      } else if (error.response.data.errors) {
        const errMessages = []
        for (const field in error.response.data.errors) {
          errMessages.push(...error.response.data.errors[field])
        }
        validationError.value = errMessages.join(" | ")
      } else {
        validationError.value = "Ocurrió un error de validación en el servidor."
      }
    } else {
      validationError.value = "Ocurrió un error de red al guardar la categoría."
    }
  }
}

const editCategoria = (categoria) => {
  currentCategoria.value = { ...categoria }
  showModal.value = true
}

const deleteCategoria = async (id) => {
  if (confirm('¿Está seguro de eliminar esta categoría?')) {
    try {
      await axios.delete(`${API_BASE_URL}/api/Categorias/${id}`)
      showToast('Categoría eliminada correctamente.', 'success')
      await loadCategorias()
      
      if (paginatedCategorias.value.length === 0 && currentPage.value > 1) {
        currentPage.value--
      }
    } catch (err) {
      console.error("Error al eliminar categoría", err)
      if (err.response && err.response.data && err.response.data.mensaje) {
        showToast(err.response.data.mensaje, 'error')
      } else {
        showToast('Error al eliminar la categoría.', 'error')
      }
    }
  }
}

onMounted(() => {
  loadCategorias()
})
</script>

<template>
  <div>
    <!-- Toast Notification -->
    <div class="toast-container" :class="{ 'toast-show': toast.show, 'toast-success': toast.type === 'success', 'toast-error': toast.type === 'error' }">
      <div class="toast-icon">
        <svg v-if="toast.type === 'success'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
        <svg v-if="toast.type === 'error'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
      </div>
      <div class="toast-message">{{ toast.message }}</div>
    </div>

    <!-- Cabecera -->
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Categorías de Libros</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">{{ categorias.length }} Categorías Registradas</div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        <!-- Buscador -->
        <div style="position: relative;">
          <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: #9ca3af;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input
            type="text"
            v-model="searchQuery"
            @input="currentPage = 1"
            placeholder="Buscar por código, nombre o descripción..."
            :style="{
              padding: '0.5rem 1rem 0.5rem 2rem',
              borderRadius: '0.5rem',
              border: '1px solid #e5e7eb',
              width: '320px',
              fontSize: '0.875rem',
              color: '#111827',
              outline: 'none'
            }"
          >
        </div>
        <!-- Botón Nuevo -->
        <button class="btn btn-primary" @click="currentCategoria = { categoriaID: null, codigo: '', nombre: '', descripcion: '' }; showModal = true">Nueva Categoría</button>
      </div>
    </div>

    <!-- Tabla -->
    <table class="centered-table">
      <thead>
        <tr>
          <th style="width: 80px;">Nº</th>
          <th style="width: 150px;">Código</th>
          <th style="width: 250px;">Nombre</th>
          <th>Descripción</th>
          <th style="width: 150px;">Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="paginatedCategorias.length === 0">
          <td colspan="5" style="text-align: center; color: #9ca3af; padding: 3rem;">No se encontraron categorías registradas.</td>
        </tr>
        <tr v-for="(c, idx) in paginatedCategorias" :key="c.categoriaID">
          <td style="color: #6b7280; font-weight: 600;">{{ (currentPage - 1) * pageSize + idx + 1 }}</td>
          <td><span style="background: #f1f5f9; color: #475569; padding: 4px 8px; border-radius: 6px; font-size: 0.8rem; font-family: monospace; border: 1px solid #e2e8f0; font-weight: 600;">{{ c.codigo }}</span></td>
          <td><strong style="color: #111827;">{{ c.nombre }}</strong></td>
          <td style="text-align: left; padding-left: 1rem;">{{ c.descripcion || '-' }}</td>
          <td style="white-space: nowrap;">
            <button class="icon-btn edit-btn" @click="editCategoria(c)" title="Editar Categoría">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </button>
            <button class="icon-btn delete-btn" @click="deleteCategoria(c.categoriaID)" title="Eliminar Categoría">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Paginación -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 1.5rem; background: white; padding: 1rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);">
      <span style="color: #64748b; font-size: 0.875rem; font-weight: 500;">
        Mostrando {{ (currentPage - 1) * pageSize + 1 }} a {{ Math.min(currentPage * pageSize, filteredCategorias.length) }} de {{ filteredCategorias.length }} registros
      </span>
      <div style="display: flex; gap: 0.5rem; align-items: center;">
        <button class="btn" style="background: white; border: 1px solid #e2e8f0; color: #475569; display: flex; align-items: center; gap: 4px;" @click="prevPage" :disabled="currentPage === 1" :style="{ opacity: currentPage === 1 ? '0.5' : '1', cursor: currentPage === 1 ? 'not-allowed' : 'pointer' }">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="15 18 9 12 15 6"></polyline></svg>
          Anterior
        </button>
        
        <span style="color: #111827; font-size: 0.875rem; font-weight: 600; padding: 0 0.5rem;">
          Página {{ currentPage }} de {{ totalPages }}
        </span>

        <button class="btn" style="background: white; border: 1px solid #e2e8f0; color: #475569; display: flex; align-items: center; gap: 4px;" @click="nextPage" :disabled="currentPage === totalPages" :style="{ opacity: currentPage === totalPages ? '0.5' : '1', cursor: currentPage === totalPages ? 'not-allowed' : 'pointer' }">
          Siguiente
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="9 18 15 12 9 6"></polyline></svg>
        </button>
      </div>
    </div>

    <!-- Premium Modal Overlay -->
    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal-card" style="max-width: 500px;">
        <!-- Header -->
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2A10 10 0 0 0 2 12a10 10 0 0 0 10 10a10 10 0 0 0 10-10A10 10 0 0 0 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z"></path></svg>
            </div>
            <h3>{{ currentCategoria.categoriaID ? 'Editar Categoría' : 'Nueva Categoría' }}</h3>
          </div>
          <button class="close-btn" @click="showModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>
        
        <!-- Body -->
        <div class="modal-body">
          <!-- Validation Banner -->
          <div v-if="validationError" class="validation-banner">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="flex-shrink: 0;">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{{ validationError }}</span>
          </div>

          <div class="input-group">
            <label :class="{ 'error-label': errors.codigo }">Código de Categoría *</label>
            <input v-model="currentCategoria.codigo" placeholder="Ej. PED, DERM, ANAT" class="premium-input" :class="{ 'invalid': errors.codigo }">
            <span v-if="errors.codigo" class="field-error-message">{{ errors.codigo }}</span>
          </div>
          
          <div class="input-group">
            <label :class="{ 'error-label': errors.nombre }">Nombre de Categoría *</label>
            <input v-model="currentCategoria.nombre" placeholder="Ej. Pediatría" class="premium-input" :class="{ 'invalid': errors.nombre }">
            <span v-if="errors.nombre" class="field-error-message">{{ errors.nombre }}</span>
          </div>

          <div class="input-group">
            <label>Descripción / Detalles</label>
            <textarea v-model="currentCategoria.descripcion" placeholder="Opcional..." class="premium-input" style="height: 80px; resize: none;"></textarea>
          </div>
        </div>
        
        <!-- Footer -->
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Cancelar</button>
          <button class="btn btn-primary premium-btn" @click="saveCategoria">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path><polyline points="17 21 17 13 7 13 7 21"></polyline><polyline points="7 3 7 8 15 8"></polyline></svg>
            Guardar Categoría
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.centered-table th, .centered-table td {
  text-align: center;
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
  margin-right: 8px;
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

/* Premium Modal Styles */
.modal-backdrop {
  position: fixed;
  top: 0; left: 0; width: 100%; height: 100%;
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
  max-width: 500px;
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
  font-size: 1.25rem;
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
  margin-bottom: 1.5rem;
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
  transition: color 0.2s;
}

.error-label {
  color: #ef4444 !important;
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
  border-color: #6366f1;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(99, 102, 241, 0.1);
}

.premium-input.invalid {
  border-color: #fca5a5;
  background: #fff8f8;
}

.premium-input.invalid:focus {
  box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.1);
}

.field-error-message {
  font-size: 0.75rem;
  font-weight: 600;
  color: #ef4444;
  margin-top: 4px;
}

.modal-footer {
  padding: 1.25rem 2rem;
  border-top: 1px solid #f1f5f9;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  background: #f8fafc;
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
