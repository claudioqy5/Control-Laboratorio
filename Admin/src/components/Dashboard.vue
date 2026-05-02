<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import { Line, Doughnut, Bar } from 'vue-chartjs'
import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Filler } from 'chart.js'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Filler)

const dashboardData = ref(null)
const loaded = ref(false)

const getDashboardStats = async () => {
  try {
    const res = await axios.get('https://localhost:7215/api/stats/dashboard')
    dashboardData.value = res.data
    loaded.value = true
  } catch (err) {
    console.error("Error cargando dashboard", err)
  }
}

onMounted(() => {
  getDashboardStats()
  setInterval(getDashboardStats, 10000) 
})

const ocupacionPorcentaje = computed(() => {
  if (!dashboardData.value || dashboardData.value.totalEstaciones === 0) return 0
  return Math.round((dashboardData.value.sesionesActivas / dashboardData.value.totalEstaciones) * 100)
})

const lineChartData = computed(() => {
  if (!dashboardData.value) return null
  return {
    labels: dashboardData.value.afluenciaPorHora.map((_, i) => `${i + 7}:00`),
    datasets: [{
      label: 'Sesiones',
      data: dashboardData.value.afluenciaPorHora,
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

const commonOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { display: false } },
  scales: {
    y: { beginAtZero: true, grid: { color: '#f3f4f6', drawBorder: false }, ticks: { font: { size: 10 } } },
    x: { grid: { display: false }, ticks: { font: { size: 10 } } }
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
      <div>
        <h2 style="color: #111827; font-size: 1.5rem; font-weight: 800; margin: 0;">Resumen General</h2>
        <p style="color: #6b7280; font-size: 0.8rem; margin: 0;">Control en tiempo real · {{ new Date().toLocaleDateString() }}</p>
      </div>
      <div class="status-pill">
        <span class="pulse-dot"></span>
        Sistema en línea
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
    </div>

    <!-- Charts Layout -->
    <div class="main-charts-grid">
      <!-- Left Column -->
      <div class="charts-left">
        <div class="chart-box">
          <div class="chart-header">
            <span>Afluencia por hora (Últimas 24h)</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#e11d48" stroke-width="2.5"><polyline points="22 7 13.5 15.5 8.5 10.5 2 17"></polyline></svg>
          </div>
          <div class="chart-container">
            <Line :data="lineChartData" :options="commonOptions" />
          </div>
        </div>
        
        <div class="chart-box">
          <div class="chart-header">
            <span>Asistencia Semanal</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#3b82f6" stroke-width="2.5"><rect x="18" y="3" width="4" height="18"></rect><rect x="10" y="8" width="4" height="13"></rect><rect x="2" y="13" width="4" height="8"></rect></svg>
          </div>
          <div class="chart-container">
            <Bar :data="barChartData" :options="commonOptions" />
          </div>
        </div>
      </div>

      <!-- Right Column -->
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
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
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

.stat-label { color: #6b7280; font-size: 0.7rem; font-weight: 600; text-transform: uppercase; }
.stat-value { color: #111827; font-size: 1.25rem; font-weight: 800; }
.stat-value small { font-size: 0.8rem; color: #9ca3af; font-weight: 500; }

.main-charts-grid {
  display: grid;
  grid-template-columns: 1.8fr 1fr;
  gap: 1.25rem;
}

.charts-left {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
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
  height: 25vh; /* Se adapta a la pantalla de forma responsiva */
  position: relative;
}

.donut-box { 
  display: block; 
}
.donut-wrapper {
  height: 40vh; /* Reducido para que la dona no se vea tan masiva */
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
</style>
