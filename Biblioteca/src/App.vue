<script setup>
import { ref, onMounted } from 'vue'
import logoFAMURP from './assets/logoFAMURP.png'

// Component imports
import Header from './components/Header.vue'
import Footer from './components/Footer.vue'
import InicioView from './components/InicioView.vue'
import CatalogoView from './components/CatalogoView.vue'
import BasesDatosView from './components/BasesDatosView.vue'
import PerfilView from './components/PerfilView.vue'
import DetalleLibroView from './components/DetalleLibroView.vue'

const currentView = ref('inicio')
const selectedLibro = ref(null)

// Navigation state transfer variables
const heroSearchQuery = ref('')
const filterEspecialidades = ref([])

const toast = ref({ show: false, message: '', type: 'success' })

const showToast = (msg, type = 'success') => {
  toast.value = { show: true, message: msg, type }
  setTimeout(() => {
    toast.value.show = false
  }, 3000)
}

// Student Mock Data (Shared State)
const loans = ref([
  {
    prestamoID: 1,
    titulo: 'Anatomía Humana: Descriptiva, Topográfica y Funcional',
    autor: 'Henri Rouvière, André Delmas',
    devolucion: '24 Jun, 2026',
    diasRestantes: 8,
    portada: ''
  },
  {
    prestamoID: 2,
    titulo: 'Microbiología Médica de Jawetz, Melnick y Adelberg',
    autor: 'Stefan Riedel, Stephen A. Morse',
    devolucion: '28 Jun, 2026',
    diasRestantes: 12,
    portada: ''
  }
])

const favorites = ref([
  { libroID: 1, titulo: 'Principios de Medicina Interna de Harrison', autor: 'Dennis L. Kasper, Anthony S. Fauci' },
  { libroID: 4, titulo: 'Las Bases Farmacológicas de la Terapéutica', autor: 'Alfred Goodman Gilman, Laurence Brunton' }
])

const activities = ref([
  { fecha: '12 Jun, 2026', actividad: 'Préstamo Activo', detalle: 'Microbiología Médica de Jawetz', estado: 'Pendiente' },
  { fecha: '10 Jun, 2026', actividad: 'Préstamo Activo', detalle: 'Anatomía Humana Rouvière', estado: 'Pendiente' },
  { fecha: '04 Jun, 2026', actividad: 'Préstamo Devuelto', detalle: 'Principios de Medicina Interna de Harrison', estado: 'Completado' },
  { fecha: '28 May, 2026', actividad: 'Multa Pagada', detalle: 'Retraso en entrega - Código #REG-00234', estado: 'Solventado' }
])

// Books array
const libros = ref([])

// Load books from localStorage
const loadLibros = () => {
  const stored = localStorage.getItem('mockLibros')
  if (stored) {
    libros.value = JSON.parse(stored)
  } else {
    const defaultLibros = [
      {
        libroID: 1,
        nroRegistro: 'REG-00234',
        codigoBarras: '7701234567891',
        nroClasificacion: 'WB 100 G216 2021',
        titulo: 'Principios de Medicina Interna de Harrison',
        autor: 'Dennis L. Kasper, Anthony S. Fauci',
        anio: '2021',
        ejemplar: '1',
        portada: '',
        categoria: 'Cardiología',
        idioma: 'Español',
        estado: 'Disponible',
        resumen: 'El texto de medicina interna más confiable del mundo, proporciona respuestas clínicas precisas y un marco fisiopatológico sólido para el ejercicio de la medicina.',
        paginas: '4200',
        editorial: 'McGraw-Hill Education'
      },
      {
        libroID: 2,
        nroRegistro: 'REG-00235',
        codigoBarras: '7701234567892',
        nroClasificacion: 'WB 100 G216 2021',
        titulo: 'Principios de Medicina Interna de Harrison',
        autor: 'Dennis L. Kasper, Anthony S. Fauci',
        anio: '2021',
        ejemplar: '2',
        portada: '',
        categoria: 'Cardiología',
        idioma: 'Español',
        estado: 'Prestado',
        resumen: 'Ejemplar N° 2 de la obra cumbre Harrison. Un pilar fundamental para médicos generales, residentes y estudiantes de medicina humana.',
        paginas: '4200',
        editorial: 'McGraw-Hill Education'
      },
      {
        libroID: 3,
        nroRegistro: 'REG-00512',
        codigoBarras: '9788413821731',
        nroClasificacion: 'QS 4 G283 2022',
        titulo: 'Anatomía Humana: Descriptiva, Topográfica y Funcional',
        autor: 'Henri Rouvière, André Delmas',
        anio: '2022',
        ejemplar: '1',
        portada: '',
        categoria: 'Anatomía',
        idioma: 'Español',
        estado: 'Prestado',
        resumen: 'Esta obra clásica describe exhaustivamente las estructuras y órganos del cuerpo humano, orientando al alumno hacia la práctica clínica y quirúrgica.',
        paginas: '2150',
        editorial: 'Elsevier'
      },
      {
        libroID: 4,
        nroRegistro: 'REG-00891',
        codigoBarras: '9786071514134',
        nroClasificacion: 'QV 4 G653 2020',
        titulo: 'Las Bases Farmacológicas de la Terapéutica',
        autor: 'Alfred Goodman Gilman, Laurence Brunton',
        anio: '2020',
        ejemplar: '3',
        portada: '',
        categoria: 'Farmacología',
        idioma: 'Español',
        estado: 'Disponible',
        resumen: 'La biblia de la farmacología mundial. Explica de forma magistral las interacciones de los fármacos con sus receptores y sus aplicaciones en la terapéutica médica.',
        paginas: '1800',
        editorial: 'McGraw-Hill'
      },
      {
        libroID: 5,
        nroRegistro: 'REG-01124',
        codigoBarras: '9788418534010',
        nroClasificacion: 'QW 4 J473 2022',
        titulo: 'Microbiología Médica de Jawetz, Melnick y Adelberg',
        autor: 'Stefan Riedel, Stephen A. Morse',
        anio: '2022',
        ejemplar: '1',
        portada: '',
        categoria: 'Microbiología',
        idioma: 'Español',
        estado: 'Disponible',
        resumen: 'Proporciona una descripción clara y concisa de los fundamentos de la microbiología, virología, parasitología y micología aplicadas a la medicina humana.',
        paginas: '920',
        editorial: 'Lippincott Williams'
      }
    ]
    libros.value = defaultLibros
    localStorage.setItem('mockLibros', JSON.stringify(defaultLibros))
  }
}

// Navigation methods
const setView = (viewName) => {
  currentView.value = viewName
  if (viewName !== 'catalogo') {
    heroSearchQuery.value = ''
    filterEspecialidades.value = []
  }
}

const handleHeroSearch = (queryText) => {
  heroSearchQuery.value = queryText
  filterEspecialidades.value = []
  currentView.value = 'catalogo'
}

const handleHeroCategorySelect = (categoryName) => {
  heroSearchQuery.value = ''
  filterEspecialidades.value = [categoryName]
  currentView.value = 'catalogo'
}

const handleBookSelection = (libro) => {
  selectedLibro.value = libro
  currentView.value = 'detalle'
}

// Student loan actions
const requestLoan = (libro) => {
  if (libro.estado !== 'Disponible') {
    showToast('Este libro no está disponible para préstamo en este momento.', 'error')
    return
  }
  
  libro.estado = 'Prestado'
  const index = libros.value.findIndex(l => l.libroID === libro.libroID)
  if (index !== -1) {
    libros.value[index].estado = 'Prestado'
    localStorage.setItem('mockLibros', JSON.stringify(libros.value))
  }

  loans.value.push({
    prestamoID: Date.now(),
    titulo: libro.titulo,
    autor: libro.autor,
    devolucion: '30 Jun, 2026',
    diasRestantes: 14,
    portada: libro.portada
  })

  activities.value.unshift({
    fecha: '17 Jun, 2026',
    actividad: 'Préstamo Activo',
    detalle: libro.titulo,
    estado: 'Pendiente'
  })

  showToast('Préstamo solicitado con éxito. Recógelo en el Ala Sur con tu carnet.', 'success')
}

const renewLoan = (loan) => {
  loan.devolucion = '10 Jul, 2026'
  loan.diasRestantes += 7
  showToast(`Préstamo renovado con éxito hasta el ${loan.devolucion}.`, 'success')
  
  activities.value.unshift({
    fecha: '17 Jun, 2026',
    actividad: 'Renovación',
    detalle: loan.titulo,
    estado: 'Completado'
  })
}

const toggleFavorite = (libro) => {
  const index = favorites.value.findIndex(f => f.libroID === libro.libroID)
  if (index !== -1) {
    favorites.value.splice(index, 1)
    showToast('Eliminado de tus favoritos.', 'info')
  } else {
    favorites.value.push({
      libroID: libro.libroID,
      titulo: libro.titulo,
      autor: libro.autor
    })
    showToast('Añadido a tus favoritos.', 'success')
  }
}

const removeFavoriteByIndex = (index) => {
  favorites.value.splice(index, 1)
  showToast('Eliminado de favoritos.', 'info')
}

const isFavorite = (libro) => {
  return favorites.value.some(f => f.libroID === libro.libroID)
}

onMounted(() => {
  loadLibros()
  window.addEventListener('storage', (e) => {
    if (e.key === 'mockLibros') {
      loadLibros()
    }
  })
})
</script>

<template>
  <div id="app">
    <!-- Header Component -->
    <Header 
      :currentView="currentView" 
      :logoFAMURP="logoFAMURP" 
      :class="{ 'header-overlay': currentView === 'inicio' }"
      @set-view="setView" 
      @show-toast="showToast" 
    />

    <!-- Hero Carousel outside main container (only for Inicio view) -->
    <InicioView 
      v-if="currentView === 'inicio'"
      :libros="libros"
      @set-view="setView"
      @view-book="handleBookSelection"
      @trigger-search="handleHeroSearch"
      @select-category="handleHeroCategorySelect"
      @show-toast="showToast"
    />

    <!-- Main View Section (non-Inicio views) -->
    <main v-if="currentView !== 'inicio'" class="main-layout">
      <!-- VIEW: Catálogo -->
      <CatalogoView 
        v-if="currentView === 'catalogo'"
        :libros="libros"
        :initialSearchQuery="heroSearchQuery"
        :initialCategoryFilter="filterEspecialidades"
        @view-book="handleBookSelection"
      />

      <!-- VIEW: Bases de Datos -->
      <BasesDatosView 
        v-if="currentView === 'bases-datos'"
        @show-toast="showToast"
      />

      <!-- VIEW: Mi Perfil -->
      <PerfilView 
        v-if="currentView === 'perfil'"
        :loans="loans"
        :favorites="favorites"
        :activities="activities"
        @renew-loan="renewLoan"
        @remove-favorite="removeFavoriteByIndex"
        @set-view="setView"
        @show-toast="showToast"
      />

      <!-- VIEW: Detalle del Libro -->
      <DetalleLibroView 
        v-if="currentView === 'detalle' && selectedLibro"
        :libro="selectedLibro"
        :libros="libros"
        :isFavorite="isFavorite(selectedLibro)"
        @set-view="setView"
        @select-category="handleHeroCategorySelect"
        @view-book="handleBookSelection"
        @request-loan="requestLoan"
        @toggle-favorite="toggleFavorite"
        @show-toast="showToast"
      />
    </main>

    <!-- Footer Component -->
    <Footer 
      @set-view="setView" 
      @show-toast="showToast" 
    />

    <!-- Toast message container -->
    <div class="toast-container">
      <div v-if="toast.show" class="toast" :class="toast.type">
        <svg v-if="toast.type === 'success'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"></polyline></svg>
        <svg v-else-if="toast.type === 'error'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
        <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
        <span>{{ toast.message }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-in {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
