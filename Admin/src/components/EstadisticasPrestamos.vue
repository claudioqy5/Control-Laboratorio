<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'
import FiltroEstadisticas from './FiltroEstadisticas.vue'
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

const props = defineProps({
  hideMainHeader: {
    type: Boolean,
    default: false
  }
})

const statsData = ref(null)
const loading = ref(true)

// Estado del filtro
const getPeruDate = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' }
  return new Intl.DateTimeFormat('en-CA', options).format(new Date())
}

const currentMonth = new Date().getMonth() + 1
const currentYear = new Date().getFullYear()

const selectedMonth = ref(currentMonth)
const selectedYear = ref(currentYear)
const selectedDate = ref(getPeruDate())
const filterMode = ref('month')
const customPeriodLabel = ref('')

const onFilterChange = async (filter) => {
  selectedMonth.value = filter.month
  selectedYear.value = filter.year
  selectedDate.value = filter.date
  filterMode.value = filter.mode
  customPeriodLabel.value = filter.label
  await loadStats()
}

const loadStats = async () => {
  loading.value = true
  try {
    const params = new URLSearchParams()
    params.append('mode', filterMode.value)

    if (filterMode.value === 'date') {
      params.append('date', selectedDate.value)
    } else if (filterMode.value === 'year') {
      params.append('year', selectedYear.value)
    } else {
      params.append('month', selectedMonth.value)
      params.append('year', selectedYear.value)
      if (selectedDate.value) params.append('date', selectedDate.value)
    }

    const res = await axios.get(`${API_BASE_URL}/api/Prestamos/Estadisticas?${params.toString()}`)
    statsData.value = res.data
  } catch (err) {
    console.error('Error cargando estadísticas de préstamos:', err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadStats()
})

// ==========================================
// CONFIGURACIÓN DE GRÁFICOS
// ==========================================

// 1. Gráfico de Tendencia (Líneas Doble: Préstamos vs Devoluciones)
const flujoChartData = computed(() => {
  if (!statsData.value?.tendenciaFlujo) return null
  const items = statsData.value.tendenciaFlujo
  return {
    labels: items.map(i => i.etiqueta),
    datasets: [
      {
        label: 'Préstamos Realizados',
        data: items.map(i => i.prestamos),
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
        data: items.map(i => i.devoluciones),
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
  if (!statsData.value?.topLibros || statsData.value.topLibros.length === 0) return null
  const items = statsData.value.topLibros
  return {
    labels: items.map(l => l.titulo.length > 28 ? l.titulo.substring(0, 28) + '...' : l.titulo),
    datasets: [
      {
        label: 'Préstamos',
        data: items.map(l => l.total),
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
          return statsData.value.topLibros[index]?.titulo || ''
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
  if (!statsData.value?.porCategoria || statsData.value.porCategoria.length === 0) return null
  const items = statsData.value.porCategoria
  const palette = ['#9f1239', '#2563eb', '#059669', '#d97706', '#7c3aed', '#0891b2', '#ea580c', '#64748b']
  return {
    labels: items.map(c => c.categoria),
    datasets: [
      {
        data: items.map(c => c.cantidad),
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
  if (!statsData.value?.horasPunta) return null
  const items = statsData.value.horasPunta
  return {
    labels: items.map(h => h.hora),
    datasets: [
      {
        label: 'Movimientos (Préstamos + Devoluciones)',
        data: items.map(h => h.totalMovimientos),
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
  if (!statsData.value?.cumplimiento) return null
  const c = statsData.value.cumplimiento
  const total = c.devueltosATiempo + c.devueltosConRetraso + c.activosEnPlazo + c.activosVencidos
  if (total === 0) return null

  return {
    labels: ['Devuelto a Tiempo', 'Devuelto con Retraso', 'Activo en Plazo', 'Activo Vencido'],
    datasets: [
      {
        data: [c.devueltosATiempo, c.devueltosConRetraso, c.activosEnPlazo, c.activosVencidos],
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
</script>

<template>
  <div class="stats-prestamos-page" :class="{ 'is-embedded': hideMainHeader }">
    <!-- CABECERA -->
    <div class="page-top-bar" :class="{ 'compact-bar': hideMainHeader }">
      <div v-if="!hideMainHeader" class="header-titles">
        <div class="badge-tag">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z"/>
            <path d="M6 6h10"/>
            <path d="M6 10h10"/>
          </svg>
          <span>Biblioteca BVE-FAMURP</span>
        </div>
        <h1 class="main-title">Estadísticas de Préstamos y Circulación</h1>
        <p class="subtitle">Análisis métrico y analítica de libros sincronizados desde AbsysNet</p>
      </div>

      <div v-else class="header-titles-compact">
        <div class="compact-tag">
          <span class="compact-dot"></span>
          <span>Analítica de Préstamos y Circulación de Libros</span>
        </div>
      </div>

      <!-- FILTRO DE FECHAS REUTILIZABLE -->
      <div class="filter-action-wrap">
        <FiltroEstadisticas
          :initial-month="selectedMonth"
          :initial-year="selectedYear"
          :initial-date="selectedDate"
          :initial-mode="filterMode"
          @change="onFilterChange"
        />
        <button class="btn-refresh-stats" @click="loadStats" :disabled="loading" title="Actualizar datos">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" :class="{ 'spin': loading }">
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
      <!-- FILA 1: KPIs SUPERIORES -->
      <div class="kpis-grid">
        <!-- KPI 1: Préstamos Activos -->
        <div class="kpi-card card-active">
          <div class="kpi-header">
            <span class="kpi-label">Préstamos Activos</span>
            <div class="kpi-icon icon-emerald">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <polyline points="12 6 12 12 16 14"/>
              </svg>
            </div>
          </div>
          <div class="kpi-value">{{ statsData.kpis.totalActivos }}</div>
          <div class="kpi-foot">
            <span class="foot-desc">Libros en sala o a domicilio</span>
          </div>
        </div>

        <!-- KPI 2: Préstamos Vencidos -->
        <div class="kpi-card card-alert">
          <div class="kpi-header">
            <span class="kpi-label">Préstamos Vencidos</span>
            <div class="kpi-icon icon-rose">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>
            </div>
          </div>
          <div class="kpi-value text-rose">{{ statsData.kpis.totalVencidos }}</div>
          <div class="kpi-foot">
            <span class="foot-badge badge-rose">{{ statsData.kpis.porVencerHoy }} vencen hoy</span>
          </div>
        </div>

        <!-- KPI 3: Tasa a Tiempo -->
        <div class="kpi-card">
          <div class="kpi-header">
            <span class="kpi-label">Devolución a Tiempo</span>
            <div class="kpi-icon icon-blue">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
              </svg>
            </div>
          </div>
          <div class="kpi-value text-blue">{{ statsData.kpis.tasaATiempo }}%</div>
          <div class="kpi-foot">
            <span class="foot-desc">Tasa de cumplimiento en período</span>
          </div>
        </div>

        <!-- KPI 4: Circulación en Período -->
        <div class="kpi-card">
          <div class="kpi-header">
            <span class="kpi-label">Movimientos Período</span>
            <div class="kpi-icon icon-amber">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/>
                <rect x="8" y="2" width="8" height="4" rx="1" ry="1"/>
                <path d="M9 14l2 2 4-4"/>
              </svg>
            </div>
          </div>
          <div class="kpi-value">{{ statsData.kpis.totalPrestamosPeriodo }} <span class="kpi-sub">préstamos</span></div>
          <div class="kpi-foot">
            <span class="foot-desc">{{ statsData.kpis.totalDevueltosPeriodo }} devoluciones procesadas</span>
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
              <h2 class="panel-title">Distribución por Área Médica</h2>
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
          <div v-if="statsData.topAlumnos && statsData.topAlumnos.length > 0" class="ranking-table-wrap">
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
                <tr v-for="(alumno, idx) in statsData.topAlumnos" :key="alumno.alumnoID">
                  <td>
                    <span :class="['rank-badge', `rank-${idx + 1}`]">
                      #{{ idx + 1 }}
                    </span>
                  </td>
                  <td class="font-mono text-bold">{{ alumno.codigoUniversitario }}</td>
                  <td>
                    <div class="student-name-cell">
                      <div class="student-avatar-mini">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                          <circle cx="12" cy="7" r="4"/>
                        </svg>
                      </div>
                      <span>{{ alumno.nombre }}</span>
                    </div>
                  </td>
                  <td>
                    <span class="career-pill">{{ alumno.carrera || 'Medicina Humana' }}</span>
                  </td>
                  <td style="text-align: right;">
                    <span class="loans-count-pill">{{ alumno.totalPrestamos }} libros</span>
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
  padding: 24px 32px 60px 32px;
  background: #f8fafc;
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

.page-top-bar.compact-bar {
  margin-bottom: 18px;
  align-items: center;
}

.header-titles-compact {
  display: flex;
  align-items: center;
  gap: 10px;
}

.compact-tag {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #475569;
  background: #ffffff;
  padding: 6px 14px;
  border-radius: 9999px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}

.compact-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #be123c;
  box-shadow: 0 0 0 3px rgba(190, 18, 60, 0.15);
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

.filter-action-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-refresh-stats {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  color: #475569;
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-refresh-stats:hover {
  background: #f1f5f9;
  color: #0f172a;
  border-color: #cbd5e1;
}

/* KPIS */
.kpis-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.kpi-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 18px 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  display: flex;
  flex-direction: column;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.kpi-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.07);
}

.kpi-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.kpi-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.kpi-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.icon-emerald { background: #ecfdf5; color: #059669; }
.icon-rose { background: #fff1f2; color: #e11d48; }
.icon-blue { background: #eff6ff; color: #2563eb; }
.icon-amber { background: #fffbeb; color: #d97706; }

.kpi-value {
  font-size: 1.85rem;
  font-weight: 800;
  color: #0f172a;
  line-height: 1.1;
  margin-bottom: 6px;
}

.kpi-sub {
  font-size: 0.85rem;
  font-weight: 500;
  color: #64748b;
}

.text-rose { color: #e11d48; }
.text-blue { color: #2563eb; }
.text-emerald { color: #059669; }

.kpi-foot {
  margin-top: auto;
  font-size: 0.78rem;
  color: #64748b;
}

.foot-desc {
  color: #64748b;
}

.foot-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 600;
  font-size: 0.72rem;
}

.badge-rose {
  background: #fff1f2;
  color: #e11d48;
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

.full-width {
  width: 100%;
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

