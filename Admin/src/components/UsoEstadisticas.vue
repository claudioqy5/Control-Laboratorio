<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'
import { API_BASE_URL } from '../config'
import { Bar, Doughnut } from 'vue-chartjs'
import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, ArcElement } from 'chart.js'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, ArcElement)

const dashboardData = ref(null)
const loaded = ref(false)
const mapModalOpen = ref(false)

// Obtener fecha actual en zona horaria de Perú (UTC-5)
const getPeruDate = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' };
  const formatter = new Intl.DateTimeFormat('en-CA', options); // en-CA da formato YYYY-MM-DD
  return formatter.format(new Date());
}
const selectedDate = ref(getPeruDate())

const getPeruDateLabel = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: 'long', day: 'numeric' };
  const formatter = new Intl.DateTimeFormat('es-PE', options);
  return formatter.format(new Date());
}

const currentMonth = new Date().getMonth() + 1
const currentYear = new Date().getFullYear()

const selectedMonth = ref(currentMonth)
const selectedYear = ref(currentYear)

const months = [
  { value: 1, label: 'Enero' },
  { value: 2, label: 'Febrero' },
  { value: 3, label: 'Marzo' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Mayo' },
  { value: 6, label: 'Junio' },
  { value: 7, label: 'Julio' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Septiembre' },
  { value: 10, label: 'Octubre' },
  { value: 11, label: 'Noviembre' },
  { value: 12, label: 'Diciembre' }
]

const startYear = 2026;
const currentSystemYear = new Date().getFullYear();
const years = Array.from({ length: Math.max(1, currentSystemYear - startYear + 1) }, (_, i) => startYear + i);

const currentPeriodLabel = computed(() => {
  const m = months.find(x => x.value === selectedMonth.value)?.label || ''
  return `${m} ${selectedYear.value}`
})

const getStats = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/stats/dashboard?date=${selectedDate.value}&month=${selectedMonth.value}&year=${selectedYear.value}`)
    dashboardData.value = res.data
    loaded.value = true
  } catch (err) {
    console.error("Error cargando estadísticas", err)
  }
}

onMounted(getStats)

// Helpers de conversión y formato
const formatMinutes = (minutes) => {
  if (!minutes) return '0 min'
  if (minutes < 60) return `${minutes} min`
  const hours = Math.floor(minutes / 60)
  const mins = Math.round(minutes % 60)
  return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`
}

const getPercentage = (value, max) => {
  if (!max || max === 0) return 0
  return Math.min(100, Math.round((value / max) * 100))
}

// Configuración de Gráficos de Alumnos (Barra Horizontal)
const topAlumnosChartData = computed(() => {
  if (!dashboardData.value || !dashboardData.value.topAlumnos) return null
  return {
    labels: dashboardData.value.topAlumnos.map(a => a.nombreCompleto),
    datasets: [{
      label: 'Tiempo (Horas)',
      data: dashboardData.value.topAlumnos.map(a => Math.round((a.totalMinutos / 60) * 10) / 10),
      backgroundColor: 'rgba(245, 158, 11, 0.8)',
      hoverBackgroundColor: 'rgba(217, 119, 6, 1)',
      borderRadius: 6,
      barThickness: 14
    }]
  }
})

const horizontalBarOptions = {
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  plugins: { legend: { display: false } },
  scales: {
    x: { beginAtZero: true, grid: { color: '#f3f4f6', drawBorder: false }, ticks: { font: { size: 9 } } },
    y: { grid: { display: false }, ticks: { font: { size: 9 } } }
  }
}

// Configuración de Gráficos de Equipos (Dona)
const topEquiposChartData = computed(() => {
  if (!dashboardData.value || !dashboardData.value.topEquipos) return null
  return {
    labels: dashboardData.value.topEquipos.map(e => e.alias || e.nombreRed),
    datasets: [{
      label: 'Tiempo (Horas)',
      data: dashboardData.value.topEquipos.map(e => Math.round((e.totalMinutos / 60) * 10) / 10),
      backgroundColor: ['#3b82f6', '#10b981', '#8b5cf6', '#f97316', '#e11d48'],
      borderWidth: 0,
      hoverOffset: 6
    }]
  }
})

const topEquiposDonutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '70%',
  plugins: {
    legend: { display: false }
  }
}

const totalEquiposMinutos = computed(() => {
  if (!dashboardData.value || !dashboardData.value.topEquipos) return 0
  return dashboardData.value.topEquipos.reduce((acc, curr) => acc + curr.totalMinutos, 0)
})

// Configuración de Gráficos de Carrera
const careerChartData = computed(() => {
  if (!dashboardData.value || !dashboardData.value.distribucionCarrera30Dias) return null
  return {
    labels: dashboardData.value.distribucionCarrera30Dias.map(c => c.carrera),
    datasets: [{
      label: 'Cantidad de Sesiones',
      data: dashboardData.value.distribucionCarrera30Dias.map(c => c.cantidad),
      backgroundColor: 'rgba(16, 185, 129, 0.8)',
      hoverBackgroundColor: 'rgba(5, 150, 105, 1)',
      borderRadius: 4,
      barThickness: 24,
      borderWidth: 1,
      borderColor: 'rgba(16, 185, 129, 1)'
    }]
  }
})

const verticalBarOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { display: false } },
  scales: {
    y: { beginAtZero: true, grid: { color: '#f3f4f6', drawBorder: false }, ticks: { font: { size: 9 } } },
    x: { grid: { display: false }, ticks: { font: { size: 9 } } }
  }
}

const totalCarreraSesiones = computed(() => {
  if (!dashboardData.value || !dashboardData.value.distribucionCarrera30Dias) return 0
  return dashboardData.value.distribucionCarrera30Dias.reduce((acc, curr) => acc + curr.cantidad, 0)
})

// Mapeo exacto de posiciones para el mapa de rendimiento
const layoutPositions = [
  [2,1],[1,2],[1,3],[1,4],[1,5],[1,6],[1,7],[1,8],[1,9],[1,10],[1,12],
  [3,1],                                                      [2,12],
  [4,1],                                                      [3,12],
  [5,1],       [4,3],[4,4],[4,5],[4,6],[4,7],[4,8],[4,9],       [4,12],
  [6,1],       [5,3],[5,4],[5,5],[5,6],[5,7],[5,8],[5,9],       [5,12],
  [6,12],
  [9,1],                                                      [8,1]
]

const getEquipoAtSlot = (index) => {
  if (!dashboardData.value || !dashboardData.value.equiposRendimiento) return null
  return dashboardData.value.equiposRendimiento.find(e => e.posicionMapa === index)
}

// Determinar el color del rendimiento
const maxMinutos = computed(() => {
  if (!dashboardData.value || !dashboardData.value.equiposRendimiento || dashboardData.value.equiposRendimiento.length === 0) return 1
  const max = Math.max(...dashboardData.value.equiposRendimiento.map(e => e.totalMinutos))
  return max > 0 ? max : 1
})

const getPCOutlineColor = (minutes) => {
  if (minutes === 0) return '#cbd5e1'
  const pct = (minutes / maxMinutos.value) * 100
  if (pct >= 75) return '#f43f5e' // Rojo/Rosa (Alto)
  if (pct >= 30) return '#f59e0b' // Ámbar/Naranja (Medio)
  return '#3b82f6' // Azul (Bajo)
}

const getPCPowerColor = (minutes) => {
  if (minutes === 0) return '#94a3b8'
  const pct = (minutes / maxMinutos.value) * 100
  if (pct >= 75) return '#e11d48'
  if (pct >= 30) return '#d97706'
  return '#2563eb'
}

// Descargar Excel Alumnos
const downloadAlumnosExcel = () => {
  if (!dashboardData.value || !dashboardData.value.reporteAlumnos) return
  
  const dataToExport = dashboardData.value.reporteAlumnos.map((a, i) => ({
    'Puesto': i + 1,
    'Código Universitario': a.codigo,
    'DNI': a.dni || 'N/A',
    'Nombre Completo': a.nombreCompleto,
    'Carrera': a.carrera || 'N/A',
    'Correo Institucional': a.correoInstitucional || 'N/A',
    'Total Sesiones': a.totalSesiones,
    'Minutos de Uso': a.totalMinutos,
    'Horas de Uso': Math.round((a.totalMinutos / 60) * 10) / 10
  }))

  const ws = XLSX.utils.json_to_sheet(dataToExport)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, "Reporte Uso Alumnos")
  XLSX.writeFile(wb, `Reporte_Uso_Alumnos_${selectedDate.value}.xlsx`)
}

// Descargar Excel Equipos
const downloadEquiposExcel = () => {
  if (!dashboardData.value || !dashboardData.value.equiposRendimiento) return
  
  const dataToExport = dashboardData.value.equiposRendimiento.map((e, idx) => ({
    'N°': idx + 1,
    'Nombre de Red': e.nombreRed,
    'Alias': e.alias || 'N/A',
    'Ubicación': e.ubicacion || 'N/A',
    'Total Sesiones': e.totalSesiones,
    'Minutos de Uso': e.totalMinutos,
    'Horas de Uso': Math.round((e.totalMinutos / 60) * 10) / 10,
    'Comentario / Estado': e.comentario || 'OK'
  }))

  const ws = XLSX.utils.json_to_sheet(dataToExport)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, "Rendimiento Equipos")
  XLSX.writeFile(wb, `Reporte_Rendimiento_Equipos_${selectedDate.value}.xlsx`)
}
</script>

<template>
  <div v-if="loaded" class="stats-view-wrapper">
    <!-- Header -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
      <div>
        <h2 style="color: #111827; font-size: 1.5rem; font-weight: 800; margin: 0;">Estadísticas de Uso</h2>
        <p style="color: #6b7280; font-size: 0.8rem; margin: 0;">Análisis del rendimiento e historial acumulado ({{ currentPeriodLabel }})</p>
      </div>
      <div style="display: flex; align-items: center; gap: 8px;">
        <div style="display: flex; align-items: center; gap: 8px; background: white; padding: 6px 12px; border-radius: 8px; border: 1px solid #e5e7eb; box-shadow: 0 1px 2px rgba(0,0,0,0.05);">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#6b7280" stroke-width="2"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
          <select v-model="selectedMonth" @change="getStats" style="border: none; outline: none; color: #374151; font-weight: 600; font-size: 0.85rem; cursor: pointer; background: transparent;">
            <option v-for="m in months" :key="m.value" :value="m.value">{{ m.label }}</option>
          </select>
          <select v-model="selectedYear" @change="getStats" style="border: none; outline: none; color: #374151; font-weight: 600; font-size: 0.85rem; cursor: pointer; background: transparent;">
            <option v-for="y in years" :key="y" :value="y">{{ y }}</option>
          </select>
        </div>
        <div style="display: flex; align-items: center; gap: 8px; background: white; padding: 6px 12px; border-radius: 8px; border: 1px solid #e5e7eb; box-shadow: 0 1px 2px rgba(0,0,0,0.05);">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#6b7280" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
          <input type="date" v-model="selectedDate" @change="getStats" style="border: none; outline: none; color: #374151; font-weight: 600; font-size: 0.85rem; cursor: pointer;">
        </div>
      </div>
    </div>

    <!-- Tops Layout -->
    <div class="tops-section-grid">
      <!-- Top Alumnos -->
      <div class="chart-box">
        <div class="chart-header">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span>Ranking de Alumnos</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#eab308" stroke-width="2.5"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"></path></svg>
          </div>
          <button @click="downloadAlumnosExcel" class="export-btn" title="Descargar Excel Completo">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
            Excel Alumnos
          </button>
        </div>
        <div class="split-layout">
          <!-- Left side: Horizontal Bar Chart -->
          <div class="split-chart">
            <Bar v-if="topAlumnosChartData" :data="topAlumnosChartData" :options="horizontalBarOptions" />
          </div>
          <!-- Right side: List -->
          <div class="ranking-list split-list">
            <div v-for="(alumno, index) in dashboardData.topAlumnos" :key="alumno.alumnoID" class="ranking-item">
              <div :class="['ranking-badge', `rank-${index + 1}`]">{{ index + 1 }}</div>
              <div style="flex-grow: 1; min-width: 0;">
                <div style="display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 2px;">
                  <span class="ranking-name">{{ alumno.nombreCompleto }}</span>
                  <span class="ranking-time">{{ formatMinutes(alumno.totalMinutos) }}</span>
                </div>
                <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.7rem; color: #6b7280;">
                  <span style="white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 75%;">{{ alumno.carrera }}</span>
                  <span style="font-weight: 500; font-size: 0.65rem; color: #9ca3af;">{{ alumno.totalSesiones }} ses.</span>
                </div>
              </div>
            </div>
            <div v-if="!dashboardData.topAlumnos || dashboardData.topAlumnos.length === 0" class="empty-state">
              Sin datos de ranking de alumnos
            </div>
          </div>
        </div>
      </div>

      <!-- Top Computadoras -->
      <div class="chart-box" style="grid-column: span 1;">
        <div class="chart-header">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span>Equipos Más Usados</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#3b82f6" stroke-width="2.5"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="12" y1="17" x2="12" y2="21"></line></svg>
          </div>
          <div style="display: flex; gap: 8px;">
            <button @click="mapModalOpen = true" class="toggle-btn">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21"></polygon>
                <line x1="9" y1="3" x2="9" y2="18"></line>
                <line x1="15" y1="6" x2="15" y2="21"></line>
              </svg>
              Ver Mapa
            </button>
            <button @click="downloadEquiposExcel" class="export-btn" title="Descargar Excel Completo">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
              Excel Equipos
            </button>
          </div>
        </div>

        <div class="split-layout">
          <!-- Left side: Doughnut Chart -->
          <div class="split-chart donut-container">
            <Doughnut v-if="topEquiposChartData" :data="topEquiposChartData" :options="topEquiposDonutOptions" />
            <div class="donut-center-text">
              <div class="donut-center-title">Total Uso</div>
              <div class="donut-center-val">{{ formatMinutes(totalEquiposMinutos) }}</div>
            </div>
          </div>
          <!-- Right side: List -->
          <div class="ranking-list split-list">
            <div v-for="(equipo, index) in dashboardData.topEquipos" :key="equipo.equipoID" class="ranking-item">
              <div :class="['ranking-badge', `rank-${index + 1}`]">
                <span class="color-dot" :style="{ backgroundColor: ['#3b82f6', '#10b981', '#8b5cf6', '#f97316', '#e11d48'][index] }"></span>
                {{ index + 1 }}
              </div>
              <div style="flex-grow: 1; min-width: 0;">
                <div style="display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 2px;">
                  <span class="ranking-name">{{ equipo.alias || equipo.nombreRed }}</span>
                  <span class="ranking-time">{{ formatMinutes(equipo.totalMinutos) }}</span>
                </div>
                <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.7rem; color: #6b7280;">
                  <span>{{ equipo.alias ? equipo.nombreRed : 'Equipo Lab' }}</span>
                  <span style="font-weight: 500; font-size: 0.65rem; color: #9ca3af;">{{ equipo.totalSesiones }} ses.</span>
                </div>
              </div>
            </div>
            <div v-if="!dashboardData.topEquipos || dashboardData.topEquipos.length === 0" class="empty-state">
              Sin datos de ranking de equipos
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Sección de Afluencia por Carrera -->
    <div class="career-section-container" style="margin-top: 1.5rem; margin-bottom: 1.5rem;">
      <div class="chart-box">
        <div class="chart-header">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span>Uso por Carrera ({{ currentPeriodLabel }})</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2.5"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>
          </div>
        </div>
        <div class="split-layout">
          <!-- Left side: Bar Chart -->
          <div class="split-chart">
            <Bar v-if="careerChartData" :data="careerChartData" :options="verticalBarOptions" />
          </div>
          <!-- Right side: Legend and Details -->
          <div class="ranking-list split-list">
            <div v-for="(carrera, index) in dashboardData.distribucionCarrera30Dias" :key="carrera.carrera" class="ranking-item">
              <div :class="['ranking-badge']" style="background: #f8fafc; border: 1px solid #e2e8f0;">
                <span class="color-dot" :style="{ backgroundColor: ['#e11d48', '#3b82f6', '#eab308', '#10b981', '#8b5cf6', '#f97316'][index % 6] }"></span>
                {{ index + 1 }}
              </div>
              <div style="flex-grow: 1; min-width: 0;">
                <div style="display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 2px;">
                  <span class="ranking-name">{{ carrera.carrera }}</span>
                  <span class="ranking-time" style="color: #10b981;">{{ carrera.cantidad }} <small style="color: #6b7280; font-size: 0.65rem; font-weight: 500;">ses.</small></span>
                </div>
                <div class="progress-bar-bg" style="margin-top: 4px;">
                  <div class="progress-bar-fill" :style="{ width: getPercentage(carrera.cantidad, totalCarreraSesiones) + '%', backgroundColor: ['#e11d48', '#3b82f6', '#eab308', '#10b981', '#8b5cf6', '#f97316'][index % 6] }"></div>
                </div>
              </div>
            </div>
            <div v-if="!dashboardData.distribucionCarrera30Dias || dashboardData.distribucionCarrera30Dias.length === 0" class="empty-state">
              Sin datos de carreras en {{ currentPeriodLabel }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal del Mapa de Rendimiento Completo -->
    <Teleport to="body">
      <div v-if="mapModalOpen" class="detail-overlay" @click="mapModalOpen = false">
        <div class="card detail-card map-modal-card" @click.stop>
          <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f3f4f6; padding-bottom: 15px; margin-bottom: 20px;">
            <h3 style="margin: 0; color: rgb(17, 24, 39); font-weight: 800; font-size: 1.25rem; display: flex; align-items: center; gap: 8px;">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
              Mapa de Rendimiento de Computadoras ({{ currentPeriodLabel }})
            </h3>
            <div style="display: flex; gap: 10px; align-items: center;">
              <button @click="downloadEquiposExcel" class="export-btn" style="padding: 8px 16px;">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                Descargar Reporte Excel
              </button>
              <button @click="mapModalOpen = false" class="close-modal-btn">X</button>
            </div>
          </div>

          <div style="display: flex; justify-content: center; gap: 20px; margin-bottom: 20px; font-size: 0.8rem; font-weight: 700; flex-wrap: wrap;">
            <div style="display: flex; align-items: center; gap: 6px;"><span class="legend-color-box" style="background: #f43f5e; width: 14px; height: 14px;"></span> Alta Afluencia (>=75%)</div>
            <div style="display: flex; align-items: center; gap: 6px;"><span class="legend-color-box" style="background: #f59e0b; width: 14px; height: 14px;"></span> Mediana Afluencia (30% - 75%)</div>
            <div style="display: flex; align-items: center; gap: 6px;"><span class="legend-color-box" style="background: #3b82f6; width: 14px; height: 14px;"></span> Baja Afluencia (<30%)</div>
            <div style="display: flex; align-items: center; gap: 6px;"><span class="legend-color-box" style="background: #cbd5e1; width: 14px; height: 14px;"></span> Sin Uso (0 min)</div>
          </div>

          <div class="perf-map-container" style="background: transparent; border: none; padding: 0; display: flex; justify-content: center;">
            <div class="perf-grid">
              <div v-for="(pos, index) in layoutPositions" :key="index"
                   class="pc-card-wrapper"
                   :style="{ gridRow: pos[0], gridColumn: pos[1] }">
                
                <!-- Si existe un equipo -->
                <div v-if="getEquipoAtSlot(index)" 
                     class="pc-card"
                     :title="`${getEquipoAtSlot(index).alias || getEquipoAtSlot(index).nombreRed} - Uso: ${formatMinutes(getEquipoAtSlot(index).totalMinutos)} (${getEquipoAtSlot(index).totalSesiones} ses.)`">
                  <div class="monitor" :style="{ background: getPCOutlineColor(getEquipoAtSlot(index).totalMinutos) }">
                    <div class="screen" :style="{ color: getPCPowerColor(getEquipoAtSlot(index).totalMinutos) }">
                      <span style="font-size: 0.55rem; font-weight: 800; font-family: monospace;">
                        {{ Math.round((getEquipoAtSlot(index).totalMinutos / 60) * 10) / 10 }}h
                      </span>
                    </div>
                    <div class="stand" :style="{ background: getPCOutlineColor(getEquipoAtSlot(index).totalMinutos) }"></div>
                  </div>
                  <span class="pc-name" style="font-size: 0.65rem;">{{ getEquipoAtSlot(index).alias || getEquipoAtSlot(index).nombreRed }}</span>
                </div>
                
                <!-- Si no hay PC asignada -->
                <div v-else class="pc-card empty-slot">
                  <div class="monitor" style="background: #f1f5f9; border: 1px dashed #cbd5e1; box-shadow: none;">
                    <div class="screen" style="background: transparent; border: none; font-size: 0.8rem; color: #cbd5e1; font-weight: 200;">-</div>
                    <div class="stand" style="background: #cbd5e1;"></div>
                  </div>
                </div>

              </div>
            </div>
          </div>

          <div style="margin-top: 20px; text-align: right;">
            <button class="btn" style="padding: 10px 24px; border-radius: 8px; background: #f1f5f9; color: #475569; border: 1px solid #cbd5e1; font-weight: 700; cursor: pointer;" @click="mapModalOpen = false">Cerrar</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>

  <div v-else class="loading-overlay">
    <div class="spinner"></div>
    <p>Cargando estadísticas de uso...</p>
  </div>
</template>

<style scoped>
.stats-view-wrapper {
  max-width: 100%;
}

.tops-section-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}

@media (min-width: 1200px) {
  .tops-section-grid {
    grid-template-columns: 1fr 1fr;
  }
}

.chart-box {
  background: #ffffff;
  padding: 1.5rem;
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
  margin-bottom: 1.5rem;
  color: #111827;
  font-size: 0.95rem;
  font-weight: 800;
  text-transform: uppercase;
}

.split-layout {
  display: flex;
  gap: 1.5rem;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  margin-top: 0.5rem;
}

.split-chart {
  flex: 1;
  min-width: 180px;
  height: 220px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.split-list {
  flex: 1.25;
  min-width: 260px;
}

.donut-container {
  position: relative;
}

.donut-center-text {
  position: absolute;
  text-align: center;
  pointer-events: none;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.donut-center-title {
  font-size: 0.65rem;
  font-weight: 700;
  text-transform: uppercase;
  color: #6b7280;
  letter-spacing: 0.05em;
}

.donut-center-val {
  font-size: 1rem;
  font-weight: 800;
  color: #111827;
  margin-top: 2px;
}

.ranking-list {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.ranking-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  border-radius: 0.75rem;
  background: #fdfdfd;
  border: 1px solid #f3f4f6;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
}

.ranking-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.04);
  border-color: #e5e7eb;
  background: #ffffff;
}

.ranking-badge {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 800;
  flex-shrink: 0;
}

.ranking-badge.rank-1 {
  background: #fef3c7;
  color: #d97706;
  border: 1px solid #fde68a;
  box-shadow: 0 2px 4px rgba(217, 119, 6, 0.1);
}

.ranking-badge.rank-2 {
  background: #f1f5f9;
  color: #475569;
  border: 1px solid #cbd5e1;
}

.ranking-badge.rank-3 {
  background: #ffedd5;
  color: #c2410c;
  border: 1px solid #fed7aa;
}

.ranking-badge.rank-4,
.ranking-badge.rank-5 {
  background: #f3f4f6;
  color: #6b7280;
  border: 1px solid #e5e7eb;
}

.ranking-name {
  color: #111827;
  font-weight: 700;
  font-size: 0.85rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.ranking-time {
  font-size: 0.85rem;
  font-weight: 800;
  color: #111827;
  white-space: nowrap;
}

.color-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  margin-right: 6px;
  display: inline-block;
  flex-shrink: 0;
}

.export-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 700;
  color: #166534;
  cursor: pointer;
  transition: all 0.2s;
}

.export-btn:hover {
  background: #dcfce7;
  border-color: #86efac;
}

.toggle-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #f0f9ff;
  border: 1px solid #bae6fd;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 700;
  color: #0369a1;
  cursor: pointer;
  transition: all 0.2s;
}

.toggle-btn:hover {
  background: #e0f2fe;
  border-color: #7dd3fc;
}

.perf-map-container {
  background: #fdfdfd;
  padding: 1.5rem;
  border-radius: 12px;
  border: 1px solid #f3f4f6;
  overflow-x: auto;
  margin-top: 0.5rem;
}

.legend-color-box {
  width: 12px;
  height: 12px;
  border-radius: 3px;
  display: inline-block;
}

.perf-grid {
  display: grid;
  grid-template-columns: repeat(12, 42px);
  grid-template-rows: repeat(9, 65px);
  gap: 15px;
  justify-content: center;
}

.pc-card-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
}

.pc-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  transition: all 0.2s ease;
}

.pc-card:hover {
  transform: scale(1.08);
}

.monitor {
  width: 42px;
  height: 32px;
  border-radius: 4px;
  padding: 2px;
  margin-bottom: 2px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.screen {
  width: 100%;
  height: 100%;
  background: #ffffff;
  border-radius: 2px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid rgba(0,0,0,0.05);
}

.stand {
  width: 14px;
  height: 6px;
  margin: -1px auto 0;
  border-radius: 0 0 2px 2px;
}

.pc-name {
  font-size: 0.55rem;
  color: #475569;
  font-weight: 700;
  white-space: nowrap;
  background: #f8fafc;
  padding: 1px 4px;
  border-radius: 3px;
  border: 1px solid #e2e8f0;
}

.empty-slot {
  opacity: 0.15;
}

.loading-overlay {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 60vh;
  color: #6b7280;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid #f3f4f6;
  border-top: 3px solid #3b82f6;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin { 100% { transform: rotate(360deg); } }

.detail-overlay {
  position: fixed; 
  top: 0; 
  left: 0; 
  width: 100vw; 
  height: 100vh;
  background: rgba(15, 23, 42, 0.4); 
  backdrop-filter: blur(8px);
  display: flex; 
  align-items: center; 
  justify-content: center;
  z-index: 9999;
}

.detail-card { 
  text-align: left;
  border: 1px solid #ffffff;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.15);
  animation: modalEnter 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes modalEnter {
  from { transform: scale(0.95); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

.map-modal-card {
  width: 95vw !important;
  max-width: 1200px !important;
  max-height: 90vh !important;
  overflow-y: auto;
  padding: 2rem;
  background: #ffffff;
  border-radius: 20px;
}

.close-modal-btn {
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  color: #475569;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-weight: bold;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.close-modal-btn:hover {
  background: #e2e8f0;
  color: #0f172a;
}
</style>
