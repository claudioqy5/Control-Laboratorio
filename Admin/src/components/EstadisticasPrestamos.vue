<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'
import { Bar, Doughnut, Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  BarElement,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  ArcElement,
  Filler
} from 'chart.js'

ChartJS.register(
  Title,
  Tooltip,
  Legend,
  BarElement,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  ArcElement,
  Filler
)

const statsData = ref(null)
const loading = ref(true)

// Helper para fecha de Perú (UTC-5)
const getPeruDate = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' }
  return new Intl.DateTimeFormat('en-CA', options).format(new Date())
}

// Filtro por Semana, Mes o Año
const filterMode = ref('month') // 'week' | 'month' | 'year'

const monthsNames = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
]

const startYear = 2024
const currentSysYear = new Date().getFullYear()
const availableYears = Array.from(
  { length: Math.max(5, currentSysYear - startYear + 3) },
  (_, i) => startYear + i
)

const selectedMonth = ref(new Date().getMonth() + 1)
const selectedYear = ref(new Date().getFullYear())
const selectedDate = ref(getPeruDate())

// Helper de rango de semana (Lunes a Domingo)
const getWeekRange = (dateStr) => {
  if (!dateStr) dateStr = getPeruDate()
  const [y, m, d] = dateStr.split('-').map(Number)
  const curr = new Date(y, m - 1, d)
  const day = curr.getDay() // 0 = Domingo, 1 = Lunes, ..., 6 = Sábado
  
  // Calcular Lunes
  const monday = new Date(curr)
  monday.setDate(curr.getDate() - (day === 0 ? 6 : day - 1))
  monday.setHours(0, 0, 0, 0)
  
  // Calcular Domingo
  const sunday = new Date(monday)
  sunday.setDate(monday.getDate() + 6)
  sunday.setHours(23, 59, 59, 999)

  const pad = (n) => String(n).padStart(2, '0')
  const toISO = (dt) => `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}`

  const monStr = `${pad(monday.getDate())}/${pad(monday.getMonth() + 1)}`
  const sunStr = `${pad(sunday.getDate())}/${pad(sunday.getMonth() + 1)}/${sunday.getFullYear()}`

  return {
    monday,
    sunday,
    mondayISO: toISO(monday),
    sundayISO: toISO(sunday),
    label: `${monStr} - ${sunStr}`
  }
}

// Navegación de períodos con flechas
const changePeriod = (offset) => {
  if (filterMode.value === 'week') {
    const [y, m, d] = selectedDate.value.split('-').map(Number)
    const curr = new Date(y, m - 1, d)
    curr.setDate(curr.getDate() + offset * 7)
    const pad = (n) => String(n).padStart(2, '0')
    selectedDate.value = `${curr.getFullYear()}-${pad(curr.getMonth() + 1)}-${pad(curr.getDate())}`
    loadStats()
  } else if (filterMode.value === 'month') {
    let m = selectedMonth.value + offset
    let y = selectedYear.value
    if (m > 12) {
      m = 1
      y++
    } else if (m < 1) {
      m = 12
      y--
    }
    selectedMonth.value = m
    selectedYear.value = y
    loadStats()
  } else if (filterMode.value === 'year') {
    selectedYear.value += offset
    loadStats()
  }
}

const setFilterMode = (mode) => {
  if (filterMode.value === mode) return
  filterMode.value = mode
  loadStats()
}

const currentPeriodDisplay = computed(() => {
  if (filterMode.value === 'week') {
    const range = getWeekRange(selectedDate.value)
    return range.label
  } else if (filterMode.value === 'year') {
    return `Año ${selectedYear.value}`
  } else {
    const mName = monthsNames[selectedMonth.value - 1] || ''
    return `${mName} ${selectedYear.value}`
  }
})

const weekInputRef = ref(null)
const triggerDatePicker = () => {
  if (weekInputRef.value) {
    if (typeof weekInputRef.value.showPicker === 'function') {
      weekInputRef.value.showPicker()
    } else {
      weekInputRef.value.focus()
    }
  }
}

const periodPrestamosLabel = computed(() => {
  if (filterMode.value === 'week') return 'Préstamos Semana'
  if (filterMode.value === 'year') return 'Préstamos Año'
  return 'Préstamos del Mes'
})

const periodDevolucionesLabel = computed(() => {
  if (filterMode.value === 'week') return 'Devoluciones Semana'
  if (filterMode.value === 'year') return 'Devoluciones Año'
  return 'Devoluciones del Mes'
})

// Cargar estadísticas desde el Backend
const loadStats = async () => {
  loading.value = true
  try {
    const params = new URLSearchParams()
    params.append('mode', filterMode.value)

    if (filterMode.value === 'week') {
      const weekRange = getWeekRange(selectedDate.value)
      params.append('date', weekRange.mondayISO)
    } else if (filterMode.value === 'year') {
      params.append('year', selectedYear.value)
    } else {
      params.append('month', selectedMonth.value)
      params.append('year', selectedYear.value)
      if (selectedDate.value) params.append('date', selectedDate.value)
    }

    const res = await axios.get(`${API_BASE_URL}/api/Prestamos/Estadisticas?${params.toString()}`)
    statsData.value = res.data

    // Fallback de compatibilidad: si el backend remoto responde en modo mes al pedir 'week'
    if (filterMode.value === 'week') {
      const weekRange = getWeekRange(selectedDate.value)
      const flujoData = statsData.value.tendenciaFlujo || statsData.value.TendenciaFlujo || []
      const isRemoteMonthFallback = flujoData.length > 7 && (flujoData[0]?.etiqueta || flujoData[0]?.Etiqueta || '').startsWith('Día')

      if (isRemoteMonthFallback || flujoData.length === 0) {
        try {
          const prestamosRes = await axios.get(`${API_BASE_URL}/api/Prestamos`)
          const allPrestamos = prestamosRes.data || []
          
          const startMonday = new Date(weekRange.mondayISO + 'T00:00:00')
          const weekDays = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom']
          const flujo = []
          
          let prestamosPeriodoCount = 0
          let devolucionesPeriodoCount = 0
          let devueltosATiempoCount = 0
          let devueltosConFechaCount = 0
          
          const librosMap = {}
          const catMap = {}
          const horasCount = {}
          for (let h = 8; h <= 20; h++) {
            horasCount[`${h.toString().padStart(2, '0')}:00`] = 0
          }

          for (let i = 0; i < 7; i++) {
            const currentDay = new Date(startMonday)
            currentDay.setDate(startMonday.getDate() + i)
            const dayISO = `${currentDay.getFullYear()}-${String(currentDay.getMonth() + 1).padStart(2, '0')}-${String(currentDay.getDate()).padStart(2, '0')}`
            const dayLabel = `${weekDays[i]} ${String(currentDay.getDate()).padStart(2, '0')}/${String(currentDay.getMonth() + 1).padStart(2, '0')}`

            let pCount = 0
            let dCount = 0

            allPrestamos.forEach(p => {
              const pDate = p.fechaPrestamo ? p.fechaPrestamo.split('T')[0] : ''
              const dDate = p.fechaEntregado ? p.fechaEntregado.split('T')[0] : ''

              if (pDate === dayISO) {
                pCount++
                prestamosPeriodoCount++
                const tit = p.libroTitulo || 'Sin título'
                librosMap[tit] = (librosMap[tit] || 0) + 1
                const cat = p.libroCategoria || 'General'
                catMap[cat] = (catMap[cat] || 0) + 1
                if (p.fechaPrestamo) {
                  const hour = new Date(p.fechaPrestamo).getHours()
                  const hKey = `${hour.toString().padStart(2, '0')}:00`
                  if (horasCount[hKey] !== undefined) horasCount[hKey]++
                }
              }

              if (dDate === dayISO) {
                dCount++
                devolucionesPeriodoCount++
                devueltosConFechaCount++
                if (p.fechaDevolucion && new Date(p.fechaEntregado) <= new Date(p.fechaDevolucion)) {
                  devueltosATiempoCount++
                }
                if (p.fechaEntregado) {
                  const hour = new Date(p.fechaEntregado).getHours()
                  const hKey = `${hour.toString().padStart(2, '0')}:00`
                  if (horasCount[hKey] !== undefined) horasCount[hKey]++
                }
              }
            })

            flujo.push({
              etiqueta: dayLabel,
              prestamos: pCount,
              devoluciones: dCount
            })
          }

          statsData.value.tendenciaFlujo = flujo
          if (statsData.value.kpis || statsData.value.Kpis) {
            const k = statsData.value.kpis || statsData.value.Kpis
            k.totalPrestamosPeriodo = prestamosPeriodoCount
            k.totalDevueltosPeriodo = devolucionesPeriodoCount
            k.tasaATiempo = devueltosConFechaCount > 0 
              ? Math.round((devueltosATiempoCount / devueltosConFechaCount) * 100) 
              : 100
          }

          if (prestamosPeriodoCount > 0) {
            statsData.value.topLibros = Object.entries(librosMap)
              .map(([titulo, total]) => ({ titulo, total }))
              .sort((a, b) => b.total - a.total)
              .slice(0, 10)

            statsData.value.porCategoria = Object.entries(catMap)
              .map(([categoria, cantidad]) => ({ categoria, cantidad }))
              .sort((a, b) => b.cantidad - a.cantidad)
              .slice(0, 8)
          }

          statsData.value.horasPunta = Object.entries(horasCount).map(([hora, totalMovimientos]) => ({
            hora,
            totalMovimientos
          }))
        } catch (fbErr) {
          console.warn('Fallback semanal no disponible:', fbErr)
        }
      }
    }
  } catch (err) {
    console.error('Error cargando estadísticas de préstamos:', err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadStats()
})

// KPIs Computados normalizados
const kpis = computed(() => {
  const k = statsData.value?.kpis || statsData.value?.Kpis || {}
  return {
    totalActivos: k.totalActivos ?? k.TotalActivos ?? 0,
    totalVencidos: k.totalVencidos ?? k.TotalVencidos ?? 0,
    porVencerHoy: k.porVencerHoy ?? k.PorVencerHoy ?? 0,
    tasaATiempo: k.tasaATiempo ?? k.TasaATiempo ?? 100,
    totalPrestamosPeriodo: k.totalPrestamosPeriodo ?? k.TotalPrestamosPeriodo ?? 0,
    totalDevueltosPeriodo: k.totalDevueltosPeriodo ?? k.TotalDevueltosPeriodo ?? 0,
    totalCatalogo: k.totalCatalogo ?? k.TotalCatalogo ?? 0,
    librosPrestados: k.librosPrestados ?? k.LibrosPrestados ?? 0
  }
})

// ==========================================
// CONFIGURACIÓN DE GRÁFICOS
// ==========================================

// 1. Gráfico de Tendencia (Líneas Doble: Préstamos vs Devoluciones)
const flujoChartData = computed(() => {
  const items = statsData.value?.tendenciaFlujo || statsData.value?.TendenciaFlujo
  if (!items || items.length === 0) return null
  return {
    labels: items.map(i => i.etiqueta || i.Etiqueta),
    datasets: [
      {
        label: 'Préstamos Realizados',
        data: items.map(i => i.prestamos ?? i.Prestamos ?? 0),
        borderColor: '#9f1239', // Borgoña institucional
        backgroundColor: 'rgba(159, 18, 57, 0.12)',
        borderWidth: 2.2,
        tension: 0.35,
        fill: true,
        pointBackgroundColor: '#9f1239',
        pointRadius: items.length > 20 ? 2 : 4,
        pointHoverRadius: 6
      },
      {
        label: 'Devoluciones Registradas',
        data: items.map(i => i.devoluciones ?? i.Devoluciones ?? 0),
        borderColor: '#059669', // Verde esmeralda
        backgroundColor: 'rgba(5, 150, 105, 0.1)',
        borderWidth: 2.2,
        tension: 0.35,
        fill: true,
        pointBackgroundColor: '#059669',
        pointRadius: items.length > 20 ? 2 : 4,
        pointHoverRadius: 6
      }
    ]
  }
})

const flujoChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: {
      position: 'top',
      align: 'end',
      labels: {
        usePointStyle: true,
        pointStyle: 'circle',
        boxWidth: 8,
        font: { size: 11, family: 'system-ui, sans-serif', weight: 600 },
        color: '#475569'
      }
    },
    tooltip: {
      backgroundColor: '#0f172a',
      titleFont: { size: 12, weight: 700 },
      bodyFont: { size: 11 },
      padding: 10,
      cornerRadius: 8
    }
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { font: { size: 10 }, color: '#64748b', maxRotation: 45 }
    },
    y: {
      beginAtZero: true,
      grid: { color: '#f1f5f9', drawBorder: false },
      ticks: { precision: 0, font: { size: 10 }, color: '#64748b' }
    }
  }
}

// 2. Gráfico Top 10 Libros (Barras Horizontales)
const topLibrosChartData = computed(() => {
  const items = statsData.value?.topLibros || statsData.value?.TopLibros
  if (!items || items.length === 0) return null
  return {
    labels: items.map(l => {
      const tit = l.titulo || l.Titulo || ''
      return tit.length > 28 ? tit.substring(0, 28) + '...' : tit
    }),
    datasets: [
      {
        label: 'Préstamos',
        data: items.map(l => l.total ?? l.Total ?? 0),
        backgroundColor: 'rgba(159, 18, 57, 0.85)',
        hoverBackgroundColor: '#881337',
        borderRadius: 5,
        barThickness: 14
      }
    ]
  }
})

const topLibrosChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        title: (context) => {
          const index = context[0].dataIndex
          const items = statsData.value?.topLibros || statsData.value?.TopLibros || []
          return items[index]?.titulo || items[index]?.Titulo || ''
        }
      }
    }
  },
  scales: {
    x: {
      beginAtZero: true,
      grid: { color: '#f1f5f9' },
      ticks: { precision: 0, font: { size: 9 }, color: '#64748b' }
    },
    y: {
      grid: { display: false },
      ticks: { font: { size: 10, weight: 500 }, color: '#1e293b' }
    }
  }
}

// 3. Gráfico de Categorías Médicas (Dona)
const categoriaChartData = computed(() => {
  const items = statsData.value?.porCategoria || statsData.value?.PorCategoria
  if (!items || items.length === 0) return null
  const palette = ['#9f1239', '#2563eb', '#059669', '#d97706', '#7c3aed', '#0891b2', '#ea580c', '#64748b']
  return {
    labels: items.map(c => c.categoria || c.Categoria),
    datasets: [
      {
        data: items.map(c => c.cantidad ?? c.Cantidad ?? 0),
        backgroundColor: palette.slice(0, items.length),
        borderWidth: 2,
        borderColor: '#ffffff',
        hoverOffset: 6
      }
    ]
  }
})

const categoriaChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '68%',
  plugins: {
    legend: {
      position: 'right',
      labels: {
        usePointStyle: true,
        pointStyle: 'circle',
        boxWidth: 7,
        font: { size: 10.5, family: 'system-ui, sans-serif' },
        color: '#334155',
        padding: 10
      }
    }
  }
}

// 4. Gráfico Horas Punta (Barras Verticales)
const horasChartData = computed(() => {
  const items = statsData.value?.horasPunta || statsData.value?.HorasPunta
  if (!items || items.length === 0) return null
  return {
    labels: items.map(h => h.hora || h.Hora),
    datasets: [
      {
        label: 'Movimientos (Préstamos + Devoluciones)',
        data: items.map(h => h.totalMovimientos ?? h.TotalMovimientos ?? 0),
        backgroundColor: 'rgba(37, 99, 235, 0.8)',
        hoverBackgroundColor: '#1d4ed8',
        borderRadius: 4,
        barThickness: 16
      }
    ]
  }
})

const horasChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false }
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { font: { size: 10 }, color: '#64748b' }
    },
    y: {
      beginAtZero: true,
      grid: { color: '#f1f5f9' },
      ticks: { precision: 0, font: { size: 10 }, color: '#64748b' }
    }
  }
}

// 5. Gráfico de Cumplimiento / Morosidad (Dona)
const cumplimientoChartData = computed(() => {
  const c = statsData.value?.cumplimiento || statsData.value?.Cumplimiento
  if (!c) return null
  const devATiempo = c.devueltosATiempo ?? c.DevueltosATiempo ?? 0
  const devConRetraso = c.devueltosConRetraso ?? c.DevueltosConRetraso ?? 0
  const actEnPlazo = c.activosEnPlazo ?? c.ActivosEnPlazo ?? 0
  const actVencidos = c.activosVencidos ?? c.ActivosVencidos ?? 0

  const total = devATiempo + devConRetraso + actEnPlazo + actVencidos
  if (total === 0) return null

  return {
    labels: ['Devuelto a Tiempo', 'Devuelto con Retraso', 'Activo en Plazo', 'Activo Vencido'],
    datasets: [
      {
        data: [devATiempo, devConRetraso, actEnPlazo, actVencidos],
        backgroundColor: ['#059669', '#d97706', '#2563eb', '#dc2626'],
        borderWidth: 2,
        borderColor: '#ffffff',
        hoverOffset: 6
      }
    ]
  }
})

const cumplimientoChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '68%',
  plugins: {
    legend: {
      position: 'right',
      labels: {
        usePointStyle: true,
        pointStyle: 'circle',
        boxWidth: 7,
        font: { size: 10.5, family: 'system-ui, sans-serif' },
        color: '#334155',
        padding: 8
      }
    }
  }
}

defineExpose({
  loadStats,
  filterMode,
  selectedMonth,
  selectedYear,
  selectedDate,
  setFilterMode,
  changePeriod,
  triggerDatePicker,
  weekInputRef,
  kpis,
  statsData,
  loading
})
</script>

<template>
  <div class="stats-prestamos-page">
    <!-- CABECERA CON FILTRO POR SEMANA, MES O AÑO -->
    <div class="page-top-bar">
      <div class="header-titles">        
        <h1 class="main-title">Estadísticas de Préstamos y Circulación</h1>
        <p class="subtitle">Análisis métrico y analítica de libros sincronizados desde AbsysNet</p>
      </div>

      <!-- FILTRO DE SEMANA, MES O AÑO -->
      <div class="filter-controls-group">
        <!-- SELECTOR DE MODO: Semana | Mes | Año -->
        <div class="mode-pill-selector">
          <button
            type="button"
            :class="['mode-pill-btn', { active: filterMode === 'week' }]"
            @click="setFilterMode('week')"
            data-test="btn-filter-week"
          >
            Semana
          </button>
          <button
            type="button"
            :class="['mode-pill-btn', { active: filterMode === 'month' }]"
            @click="setFilterMode('month')"
            data-test="btn-filter-month"
          >
            Mes
          </button>
          <button
            type="button"
            :class="['mode-pill-btn', { active: filterMode === 'year' }]"
            @click="setFilterMode('year')"
            data-test="btn-filter-year"
          >
            Año
          </button>
        </div>

        <!-- BARRA NAVEGADORA DE PERÍODO (‹ Actual ›) -->
        <div class="period-navigator-bar">
          <button
            type="button"
            class="nav-arrow-btn"
            @click="changePeriod(-1)"
            title="Período anterior"
            data-test="btn-period-prev"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
              <polyline points="15 18 9 12 15 6"></polyline>
            </svg>
          </button>

          <!-- VISTA SEMANA: Rango de lunes a domingo compacto sin fecha duplicada -->
          <div
            class="period-display-box clickable-period"
            v-if="filterMode === 'week'"
            @click="triggerDatePicker"
            title="Clic para seleccionar otra fecha"
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#be123c" stroke-width="2">
              <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
              <line x1="16" y1="2" x2="16" y2="6"></line>
              <line x1="8" y1="2" x2="8" y2="6"></line>
              <line x1="3" y1="10" x2="21" y2="10"></line>
            </svg>
            <span class="period-text" data-test="period-display">{{ currentPeriodDisplay }}</span>
            <input
              ref="weekInputRef"
              type="date"
              v-model="selectedDate"
              @change="loadStats"
              class="hidden-native-input"
              tabindex="-1"
              aria-hidden="true"
            />
          </div>

          <!-- VISTA MES: Selector de Mes y Año -->
          <div class="period-display-box" v-else-if="filterMode === 'month'">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#be123c" stroke-width="2">
              <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
              <line x1="16" y1="2" x2="16" y2="6"></line>
              <line x1="8" y1="2" x2="8" y2="6"></line>
              <line x1="3" y1="10" x2="21" y2="10"></line>
            </svg>
            <select v-model="selectedMonth" @change="loadStats" class="period-select" data-test="select-month">
              <option v-for="(m, idx) in monthsNames" :key="idx" :value="idx + 1">
                {{ m }}
              </option>
            </select>
            <select v-model="selectedYear" @change="loadStats" class="period-select" data-test="select-year">
              <option v-for="y in availableYears" :key="y" :value="y">{{ y }}</option>
            </select>
          </div>

          <!-- VISTA AÑO: Selector de Año -->
          <div class="period-display-box" v-else>
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#be123c" stroke-width="2">
              <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
              <line x1="16" y1="2" x2="16" y2="6"></line>
              <line x1="8" y1="2" x2="8" y2="6"></line>
              <line x1="3" y1="10" x2="21" y2="10"></line>
            </svg>
            <select v-model="selectedYear" @change="loadStats" class="period-select font-bold" data-test="select-year-mode">
              <option v-for="y in availableYears" :key="y" :value="y">Año {{ y }}</option>
            </select>
          </div>

          <button
            type="button"
            class="nav-arrow-btn"
            @click="changePeriod(1)"
            title="Período siguiente"
            data-test="btn-period-next"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
              <polyline points="9 18 15 12 9 6"></polyline>
            </svg>
          </button>
        </div>

        <!-- BOTÓN REFRESH -->
        <button
          class="btn-refresh-stats"
          @click="loadStats"
          :disabled="loading"
          title="Actualizar datos"
          data-test="btn-refresh-stats"
        >
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.2"
            stroke-linecap="round"
            stroke-linejoin="round"
            :class="{ 'spin': loading }"
          >
            <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- SPINNER DE CARGA -->
    <div v-if="loading && !statsData" class="loading-state">
      <div class="spinner"></div>
      <p>Cargando analítica de préstamos...</p>
    </div>

    <div v-else-if="statsData" class="stats-content">
      <!-- FILA 1: CARDS ESTILO RESUMEN PRESTAMOS (5 KPIs) -->
      <div class="stats-grid">
        <!-- 1. Préstamos en Período -->
        <div class="stat-card">
          <div class="stat-icon red">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z" />
              <path d="M6 6h10" />
              <path d="M6 10h10" />
            </svg>
          </div>
          <div>
            <div class="stat-label">{{ periodPrestamosLabel }}</div>
            <div class="stat-value" data-test="kpi-prestamos-periodo">{{ kpis.totalPrestamosPeriodo }}</div>
            <div class="stat-sub">Registrados en el período</div>
          </div>
        </div>

        <!-- 2. Devoluciones en Período -->
        <div class="stat-card">
          <div class="stat-icon green">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M9 11l3 3L22 4" />
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
            </svg>
          </div>
          <div>
            <div class="stat-label">{{ periodDevolucionesLabel }}</div>
            <div class="stat-value" data-test="kpi-devoluciones-periodo">{{ kpis.totalDevueltosPeriodo }}</div>
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
              {{ kpis.totalActivos }} <small v-if="kpis.totalCatalogo">/ {{ kpis.totalCatalogo }}</small>
            </div>
            <div class="stat-sub">En sala o a domicilio</div>
          </div>
        </div>

        <!-- 4. Préstamos Vencidos -->
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

        <!-- 5. Devolución a Tiempo -->
        <div class="stat-card">
          <div class="stat-icon blue">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
              <path d="m9 12 2 2 4-4" />
            </svg>
          </div>
          <div>
            <div class="stat-label">Devolución a Tiempo</div>
            <div class="stat-value" data-test="kpi-tasa">{{ kpis.tasaATiempo }}%</div>
            <div class="stat-sub">Tasa de cumplimiento en período</div>
          </div>
        </div>
      </div>

      <!-- FILA 2: GRÁFICO DE TENDENCIA (LÍNEA DOBLE) -->
      <div class="chart-panel full-width">
        <div class="panel-header">
          <div>
            <h2 class="panel-title">Flujo Temporal: Préstamos vs. Devoluciones</h2>
            <p class="panel-subtitle">Comparativa cronológica de salidas y retornos de material bibliográfico</p>
          </div>
          <span class="panel-pill">Serie Temporal</span>
        </div>
        <div class="panel-body chart-tall">
          <Line v-if="flujoChartData" :data="flujoChartData" :options="flujoChartOptions" />
          <div v-else class="empty-chart">No hay movimientos registrados en este período</div>
        </div>
      </div>

      <!-- FILA 3: TOP 10 LIBROS + HORAS PUNTA -->
      <div class="charts-row-split">
        <!-- Top 10 Libros Más Prestados -->
        <div class="chart-panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">Top 10 Libros Más Solicitados</h2>
              <p class="panel-subtitle">Títulos con mayor demanda en el período</p>
            </div>
            <div class="panel-icon-badge">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#9f1239" stroke-width="2">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
          </div>
          <div class="panel-body chart-medium">
            <Bar v-if="topLibrosChartData" :data="topLibrosChartData" :options="topLibrosChartOptions" />
            <div v-else class="empty-chart">Sin registros de préstamos en este rango</div>
          </div>
        </div>

        <!-- Horas de Mayor Afluencia -->
        <div class="chart-panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">Horarios de Mayor Tráfico</h2>
              <p class="panel-subtitle">Distribución por hora de atención en mostrador (08:00 a 20:00)</p>
            </div>
            <div class="panel-icon-badge">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="#2563eb" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <polyline points="12 6 12 12 16 14"/>
              </svg>
            </div>
          </div>
          <div class="panel-body chart-medium">
            <Bar v-if="horasChartData" :data="horasChartData" :options="horasChartOptions" />
            <div v-else class="empty-chart">Sin actividad en este rango horario</div>
          </div>
        </div>
      </div>

      <!-- FILA 4: DISTRIBUCIÓN POR CATEGORÍAS + ESTADO DE CUMPLIMIENTO -->
      <div class="charts-row-split">
        <!-- Donut Categorías Médicas -->
        <div class="chart-panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">Distribución por Especialidad</h2>
              <p class="panel-subtitle">Porcentaje de préstamos según especialidad</p>
            </div>
            <span class="panel-pill">Especialidad</span>
          </div>
          <div class="panel-body chart-medium flex-center">
            <Doughnut v-if="categoriaChartData" :data="categoriaChartData" :options="categoriaChartOptions" />
            <div v-else class="empty-chart">Sin categorías registradas en este período</div>
          </div>
        </div>

        <!-- Donut Cumplimiento / Morosidad -->
        <div class="chart-panel">
          <div class="panel-header">
            <div>
              <h2 class="panel-title">Estado de Cumplimiento</h2>
              <p class="panel-subtitle">Puntualidad en devoluciones y alertas de mora</p>
            </div>
            <span class="panel-pill">Seguimiento</span>
          </div>
          <div class="panel-body chart-medium flex-center">
            <Doughnut v-if="cumplimientoChartData" :data="cumplimientoChartData" :options="cumplimientoChartOptions" />
            <div v-else class="empty-chart">Sin datos de devolución en este período</div>
          </div>
        </div>
      </div>

      <!-- FILA 5: TOP ALUMNOS LECTORES (RANKING PODIO) -->
      <div class="chart-panel full-width">
        <div class="panel-header">
          <div>
            <h2 class="panel-title">Top Estudiantes Lectores del Período</h2>
            <p class="panel-subtitle">Alumnos con mayor aprovechamiento de la bibliografía médica física</p>
          </div>
          <span class="panel-pill">Ranking</span>
        </div>
        <div class="panel-body p-0">
          <div v-if="(statsData.topAlumnos || statsData.TopAlumnos) && (statsData.topAlumnos || statsData.TopAlumnos).length > 0" class="ranking-table-wrap">
            <table class="ranking-table">
              <thead>
                <tr>
                  <th style="width: 60px;">Puesto</th>
                  <th>Código Universitario</th>
                  <th>Estudiante</th>
                  <th>Carrera</th>
                  <th style="text-align: right;">Total Préstamos</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(alumno, idx) in (statsData.topAlumnos || statsData.TopAlumnos)" :key="alumno.alumnoID || alumno.AlumnoID">
                  <td>
                    <span :class="['rank-badge', `rank-${idx + 1}`]">
                      #{{ idx + 1 }}
                    </span>
                  </td>
                  <td class="font-mono text-bold">{{ alumno.codigoUniversitario || alumno.CodigoUniversitario }}</td>
                  <td>
                    <div class="student-name-cell">
                      <div class="student-avatar-mini">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                          <circle cx="12" cy="7" r="4"/>
                        </svg>
                      </div>
                      <span>{{ alumno.nombre || alumno.Nombre }}</span>
                    </div>
                  </td>
                  <td>
                    <span class="career-pill">{{ alumno.carrera || alumno.Carrera || 'Medicina Humana' }}</span>
                  </td>
                  <td style="text-align: right;">
                    <span class="loans-count-pill">{{ alumno.totalPrestamos ?? alumno.TotalPrestamos }} libros</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="empty-chart" style="padding: 30px;">
            No se registran alumnos con préstamos en este rango de tiempo.
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.stats-prestamos-page {  
  background: #ffffffff;
  min-height: 100vh;
  box-sizing: border-box;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
  color: #0f172a;
}

.stats-prestamos-page.is-embedded {
  padding: 0;
  background: transparent;
  min-height: auto;
}

/* TOP BAR */
.page-top-bar {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 16px;
}

.badge-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #fff1f2;
  color: #9f1239;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  margin-bottom: 6px;
}

.main-title {
  font-size: 1.5rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  line-height: 1.2;
}

.subtitle {
  font-size: 0.85rem;
  color: #64748b;
  margin: 4px 0 0 0;
}

/* CONTROLES DE FILTRO POR SEMANA, MES O AÑO */
.filter-controls-group {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.mode-pill-selector {
  display: flex;
  background: #f1f5f9;
  border-radius: 9999px;
  padding: 3px;
  border: 1px solid #e2e8f0;
}

.mode-pill-btn {
  border: none;
  background: transparent;
  padding: 6px 14px;
  border-radius: 9999px;
  font-size: 0.8rem;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  transition: all 0.2s ease;
}

.mode-pill-btn:hover:not(.active) {
  color: #0f172a;
}

.mode-pill-btn.active {
  background: #9f1239;
  color: #ffffff;
  box-shadow: 0 1px 3px rgba(159, 18, 57, 0.3);
}

.period-navigator-bar {
  display: flex;
  align-items: center;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 3px 6px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);
}

.nav-arrow-btn {
  background: transparent;
  border: none;
  padding: 6px 8px;
  border-radius: 6px;
  cursor: pointer;
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s, color 0.15s;
}

.nav-arrow-btn:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.period-display-box {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 10px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #1e293b;
}

.period-text {
  font-size: 0.85rem;
  font-weight: 600;
  color: #1e293b;
  white-space: nowrap;
}

.period-select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 0.85rem;
  font-weight: 600;
  color: #1e293b;
  cursor: pointer;
  padding: 2px 4px;
}

.period-select:focus {
  background: #f8fafc;
  border-radius: 4px;
}

.clickable-period {
  cursor: pointer;
  border-radius: 6px;
  transition: background 0.15s ease;
  user-select: none;
}

.clickable-period:hover {
  background: #f1f5f9;
}

.hidden-native-input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
  pointer-events: none;
  border: none;
  padding: 0;
  margin: 0;
}

.btn-refresh-stats {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  color: #4b5563;
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-refresh-stats:hover:not(:disabled) {
  background: #f9fafb;
  color: #111827;
  border-color: #cbd5e1;
}

/* ==========================================
   CARDS DE ESTADÍSTICAS (ESTILO RESUMEN PRESTAMOS)
   ========================================== */
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
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
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
  letter-spacing: 0.02em;
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

/* PANELES DE GRÁFICOS */
.chart-panel {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  margin-bottom: 24px;
}



.charts-row-split {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(420px, 1fr));
  gap: 20px;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
}

.panel-title {
  font-size: 1rem;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}

.panel-subtitle {
  font-size: 0.78rem;
  color: #64748b;
  margin: 3px 0 0 0;
}

.panel-pill {
  background: #f1f5f9;
  color: #475569;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 600;
}

.panel-icon-badge {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  background: #f8fafc;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #f1f5f9;
}

.panel-body {
  position: relative;
}

.chart-tall {
  height: 280px;
}

.chart-medium {
  height: 240px;
}

.flex-center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-chart {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #94a3b8;
  font-size: 0.85rem;
  font-style: italic;
}

.p-0 {
  padding: 0 !important;
}

/* TABLA DE RANKING */
.ranking-table-wrap {
  overflow-x: auto;
}

.ranking-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.82rem;
}

.ranking-table th {
  background: #f8fafc;
  color: #475569;
  font-weight: 600;
  text-align: left;
  padding: 10px 16px;
  border-bottom: 1px solid #e2e8f0;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.ranking-table td {
  padding: 12px 16px;
  border-bottom: 1px solid #f1f5f9;
  color: #1e293b;
}

.ranking-table tr:last-child td {
  border-bottom: none;
}

.ranking-table tr:hover {
  background: #f8fafc;
}

.rank-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border-radius: 6px;
  font-weight: 800;
  font-size: 0.75rem;
  background: #f1f5f9;
  color: #475569;
}

.rank-1 { background: #fef3c7; color: #b45309; }
.rank-2 { background: #f1f5f9; color: #475569; font-weight: 700; }
.rank-3 { background: #ffedd5; color: #c2410c; }

.font-mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.text-bold {
  font-weight: 700;
}

.student-name-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.student-avatar-mini {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #fff1f2;
  color: #9f1239;
  display: flex;
  align-items: center;
  justify-content: center;
}

.career-pill {
  background: #f1f5f9;
  color: #334155;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
}

.loans-count-pill {
  background: #fff1f2;
  color: #9f1239;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 0.75rem;
}

/* LOADING & SPINNER */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 80px 20px;
  color: #64748b;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid #e2e8f0;
  border-top-color: #9f1239;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 12px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.spin {
  animation: spin 0.8s linear infinite;
}

@media (max-width: 900px) {
  .charts-row-split {
    grid-template-columns: 1fr;
  }
}
</style>
