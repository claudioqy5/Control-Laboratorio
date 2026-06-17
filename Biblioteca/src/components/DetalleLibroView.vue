<script setup>
const props = defineProps({
  libro: {
    type: Object,
    required: true
  },
  libros: {
    type: Array,
    required: true
  },
  isFavorite: {
    type: Boolean,
    required: true
  }
})

const emit = defineEmits([
  'set-view',
  'select-category',
  'view-book',
  'request-loan',
  'toggle-favorite',
  'show-toast'
])
</script>

<template>
  <div class="fade-in">
    <!-- Breadcrumbs -->
    <div class="breadcrumbs">
      <span class="breadcrumb-link" @click="emit('set-view', 'inicio')">Inicio</span>
      <span class="breadcrumb-sep">/</span>
      <span class="breadcrumb-link" @click="emit('set-view', 'catalogo')">Catálogo</span>
      <span class="breadcrumb-sep">/</span>
      <span class="breadcrumb-link" @click="emit('select-category', libro.categoria)">{{ libro.categoria || 'Medicina' }}</span>
      <span class="breadcrumb-sep">/</span>
      <span style="color: var(--navy-dark); font-weight: 600;">{{ libro.titulo }}</span>
    </div>

    <!-- Main details grid -->
    <div class="detail-main-grid">
      <!-- Left Cover Column -->
      <div class="detail-left-column">
        <div class="detail-cover-card">
          <div class="detail-cover-img">
            <img v-if="libro.portada" :src="libro.portada" alt="Portada del Libro">
            <div v-else class="detail-cover-placeholder-icon">
              <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
            </div>
          </div>
          <span class="badge-peer-reviewed">Ficha Médica</span>
        </div>

        <!-- Action buttons -->
        <button class="btn btn-primary" style="padding: 12px;" @click="emit('request-loan', libro)" :disabled="libro.estado !== 'Disponible'" :style="{ opacity: libro.estado !== 'Disponible' ? '0.6' : '1', cursor: libro.estado === 'Disponible' ? 'pointer' : 'not-allowed' }">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
          Solicitar Préstamo
        </button>
        
        <button class="btn btn-secondary" style="padding: 12px;" @click="emit('toggle-favorite', libro)">
          <svg width="18" height="18" viewBox="0 0 24 24" :fill="isFavorite ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="2.5" :style="{ color: isFavorite ? 'var(--urp-red)' : 'inherit' }"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path></svg>
          {{ isFavorite ? 'Quitar de Favoritos' : 'Añadir a Favoritos' }}
        </button>
      </div>

      <!-- Right Info Column -->
      <div class="detail-right-column">
        <div class="detail-header">
          <h1 class="detail-title">{{ libro.titulo }}</h1>
          
          <div class="detail-author-row">
            <span class="detail-author-item">Por: <strong>{{ libro.autor }}</strong></span>
            <span style="color: #cbd5e1;">|</span>
            <span class="detail-author-item">Editorial: <strong>{{ libro.editorial || 'Editorial Médica URP' }}</strong></span>
            <span style="color: #cbd5e1;">|</span>
            <span class="detail-author-item">ISBN: <strong>{{ libro.codigoBarras }}</strong></span>
          </div>
        </div>

        <!-- Summary -->
        <div class="detail-summary-section">
          <h3>Resumen de la Obra</h3>
          <p class="detail-summary-text">
            {{ libro.resumen || 'Esta obra especializada pertenece al catálogo de la Facultad de Medicina Humana de la Universidad Ricardo Palma. Constituye una pieza clave de estudio para la especialidad de ' + (libro.categoria || 'Medicina') + '.' }}
          </p>
          
          <div class="detail-category-tags">
            <span class="category-tag">{{ libro.categoria || 'Medicina Humana' }}</span>
            <span class="category-tag">Publicación: {{ libro.anio }}</span>
            <span class="category-tag">Páginas: {{ libro.paginas || 'N/A' }}</span>
            <span class="category-tag">Clasificación: {{ libro.nroClasificacion }}</span>
          </div>
        </div>

        <!-- Physical availability table -->
        <div class="availability-table-panel">
          <div class="availability-header">
            <h4 class="availability-title">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
              Disponibilidad Física en Biblioteca
            </h4>
            <span class="availability-badge">Ejemplares: {{ libro.ejemplar }}</span>
          </div>

          <table class="availability-table">
            <thead>
              <tr>
                <th>Código Barras</th>
                <th>Ubicación</th>
                <th>Signatura Topográfica</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td style="font-family: monospace; font-weight: 600;">{{ libro.codigoBarras }}</td>
                <td>Sala General - 2do Piso</td>
                <td style="font-weight: 600;">{{ libro.nroClasificacion }}</td>
                <td>
                  <span class="status-indicator" :class="libro.estado === 'Disponible' ? 'dispo' : 'no-dispo'">
                    <span class="status-dot"></span>
                    {{ libro.estado }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Reviews / Reseñas section -->
    <div>
      <h3 class="reviews-title-header">Reseñas y Calificaciones de Estudiantes</h3>
      <div class="reviews-grid">
        <!-- Summary card -->
        <div class="rating-summary-card">
          <div class="rating-number">4.9</div>
          <div class="rating-stars">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
          </div>
          <div class="rating-count">Basado en 24 reseñas</div>

          <div class="rating-bar-row">
            <span>5 ★</span>
            <div class="rating-bar-bg"><div class="rating-bar-fill" style="width: 90%;"></div></div>
            <span>90%</span>
          </div>
          <div class="rating-bar-row">
            <span>4 ★</span>
            <div class="rating-bar-bg"><div class="rating-bar-fill" style="width: 8%;"></div></div>
            <span>8%</span>
          </div>
          <div class="rating-bar-row">
            <span>3 ★</span>
            <div class="rating-bar-bg"><div class="rating-bar-fill" style="width: 2%;"></div></div>
            <span>2%</span>
          </div>
        </div>

        <!-- Reviews list -->
        <div class="reviews-list-container">
          <div class="review-item-bubble">
            <div class="review-author-row">
              <div class="review-author-info">
                <div class="review-author-avatar blue">RM</div>
                <div>
                  <h4 class="review-author-name">Ricardo Mendoza</h4>
                  <span class="review-author-meta">Estudiante de Medicina, 4to año</span>
                </div>
              </div>
              <div class="rating-stars" style="font-size: 0.8rem;">★★★★★</div>
            </div>
            <p class="review-text">
              "Un recurso indispensable para rotaciones clínicas. Los diagramas fisiopatológicos y la clasificación terapéutica en este libro facilitan enormemente el estudio."
            </p>
          </div>

          <div class="review-item-bubble">
            <div class="review-author-row">
              <div class="review-author-info">
                <div class="review-author-avatar green">AP</div>
                <div>
                  <h4 class="review-author-name">Dra. Ana Paula Castro</h4>
                  <span class="review-author-meta">Docente de la Facultad de Medicina URP</span>
                </div>
              </div>
              <div class="rating-stars" style="font-size: 0.8rem;">★★★★★</div>
            </div>
            <p class="review-text">
              "Lectura obligatoria para el desarrollo de casos clínicos integrados en nuestra facultad. Altamente recomendado por la claridad de sus explicaciones."
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- Related books row -->
    <div class="related-books-section">
      <h3 class="reviews-title-header">Recursos Relacionados</h3>
      <div class="related-books-grid">
        <div v-for="relLibro in libros.filter(l => l.libroID !== libro.libroID).slice(0, 5)" :key="relLibro.libroID" class="book-card" @click="emit('view-book', relLibro)">
          <div class="book-card-cover-wrapper">
            <img v-if="relLibro.portada" :src="relLibro.portada" alt="Portada">
            <div v-else style="color: #94a3b8;">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
            </div>
          </div>
          <h4 class="book-card-title" style="font-size: 0.8rem; height: 2.3em;">{{ relLibro.titulo }}</h4>
          <div class="book-card-author" style="font-size: 0.72rem; margin-bottom: 2px;">{{ relLibro.autor }}</div>
        </div>
      </div>
    </div>
  </div>
</template>
