<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'

const alumnos = ref([])
const searchQuery = ref('')
const showModal = ref(false)
const showImportModal = ref(false)
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

const fetchAlumnos = async () => {
  const res = await axios.get('https://localhost:7215/api/alumnos')
  alumnos.value = res.data
}

const filteredAlumnos = computed(() => {
  if (!searchQuery.value) return alumnos.value
  const query = searchQuery.value.toLowerCase()
  return alumnos.value.filter(a => 
    a.nombres.toLowerCase().includes(query) || 
    a.apellidoPaterno.toLowerCase().includes(query) || 
    a.codigoUniversitario.toLowerCase().includes(query) || 
    (a.dni && a.dni.toLowerCase().includes(query))
  )
})

const saveAlumno = async () => {
  if (currentAlumno.value.alumnoID) {
    await axios.put(`https://localhost:7215/api/alumnos/${currentAlumno.value.alumnoID}`, currentAlumno.value)
  } else {
    await axios.post('https://localhost:7215/api/alumnos', currentAlumno.value)
  }
  showModal.value = false
  fetchAlumnos()
}

const deleteAlumno = async (id) => {
  if (confirm('¿Eliminar registro?')) {
    await axios.delete(`https://localhost:7215/api/alumnos/${id}`)
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
      await axios.put(`https://localhost:7215/api/alumnos/${alumno.alumnoID}`, updatedAlumno)
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
    const res = await axios.post('https://localhost:7215/api/alumnos/bulk', importPreviewData.value)
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

onMounted(fetchAlumnos)
</script>

<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Participantes</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">{{ alumnos.length }} Registrados</div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        <div style="position: relative;">
          <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: #9ca3af;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input type="text" v-model="searchQuery" placeholder="Buscar por nombre, código o DNI..." style="padding: 0.5rem 1rem 0.5rem 2rem; border-radius: 0.5rem; border: 1px solid #e5e7eb; width: 300px; font-size: 0.875rem; color: #111827;">
        </div>
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
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(a, index) in filteredAlumnos" :key="a.alumnoID">
          <td style="color: #6b7280; font-weight: 600;">{{ index + 1 }}</td>
          <td><strong style="color: #111827;">{{ a.codigoUniversitario }}</strong></td>
          <td>{{ a.dni }}</td>
          <td>{{ a.nombres }} {{ a.apellidoPaterno }} {{ a.apellidoMaterno }}</td>
          <td>
            <span :style="{ background: a.estado ? '#f0fdf4' : '#fef2f2', color: a.estado ? '#166534' : '#991b1b', padding: '4px 8px', borderRadius: '4px', fontSize: '0.85rem', fontWeight: '600' }">
              {{ a.estado ? 'Activo' : 'Inactivo' }}
            </span>
          </td>
          <td style="color: #0ea5e9;">{{ a.correoInstitucional || '-' }}</td>
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
          <div class="form-grid-2">
            <div class="input-group">
              <label>Código Universitario / ID</label>
              <input v-model="currentAlumno.codigoUniversitario" placeholder="Ej. 20212345" class="premium-input">
            </div>
            <div class="input-group">
              <label>DNI</label>
              <input v-model="currentAlumno.dni" placeholder="Nro. Documento" class="premium-input">
            </div>
          </div>
          
          <div class="input-group">
            <label>Nombres</label>
            <input v-model="currentAlumno.nombres" placeholder="Nombres completos" class="premium-input">
          </div>
          
          <div class="form-grid-2">
            <div class="input-group">
              <label>Apellido Paterno</label>
              <input v-model="currentAlumno.apellidoPaterno" placeholder="Paterno" class="premium-input">
            </div>
            <div class="input-group">
              <label>Apellido Materno</label>
              <input v-model="currentAlumno.apellidoMaterno" placeholder="Materno" class="premium-input">
            </div>
          </div>
          
          <div class="form-grid-2">
            <div class="input-group">
              <label>Carrera / Cargo</label>
              <input v-model="currentAlumno.carrera" placeholder="Ej. Medicina Humana" class="premium-input">
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
  height: 90vh;
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
</style>
