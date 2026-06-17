<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import carouselVideo from '../assets/carousel1.mp4'
import carousel2Img from '../assets/carousel2.png'
import carousel3Img from '../assets/carousel3.png'

const props = defineProps({
  libros: {
    type: Array,
    required: true
  }
})

const emit = defineEmits([
  'trigger-search',
  'select-category',
  'view-book',
  'set-view',
  'show-toast'
])

const heroSearchQuery = ref('')

// Typing animation state
const typedLine1 = ref('')
const typedLine2 = ref('')
const line1Text = "BIBLIOTECA VIRTUAL"
const line2Text = "Y ESPECIALIZADA."
let typingTimeout1 = null
let typingTimeout2 = null

const typeText = () => {
  if (typingTimeout1) clearTimeout(typingTimeout1)
  if (typingTimeout2) clearTimeout(typingTimeout2)
  
  typedLine1.value = ''
  typedLine2.value = ''
  
  let i = 0
  let j = 0
  
  const typeLine1 = () => {
    if (i < line1Text.length) {
      typedLine1.value += line1Text.charAt(i)
      i++
      typingTimeout1 = setTimeout(typeLine1, 60)
    } else {
      typingTimeout2 = setTimeout(typeLine2, 150)
    }
  }
  
  const typeLine2 = () => {
    if (j < line2Text.length) {
      typedLine2.value += line2Text.charAt(j)
      j++
      typingTimeout2 = setTimeout(typeLine2, 60)
    }
  }
  
  typeLine1()
}

// Carousel state
const activeSlide = ref(0)
const slideCount = 3
let carouselTimeoutId = null
const isSearchFocused = ref(false)

const getSlideDuration = (slideIndex) => {
  return slideIndex === 0 ? 7000 : 5000
}

const runCarouselTimer = () => {
  if (carouselTimeoutId) clearTimeout(carouselTimeoutId)
  if (isSearchFocused.value) return
  
  const duration = getSlideDuration(activeSlide.value)
  carouselTimeoutId = setTimeout(() => {
    nextSlide()
  }, duration)
}

const nextSlide = () => {
  activeSlide.value = (activeSlide.value + 1) % slideCount
  runCarouselTimer()
}

const prevSlide = () => {
  activeSlide.value = (activeSlide.value - 1 + slideCount) % slideCount
  runCarouselTimer()
}

const goToSlide = (idx) => {
  activeSlide.value = idx
  runCarouselTimer()
}

const onSearchFocus = () => {
  isSearchFocused.value = true
  if (carouselTimeoutId) clearTimeout(carouselTimeoutId)
}

const onSearchBlur = () => {
  isSearchFocused.value = false
  setTimeout(() => {
    runCarouselTimer()
  }, 200)
}

const triggerSearch = () => {
  emit('trigger-search', heroSearchQuery.value)
}

watch(activeSlide, (newVal) => {
  if (newVal === 0) {
    typeText()
  }
})

onMounted(() => {
  typeText()
  runCarouselTimer()
})

onUnmounted(() => {
  if (carouselTimeoutId) clearTimeout(carouselTimeoutId)
  if (typingTimeout1) clearTimeout(typingTimeout1)
  if (typingTimeout2) clearTimeout(typingTimeout2)
})
</script>

<template>
  <div class="fade-in">
    <!-- Hero Carousel outside main layout style layout -->
    <div class="hero-carousel">
      <div class="carousel-track" :style="{ transform: `translateX(-${activeSlide * 100}%)` }">
        
        <!-- Slide 1: General & Search -->
        <div class="carousel-slide slide-1">
          <!-- Background Video -->
          <div class="slide-video-container">
            <video autoplay loop muted playsinline class="slide-video">
              <source :src="carouselVideo" type="video/mp4">
            </video>
            <div class="slide-video-overlay"></div>
          </div>

          <div class="hero-content slide-1-content">
            <span class="slide-badge-minimal">FACULTAD DE MEDICINA HUMANA</span>
            <h1 class="hero-title-giant">
              {{ typedLine1 }}<br v-if="typedLine1">
              <span class="outline-text" v-if="typedLine2">{{ typedLine2 }}</span><span class="cursor-blink" v-if="typedLine1">|</span>
            </h1>
            <p class="hero-subtitle-minimal">
              Acceso completo a la colección más completa de recursos académicos, investigaciones clínicas y guías de estudio de la Universidad Ricardo Palma.
            </p>
            
            <div class="search-box-wrapper-left">
              <div class="search-box-inner">
                <input v-model="heroSearchQuery" @keyup.enter="triggerSearch" @focus="onSearchFocus" @blur="onSearchBlur" type="text" placeholder="Buscar libros, revistas, artículos..." class="search-input-left">
                <button class="search-btn-left" @click="triggerSearch">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                </button>
              </div>
            </div>

            <div class="search-trends-left">
              <span>TENDENCIAS:</span>
              <a href="#" class="trend-tag-minimal" @click.prevent="emit('select-category', 'Anatomía')">ANATOMÍA</a>
              <span>•</span>
              <a href="#" class="trend-tag-minimal" @click.prevent="emit('select-category', 'Cardiología')">CARDIOLOGÍA</a>
              <span>•</span>
              <a href="#" class="trend-tag-minimal" @click.prevent="emit('select-category', 'Microbiología')">MICROBIOLOGÍA</a>
            </div>
          </div>
        </div>

        <!-- Slide 2: Bases de Datos Ads -->
        <div class="carousel-slide slide-2">
          <!-- Background Image -->
          <div class="slide-video-container">
            <img :src="carousel2Img" alt="Bases de Datos" class="slide-video">
            <div class="slide-video-overlay"></div>
          </div>

          <div class="hero-content">
            <span class="slide-badge">SUSCRIPCIÓN INSTITUCIONAL</span>
            <h1 class="hero-title">Bases de Datos Científicas Premium</h1>
            <p class="hero-subtitle">Accede de forma gratuita a Access Medicina, BMJ Best Practice, ClinicalKey, DynaMedex y más. Herramientas avanzadas para estudiantes e investigadores de la URP.</p>
            <div style="margin-top: 1.5rem;">
              <button class="btn btn-urp" @click="emit('set-view', 'bases-datos')">Explorar Bases de Datos</button>
            </div>
          </div>
        </div>

        <!-- Slide 3: Talleres de Formación / Publicidad -->
        <div class="carousel-slide slide-3">
          <!-- Background Image -->
          <div class="slide-video-container">
            <img :src="carousel3Img" alt="Talleres" class="slide-video">
            <div class="slide-video-overlay"></div>
          </div>

          <div class="hero-content">
            <span class="slide-badge">TALLERES 2026</span>
            <h1 class="hero-title">Formación en Redacción Científica</h1>
            <p class="hero-subtitle">Participa en las sesiones virtuales sobre gestores de referencias (Mendeley, EndNote) y normas Vancouver para tus trabajos de investigación.</p>
            <div style="margin-top: 1.5rem;">
              <button class="btn btn-secondary" @click="emit('show-toast', 'Redirigiendo al calendario de talleres.', 'success')">Ver Calendario de Sesiones</button>
            </div>
          </div>
        </div>

      </div>

      <!-- Carousel Controls -->
      <button class="carousel-arrow prev" @click="prevSlide" aria-label="Slide anterior">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="15 18 9 12 15 6"></polyline></svg>
      </button>
      <button class="carousel-arrow next" @click="nextSlide" aria-label="Siguiente slide">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="9 18 15 12 9 6"></polyline></svg>
      </button>

      <!-- Carousel Indicators / Dots -->
      <div class="carousel-dots">
        <button v-for="idx in slideCount" :key="idx" class="carousel-dot" :class="{ active: activeSlide === idx - 1 }" @click="goToSlide(idx - 1)" :title="`Ir al slide ${idx}`"></button>
      </div>
    </div>

    <!-- Rest of Home Content inside main wrapper -->
    <div class="home-inner-content">
      <!-- Section: Nuevas Bases de Datos -->
      <div>
        <div class="home-section-header">
          <div>
            <h2 class="section-title">Nuevas Bases de Datos</h2>
            <p class="section-subtitle">Herramientas premium integradas para investigación clínica avanzada.</p>
          </div>
          <div class="home-section-nav">
            <button class="round-nav-btn" @click="emit('show-toast', 'Desplazamiento a la izquierda.', 'info')">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>
            </button>
            <button class="round-nav-btn" @click="emit('show-toast', 'Desplazamiento a la derecha.', 'info')">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
            </button>
          </div>
        </div>

        <div class="bases-datos-row">
          <!-- Card 1: Access Medicina -->
          <div class="db-card" @click="window.open('https://accessmedicina.urp.elogim.com/', '_blank')">
            <div class="db-card-image">
              <div class="badge-db badge-premium">McGraw-Hill</div>
              <div style="background: linear-gradient(135deg, #0066cc, #003f99); width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; color: white; font-weight: 800; font-size: 1.2rem; text-align: center; padding: 1rem; letter-spacing: -0.03em;">Access<br>Medicina</div>
            </div>
            <div class="db-card-content">
              <h3 class="db-card-title">Access Medicina – Español</h3>
              <p class="db-card-desc">Más de 100 títulos en español: Harrison, Goodman &amp; Gilman y más. Incluye Diagnosaurus®, calculadoras clínicas y autoevaluación.</p>
            </div>
          </div>

          <!-- Card 2: BMJ Best Practice -->
          <div class="db-card" @click="window.open('https://bestpracticebmj.urp.elogim.com/info/us/', '_blank')">
            <div class="db-card-image">
              <div class="badge-db badge-updated">Actualización Diaria</div>
              <div style="background: linear-gradient(135deg, #c00000, #8b0000); width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; color: white; font-weight: 800; font-size: 1.1rem; text-align: center; padding: 1rem;">BMJ<br>Best Practice</div>
            </div>
            <div class="db-card-content">
              <h3 class="db-card-title">BMJ Best Practice</h3>
              <p class="db-card-desc">Guías clínicas basadas en evidencia del British Medical Journal. Diagnóstico diferencial, algoritmos de tratamiento y 250+ calculadoras médicas.</p>
            </div>
          </div>

          <!-- Card 3: DynaMedex -->
          <div class="db-card" @click="window.open('https://dynamedex.urp.elogim.com/', '_blank')">
            <div class="db-card-image">
              <div class="badge-db badge-open">EBSCO Health</div>
              <div style="background: linear-gradient(135deg, #005f9e, #003366); width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; color: white; font-weight: 800; font-size: 1.3rem; text-align: center; padding: 1rem;">DynaMedex</div>
            </div>
            <div class="db-card-content">
              <h3 class="db-card-title">DynaMedex</h3>
              <p class="db-card-desc">Combina DynaMed y Micromedex: evidencia clínica + información farmacológica avanzada. IA integrada, interacciones de medicamentos y actualización diaria.</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Grid split: Nuevas Adquisiciones y Sidebar panel -->
      <div class="home-split-grid">
        <!-- Left side acquisitions -->
        <div class="acquisitions-panel">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
            <h3 style="font-size: 1.2rem; font-weight: 800; color: var(--navy-dark);">Nuevas Adquisiciones</h3>
            <a href="#" @click.prevent="emit('set-view', 'catalogo')" style="font-size: 0.85rem; color: var(--urp-red); font-weight: 700; text-decoration: none;">Ver todo →</a>
          </div>

          <div class="acquisitions-grid">
            <div v-for="libro in libros.slice(0, 4)" :key="libro.libroID"
                 class="book-wrapper" @click="emit('view-book', libro)">
              <div class="book">
                <!-- Back content (visible behind cover) -->
                <div class="book-inner">
                  <div class="book-inner-meta">{{ libro.categoria || 'Medicina' }}</div>
                  <div class="book-inner-title">{{ libro.titulo }}</div>
                  <div class="book-inner-author">{{ libro.autor }}</div>
                  <div class="book-inner-year">{{ libro.anio || '' }}</div>
                  <span class="book-inner-status" :class="libro.estado === 'Disponible' ? 'disponible' : 'prestado'">
                    {{ libro.estado }}
                  </span>
                </div>
                <!-- Cover (flips open on hover) -->
                <div class="cover">
                  <img v-if="libro.portada" :src="libro.portada" alt="Portada" class="cover-img">
                  <div v-else class="cover-placeholder">
                    <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                      <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path>
                      <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path>
                    </svg>
                    <p>{{ libro.titulo }}</p>
                  </div>
                </div>
              </div>
              <div class="book-card-footer-3d">
                <span class="book-card-action">
                  Detalles
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><polyline points="9 18 15 12 9 6"></polyline></svg>
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Right side sidebar widgets -->
        <div class="sidebar-panel">
          <div class="research-widget">
            <h3>Recursos para Investigadores</h3>
            <div class="research-links">
              <div class="research-item">
                <div class="research-item-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
                </div>
                <div class="research-item-text">
                  <h4>Guías de Estilo APA/Vancouver</h4>
                  <p>Estandarización de citas médicas.</p>
                </div>
              </div>

              <div class="research-item">
                <div class="research-item-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="2" y1="12" x2="22" y2="12"></line><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path></svg>
                </div>
                <div class="research-item-text">
                  <h4>Repositorio Institucional</h4>
                  <p>Tesis y trabajos de investigación URP.</p>
                </div>
              </div>

              <div class="research-item">
                <div class="research-item-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
                </div>
                <div class="research-item-text">
                  <h4>Asesoría Bibliotecaria</h4>
                  <p>Agendar sesión personalizada.</p>
                </div>
              </div>
            </div>
            
            <button class="btn btn-urp" style="width: 100%; padding: 12px;" @click="emit('show-toast', 'Abriendo chat con el bibliotecario en línea...', 'success')">
              Consultar Bibliotecario Online
            </button>
          </div>

          <div class="workshop-widget">
            <h3>Talleres de Formación</h3>
            <p>Aprende a manejar gestores bibliográficos y bases de datos especializadas para tus investigaciones clínicas.</p>
            <a href="#" @click.prevent="emit('show-toast', 'Redirigiendo al calendario de talleres de medicina.', 'info')" style="color: white; font-weight: 700; text-decoration: none; display: flex; align-items: center; gap: 6px; font-size: 0.85rem;">
              Ver Calendario 📅
            </a>
          </div>
        </div>
      </div>

      <!-- Info Strip Bar -->
      <div class="info-strip">
        <div class="info-item">
          <div class="info-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
          </div>
          <div class="info-text">
            <h4>Horario de Atención</h4>
            <p>Lunes a Viernes: 08:00 - 21:00</p>
            <span>Sábados: 09:00 - 13:00</span>
          </div>
        </div>

        <div class="info-item">
          <div class="info-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
          </div>
          <div class="info-text">
            <h4>Ubicación</h4>
            <p>Campus URP - Facultad de Medicina</p>
            <span>Piso 2, Ala Sur</span>
          </div>
        </div>

        <div class="info-item">
          <div class="info-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
          </div>
          <div class="info-text">
            <h4>Contacto Directo</h4>
            <p>biblioteca.med@urp.edu.pe</p>
            <span>Tel: +51 1 708-0000 Anexo 212</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.book-wrapper {
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.book {
  position: relative;
  border-radius: 10px;
  width: 140px;
  height: 190px;
  background-color: whitesmoke;
  -webkit-box-shadow: 1px 1px 12px rgba(0, 0, 0, 0.3);
  box-shadow: 1px 1px 12px rgba(0, 0, 0, 0.3);
  -webkit-transform: preserve-3d;
  -ms-transform: preserve-3d;
  transform: preserve-3d;
  -webkit-perspective: 2000px;
  perspective: 2000px;
  display: -webkit-box;
  display: -ms-flexbox;
  display: flex;
  -webkit-box-align: center;
  -ms-flex-align: center;
  align-items: center;
  -webkit-box-pack: center;
  -ms-flex-pack: center;
  justify-content: center;
  color: #000;
}

.cover {
  top: 0;
  position: absolute;
  background-color: lightgray;
  width: 100%;
  height: 100%;
  border-radius: 10px;
  cursor: pointer;
  -webkit-transition: all 0.5s;
  transition: all 0.5s;
  -webkit-transform-origin: 0;
  -ms-transform-origin: 0;
  transform-origin: 0;
  -webkit-box-shadow: 1px 1px 12px rgba(0, 0, 0, 0.3);
  box-shadow: 1px 1px 12px rgba(0, 0, 0, 0.3);
  display: -webkit-box;
  display: -ms-flexbox;
  display: flex;
  -webkit-box-align: center;
  -ms-flex-align: center;
  align-items: center;
  -webkit-box-pack: center;
  -ms-flex-pack: center;
  justify-content: center;
}

.book:hover .cover {
  -webkit-transform: rotateY(-80deg);
  -ms-transform: rotateY(-80deg);
  transform: rotateY(-80deg);
}

.cover-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 10px;
}

.cover-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 0.75rem;
  text-align: center;
  background: linear-gradient(135deg, #1e3a8a, #0f172a);
  color: white;
  width: 100%;
  height: 100%;
  border-radius: 10px;
}

.cover-placeholder svg {
  width: 24px;
  height: 24px;
}

.cover-placeholder p {
  font-size: 0.65rem;
  font-weight: 700;
  margin-top: 6px;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.2;
}

.book-inner {
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  width: 100%;
  height: 100%;
  justify-content: space-between;
  box-sizing: border-box;
  background-color: #f8fafc;
  border-radius: 10px;
}

.book-inner-meta {
  font-size: 0.55rem;
  font-weight: 800;
  color: var(--urp-red);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.book-inner-title {
  font-size: 0.72rem;
  font-weight: 800;
  color: var(--navy-dark);
  line-height: 1.2;
  display: -webkit-box;
  -webkit-line-clamp: 4;
  -webkit-box-orient: vertical;
  overflow: hidden;
  margin-bottom: 2px;
}

.book-inner-author {
  font-size: 0.65rem;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.book-inner-year {
  font-size: 0.6rem;
  color: var(--text-muted);
}

.book-inner-status {
  font-size: 0.55rem;
  font-weight: 800;
  padding: 1px 4px;
  border-radius: 3px;
  align-self: flex-start;
  text-transform: uppercase;
}

.book-inner-status.disponible {
  background-color: #d1fae5;
  color: #065f46;
}

.book-inner-status.prestado {
  background-color: #fee2e2;
  color: #991b1b;
}

.book-card-footer-3d {
  margin-top: 0.5rem;
  text-align: center;
  width: 100%;
}

.book-card-action {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--navy-primary);
  display: inline-flex;
  align-items: center;
  gap: 3px;
}

.book-wrapper:hover .book-card-action {
  color: var(--urp-red);
}
</style>
