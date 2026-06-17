<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  libros: {
    type: Array,
    required: true
  },
  initialSearchQuery: {
    type: String,
    default: ''
  },
  initialCategoryFilter: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['view-book'])

const searchQuery = ref(props.initialSearchQuery)
const filterEspecialidades = ref([...props.initialCategoryFilter])
const filterAnio = ref('Todos')
const filterIdioma = ref('Todos')
const filterOnlyAvailable = ref(false)
const viewMode = ref('grid')
const currentPage = ref(1)
const pageSize = ref(6)

// Reset page on filter changes
watch([searchQuery, filterEspecialidades, filterAnio, filterIdioma, filterOnlyAvailable], () => {
  currentPage.value = 1
})

// Watch initial values from home search
watch(() => props.initialSearchQuery, (newVal) => {
  searchQuery.value = newVal
})

watch(() => props.initialCategoryFilter, (newVal) => {
  filterEspecialidades.value = [...newVal]
})

// Specialty category dynamic set
const availableCategories = computed(() => {
  const cats = new Set(['Anatomía', 'Pediatría', 'Cardiología', 'Neurología', 'Ginecología', 'Microbiología', 'Farmacología'])
  props.libros.forEach(l => {
    if (l.categoria) cats.add(l.categoria)
  })
  return Array.from(cats)
})

// Years set
const availableAnios = computed(() => {
  const years = new Set()
  props.libros.forEach(l => {
    if (l.anio) years.add(l.anio)
  })
  return Array.from(years).sort((a, b) => b - a)
})

// Computed filtered books
const filteredLibros = computed(() => {
  return props.libros.filter(l => {
    const matchesQuery = !searchQuery.value || 
      l.titulo.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      l.autor.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      (l.categoria && l.categoria.toLowerCase().includes(searchQuery.value.toLowerCase())) ||
      (l.codigoBarras && l.codigoBarras.includes(searchQuery.value))

    const matchesSpecialty = filterEspecialidades.value.length === 0 || 
      filterEspecialidades.value.includes(l.categoria)

    const matchesAnio = filterAnio.value === 'Todos' || l.anio === filterAnio.value

    const matchesIdioma = filterIdioma.value === 'Todos' || l.idioma === filterIdioma.value

    const matchesAvailable = !filterOnlyAvailable.value || l.estado === 'Disponible'

    return matchesQuery && matchesSpecialty && matchesAnio && matchesIdioma && matchesAvailable
  })
})

const paginatedLibros = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  const end = start + pageSize.value
  return filteredLibros.value.slice(start, end)
})

const totalPages = computed(() => {
  return Math.ceil(filteredLibros.value.length / pageSize.value) || 1
})

const nextPage = () => {
  if (currentPage.value < totalPages.value) currentPage.value++
}

const prevPage = () => {
  if (currentPage.value > 1) currentPage.value--
}

const goToPage = (page) => {
  currentPage.value = page
}
</script>

<template>
  <div class="fade-in">
    <div class="catalog-layout">
      <!-- Left Sidebar Filters -->
      <aside class="catalog-filters">
        <div>
          <h4 class="filter-section-title">Especialidad</h4>
          <div class="filter-options-list">
            <label v-for="cat in availableCategories" :key="cat" class="filter-checkbox-label">
              <input type="checkbox" v-model="filterEspecialidades" :value="cat">
              {{ cat }}
            </label>
          </div>
        </div>

        <div>
          <h4 class="filter-section-title">Año de Publicación</h4>
          <select v-model="filterAnio" class="filter-select">
            <option value="Todos">Cualquier año</option>
            <option v-for="y in availableAnios" :key="y" :value="y">{{ y }}</option>
          </select>
        </div>

        <div>
          <h4 class="filter-section-title">Idioma</h4>
          <div class="filter-options-list">
            <label class="filter-checkbox-label">
              <input type="radio" v-model="filterIdioma" value="Todos"> Todos
            </label>
            <label class="filter-checkbox-label">
              <input type="radio" v-model="filterIdioma" value="Español"> Español
            </label>
            <label class="filter-checkbox-label">
              <input type="radio" v-model="filterIdioma" value="Inglés"> Inglés
            </label>
          </div>
        </div>

        <div>
          <h4 class="filter-section-title">Disponibilidad</h4>
          <label class="filter-checkbox-label">
            <input type="checkbox" v-model="filterOnlyAvailable"> Solo disponibles ahora
          </label>
        </div>

        <div class="filter-info-box">
          ¿No encuentras lo que buscas? Solicita una adquisición a través de tu panel de usuario.
        </div>
      </aside>

      <!-- Right Catalog Listings -->
      <div class="catalog-main">
        <!-- Catalog Search Box -->
        <div class="catalog-search-bar">
          <input v-model="searchQuery" type="text" placeholder="Buscar por título, autor o ISBN..." class="catalog-search-input" @keyup.enter="currentPage = 1">
          <button class="catalog-search-btn" @click="currentPage = 1">Buscar</button>
        </div>

        <!-- Header and view switchers -->
        <div class="catalog-header-bar">
          <div class="results-count">
            Libros Encontrados ({{ filteredLibros.length }})
          </div>
          <div class="view-toggles">
            <button class="toggle-btn" :class="{ active: viewMode === 'grid' }" @click="viewMode = 'grid'" title="Ver en Cuadrícula">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect></svg>
            </button>
            <button class="toggle-btn" :class="{ active: viewMode === 'list' }" @click="viewMode = 'list'" title="Ver en Lista">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="8" y1="6" x2="21" y2="6"></line><line x1="8" y1="12" x2="21" y2="12"></line><line x1="8" y1="18" x2="21" y2="18"></line><line x1="3" y1="6" x2="3.01" y2="6"></line><line x1="3" y1="12" x2="3.01" y2="12"></line><line x1="3" y1="18" x2="3.01" y2="18"></line></svg>
            </button>
          </div>
        </div>

        <!-- Books Rendering Grid Mode -->
        <div v-if="viewMode === 'grid'" class="catalog-books-grid">
          <div v-for="libro in paginatedLibros" :key="libro.libroID" class="book-card" @click="emit('view-book', libro)">
            <div class="book-card-cover-wrapper">
              <img v-if="libro.portada" :src="libro.portada" alt="Portada">
              <div v-else class="book-card-placeholder-icon">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
              </div>
              <span class="badge-status" :class="libro.estado === 'Disponible' ? 'disponible' : 'prestado'">
                {{ libro.estado }}
              </span>
            </div>
            <div class="book-card-meta">{{ libro.categoria || 'Medicina' }}</div>
            <h4 class="book-card-title">{{ libro.titulo }}</h4>
            <div class="book-card-author">{{ libro.autor }}</div>
            <div class="book-card-footer">
              <span class="book-card-year">{{ libro.anio || '-' }}</span>
              <span class="book-card-action">
                Ver detalles
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="9 18 15 12 9 6"></polyline></svg>
              </span>
            </div>
          </div>
        </div>

        <!-- Books Rendering List Mode -->
        <div v-else class="catalog-books-list">
          <div v-for="libro in paginatedLibros" :key="libro.libroID" class="book-list-item" @click="emit('view-book', libro)">
            <div class="book-list-cover">
              <img v-if="libro.portada" :src="libro.portada" alt="Portada">
              <div v-else style="color: #94a3b8;">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
              </div>
            </div>
            <div class="book-list-content">
              <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 2px;">
                <span style="font-size: 0.7rem; font-weight: 800; color: var(--urp-red); text-transform: uppercase; letter-spacing: 0.05em;">{{ libro.categoria || 'Medicina' }}</span>
                <span class="status-pill" :class="libro.estado === 'Disponible' ? 'completado' : 'pendiente'" style="padding: 2px 8px; font-size: 0.7rem;">{{ libro.estado }}</span>
              </div>
              <h3 class="book-list-title">{{ libro.titulo }}</h3>
              <div class="book-list-author">{{ libro.autor }}</div>
              <p class="book-list-summary">{{ libro.resumen || 'Sin resumen disponible para esta obra en la biblioteca virtual.' }}</p>
              
              <div class="book-list-footer">
                <span style="font-size: 0.8rem; color: var(--text-muted);">Edición/Año: <strong>{{ libro.anio }}</strong></span>
                <span style="font-size: 0.8rem; color: var(--text-muted);">Clasificación: <strong>{{ libro.nroClasificacion }}</strong></span>
                <span class="book-card-action" style="font-size: 0.85rem;">Ver ficha →</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination Bar -->
        <div class="pagination-wrapper" v-if="totalPages > 1">
          <button class="pagination-btn" @click="prevPage" :disabled="currentPage === 1">Anterior</button>
          <button v-for="page in totalPages" :key="page" class="pagination-btn" :class="{ active: currentPage === page }" @click="goToPage(page)">
            {{ page }}
          </button>
          <button class="pagination-btn" @click="nextPage" :disabled="currentPage === totalPages">Siguiente</button>
        </div>

        <!-- Empty State -->
        <div v-if="filteredLibros.length === 0" style="text-align: center; padding: 4rem 2rem; background: white; border-radius: 16px; border: 1px solid var(--border-color);">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#94a3b8" stroke-width="1.5" style="margin-bottom: 1rem;"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <h3 style="color: var(--navy-dark); font-weight: 800; font-size: 1.15rem;">No se encontraron libros</h3>
          <p style="color: var(--text-muted); font-size: 0.9rem; margin-top: 4px;">Intenta ajustando tus filtros de especialidad o realizando una nueva búsqueda.</p>
        </div>
      </div>
    </div>
  </div>
</template>
