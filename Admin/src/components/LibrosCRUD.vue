<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'
import { API_BASE_URL } from '../config'

// Estado del componente
const libros = ref([])
const categorias = ref([])
const searchQuery = ref('')
const showModal = ref(false)
const showImportModal = ref(false)
const importPreviewData = ref([])
const currentFileName = ref('')
const fileInput = ref(null)
const importLoading = ref(false)
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
  categoria: '',
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
  nroRegistro: '',
  codigoBarras: '',
  titulo: '',
  autor: '',
  anio: '',
  paginas: '',
  estante: '',
  piso: ''
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

const loadCategorias = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/Categorias`)
    categorias.value = res.data
    // Si no hay categoría seleccionada y hay categorías cargadas, establecer la primera por defecto
    if (!currentLibro.value.categoria && res.data.length > 0) {
      currentLibro.value.categoria = res.data[0].nombre
    }
  } catch (err) {
    console.error("Error al cargar categorías:", err)
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
      nroRegistro: '',
      codigoBarras: '',
      titulo: '',
      autor: '',
      anio: '',
      paginas: '',
      estante: '',
      piso: ''
    }
    validationError.value = ''
  }
})

const saveLibro = async () => {
  errors.value = {
    nroRegistro: '',
    codigoBarras: '',
    titulo: '',
    autor: '',
    anio: '',
    paginas: '',
    estante: '',
    piso: ''
  }
  validationError.value = ''

  let hasErrors = false

  if (!currentLibro.value.titulo?.trim()) {
    errors.value.titulo = 'El título es obligatorio.'
    hasErrors = true
  }
  if (!currentLibro.value.autor?.trim()) {
    errors.value.autor = 'El autor es obligatorio.'
    hasErrors = true
  }
  if (!currentLibro.value.nroRegistro?.trim()) {
    errors.value.nroRegistro = 'El número de registro es obligatorio.'
    hasErrors = true
  }
  if (!currentLibro.value.codigoBarras?.trim()) {
    errors.value.codigoBarras = 'El código de barras es obligatorio.'
    hasErrors = true
  }

  // Validar Año (debe ser numérico y estar en un rango razonable, por ej. 1500 a año actual + 2)
  if (currentLibro.value.anio) {
    const anioNum = Number(currentLibro.value.anio)
    const currentYear = new Date().getFullYear()
    if (isNaN(anioNum) || anioNum < 1500 || anioNum > currentYear + 2) {
      errors.value.anio = `El año debe ser un número válido entre 1500 y ${currentYear + 2}.`
      hasErrors = true
    }
  }

  // Validar Páginas
  if (currentLibro.value.paginas !== undefined && currentLibro.value.paginas !== null && currentLibro.value.paginas !== '') {
    const paginasNum = Number(currentLibro.value.paginas)
    if (isNaN(paginasNum) || paginasNum < 0 || !Number.isInteger(paginasNum)) {
      errors.value.paginas = 'Las páginas deben ser un número entero mayor o igual a 0.'
      hasErrors = true
    }
  }

  // Validar Estante
  if (currentLibro.value.estante !== undefined && currentLibro.value.estante !== null && currentLibro.value.estante !== '') {
    const estanteNum = Number(currentLibro.value.estante)
    if (isNaN(estanteNum) || estanteNum < 1 || estanteNum > 31) {
      errors.value.estante = 'El estante debe estar entre 1 y 31.'
      hasErrors = true
    }
  }

  // Validar Piso
  if (currentLibro.value.piso !== undefined && currentLibro.value.piso !== null && currentLibro.value.piso !== '') {
    const pisoNum = Number(currentLibro.value.piso)
    if (isNaN(pisoNum) || pisoNum < 1 || pisoNum > 6) {
      errors.value.piso = 'El piso debe estar entre 1 y 6.'
      hasErrors = true
    }
  }

  if (hasErrors) {
    validationError.value = 'Por favor, corrija los campos marcados en rojo con sus respectivos errores.'
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

// Mapa Selector logic
const showMapSelectorModal = ref(false)
const tempSelectedShelf = ref(null)
const tempSelectedPiso = ref(null)

const estantes = []
estantes.push({ id: 1, left: 830, top: 50, width: 18, height: 170, pisos: 5 })
let currentId = 2
const aislesLeft = [720, 610, 500, 390, 280, 170]
for (const aisleX of aislesLeft) {
  estantes.push({ id: currentId++, left: aisleX, top: 50, width: 18, height: 170, pisos: 6 })
  estantes.push({ id: currentId++, left: aisleX + 22, top: 50, width: 18, height: 170, pisos: 6 })
  estantes.push({ id: currentId++, left: aisleX, top: 230, width: 18, height: 170, pisos: 6 })
  estantes.push({ id: currentId++, left: aisleX + 22, top: 230, width: 18, height: 170, pisos: 6 })
}

const tempSelectedShelfObj = computed(() => {
  return estantes.find(e => e.id === tempSelectedShelf.value)
})

const openMapSelector = () => {
  tempSelectedShelf.value = currentLibro.value.estante ? parseInt(currentLibro.value.estante, 10) : null
  tempSelectedPiso.value = currentLibro.value.piso ? parseInt(currentLibro.value.piso, 10) : null
  showMapSelectorModal.value = true
}

const selectShelf = (shelfId) => {
  tempSelectedShelf.value = shelfId
  // Si cambiamos de estante y el estante nuevo tiene menos pisos que el piso actualmente seleccionado temporalmente, lo reiniciamos
  const newShelfObj = estantes.find(e => e.id === shelfId)
  if (tempSelectedPiso.value && newShelfObj && tempSelectedPiso.value > newShelfObj.pisos) {
    tempSelectedPiso.value = null
  }
}

const selectFloor = (floorNum) => {
  tempSelectedPiso.value = floorNum
}

const confirmMapSelection = () => {
  currentLibro.value.estante = tempSelectedShelf.value
  currentLibro.value.piso = tempSelectedPiso.value
  showMapSelectorModal.value = false
}

const handleExcelUpload = (event) => {
  const file = event.target.files[0]
  if (!file) return

  currentFileName.value = file.name
  const reader = new FileReader()
  reader.onload = (e) => {
    const data = new Uint8Array(e.target.result)
    const workbook = XLSX.read(data, { type: 'array' })
    const firstSheetName = workbook.SheetNames[0]
    const worksheet = workbook.Sheets[firstSheetName]
    const jsonData = XLSX.utils.sheet_to_json(worksheet)

    if (jsonData.length === 0) {
      alert("El archivo está vacío o no tiene el formato correcto.")
      return
    }

    const mappedData = jsonData.map(row => {
      const newObj = {
        nroRegistro: '',
        codigoBarras: '',
        nroClasificacion: '',
        titulo: '',
        autor: '',
        anio: '',
        editorial: '',
        edicion: '',
        categoria: categorias.value[0]?.nombre || 'Medicina General',
        idioma: 'Español',
        paginas: 0,
        estado: 'Disponible',
        resumen: '',
        estante: null,
        cara: '',
        piso: null
      }
      const keys = Object.keys(row)
      
      keys.forEach(k => {
        const key = k.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim()
        const val = row[k] ? String(row[k]).trim() : ''
        
        if (key === 'n registro' || key === 'no registro' || key.includes('nro registro') || key.includes('n registro') || key.includes('n° registro')) 
          newObj.nroRegistro = val
        else if (key === 'codigo de barras' || key === 'codigo barras' || key.includes('barras')) 
          newObj.codigoBarras = val
        else if (key.includes('clasificacion') || key.includes('nlm')) 
          newObj.nroClasificacion = val
        else if (key === 'titulo' || key === 'title') 
          newObj.titulo = val
        else if (key === 'autor' || key === 'author') 
          newObj.autor = val
        else if (key === 'anio' || key === 'ano' || key === 'año') 
          newObj.anio = val
        else if (key === 'editorial' || key === 'publisher') 
          newObj.editorial = val
        else if (key === 'edicion' || key === 'edition') 
          newObj.edicion = val
        else if (key === 'categoria' || key === 'category') 
          newObj.categoria = val
        else if (key === 'idioma' || key === 'language') 
          newObj.idioma = val
        else if (key.includes('pagina') || key.includes('pages')) 
          newObj.paginas = Number(val) || 0
        else if (key === 'estado' || key === 'status') 
          newObj.estado = val || 'Disponible'
        else if (key.includes('resumen') || key.includes('descripcion') || key.includes('detalles')) 
          newObj.resumen = val
        else if (key === 'estante' || key === 'shelf') 
          newObj.estante = val ? parseInt(val, 10) : null
        else if (key === 'cara' || key === 'side') {
          newObj.cara = (val.toUpperCase() === 'N/A' || val.trim() === '') ? '' : val
        }
        else if (key === 'piso' || key === 'floor') 
          newObj.piso = val ? parseInt(val, 10) : null
      })
      
      return newObj
    })

    importPreviewData.value = mappedData
    showImportModal.value = true
    event.target.value = '' // Reset input
  }
  reader.readAsArrayBuffer(file)
}

const confirmImport = async () => {
  importLoading.value = true
  try {
    const res = await axios.post(`${API_BASE_URL}/api/Libros/bulk`, importPreviewData.value)
    alert(`Importación completada:\n- Procesados: ${res.data.procesados}\n- Insertados: ${res.data.insertados}\n- Omitidos (duplicados): ${res.data.omitidos}`)
    showImportModal.value = false
    await loadLibros()
  } catch (error) {
    console.error("Error en importación de libros", error)
    alert("Ocurrió un error al cargar los libros.")
  } finally {
    importLoading.value = false
  }
}

onMounted(() => {
  loadLibros()
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
        <!-- Botones de Acción -->
        <input type="file" ref="fileInput" @change="handleExcelUpload" accept=".xlsx, .xls" style="display: none;">
        <button class="btn" style="background: #ffffff; color: #16a34a; border: 1px solid #16a34a; font-weight: 700; padding: 0.5rem 1rem; border-radius: 0.5rem; display: flex; align-items: center; gap: 8px;" @click="$refs.fileInput.click()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="12" y1="18" x2="12" y2="12"></line><polyline points="9 15 12 12 15 15"></polyline></svg>
          Cargar Excel
        </button>
        <button class="btn btn-primary" @click="currentLibro = { libroID: null, nroRegistro: '', codigoBarras: '', nroClasificacion: '', titulo: '', autor: '', anio: '', editorial: '', edicion: '', categoria: categorias[0]?.nombre || '', idioma: 'Español', paginas: 0, estado: 'Disponible', resumen: '', portada: '', estante: null, cara: '', piso: null }; showModal = true">Nuevo Libro</button>
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
            <!-- Columna 1: Información General & Editorial -->
            <div class="modal-form-column">
              <!-- Card: Información Principal -->
              <div class="form-section">
                <div class="form-section-title">Información Principal</div>
                <div class="input-group">
                  <label :class="{ 'error-label': errors.titulo }">Título *</label>
                  <input v-model="currentLibro.titulo" placeholder="Título del libro" class="premium-input" :class="{ 'invalid': errors.titulo }">
                  <span v-if="errors.titulo" class="field-error-message">{{ errors.titulo }}</span>
                </div>
                
                <div class="input-group">
                  <label :class="{ 'error-label': errors.autor }">Autor *</label>
                  <input v-model="currentLibro.autor" placeholder="Autor principal" class="premium-input" :class="{ 'invalid': errors.autor }">
                  <span v-if="errors.autor" class="field-error-message">{{ errors.autor }}</span>
                </div>
              </div>

              <!-- Card: Datos Editoriales & Categorías -->
              <div class="form-section">
                <div class="form-section-title">Datos Editoriales & Categorización</div>
                
                <div class="form-grid-2">
                  <div class="input-group">
                    <label>Categoría</label>
                    <select v-model="currentLibro.categoria" class="premium-input">
                      <option v-for="cat in categorias" :key="cat.categoriaID" :value="cat.nombre">
                        {{ cat.nombre }}
                      </option>
                      <option v-if="categorias.length === 0" value="">Sin categorías disponibles</option>
                    </select>
                  </div>
                  <div class="input-group">
                    <label>Idioma</label>
                    <input v-model="currentLibro.idioma" placeholder="Ej. Español" class="premium-input">
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

                <div class="form-grid-2">
                  <div class="input-group">
                    <label :class="{ 'error-label': errors.paginas }">Páginas</label>
                    <input type="number" v-model="currentLibro.paginas" placeholder="0" class="premium-input" :class="{ 'invalid': errors.paginas }">
                    <span v-if="errors.paginas" class="field-error-message">{{ errors.paginas }}</span>
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
              </div>
            </div>

            <!-- Columna 2: Identificación & Ubicación Física -->
            <div class="modal-form-column">
              <!-- Card: Identificación del Catálogo -->
              <div class="form-section">
                <div class="form-section-title">Identificación & Registro</div>
                
                <div class="form-grid-2">
                  <div class="input-group">
                    <label :class="{ 'error-label': errors.nroRegistro }">N° Registro *</label>
                    <input v-model="currentLibro.nroRegistro" placeholder="Ej. REG-00234" class="premium-input" :class="{ 'invalid': errors.nroRegistro }">
                    <span v-if="errors.nroRegistro" class="field-error-message">{{ errors.nroRegistro }}</span>
                  </div>
                  <div class="input-group">
                    <label :class="{ 'error-label': errors.codigoBarras }">Código de Barras *</label>
                    <input v-model="currentLibro.codigoBarras" placeholder="Escanear o ingresar número" class="premium-input" :class="{ 'invalid': errors.codigoBarras }">
                    <span v-if="errors.codigoBarras" class="field-error-message">{{ errors.codigoBarras }}</span>
                  </div>
                </div>

                <div class="form-grid-2">
                  <div class="input-group">
                    <label>N° Clasificación</label>
                    <input v-model="currentLibro.nroClasificacion" placeholder="Ej. WB 100 G216 2021" class="premium-input">
                  </div>
                  <div class="input-group">
                    <label :class="{ 'error-label': errors.anio }">Año</label>
                    <input v-model="currentLibro.anio" placeholder="Ej. 2021" class="premium-input" :class="{ 'invalid': errors.anio }">
                    <span v-if="errors.anio" class="field-error-message">{{ errors.anio }}</span>
                  </div>
                </div>
              </div>

              <!-- Card: Ubicación Física -->
              <div class="form-section">
                <div class="form-section-title">
                  <span>Ubicación en Biblioteca</span>
                  <button type="button" class="btn btn-secondary" style="padding: 4px 10px; font-size: 0.75rem; display: inline-flex; align-items: center; gap: 4px; margin: 0;" @click="openMapSelector">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21"></polygon><line x1="9" y1="3" x2="9" y2="18"></line><line x1="15" y1="6" x2="15" y2="21"></line></svg>
                    Seleccionar en Mapa
                  </button>
                </div>
                
                <div class="form-grid-3">
                  <div class="input-group">
                    <label :class="{ 'error-label': errors.estante }">Estante</label>
                    <input type="number" min="1" max="31" step="1" v-model="currentLibro.estante" placeholder="1-31" class="premium-input" :class="{ 'invalid': errors.estante }" oninput="if(this.value){ this.value = Math.round(this.value); if(this.value > 31) this.value = 31; }">
                    <span v-if="errors.estante" class="field-error-message">{{ errors.estante }}</span>
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
                    <label :class="{ 'error-label': errors.piso }">Piso</label>
                    <input type="number" min="1" max="6" v-model="currentLibro.piso" placeholder="1-6" class="premium-input" :class="{ 'invalid': errors.piso }">
                    <span v-if="errors.piso" class="field-error-message">{{ errors.piso }}</span>
                  </div>
                </div>
              </div>

              <!-- Card: Resumen / Descripción -->
              <div class="form-section">
                <div class="form-section-title">Resumen / Descripción</div>
                <div class="input-group">
                  <textarea v-model="currentLibro.resumen" placeholder="Breve descripción del contenido" class="premium-input" style="height: 60px; resize: none;"></textarea>
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


    <!-- Modal de Selección en Mapa -->
    <div v-if="showMapSelectorModal" class="map-selector-backdrop" @click.self="showMapSelectorModal = false">
      <div class="map-selector-card">
        <div class="map-selector-header">
          <div style="display: flex; align-items: center; gap: 10px;">
            <div class="modal-icon" style="background: #fdf2f8; color: #9f1239; width: 36px; height: 36px; border-radius: 8px;">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21"></polygon></svg>
            </div>
            <h3 style="margin: 0; font-size: 1.15rem; font-weight: 800; color: #0f172a;">Seleccionar Ubicación en Mapa 3D</h3>
          </div>
          <button class="close-btn" @click="showMapSelectorModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>
        
        <div class="map-selector-body">
          <!-- Left side: floor selector -->
          <div class="map-selector-left">
            <div class="selected-shelf-info">
              <h4>{{ tempSelectedShelf ? `ESTANTE ${tempSelectedShelf}` : 'Seleccione un estante' }}</h4>
              <p>Haga clic en un estante a la derecha en el mapa 3D y elija el piso aquí.</p>
            </div>
            
            <div class="shelf-vertical-preview" v-if="tempSelectedShelf">
              <div v-for="piso in (tempSelectedShelfObj?.pisos || 6)" :key="piso" 
                   class="floor-row" 
                   :class="{ 'selected-floor': tempSelectedPiso === ((tempSelectedShelfObj?.pisos || 6) + 1 - piso) }"
                   @click="selectFloor((tempSelectedShelfObj?.pisos || 6) + 1 - piso)">
                <div class="floor-shelf-line">
                  <div class="floor-books">
                    <div class="book-spine" v-for="n in 8" :key="n" :style="{ height: 12 + (n % 3) * 4 + 'px', background: `hsl(${(n * 45) % 360}, 45%, 65%)` }"></div>
                  </div>
                </div>
                <div class="floor-number" :class="{ 'selected-floor-text': tempSelectedPiso === ((tempSelectedShelfObj?.pisos || 6) + 1 - piso) }">PISO {{ (tempSelectedShelfObj?.pisos || 6) + 1 - piso }}</div>
              </div>
            </div>
            <div v-else class="select-shelf-placeholder">
              <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="#94a3b8" stroke-width="1.5"><polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21"></polygon></svg>
              <span>Por favor, seleccione un estante en el mapa 3D primero</span>
            </div>
          </div>

          <!-- Right side: 3D interactive map -->
          <div class="map-selector-right">
            <div class="map-scene-wrapper">
              <div class="isometric-scene-mini">
                <div class="wood-floor-mini">
                  <div v-for="estante in estantes" :key="estante.id"
                       class="iso-shelf-mini"
                       :class="{ 'is-selected-shelf': tempSelectedShelf === estante.id }"
                       :style="{ left: (estante.left * 0.70) + 'px', top: (estante.top * 0.70) + 'px', width: (estante.width * 0.70) + 'px', height: (estante.height * 0.70) + 'px' }"
                       @click="selectShelf(estante.id)">
                    <div class="iso-face-mini top-mini">
                      <span>{{ estante.id }}</span>
                    </div>
                    <div class="iso-face-mini front-mini"></div>
                    <div class="iso-face-mini left-mini"></div>
                    <div class="iso-face-mini right-mini"></div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="map-selector-footer">
          <div class="current-selection-summary">
            Ubicación seleccionada: Estante <strong>{{ tempSelectedShelf || '-' }}</strong> &bull; Piso <strong>{{ tempSelectedPiso || '-' }}</strong>
          </div>
          <div style="display: flex; gap: 0.75rem;">
            <button class="btn btn-secondary" @click="showMapSelectorModal = false">Cancelar</button>
            <button class="btn btn-primary" :disabled="!tempSelectedShelf || !tempSelectedPiso" @click="confirmMapSelection">Confirmar Ubicación</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal Vista Previa Excel -->
    <div v-if="showImportModal" class="modal-backdrop" @click.self="showImportModal = false">
      <div class="modal-card" style="max-width: 1100px;">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon" style="background: #dcfce7; color: #16a34a;">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="12" y1="18" x2="12" y2="12"></line><polyline points="9 15 12 12 15 15"></polyline></svg>
            </div>
            <div>
              <h3 style="margin: 0; line-height: 1;">Vista Previa de Importación de Libros</h3>
              <span style="font-size: 0.8rem; color: #16a34a; font-weight: 600;">Archivo: {{ currentFileName }}</span>
            </div>
          </div>
          <button class="close-btn" @click="showImportModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>
        
        <div class="modal-body" style="max-height: 70vh; overflow-y: auto; padding: 1.5rem;">
          <div style="background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 10px; padding: 1rem; display: flex; gap: 12px; align-items: flex-start; margin-bottom: 1rem;">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#0ea5e9" stroke-width="2.5" style="flex-shrink: 0;"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
            <div>
              <div style="color: #0369a1; font-weight: 700; font-size: 0.85rem; margin-bottom: 4px;">RECOMENDACIÓN DE COLUMNAS</div>
              <p style="color: #0c4a6e; font-size: 0.8rem; margin: 0; line-height: 1.4;">
                Para una carga correcta, el Excel debe poseer columnas con cabeceras similares a: 
                <strong style="color: #0369a1;">Título, Autor, N° Registro, Código de Barras, Clasificación, Año, Categoría, Idioma, Editorial, Edición, Páginas, Resumen, Estante, Cara, Piso.</strong>
              </p>
            </div>
          </div>

          <p style="color: #64748b; font-size: 0.875rem; margin-bottom: 1rem;">Se han detectado <strong>{{ importPreviewData.length }}</strong> registros de libros. Revise que la información mapeada sea la adecuada antes de proceder a la carga masiva.</p>
          
          <table class="centered-table" style="font-size: 0.75rem;">
            <thead>
              <tr style="background: #f8fafc;">
                <th>N°</th>
                <th>N° Registro</th>
                <th>Código Barras</th>
                <th>Título</th>
                <th>Autor</th>
                <th>Clasificación</th>
                <th>Categoría</th>
                <th>Editorial / Año</th>
                <th>Ubicación</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, idx) in importPreviewData" :key="idx">
                <td style="color: #94a3b8;">{{ idx + 1 }}</td>
                <td style="font-weight: 700;">{{ row.nroRegistro || '-' }}</td>
                <td>{{ row.codigoBarras || '-' }}</td>
                <td style="text-align: left; font-weight: 600; max-width: 200px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">{{ row.titulo || '-' }}</td>
                <td style="text-align: left; max-width: 150px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">{{ row.autor || '-' }}</td>
                <td>{{ row.nroClasificacion || '-' }}</td>
                <td><span style="background: #eff6ff; color: #1d4ed8; padding: 2px 6px; border-radius: 4px; font-weight: 600;">{{ row.categoria }}</span></td>
                <td>{{ row.editorial || '-' }} ({{ row.anio || '-' }})</td>
                <td>Estante {{ row.estante || '-' }} - P.{{ row.piso || '-' }} - C.{{ row.cara || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="modal-footer">
          <span style="margin-right: auto; color: #64748b; font-size: 0.85rem; display: flex; align-items: center; gap: 5px;">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
            Los duplicados por N° Registro o Código de Barras se omitirán automáticamente.
          </span>
          <button class="btn btn-secondary" @click="showImportModal = false">Cancelar</button>
          <button class="btn premium-btn" style="background: #16a34a; box-shadow: 0 4px 6px -1px rgba(22, 163, 74, 0.3);" @click="confirmImport" :disabled="importLoading">
            {{ importLoading ? 'Procesando...' : 'Procesar e Importar' }}
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
.form-section {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.form-section-title {
  font-size: 0.85rem;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin: 0 0 0.5rem 0;
  border-bottom: 1px solid #f1f5f9;
  padding-bottom: 0.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.field-error-message {
  font-size: 0.72rem;
  color: #ef4444;
  margin-top: 2px;
  font-weight: 600;
  text-align: left;
  line-height: 1.2;
}

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
  max-width: 1250px;
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
  grid-template-columns: 1fr 1fr 260px;
  gap: 1.5rem;
  align-items: start;
}

.modal-form-column {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.modal-cover-column {
  display: flex;
  flex-direction: column;
  height: auto;
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
  height: auto;
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

/* Map Selector Modal CSS */
.map-selector-backdrop {
  position: fixed;
  top: 0; left: 0; width: 100%; height: 100%;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1100;
  animation: fadeIn 0.2s ease-out;
}

.map-selector-card {
  width: 90%;
  max-width: 980px;
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.map-selector-header {
  padding: 1.25rem 1.75rem;
  border-bottom: 1px solid #f1f5f9;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #f8fafc;
}

.map-selector-body {
  display: grid;
  grid-template-columns: 280px 1fr;
  padding: 1.5rem;
  gap: 1.5rem;
  height: 480px;
}

.map-selector-left {
  border-right: 1px solid #f1f5f9;
  padding-right: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  overflow-y: auto;
}

.selected-shelf-info h4 {
  margin: 0 0 4px 0;
  color: #0f172a;
  font-size: 1.1rem;
  font-weight: 800;
}

.selected-shelf-info p {
  margin: 0;
  color: #64748b;
  font-size: 0.8rem;
  line-height: 1.3;
}

.shelf-vertical-preview {
  display: flex;
  flex-direction: column;
  gap: 8px;
  background: #fafafa;
  padding: 10px;
  border-radius: 12px;
  border: 1px solid #f1f5f9;
}

.floor-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.floor-row:hover {
  border-color: #9f1239;
  background: #fff1f2;
}

.floor-row.selected-floor {
  background: #9f1239;
  border-color: #9f1239;
}

.floor-shelf-line {
  flex: 1;
  display: flex;
  align-items: flex-end;
  height: 24px;
  border-bottom: 2px solid #cbd5e1;
  margin-right: 12px;
  padding-bottom: 1px;
}

.floor-books {
  display: flex;
  align-items: flex-end;
  gap: 1px;
  height: 100%;
}

.floor-row.selected-floor .floor-shelf-line {
  border-bottom-color: rgba(255, 255, 255, 0.4);
}

.book-spine {
  width: 5px;
  border-radius: 1px 1px 0 0;
  opacity: 0.85;
}

.floor-number {
  font-size: 0.8rem;
  font-weight: 700;
  color: #475569;
}

.floor-number.selected-floor-text {
  color: white;
}

.select-shelf-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #94a3b8;
  font-size: 0.85rem;
  text-align: center;
  gap: 12px;
  padding: 2rem;
}

.map-selector-right {
  display: flex;
  justify-content: center;
  align-items: center;
  background: #f8fafc;
  border-radius: 16px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  position: relative;
}

.map-scene-wrapper {
  transform: scale(0.9);
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
}

.isometric-scene-mini {
  width: 100%;
  height: 100%;
  perspective: 2000px;
  display: flex;
  justify-content: center;
  align-items: center;
}

.wood-floor-mini {
  width: 638px; /* 850 * 0.75 scaled down slightly to fit */
  height: 450px;
  background-color: #d1bfae;
  background-image: 
    repeating-linear-gradient(45deg, #c3b09e 25%, transparent 25%, transparent 75%, #c3b09e 75%, #c3b09e), 
    repeating-linear-gradient(45deg, #c3b09e 25%, #d1bfae 25%, #d1bfae 75%, #c3b09e 75%, #c3b09e);
  background-position: 0 0, 11px 11px;
  background-size: 22px 22px;
  transform: rotateX(60deg) rotateZ(-35deg);
  transform-style: preserve-3d;
  position: relative;
  border: 1px solid #111;
  box-shadow: -1px 1px 0 #111, -2px 2px 0 #111, -3px 3px 10px rgba(0,0,0,0.3);
}

.iso-shelf-mini {
  position: absolute;
  transform-style: preserve-3d;
  cursor: pointer;
  transition: transform 0.2s;
}

.iso-shelf-mini:hover {
  transform: translateZ(5px);
}

.iso-shelf-mini.is-selected-shelf {
  transform: translateZ(12px);
}

.iso-face-mini {
  position: absolute;
  background: #8B5A2B;
  border: 1px solid #5C3A21;
  backface-visibility: hidden;
}

.iso-face-mini.top-mini {
  width: 100%; height: 100%;
  transform: translateZ(60px);
  background: #A06E3D;
  display: flex;
  justify-content: center;
  align-items: center;
  color: #3E2723;
  font-weight: 900;
  font-size: 0.65rem;
}

.iso-shelf-mini.is-selected-shelf .iso-face-mini.top-mini {
  background: #9f1239;
  color: white;
  border-color: #9f1239;
}

.iso-face-mini.front-mini {
  bottom: 0; left: 0;
  width: 100%; height: 60px;
  transform-origin: bottom;
  transform: rotateX(-90deg);
  background: #794D24;
}
.iso-shelf-mini.is-selected-shelf .iso-face-mini.front-mini {
  background: #881337;
}

.iso-face-mini.left-mini {
  top: 0; left: 0;
  width: 60px; height: 100%;
  transform-origin: left;
  transform: rotateY(-90deg);
  background: #2E1B10;
}

.iso-face-mini.right-mini {
  top: 0; right: 0;
  width: 60px; height: 100%;
  transform-origin: right;
  transform: rotateY(90deg);
  background: #2E1B10;
}

.map-selector-footer {
  padding: 1rem 1.75rem;
  border-top: 1px solid #f1f5f9;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #f8fafc;
}

.current-selection-summary {
  font-size: 0.85rem;
  color: #475569;
}
.current-selection-summary strong {
  color: #9f1239;
  font-size: 0.95rem;
}
</style>
