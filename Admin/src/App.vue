<script setup>
import { ref, onMounted } from 'vue'
import Dashboard from './components/Dashboard.vue'
import AlumnosCRUD from './components/AlumnosCRUD.vue'
import ActiveSessions from './components/ActiveSessions.vue'
import AdminLogin from './components/AdminLogin.vue'
import LaboratoryMap from './components/LaboratoryMap.vue'
import EquiposList from './components/EquiposList.vue'
import ReportesConexiones from './components/ReportesConexiones.vue'
import ActualizacionesSistema from './components/ActualizacionesSistema.vue'

const currentView = ref('dashboard')
const isAuthenticated = ref(false)
const isSidebarCollapsed = ref(true) // Iniciamos colapsado por defecto

const checkAuth = () => {
  const token = localStorage.getItem('adminToken')
  if (token) {
    isAuthenticated.ref = true // Simplificado para este ejemplo
    // En un sistema real aquí validarías el token contra el servidor
    isAuthenticated.value = true
  }
}

const logout = () => {
  localStorage.removeItem('adminToken')
  isAuthenticated.value = false
}

onMounted(checkAuth)
</script>

<template>
  <AdminLogin v-if="!isAuthenticated" @login-success="isAuthenticated = true" />

  <template v-else>
    <div class="sidebar" 
         :class="{ collapsed: isSidebarCollapsed }"
         @mouseenter="isSidebarCollapsed = false"
         @mouseleave="isSidebarCollapsed = true">
      
      <div style="display: flex; align-items: center; gap: 10px; margin-bottom: 2rem; border-bottom: 1px solid #e5e7eb; padding-bottom: 1rem; overflow: hidden;">
        <div style="background: #9f1239; border-radius: 8px; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 10v6M2 10l10-5 10 5-10 5z"/><path d="M6 12v5c3 3 9 3 12 0v-5"/></svg>
        </div>
        <div v-if="!isSidebarCollapsed">
          <h1 style="margin: 0; color: #111827; font-size: 1.1rem; line-height: 1.2; white-space: nowrap;">BVE Medicina</h1>
          <span style="color: #6b7280; font-size: 0.75rem; font-weight: 500; white-space: nowrap;">URP - Lab. de Cómputo</span>
        </div>
      </div>

      <a href="#" class="nav-item" :class="{ active: currentView === 'dashboard' }" @click="currentView = 'dashboard'" :title="isSidebarCollapsed ? 'Resumen' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect></svg>
        <span v-if="!isSidebarCollapsed">Resumen</span>
      </a>
      <a href="#" class="nav-item" :class="{ active: currentView === 'map' }" @click="currentView = 'map'" :title="isSidebarCollapsed ? 'Estaciones' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
        <span v-if="!isSidebarCollapsed">Estaciones</span>
      </a>
      <a href="#" class="nav-item" :class="{ active: currentView === 'equipos' }" @click="currentView = 'equipos'" :title="isSidebarCollapsed ? 'Computadoras' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg>
        <span v-if="!isSidebarCollapsed">Computadoras</span>
      </a>
      <a href="#" class="nav-item" :class="{ active: currentView === 'alumnos' }" @click="currentView = 'alumnos'" :title="isSidebarCollapsed ? 'Participantes' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
        <span v-if="!isSidebarCollapsed">Participantes</span>
      </a>
      <a href="#" class="nav-item" :class="{ active: currentView === 'active' }" @click="currentView = 'active'" :title="isSidebarCollapsed ? 'En Línea' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline></svg>
        <span v-if="!isSidebarCollapsed">En Línea</span>
      </a>
      <a href="#" class="nav-item" :class="{ active: currentView === 'conexiones' }" @click="currentView = 'conexiones'" :title="isSidebarCollapsed ? 'Reportes' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>
        <span v-if="!isSidebarCollapsed">Reportes</span>
      </a>
      <a href="#" class="nav-item" :class="{ active: currentView === 'actualizaciones' }" @click="currentView = 'actualizaciones'" :title="isSidebarCollapsed ? 'Actualizaciones' : ''">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
        <span v-if="!isSidebarCollapsed">Actualizaciones</span>
      </a>
      
      <div style="margin-top: auto; background: #f9fafb; padding: 1rem; border-radius: 0.5rem; display: flex; flex-direction: column; gap: 0.5rem; overflow: hidden;">
        <div v-if="!isSidebarCollapsed">
          <div style="font-weight: 600; color: #111827; font-size: 0.875rem; white-space: nowrap;">Lic. Francisca Valero</div>
          <div style="color: #6b7280; font-size: 0.75rem; white-space: nowrap;">Bibl. Virtual FAMURP</div>          
        </div>
        <button style="background: transparent; border: none; color: #6b7280; display: flex; align-items: center; gap: 8px; padding: 0; cursor: pointer; font-weight: 500; font-size: 0.875rem; margin-top: 0.5rem;" @click="logout" :title="isSidebarCollapsed ? 'Cerrar sesión' : ''">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path><polyline points="16 17 21 12 16 7"></polyline><line x1="21" y1="12" x2="9" y2="12"></line></svg>
          <span v-if="!isSidebarCollapsed">Cerrar sesión</span>
        </button>
      </div>
    </div>

    <main class="main-content">
      <Dashboard v-if="currentView === 'dashboard'" />
      <LaboratoryMap v-if="currentView === 'map'" />
      <EquiposList v-if="currentView === 'equipos'" />
      <AlumnosCRUD v-if="currentView === 'alumnos'" />
      <ActiveSessions v-if="currentView === 'active'" />
      <ReportesConexiones v-if="currentView === 'conexiones'" />
      <ActualizacionesSistema v-if="currentView === 'actualizaciones'" />
    </main>
  </template>
</template>
