<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const emit = defineEmits(['ver-estadisticas'])

const prestamos = ref([])
const loading = ref(true)
const searchQuery = ref('')
const selectedEstado = ref('todos')
const currentPage = ref(1)
const pageSize = 10

const toast = ref({ show: false, message: '', type: 'success' })

const showToast = (message, type = 'success') => {
  toast.value = { show: true, message, type }
  setTimeout(() => {
    toast.value.show = false
  }, 4000)
}

const loadPrestamos = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/Prestamos`)
    prestamos.value = res.data
  } catch (err) {
    console.error("Error al cargar préstamos:", err)
    showToast('Error al conectar con el servidor', 'error')
  } finally {
    loading.value = false
  }
}

const devolverLibro = async (prestamoId) => {
  if (!confirm('¿Confirmas la devolución de este libro en el sistema?')) return

  try {
    const res = await axios.post(`${API_BASE_URL}/api/Prestamos/Devolver/${prestamoId}`)
    showToast(res.data.mensaje || 'Libro devuelto con éxito', 'success')
    await loadPrestamos()
  } catch (err) {
    console.error("Error al devolver libro:", err)
    showToast(err.response?.data?.mensaje || 'Error al procesar devolución', 'error')
  }
}

const eliminarPrestamo = async (prestamoId) => {
  if (!confirm('¿Seguro que deseas eliminar este registro de préstamo?')) return

  try {
    const res = await axios.delete(`${API_BASE_URL}/api/Prestamos/${prestamoId}`)
    showToast(res.data.mensaje || 'Préstamo eliminado', 'success')
    await loadPrestamos()
  } catch (err) {
    console.error("Error al eliminar préstamo:", err)
    showToast('Error al eliminar préstamo', 'error')
  }
}

const limpiarPruebas = async () => {
  if (!confirm('¿Deseas eliminar todos los registros que digan "Prueba" o "Test"?')) return

  try {
    const res = await axios.delete(`${API_BASE_URL}/api/Prestamos/LimpiarPruebas`)
    showToast(res.data.mensaje || 'Pruebas eliminadas correctamente', 'success')
    await loadPrestamos()
  } catch (err) {
    console.error("Error al limpiar pruebas:", err)
    showToast('Error al limpiar registros de prueba', 'error')
  }
}

// Métricas de resumen
const totalPrestamos = computed(() => prestamos.value.length)
const prestamosActivos = computed(() => prestamos.value.filter(p => p.estado === 'Activo' && !p.esVencido).length)
const prestamosVencidos = computed(() => prestamos.value.filter(p => p.esVencido).length)
const prestamosDevueltos = computed(() => prestamos.value.filter(p => p.estado === 'Devuelto').length)

// Filtro y búsqueda
const filteredPrestamos = computed(() => {
  let list = prestamos.value

  if (selectedEstado.value !== 'todos') {
    if (selectedEstado.value === 'vencidos') {
      list = list.filter(p => p.esVencido)
    } else if (selectedEstado.value === 'activos') {
      list = list.filter(p => p.estado === 'Activo' && !p.esVencido)
    } else if (selectedEstado.value === 'devueltos') {
      list = list.filter(p => p.estado === 'Devuelto')
    }
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    list = list.filter(p => 
      (p.alumnoCodigo && p.alumnoCodigo.toLowerCase().includes(q)) ||
      (p.alumnoNombre && p.alumnoNombre.toLowerCase().includes(q)) ||
      (p.libroTitulo && p.libroTitulo.toLowerCase().includes(q)) ||
      (p.libroCodigoBarras && p.libroCodigoBarras.toLowerCase().includes(q))
    )
  }

  return list
})

// Paginación
const totalPages = computed(() => Math.ceil(filteredPrestamos.value.length / pageSize) || 1)
const paginatedPrestamos = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredPrestamos.value.slice(start, start + pageSize)
})

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  const d = new Date(dateStr)
  return d.toLocaleDateString('es-PE', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  loadPrestamos()
})
</script>

<template>
  <div class="prestamos-container">
    <!-- TOAST NOTIFICACIÓN -->
    <transition name="toast-fade">
      <div v-if="toast.show" :class="['toast-notification', toast.type]">
        <span>{{ toast.message }}</span>
      </div>
    </transition>

    <!-- HEADER DE LA PÁGINA -->
    <div class="page-header">
      <div>
        <div style="display: flex; align-items: center; gap: 10px;">
          <h1 class="page-title">Control de Préstamos</h1>
          <span class="sync-badge" title="Sincronizado con AbsysNet URP">
            <span class="sync-dot"></span> AbsysNet Conectado
          </span>
        </div>
        <p class="page-subtitle">Visualización y seguimiento de libros prestados a estudiantes de Medicina</p>
      </div>

      <div style="display: flex; gap: 8px; align-items: center;">
        <button class="btn-stats" @click="emit('ver-estadisticas')" title="Ver Gráficos y Estadísticas de Préstamos">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 3v18h18" />
            <path d="m19 9-5 5-4-4-3 3" />
          </svg>
          <span>Estadísticas</span>
        </button>
        <button class="btn-clean" @click="limpiarPruebas" title="Eliminar registros de prueba">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
          <span>Limpiar Pruebas</span>
        </button>
        <button class="btn-refresh" @click="loadPrestamos" :disabled="loading" title="Refrescar datos">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" :class="{ 'spin': loading }">
            <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67"/>
          </svg>
          <span>Actualizar</span>
        </button>
      </div>
    </div>

    <!-- TARJETAS DE MÉTRICAS -->
    <div class="stats-grid">
      <div class="stat-card" @click="selectedEstado = 'todos'" :class="{ active: selectedEstado === 'todos' }">
        <div class="stat-icon total-icon">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Total Préstamos</span>
          <span class="stat-value">{{ totalPrestamos }}</span>
        </div>
      </div>

      <div class="stat-card" @click="selectedEstado = 'activos'" :class="{ active: selectedEstado === 'activos' }">
        <div class="stat-icon active-icon">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Activos en Sala / Domicilio</span>
          <span class="stat-value text-emerald">{{ prestamosActivos }}</span>
        </div>
      </div>

      <div class="stat-card" @click="selectedEstado = 'vencidos'" :class="{ active: selectedEstado === 'vencidos' }">
        <div class="stat-icon alert-icon">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Préstamos Vencidos</span>
          <span class="stat-value text-rose">{{ prestamosVencidos }}</span>
        </div>
      </div>

      <div class="stat-card" @click="selectedEstado = 'devueltos'" :class="{ active: selectedEstado === 'devueltos' }">
        <div class="stat-icon returned-icon">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Devueltos</span>
          <span class="stat-value text-gray">{{ prestamosDevueltos }}</span>
        </div>
      </div>
    </div>

    <!-- BARRA DE BÚSQUEDA Y FILTROS -->
    <div class="controls-bar">
      <div class="search-wrap">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#9ca3af" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
        <input 
          type="text" 
          v-model="searchQuery" 
          placeholder="Buscar por código de alumno, nombre o título del libro..."
          class="search-input"
        />
        <button v-if="searchQuery" class="clear-search" @click="searchQuery = ''">&times;</button>
      </div>

      <div class="filter-wrap">
        <select v-model="selectedEstado" class="filter-select">
          <option value="todos">Todos los Estados</option>
          <option value="activos">Solo Activos</option>
          <option value="vencidos">Solo Vencidos</option>
          <option value="devueltos">Solo Devueltos</option>
        </select>
      </div>
    </div>

    <!-- TABLA DE PRÉSTAMOS -->
    <div class="table-card">
      <div v-if="loading" class="loading-state">
        <div class="spinner"></div>
        <span>Cargando préstamos...</span>
      </div>

      <div v-else-if="filteredPrestamos.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#9ca3af" stroke-width="1.5"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
        <p>No se encontraron registros de préstamos</p>
        <span class="empty-sub">Cuando se registre un préstamo en AbsysNet, aparecerá aquí en tiempo real.</span>
      </div>

      <table v-else class="prestamos-table">
        <thead>
          <tr>
            <th>Estudiante</th>
            <th>Libro Prestado</th>
            <th>Ubicación</th>
            <th>Fecha Préstamo</th>
            <th>Fecha Límite</th>
            <th>Estado</th>
            <th style="text-align: right;">Acción</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in paginatedPrestamos" :key="p.prestamoID" :class="{ 'row-vencido': p.esVencido }">
            <!-- ALUMNO -->
            <td>
              <div class="student-cell">
                <div class="student-avatar">
                  {{ (p.alumnoNombre || 'A').charAt(0).toUpperCase() }}
                </div>
                <div>
                  <div class="student-name">{{ p.alumnoNombre }}</div>
                  <div class="student-meta">
                    <span class="badge-code">{{ p.alumnoCodigo }}</span>
                    <span v-if="p.alumnoCarrera" class="career-text">{{ p.alumnoCarrera }}</span>
                  </div>
                </div>
              </div>
            </td>

            <!-- LIBRO -->
            <td>
              <div class="book-cell">
                <div class="book-title">{{ p.libroTitulo }}</div>
                <div class="book-meta">
                  <span class="badge-barcode">🏷️ {{ p.libroCodigoBarras }}</span>
                  <span v-if="p.libroAutor" class="book-author">por {{ p.libroAutor }}</span>
                </div>
              </div>
            </td>

            <!-- UBICACIÓN -->
            <td>
              <span class="location-badge">{{ p.libroUbicacion }}</span>
            </td>

            <!-- FECHA PRÉSTAMO -->
            <td class="date-cell">
              {{ formatDate(p.fechaPrestamo) }}
            </td>

            <!-- FECHA DEVOLUCIÓN -->
            <td class="date-cell">
              <div>{{ formatDate(p.fechaDevolucion) }}</div>
              <div v-if="p.estado === 'Activo'">
                <span v-if="p.esVencido" class="overdue-text">
                  ⚠️ Vencido hace {{ Math.abs(p.diasRestantes) }} d
                </span>
                <span v-else class="remaining-text">
                  ⏳ Quedan {{ p.diasRestantes }} día(s)
                </span>
              </div>
            </td>

            <!-- ESTADO -->
            <td>
              <span v-if="p.esVencido" class="status-badge badge-vencido">
                Vencido
              </span>
              <span v-else-if="p.estado === 'Activo'" class="status-badge badge-activo">
                Activo
              </span>
              <span v-else class="status-badge badge-devuelto">
                Devuelto
              </span>
            </td>

            <!-- ACCIÓN -->
            <td style="text-align: right;">
              <div style="display: inline-flex; align-items: center; gap: 6px; justify-content: flex-end;">
                <button 
                  v-if="p.estado === 'Activo'" 
                  class="btn-devolver"
                  @click="devolverLibro(p.prestamoID)"
                  title="Registrar devolución en sala"
                >
                  Marcar Devuelto
                </button>
                <span v-else class="text-muted-ok">
                  ✓ Entregado
                </span>

                <button 
                  class="btn-delete"
                  @click="eliminarPrestamo(p.prestamoID)"
                  title="Eliminar este préstamo"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- PAGINACIÓN -->
      <div v-if="totalPages > 1" class="pagination-bar">
        <span>Mostrando página {{ currentPage }} de {{ totalPages }} ({{ filteredPrestamos.length }} registros)</span>
        <div class="pagination-buttons">
          <button :disabled="currentPage === 1" @click="currentPage--" class="page-btn">Anterior</button>
          <button :disabled="currentPage === totalPages" @click="currentPage++" class="page-btn">Siguiente</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.prestamos-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
  color: #111827;
}

/* HEADER */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.page-subtitle {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 4px 0 0 0;
}

.sync-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #ecfdf5;
  color: #065f46;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: 9999px;
  border: 1px solid #a7f3d0;
}

.sync-dot {
  width: 7px;
  height: 7px;
  background: #10b981;
  border-radius: 50%;
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
}

.btn-stats {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #9f1239;
  border: 1px solid #9f1239;
  color: #ffffff;
  padding: 0.6rem 0.95rem;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.8rem;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 2px 4px rgba(159, 18, 57, 0.2);
}

.btn-stats:hover {
  background: #881337;
  border-color: #881337;
  transform: translateY(-1px);
}

.btn-clean {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #ffffff;
  border: 1px solid #fecdd3;
  color: #9f1239;
  padding: 0.6rem 0.9rem;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.8rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-clean:hover {
  background: #fff1f2;
  border-color: #f43f5e;
}

.btn-delete {
  background: transparent;
  border: 1px solid #fee2e2;
  color: #ef4444;
  width: 28px;
  height: 28px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-delete:hover {
  background: #fee2e2;
  color: #b91c1c;
}

.btn-refresh {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #ffffff;
  border: 1px solid #d1d5db;
  color: #374151;
  padding: 0.6rem 1rem;
  border-radius: 8px;
  font-weight: 500;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-refresh:hover:not(:disabled) {
  background: #f9fafb;
  border-color: #9ca3af;
}

.spin {
  animation: spin 1s linear infinite;
}
@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* MÉTRICAS */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.stat-card {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.1rem;
  display: flex;
  align-items: center;
  gap: 14px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.stat-card.active {
  border-color: #9f1239;
  background: #fff1f2;
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.total-icon { background: #eff6ff; color: #2563eb; }
.active-icon { background: #ecfdf5; color: #059669; }
.alert-icon { background: #fff1f2; color: #e11d48; }
.returned-icon { background: #f3f4f6; color: #6b7280; }

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 0.78rem;
  font-weight: 500;
  color: #6b7280;
}

.stat-value {
  font-size: 1.4rem;
  font-weight: 700;
  color: #111827;
  line-height: 1.2;
}

.text-emerald { color: #059669; }
.text-rose { color: #e11d48; }
.text-gray { color: #6b7280; }

/* CONTROLES / BUSCADOR */
.controls-bar {
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
}

.search-wrap {
  flex: 1;
  min-width: 280px;
  display: flex;
  align-items: center;
  gap: 10px;
  background: #ffffff;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  padding: 0.6rem 1rem;
}

.search-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 0.875rem;
  color: #111827;
}

.clear-search {
  background: transparent;
  border: none;
  font-size: 1.2rem;
  color: #9ca3af;
  cursor: pointer;
}

.filter-select {
  padding: 0.6rem 1rem;
  background: #ffffff;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 0.875rem;
  color: #374151;
  outline: none;
  cursor: pointer;
}

/* TABLA */
.table-card {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.prestamos-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.875rem;
}

.prestamos-table th {
  background: #f9fafb;
  padding: 12px 16px;
  font-weight: 600;
  color: #4b5563;
  border-bottom: 1px solid #e5e7eb;
}

.prestamos-table td {
  padding: 14px 16px;
  border-bottom: 1px solid #f3f4f6;
  vertical-align: middle;
}

.row-vencido {
  background-color: #fff9fa;
}

/* ESTUDIANTE */
.student-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.student-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #9f1239;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 0.875rem;
  flex-shrink: 0;
}

.student-name {
  font-weight: 600;
  color: #111827;
}

.student-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 2px;
}

.badge-code {
  background: #f3f4f6;
  color: #374151;
  font-size: 0.72rem;
  font-weight: 600;
  padding: 1px 6px;
  border-radius: 4px;
}

.career-text {
  font-size: 0.72rem;
  color: #6b7280;
}

/* LIBRO */
.book-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.book-title {
  font-weight: 600;
  color: #1f2937;
  max-width: 280px;
}

.book-meta {
  display: flex;
  align-items: center;
  gap: 6px;
}

.badge-barcode {
  background: #fef3c7;
  color: #92400e;
  font-size: 0.72rem;
  font-weight: 600;
  padding: 1px 6px;
  border-radius: 4px;
}

.book-author {
  font-size: 0.75rem;
  color: #6b7280;
}

.location-badge {
  background: #f1f5f9;
  color: #475569;
  font-size: 0.75rem;
  font-weight: 500;
  padding: 3px 8px;
  border-radius: 6px;
}

.date-cell {
  color: #4b5563;
  font-size: 0.82rem;
}

.overdue-text {
  color: #e11d48;
  font-size: 0.72rem;
  font-weight: 600;
}

.remaining-text {
  color: #059669;
  font-size: 0.72rem;
  font-weight: 500;
}

/* ESTADOS */
.status-badge {
  display: inline-block;
  padding: 3px 8px;
  border-radius: 9999px;
  font-size: 0.72rem;
  font-weight: 600;
}

.badge-activo { background: #dcfce7; color: #15803d; }
.badge-vencido { background: #fee2e2; color: #b91c1c; }
.badge-devuelto { background: #f3f4f6; color: #4b5563; }

/* ACCIONES */
.btn-devolver {
  background: #ffffff;
  border: 1px solid #d1d5db;
  color: #9f1239;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 5px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-devolver:hover {
  background: #fff1f2;
  border-color: #f43f5e;
}

.text-muted-ok {
  color: #9ca3af;
  font-size: 0.78rem;
}

/* ESTADOS VACÍO Y CARGA */
.loading-state, .empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  color: #6b7280;
  gap: 8px;
}

.empty-state p {
  font-weight: 600;
  font-size: 1rem;
  color: #374151;
  margin: 0;
}

.empty-sub {
  font-size: 0.8rem;
  color: #9ca3af;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #e5e7eb;
  border-top-color: #9f1239;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

/* PAGINACIÓN */
.pagination-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  border-top: 1px solid #e5e7eb;
  font-size: 0.8rem;
  color: #6b7280;
}

.pagination-buttons {
  display: flex;
  gap: 8px;
}

.page-btn {
  background: #ffffff;
  border: 1px solid #d1d5db;
  color: #374151;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.78rem;
  cursor: pointer;
}

.page-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* TOAST */
.toast-notification {
  position: fixed;
  top: 24px;
  right: 28px;
  padding: 12px 18px;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 500;
  color: #ffffff;
  z-index: 99999;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.toast-notification.success { background: #059669; }
.toast-notification.error { background: #e11d48; }

.toast-fade-enter-active, .toast-fade-leave-active { transition: opacity 0.3s, transform 0.3s; }
.toast-fade-enter-from, .toast-fade-leave-to { opacity: 0; transform: translateY(-10px); }
</style>

