<script setup>
const emit = defineEmits(['set-view', 'show-toast'])
</script>

<template>
  <footer class="footer">
    <div class="footer-container">
      <div class="footer-brand">
        <h4>Facultad de Medicina URP</h4>
        <p>Comprometidos con la formación médica de excelencia y la investigación científica de vanguardia en el Perú.</p>
      </div>
      
      <div class="footer-section">
        <h5>Recursos</h5>
        <ul class="footer-links">
          <li><a href="#" @click.prevent="emit('set-view', 'catalogo')">Catálogo</a></li>
          <li><a href="#" @click.prevent="emit('set-view', 'bases-datos')">Bases de Datos</a></li>
          <li><a href="#" @click.prevent="emit('show-toast', 'Sección de revistas científicas en construcción.', 'info')">Revistas</a></li>
          <li><a href="#" @click.prevent="emit('show-toast', 'Sección de tesis médicas en construcción.', 'info')">Tesis</a></li>
        </ul>
      </div>

      <div class="footer-section">
        <h5>Ayuda</h5>
        <ul class="footer-links">
          <li><a href="#" @click.prevent="emit('show-toast', 'Horarios de atención e instalaciones.', 'info')">Horarios</a></li>
          <li><a href="#" @click.prevent="emit('show-toast', 'Formulario de contacto de biblioteca.', 'info')">Contacto</a></li>
          <li><a href="#" @click.prevent="emit('show-toast', 'Preguntas frecuentes sobre préstamos y multas.', 'info')">FAQ</a></li>
        </ul>
      </div>

      <div class="footer-section">
        <h5>Legal</h5>
        <ul class="footer-links">
          <li><a href="#" @click.prevent="emit('show-toast', 'Términos legales y condiciones de uso.', 'info')">Términos Legales</a></li>
          <li><a href="#" @click.prevent="emit('show-toast', 'Políticas de privacidad de la Universidad.', 'info')">Privacidad</a></li>
        </ul>
      </div>
    </div>

    <div class="footer-bottom">
      <p>© 2026 Universidad Ricardo Palma - Facultad de Medicina. Todos los derechos reservados.</p>
      <div class="footer-socials">
        <a href="#" class="social-icon" title="Facebook">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"></path></svg>
        </a>
        <a href="#" class="social-icon" title="Twitter">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M23 3a10.9 10.9 0 0 1-3.14 1.53 4.48 4.48 0 0 0-7.86 3v1A10.66 10.66 0 0 1 3 4s-4 9 5 13a11.64 11.64 0 0 1-7 2c9 5 20 0 20-11.5a4.5 4.5 0 0 0-.08-.83A7.72 7.72 0 0 0 23 3z"></path></svg>
        </a>
      </div>
    </div>
  </footer>
</template>
