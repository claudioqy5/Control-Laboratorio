<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'
import { API_BASE_URL } from '../config'
import { Line, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  BarElement,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Filler
} from 'chart.js'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Filler)

// Helper para fecha en zona horaria de Perú (UTC-5)
const getPeruDate = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' }
  const formatter = new Intl.DateTimeFormat('en-CA', options) // YYYY-MM-DD
  return formatter.format(new Date())
}

const todayDate = getPeruDate()
const selectedDate = ref(todayDate)
const loading = ref(false)
const dailyData = ref(null)

// Filtro de tabla de movimientos
const searchFilter = ref('')
const tipoFilter = ref('todos') // 'todos' | 'prestamos' | 'devoluciones'

const loadDailyData = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/Prestamos/Estadisticas?date=${selectedDate.value}&mode=date`)
    dailyData.value = res.data

    // Fallback: si el backend remoto aún no envía 'movimientos', los cargamos desde /api/Prestamos
    if (!dailyData.value.movimientos || dailyData.value.movimientos.length === 0) {
      try {
        const prestamosRes = await axios.get(`${API_BASE_URL}/api/Prestamos`)
        const allPrestamos = prestamosRes.data || []
        const selDate = selectedDate.value

        const movs = []
        allPrestamos.forEach(p => {
          const fechaPrestamoStr = p.fechaPrestamo ? p.fechaPrestamo.split('T')[0] : ''
          const fechaEntregadoStr = p.fechaEntregado ? p.fechaEntregado.split('T')[0] : ''

          if (fechaPrestamoStr === selDate) {
            movs.push({
              tipo: 'Préstamo',
              fecha: p.fechaPrestamo,
              hora: p.fechaPrestamo ? new Date(p.fechaPrestamo).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '',
              prestamoID: p.prestamoID,
              libroID: p.libroID,
              libroTitulo: p.libroTitulo || 'Sin título',
              libroCodigo: p.libroCodigoBarras || '',
              alumnoID: p.alumnoID,
              alumnoNombre: p.alumnoNombre || 'Desconocido',
              alumnoCodigo: p.alumnoCodigo || '',
              alumnoCarrera: p.alumnoCarrera || '',
              fechaDevolucion: p.fechaDevolucion,
              estado: p.estado
            })
          }

          if (fechaEntregadoStr === selDate) {
            movs.push({
              tipo: 'Devolución',
              fecha: p.fechaEntregado,
              hora: p.fechaEntregado ? new Date(p.fechaEntregado).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '',
              prestamoID: p.prestamoID,
              libroID: p.libroID,
              libroTitulo: p.libroTitulo || 'Sin título',
              libroCodigo: p.libroCodigoBarras || '',
              alumnoID: p.alumnoID,
              alumnoNombre: p.alumnoNombre || 'Desconocido',
              alumnoCodigo: p.alumnoCodigo || '',
              alumnoCarrera: p.alumnoCarrera || '',
              fechaDevolucion: p.fechaDevolucion,
              estado: 'Devuelto'
            })
          }
        })

        movs.sort((a, b) => new Date(b.fecha) - new Date(a.fecha))
        dailyData.value.movimientos = movs
      } catch (fallbackErr) {
        console.warn('Fallback de movimientos no disponible:', fallbackErr)
      }
    }
  } catch (err) {
    console.error('Error al cargar resumen diario de préstamos:', err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadDailyData()
})

// KPIs Computados
const kpis = computed(() => dailyData.value?.kpis || {
  totalPrestamosPeriodo: 0,
  totalDevueltosPeriodo: 0,
  totalActivos: 0,
  totalVencidos: 0,
  porVencerHoy: 0,
  tasaATiempo: 100,
  totalCatalogo: 0,
  librosPrestados: 0
})

// Datos de gráfico por hora (07:00 a 21:00)
const lineChartData = computed(() => {
  const flujo = dailyData.value?.tendenciaFlujo || []
  const labels = flujo.map(f => f.etiqueta || f.Etiqueta || '')
  const prestamos = flujo.map(f => f.prestamos ?? f.Prestamos ?? 0)
  const devoluciones = flujo.map(f => f.devoluciones ?? f.Devoluciones ?? 0)

  return {
    labels,
    datasets: [
      {
        label: 'Préstamos Realizados',
        data: prestamos,
        borderColor: '#be123c',
        backgroundColor: 'rgba(190, 18, 60, 0.12)',
        tension: 0.35,
        fill: true,
        pointBackgroundColor: '#be123c',
        pointRadius: 4,
        pointHoverRadius: 6
      },
      {
        label: 'Devoluciones Procesadas',
        data: devoluciones,
        borderColor: '#059669',
        backgroundColor: 'rgba(5, 150, 105, 0.12)',
        tension: 0.35,
        fill: true,
        pointBackgroundColor: '#059669',
        pointRadius: 4,
        pointHoverRadius: 6
      }
    ]
  }
})

const lineChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    mode: 'index',
    intersect: false
  },
  plugins: {
    legend: {
      position: 'top',
      align: 'end',
      labels: {
        boxWidth: 10,
        boxHeight: 10,
        usePointStyle: true,
        font: { size: 12, weight: '600' }
      }
    },
    tooltip: {
      backgroundColor: '#1f2937',
      padding: 10,
      cornerRadius: 8
    }
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: { precision: 0, font: { size: 11 } },
      grid: { color: '#f3f4f6' }
    },
    x: {
      ticks: { font: { size: 11 } },
      grid: { display: false }
    }
  }
}

// Datos de gráfico de categorías
const categoryPalette = ['#be123c', '#0284c7', '#059669', '#d97706', '#7c3aed', '#ec4899', '#f97316', '#64748b']

const doughnutChartData = computed(() => {
  const cats = dailyData.value?.porCategoria || []
  return {
    labels: cats.map(c => c.categoria || c.Categoria || 'Sin categoría'),
    datasets: [
      {
        data: cats.map(c => c.cantidad || c.Cantidad || 0),
        backgroundColor: categoryPalette.slice(0, cats.length),
        borderWidth: 2,
        borderColor: '#ffffff'
      }
    ]
  }
})

const doughnutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '70%',
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        boxWidth: 8,
        usePointStyle: true,
        font: { size: 11 }
      }
    }
  }
}

// Movimientos detallados filtrados
const movimientosFiltrados = computed(() => {
  const movs = dailyData.value?.movimientos || []
  return movs.filter(m => {
    // Filtro por tipo
    if (tipoFilter.value === 'prestamos' && m.tipo !== 'Préstamo' && m.Tipo !== 'Préstamo') return false
    if (tipoFilter.value === 'devoluciones' && m.tipo !== 'Devolución' && m.Tipo !== 'Devolución') return false

    // Filtro por texto
    if (!searchFilter.value.trim()) return true
    const term = searchFilter.value.toLowerCase().trim()
    const libroTitulo = (m.libroTitulo || m.LibroTitulo || '').toLowerCase()
    const libroCodigo = (m.libroCodigo || m.LibroCodigo || '').toLowerCase()
    const alumnoNombre = (m.alumnoNombre || m.AlumnoNombre || '').toLowerCase()
    const alumnoCodigo = (m.alumnoCodigo || m.AlumnoCodigo || '').toLowerCase()
    const alumnoCarrera = (m.alumnoCarrera || m.AlumnoCarrera || '').toLowerCase()

    return (
      libroTitulo.includes(term) ||
      libroCodigo.includes(term) ||
      alumnoNombre.includes(term) ||
      alumnoCodigo.includes(term) ||
      alumnoCarrera.includes(term)
    )
  })
})

// Exportar movimientos a Excel
const exportMovimientosExcel = () => {
  const rows = movimientosFiltrados.value.map(m => ({
    'Hora': m.hora || m.Hora || '',
    'Tipo': m.tipo || m.Tipo || '',
    'Código Libro': m.libroCodigo || m.LibroCodigo || '',
    'Título del Libro': m.libroTitulo || m.LibroTitulo || '',
    'Código Alumno': m.alumnoCodigo || m.AlumnoCodigo || '',
    'Alumno': m.alumnoNombre || m.AlumnoNombre || '',
    'Carrera': m.alumnoCarrera || m.AlumnoCarrera || '',
    'Fecha Devolución': m.fechaDevolucion || m.FechaDevolucion ? new Date(m.fechaDevolucion || m.FechaDevolucion).toLocaleDateString() : '',
    'Estado': m.estado || m.Estado || ''
  }))

  const ws = XLSX.utils.json_to_sheet(rows)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Movimientos')
  XLSX.writeFile(wb, `Movimientos_Prestamos_${selectedDate.value}.xlsx`)
}

// Exportar afluencia por hora a Excel
const exportHourlyExcel = () => {
  const flujo = dailyData.value?.tendenciaFlujo || []
  const rows = flujo.map(f => ({
    'Hora': f.etiqueta || f.Etiqueta || '',
    'Préstamos Realizados': f.prestamos ?? f.Prestamos ?? 0,
    'Devoluciones Procesadas': f.devoluciones ?? f.Devoluciones ?? 0,
    'Total Movimientos': (f.prestamos ?? f.Prestamos ?? 0) + (f.devoluciones ?? f.Devoluciones ?? 0)
  }))

  const ws = XLSX.utils.json_to_sheet(rows)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Afluencia_Horaria')
  XLSX.writeFile(wb, `Afluencia_Prestamos_${selectedDate.value}.xlsx`)
}

defineExpose({
  loadDailyData,
  selectedDate,
  dailyData
})
</script>

<template>
  <div class="resumen-prestamos-wrapper">
    <!-- HEADER MINIMAL -->
    <div class="top-header-bar">
      <div>
        <h2 class="page-title">Resumen Diario de Préstamos</h2>
        <p class="page-subtitle">Circulación y analítica de biblioteca · {{ selectedDate }}</p>
      </div>

      <div class="header-right-controls">
        <!-- SELECTOR DE FECHA -->
        <div class="date-picker-box">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#6b7280" stroke-width="2">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
            <line x1="16" y1="2" x2="16" y2="6"></line>
            <line x1="8" y1="2" x2="8" y2="6"></line>
            <line x1="3" y1="10" x2="21" y2="10"></line>
          </svg>
          <input
            type="date"
            v-model="selectedDate"
            @change="loadDailyData"
            class="date-input"
            data-test="date-picker"
          />
        </div>

        <!-- STATUS PILL -->
        <div class="status-pill">
          <span class="pulse-dot"></span>
          {{ selectedDate === todayDate ? 'AbsysNet En Línea' : 'Datos Históricos' }}
        </div>

        <!-- REFRESH BUTTON -->
        <button
          type="button"
          class="btn-refresh"
          @click="loadDailyData"
          :disabled="loading"
          title="Actualizar datos del día"
        >
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.2"
            :class="{ 'spin': loading }"
          >
            <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67" />
          </svg>
        </button>
      </div>
    </div>

    <!-- SPINNER INICIAL -->
    <div v-if="loading && !dailyData" class="loading-overlay">
      <div class="spinner"></div>
      <p>Cargando información del día...</p>
    </div>

    <template v-else-if="dailyData">
      <!-- MINI CARDS ROW (5 KPIs) -->
      <div class="stats-grid">
        <!-- 1. Préstamos del día -->
        <div class="stat-card">
          <div class="stat-icon red">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z" />
              <path d="M6 6h10" />
              <path d="M6 10h10" />
            </svg>
          </div>
          <div>
            <div class="stat-label">Préstamos de Hoy</div>
            <div class="stat-value" data-test="kpi-prestamos-hoy">{{ kpis.totalPrestamosPeriodo }}</div>
            <div class="stat-sub">Registrados en la fecha</div>
          </div>
        </div>

        <!-- 2. Devoluciones del día -->
        <div class="stat-card">
          <div class="stat-icon green">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M9 11l3 3L22 4" />
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
            </svg>
          </div>
          <div>
            <div class="stat-label">Devoluciones de Hoy</div>
            <div class="stat-value" data-test="kpi-devoluciones-hoy">{{ kpis.totalDevueltosPeriodo }}</div>
            <div class="stat-sub">Procesadas en mostrador</div>
          </div>
        </div>

        <!-- 3. Préstamos Activos -->
        <div class="stat-card">
          <div class="stat-icon amber">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <circle cx="12" cy="12" r="10" />
              <polyline points="12 6 12 12 16 14" />
            </svg>
          </div>
          <div>
            <div class="stat-label">Préstamos Activos</div>
            <div class="stat-value" data-test="kpi-activos">
              {{ kpis.totalActivos }} <small>/ {{ kpis.totalCatalogo }}</small>
            </div>
            <div class="stat-sub">En sala o a domicilio</div>
          </div>
        </div>

        <!-- 4. Vencidos / Por Vencer -->
        <div class="stat-card">
          <div class="stat-icon red-light">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
          </div>
          <div>
            <div class="stat-label">Préstamos Vencidos</div>
            <div class="stat-value" data-test="kpi-vencidos">{{ kpis.totalVencidos }}</div>
            <div class="stat-sub text-danger">{{ kpis.porVencerHoy }} vencen hoy</div>
          </div>
        </div>

        <!-- 5. Tasa a Tiempo -->
        <div class="stat-card">
          <div class="stat-icon blue">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
              <path d="m9 12 2 2 4-4" />
            </svg>
          </div>
          <div>
            <div class="stat-label">Cumplimiento en Plazo</div>
            <div class="stat-value" data-test="kpi-tasa">{{ kpis.tasaATiempo }}%</div>
            <div class="stat-sub">Tasa de devolución a tiempo</div>
          </div>
        </div>
      </div>

      <!-- FILA DE GRÁFICOS (2 COLUMNAS) -->
      <div class="charts-row-two">
        <!-- Izquierda: Flujo Horario -->
        <div class="chart-box">
          <div class="chart-header">
            <div style="display: flex; align-items: center; gap: 8px;">
              <span>Afluencia por hora (Préstamos vs Devoluciones)</span>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#be123c" stroke-width="2.5">
                <polyline points="22 7 13.5 15.5 8.5 10.5 2 17" />
              </svg>
            </div>
            <button @click="exportHourlyExcel" class="export-btn" title="Descargar reporte por horas en Excel">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
              Reporte
            </button>
          </div>
          <div class="chart-container">
            <Line :data="lineChartData" :options="lineChartOptions" />
          </div>
        </div>

        <!-- Derecha: Distribución por Categoría del Libro -->
        <div class="chart-box">
          <div class="chart-header">
            <div>
              <span>Categoría del Libro</span>
              <div style="font-size: 0.72rem; color: #6b7280; font-weight: 500;">
                Temática de los libros solicitados en la fecha
              </div>
            </div>
            <span style="font-size: 0.75rem; color: #6b7280; font-weight: 500;">
              {{ (dailyData.porCategoria || []).length }} {{ (dailyData.porCategoria || []).length === 1 ? 'categoría' : 'categorías' }}
            </span>
          </div>
          <div class="chart-container donut-wrapper">
            <Doughnut
              v-if="(dailyData.porCategoria || []).length > 0"
              :data="doughnutChartData"
              :options="doughnutOptions"
            />
            <div v-else class="empty-state">
              Sin préstamos registrados en esta fecha para categorizar
            </div>
          </div>
        </div>
      </div>

      <!-- TABLA DE MOVIMIENTOS DEL DÍA -->
      <div class="movements-card">
        <div class="movements-header">
          <div class="movements-title-group">
            <h3 class="movements-title">Movimientos Registrados en la Fecha</h3>
            <span class="count-tag">{{ movimientosFiltrados.length }} registros</span>
          </div>

          <div class="movements-controls">
            <!-- Selector de Tipo: Todos / Préstamos / Devoluciones -->
            <div class="type-filter-group">
              <button
                type="button"
                :class="['filter-btn', { active: tipoFilter === 'todos' }]"
                @click="tipoFilter = 'todos'"
              >
                Todos
              </button>
              <button
                type="button"
                :class="['filter-btn', { active: tipoFilter === 'prestamos' }]"
                @click="tipoFilter = 'prestamos'"
              >
                Solo Préstamos
              </button>
              <button
                type="button"
                :class="['filter-btn', { active: tipoFilter === 'devoluciones' }]"
                @click="tipoFilter = 'devoluciones'"
              >
                Solo Devoluciones
              </button>
            </div>

            <!-- Buscador -->
            <div class="search-box">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#9ca3af" stroke-width="2">
                <circle cx="11" cy="11" r="8" />
                <line x1="21" y1="21" x2="16.65" y2="16.65" />
              </svg>
              <input
                type="text"
                v-model="searchFilter"
                placeholder="Buscar por libro, alumno o código..."
                class="search-input"
              />
            </div>

            <!-- Botón Exportar -->
            <button
              @click="exportMovimientosExcel"
              :disabled="movimientosFiltrados.length === 0"
              class="export-btn"
              title="Descargar lista de movimientos en Excel"
            >
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
              Descargar Excel
            </button>
          </div>
        </div>

        <!-- TABLA -->
        <div class="table-responsive">
          <table class="styled-table" v-if="movimientosFiltrados.length > 0">
            <thead>
              <tr>
                <th style="width: 80px;">Hora</th>
                <th style="width: 120px;">Tipo</th>
                <th>Material Bibliográfico</th>
                <th>Estudiante / Lector</th>
                <th>Carrera</th>
                <th style="width: 140px;">Fecha Límite</th>
                <th style="width: 100px;">Estado</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(m, idx) in movimientosFiltrados" :key="idx">
                <td class="font-mono text-center">{{ m.hora || m.Hora }}</td>
                <td>
                  <span
                    :class="[
                      'type-badge',
                      (m.tipo || m.Tipo) === 'Préstamo' ? 'badge-prestamo' : 'badge-devolucion'
                    ]"
                  >
                    <svg
                      v-if="(m.tipo || m.Tipo) === 'Préstamo'"
                      width="12"
                      height="12"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2.5"
                    >
                      <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z" />
                    </svg>
                    <svg
                      v-else
                      width="12"
                      height="12"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      stroke-width="2.5"
                    >
                      <path d="M9 11l3 3L22 4" />
                    </svg>
                    {{ m.tipo || m.Tipo }}
                  </span>
                </td>
                <td>
                  <div class="book-title">{{ m.libroTitulo || m.LibroTitulo }}</div>
                  <div class="book-code font-mono">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <rect x="3" y="4" width="18" height="16" rx="2" />
                      <line x1="7" y1="8" x2="7" y2="16" />
                      <line x1="12" y1="8" x2="12" y2="16" />
                      <line x1="17" y1="8" x2="17" y2="16" />
                    </svg>
                    {{ m.libroCodigo || m.LibroCodigo }}
                  </div>
                </td>
                <td>
                  <div class="student-name">{{ m.alumnoNombre || m.AlumnoNombre }}</div>
                  <div class="student-code font-mono">{{ m.alumnoCodigo || m.AlumnoCodigo }}</div>
                </td>
                <td class="text-muted">{{ m.alumnoCarrera || m.AlumnoCarrera || '—' }}</td>
                <td>
                  <span v-if="m.fechaDevolucion || m.FechaDevolucion" class="due-date">
                    {{ new Date(m.fechaDevolucion || m.FechaDevolucion).toLocaleDateString() }}
                  </span>
                  <span v-else class="text-muted">—</span>
                </td>
                <td>
                  <span
                    :class="[
                      'status-badge',
                      (m.estado || m.Estado) === 'Devuelto' ? 'badge-ok' : ((m.estado || m.Estado) === 'Activo' ? 'badge-info' : 'badge-warning')
                    ]"
                  >
                    {{ m.estado || m.Estado }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>

          <div v-else class="empty-table-state">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#cbd5e1" stroke-width="1.5">
              <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z" />
              <path d="M6 6h10" />
              <path d="M6 10h10" />
            </svg>
            <p>No se encontraron movimientos de préstamos ni devoluciones para la fecha seleccionada.</p>
          </div>
        </div>
      </div>

      <!-- FILA DE RANKING: TOP LIBROS Y TOP LECTORES DEL DÍA -->
      <div class="charts-row-three">
        <!-- Top Libros del día -->
        <div class="chart-box">
          <div class="chart-header">
            <span>Top Libros Solicitados en la Fecha</span>
            <span style="font-size: 0.75rem; color: #6b7280; font-weight: 500;">
              {{ (dailyData.topLibros || []).length }} títulos
            </span>
          </div>
          <div v-if="(dailyData.topLibros || []).length > 0" class="ranking-list">
            <div v-for="(b, i) in (dailyData.topLibros || []).slice(0, 5)" :key="i" class="ranking-item">
              <div class="ranking-pos">{{ i + 1 }}</div>
              <div class="ranking-info">
                <div class="ranking-title">{{ b.titulo || b.Titulo }}</div>
                <div class="ranking-code font-mono">{{ b.codigoBarras || b.CodigoBarras }}</div>
              </div>
              <div class="ranking-val">{{ b.total || b.Total }} {{ (b.total || b.Total) === 1 ? 'préstamo' : 'préstamos' }}</div>
            </div>
          </div>
          <div v-else class="empty-state">Sin solicitudes en esta fecha</div>
        </div>

        <!-- Top Lectores del día -->
        <div class="chart-box">
          <div class="chart-header">
            <span>Lectores Activos en la Fecha</span>
            <span style="font-size: 0.75rem; color: #6b7280; font-weight: 500;">
              {{ (dailyData.topAlumnos || []).length }} estudiantes
            </span>
          </div>
          <div v-if="(dailyData.topAlumnos || []).length > 0" class="ranking-list">
            <div v-for="(a, i) in (dailyData.topAlumnos || []).slice(0, 5)" :key="i" class="ranking-item">
              <div class="ranking-pos">{{ i + 1 }}</div>
              <div class="ranking-info">
                <div class="ranking-title">{{ a.nombre || a.Nombre }}</div>
                <div class="ranking-code font-mono">{{ a.codigoUniversitario || a.CodigoUniversitario }} · {{ a.carrera || a.Carrera }}</div>
              </div>
              <div class="ranking-val">{{ a.totalPrestamos || a.TotalPrestamos }} préstamos</div>
            </div>
          </div>
          <div v-else class="empty-state">Sin préstamos a alumnos en esta fecha</div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.resumen-prestamos-wrapper {
  max-width: 100%;
  overflow: hidden;
}

/* Header Minimal */
.top-header-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  gap: 1rem;
  flex-wrap: wrap;
}

.page-title {
  color: #111827;
  font-size: 1.5rem;
  font-weight: 800;
  margin: 0;
}

.page-subtitle {
  color: #6b7280;
  font-size: 0.8rem;
  margin: 0;
}

.header-right-controls {
  display: flex;
  align-items: center;
  gap: 12px;
}

.date-picker-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: white;
  padding: 6px 12px;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.date-input {
  border: none;
  outline: none;
  color: #374151;
  font-weight: 600;
  font-size: 0.85rem;
  cursor: pointer;
  background: transparent;
}

.status-pill {
  background: #ecfdf5;
  color: #059669;
  padding: 0.4rem 1rem;
  border-radius: 2rem;
  font-size: 0.75rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid #d1fae5;
  white-space: nowrap;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  background: #10b981;
  border-radius: 50%;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
  70% { transform: scale(1); box-shadow: 0 0 0 10px rgba(16, 185, 129, 0); }
  100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}

.btn-refresh {
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 8px 10px;
  cursor: pointer;
  color: #4b5563;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.btn-refresh:hover:not(:disabled) {
  background: #f9fafb;
  color: #111827;
  border-color: #cbd5e1;
}

.spin {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  100% { transform: rotate(360deg); }
}

/* Stats Cards Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
}

@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.stat-card {
  background: #ffffff;
  padding: 1rem;
  border-radius: 1rem;
  border: 1px solid #f3f4f6;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);
}

.stat-icon {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-icon.red { background: #ffe4e6; color: #be123c; }
.stat-icon.green { background: #d1fae5; color: #059669; }
.stat-icon.amber { background: #fef3c7; color: #d97706; }
.stat-icon.red-light { background: #fee2e2; color: #dc2626; }
.stat-icon.blue { background: #e0f2fe; color: #0284c7; }

.stat-label {
  color: #6b7280;
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
}

.stat-value {
  color: #111827;
  font-size: 1.25rem;
  font-weight: 800;
}

.stat-value small {
  font-size: 0.8rem;
  color: #9ca3af;
  font-weight: 500;
}

.stat-sub {
  font-size: 0.68rem;
  color: #9ca3af;
  margin-top: 2px;
}

.text-danger {
  color: #dc2626 !important;
  font-weight: 600;
}

/* Charts Rows */
.charts-row-two {
  display: grid;
  grid-template-columns: 1.8fr 1fr;
  gap: 1.25rem;
  margin-bottom: 1.25rem;
}

.charts-row-three {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
  margin-top: 1.25rem;
}

@media (max-width: 1024px) {
  .charts-row-two,
  .charts-row-three {
    grid-template-columns: 1fr;
  }
}

.chart-box {
  background: #ffffff;
  padding: 1.25rem;
  border-radius: 1rem;
  border: 1px solid #f3f4f6;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  color: #111827;
  font-size: 0.9rem;
  font-weight: 700;
}

.chart-container {
  height: 25vh;
  position: relative;
}

.donut-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #9ca3af;
  font-size: 0.8rem;
  text-align: center;
}

.export-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  padding: 5px 12px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s;
}

.export-btn:hover:not(:disabled) {
  background: #f1f5f9;
  border-color: #cbd5e1;
  color: #1e293b;
}

.export-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.export-btn svg {
  color: #16a34a;
}

/* Movements Card and Table */
.movements-card {
  background: #ffffff;
  border-radius: 1rem;
  border: 1px solid #f3f4f6;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  margin-top: 1.25rem;
}

.movements-header {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid #f1f5f9;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.movements-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.movements-title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 700;
  color: #111827;
}

.count-tag {
  background: #f1f5f9;
  color: #64748b;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.72rem;
  font-weight: 600;
}

.movements-controls {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.type-filter-group {
  display: flex;
  background: #f1f5f9;
  padding: 3px;
  border-radius: 8px;
  gap: 2px;
}

.filter-btn {
  background: transparent;
  border: none;
  font-size: 0.75rem;
  font-weight: 600;
  color: #64748b;
  padding: 4px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.filter-btn.active {
  background: white;
  color: #be123c;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.search-box {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  padding: 4px 10px;
}

.search-input {
  border: none;
  outline: none;
  background: transparent;
  font-size: 0.8rem;
  color: #334155;
  width: 220px;
}

.table-responsive {
  max-height: 420px;
  overflow-y: auto;
}

.styled-table {
  width: 100%;
  border-collapse: collapse;
}

.styled-table th {
  position: sticky;
  top: 0;
  background: #f8fafc;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  color: #64748b;
  padding: 10px 14px;
  border-bottom: 1px solid #e2e8f0;
  z-index: 10;
}

.styled-table td {
  padding: 10px 14px;
  border-bottom: 1px solid #f1f5f9;
  font-size: 0.82rem;
  color: #334155;
}

.styled-table tr:hover td {
  background: #fafafa;
}

.font-mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.text-center {
  text-align: center;
}

.text-muted {
  color: #94a3b8;
}

.type-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 700;
}

.badge-prestamo {
  background: #ffe4e6;
  color: #be123c;
}

.badge-devolucion {
  background: #d1fae5;
  color: #059669;
}

.book-title {
  font-weight: 600;
  color: #1e293b;
}

.book-code {
  font-size: 0.72rem;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 4px;
  margin-top: 2px;
}

.student-name {
  font-weight: 600;
  color: #1e293b;
}

.student-code {
  font-size: 0.72rem;
  color: #64748b;
}

.due-date {
  font-size: 0.8rem;
  color: #475569;
  font-weight: 500;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.72rem;
  font-weight: 600;
}

.badge-ok {
  background: #ecfdf5;
  color: #059669;
}

.badge-info {
  background: #eff6ff;
  color: #2563eb;
}

.badge-warning {
  background: #fef2f2;
  color: #dc2626;
}

.empty-table-state {
  padding: 2.5rem;
  text-align: center;
  color: #94a3b8;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
}

/* Ranking Items */
.ranking-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.ranking-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 10px;
  border-radius: 8px;
  background: #f8fafc;
  transition: background 0.15s;
}

.ranking-item:hover {
  background: #f1f5f9;
}

.ranking-pos {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #e2e8f0;
  color: #475569;
  font-size: 0.75rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.ranking-info {
  flex: 1;
  min-width: 0;
}

.ranking-title {
  font-size: 0.82rem;
  font-weight: 600;
  color: #1e293b;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.ranking-code {
  font-size: 0.7rem;
  color: #64748b;
}

.ranking-val {
  font-size: 0.78rem;
  font-weight: 700;
  color: #be123c;
  flex-shrink: 0;
}

.loading-overlay {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 50vh;
  color: #6b7280;
  gap: 12px;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid #f3f4f6;
  border-top: 3px solid #be123c;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}
</style>

