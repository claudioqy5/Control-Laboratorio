<script setup>
const props = defineProps({
  loans: {
    type: Array,
    required: true
  },
  favorites: {
    type: Array,
    required: true
  },
  activities: {
    type: Array,
    required: true
  }
})

const emit = defineEmits([
  'renew-loan',
  'remove-favorite',
  'set-view',
  'show-toast'
])
</script>

<template>
  <div class="fade-in">
    <!-- Dashboard Welcome header -->
    <div class="student-dashboard-header">
      <h2 class="student-title">Panel del Estudiante</h2>
      <p class="student-subtitle">Bienvenido a tu portal académico. Gestiona tus recursos bibliográficos, revisa tu historial de investigación y accede a las bases de datos de URP.</p>
    </div>

    <div class="dashboard-split-grid">
      <!-- Left: active loans -->
      <div class="dashboard-card">
        <div class="dashboard-card-header">
          <h3 class="dashboard-card-title">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
            Mis Préstamos Activos
          </h3>
          <span class="dashboard-card-badge">{{ loans.length }} Libros</span>
        </div>

        <div class="loans-list">
          <div v-for="loan in loans" :key="loan.prestamoID" class="loan-item-card">
            <div class="loan-item-cover">
              <img v-if="loan.portada" :src="loan.portada" alt="Portada">
              <div v-else style="color: #94a3b8;">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
              </div>
            </div>
            <div class="loan-item-info">
              <h4 class="loan-item-title">{{ loan.titulo }}</h4>
              <div class="loan-item-meta">Autor: <strong>{{ loan.autor }}</strong></div>
              <div class="loan-item-due">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
                Devolución: {{ loan.devolucion }} (Quedan {{ loan.diasRestantes }} días)
              </div>
            </div>
            <button class="loan-action-btn" @click="emit('renew-loan', loan)">Renovar</button>
          </div>

          <div v-if="loans.length === 0" style="text-align: center; padding: 2rem; color: var(--text-muted);">
            No tienes préstamos activos de libros físicos en este momento.
          </div>
        </div>
      </div>

      <!-- Right: favorites -->
      <div class="dashboard-card">
        <div class="dashboard-card-header">
          <h3 class="dashboard-card-title">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path></svg>
            Mis Favoritos
          </h3>
        </div>

        <div class="favorites-list">
          <div v-for="(fav, index) in favorites" :key="fav.libroID" class="favorite-item-card">
            <div class="favorite-avatar" :class="{ alt: index % 2 === 1 }">
              {{ fav.titulo.substring(0, 2).toUpperCase() }}
            </div>
            <div class="favorite-info">
              <h4 class="favorite-title">{{ fav.titulo }}</h4>
              <div class="favorite-author">{{ fav.autor }}</div>
            </div>
            <button class="action-btn" title="Eliminar de favoritos" @click="emit('remove-favorite', index)" style="color: #ef4444;">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
            </button>
          </div>
          
          <div v-if="favorites.length === 0" style="text-align: center; padding: 2rem; color: var(--text-muted); font-size: 0.85rem;">
            No has agregado ningún libro a tus favoritos aún.
          </div>

          <button class="btn btn-secondary" style="margin-top: 0.5rem; font-size: 0.82rem; padding: 8px;" @click="emit('set-view', 'catalogo')">Gestionar Favoritos</button>
        </div>
      </div>
    </div>

    <!-- Institutional DB Row -->
    <div class="instit-db-grid">
      <div class="instit-db-header">
        <h3 style="font-size: 1.3rem; font-weight: 850;">Acceso a Bases de Datos Especializadas</h3>
        <span class="instit-badge">Acceso Institucional Activo</span>
      </div>

      <div class="instit-grid">
        <a href="#" class="instit-card" @click.prevent="emit('show-toast', 'Abriendo PubMed / Medline', 'success')">
          <h4 class="instit-card-title">PubMed / Medline</h4>
          <p class="instit-card-desc">Principal motor de búsqueda de literatura biomédica.</p>
          <div class="instit-card-link-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
          </div>
        </a>

        <a href="#" class="instit-card" @click.prevent="emit('show-toast', 'Abriendo ClinicalKey', 'success')">
          <h4 class="instit-card-title">ClinicalKey</h4>
          <p class="instit-card-desc">Herramienta de soporte para decisiones clínicas de Elsevier.</p>
          <div class="instit-card-link-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
          </div>
        </a>

        <a href="#" class="instit-card" @click.prevent="emit('show-toast', 'Abriendo UpToDate', 'success')">
          <h4 class="instit-card-title">UpToDate</h4>
          <p class="instit-card-desc">Recurso de medicina basada en evidencia en la práctica clínica.</p>
          <div class="instit-card-link-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
          </div>
        </a>

        <a href="#" class="instit-card" @click.prevent="emit('show-toast', 'Abriendo Scopus', 'success')">
          <h4 class="instit-card-title">Scopus</h4>
          <p class="instit-card-desc">Base de datos de citas y resúmenes de literatura revisada por pares.</p>
          <div class="instit-card-link-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
          </div>
        </a>
      </div>
    </div>

    <!-- Activity History Table -->
    <div class="dashboard-card">
      <div class="dashboard-card-header">
        <h3 class="dashboard-card-title">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
          Historial de Actividad Reciente
        </h3>
      </div>

      <div class="activity-table-wrapper">
        <table class="activity-table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Actividad</th>
              <th>Detalles</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(act, idx) in activities" :key="idx">
              <td style="font-weight: 600; color: var(--text-muted);">{{ act.fecha }}</td>
              <td style="font-weight: 700; color: var(--navy-dark);">{{ act.actividad }}</td>
              <td>{{ act.detalle }}</td>
              <td>
                <span class="status-pill" :class="act.estado.toLowerCase()">
                  {{ act.estado }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
