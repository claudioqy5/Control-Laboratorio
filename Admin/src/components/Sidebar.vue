<script setup>
import { ref, computed, reactive, watch, onBeforeUnmount } from 'vue'

const props = defineProps({
  currentView: {
    type: String,
    default: 'dashboard'
  },
  user: {
    type: Object,
    default: null
  }
})

const emit = defineEmits(['change-view', 'logout'])

const isCollapsed = ref(true)
const isPinned = ref(false)
const activeTab = ref('biblioteca')

const openSubmenus = reactive({
  catalogoFisico: true
})

const bibliotecaViews = [
  'dashboard',
  'estadisticas',
  'map',
  'equipos',
  'alumnos',
  'active',
  'conexiones',
  'escaneos',
  'reportes',
  'libros',
  'prestamos',
  'estadisticas-prestamos',
  'categorias',
  'mapa-biblio'
]

const webViews = ['conferencias', 'conferencias-reportes', 'objetos-perdidos', 'boletin']

watch(
  () => props.currentView,
  (view) => {
    if (view === 'accesos') {
      activeTab.value = 'sistema'
    } else if (webViews.includes(view)) {
      activeTab.value = 'web'
    } else if (bibliotecaViews.includes(view)) {
      activeTab.value = 'biblioteca'
    }
  },
  { immediate: true }
)

const toastMessage = ref('')
let toastTimer = null

const toggleCollapse = () => {
  if (isPinned.value) {
    isPinned.value = false
    isCollapsed.value = true
  } else {
    isCollapsed.value = !isCollapsed.value
  }
}

const togglePin = () => {
  isPinned.value = !isPinned.value
  if (isPinned.value) {
    isCollapsed.value = false
  }
}

const handleMouseEnter = () => {
  if (!isPinned.value) {
    isCollapsed.value = false
  }
}

const handleMouseLeave = () => {
  if (!isPinned.value) {
    isCollapsed.value = true
  }
}

const toggleSubmenu = (key) => {
  openSubmenus[key] = !openSubmenus[key]
}

const navigate = (view) => {
  emit('change-view', view)
}

const showToast = (message) => {
  toastMessage.value = message
  if (toastTimer) {
    clearTimeout(toastTimer)
  }
  toastTimer = setTimeout(() => {
    toastMessage.value = ''
    toastTimer = null
  }, 4000)
}

const closeToast = () => {
  toastMessage.value = ''
  if (toastTimer) {
    clearTimeout(toastTimer)
    toastTimer = null
  }
}

const handleAccesosClick = () => {
  const u = props.user
  const isSuperAdmin = Boolean(
    u && (
      u.rol === 1 ||
      u.rol === '1' ||
      u.Rol === 1 ||
      u.Rol === '1' ||
      u.rolNombre === 'SuperAdmin' ||
      u.RolNombre === 'SuperAdmin'
    )
  )

  if (isSuperAdmin) {
    emit('change-view', 'accesos')
  } else {
    showToast('Acceso denegado: Se requiere rol SuperAdmin para acceder a esta pestaña.')
  }
}

const userName = computed(() => {
  if (!props.user) return 'Usuario'
  return props.user.nombres || props.user.nombre || props.user.username || props.user.name || 'Usuario'
})

const userRole = computed(() => {
  if (!props.user) return 'Sin rol'
  if (props.user.rolNombre) return props.user.rolNombre
  if (props.user.RolNombre) return props.user.RolNombre
  const rol = props.user.rol ?? props.user.Rol
  if (rol === 1 || rol === '1') return 'SuperAdmin'
  if (rol === 2 || rol === '2') return 'Biblioteca'
  if (rol === 3 || rol === '3') return 'Laboratorio'
  return props.user.role || 'Administrador'
})

onBeforeUnmount(() => {
  if (toastTimer) {
    clearTimeout(toastTimer)
  }
})
</script>

<template>
  <aside
    class="sidebar"
    :class="{ collapsed: isCollapsed, pinned: isPinned }"
    @mouseenter="handleMouseEnter"
    @mouseleave="handleMouseLeave"
  >
    <div class="brand-header">
      <button type="button" class="brand-icon" @click="toggleCollapse" title="Alternar menú">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M22 10v6M2 10l10-5 10 5-10 5z" />
          <path d="M6 12v5c3 3 9 3 12 0v-5" />
        </svg>
      </button>
      <div class="brand-text">
        <h1 class="brand-title">BVE Medicina</h1>
        <span class="brand-subtitle">URP - Lab. de Cómputo</span>
      </div>
      <button
        v-if="!isCollapsed"
        type="button"
        class="pin-button"
        :class="{ active: isPinned }"
        @click="togglePin"
        :title="isPinned ? 'Desfijar menú lateral' : 'Fijar menú lateral'"
      >
        <svg v-if="!isPinned" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="17" x2="12" y2="22" />
          <path d="M5 17h14v-2l-2-2V5a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v8l-2 2v2z" />
        </svg>
        <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <line x1="12" y1="17" x2="12" y2="22" />
          <path d="M5 17h14v-2l-2-2V5a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v8l-2 2v2z" />
          <line x1="3" y1="3" x2="21" y2="21" stroke="#dc2626" stroke-width="2.5" />
        </svg>
      </button>
    </div>

    <div v-show="!isCollapsed" class="workspace-tabs">
      <button
        type="button"
        class="workspace-tab"
        :class="{ active: activeTab === 'biblioteca' }"
        @click="activeTab = 'biblioteca'"
      >
        Biblioteca
      </button>
      <button
        type="button"
        class="workspace-tab"
        :class="{ active: activeTab === 'web' }"
        @click="activeTab = 'web'"
      >
        Página Web
      </button>
      <button
        type="button"
        class="workspace-tab"
        :class="{ active: activeTab === 'sistema' }"
        @click="activeTab = 'sistema'"
      >
        Sistema
      </button>
    </div>

    <div class="nav-scroll">
      <template v-if="activeTab === 'biblioteca'">
        <div class="sidebar-section-header">
          <span>Laboratorio de Cómputo</span>
        </div>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'dashboard' }"
          @click.prevent="navigate('dashboard')"
          :title="isCollapsed ? 'Resumen Diario' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="3" width="7" height="7" />
            <rect x="14" y="3" width="7" height="7" />
            <rect x="14" y="14" width="7" height="7" />
            <rect x="3" y="14" width="7" height="7" />
          </svg>
          <span class="item-text">Resumen Diario</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'estadisticas' }"
          @click.prevent="navigate('estadisticas')"
          :title="isCollapsed ? 'Estadísticas Equipos' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="20" x2="18" y2="10" />
            <line x1="12" y1="20" x2="12" y2="4" />
            <line x1="6" y1="20" x2="6" y2="14" />
          </svg>
          <span class="item-text">Estadísticas Equipos</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'active' }"
          @click.prevent="navigate('active')"
          :title="isCollapsed ? 'En Línea' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
          </svg>
          <span class="item-text">En Línea</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'map' }"
          @click.prevent="navigate('map')"
          :title="isCollapsed ? 'Mapa de Estaciones' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
            <line x1="8" y1="21" x2="16" y2="21" />
            <line x1="12" y1="17" x2="12" y2="21" />
          </svg>
          <span class="item-text">Mapa de Estaciones</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'equipos' }"
          @click.prevent="navigate('equipos')"
          :title="isCollapsed ? 'Computadoras' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
            <line x1="8" y1="21" x2="16" y2="21" />
            <line x1="12" y1="17" x2="12" y2="21" />
          </svg>
          <span class="item-text">Computadoras</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'alumnos' }"
          @click.prevent="navigate('alumnos')"
          :title="isCollapsed ? 'Participantes' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
            <path d="M16 3.13a4 4 0 0 1 0 7.75" />
          </svg>
          <span class="item-text">Participantes</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'conexiones' || currentView === 'escaneos' || currentView === 'reportes' }"
          @click.prevent="navigate('conexiones')"
          :title="isCollapsed ? 'Reportes' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
            <polyline points="10 9 9 9 8 9" />
          </svg>
          <span class="item-text">Reportes</span>
        </a>

        <div class="sidebar-section-header">
          <span>Biblioteca y Préstamos</span>
        </div>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'resumen-prestamos' }"
          @click.prevent="navigate('resumen-prestamos')"
          :title="isCollapsed ? 'Resumen Préstamos' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="3" width="7" height="7" />
            <rect x="14" y="3" width="7" height="7" />
            <rect x="14" y="14" width="7" height="7" />
            <rect x="3" y="14" width="7" height="7" />
          </svg>
          <span class="item-text">Resumen Préstamos</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'estadisticas-prestamos' }"
          @click.prevent="navigate('estadisticas-prestamos')"
          :title="isCollapsed ? 'Estadísticas Préstamos' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="20" x2="18" y2="10" />
            <line x1="12" y1="20" x2="12" y2="4" />
            <line x1="6" y1="20" x2="6" y2="14" />
          </svg>
          <span class="item-text">Estadísticas Préstamos</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'prestamos' }"
          @click.prevent="navigate('prestamos')"
          :title="isCollapsed ? 'Préstamos' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
            <rect x="8" y="2" width="8" height="4" rx="1" ry="1" />
            <path d="M9 14l2 2 4-4" />
          </svg>
          <span class="item-text">Préstamos</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'libros' }"
          @click.prevent="navigate('libros')"
          :title="isCollapsed ? 'Libros' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
            <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
          </svg>
          <span class="item-text">Libros</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'categorias' }"
          @click.prevent="navigate('categorias')"
          :title="isCollapsed ? 'Categorías' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
            <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
            <path d="M12 6V3M9 6V3M15 6V3" />
          </svg>
          <span class="item-text">Categorías</span>
        </a>

        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'mapa-biblio' }"
          @click.prevent="navigate('mapa-biblio')"
          :title="isCollapsed ? 'Mapa Biblioteca' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21" />
            <line x1="9" y1="3" x2="9" y2="18" />
            <line x1="15" y1="6" x2="15" y2="21" />
          </svg>
          <span class="item-text">Mapa Biblioteca</span>
        </a>
      </template>

      <template v-else-if="activeTab === 'web'">
        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'conferencias' }"
          :title="isCollapsed ? 'Gestión de Conferencias' : ''"
          @click.prevent="navigate('conferencias')"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
            <line x1="16" y1="2" x2="16" y2="6" />
            <line x1="8" y1="2" x2="8" y2="6" />
            <line x1="3" y1="10" x2="21" y2="10" />
          </svg>
          <span class="item-text">Gestión de Conferencias</span>
        </a>
        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'conferencias-reportes' }"
          :title="isCollapsed ? 'Estadísticas Conferencias' : ''"
          @click.prevent="navigate('conferencias-reportes')"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="20" x2="12" y2="10" />
            <line x1="18" y1="20" x2="18" y2="4" />
            <line x1="6" y1="20" x2="6" y2="16" />
          </svg>
          <span class="item-text">Estadísticas Conferencias</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <ellipse cx="12" cy="5" rx="9" ry="3" />
            <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
            <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
          </svg>
          <span class="item-text">Bases de Datos</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
          </svg>
          <span class="item-text">Matriz Hexagonal</span>
        </a>
        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'objetos-perdidos' }"
          :title="isCollapsed ? 'Objetos Perdidos' : ''"
          @click.prevent="navigate('objetos-perdidos')"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
            <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
            <line x1="12" y1="22.08" x2="12" y2="12" />
          </svg>
          <span class="item-text">Objetos Perdidos</span>
        </a>
        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'boletin' }"
          :title="isCollapsed ? 'Boletín Informativo' : ''"
          @click.prevent="navigate('boletin')"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 22h16a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H8a2 2 0 0 0-2 2v16a2 2 0 0 1-2 2Zm0 0a2 2 0 0 1-2-2v-9c0-1.1.9-2 2-2h2" />
            <path d="M18 14h-8" />
            <path d="M15 18h-5" />
            <path d="M10 6h8v4h-8V6Z" />
          </svg>
          <span class="item-text">Boletín Informativo</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10" />
            <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" />
            <line x1="12" y1="17" x2="12.01" y2="17" />
          </svg>
          <span class="item-text">Preguntas Frecuentes</span>
        </a>
      </template>

      <template v-else-if="activeTab === 'sistema'">
        <a
          href="#"
          class="nav-item"
          :class="{ active: currentView === 'accesos' }"
          @click.prevent="handleAccesosClick"
          :title="isCollapsed ? 'Configuración / Accesos' : ''"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
          <span class="item-text">Configuración / Accesos</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M5 12.55a11 11 0 0 1 14.08 0" />
            <path d="M1.42 9a16 16 0 0 1 21.16 0" />
            <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
            <line x1="12" y1="20" x2="12.01" y2="20" />
          </svg>
          <span class="item-text">Pineo de Equipos</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2z" />
            <path d="M13 5v2" />
            <path d="M13 17v2" />
            <path d="M13 11v2" />
          </svg>
          <span class="item-text">Tickets de Reportes</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
            <rect x="8" y="2" width="8" height="4" rx="1" ry="1" />
            <line x1="9" y1="12" x2="15" y2="12" />
            <line x1="9" y1="16" x2="15" y2="16" />
          </svg>
          <span class="item-text">Log de Acciones</span>
        </a>
        <a
          href="#"
          class="nav-item disabled"
          title="Próximamente"
          @click.prevent
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10" />
            <polyline points="12 6 12 12 16 14" />
          </svg>
          <span class="item-text">Horario de Atención</span>
        </a>
      </template>
    </div>

    <div class="sidebar-footer">
      <div class="user-info">
        <div class="user-name">{{ userName }}</div>
        <div class="user-role">{{ userRole }}</div>
      </div>
      <button
        type="button"
        class="logout-button"
        @click="emit('logout')"
        :title="isCollapsed ? 'Cerrar sesión' : ''"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <polyline points="16 17 21 12 16 7" />
          <line x1="21" y1="12" x2="9" y2="12" />
        </svg>
        <span>Cerrar sesión</span>
      </button>
    </div>

    <div v-if="toastMessage" class="toast-alert" role="alert">
      <div class="toast-content">
        <svg class="toast-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        <span class="toast-text">{{ toastMessage }}</span>
      </div>
      <button type="button" class="toast-close" @click="closeToast" aria-label="Cerrar alerta">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="18" y1="6" x2="6" y2="18" />
          <line x1="6" y1="6" x2="18" y2="18" />
        </svg>
      </button>
    </div>
  </aside>
</template>

<style scoped>
.brand-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 1.5rem;
  border-bottom: 1px solid #e5e7eb;
  padding-bottom: 1rem;
  overflow: hidden;
  flex-shrink: 0;
}

.brand-icon {
  background: #9f1239;
  border-radius: 8px;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: none;
  cursor: pointer;
  padding: 0;
}

.brand-title {
  margin: 0;
  color: #111827;
  font-size: 1.1rem;
  line-height: 1.2;
  white-space: nowrap;
  font-weight: 700;
}

.brand-subtitle {
  color: #6b7280;
  font-size: 0.75rem;
  font-weight: 500;
  white-space: nowrap;
}

.pin-button {
  margin-left: auto;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 6px;
  color: #64748b;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  padding: 0;
  flex-shrink: 0;
  transition: all 0.2s ease;
}

.pin-button:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.pin-button.active {
  background: #fef2f2;
  color: #dc2626;
  border-color: #fecaca;
}

.workspace-tabs {
  display: flex;
  background: #f1f5f9;
  padding: 3px;
  border-radius: 8px;
  margin-bottom: 1rem;
  gap: 2px;
}

.workspace-tab {
  flex: 1;
  padding: 6px 4px;
  font-size: 11px;
  font-weight: 600;
  border-radius: 6px;
  border: none;
  cursor: pointer;
  color: #64748b;
  background: transparent;
  transition: all 0.15s ease;
  text-align: center;
}

.workspace-tab.active {
  background: #ffffff;
  color: #9f1239;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.nav-scroll {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding-right: 2px;
}

.submenu-container {
  display: flex;
  flex-direction: column;
}

.submenu-header {
  padding: 0.625rem 0.75rem;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
  color: #4b5563;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  box-sizing: border-box;
  background: transparent;
  border: none;
  font-size: 0.875rem;
  font-family: inherit;
}

.submenu-header:hover {
  background: #f3f4f6;
  color: #111827;
}

.submenu-header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.submenu-chevron {
  transition: transform 0.2s ease;
  flex-shrink: 0;
}

.submenu-chevron.open {
  transform: rotate(180deg);
}

.nav-item svg,
.sub-nav-item svg {
  flex-shrink: 0;
}

.nav-item.disabled,
.sub-nav-item.disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.nav-item.disabled:hover,
.sub-nav-item.disabled:hover {
  background: transparent;
  color: #4b5563;
}

.sidebar-footer {
  margin-top: auto;
  background: #f9fafb;
  padding: 1rem;
  border-radius: 0.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  overflow: hidden;
  border: 1px solid #f3f4f6;
  flex-shrink: 0;
}

.user-name {
  font-weight: 600;
  color: #111827;
  font-size: 0.875rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-role {
  color: #6b7280;
  font-size: 0.75rem;
  white-space: nowrap;
}

.logout-button {
  background: transparent;
  border: none;
  color: #6b7280;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0.25rem 0;
  cursor: pointer;
  font-weight: 500;
  font-size: 0.875rem;
  margin-top: 0.25rem;
  transition: color 0.2s;
}

.logout-button svg {
  width: 16px;
  height: 16px;
  min-width: 16px;
  min-height: 16px;
  max-width: 16px;
  max-height: 16px;
  flex-shrink: 0;
  stroke: currentColor;
}

.logout-button:hover {
  color: #dc2626;
}

.toast-alert {
  position: fixed;
  top: 1.5rem;
  right: 1.5rem;
  z-index: 9999;
  background-color: #fef2f2;
  border: 1px solid #fecaca;
  color: #991b1b;
  padding: 0.875rem 1rem;
  border-radius: 0.5rem;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  max-width: 420px;
}

.toast-content {
  display: flex;
  align-items: center;
  gap: 0.625rem;
}

.toast-icon {
  flex-shrink: 0;
  stroke: #dc2626;
}

.toast-text {
  font-size: 0.875rem;
  font-weight: 500;
  line-height: 1.25rem;
}

.toast-close {
  background: transparent;
  border: none;
  cursor: pointer;
  color: #991b1b;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem;
  border-radius: 0.25rem;
  transition: background-color 0.2s;
}

.toast-close:hover {
  background-color: #fee2e2;
}

.sidebar-section-header {
  display: flex;
  align-items: center;
  font-size: 0.68rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #94a3b8;
  padding: 0.75rem 0.5rem 0.35rem;
  margin-top: 0.5rem;
  white-space: nowrap;
  user-select: none;
}

.sidebar-section-header::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #f1f5f9;
  margin-left: 8px;
}

.sidebar-section-header:first-of-type {
  margin-top: 0;
  padding-top: 0.15rem;
}

.sidebar.collapsed .brand-text,
.sidebar.collapsed .sidebar-section-header,
.sidebar.collapsed .item-text,
.sidebar.collapsed .submenu-chevron,
.sidebar.collapsed .user-info,
.sidebar.collapsed .logout-button span,
.sidebar.collapsed .sub-nav-group,
.sidebar.collapsed .workspace-tabs {
  display: none;
}

.sidebar.collapsed .nav-item,
.sidebar.collapsed .submenu-header {
  justify-content: center;
  padding: 0.75rem 0;
}

.sidebar.collapsed .logout-button {
  justify-content: center;
}
</style>
