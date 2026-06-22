<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import * as XLSX from 'xlsx'
import { API_BASE_URL } from '../config'
import { Line, Doughnut, Bar } from 'vue-chartjs'
import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Filler } from 'chart.js'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Filler)

const dashboardData = ref(null)
const loaded = ref(false)

// Helper para convertir hora 24h a 12h con AM/PM
const formatHour12 = (h) => {
  const period = h >= 12 ? 'PM' : 'AM'
  const hour = h % 12 || 12
  return `${hour} ${period}`
}

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

// Obtener fecha actual en zona horaria de Perú (UTC-5)
const getPeruDate = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' };
  const formatter = new Intl.DateTimeFormat('en-CA', options); // en-CA da formato YYYY-MM-DD
  return formatter.format(new Date());
}
const selectedDate = ref(getPeruDate())
let refreshInterval = null
const scanStats = ref({ escaneosEsteMes: 0, limiteMensual: 1000, limiteSeguridad: 950, diasRestantes: 0 })

const getDashboardStats = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/stats/dashboard?date=${selectedDate.value}`)
    dashboardData.value = res.data
    
    try {
      const scanRes = await axios.get(`${API_BASE_URL}/api/reportes/escaneos-stats`)
      scanStats.value = scanRes.data
    } catch (scanErr) {
      console.error("Error cargando estadísticas de escaneos:", scanErr)
    }

    loaded.value = true
  } catch (err) {
    console.error("Error cargando dashboard", err)
  }
}

const exportToExcel = (type) => {
  if (!dashboardData.value) return
  
  let dataToExport = []
  let fileName = ""
  
  if (type === 'hourly') {
    fileName = `Afluencia_Horaria_${selectedDate.value}.xlsx`
    dashboardData.value.afluenciaPorHora.forEach((h, i) => {
      const horaStr = formatHour12(i + 7)
      const count = typeof h === 'object' && h !== null ? (h.cantidad ?? 0) : h
      const sesionesList = typeof h === 'object' && h !== null && Array.isArray(h.sesiones) ? h.sesiones : []
      
      if (sesionesList.length === 0) {
        dataToExport.push({
          'Hora': horaStr,
          'Sesiones': count,
          'Código Universitario': '',
          'DNI': '',
          'Nombres': '',
          'Apellido Paterno': '',
          'Apellido Materno': '',
          'Nombre Completo': '',
          'Carrera': '',
          'Teléfono': '',
          'Correo Institucional': '',
          'Correo Personal': '',
          'Equipo': '',
          'Ubicación de PC': '',
          'Hora Inicio': '',
          'Hora Fin': '',
          'Duración (minutos)': ''
        })
      } else {
        sesionesList.forEach(s => {
          dataToExport.push({
            'Hora': horaStr,
            'Sesiones': count,
            'Código Universitario': s.codigoUniversitario || '',
            'DNI': s.dni || '',
            'Nombres': s.nombres || '',
            'Apellido Paterno': s.apellidoPaterno || '',
            'Apellido Materno': s.apellidoMaterno || '',
            'Nombre Completo': s.alumnoNombre || '',
            'Carrera': s.carrera || '',
            'Teléfono': s.telefono || '',
            'Correo Institucional': s.correoInstitucional || '',
            'Correo Personal': s.correoPersonal || '',
            'Equipo': s.equipo || '',
            'Ubicación de PC': s.equipoUbicacion || '',
            'Hora Inicio': s.horaInicio || '',
            'Hora Fin': s.horaFin || '',
            'Duración (minutos)': s.duracionMinutos !== null && s.duracionMinutos !== undefined ? s.duracionMinutos : 'Activo'
          })
        })
      }
    })
  } else if (type === 'weekly') {
    fileName = `Asistencia_Semanal_${selectedDate.value}.xlsx`
    dataToExport = []
    dashboardData.value.afluenciaPorDia.forEach(d => {
      const dayLabel = typeof d === 'object' && d !== null ? d.dia : ''
      const count = typeof d === 'object' && d !== null ? (d.cantidad ?? 0) : d
      const sesionesList = typeof d === 'object' && d !== null && Array.isArray(d.sesiones) ? d.sesiones : []
      const fechaStr = typeof d === 'object' && d !== null ? (d.fechaCompleta ?? '') : ''
      
      if (sesionesList.length === 0) {
        dataToExport.push({
          'Día': dayLabel,
          'Fecha': fechaStr,
          'Asistencias Totales': count,
          'Código Universitario': '',
          'DNI': '',
          'Nombres': '',
          'Apellido Paterno': '',
          'Apellido Materno': '',
          'Nombre Completo': '',
          'Carrera': '',
          'Teléfono': '',
          'Correo Institucional': '',
          'Correo Personal': '',
          'Equipo': '',
          'Ubicación de PC': '',
          'Hora Inicio': '',
          'Hora Fin': '',
          'Duración (minutos)': ''
        })
      } else {
        sesionesList.forEach(s => {
          dataToExport.push({
            'Día': dayLabel,
            'Fecha': fechaStr,
            'Asistencias Totales': count,
            'Código Universitario': s.codigoUniversitario || '',
            'DNI': s.dni || '',
            'Nombres': s.nombres || '',
            'Apellido Paterno': s.apellidoPaterno || '',
            'Apellido Materno': s.apellidoMaterno || '',
            'Nombre Completo': s.alumnoNombre || '',
            'Carrera': s.carrera || '',
            'Teléfono': s.telefono || '',
            'Correo Institucional': s.correoInstitucional || '',
            'Correo Personal': s.correoPersonal || '',
            'Equipo': s.equipo || '',
            'Ubicación de PC': s.equipoUbicacion || '',
            'Hora Inicio': s.horaInicio || '',
            'Hora Fin': s.horaFin || '',
            'Duración (minutos)': s.duracionMinutos !== null && s.duracionMinutos !== undefined ? s.duracionMinutos : 'Activo'
          })
        })
      }
    })
  }
  
  const ws = XLSX.utils.json_to_sheet(dataToExport)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, "Reporte")
  XLSX.writeFile(wb, fileName)
}

onMounted(() => {
  getDashboardStats()
  refreshInterval = setInterval(() => {
    // Solo refrescar si es el día de hoy en Perú
    if (selectedDate.value === getPeruDate()) {
      getDashboardStats()
    }
  }, 10000) 
})

const ocupacionPorcentaje = computed(() => {
  if (!dashboardData.value || dashboardData.value.totalEstaciones === 0) return 0
  return Math.round((dashboardData.value.sesionesActivas / dashboardData.value.totalEstaciones) * 100)
})

const lineChartData = computed(() => {
  if (!dashboardData.value) return null
  return {
    labels: dashboardData.value.afluenciaPorHora.map((_, i) => formatHour12(i + 7)),
    datasets: [{
      label: 'Sesiones',
      data: dashboardData.value.afluenciaPorHora.map(h => typeof h === 'object' && h !== null ? (h.cantidad ?? 0) : h),
      borderColor: '#e11d48',
      backgroundColor: 'rgba(225, 29, 72, 0.05)',
      borderWidth: 3,
      pointRadius: 0,
      fill: true,
      tension: 0.4
    }]
  }
})

const barChartData = computed(() => {
  if (!dashboardData.value) return null
  
  // Crear un gradiente o usar un color más moderno
  return {
    labels: dashboardData.value.afluenciaPorDia.map(d => d.dia),
    datasets: [{
      label: 'Sesiones',
      data: dashboardData.value.afluenciaPorDia.map(d => d.cantidad),
      backgroundColor: 'rgba(59, 130, 246, 0.8)',
      hoverBackgroundColor: 'rgba(37, 99, 235, 1)',
      borderRadius: 6,
      borderSkipped: false,
      barThickness: 32,
      borderWidth: 1,
      borderColor: 'rgba(59, 130, 246, 1)'
    }]
  }
})

const donutChartData = computed(() => {
  if (!dashboardData.value) return null
  const bgColors = ['#e11d48', '#3b82f6', '#eab308', '#10b981', '#8b5cf6', '#f97316']
  return {
    labels: dashboardData.value.distribucionCarrera.map(c => c.carrera),
    datasets: [{
      data: dashboardData.value.distribucionCarrera.map(c => c.cantidad),
      backgroundColor: bgColors,
      borderWidth: 0,
      hoverOffset: 10
    }]
  }
})

const topAlumnosChartData = computed(() => {
  if (!dashboardData.value || !dashboardData.value.topAlumnos) return null
  return {
    labels: dashboardData.value.topAlumnos.map(a => a.nombreCompleto),
    datasets: [{
      label: 'Tiempo (Horas)',
      data: dashboardData.value.topAlumnos.map(a => Math.round((a.totalMinutos / 60) * 10) / 10),
      backgroundColor: 'rgba(245, 158, 11, 0.8)',
      hoverBackgroundColor: 'rgba(217, 119, 6, 1)',
      borderRadius: 4,
      barThickness: 14
    }]
  }
})

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

const totalEquiposMinutos = computed(() => {
  if (!dashboardData.value || !dashboardData.value.topEquipos) return 0
  return dashboardData.value.topEquipos.reduce((acc, curr) => acc + curr.totalMinutos, 0)
})

const commonOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { display: false } },
  scales: {
    y: { beginAtZero: true, grid: { color: '#f3f4f6', drawBorder: false }, ticks: { font: { size: 10 } } },
    x: { grid: { display: false }, ticks: { font: { size: 10 } } }
  }
}

const barHoursOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { display: false } },
  scales: {
    y: { 
      beginAtZero: true, 
      grid: { color: '#f3f4f6', drawBorder: false }, 
      ticks: { 
        font: { size: 10 },
        callback: (value) => `${value}h`
      } 
    },
    x: { grid: { display: false }, ticks: { font: { size: 10 } } }
  }
}

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

const topEquiposDonutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '70%',
  plugins: {
    legend: { display: false }
  }
}

const donutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '75%',
  plugins: {
    legend: { position: 'bottom', labels: { boxWidth: 8, usePointStyle: true, font: { size: 11 } } }
  }
}
</script>

<template>
  <div v-if="loaded" class="dashboard-wrapper">
    <!-- Header Minimal -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
      <div style="display: flex; align-items: center; gap: 1.5rem;">
        <div>
          <h2 style="color: #111827; font-size: 1.5rem; font-weight: 800; margin: 0;">Resumen General</h2>
          <p style="color: #6b7280; font-size: 0.8rem; margin: 0;">Control en tiempo real · {{ selectedDate }}</p>
        </div>
        <div style="display: flex; align-items: center; gap: 8px; background: white; padding: 6px 12px; border-radius: 8px; border: 1px solid #e5e7eb; box-shadow: 0 1px 2px rgba(0,0,0,0.05);">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#6b7280" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
          <input type="date" v-model="selectedDate" @change="getDashboardStats" style="border: none; outline: none; color: #374151; font-weight: 600; font-size: 0.85rem; cursor: pointer;">
        </div>
      </div>
      <div class="status-pill">
        <span class="pulse-dot"></span>
        {{ selectedDate === new Date().toISOString().split('T')[0] ? 'Sistema en línea' : 'Datos Históricos' }}
      </div>
    </div>

    <!-- Mini Cards Row -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon red"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline></svg></div>
        <div>
          <div class="stat-label">Sesiones Activas</div>
          <div class="stat-value">{{ dashboardData.sesionesActivas }} <small>/ {{ dashboardData.totalEstaciones }}</small></div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon green"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg></div>
        <div>
          <div class="stat-label">Ingresos Hoy</div>
          <div class="stat-value">{{ dashboardData.sesionesHoy }}</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon amber"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg></div>
        <div>
          <div class="stat-label">Tiempo Promedio</div>
          <div class="stat-value">{{ dashboardData.tiempoPromedioMinutos }} <small>min</small></div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon blue"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="12" y1="17" x2="12" y2="21"></line></svg></div>
        <div>
          <div class="stat-label">Ocupación</div>
          <div class="stat-value">{{ ocupacionPorcentaje }}%</div>
        </div>
      </div>
      <div class="stat-card">
        <div :class="['stat-icon', scanStats.escaneosEsteMes >= scanStats.limiteSeguridad ? 'red' : (scanStats.escaneosEsteMes >= 800 ? 'amber' : 'violet')]">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>
        </div>
        <div>
          <div class="stat-label">Escaneos del Mes</div>
          <div class="stat-value">{{ scanStats.escaneosEsteMes }} <small>/ {{ scanStats.limiteMensual }}</small></div>
          <div style="font-size: 0.65rem; color: #6b7280; margin-top: 2px; font-weight: 600; text-transform: uppercase;">
            Faltan {{ scanStats.diasRestantes }} {{ scanStats.diasRestantes === 1 ? 'día' : 'días' }}
          </div>
        </div>
      </div>
    </div>

    <!-- Charts Layout Rows -->
    <div class="charts-row-two">
      <!-- Left Column: Afluencia por hora -->
      <div class="chart-box">
        <div class="chart-header">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span>Afluencia por hora</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#e11d48" stroke-width="2.5"><polyline points="22 7 13.5 15.5 8.5 10.5 2 17"></polyline></svg>
          </div>
          <button @click="exportToExcel('hourly')" class="export-btn" title="Descargar Excel">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
            Reporte
          </button>
        </div>
        <div class="chart-container">
          <Line :data="lineChartData" :options="commonOptions" />
        </div>
      </div>
      
      <!-- Right Column: Distribución por Carrera -->
      <div class="chart-box donut-box">
        <div class="chart-header">
          <span>Distribución por Carrera</span>
        </div>
        <div class="donut-wrapper">
          <Doughnut v-if="donutChartData && donutChartData.labels && donutChartData.labels.length > 0" :data="donutChartData" :options="donutOptions" />
          <div v-else-if="loaded" class="empty-state">Sin datos de carreras</div>
        </div>
      </div>
    </div>

    <div class="charts-row-three">
      <!-- Left: Asistencia Semanal -->
      <div class="chart-box">
        <div class="chart-header">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span>Asistencia Semanal</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#3b82f6" stroke-width="2.5"><rect x="18" y="3" width="4" height="18"></rect><rect x="10" y="8" width="4" height="13"></rect><rect x="2" y="13" width="4" height="8"></rect></svg>
          </div>
          <button @click="exportToExcel('weekly')" class="export-btn" title="Descargar Excel">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
            Reporte
          </button>
        </div>
        <div class="chart-container">
          <Bar v-if="barChartData" :data="barChartData" :options="commonOptions" />
        </div>
      </div>

      <!-- Right: Horas de Uso Semanal -->
      <div class="chart-box">
        <div class="chart-header">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span>Horas de Uso Semanal</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2.5"><rect x="18" y="3" width="4" height="18"></rect><rect x="10" y="8" width="4" height="13"></rect><rect x="2" y="13" width="4" height="8"></rect></svg>
          </div>
        </div>
        <div class="chart-container">
          <Bar v-if="barHoursChartData" :data="barHoursChartData" :options="barHoursOptions" />
        </div>
      </div>
    </div>
  </div>

  <div v-else class="loading-overlay">
    <div class="spinner"></div>
    <p>Cargando panel de control...</p>
  </div>
</template>

<style scoped>
.dashboard-wrapper {
  max-width: 100%;
  overflow: hidden;
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
  box-shadow: 0 2px 4px rgba(0,0,0,0.02);
}

.stat-icon {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.stat-icon.red { background: #ffe4e6; color: #e11d48; }
.stat-icon.green { background: #d1fae5; color: #059669; }
.stat-icon.amber { background: #fef3c7; color: #d97706; }
.stat-icon.blue { background: #e0f2fe; color: #0284c7; }
.stat-icon.violet { background: #f5f3ff; color: #7c3aed; }

.stat-label { color: #6b7280; font-size: 0.7rem; font-weight: 600; text-transform: uppercase; }
.stat-value { color: #111827; font-size: 1.25rem; font-weight: 800; }
.stat-value small { font-size: 0.8rem; color: #9ca3af; font-weight: 500; }

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
  margin-bottom: 1.25rem;
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

.donut-box { 
  display: block; 
}
.donut-wrapper {
  height: 25vh;
  position: relative;
  width: 100%;
  padding-top: 1rem;
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
  border-top: 3px solid #e11d48;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin { 100% { transform: rotate(360deg); } }

.export-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s;
}

.export-btn:hover {
  background: #f1f5f9;
  border-color: #cbd5e1;
  color: #1e293b;
}

.export-btn svg {
  color: #16a34a; /* Color verde para excel */
}
</style>
