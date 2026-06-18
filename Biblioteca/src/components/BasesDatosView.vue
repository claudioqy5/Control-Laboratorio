<script setup>
import { ref, computed } from 'vue'

const emit = defineEmits(['show-toast'])

const getImageUrl = (imageName) => {
  return new URL(`../assets/bases de datos/${imageName}`, import.meta.url).href
}

const databases = [
  {
    id: 1,
    name: '5 Minute Consult',
    provider: 'Wolters Kluwer / Lippincott',
    color: '#388e3c',
    bgGradient: 'linear-gradient(135deg, #f1f8e9 0%, #e8f5e9 100%)',
    borderColor: '#4caf50',
    icon: '⏱️',
    tags: ['Decisión Clínica', 'Punto de Atención'],
    image: '5minuteconsult.jpg',
    logoBg: '#ffffff',
    description:
      'Herramienta de apoyo clínico basada en evidencia que brinda acceso a más de 2,000 monografías de enfermedades y condiciones, algoritmos diagnósticos, guías de tratamiento, calculadoras clínicas y más de 200 videos de procedimientos. Diseñada para responder preguntas clínicas críticas en minutos.',
    features: ['2,000+ monografías clínicas', 'Algoritmos diagnósticos', 'Calculadoras médicas', 'Información de fármacos A–Z'],
    url: 'https://loginwolterskluwer.urp.elogim.com/as/authorization.oauth2?client_id=HLRP.MedicalProcedures.Kauri&code_challenge=BFwEbHOyzTOTPCc1Rheulf5jXxml-wsy9b6rR5oVtJk&code_challenge_method=S256&response_type=code&pfidpadapterid=KauriAdapter&response_mode=form_post&scope=openid%20profile%20email&referer=https%3A%2F%2Fmenu.urp.elogim.com%2F&state=20680c61-06a5-4044-a033-f3ed9a183a3e&redirect_uri=https%3A%2F%2Fclinicalcontext.lww.com%2F.sso%2Fcode%2Foneid'
  },
  {
    id: 2,
    name: 'Access Medicina – Español',
    provider: 'McGraw-Hill Education',
    color: '#d32f2f',
    bgGradient: 'linear-gradient(135deg, #ffebee 0%, #ffcdd2 100%)',
    borderColor: '#ef5350',
    icon: '📚',
    tags: ['Libros Médicos', 'Español'],
    image: 'accessmedicine.png',
    logoBg: '#ffffff',
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
    image: 'biodigital2.png',
    logoBg: '#ffffff',
    description:
      'Plataforma de visualización 3D interactiva conocida como el "Google Maps del cuerpo humano". Presenta más de 8,000 estructuras anatómicas seleccionables individualmente, más de 600 condiciones y tratamientos simulados, disponible en 8 idiomas. Ideal para el aprendizaje anatómico y la comunicación médico-paciente.',
    features: ['8,000+ estructuras anatómicas', '600+ condiciones simuladas', 'Modelos 3D interactivos', 'Accesible desde cualquier dispositivo'],
    url: 'https://humanbiodigital.urp.elogim.com/login/create?code=URP9XF4H'
  },
  {
    id: 4,
    name: 'BMJ Best Practice',
    provider: 'British Medical Journal (BMJ)',
    color: '#0d47a1',
    bgGradient: 'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)',
    borderColor: '#2196f3',
    icon: '🏥',
    tags: ['Guías Clínicas', 'Evidencia'],
    image: 'bjmbestpractice.png',
    logoBg: '#ffffff',
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
    logoBg: '#ffffff',
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
    logoBg: '#ffffff',
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
    logoBg: '#ffffff',
    description:
      'Solución integral de apoyo a la decisión clínica de EBSCO que combina el contenido basado en evidencia de DynaMed con la información farmacológica avanzada de Micromedex. Ofrece búsqueda con IA (Dyna AI), interacciones medicamentosas, compatibilidad IV, calculadoras clínicas, árboles de decisión y actualización diaria de contenido.',
    features: ['Contenido actualizado diariamente', 'Interacciones de medicamentos', 'Integración con EHR (FHIR)', 'Créditos CME/MOC'],
    url: 'https://dynamedex.urp.elogim.com/'
  },
  {
    id: 8,
    name: 'Ebooks de Ovid',
    provider: 'Wolters Kluwer (Books@Ovid)',
    color: '#0b5a93',
    bgGradient: 'linear-gradient(135deg, #e1f5fe 0%, #b3e5fc 100%)',
    borderColor: '#29b6f6',
    icon: '📖',
    tags: ['Libros Electrónicos', 'Referencia'],
    image: 'ovid.jpg',
    logoBg: '#0b5a93',
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
    logoBg: '#ffffff',
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
    logoBg: '#e5392b',
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
    logoBg: '#ffffff',
    description:
      'Base de datos bibliográfica agregada de Wolters Kluwer que reúne cientos de revistas de más de 50 editoriales y sociedades científicas, incluyendo el catálogo completo de Lippincott® (LWW). Permite búsqueda simultánea en libros, journals y bases de datos; vinculación bidireccional con MEDLINE y descarga en PDF o formato completo Ovid.',
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
    image: 'springerlink2.jpg',
    logoBg: '#ffffff',
    description:
      'Plataforma de acceso de Springer Nature con más de 10 millones de documentos científicos: journals revisados por pares, libros electrónicos, series de libros, obras de referencia, protocolos y actas de congresos. Cubre todas las especialidades médicas y áreas de investigación biomédica, con búsqueda avanzada y contenido semánticamente interligado.',
    features: ['10+ millones de documentos', 'Todas las especialidades médicas', 'E-books y journals integrados', 'Búsqueda avanzada por disciplina'],
    url: 'https://link.springer.com/journals/browse-subject?subject=HEALTH_SCIENCES'
  }
]

const searchQuery = ref('')
const filteredDatabases = computed(() => {
  if (!searchQuery.value) return databases
  const query = searchQuery.value.toLowerCase().trim()
  return databases.filter(db => 
    db.name.toLowerCase().includes(query) ||
    db.provider.toLowerCase().includes(query) ||
    db.tags.some(tag => tag.toLowerCase().includes(query))
  )
})
</script>

<template>
  <div class="fade-in">
    <!-- Header -->
    <div class="db-header-left">
      <div class="db-header-top-row">
        <h2 class="section-title-left">
          Bases de Datos
          <span class="title-highlight-wrap">
            <span class="title-green">CIENTIFICAS</span>
            <span class="title-script">y Médicas</span>
          </span>
        </h2>
        
        <!-- Search Bar -->
        <div class="db-search-wrapper">
          <svg class="db-search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input 
            v-model="searchQuery" 
            type="text" 
            placeholder="Buscar por nombre o proveedor..." 
            class="db-search-input"
          />
        </div>
      </div>
      <div class="header-divider-line"></div>
      <p class="section-subtitle-left">
        Acceso institucional a las principales plataformas de investigación clínica y recursos biomédicos
        suscritos por la Universidad Ricardo Palma.
      </p>
    </div>

    <!-- Main Content Area with vertical split -->
    <div class="db-split-layout">
      <!-- Left Sidebar: Stats -->
      <div class="db-left-stats">
        <div class="db-stat-card">
          <div class="stat-icon-wrapper">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
          </div>
          <div class="stat-info">
            <span class="stat-value">12</span>
            <span class="stat-title">Bases de Datos</span>
          </div>
        </div>
        
        <div class="db-stat-card">
          <div class="stat-icon-wrapper">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>
          </div>
          <div class="stat-info">
            <span class="stat-value">10M+</span>
            <span class="stat-title">Documentos suscriptos</span>
          </div>
        </div>

        <div class="db-stat-card">
          <div class="stat-icon-wrapper">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
          </div>
          <div class="stat-info">
            <span class="stat-value">24/7</span>
            <span class="stat-title">Disponibilidad Online</span>
          </div>
        </div>
      </div>

      <!-- Vertical Green Divider -->
      <div class="db-vertical-divider"></div>

      <!-- Right Area: Database Cards Grid -->
      <div class="db-right-content">
        <div class="db-grid">
          <div
            v-for="db in filteredDatabases"
            :key="db.id"
            class="db-card"
            :style="{ background: db.bgGradient }"
          >
            <!-- Image Layer (Horizontal Logo fully visible by default) -->
            <div class="db-card-image-layer" :style="{ background: db.logoBg || '#ffffff' }">
              <img :src="getImageUrl(db.image)" :alt="db.name" class="db-card-logo-img" />
            </div>

            <!-- Content Layer (Slides up on hover) -->
            <div class="db-card-content-layer">
              <div class="db-card-top">
                <div class="db-icon" :style="{ background: db.color + '18', color: db.color }">
                  {{ db.icon }}
                </div>
                <div class="db-title-block">
                  <h3 class="db-name" :style="{ color: db.color }">{{ db.name }}</h3>
                  <span class="db-provider">{{ db.provider }}</span>
                </div>
              </div>

              <!-- Tags -->
              <div class="db-tags">
                <span
                  v-for="tag in db.tags"
                  :key="tag"
                  class="db-tag"
                  :style="{ background: db.color + '18', color: db.color, borderColor: db.borderColor }"
                >{{ tag }}</span>
              </div>

              <!-- Description -->
              <p class="db-description">{{ db.description }}</p>

              <!-- Features -->
              <ul class="db-features">
                <li v-for="feat in db.features" :key="feat">
                  <span class="db-feat-dot" :style="{ background: db.color }"></span>
                  {{ feat }}
                </li>
              </ul>

              <!-- Button -->
              <a
                :href="db.url"
                target="_blank"
                class="db-btn"
                :style="{ background: db.color, boxShadow: `0 4px 14px ${db.color}30` }"
              >
                Acceder a Plataforma
              </a>
            </div>
          </div>

          <!-- Empty State -->
          <div v-if="filteredDatabases.length === 0" class="db-empty-state">
            <span class="empty-icon">🔍</span>
            <p class="empty-text">No se encontraron bases de datos que coincidan con "{{ searchQuery }}"</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
:global(.main-layout) {
  max-width: 1440px !important;
  padding-left: 1rem !important;
  padding-right: 1rem !important;
}

.fade-in {
  animation: fadeIn 0.5s ease-in-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.db-header-left {
  margin-bottom: 2rem;
  position: relative;
  text-align: left;
}

.db-header-top-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 1.5rem;
}

.db-search-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
  max-width: 340px;
  margin-bottom: -0.5rem;
  z-index: 10;
}

.db-search-icon {
  position: absolute;
  left: 14px;
  color: #64748b;
  pointer-events: none;
  transition: color 0.2s;
}

.db-search-input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 2.8rem;
  font-size: 0.9rem;
  font-family: 'Outfit', sans-serif;
  color: #1e293b;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  outline: none;
  transition: all 0.2s ease;
}

.db-search-input:focus {
  background-color: #ffffff;
  border-color: #009639;
  box-shadow: 0 0 0 4px rgba(0, 150, 57, 0.1);
}

.db-search-input:focus + .db-search-icon {
  color: #009639;
}

.db-empty-state {
  grid-column: 1 / -1;
  text-align: center;
  padding: 4rem 2rem;
  background: #f8fafc;
  border-radius: 20px;
  border: 2px dashed #e2e8f0;
  margin-top: 1rem;
}

.empty-icon {
  font-size: 2.5rem;
  display: block;
  margin-bottom: 1rem;
}

.empty-text {
  font-size: 1rem;
  color: #64748b;
  margin: 0;
  font-weight: 500;
}

.section-title-left {
  font-size: 2.2rem;
  font-weight: 800;
  color: #1e293b;
  margin: 0;
  font-family: 'Outfit', sans-serif;
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.8rem;
}

.title-highlight-wrap {
  position: relative;
  display: inline-flex;
  align-items: center;
}

.title-green {
  color: #009639;
  font-size: 3.4rem;
  font-weight: 900;
  font-family: 'Outfit', sans-serif;
  letter-spacing: -0.01em;
  text-transform: uppercase;
  line-height: 1;
  display: inline-block;
}

.title-script {
  font-family: 'Herr Von Muellerhoff', cursive;
  font-size: 4.8rem;
  color: #000000;
  position: absolute;
  top: 1.4rem;
  left: 65%;
  transform: rotate(-3deg);
  white-space: nowrap;
  font-weight: 400;
  pointer-events: none;
}

.header-divider-line {
  height: 3px;
  background-color: #881337;
  width: 100%;
  margin-top: 2.8rem;
  margin-bottom: 1rem;
}

.section-subtitle-left {
  font-size: 1rem;
  color: #64748b;
  margin: 0;
  line-height: 1.5;
}

/* Split Layout */
.db-split-layout {
  display: flex;
  gap: 2rem;
  align-items: flex-start;
  margin-top: 1.5rem;
}

/* Left stats column */
.db-left-stats {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  flex-shrink: 0;
  width: 260px;
}

.db-left-stats .db-stat-card {
  width: 100%;
  min-width: 0;
}

.db-stat-card {
  background: white;
  border: 1px solid var(--border-color, #e2e8f0);
  border-radius: 16px;
  padding: 1rem 1.75rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.02);
  transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.db-stat-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.05);
}

.stat-icon-wrapper {
  width: 46px;
  height: 46px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.db-stat-card:nth-child(1) .stat-icon-wrapper {
  background: #eef2ff;
  color: #4f46e5;
}

.db-stat-card:nth-child(2) .stat-icon-wrapper {
  background: #fdf2f8;
  color: #db2777;
}

.db-stat-card:nth-child(3) .stat-icon-wrapper {
  background: #ecfdf5;
  color: #059669;
}

.stat-info {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 800;
  color: var(--navy-dark, #1e293b);
  line-height: 1.2;
}

.stat-title {
  font-size: 0.78rem;
  color: var(--text-muted, #64748b);
  font-weight: 600;
}

/* Vertical Green Divider */
.db-vertical-divider {
  width: 2px;
  align-self: stretch;
  background-color: #881337;
  flex-shrink: 0;
}

/* Right content grid wrapper */
.db-right-content {
  flex: 1;
}

/* Grid */
.db-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 1.5rem;
}

/* Card */
.db-card {
  position: relative;
  overflow: hidden;
  height: 405px;
  border: 1px solid #e2e8f0;
  border-radius: 32px 80px 0px 80px;
  padding: 0;
  background: #ffffff;
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.05);
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), box-shadow 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.db-card:hover {
  transform: translateY(-6px);
  box-shadow: 0 16px 40px rgba(0, 0, 0, 0.12);
}


/* Image Layer */
.db-card-image-layer {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  box-sizing: border-box;
  transition: transform 0.5s cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 2;
  background: #ffffff;
}
.db-card-logo-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* Content Layer */
.db-card-content-layer {
  position: absolute;
  top: 100%;
  left: 0;
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  padding: 1.25rem;
  box-sizing: border-box;
  transition: transform 0.5s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
  z-index: 3;
  background: inherit;
}


/* Hover Actions */
.db-card:hover .db-card-image-layer {
  transform: translateY(-100%);
}
.db-card:hover .db-card-content-layer {
  transform: translateY(-100%);
}

/* Card top */
.db-card-top {
  display: flex;
  align-items: flex-start;
  gap: 0.8rem;
}
.db-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.4rem;
  flex-shrink: 0;
}
.db-title-block {
  flex: 1;
  min-width: 0;
}
.db-name {
  font-size: 1rem;
  font-weight: 800;
  margin: 0 0 0.15rem 0;
  line-height: 1.25;
}
.db-provider {
  font-size: 0.72rem;
  color: var(--text-muted);
}

/* Tags */
.db-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}
.db-tag {
  font-size: 0.65rem;
  font-weight: 700;
  padding: 0.15rem 0.55rem;
  border-radius: 50px;
  border: 1px solid;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

/* Description */
.db-description {
  font-size: 0.78rem;
  color: var(--text-muted);
  line-height: 1.45;
  margin: 0;
}

/* Features */
.db-features {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.db-features li {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.76rem;
  color: var(--text-body, #444);
  font-weight: 500;
}
.db-feat-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  flex-shrink: 0;
}

/* Button */
.db-btn {
  display: block;
  text-align: center;
  color: white;
  font-weight: 700;
  font-size: 0.82rem;
  padding: 0.7rem 1rem;
  border-radius: 10px;
  text-decoration: none;
  border: none !important;
  cursor: pointer;
  transition: opacity 0.2s, transform 0.15s, box-shadow 0.15s;
  margin-top: auto;
  flex-shrink: 0;
}
.db-btn:hover {
  opacity: 0.95;
  transform: translateY(-2px);
}
.db-btn:active {
  transform: translateY(0);
}

@media (max-width: 700px) {
  .db-grid {
    grid-template-columns: 1fr;
  }
  .db-stats {
    flex-direction: column;
    gap: 0.75rem;
    padding: 1rem 1.5rem;
  }
  .db-stat-divider {
    width: 60px;
    height: 1px;
  }
}
</style>
