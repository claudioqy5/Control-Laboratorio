<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
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

let animationFrameId = null

onMounted(() => {
  typeText()
  runCarouselTimer()
  
  setTimeout(() => {
    if (basesDatosContainer.value) {
      const halfWidth = basesDatosContainer.value.scrollWidth / 2
      basesDatosContainer.value.scrollLeft = halfWidth
    }
    animateMarquee()
  }, 100)
})

onUnmounted(() => {
  if (carouselTimeoutId) clearTimeout(carouselTimeoutId)
  if (typingTimeout1) clearTimeout(typingTimeout1)
  if (typingTimeout2) clearTimeout(typingTimeout2)
  if (animationFrameId) cancelAnimationFrame(animationFrameId)
})

const basesDatosContainer = ref(null)

const databases = [
  {
    id: 1,
    name: '5 Minute Consult',
    provider: 'Wolters Kluwer / Lippincott',
    color: '#e07b2a',
    bgGradient: 'linear-gradient(135deg, #fff7f0 0%, #fff0e0 100%)',
    borderColor: '#f0a060',
    icon: '⏱️',
    tags: ['Decisión Clínica', 'Punto de Atención'],
    image: '5minuteconsult.jpg',
    description:
      'Herramienta de apoyo clínico basada en evidencia que brinda acceso a más de 2,000 monografías de enfermedades y condiciones, algoritmos diagnósticos, guías de tratamiento, calculadoras clínicas y más de 200 videos de procedimientos. Diseñada para responder preguntas clínicas críticas en minutos.',
    features: ['2,000+ monografías clínicas', 'Algoritmos diagnósticos', 'Calculadoras médicas', 'Información de fármacos A–Z'],
    url: 'https://loginwolterskluwer.urp.elogim.com/as/authorization.oauth2?client_id=HLRP.MedicalProcedures.Kauri&code_challenge=BFwEbHOyzTOTPCc1Rheulf5jXxml-wsy9b6rR5oVtJk&code_challenge_method=S256&response_type=code&pfidpadapterid=KauriAdapter&response_mode=form_post&scope=openid%20profile%20email&referer=https%3A%2F%2Fmenu.urp.elogim.com%2F&state=20680c61-06a5-4044-a033-f3ed9a183a3e&redirect_uri=https%3A%2F%2Fclinicalcontext.lww.com%2F.sso%2Fcode%2Foneid'
  },
  {
    id: 2,
    name: 'Access Medicina – Español',
    provider: 'McGraw-Hill Education',
    color: '#0066cc',
    bgGradient: 'linear-gradient(135deg, #f0f6ff 0%, #e0edff 100%)',
    borderColor: '#80bbff',
    icon: '📚',
    tags: ['Libros Médicos', 'Español'],
    image: 'accessmedicine.png',
    description:
      'Plataforma integral de McGraw-Hill con más de 100 títulos de referencia en español, incluyendo Harrison, Goodman & Gilman, y Diagnóstico Clínico y Tratamiento. Incluye recursos multimedia, herramienta Diagnosaurus® para diagnóstico diferencial, base de datos de fármacos, calculadoras clínicas y casos clínicos interactivos.',
    features: ['100+ libros en español', 'Diagnosaurus® diferencial', 'Videos y animaciones 3D', 'Autoevaluación interactiva'],
    url: 'https://accessmedicina.urp.elogim.com/'
  },
  {
    id: 3,
    name: 'BioDigital',
    provider: 'BioDigital, Inc.',
    color: '#1a1a2e',
    bgGradient: 'linear-gradient(135deg, #f0f0ff 0%, #e0e0f8 100%)',
    borderColor: '#9090d0',
    icon: '🫀',
    tags: ['Anatomía 3D', 'Visualización'],
    image: 'biodigital.jpg',
    description:
      'Plataforma de visualización 3D interactiva conocida como el "Google Maps del cuerpo humano". Presenta más de 8,000 estructuras anatómicas seleccionables individualmente, más de 600 condiciones y tratamientos simulados, disponible en 8 idiomas. Ideal para el aprendizaje anatómico y la comunicación médico-paciente.',
    features: ['8,000+ estructuras anatómicas', '600+ condiciones simuladas', 'Modelos 3D interactivos', 'Accesible desde cualquier dispositivo'],
    url: 'https://humanbiodigital.urp.elogim.com/login/create?code=URP9XF4H'
  },
  {
    id: 4,
    name: 'BMJ Best Practice',
    provider: 'British Medical Journal (BMJ)',
    color: '#c00000',
    bgGradient: 'linear-gradient(135deg, #fff5f5 0%, #ffe0e0 100%)',
    borderColor: '#f09090',
    icon: '🏥',
    tags: ['Guías Clínicas', 'Evidencia'],
    image: 'bjmbestpractice.png',
    description:
      'Herramienta de apoyo a la decisión clínica basada en evidencia del BMJ. Cubre el proceso completo de atención al paciente: evaluación de síntomas, diagnóstico diferencial, tratamiento y seguimiento. Incluye más de 250 calculadoras médicas, videos de procedimientos, miles de guías clínicas y seguimiento automático de créditos CME/CPD.',
    features: ['Diagnóstico diferencial', '250+ calculadoras médicas', 'Algoritmos de tratamiento', 'Hojas educativas para pacientes'],
    url: 'https://bestpracticebmj.urp.elogim.com/info/us/'
  },
  {
    id: 5,
    name: 'Clinical Key Español',
    provider: 'Elsevier',
    color: '#ff6600',
    bgGradient: 'linear-gradient(135deg, #fff8f0 0%, #fff0e0 100%)',
    borderColor: '#ffb080',
    icon: '🔑',
    tags: ['Libros & Journals', 'Clínica'],
    image: 'clinicalkeyespañol.png',
    description:
      'Plataforma de búsqueda clínica de Elsevier en español diseñada para la práctica diaria del médico. Brinda acceso a libros líderes en medicina, journals con revisión por pares, fichas de medicamentos y pautas de práctica clínica. Actualizada continuamente con contenido de más de 1,000 revistas biomédicas de Elsevier.',
    features: ['1,000+ revistas biomédicas', 'Libros clínicos en español', 'Fichas de medicamentos', 'Imágenes y videos clínicos'],
    url: 'https://clinicalkey.urp.elogim.com/#!/'
  },
  {
    id: 6,
    name: 'Clinical Key Student',
    provider: 'Elsevier',
    color: '#e05a00',
    bgGradient: 'linear-gradient(135deg, #fff8f0 0%, #ffefe0 100%)',
    borderColor: '#ffa060',
    icon: '🎓',
    tags: ['Estudiantes', 'Autoevaluación'],
    image: 'clinicalkeystudent.png',
    description:
      'Plataforma educativa de Elsevier para estudiantes de medicina y ciencias de la salud. Incluye los mejores libros de texto como Gray\'s Anatomy for Students, Costanzo Physiology y más. Ofrece tarjetas de estudio (flashcards), miles de preguntas de autoevaluación, análisis de desempeño docente y acceso offline.',
    features: ['Flashcards personalizables', '4,700+ preguntas de examen', 'Notas y marcadores colaborativos', 'Acceso offline móvil'],
    url: 'https://clinicalkeystudent.urp.elogim.com/student/login'
  },
  {
    id: 7,
    name: 'DynaMedex',
    provider: 'EBSCO Health',
    color: '#005f9e',
    bgGradient: 'linear-gradient(135deg, #f0f7ff 0%, #ddeeff 100%)',
    borderColor: '#70b0e0',
    icon: '⚕️',
    tags: ['Decisión Clínica', 'Fármacos'],
    image: 'dynamedex.png',
    description:
      'Solución integral de apoyo a la decisión clínica de EBSCO que combina el contenido basado en evidencia de DynaMed con la información farmacológica avanzada de Micromedex. Ofrece búsqueda con IA (Dyna AI), interacciones medicamentosas, compatibilidad IV, calculadoras clínicas, árboles de decisión y actualización diaria de contenido.',
    features: ['Contenido actualizado diariamente', 'Interacciones de medicamentos', 'Integración con EHR (FHIR)', 'Créditos CME/MOC'],
    url: 'https://dynamedex.urp.elogim.com/'
  },
  {
    id: 8,
    name: 'Ebooks de Ovid',
    provider: 'Wolters Kluwer (Books@Ovid)',
    color: '#4a0072',
    bgGradient: 'linear-gradient(135deg, #f8f0ff 0%, #ede0ff 100%)',
    borderColor: '#c090e0',
    icon: '📖',
    tags: ['Libros Electrónicos', 'Referencia'],
    image: 'ovid.jpg',
    description:
      'Plataforma web de libros electrónicos de Wolters Kluwer que reúne miles de textos médicos autorizados en un entorno interligado. Permite búsqueda en lenguaje natural, descarga de capítulos en PDF, anotaciones personales y acceso a colecciones multidisciplinarias de medicina, enfermería, farmacología y salud pública.',
    features: ['Miles de libros de texto médicos', 'Descarga de capítulos en PDF', 'Anotaciones y marcadores', 'Colecciones especializadas'],
    url: 'https://oceovid.urp.elogim.com/booksbrowse?contentLang=spa,eng'
  },
  {
    id: 9,
    name: 'Health Library Clerkship',
    provider: 'LWW / Wolters Kluwer',
    color: '#006a4e',
    bgGradient: 'linear-gradient(135deg, #f0fff8 0%, #d0f0e8 100%)',
    borderColor: '#70c0a0',
    icon: '🩺',
    tags: ['Rotaciones Clínicas', 'Residentes'],
    image: 'healthlibrary.jpg',
    description:
      'Colección digital de LWW (Lippincott Williams & Wilkins) diseñada para apoyar al estudiante durante las 6 rotaciones clínicas principales: Medicina Interna, Cirugía, Pediatría, Gineco-Obstetricia, Psiquiatría y Medicina Familiar. Incluye más de 30 libros de las series Blueprints y Step-Up, 150+ casos clínicos y 4,700+ preguntas de autoevaluación.',
    features: ['6 rotaciones clínicas cubiertas', '30+ libros Blueprints/Step-Up', '150+ casos clínicos', '4,700+ preguntas MCQ'],
    url: 'https://clerkship.lwwhealthlibrary.com/index.aspx?rotationId=0'
  },
  {
    id: 10,
    name: 'New England Journal of Medicine',
    provider: 'Massachusetts Medical Society',
    color: '#b22222',
    bgGradient: 'linear-gradient(135deg, #fff5f5 0%, #ffeded 100%)',
    borderColor: '#e09090',
    icon: '📰',
    tags: ['Journal Líder', 'Investigación'],
    image: 'neom.jpg',
    description:
      'La revista médica de mayor impacto y prestigio en el mundo, publicada semanalmente desde 1812. Con un factor de impacto de 78.5 (2024), publica investigaciones originales, revisiones clínicas, casos y comentarios editoriales que definen las guías de práctica médica global. Tasa de aceptación aproximada del 5%.',
    features: ['Factor de impacto: 78.5', 'Publicación semanal desde 1812', 'Investigación práctica-cambiante', 'Videos y casos interactivos NEJM'],
    url: 'https://nejm.urp.elogim.com/'
  },
  {
    id: 11,
    name: 'Revistas Ovid',
    provider: 'Wolters Kluwer (Journals@Ovid)',
    color: '#7b3f9e',
    bgGradient: 'linear-gradient(135deg, #f9f0ff 0%, #ede0ff 100%)',
    borderColor: '#c090dd',
    icon: '📑',
    tags: ['Journals', 'Texto Completo'],
    image: 'revistasovid.jpg',
    description:
      'Base de datos bibliográfica agregada de Wolters Kluwer que reúne cientos de revistas de más de 50 editoriales y sociedades científicas, incluyendo el catálogo completo de Lippincott® (LWW). Permite búsqueda de revistas y descarga en PDF o formato completo Ovid.',
    features: ['Cientos de revistas científicas', 'Acceso texto completo LWW', 'Vinculación con MEDLINE', 'Gestión de citas integrada'],
    url: 'https://oviddc2.urp.elogim.com/ovid-new-a/ovidweb.cgi?QS2=434f4e1a73d37e8c79e5d8c142641a542280cb57f0416e41d37d53a4d3815bf29da7a83e487343ffffa6718412c0ffea71f57506abe817240749582d80fe2e6fc5f8c2ebcd70909b59ce62630189ba4083fa4dd0d125180a24206cd935bd149dac3b7f1c0aa6bfd342d180eaa7f86eacc822b42709cf3ec2c586abdee77ae0287b547be6903a4dd2f3c534cff5da574e913fec17c05fbc8a1aeab0bc6c07a4e80db9f5e3bf56e2e9ec70bb9df0976e2f23b57f1770a3df8ff3ece399f88f1b69cf541864e834848186735c5b60d7eecd648e142165148443fb0eec92300e1878dbd74a3f7167356feeddef15fc9466f9'
  },
  {
    id: 12,
    name: 'Springer Link',
    provider: 'Springer Nature',
    color: '#d4770a',
    bgGradient: 'linear-gradient(135deg, #fff9f0 0%, #ffefd8 100%)',
    borderColor: '#f0b060',
    icon: '🔬',
    tags: ['Investigación', 'Ciencias Biomédicas'],
    image: 'springerlink.jpg',
    description:
      'Plataforma de acceso de Springer Nature con más de 10 millones de documentos científicos: journals revisados por pares, libros electrónicos, series de libros, obras de referencia, protocolos y actas de congresos. Cubre todas las especialidades médicas y áreas de investigación biomédica, con búsqueda avanzada y contenido semánticamente interligado.',
    features: ['10+ millones de documentos', 'Todas las especialidades médicas', 'E-books y journals integrados', 'Búsqueda avanzada por disciplina'],
    url: 'https://link.springer.com/journals/browse-subject?subject=HEALTH_SCIENCES'
  }
]

const doubledDatabases = computed(() => [...databases, ...databases])

let isDragging = false
let isHovered = false
let startX = 0
let scrollStart = 0
let dragVelocity = 0
let lastTime = 0
let lastX = 0
let dragDistance = 0

const startDrag = (e) => {
  isDragging = true
  dragDistance = 0
  startX = e.clientX
  lastX = e.clientX
  scrollStart = basesDatosContainer.value.scrollLeft
  dragVelocity = 0
  lastTime = performance.now()
  
  if (basesDatosContainer.value) {
    basesDatosContainer.value.style.scrollBehavior = 'auto'
  }
}

const onDrag = (e) => {
  if (!isDragging) return
  const dx = e.clientX - lastX
  dragDistance += Math.abs(dx)
  
  const totalDx = e.clientX - startX
  basesDatosContainer.value.scrollLeft = scrollStart - totalDx
  
  const container = basesDatosContainer.value
  const halfWidth = container.scrollWidth / 2
  if (container.scrollLeft <= 0) {
    container.scrollLeft += halfWidth
    scrollStart += halfWidth
  } else if (container.scrollLeft >= halfWidth) {
    container.scrollLeft -= halfWidth
    scrollStart -= halfWidth
  }
  
  const now = performance.now()
  const dt = now - lastTime
  if (dt > 0) {
    const instantaneousX = e.clientX
    const velocity = (instantaneousX - lastX) / dt * 16.67
    dragVelocity = dragVelocity * 0.2 + velocity * 0.8
    lastX = instantaneousX
    lastTime = now
  }
}

const stopDrag = () => {
  isDragging = false
  if (basesDatosContainer.value) {
    basesDatosContainer.value.style.scrollBehavior = 'smooth'
  }
}

const onMouseLeave = () => {
  isHovered = false
  stopDrag()
}

const onMouseEnter = () => {
  isHovered = true
}

const onTouchStart = (e) => {
  if (e.touches.length > 0) {
    startDrag({
      clientX: e.touches[0].clientX
    })
  }
}

const onTouchMove = (e) => {
  if (e.touches.length > 0) {
    onDrag({
      clientX: e.touches[0].clientX
    })
  }
}

const handleCardClick = (url, name) => {
  if (dragDistance > 5) return
  openDatabase(url, name)
}

const animateMarquee = () => {
  const container = basesDatosContainer.value
  if (container) {
    if (!isDragging) {
      if (!isHovered) {
        if (Math.abs(dragVelocity) > 0.1) {
          container.scrollLeft -= dragVelocity
          dragVelocity *= 0.96
        } else {
          container.scrollLeft -= 1.0 // Moves from left to right (scrollLeft decreases)
        }
      } else {
        if (Math.abs(dragVelocity) > 0.1) {
          container.scrollLeft -= dragVelocity
          dragVelocity *= 0.96
        }
      }

      const halfWidth = container.scrollWidth / 2
      if (container.scrollLeft <= 0) {
        container.scrollLeft += halfWidth
      } else if (container.scrollLeft >= halfWidth) {
        container.scrollLeft -= halfWidth
      }
    }
  }
  animationFrameId = requestAnimationFrame(animateMarquee)
}

const openDatabase = (url, name) => {
  emit('show-toast', `Abriendo ${name}...`, 'success')
  window.open(url, '_blank')
}

const darkenColor = (hex, percent = 20) => {
  const num = parseInt(hex.replace("#",""), 16),
  amt = Math.round(2.55 * percent),
  R = (num >> 16) - amt,
  G = (num >> 8 & 0x00FF) - amt,
  B = (num & 0x0000FF) - amt;
  return "#" + (0x1000000 + (R<0?0:R>255?255:R)*0x10000 + (G<0?0:G>255?255:G)*0x100 + (B<0?0:B>255?255:B)).toString(16).slice(1);
}

const getGradient = (color) => {
  return `linear-gradient(135deg, ${color}, ${darkenColor(color, 25)})`
}

const getShortName = (name) => {
  if (name.includes('–')) {
    return name.split('–')[0].trim().replace(' ', '<br>')
  }
  if (name.includes('-')) {
    return name.split('-')[0].trim().replace(' ', '<br>')
  }
  if (name === 'New England Journal of Medicine') {
    return 'NEJM'
  }
  const words = name.split(' ')
  if (words.length > 2) {
    return `${words[0]} ${words[1]}<br>${words.slice(2).join(' ')}`
  } else if (words.length === 2) {
    return `${words[0]}<br>${words[1]}`
  }
  return name
}

const getImageUrl = (imageName) => {
  return new URL(`../assets/bases de datos/${imageName}`, import.meta.url).href
}
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
      <div>
        <div class="home-section-header">
          <div>
            <h2 class="section-title">Nuevas Bases de Datos</h2>
            <p class="section-subtitle">Herramientas premium integradas para investigación clínica avanzada.</p>
          </div>
        </div>

        <div class="bases-datos-marquee-wrapper">
          <div 
            class="bases-datos-row" 
            ref="basesDatosContainer"
            @mousedown="startDrag"
            @mousemove="onDrag"
            @mouseup="stopDrag"
            @mouseleave="onMouseLeave"
            @mouseenter="onMouseEnter"
            @touchstart="onTouchStart"
            @touchmove="onTouchMove"
            @touchend="stopDrag"
          >
            <div 
              v-for="(db, idx) in doubledDatabases" 
              :key="`${db.id}-${idx}`" 
              class="db-card" 
              @click="handleCardClick(db.url, db.name)"
            >
              <div class="db-card-image">
                <img :src="getImageUrl(db.image)" :alt="db.name" draggable="false" />
                <div class="badge-db" :style="{ backgroundColor: db.color, zIndex: 1 }">{{ db.provider }}</div>
              </div>
              <div class="db-card-content">
                <h3 class="db-card-title">{{ db.name }}</h3>
                <p class="db-card-desc">{{ db.description.length > 150 ? db.description.slice(0, 147) + '...' : db.description }}</p>
              </div>
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
            <span>Sábados: 08:00 - 15:00</span>
          </div>
        </div>

        <div class="info-item">
          <div class="info-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
          </div>
          <div class="info-text">
            <h4>Ubicación</h4>
            <p>Campus URP - Facultad de Medicina</p>
            <span>Piso 4, Ala Sur</span>
          </div>
        </div>

        <div class="info-item">
          <div class="info-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>
          </div>
          <div class="info-text">
            <h4>Contacto Directo</h4>
            <p>fvalero@urp.edu.pe</p>
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
