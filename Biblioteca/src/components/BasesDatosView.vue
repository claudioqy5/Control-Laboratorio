<script setup>
const emit = defineEmits(['show-toast'])
</script>

<template>
  <div class="fade-in">
    <h2 class="section-title">Bases de Datos Científicas y Médicas</h2>
    <p class="section-subtitle">Acceso directo a las redes de investigación clínica y journals médicos suscritos por la Universidad Ricardo Palma.</p>
    
    <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 2rem; margin-bottom: 2.5rem;">
      <!-- Card 1 -->
      <div style="background: white; border: 1px solid var(--border-color); border-radius: 16px; padding: 2rem; display: flex; flex-direction: column; gap: 1rem; box-shadow: var(--shadow-sm);">
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <h3 style="font-size: 1.4rem; font-weight: 850; color: var(--navy-dark);">PubMed / Medline</h3>
          <span class="status-pill solventado" style="font-size: 0.7rem;">Acceso Abierto</span>
        </div>
        <p style="font-size: 0.9rem; color: var(--text-muted); line-height: 1.6; flex: 1;">
          La base de datos de literatura biomédica y médica de referencia de la Biblioteca Nacional de Medicina de los EE.UU. Contiene más de 35 millones de citas y resúmenes de artículos de revistas científicas de todo el mundo.
        </p>
        <button class="btn btn-primary" style="align-self: flex-start;" @click="emit('show-toast', 'Abriendo PubMed / Medline en una nueva pestaña.', 'success')">
          Acceder a PubMed
        </button>
      </div>

      <!-- Card 2 -->
      <div style="background: white; border: 1px solid var(--border-color); border-radius: 16px; padding: 2rem; display: flex; flex-direction: column; gap: 1rem; box-shadow: var(--shadow-sm);">
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <h3 style="font-size: 1.4rem; font-weight: 850; color: var(--navy-dark);">ClinicalKey</h3>
          <span class="status-pill completado" style="font-size: 0.7rem;">Suscripción Activa</span>
        </div>
        <p style="font-size: 0.9rem; color: var(--text-muted); line-height: 1.6; flex: 1;">
          Plataforma de búsqueda y consulta clínica de Elsevier diseñada para responder preguntas de práctica diaria. Brinda acceso a libros líderes en medicina, journals con revisiones por pares, fichas de medicamentos y pautas de práctica clínica.
        </p>
        <button class="btn btn-primary" style="align-self: flex-start;" @click="emit('show-toast', 'Iniciando sesión institucional en ClinicalKey...', 'success')">
          Acceder a ClinicalKey
        </button>
      </div>

      <!-- Card 3 -->
      <div style="background: white; border: 1px solid var(--border-color); border-radius: 16px; padding: 2rem; display: flex; flex-direction: column; gap: 1rem; box-shadow: var(--shadow-sm);">
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <h3 style="font-size: 1.4rem; font-weight: 850; color: var(--navy-dark);">UpToDate</h3>
          <span class="status-pill completado" style="font-size: 0.7rem;">Suscripción Activa</span>
        </div>
        <p style="font-size: 0.9rem; color: var(--text-muted); line-height: 1.6; flex: 1;">
          Recurso educativo líder de apoyo para decisiones clínicas basadas en evidencia. Ayuda a los profesionales de la salud a tomar las decisiones correctas en el punto de atención del paciente.
        </p>
        <button class="btn btn-primary" style="align-self: flex-start;" @click="emit('show-toast', 'Iniciando acceso a UpToDate desde el campus.', 'success')">
          Acceder a UpToDate
        </button>
      </div>

      <!-- Card 4 -->
      <div style="background: white; border: 1px solid var(--border-color); border-radius: 16px; padding: 2rem; display: flex; flex-direction: column; gap: 1rem; box-shadow: var(--shadow-sm);">
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <h3 style="font-size: 1.4rem; font-weight: 850; color: var(--navy-dark);">Scopus</h3>
          <span class="status-pill solventado" style="font-size: 0.7rem;">Acceso URP</span>
        </div>
        <p style="font-size: 0.9rem; color: var(--text-muted); line-height: 1.6; flex: 1;">
          Base de datos bibliográfica de resúmenes y citas de artículos de revistas científicas más grande del mundo. Cubre áreas en ciencia, tecnología, medicina y ciencias sociales, con excelentes herramientas de análisis.
        </p>
        <button class="btn btn-primary" style="align-self: flex-start;" @click="emit('show-toast', 'Redirigiendo a Scopus vía Intranet URP.', 'success')">
          Acceder a Scopus
        </button>
      </div>
    </div>
  </div>
</template>
