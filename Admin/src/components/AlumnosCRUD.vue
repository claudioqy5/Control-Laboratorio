<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'
import { API_BASE_URL } from '../config'
import CameraScanner from './CameraScanner.vue'

const alumnos = ref([])
const searchQuery = ref('')
const statusFilter = ref('Todos')
const showModal = ref(false)
const showImportModal = ref(false)
const showCameraScanner = ref(false)
const importPreviewData = ref([])
const currentFileName = ref('')
const fileInput = ref(null)
const importLoading = ref(false)
const currentAlumno = ref({ 
  codigoUniversitario: '', 
  dni: '', 
  nombres: '', 
  apellidoPaterno: '', 
  apellidoMaterno: '', 
  carrera: '', 
  telefono: '',
  correoInstitucional: '',
  correoPersonal: '',
  estado: true 
})

const toast = ref({ show: false, message: '', type: 'success' })

const showToast = (message, type = 'success') => {
  toast.value = { show: true, message, type }
  setTimeout(() => {
    toast.value.show = false
  }, 4000)
}

const errors = ref({
  codigoUniversitario: false,
  dni: false,
  nombres: false,
  apellidoPaterno: false,
  apellidoMaterno: false,
  carrera: false
})
const validationError = ref('')

// ── Lógica de escaneo de código de barras ─────────────────────────────────────────────
/**
 * Se dispara cuando el usuario presiona Enter en el campo de búsqueda.
 * Esto ocurre automáticamente cuando se escanea un código de barras
 * (la lectora escribe el código y envía Enter al final).
 *
 * Flujo:
 *   1. Busca el código en la base de datos local.
 *   2. Si EXISTE → el filtro ya lo muestra; sale.
 *   3. Si NO EXISTE → abre el modal vacío pre-rellenado con el código.
 */
const buscarPorEscaneo = async () => {
  const codigo = searchQuery.value?.trim()
  if (!codigo) return

  // ¿Ya existe en el sistema local?
  const existe = alumnos.value.some(
    a => a.codigoUniversitario?.toLowerCase() === codigo.toLowerCase()
  )
  if (existe) {
    // El filtro del buscador ya lo muestra → no hacer nada más
    return
  }

  // No existe → abrir modal vacío con el código
  currentAlumno.value = {
    codigoUniversitario: codigo,
    dni: '', nombres: '', apellidoPaterno: '', apellidoMaterno: '',
    carrera: '', telefono: '', correoInstitucional: '', correoPersonal: '',
    estado: true
  }
  showModal.value = true
}

const handleCameraScan = async (data) => {
  // Autogenerar correo si tenemos el código
  let correoInst = '';
  if (data.codigoUniversitario) {
    correoInst = `${data.codigoUniversitario}@urp.edu.pe`;
  }
  
  currentAlumno.value = {
    codigoUniversitario: data.codigoUniversitario || '',
    dni: data.dni || '',
    nombres: data.nombres || '',
    apellidoPaterno: data.apellidoPaterno || '',   // ← usa el campo separado del backend
    apellidoMaterno: data.apellidoMaterno || '',   // ← usa el campo separado del backend
    carrera: data.carrera || 'Medicina Humana',    // ← por defecto Medicina Humana
    telefono: '',
    correoInstitucional: correoInst,
    correoPersonal: '',
    estado: true
  };
  
  showCameraScanner.value = false;
  
  // Abrir modal para confirmación
  showModal.value = true;
}
// ──────────────────────────────────────────────────────────────────────────────────────

watch(showModal, (newVal) => {
  if (!newVal) {
    errors.value = {
      codigoUniversitario: false,
      dni: false,
      nombres: false,
      apellidoPaterno: false,
      apellidoMaterno: false,
      carrera: false
    }
    validationError.value = ''
  }
})

const fetchAlumnos = async () => {
  const res = await axios.get(`${API_BASE_URL}/api/alumnos`)
  alumnos.value = res.data
}

const filteredAlumnos = computed(() => {
  let result = alumnos.value

  if (statusFilter.value !== 'Todos') {
    const isActive = statusFilter.value === 'Activos'
    result = result.filter(a => a.estado === isActive)
  }

  if (searchQuery.value) {
    const queryWords = searchQuery.value.toLowerCase().split(/\s+/).filter(Boolean)
    result = result.filter(a => {
      const studentString = `${a.nombres} ${a.apellidoPaterno} ${a.apellidoMaterno} ${a.codigoUniversitario} ${a.dni || ''}`.toLowerCase()
      return queryWords.every(word => studentString.includes(word))
    })
  }
  return result
})

const currentPage = ref(1)
const pageSize = 10

const totalPages = computed(() => {
  return Math.ceil(filteredAlumnos.value.length / pageSize) || 1
})

const paginatedAlumnos = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  const end = start + pageSize
  return filteredAlumnos.value.slice(start, end)
})

const nextPage = () => {
  if (currentPage.value < totalPages.value) currentPage.value++
}

const prevPage = () => {
  if (currentPage.value > 1) currentPage.value--
}

const saveAlumno = async () => {
  errors.value = {
    codigoUniversitario: false,
    dni: false,
    nombres: false,
    apellidoPaterno: false,
    apellidoMaterno: false,
    carrera: false
  }
  validationError.value = ''

  const missingFields = []
  if (!currentAlumno.value.codigoUniversitario?.trim()) {
    errors.value.codigoUniversitario = true
    missingFields.push('Código Universitario')
  }
  if (!currentAlumno.value.dni?.trim()) {
    errors.value.dni = true
    missingFields.push('DNI')
  }
  if (!currentAlumno.value.nombres?.trim()) {
    errors.value.nombres = true
    missingFields.push('Nombres')
  }
  if (!currentAlumno.value.apellidoPaterno?.trim()) {
    errors.value.apellidoPaterno = true
    missingFields.push('Apellido Paterno')
  }
  if (!currentAlumno.value.apellidoMaterno?.trim()) {
    errors.value.apellidoMaterno = true
    missingFields.push('Apellido Materno')
  }
  if (!currentAlumno.value.carrera || currentAlumno.value.carrera === '') {
    errors.value.carrera = true
    missingFields.push('Carrera / Cargo')
  }

  if (missingFields.length > 0) {
    validationError.value = `Falta ingresar los siguientes datos obligatorios: ${missingFields.join(', ')}.`
    return
  }

  try {
    const isNew = !currentAlumno.value.alumnoID;
    if (currentAlumno.value.alumnoID) {
      await axios.put(`${API_BASE_URL}/api/alumnos/${currentAlumno.value.alumnoID}`, currentAlumno.value)
    } else {
      await axios.post(`${API_BASE_URL}/api/alumnos`, currentAlumno.value)
    }
    showModal.value = false
    showToast(isNew ? 'Alumno registrado exitosamente.' : 'Datos actualizados correctamente.', 'success')
    fetchAlumnos()
  } catch (error) {
    console.error("Error al guardar alumno:", error)
    if (error.response && error.response.status === 409) {
      showToast(error.response.data.message || 'El alumno ya se encuentra registrado.', 'error')
    } else if (error.response && error.response.data && error.response.data.message) {
      validationError.value = `Error del servidor: ${error.response.data.message}`
    } else {
      validationError.value = "Ocurrió un error al guardar los datos en el servidor."
    }
  }
}

const deleteAlumno = async (id) => {
  if (confirm('¿Eliminar registro?')) {
    await axios.delete(`${API_BASE_URL}/api/alumnos/${id}`)
    fetchAlumnos()
  }
}

const editAlumno = (alumno) => {
  currentAlumno.value = { ...alumno }
  showModal.value = true
}

const toggleEstado = async (alumno) => {
  if (confirm(`¿${alumno.estado ? 'Desactivar' : 'Activar'} a ${alumno.nombres}?`)) {
    const updatedAlumno = { ...alumno, estado: !alumno.estado }
    try {
      await axios.put(`${API_BASE_URL}/api/alumnos/${alumno.alumnoID}`, updatedAlumno)
      fetchAlumnos()
    } catch (error) {
      console.error("Error al cambiar estado:", error)
    }
  }
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
      alert("El archivo está vacío.")
      return
    }

    const mappedData = jsonData.map(row => {
      const newObj = { 
        codigoUniversitario: '',
        dni: '',
        nombres: '',
        apellidoPaterno: '',
        apellidoMaterno: '',
        carrera: '',
        telefono: '',
        correoInstitucional: '',
        correoPersonal: '',
        estado: true 
      }
      const keys = Object.keys(row)
      
      keys.forEach(k => {
        const key = k.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim()
        const val = row[k] ? String(row[k]).trim() : ''
        
        if (key.includes('codigo') || key.includes('code') || key === 'id' || key.includes('universitario')) 
          newObj.codigoUniversitario = val
        else if (key.includes('dni') || key.includes('documento') || key === 'doc') 
          newObj.dni = val
        else if (key.includes('nombres') || key === 'nombre' || key === 'name') 
          newObj.nombres = val
        else if (key.includes('paterno') || key.includes('apellido1') || key.includes('last name') || key === 'a. paterno') 
          newObj.apellidoPaterno = val
        else if (key.includes('materno') || key.includes('apellido2') || key === 'a. materno') 
          newObj.apellidoMaterno = val
        else if (key.includes('carrera') || key.includes('especialidad') || key.includes('facultad') || key.includes('cargo')) 
          newObj.carrera = val
        else if (key.includes('telefono') || key.includes('celular') || key.includes('phone')) 
          newObj.telefono = val
        else if (key.includes('institucional') || key === 'correo' || key === 'email') 
          newObj.correoInstitucional = val
        else if (key.includes('personal') || key.includes('correo2')) 
          newObj.correoPersonal = val
      })
      
      // If code is empty, generate a temporary one to avoid Bad Request
      if (!newObj.codigoUniversitario) {
        newObj.codigoUniversitario = "TEMP-" + Math.random().toString(36).substr(2, 6).toUpperCase()
      }
      if (!newObj.dni) newObj.dni = "-"
      if (!newObj.nombres) newObj.nombres = "-"
      if (!newObj.apellidoPaterno) newObj.apellidoPaterno = "-"
      if (!newObj.apellidoMaterno) newObj.apellidoMaterno = "-"
      
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
    const res = await axios.post(`${API_BASE_URL}/api/alumnos/bulk`, importPreviewData.value)
    alert(`Importación completada:\n- Procesados: ${res.data.procesados}\n- Insertados: ${res.data.insertados}\n- Omitidos (duplicados): ${res.data.omitidos}`)
    showImportModal.value = false
    fetchAlumnos()
  } catch (error) {
    console.error("Error en importación", error)
    alert("Ocurrió un error al cargar los datos.")
  } finally {
    importLoading.value = false
  }
}

let barcodeBuffer = ''
let lastKeyTime = 0

const handleGlobalKeydown = (e) => {
  // Evitar interferir si hay algún modal abierto
  if (showModal.value || showImportModal.value) return

  // Si el foco ya está en un campo de texto, dejar que el navegador actúe de manera predeterminada
  if (document.activeElement && (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA')) {
    return
  }

  const currentTime = Date.now()
  
  // Las lectoras de código de barras escriben a una velocidad muy rápida (menor a 50ms por caracter).
  // Si transcurre más de 100ms entre pulsaciones, asumimos que es tipeo manual y reiniciamos el búfer.
  if (currentTime - lastKeyTime > 100) {
    barcodeBuffer = ''
  }
  
  lastKeyTime = currentTime

  if (e.key === 'Enter') {
    if (barcodeBuffer.length >= 3) {
      searchQuery.value = barcodeBuffer.trim()
      currentPage.value = 1
      barcodeBuffer = ''
      e.preventDefault()
      // Disparar la búsqueda/scraping igual que si el usuario presionara Enter en el input
      buscarPorEscaneo()
    }
  } else if (e.key.length === 1) {
    barcodeBuffer += e.key
  }
}

onMounted(() => {
  fetchAlumnos()
  window.addEventListener('keydown', handleGlobalKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)
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

    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Participantes</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">{{ alumnos.length }} Registrados</div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        
        <!-- Filtro por Estado -->
        <div style="position: relative;">
          <select v-model="statusFilter" @change="currentPage = 1" style="padding: 0.5rem 2.5rem 0.5rem 1rem; border-radius: 0.5rem; border: 1px solid #e5e7eb; font-size: 0.875rem; color: #111827; background: white; appearance: none; cursor: pointer; outline: none;">
            <option value="Todos">Todos los Estados</option>
            <option value="Activos">Solo Activos</option>
            <option value="Inactivos">Solo Inactivos</option>
          </select>
          <svg style="position: absolute; right: 10px; top: 50%; transform: translateY(-50%); pointer-events: none; color: #9ca3af;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"></polyline></svg>
        </div>

        <div style="position: relative;">
          <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: #9ca3af;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input
            type="text"
            v-model="searchQuery"
            @input="currentPage = 1"
            @keyup.enter="buscarPorEscaneo"
            placeholder="Buscar o escanear código de barras..."
            :style="{
              padding: '0.5rem 1rem 0.5rem 2rem',
              borderRadius: '0.5rem',
              border: '1px solid #e5e7eb',
              width: '280px',
              fontSize: '0.875rem',
              color: '#111827',
              outline: 'none'
            }"
          >
        </div>
        <button class="btn" style="background: #ffffff; color: #6366f1; border: 1px solid #6366f1; font-weight: 700; padding: 0.5rem 1rem; border-radius: 0.5rem; display: flex; align-items: center; gap: 8px;" @click="showCameraScanner = true">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>
          Usar Cámara
        </button>
        <input type="file" ref="fileInput" @change="handleExcelUpload" accept=".xlsx, .xls" style="display: none;">
        <button class="btn" style="background: #ffffff; color: #16a34a; border: 1px solid #16a34a; font-weight: 700; padding: 0.5rem 1rem; border-radius: 0.5rem; display: flex; align-items: center; gap: 8px;" @click="$refs.fileInput.click()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="12" y1="18" x2="12" y2="12"></line><polyline points="9 15 12 12 15 15"></polyline></svg>
          Cargar Excel
        </button>
        <button class="btn btn-primary" @click="currentAlumno = { estado: true }; showModal = true">Nuevo</button>
      </div>
    </div>

    <table class="centered-table">
      <thead>
        <tr>
          <th>Nº</th>
          <th>Código</th>
          <th>DNI</th>
          <th>Nombres Completos</th>
          <th>Estado</th>
          <th>Correo Institucional</th>
          <th>Sesión Actual</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(a, index) in paginatedAlumnos" :key="a.alumnoID">
          <td style="color: #6b7280; font-weight: 600;">{{ (currentPage - 1) * pageSize + index + 1 }}</td>
          <td><strong style="color: #111827;">{{ a.codigoUniversitario }}</strong></td>
          <td>{{ a.dni }}</td>
          <td>{{ a.nombres }} {{ a.apellidoPaterno }} {{ a.apellidoMaterno }}</td>
          <td>
            <span :style="{ background: a.estado ? '#f0fdf4' : '#fef2f2', color: a.estado ? '#166534' : '#991b1b', padding: '4px 8px', borderRadius: '4px', fontSize: '0.85rem', fontWeight: '600' }">
              {{ a.estado ? 'Activo' : 'Inactivo' }}
            </span>
          </td>
          <td style="color: #0ea5e9;">{{ a.correoInstitucional || '-' }}</td>
          <td>
            <span style="background: #eff6ff; color: #1d4ed8; padding: 4px 8px; border-radius: 4px; font-size: 0.85rem; font-weight: 600; white-space: nowrap;">
              Sesión {{ Math.floor((a.limiteDiarioSegundos || 10800) / 10800) }} ({{ (a.limiteDiarioSegundos || 10800) / 3600 }}h)
            </span>
          </td>
          <td style="white-space: nowrap;">
            <button class="icon-btn" :class="a.estado ? 'suspend-btn' : 'activate-btn'" @click="toggleEstado(a)" :title="a.estado ? 'Desactivar' : 'Activar'">
              <svg v-if="a.estado" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>
              <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
            </button>
            <button class="icon-btn edit-btn" @click="editAlumno(a)" title="Editar">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </button>
            <button class="icon-btn delete-btn" @click="deleteAlumno(a.alumnoID)" title="Eliminar">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Paginación -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 1.5rem; background: white; padding: 1rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);">
      <span style="color: #64748b; font-size: 0.875rem; font-weight: 500;">
        Mostrando {{ (currentPage - 1) * pageSize + 1 }} a {{ Math.min(currentPage * pageSize, filteredAlumnos.length) }} de {{ filteredAlumnos.length }} registros
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
              <svg v-if="currentAlumno.alumnoID" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
              <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="8.5" cy="7" r="4"></circle><line x1="20" y1="8" x2="20" y2="14"></line><line x1="23" y1="11" x2="17" y2="11"></line></svg>
            </div>
            <h3>{{ currentAlumno.alumnoID ? 'Editar Usuario' : 'Nuevo Usuario' }}</h3>
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

          <div class="form-grid-2">
            <div class="input-group">
              <label :class="{ 'error-label': errors.codigoUniversitario }">Código Universitario / ID *</label>
              <input v-model="currentAlumno.codigoUniversitario" placeholder="Ej. 20212345" class="premium-input" :class="{ 'invalid': errors.codigoUniversitario }">
            </div>
            <div class="input-group">
              <label :class="{ 'error-label': errors.dni }">DNI *</label>
              <input v-model="currentAlumno.dni" placeholder="Nro. Documento" class="premium-input" :class="{ 'invalid': errors.dni }">
            </div>
          </div>
          
          <div class="input-group">
            <label :class="{ 'error-label': errors.nombres }">Nombres *</label>
            <input v-model="currentAlumno.nombres" placeholder="Nombres completos" class="premium-input" :class="{ 'invalid': errors.nombres }">
          </div>
          
          <div class="form-grid-2">
            <div class="input-group">
              <label :class="{ 'error-label': errors.apellidoPaterno }">Apellido Paterno *</label>
              <input v-model="currentAlumno.apellidoPaterno" placeholder="Paterno" class="premium-input" :class="{ 'invalid': errors.apellidoPaterno }">
            </div>
            <div class="input-group">
              <label :class="{ 'error-label': errors.apellidoMaterno }">Apellido Materno *</label>
              <input v-model="currentAlumno.apellidoMaterno" placeholder="Materno" class="premium-input" :class="{ 'invalid': errors.apellidoMaterno }">
            </div>
          </div>
          
          <div class="form-grid-2">
            <div class="input-group">
              <label :class="{ 'error-label': errors.carrera }">Carrera / Cargo *</label>
              <select v-model="currentAlumno.carrera" class="premium-input" :class="{ 'invalid': errors.carrera }" style="cursor: pointer;">
                <option value="" disabled>Seleccione una carrera...</option>
                <option value="Ingeniería Civil">Ingeniería Civil</option>
                <option value="Ingeniería Electrónica">Ingeniería Electrónica</option>
                <option value="Ingeniería Industrial">Ingeniería Industrial</option>
                <option value="Ingeniería Informática">Ingeniería Informática</option>
                <option value="Ingeniería Mecatrónica">Ingeniería Mecatrónica</option>
                <option value="Administración y Gerencia">Administración y Gerencia</option>
                <option value="Administración de Negocios Globales">Administración de Negocios Globales</option>
                <option value="Contabilidad y Finanzas">Contabilidad y Finanzas</option>
                <option value="Economía">Economía</option>
                <option value="Marketing Global y Administración Comercial">Marketing Global y Administración Comercial</option>
                <option value="Turismo, Hotelería y Gastronomía">Turismo, Hotelería y Gastronomía</option>
                <option value="Arquitectura y Urbanismo">Arquitectura y Urbanismo</option>
                <option value="Medicina Humana">Medicina Humana</option>
                <option value="Psicología">Psicología</option>
                <option value="Biología">Biología</option>
                <option value="Medicina Veterinaria">Medicina Veterinaria</option>
                <option value="Derecho y Ciencia Política">Derecho y Ciencia Política</option>
                <option value="Traducción e Interpretación">Traducción e Interpretación</option>
              </select>
            </div>
            <div class="input-group">
              <label>Teléfono</label>
              <input v-model="currentAlumno.telefono" placeholder="Opcional" class="premium-input">
            </div>
          </div>

          <div class="form-grid-2">
            <div class="input-group">
              <label>Correo Institucional</label>
              <input v-model="currentAlumno.correoInstitucional" placeholder="usuario@urp.edu.pe" class="premium-input">
            </div>
            <div class="input-group">
              <label>Correo Personal</label>
              <input v-model="currentAlumno.correoPersonal" placeholder="usuario@gmail.com" class="premium-input">
            </div>
          </div>
        </div>
        
        <!-- Footer -->
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Cancelar</button>
          <button class="btn btn-primary premium-btn" @click="saveAlumno">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path><polyline points="17 21 17 13 7 13 7 21"></polyline><polyline points="7 3 7 8 15 8"></polyline></svg>
            Guardar Datos
          </button>
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
              <h3 style="margin: 0; line-height: 1;">Vista Previa de Importación</h3>
              <span style="font-size: 0.8rem; color: #16a34a; font-weight: 600;">Archivo: {{ currentFileName }}</span>
            </div>
          </div>
          <button class="close-btn" @click="showImportModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>
        
        <div class="modal-body" style="max-height: 90vh; overflow-y: auto; padding: 1.5rem;">
          <div style="background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 10px; padding: 1rem; display: flex; gap: 12px; align-items: flex-start;">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#0ea5e9" stroke-width="2.5" style="flex-shrink: 0;"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
            <div>
              <div style="color: #0369a1; font-weight: 700; font-size: 0.85rem; margin-bottom: 4px;">RECOMENDACIÓN DE COLUMNAS</div>
              <p style="color: #0c4a6e; font-size: 0.8rem; margin: 0; line-height: 1.4;">
                Para una carga automática, asegúrate que tu Excel tenga cabeceras similares a estas: 
                <strong style="color: #0369a1;">Código, DNI, Nombres, Apellido Paterno, Apellido Materno, Carrera, Teléfono, Correo Institucional, Correo Personal.</strong>
              </p>
            </div>
          </div>

          <p style="color: #64748b; font-size: 0.875rem; margin-bottom: 1rem;">Se han detectado <strong>{{ importPreviewData.length }}</strong> registros. Revisa que el mapeo de columnas sea correcto antes de procesar.</p>
          
          <table class="centered-table" style="font-size: 0.75rem;">
            <thead>
              <tr style="background: #f8fafc;">
                <th>N°</th>
                <th>Código</th>
                <th>DNI</th>
                <th>Nombres</th>
                <th>A. Paterno</th>
                <th>A. Materno</th>
                <th>Institucional</th>
                <th>Personal</th>
                <th>Carrera</th>
                <th>Teléfono</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, idx) in importPreviewData" :key="idx">
                <td style="color: #94a3b8;">{{ idx + 1 }}</td>
                <td style="font-weight: 700;">{{ row.codigoUniversitario || '-' }}</td>
                <td>{{ row.dni || '-' }}</td>
                <td>{{ row.nombres || '-' }}</td>
                <td>{{ row.apellidoPaterno || '-' }}</td>
                <td>{{ row.apellidoMaterno || '-' }}</td>
                <td style="color: #0ea5e9;">{{ row.correoInstitucional || '-' }}</td>
                <td style="color: #64748b;">{{ row.correoPersonal || '-' }}</td>
                <td>{{ row.carrera || '-' }}</td>
                <td>{{ row.telefono || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="modal-footer">
          <span style="margin-right: auto; color: #64748b; font-size: 0.85rem; display: flex; align-items: center; gap: 5px;">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
            Los duplicados por código serán omitidos automáticamente.
          </span>
          <button class="btn btn-secondary" @click="showImportModal = false">Cancelar</button>
          <button class="btn premium-btn" style="background: #16a34a;" @click="confirmImport" :disabled="importLoading">
            {{ importLoading ? 'Procesando...' : 'Procesar e Importar' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Escáner de Cámara -->
    <CameraScanner 
      v-if="showCameraScanner" 
      @close="showCameraScanner = false"
      @scanned="handleCameraScan"
    />
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
.suspend-btn {
  color: #f59e0b;
  margin-right: 8px;
}
.suspend-btn:hover {
  background: #fef3c7;
}
.activate-btn {
  color: #10b981;
  margin-right: 8px;
}
.activate-btn:hover {
  background: #d1fae5;
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
  max-width: 650px;
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

.f.import-status span {
  font-weight: 700;
  color: #111827;
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
  border-color: #6366f1;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(99, 102, 241, 0.1);
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
  background: #4f46e5;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  box-shadow: 0 4px 6px -1px rgba(79, 70, 229, 0.3);
  transition: all 0.2s;
}
.premium-btn:hover {
  background: #4338ca;
  transform: translateY(-1px);
  box-shadow: 0 6px 8px -1px rgba(79, 70, 229, 0.4);
}

@keyframes fadeIn {
  from { opacity: 0; backdrop-filter: blur(0px); }
  to { opacity: 1; backdrop-filter: blur(8px); }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(20px) scale(0.95); }
  to { opacity: 1; transform: translateY(0) scale(1); }
}

@keyframes spin {
  from { transform: translateY(-50%) rotate(0deg); }
  to   { transform: translateY(-50%) rotate(360deg); }
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
</style>
