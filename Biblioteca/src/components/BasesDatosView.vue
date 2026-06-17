<script setup>
const emit = defineEmits(['show-toast'])

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
    description:
      'Herramienta de apoyo clínico basada en evidencia que brinda acceso a más de 2,000 monografías de enfermedades y condiciones, algoritmos diagnósticos, guías de tratamiento, calculadoras clínicas y más de 200 videos de procedimientos. Diseñada para responder preguntas clínicas críticas en minutos.',
    features: ['2,000+ monografías clínicas', 'Algoritmos diagnósticos', 'Calculadoras médicas', 'Información de fármacos A–Z'],
    url: 'https://www.5minuteconsult.com'
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
    description:
      'Plataforma integral de McGraw-Hill con más de 100 títulos de referencia en español, incluyendo Harrison, Goodman & Gilman, y Diagnóstico Clínico y Tratamiento. Incluye recursos multimedia, herramienta Diagnosaurus® para diagnóstico diferencial, base de datos de fármacos, calculadoras clínicas y casos clínicos interactivos.',
    features: ['100+ libros en español', 'Diagnosaurus® diferencial', 'Videos y animaciones 3D', 'Autoevaluación interactiva'],
    url: 'https://accessmedicina.mhmedical.com'
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
    description:
      'Plataforma de visualización 3D interactiva conocida como el "Google Maps del cuerpo humano". Presenta más de 8,000 estructuras anatómicas seleccionables individualmente, más de 600 condiciones y tratamientos simulados, disponible en 8 idiomas. Ideal para el aprendizaje anatómico y la comunicación médico-paciente.',
    features: ['8,000+ estructuras anatómicas', '600+ condiciones simuladas', 'Modelos 3D interactivos', 'Accesible desde cualquier dispositivo'],
    url: 'https://www.biodigital.com'
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
    description:
      'Herramienta de apoyo a la decisión clínica basada en evidencia del BMJ. Cubre el proceso completo de atención al paciente: evaluación de síntomas, diagnóstico diferencial, tratamiento y seguimiento. Incluye más de 250 calculadoras médicas, videos de procedimientos, miles de guías clínicas y seguimiento automático de créditos CME/CPD.',
    features: ['Diagnóstico diferencial', '250+ calculadoras médicas', 'Algoritmos de tratamiento', 'Hojas educativas para pacientes'],
    url: 'https://bestpractice.bmj.com'
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
    description:
      'Plataforma de búsqueda clínica de Elsevier en español diseñada para la práctica diaria del médico. Brinda acceso a libros líderes en medicina, journals con revisión por pares, fichas de medicamentos y pautas de práctica clínica. Actualizada continuamente con contenido de más de 1,000 revistas biomédicas de Elsevier.',
    features: ['1,000+ revistas biomédicas', 'Libros clínicos en español', 'Fichas de medicamentos', 'Imágenes y videos clínicos'],
    url: 'https://www.clinicalkey.es'
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
    description:
      'Plataforma educativa de Elsevier para estudiantes de medicina y ciencias de la salud. Incluye los mejores libros de texto como Gray\'s Anatomy for Students, Costanzo Physiology y más. Ofrece tarjetas de estudio (flashcards), miles de preguntas de autoevaluación, análisis de desempeño docente y acceso offline.',
    features: ['Flashcards personalizables', '4,700+ preguntas de examen', 'Notas y marcadores colaborativos', 'Acceso offline móvil'],
    url: 'https://www.clinicalkey.com/student'
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
    description:
      'Solución integral de apoyo a la decisión clínica de EBSCO que combina el contenido basado en evidencia de DynaMed con la información farmacológica avanzada de Micromedex. Ofrece búsqueda con IA (Dyna AI), interacciones medicamentosas, compatibilidad IV, calculadoras clínicas, árboles de decisión y actualización diaria de contenido.',
    features: ['Contenido actualizado diariamente', 'Interacciones de medicamentos', 'Integración con EHR (FHIR)', 'Créditos CME/MOC'],
    url: 'https://www.dynamedex.com'
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
    description:
      'Plataforma web de libros electrónicos de Wolters Kluwer que reúne miles de textos médicos autorizados en un entorno interligado. Permite búsqueda en lenguaje natural, descarga de capítulos en PDF, anotaciones personales y acceso a colecciones multidisciplinarias de medicina, enfermería, farmacología y salud pública.',
    features: ['Miles de libros de texto médicos', 'Descarga de capítulos en PDF', 'Anotaciones y marcadores', 'Colecciones especializadas'],
    url: 'https://ovidsp.ovid.com'
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
    description:
      'Colección digital de LWW (Lippincott Williams & Wilkins) diseñada para apoyar al estudiante durante las 6 rotaciones clínicas principales: Medicina Interna, Cirugía, Pediatría, Gineco-Obstetricia, Psiquiatría y Medicina Familiar. Incluye más de 30 libros de las series Blueprints y Step-Up, 150+ casos clínicos y 4,700+ preguntas de autoevaluación.',
    features: ['6 rotaciones clínicas cubiertas', '30+ libros Blueprints/Step-Up', '150+ casos clínicos', '4,700+ preguntas MCQ'],
    url: 'https://lwwhealthlibrary.com'
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
    description:
      'La revista médica de mayor impacto y prestigio en el mundo, publicada semanalmente desde 1812. Con un factor de impacto de 78.5 (2024), publica investigaciones originales, revisiones clínicas, casos y comentarios editoriales que definen las guías de práctica médica global. Tasa de aceptación aproximada del 5%.',
    features: ['Factor de impacto: 78.5', 'Publicación semanal desde 1812', 'Investigación práctica-cambiante', 'Videos y casos interactivos NEJM'],
    url: 'https://www.nejm.org'
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
    description:
      'Base de datos bibliográfica agregada de Wolters Kluwer que reúne cientos de revistas de más de 50 editoriales y sociedades científicas, incluyendo el catálogo completo de Lippincott® (LWW). Permite búsqueda simultánea en libros, journals y bases de datos; vinculación bidireccional con MEDLINE y descarga en PDF o formato completo Ovid.',
    features: ['Cientos de revistas científicas', 'Acceso texto completo LWW', 'Vinculación con MEDLINE', 'Gestión de citas integrada'],
    url: 'https://ovidsp.ovid.com'
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
    description:
      'Plataforma de acceso de Springer Nature con más de 10 millones de documentos científicos: journals revisados por pares, libros electrónicos, series de libros, obras de referencia, protocolos y actas de congresos. Cubre todas las especialidades médicas y áreas de investigación biomédica, con búsqueda avanzada y contenido semánticamente interligado.',
    features: ['10+ millones de documentos', 'Todas las especialidades médicas', 'E-books y journals integrados', 'Búsqueda avanzada por disciplina'],
    url: 'https://link.springer.com'
  }
]
</script>

<template>
  <div class="fade-in">
    <!-- Header -->
    <div class="db-header">
      <h2 class="section-title">Bases de Datos Científicas y Médicas</h2>
      <p class="section-subtitle">
        Acceso institucional a las principales plataformas de investigación clínica y recursos biomédicos
        suscritos por la Universidad Ricardo Palma.
      </p>
      <div class="db-stats">
        <div class="db-stat">
          <span class="db-stat-num">12</span>
          <span class="db-stat-label">Bases de Datos</span>
        </div>
        <div class="db-stat-divider"></div>
        <div class="db-stat">
          <span class="db-stat-num">10M+</span>
          <span class="db-stat-label">Documentos</span>
        </div>
        <div class="db-stat-divider"></div>
        <div class="db-stat">
          <span class="db-stat-num">24/7</span>
          <span class="db-stat-label">Disponibilidad</span>
        </div>
      </div>
    </div>

    <!-- Grid de tarjetas -->
    <div class="db-grid">
      <div
        v-for="db in databases"
        :key="db.id"
        class="db-card"
        :style="{ background: db.bgGradient, borderColor: db.borderColor }"
      >
        <!-- Card header con ícono y nombre -->
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

        <!-- Descripción -->
        <p class="db-description">{{ db.description }}</p>

        <!-- Features -->
        <ul class="db-features">
          <li v-for="feat in db.features" :key="feat">
            <span class="db-feat-dot" :style="{ background: db.color }"></span>
            {{ feat }}
          </li>
        </ul>

        <!-- Botón -->
        <a
          :href="db.url"
          target="_blank"
          rel="noopener noreferrer"
          class="db-btn"
          :style="{ background: db.color, boxShadow: '0 4px 12px ' + db.color + '44' }"
          @click="emit('show-toast', 'Abriendo ' + db.name + '...', 'success')"
        >
          Acceder a {{ db.name }} ↗
        </a>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Header */
.db-header {
  margin-bottom: 2.5rem;
  text-align: center;
}
.db-stats {
  display: inline-flex;
  align-items: center;
  gap: 1.5rem;
  background: white;
  border: 1px solid var(--border-color);
  border-radius: 50px;
  padding: 0.75rem 2rem;
  margin-top: 1.5rem;
  box-shadow: var(--shadow-sm);
}
.db-stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.1rem;
}
.db-stat-num {
  font-size: 1.3rem;
  font-weight: 800;
  color: var(--navy-dark);
}
.db-stat-label {
  font-size: 0.7rem;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
.db-stat-divider {
  width: 1px;
  height: 36px;
  background: var(--border-color);
}

/* Grid */
.db-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
  gap: 1.5rem;
}

/* Card */
.db-card {
  border: 1.5px solid;
  border-radius: 18px;
  padding: 1.75rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  transition: transform 0.2s, box-shadow 0.2s;
}
.db-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 32px rgba(0,0,0,0.1);
}

/* Card top */
.db-card-top {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
}
.db-icon {
  width: 52px;
  height: 52px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.6rem;
  flex-shrink: 0;
}
.db-title-block {
  flex: 1;
  min-width: 0;
}
.db-name {
  font-size: 1.05rem;
  font-weight: 800;
  margin: 0 0 0.2rem 0;
  line-height: 1.3;
}
.db-provider {
  font-size: 0.75rem;
  color: var(--text-muted);
}

/* Tags */
.db-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}
.db-tag {
  font-size: 0.68rem;
  font-weight: 700;
  padding: 0.2rem 0.65rem;
  border-radius: 50px;
  border: 1px solid;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

/* Description */
.db-description {
  font-size: 0.85rem;
  color: var(--text-muted);
  line-height: 1.65;
  flex: 1;
  margin: 0;
}

/* Features */
.db-features {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.db-features li {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  font-size: 0.8rem;
  color: var(--text-body, #444);
  font-weight: 500;
}
.db-feat-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}

/* Button */
.db-btn {
  display: block;
  text-align: center;
  color: white;
  font-weight: 700;
  font-size: 0.85rem;
  padding: 0.75rem 1rem;
  border-radius: 10px;
  text-decoration: none;
  border: none;
  cursor: pointer;
  transition: opacity 0.2s, transform 0.15s;
  margin-top: auto;
}
.db-btn:hover {
  opacity: 0.9;
  transform: translateY(-1px);
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
