<script setup>
defineProps({
  currentView: {
    type: String,
    required: true
  },
  logoFAMURP: {
    type: String,
    required: true
  }
})

const emit = defineEmits(['set-view', 'show-toast'])
</script>

<template>
  <header class="header">
    <div class="header-container">
      <a class="brand" href="#" @click.prevent="emit('set-view', 'inicio')">
        <img :src="logoFAMURP" alt="Logo FAMURP" class="brand-logo-img">
        <span>BVE - FAMURP</span>        
      </a>

      <ul class="nav-menu">
        <li>
          <span class="nav-link" :class="{ active: currentView === 'inicio' }" @click="emit('set-view', 'inicio')">Inicio</span>
        </li>
        <li>
          <span class="nav-link" :class="{ active: currentView === 'catalogo' }" @click="emit('set-view', 'catalogo')">Catálogo</span>
        </li>
        <li>
          <span class="nav-link" :class="{ active: currentView === 'bases-datos' }" @click="emit('set-view', 'bases-datos')">Bases de Datos</span>
        </li>
        <li>
          <span class="nav-link" :class="{ active: currentView === 'perfil' }" @click="emit('set-view', 'perfil')">Mi Perfil</span>
        </li>
      </ul>

      <div class="nav-actions">
        <button class="action-btn" title="Notificaciones" @click="emit('show-toast', 'No tienes notificaciones pendientes.', 'info')">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M13.73 21a2 2 0 0 1-3.46 0"></path></svg>
        </button>
        
        <div class="profile-pill" :class="{ active: currentView === 'perfil' }" @click="emit('set-view', 'perfil')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
          <span>Alumno URP</span>
        </div>
      </div>
    </div>
  </header>
</template>
