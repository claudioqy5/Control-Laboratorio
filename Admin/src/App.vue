<script setup>
import { ref, onMounted } from 'vue'
import Dashboard from './components/Dashboard.vue'
import AlumnosCRUD from './components/AlumnosCRUD.vue'
import ActiveSessions from './components/ActiveSessions.vue'
import AdminLogin from './components/AdminLogin.vue'
import LaboratoryMap from './components/LaboratoryMap.vue'
import EquiposList from './components/EquiposList.vue'
import ReportesConexiones from './components/ReportesConexiones.vue'
import LibrosCRUD from './components/LibrosCRUD.vue'
import PrestamosCRUD from './components/PrestamosCRUD.vue'
import CategoriasCRUD from './components/CategoriasCRUD.vue'
import MapaBiblioteca from './components/MapaBiblioteca.vue'
import UsoEstadisticas from './components/UsoEstadisticas.vue'
import ChatBot from './components/ChatBot.vue'
import Sidebar from './components/Sidebar.vue'
import ConfiguracionAccesos from './components/ConfiguracionAccesos.vue'
import EstadisticasPrestamos from './components/EstadisticasPrestamos.vue'

const currentView = ref('dashboard')
const isAuthenticated = ref(false)
const currentUser = ref(null)

const checkAuth = () => {
  const token = localStorage.getItem('adminToken')
  const user = localStorage.getItem('adminUser')
  if (token) {
    isAuthenticated.value = true
    if (user) {
      try {
        currentUser.value = JSON.parse(user)
      } catch {
        currentUser.value = null
      }
    } else {
      currentUser.value = null
    }
  }
}

checkAuth()
onMounted(checkAuth)

const handleLoginSuccess = (user) => {
  isAuthenticated.value = true
  currentUser.value = user || null
}

const handleLogout = () => {
  localStorage.removeItem('adminToken')
  localStorage.removeItem('adminRefreshToken')
  localStorage.removeItem('adminUser')
  currentUser.value = null
  isAuthenticated.value = false
  currentView.value = 'dashboard'
}
</script>

<template>
  <AdminLogin v-if="!isAuthenticated" @login-success="handleLoginSuccess" />

  <template v-else>
    <Sidebar
      :current-view="currentView"
      :user="currentUser"
      @change-view="currentView = $event"
      @logout="handleLogout"
    />

    <main class="main-content">
      <Dashboard v-if="currentView === 'dashboard'" />
      <LaboratoryMap v-if="currentView === 'map'" />
      <EquiposList v-if="currentView === 'equipos'" />
      <AlumnosCRUD v-if="currentView === 'alumnos'" />
      <LibrosCRUD v-if="currentView === 'libros'" />
      <PrestamosCRUD v-if="currentView === 'prestamos'" @ver-estadisticas="currentView = 'estadisticas-prestamos'" />
      <EstadisticasPrestamos v-if="currentView === 'estadisticas-prestamos'" />
      <CategoriasCRUD v-if="currentView === 'categorias'" />
      <MapaBiblioteca v-if="currentView === 'mapa-biblio'" />
      <ActiveSessions v-if="currentView === 'active'" />
      <ReportesConexiones
        v-if="currentView === 'conexiones' || currentView === 'escaneos' || currentView === 'reportes'"
        :initial-tab="currentView === 'escaneos' ? 'escaneos' : 'conexiones'"
      />
      <UsoEstadisticas v-if="currentView === 'estadisticas'" />
      <ConfiguracionAccesos
        v-if="currentView === 'accesos'"
        :current-user="currentUser"
      />
    </main>

    <ChatBot />
  </template>
</template>
