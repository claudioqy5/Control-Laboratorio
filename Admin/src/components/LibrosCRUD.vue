<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

// Estado del componente
const libros = ref([])
const searchQuery = ref('')
const showModal = ref(false)
const currentPage = ref(1)
const pageSize = 10

const currentLibro = ref({
  libroID: null,
  nroRegistro: '',
  codigoBarras: '',
  nroClasificacion: '',
  titulo: '',
  autor: '',
  anio: '',
  editorial: '',
  edicion: '',
  portada: '',
  categoria: 'Medicina General',
  idioma: 'Español',
  paginas: 0,
  estado: 'Disponible',
  resumen: '',
  estante: null,
  cara: '',
  piso: null
})

const toast = ref({ show: false, message: '', type: 'success' })

const showToast = (message, type = 'success') => {
  toast.value = { show: true, message, type }
  setTimeout(() => {
    toast.value.show = false
  }, 4000)
}

const errors = ref({
  nroRegistro: false,
  codigoBarras: false,
  titulo: false,
  autor: false
})
const validationError = ref('')

// Cargar datos desde el API
const loadLibros = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/Libros`)
    libros.value = res.data
  } catch (err) {
    console.error("Error al cargar libros:", err)
    showToast('Error al conectar con el servidor', 'error')
  }
}

const filteredLibros = computed(() => {
  let result = libros.value

  if (searchQuery.value) {
    const queryWords = searchQuery.value.toLowerCase().split(/\s+/).filter(Boolean)
    result = result.filter(libro => {
      const bookString = `${libro.titulo} ${libro.autor} ${libro.nroRegistro} ${libro.codigoBarras} ${libro.nroClasificacion}`.toLowerCase()
      return queryWords.every(word => bookString.includes(word))
    })
  }
  return result
})

const totalPages = computed(() => {
  return Math.ceil(filteredLibros.value.length / pageSize) || 1
})

const paginatedLibros = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  const end = start + pageSize
  return filteredLibros.value.slice(start, end)
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
      nroRegistro: false,
      codigoBarras: false,
      titulo: false,
      autor: false
    }
    validationError.value = ''
  }
})

const saveLibro = async () => {
  errors.value = {
    nroRegistro: false,
    codigoBarras: false,
    titulo: false,
    autor: false
  }
  validationError.value = ''

  const missingFields = []
  if (!currentLibro.value.nroRegistro?.trim()) {
    errors.value.nroRegistro = true
    missingFields.push('N° registro')
  }
  if (!currentLibro.value.codigoBarras?.trim()) {
    errors.value.codigoBarras = true
    missingFields.push('Código de barras')
  }
  if (!currentLibro.value.titulo?.trim()) {
    errors.value.titulo = true
    missingFields.push('Título')
  }
  if (!currentLibro.value.autor?.trim()) {
    errors.value.autor = true
    missingFields.push('Autor')
  }

  if (missingFields.length > 0) {
    validationError.value = `Falta ingresar los siguientes datos obligatorios: ${missingFields.join(', ')}.`
    return
  }

    try {
      const isNew = !currentLibro.value.libroID
      let savedLibroId = currentLibro.value.libroID
  
      // Prepare payload
      const payload = { ...currentLibro.value }
      
      // Fix for ASP.NET Core rejecting 'null' for int properties
      if (isNew) {
        payload.libroID = 0
      }
      
      // Paginacion es number
      payload.paginas = Number(payload.paginas) || 0
      if (payload.estante) {
        payload.estante = parseInt(payload.estante, 10)
        if (payload.estante > 31) payload.estante = 31
      }
      if (payload.piso) payload.piso = Number(payload.piso)
  
      if (portadaFile.value) {
        payload.portada = null // Backend will update this when we upload the file
      }
  
      if (isNew) {
        const res = await axios.post(`${API_BASE_URL}/api/Libros`, payload)
      savedLibroId = res.data.libroID
      showToast('Libro registrado exitosamente.', 'success')
    } else {
      await axios.put(`${API_BASE_URL}/api/Libros/${savedLibroId}`, payload)
      showToast('Datos del libro actualizados correctamente.', 'success')
    }

    // Si hay un archivo de portada seleccionado, subirlo
    if (portadaFile.value && savedLibroId) {
      const formData = new FormData()
      formData.append('file', portadaFile.value)
      
      const uploadRes = await axios.post(`${API_BASE_URL}/api/Libros/${savedLibroId}/Portada`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      console.log('Portada subida', uploadRes.data)
    }

    // Recargar datos desde el backend
    await loadLibros()
    showModal.value = false
    portadaFile.value = null

    } catch (error) {
      console.error("Error saving libro:", error)
      if (error.response && error.response.data) {
        if (error.response.data.mensaje) {
          validationError.value = error.response.data.mensaje
        } else if (error.response.data.errors) {
          // Extraer errores de validación automáticos de ASP.NET Core
          const errMessages = []
          for (const field in error.response.data.errors) {
            errMessages.push(...error.response.data.errors[field])
          }
          validationError.value = errMessages.join(" | ")
        } else {
          validationError.value = "Ocurrió un error de validación en el servidor."
        }
      } else {
        validationError.value = "Ocurrió un error de red al guardar el libro."
      }
    }
  }

const editLibro = (libro) => {
  currentLibro.value = { ...libro }
  showModal.value = true
}

const deleteLibro = async (id) => {
  if (confirm('¿Está seguro de eliminar este libro del catálogo?')) {
    try {
      await axios.delete(`${API_BASE_URL}/api/Libros/${id}`)
      showToast('Libro eliminado correctamente.', 'success')
      await loadLibros()
      
      // Ajustar página actual si queda vacía
      if (paginatedLibros.value.length === 0 && currentPage.value > 1) {
        currentPage.value--
      }
    } catch (err) {
      console.error("Error al eliminar", err)
      showToast('Error al eliminar libro.', 'error')
    }
  }
}

const portadaFile = ref(null)

const handlePortadaChange = (e) => {
  const file = e.target.files[0]
  if (file) {
    if (file.size > 1024 * 1024) {
      showToast('La imagen supera el límite de 1MB.', 'error')
      return
    }
    portadaFile.value = file
    const reader = new FileReader()
    reader.onload = (event) => {
      // Show preview only
      currentLibro.value.portada = event.target.result
    }
    reader.readAsDataURL(file)
  }
}

const removePortada = () => {
  currentLibro.value.portada = ''
  portadaFile.value = null
}

const showImageModal = ref(false)
const selectedImage = ref('')

const viewImage = (url) => {
  selectedImage.value = url.startsWith('data:') ? url : (url.startsWith('/portadas') ? API_BASE_URL + '/api/static' + url : API_BASE_URL + url)
  showImageModal.value = true
}

onMounted(() => {
  loadLibros()
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
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Registro de Libros</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">{{ libros.length }} Catálogos Registrados (Simulación Frontend)</div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        <!-- Buscador -->
        <div style="position: relative;">
          <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: #9ca3af;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input
            type="text"
            v-model="searchQuery"
            @input="currentPage = 1"
            placeholder="Buscar por título, autor, clasificación..."
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
        <button class="btn btn-primary" @click="currentLibro = { libroID: null, nroRegistro: '', codigoBarras: '', nroClasificacion: '', titulo: '', autor: '', anio: '', editorial: '', edicion: '', categoria: 'Medicina General', idioma: 'Español', paginas: 0, estado: 'Disponible', resumen: '', portada: '', estante: null, cara: '', piso: null }; showModal = true">Nuevo Libro</button>
      </div>
    </div>

    <!-- Tabla -->
    <table class="centered-table">
      <thead>
        <tr>
          <th>Libro</th>
          <th>Editorial / Año</th>
          <th>Nro. Registro</th>
          <th>Clasificación</th>
          <th>Estante</th>
          <th>Piso / Cara</th>
          <th>Estado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="paginatedLibros.length === 0">
          <td colspan="8" style="text-align: center; color: #9ca3af; padding: 3rem;">No se encontraron libros registrados.</td>
        </tr>
        <tr v-for="l in paginatedLibros" :key="l.libroID">
          <!-- Columna 1: Libro -->
          <td>
            <div style="display: flex; gap: 1rem; align-items: center; text-align: left;">
              <div class="book-cover-thumbnail" style="flex-shrink: 0; width: 45px; height: 60px;">
                <img v-if="l.portada" :src="l.portada.startsWith('data:') ? l.portada : (l.portada.startsWith('/portadas') ? API_BASE_URL + '/api/static' + l.portada : API_BASE_URL + l.portada)" alt="Portada" style="width: 100%; height: 100%; object-fit: cover; border-radius: 4px; cursor: zoom-in; box-shadow: 0 2px 4px rgba(0,0,0,0.1);" @click="viewImage(l.portada)" />
                <div v-else class="book-cover-placeholder">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
                </div>
              </div>
              <div style="min-width: 0;">
                <div style="font-weight: 700; color: #0f172a; font-size: 0.95rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 200px;" :title="l.titulo">{{ l.titulo }}</div>
                <div style="color: #64748b; font-size: 0.85rem; margin-top: 2px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 200px;" :title="l.autor">{{ l.autor }}</div>
              </div>
            </div>
          </td>

          <!-- Columna 2: Editorial / Año -->
          <td>
            <div style="text-align: center;">
              <div style="font-weight: 600; color: #334155; font-size: 0.9rem;">{{ l.editorial || 'S/E' }}</div>
              <div style="color: #64748b; font-size: 0.8rem; margin-top: 2px;">{{ l.anio || 'S/A' }} <span v-if="l.edicion">({{ l.edicion }})</span></div>
            </div>
          </td>

          <!-- Columna 3: Nro. Registro -->
          <td>
            <strong style="color: #111827; font-size: 0.95rem;">{{ l.nroRegistro }}</strong>
          </td>

          <!-- Columna 4: Clasificación -->
          <td>
            <span style="background: #f1f5f9; color: #475569; padding: 4px 8px; border-radius: 6px; font-size: 0.8rem; font-family: monospace; border: 1px solid #e2e8f0; font-weight: 600;">
              {{ l.nroClasificacion || 'N/A' }}
            </span>
          </td>

          <!-- Columna 5: Estante -->
          <td>
            <span style="font-weight: 700; color: #0f172a; font-size: 0.95rem;">
              Estante {{ l.estante || '-' }}
            </span>
          </td>

          <!-- Columna 6: Piso / Cara -->
          <td>
            <div style="color: #475569; font-size: 0.85rem; font-weight: 500;">
              Piso {{ l.piso || '-' }} &bull; Cara {{ l.cara || '-' }}
            </div>
          </td>

          <!-- Columna 7: Estado -->
          <td>
            <span :style="{ 
              background: l.estado === 'Disponible' ? '#dcfce7' : '#fee2e2', 
              color: l.estado === 'Disponible' ? '#166534' : '#9f1239', 
              padding: '4px 12px', 
              borderRadius: '20px', 
              fontSize: '0.8rem', 
              fontWeight: '700',
              border: l.estado === 'Disponible' ? '1px solid #bbf7d0' : '1px solid #fecdd3'
            }">
              {{ l.estado }}
            </span>
          </td>

          <!-- Columna 8: Acciones -->
          <td style="white-space: nowrap;">
            <button class="icon-btn edit-btn" @click="editLibro(l)" title="Editar Libro">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </button>
            <button class="icon-btn delete-btn" @click="deleteLibro(l.libroID)" title="Eliminar Libro">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Paginación -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 1.5rem; background: white; padding: 1rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);">
      <span style="color: #64748b; font-size: 0.875rem; font-weight: 500;">
        Mostrando {{ (currentPage - 1) * pageSize + 1 }} a {{ Math.min(currentPage * pageSize, filteredLibros.length) }} de {{ filteredLibros.length }} registros
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
      <div class="modal-card">
        <!-- Header -->
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
            </div>
            <h3>{{ currentLibro.libroID ? 'Editar Libro' : 'Nuevo Libro' }}</h3>
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

          <div class="modal-body-layout-wide">
            <!-- Columna 1: Info Principal -->
            <div class="modal-form-column">
              <div class="input-group">
                <label :class="{ 'error-label': errors.titulo }">Título *</label>
                <input v-model="currentLibro.titulo" placeholder="Título del libro" class="premium-input" :class="{ 'invalid': errors.titulo }">
              </div>
              
              <div class="input-group">
                <label :class="{ 'error-label': errors.autor }">Autor *</label>
                <input v-model="currentLibro.autor" placeholder="Autor principal" class="premium-input" :class="{ 'invalid': errors.autor }">
              </div>

              <div class="form-grid-2">
                <div class="input-group">
                  <label :class="{ 'error-label': errors.nroRegistro }">N° Registro *</label>
                  <input v-model="currentLibro.nroRegistro" placeholder="Ej. REG-00234" class="premium-input" :class="{ 'invalid': errors.nroRegistro }">
                </div>
                <div class="input-group">
                  <label :class="{ 'error-label': errors.codigoBarras }">Código de Barras *</label>
                  <input v-model="currentLibro.codigoBarras" placeholder="Escanear o ingresar número" class="premium-input" :class="{ 'invalid': errors.codigoBarras }">
                </div>
              </div>

              <div class="form-grid-2">
                <div class="input-group">
                  <label>N° Clasificación</label>
                  <input v-model="currentLibro.nroClasificacion" placeholder="Ej. WB 100 G216 2021" class="premium-input">
                </div>
                <div class="input-group">
                  <label>Año</label>
                  <input v-model="currentLibro.anio" placeholder="Ej. 2021" class="premium-input">
                </div>
              </div>

              <div class="form-grid-2">
                <div class="input-group">
                  <label>Editorial</label>
                  <input v-model="currentLibro.editorial" placeholder="Editorial" class="premium-input">
                </div>
                <div class="input-group">
                  <label>Edición</label>
                  <input v-model="currentLibro.edicion" placeholder="Ej. 3ra Edición" class="premium-input">
                </div>
              </div>
            </div>

            <!-- Columna 2: Detalles y Ubicación -->
            <div class="modal-form-column">
              <div class="form-grid-2">
                <div class="input-group">
                  <label>Categoría</label>
                  <select v-model="currentLibro.categoria" class="premium-input">
                    <option value="Medicina General">Medicina General</option>
                    <option value="Anatomía">Anatomía</option>
                    <option value="Fisiología">Fisiología</option>
                    <option value="Farmacología">Farmacología</option>
                    <option value="Microbiología">Microbiología</option>
                    <option value="Patología">Patología</option>
                    <option value="Cirugía">Cirugía</option>
                    <option value="Otros">Otros</option>
                  </select>
                </div>
                <div class="input-group">
                  <label>Idioma</label>
                  <input v-model="currentLibro.idioma" placeholder="Ej. Español" class="premium-input">
                </div>
              </div>

              <div class="form-grid-2">
                <div class="input-group">
                  <label>Páginas</label>
                  <input type="number" v-model="currentLibro.paginas" placeholder="0" class="premium-input">
                </div>
                <div class="input-group">
                  <label>Estado</label>
                  <select v-model="currentLibro.estado" class="premium-input">
                    <option value="Disponible">Disponible</option>
                    <option value="Prestado">Prestado</option>
                    <option value="Mantenimiento">Mantenimiento</option>
                  </select>
                </div>
              </div>

              <div class="input-group">
                <label>Resumen / Descripción</label>
                <textarea v-model="currentLibro.resumen" placeholder="Breve descripción del contenido" class="premium-input" style="height: 72px; resize: none;"></textarea>
              </div>

              <!-- Ubicación Física -->
              <h4 style="margin: 10px 0 0; color: #334155; font-size: 0.9rem;">Ubicación Física</h4>
              <div class="form-grid-3">
                <div class="input-group">
                  <label>Estante</label>
                  <input type="number" min="1" max="31" step="1" v-model="currentLibro.estante" placeholder="1-31" class="premium-input" oninput="if(this.value){ this.value = Math.round(this.value); if(this.value > 31) this.value = 31; }">
                </div>
                <div class="input-group">
                  <label>Cara</label>
                  <select v-model="currentLibro.cara" class="premium-input">
                    <option value="">N/A</option>
                    <option value="A">A</option>
                    <option value="B">B</option>
                  </select>
                </div>
                <div class="input-group">
                  <label>Piso</label>
                  <input type="number" min="1" max="6" v-model="currentLibro.piso" placeholder="1-6" class="premium-input">
                </div>
              </div>
            </div>

            <!-- Columna 3: Portada -->
            <div class="modal-cover-column">
              <label style="font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; display: block; margin-bottom: 6px; text-align: center;">Portada del Libro</label>
              <div class="cover-upload-container">
                <div class="book-cover-preview-lg">
                  <img v-if="currentLibro.portada" :src="currentLibro.portada.startsWith('data:') ? currentLibro.portada : API_BASE_URL + currentLibro.portada" alt="Vista previa de portada" />
                  <div v-else class="book-cover-placeholder-xl">
                    <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#94a3b8" stroke-width="1.5"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
                    <span style="font-size: 0.75rem; color: #94a3b8; margin-top: 8px; font-weight: 500;">Sin Portada</span>
                  </div>
                </div>
                
                <div style="display: flex; flex-direction: column; gap: 0.5rem; width: 100%; align-items: center;">
                  <div style="display: flex; gap: 8px; width: 100%;">
                    <label class="btn btn-secondary" style="margin: 0; padding: 8px 14px; font-size: 0.85rem; cursor: pointer; display: inline-flex; align-items: center; gap: 6px; justify-content: center; flex: 1;">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="17 8 12 3 7 8"></polyline><line x1="12" y1="3" x2="12" y2="15"></line></svg>
                      Subir
                      <input type="file" @change="handlePortadaChange" accept="image/*" style="display: none;">
                    </label>
                    <button v-if="currentLibro.portada" type="button" class="btn btn-danger" @click="removePortada" style="padding: 8px 14px; font-size: 0.85rem; display: inline-flex; align-items: center; gap: 4px; justify-content: center;">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                    </button>
                  </div>
                  <span style="font-size: 0.68rem; color: #94a3b8; text-align: center; margin-top: 4px; line-height: 1.2;">Recomendado 3:4.<br>Máx 1MB.</span>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Footer -->
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Cancelar</button>
          <button class="btn btn-primary premium-btn" @click="saveLibro">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path><polyline points="17 21 17 13 7 13 7 21"></polyline><polyline points="7 3 7 8 15 8"></polyline></svg>
            Guardar Libro
          </button>
        </div>
      </div>
    </div>

    <!-- Image Viewer Modal -->
    <div v-if="showImageModal" class="image-viewer-backdrop" @click="showImageModal = false">
      <button class="close-viewer-btn" @click="showImageModal = false">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
      </button>
      <img :src="selectedImage" class="viewer-img" @click.stop />
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
  max-width: 1100px;
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

.modal-icon {
  background: #e0e7ff;
  color: #4f46e5;
  width: 40px; height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-header h3 {
  margin: 0;
  color: #0f172a;
  font-size: 1.25rem;
  font-weight: 800;
}

.close-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 8px;
  border-radius: 50%;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}
.close-btn:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.modal-body {
  padding: 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  overflow-y: auto;
}

.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
}

.form-grid-3 {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 1.25rem;
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.input-group label {
  font-size: 0.75rem;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: left;
}

/* Toast Notifications */
.toast-container {
  position: fixed;
  bottom: 2rem;
  right: 2rem;
  background: white;
  padding: 1rem 1.5rem;
  border-radius: 12px;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
  display: flex;
  align-items: center;
  gap: 12px;
  transform: translateY(150%);
  opacity: 0;
  transition: all 0.4s cubic-bezier(0.68, -0.55, 0.265, 1.55);
  z-index: 10000;
  border-left: 5px solid transparent;
}

.toast-show {
  transform: translateY(0);
  opacity: 1;
}

.toast-success {
  border-left-color: #10b981;
}
.toast-success .toast-icon {
  color: #10b981;
  background: #d1fae5;
  padding: 6px;
  border-radius: 50%;
  display: flex;
}

.toast-error {
  border-left-color: #ef4444;
}
.toast-error .toast-icon {
  color: #ef4444;
  background: #fee2e2;
  padding: 6px;
  border-radius: 50%;
  display: flex;
}

.toast-message {
  color: #1f2937;
  font-weight: 600;
  font-size: 0.95rem;
}

.premium-input {
  padding: 12px 16px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  color: #1e293b;
  font-size: 0.95rem;
  font-family: inherit;
  transition: all 0.2s;
  width: 100%;
  box-sizing: border-box;
}

.premium-input:focus {
  outline: none;
  border-color: #9f1239;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(159, 18, 57, 0.1);
}

.premium-input::placeholder {
  color: #94a3b8;
}

.modal-footer {
  padding: 1.25rem 2rem;
  border-top: 1px solid #f1f5f9;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  background: #f8fafc;
}

.btn-secondary {
  background: #ffffff;
  color: #475569;
  border: 1px solid #cbd5e1;
  font-weight: 600;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
}
.btn-secondary:hover {
  background: #f1f5f9;
}

.premium-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 24px;
  font-weight: 700;
  background: #9f1239;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  box-shadow: 0 4px 6px -1px rgba(159, 18, 57, 0.3);
  transition: all 0.2s;
}
.premium-btn:hover {
  background: #881337;
  transform: translateY(-1px);
  box-shadow: 0 6px 8px -1px rgba(159, 18, 57, 0.4);
}

@keyframes fadeIn {
  from { opacity: 0; backdrop-filter: blur(0px); }
  to { opacity: 1; backdrop-filter: blur(8px); }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(20px) scale(0.95); }
  to { opacity: 1; transform: translateY(0) scale(1); }
}

.premium-input.invalid {
  border-color: #ef4444 !important;
  background: #fef2f2 !important;
}

.premium-input.invalid:focus {
  box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.1) !important;
}

.error-label {
  color: #ef4444 !important;
}

.validation-banner {
  background: #fef2f2;
  border: 1px solid #fee2e2;
  color: #991b1b;
  padding: 12px 16px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 0.875rem;
  font-weight: 600;
  text-align: left;
  animation: shake 0.4s ease-in-out;
  margin-bottom: 1rem;
}

@keyframes shake {
  0%, 100% { transform: translateX(0); }
  25% { transform: translateX(-4px); }
  75% { transform: translateX(4px); }
}

/* Portada Styles */
.book-cover-thumbnail {
  width: 32px;
  height: 44px;
  border-radius: 4px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f8fafc;
}

.book-cover-thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.book-cover-placeholder {
  color: #94a3b8;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-body-layout-wide {
  display: grid;
  grid-template-columns: 1.2fr 1fr 240px;
  gap: 2rem;
  align-items: start;
}

.modal-form-column {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.modal-cover-column {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.cover-upload-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.25rem;
  background: #f8fafc;
  padding: 1.5rem;
  border-radius: 16px;
  border: 1px dashed #cbd5e1;
  box-sizing: border-box;
  justify-content: center;
  flex: 1;
}

.book-cover-preview-lg {
  width: 140px;
  height: 187px; /* Proporción 3:4 */
  border-radius: 10px;
  border: 1px solid #cbd5e1;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  background: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.3s;
}

.book-cover-preview-lg img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.book-cover-placeholder-xl {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

@media (max-width: 768px) {
  .modal-body-layout {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }
}

/* Image Viewer Modal */
.image-viewer-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.85);
  backdrop-filter: blur(5px);
  z-index: 10000;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: zoom-out;
}

.viewer-img {
  max-width: 90vw;
  max-height: 90vh;
  object-fit: contain;
  border-radius: 8px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
  animation: zoomIn 0.3s ease-out;
}

.close-viewer-btn {
  position: absolute;
  top: 20px;
  right: 20px;
  background: rgba(255, 255, 255, 0.1);
  border: none;
  color: white;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
  z-index: 10001;
}

.close-viewer-btn:hover {
  background: rgba(255, 255, 255, 0.25);
  transform: scale(1.1);
}

@keyframes zoomIn {
  from { transform: scale(0.9); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}
</style>
