<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const searchQuery = ref('')
const selectedBook = ref(null)
const libros = ref([])

// 'map' or 'detail'
const currentView = ref('map')
const selectedShelfId = ref(null)

const loadLibros = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/Libros`)
    libros.value = res.data
  } catch (err) {
    console.error("Error loading books", err)
  }
}

onMounted(() => {
  loadLibros()
})

const searchResults = computed(() => {
  if (!searchQuery.value) return []
  const query = searchQuery.value.toLowerCase()
  return libros.value.filter(l => 
    (l.titulo && l.titulo.toLowerCase().includes(query)) || 
    (l.autor && l.autor.toLowerCase().includes(query)) || 
    (l.nroRegistro && l.nroRegistro.toLowerCase().includes(query))
  )
})

const selectBook = (book) => {
  selectedBook.value = book
  searchQuery.value = ''
  currentView.value = 'map' // Siempre muestra primero el mapa para ver la ubicación aérea
}

// 7 Estantes: El 1 es de una cara (A), 2-7 de dos caras (A y B)
const estantes = Array.from({ length: 7 }, (_, i) => ({
  id: i + 1,
  caras: i === 0 ? ['A'] : ['A', 'B'],
  pisos: 6
}))

const isBookInShelf = (estanteId) => {
  return selectedBook.value && selectedBook.value.estante === estanteId
}

const openShelfDetail = (estanteId) => {
  selectedShelfId.value = estanteId
  currentView.value = 'detail'
}

const isBookHere = (cara, piso) => {
  if (!selectedBook.value) return false
  return selectedBook.value.estante === selectedShelfId.value && 
         selectedBook.value.cara === cara && 
         selectedBook.value.piso === piso
}

const clearSelection = () => {
  selectedBook.value = null
  currentView.value = 'map'
}
</script>

<template>
  <div class="library-map-container">
    <div class="header">
      <h2>Mapa Interactivo 3D</h2>
      <p>Vista aérea de los estantes. Busca un libro o haz clic en un estante para inspeccionarlo.</p>
    </div>

    <!-- Buscador -->
    <div class="search-section">
      <div class="search-box">
        <svg class="search-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
        <input 
          v-model="searchQuery" 
          type="text" 
          placeholder="Buscar libro por título, autor o registro..." 
          class="search-input"
        />
      </div>

      <div v-if="searchQuery && searchResults.length > 0" class="search-results">
        <div 
          v-for="book in searchResults" 
          :key="book.libroID" 
          class="result-item"
          @click="selectBook(book)"
        >
          <div class="book-info">
            <span class="book-title">{{ book.titulo }}</span>
            <span class="book-author">{{ book.autor }} | {{ book.nroRegistro }}</span>
          </div>
          <div class="book-location">
            Estante {{ book.estante }} - Cara {{ book.cara }} - Piso {{ book.piso }}
          </div>
        </div>
      </div>
    </div>

    <!-- Información del libro seleccionado -->
    <div v-if="selectedBook" class="selected-book-card">
      <div class="selected-info">
        <h3>{{ selectedBook.titulo }}</h3>
        <p><strong>Autor:</strong> {{ selectedBook.autor }}</p>
      </div>
      <div class="location-badge">
        <div class="badge-title">UBICACIÓN EXACTA</div>
        <div class="badge-value">Estante {{ selectedBook.estante }} • Cara {{ selectedBook.cara }} • Piso {{ selectedBook.piso }}</div>
      </div>
      <button class="clear-btn" @click="clearSelection">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
      </button>
    </div>

    <!-- Escenario Principal -->
    <div class="scene-container">
      <button v-if="currentView === 'detail'" class="back-btn" @click="currentView = 'map'">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
        Volver al Mapa Aéreo
      </button>

      <!-- VISTA MAPA (Isométrica 3D Estilo Mockup) -->
      <transition name="fade">
        <div v-if="currentView === 'map'" class="isometric-scene">
          <div class="wood-floor">
            <!-- Textos en el piso (ajustados a los pasillos) -->
            <div class="floor-label-text" style="top: 420px; left: 100px; transform: translateZ(1px) rotateZ(-90deg);">ZONA MULTIDISCIPLINARIA</div>
            <div class="floor-label-text" style="top: 420px; left: 330px; transform: translateZ(1px) rotateZ(-90deg);">ZONA DE LITERATURA</div>
            <div class="floor-label-text" style="top: 420px; left: 550px; transform: translateZ(1px) rotateZ(-90deg);">ZONA DE CIENCIAS</div>
            
            <div class="entrance-marker">ENTRADA PRINCIPAL</div>
            
            <!-- Renderizar los 7 estantes según la radiografía -->
            <div 
              v-for="estante in estantes" 
              :key="estante.id"
              class="iso-shelf"
              :class="[ 'shelf-pos-' + estante.id, { 'is-target': isBookInShelf(estante.id) } ]"
              @click="openShelfDetail(estante.id)"
            >
              <!-- Cara Superior (Techo) -->
              <div class="iso-face top">
                <span class="top-label">
                  ESTANTE
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 5v14M19 12l-7 7-7-7"/></svg>
                </span>
              </div>
              
              <!-- Caras Delantera y Trasera (cortas) -->
              <div class="iso-face front"></div>
              <div class="iso-face back"></div>
              
              <!-- Caras Laterales (Largas, muestran los libros) -->
              <div class="iso-face left">
                 <div class="fake-book-col left-face"></div>
                 <div class="fake-book-col left-face alt-pattern"></div>
                 <div class="fake-book-col left-face"></div>
                 <div class="fake-book-col left-face alt-pattern"></div>
              </div>
              <div class="iso-face right">
                 <div class="fake-book-col right-face alt-pattern"></div>
                 <div class="fake-book-col right-face"></div>
                 <div class="fake-book-col right-face alt-pattern"></div>
                 <div class="fake-book-col right-face"></div>
              </div>
            </div>
          </div>
        </div>
      </transition>

      <!-- VISTA DETALLE DE ESTANTE (Frontal) -->
      <transition name="fade">
        <div v-if="currentView === 'detail'" class="detail-scene">
          <div class="detail-header">
            <h3>ESTANTE {{ selectedShelfId }}</h3>
            <p>Selecciona una cara o visualiza el piso indicado.</p>
          </div>
          <div class="detail-faces">
            <div 
              v-for="cara in estantes.find(e => e.id === selectedShelfId).caras" 
              :key="cara" 
              class="detail-face"
              :class="{ 'target-face': selectedBook?.cara === cara && selectedBook?.estante === selectedShelfId }"
            >
              <div class="face-header">CARA {{ cara }}</div>
              <div class="detail-floors">
                <div 
                  v-for="piso in 6" 
                  :key="piso" 
                  class="detail-floor"
                  :class="{ 'has-target-book': isBookHere(cara, 7 - piso) }"
                >
                  <div class="floor-bookshelf">
                    <!-- Libros de relleno decorativos -->
                    <div class="book-spine" v-for="n in 12" :key="n" :style="{ height: 35 + Math.random() * 25 + 'px', background: `hsl(${Math.random() * 360}, 20%, 70%)` }"></div>
                    
                    <!-- EL LIBRO OBJETIVO -->
                    <div v-if="isBookHere(cara, 7 - piso)" class="target-book-spine" title="¡Aquí está el libro!">
                      <div class="book-glow"></div>
                      <div class="book-label">LIBRO</div>
                    </div>

                    <!-- Más libros de relleno -->
                    <div class="book-spine" v-for="n in 8" :key="n+15" :style="{ height: 35 + Math.random() * 25 + 'px', background: `hsl(${Math.random() * 360}, 20%, 70%)` }"></div>
                  </div>
                  <div class="floor-label">PISO {{ 7 - piso }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </transition>
    </div>
  </div>
</template>

<style scoped>
.library-map-container {
  padding: 2rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.header h2 {
  color: #0f172a;
  font-size: 1.8rem;
  margin-bottom: 0.5rem;
  font-weight: 800;
}

.header p {
  color: #64748b;
  font-size: 1rem;
}

/* Buscador */
.search-section {
  position: relative;
  max-width: 600px;
  margin: 0 auto;
  width: 100%;
}

.search-box {
  position: relative;
}

.search-icon {
  position: absolute;
  left: 1.2rem;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
}

.search-input {
  width: 100%;
  padding: 1rem 1rem 1rem 3.5rem;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1.1rem;
  transition: all 0.2s;
  background: #f8fafc;
  color: #0f172a;
}

.search-input:focus {
  outline: none;
  border-color: #9f1239;
  background: white;
  box-shadow: 0 0 0 4px rgba(159, 18, 57, 0.1);
}

.search-results {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  margin-top: 0.5rem;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1);
  z-index: 50;
  max-height: 300px;
  overflow-y: auto;
}

.result-item {
  padding: 1rem 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
  border-bottom: 1px solid #f1f5f9;
  transition: background 0.2s;
}

.result-item:hover {
  background: #fdf2f8;
}

.book-title {
  display: block;
  font-weight: 700;
  color: #0f172a;
  font-size: 1.05rem;
  margin-bottom: 4px;
}

.book-author {
  font-size: 0.85rem;
  color: #64748b;
}

.book-location {
  background: #ffe4e6;
  color: #9f1239;
  padding: 0.4rem 0.8rem;
  border-radius: 999px;
  font-size: 0.85rem;
  font-weight: 700;
  white-space: nowrap;
}

/* Tarjeta Seleccionada */
.selected-book-card {
  background: linear-gradient(135deg, #fdf2f8 0%, #fff1f2 100%);
  border: 1px solid #fecdd3;
  border-radius: 12px;
  padding: 1.5rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  position: relative;
  box-shadow: 0 4px 15px -3px rgba(159, 18, 57, 0.1);
}

.selected-info h3 {
  color: #9f1239;
  margin-bottom: 0.5rem;
  font-size: 1.5rem;
  font-weight: 800;
}

.selected-info p {
  color: #475569;
  margin: 0.25rem 0;
  font-size: 0.95rem;
}

.location-badge {
  background: white;
  padding: 1rem 1.5rem;
  border-radius: 12px;
  box-shadow: 0 4px 6px rgba(0,0,0,0.05);
  text-align: center;
  border: 2px solid #fecdd3;
}

.badge-title {
  font-size: 0.75rem;
  font-weight: 800;
  color: #9f1239;
  letter-spacing: 0.1em;
  margin-bottom: 0.25rem;
}

.badge-value {
  font-size: 1.25rem;
  font-weight: 800;
  color: #0f172a;
}

.clear-btn {
  position: absolute;
  top: 1rem;
  right: 1rem;
  background: white;
  border: 1px solid #e2e8f0;
  color: #64748b;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 50%;
  transition: all 0.2s;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.clear-btn:hover {
  background: #f1f5f9;
  color: #0f172a;
  transform: scale(1.1);
}

/* =========================================
   ESCENARIO (CONTENEDOR PRINCIPAL)
   ========================================= */
.scene-container {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  position: relative;
  overflow: hidden;
  height: 650px;
  display: flex;
  justify-content: center;
  align-items: center;
  box-shadow: inset 0 0 20px rgba(0,0,0,0.02);
}

.back-btn {
  position: absolute;
  top: 1.5rem;
  left: 1.5rem;
  z-index: 20;
  background: white;
  border: 1px solid #cbd5e1;
  padding: 0.75rem 1.2rem;
  border-radius: 12px;
  font-weight: 700;
  color: #334155;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 6px rgba(0,0,0,0.05);
  transition: all 0.2s;
}

.back-btn:hover {
  background: #f1f5f9;
  transform: translateY(-2px);
  box-shadow: 0 6px 12px rgba(0,0,0,0.1);
}

/* =========================================
   VISTA MAPA (ISOMÉTRICA 3D - ESTILO MOCKUP)
   ========================================= */
.isometric-scene {
  width: 100%;
  height: 100%;
  perspective: 2500px;
  display: flex;
  justify-content: center;
  align-items: center;
  background: #ffffff; /* Fondo blanco limpio */
}

.wood-floor {
  width: 850px;
  height: 600px;
  background-color: #d1bfae;
  background-image: 
    repeating-linear-gradient(45deg, #c3b09e 25%, transparent 25%, transparent 75%, #c3b09e 75%, #c3b09e), 
    repeating-linear-gradient(45deg, #c3b09e 25%, #d1bfae 25%, #d1bfae 75%, #c3b09e 75%, #c3b09e);
  background-position: 0 0, 15px 15px;
  background-size: 30px 30px;
  
  transform: rotateX(60deg) rotateZ(-35deg);
  transform-style: preserve-3d;
  position: relative;
  
  /* Grosor del piso negro estilo bloque 3D */
  box-shadow: 
    -2px 2px 0 #111,
    -4px 4px 0 #111,
    -6px 6px 0 #111,
    -8px 8px 15px rgba(0,0,0,0.5);
  border: 1px solid #111;
}

.floor-label-text {
  position: absolute;
  color: #f1f5f9;
  font-weight: 800;
  font-size: 0.8rem;
  letter-spacing: 1px;
  transform: translateZ(1px);
  text-shadow: 1px 1px 2px rgba(0,0,0,0.3);
}

.entrance-marker {
  position: absolute;
  bottom: 0px;
  right: 0px;
  background: #111;
  color: #fff;
  padding: 8px 40px;
  font-weight: 900;
  font-size: 1.1rem;
  letter-spacing: 2px;
  transform: translateZ(1px) translateX(25px) translateY(10px);
  box-shadow: 0 4px 10px rgba(0,0,0,0.5);
  border-radius: 2px;
}

/* Prisma Rectangular del Estante (Vertical) */
.iso-shelf {
  position: absolute;
  width: 40px; /* Ancho X */
  height: 350px; /* Largo Y */
  transform-style: preserve-3d;
  cursor: pointer;
  transition: transform 0.3s;
}

.iso-shelf:hover {
  transform: translateZ(10px);
}

.iso-face {
  position: absolute;
  background: #f4f4f5; /* Very light grey */
  border: 1px solid #d4d4d8;
  backface-visibility: hidden;
}

/* Z Extrusion: 120px */
.iso-face.top {
  width: 100%; height: 100%;
  transform: translateZ(120px);
  background: #fafafa;
  display: flex;
  justify-content: center;
  align-items: center;
  color: #a1a1aa;
  font-weight: 800;
  font-size: 0.9rem;
  letter-spacing: 2px;
}
.top-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.iso-face.front { /* Frontal = Ancho (X=40), Extrusión Z=120 */
  bottom: 0; left: 0;
  width: 100%; height: 120px;
  transform-origin: bottom;
  transform: rotateX(-90deg);
  background: #e4e4e7;
}

.iso-face.back {
  top: 0; left: 0;
  width: 100%; height: 120px;
  transform-origin: top;
  transform: rotateX(90deg);
  background: #e4e4e7;
}

.iso-face.left { /* Largo (Y), Extrusión Z=120 */
  top: 0; left: 0;
  width: 120px; height: 100%;
  transform-origin: left;
  transform: rotateY(-90deg);
  background: #27272a; /* Dark interior */
  display: flex;
  justify-content: space-evenly;
  align-items: center;
  padding: 5px 0;
  box-sizing: border-box;
}

.iso-face.right { /* Largo (Y), Extrusión Z=120 */
  top: 0; right: 0;
  width: 120px; height: 100%;
  transform-origin: right;
  transform: rotateY(90deg);
  background: #27272a; /* Dark interior */
  display: flex;
  flex-direction: row-reverse;
  justify-content: space-evenly;
  align-items: center;
  padding: 5px 0;
  box-sizing: border-box;
}

/* Book texturing */
.fake-book-col {
  width: 25px; /* Altura de repisa en 3D (eje Z) */
  height: 96%; /* Largo de libros en el estante (eje Y) */
  background-image: repeating-linear-gradient(
    to bottom,
    #991b1b 0px, #991b1b 12px,
    #1e40af 12px, #1e40af 24px,
    #065f46 24px, #065f46 32px,
    #92400e 32px, #92400e 48px,
    #374151 48px, #374151 58px,
    #b91c1c 58px, #b91c1c 66px,
    #4338ca 66px, #4338ca 84px
  );
  background-size: 100% 100px;
  box-shadow: inset 4px 0 8px rgba(0,0,0,0.5);
  position: relative;
}
.fake-book-col.alt-pattern {
  background-image: repeating-linear-gradient(
    to bottom,
    #4338ca 0px, #4338ca 16px,
    #991b1b 16px, #991b1b 24px,
    #065f46 24px, #065f46 40px,
    #374151 40px, #374151 48px,
    #1e40af 48px, #1e40af 60px,
    #92400e 60px, #92400e 70px,
    #b91c1c 70px, #b91c1c 84px
  );
}
.fake-book-col.left-face { border-left: 4px solid #a1a1aa; }
.fake-book-col.right-face { border-right: 4px solid #a1a1aa; }

/* TARGET SHELF (Resaltado Rojo Brillante) */
.iso-shelf.is-target .iso-face {
  background: rgba(239, 68, 68, 0.8) !important;
  border-color: #f87171 !important;
  box-shadow: 0 0 20px rgba(239, 68, 68, 0.4);
}
.iso-shelf.is-target .iso-face.top {
  background: #ef4444 !important;
  color: white !important;
  text-shadow: 0 0 5px rgba(255,255,255,0.8);
}
.iso-shelf.is-target .iso-face.left,
.iso-shelf.is-target .iso-face.right {
  background: #7f1d1d !important; /* Oscuro adentro para contraste */
}
.iso-shelf.is-target .fake-book-col {
  /* Overlay rojizo semitransparente sobre los libros */
  box-shadow: inset 0 0 0 100px rgba(239, 68, 68, 0.5), inset 4px 0 8px rgba(0,0,0,0.6) !important;
}
.iso-shelf.is-target .fake-book-col.left-face { border-left-color: #fca5a5 !important; }
.iso-shelf.is-target .fake-book-col.right-face { border-right-color: #fca5a5 !important; }

/* Layout de los 7 estantes en fila */
.shelf-pos-1 { top: 220px; left: 60px; height: 180px; } 
.shelf-pos-2 { top: 50px; left: 170px; }
.shelf-pos-3 { top: 50px; left: 280px; }
.shelf-pos-4 { top: 50px; left: 390px; }
.shelf-pos-5 { top: 50px; left: 500px; }
.shelf-pos-6 { top: 50px; left: 610px; }
.shelf-pos-7 { top: 50px; left: 720px; }


/* =========================================
   VISTA DETALLE (FRONTAL)
   ========================================= */
.detail-scene {
  position: absolute;
  width: 100%;
  height: 100%;
  background: white;
  display: flex;
  flex-direction: column;
  padding: 2rem;
  padding-top: 5rem;
}

.detail-header {
  text-align: center;
  margin-bottom: 2rem;
}
.detail-header h3 {
  font-size: 2rem;
  color: #0f172a;
  font-weight: 900;
}
.detail-header p {
  color: #64748b;
}

.detail-faces {
  display: flex;
  gap: 4rem;
  justify-content: center;
  height: 100%;
}

.detail-face {
  background: #f8fafc;
  border: 12px solid #cbd5e1; /* Marco del estante */
  border-radius: 8px;
  padding: 0 1rem;
  width: 350px;
  box-shadow: 0 20px 40px rgba(0,0,0,0.1), inset 0 0 30px rgba(0,0,0,0.05);
  display: flex;
  flex-direction: column;
  transition: all 0.3s;
}

.target-face {
  border-color: #fca5a5;
  box-shadow: 0 20px 50px rgba(225, 29, 72, 0.2), inset 0 0 30px rgba(254, 205, 211, 0.5);
}

.face-header {
  text-align: center;
  background: #94a3b8;
  color: white;
  padding: 0.5rem;
  font-weight: 800;
  letter-spacing: 0.1em;
  border-radius: 0 0 8px 8px;
  margin-bottom: 1rem;
}

.target-face .face-header {
  background: #e11d48;
}

.detail-floors {
  display: flex;
  flex-direction: column;
  flex: 1;
}

.detail-floor {
  flex: 1;
  border-bottom: 10px solid #cbd5e1; /* Madera del estante */
  position: relative;
  display: flex;
  align-items: flex-end;
  transition: background 0.3s;
}

.detail-floor:last-child {
  border-bottom: none;
}

.has-target-book {
  background: linear-gradient(to top, rgba(254, 205, 211, 0.5) 0%, transparent 100%);
}

.floor-bookshelf {
  display: flex;
  gap: 2px;
  align-items: flex-end;
  height: 100%;
  width: 100%;
  padding-bottom: 2px;
}

.book-spine {
  width: 12px;
  border-radius: 2px 2px 0 0;
  box-shadow: inset -3px 0 5px rgba(0,0,0,0.3);
  border-left: 1px solid rgba(255,255,255,0.2);
}

.target-book-spine {
  width: 30px;
  height: 80%;
  background: linear-gradient(135deg, #e11d48 0%, #9f1239 100%);
  border-radius: 4px 4px 0 0;
  box-shadow: 0 0 20px #f43f5e, inset -2px 0 10px rgba(0,0,0,0.5);
  position: relative;
  z-index: 10;
  display: flex;
  justify-content: center;
  align-items: center;
  animation: targetPulse 1.5s infinite;
  cursor: pointer;
}

.book-label {
  color: white;
  font-size: 0.6rem;
  font-weight: 900;
  transform: rotate(-90deg);
  letter-spacing: 1px;
}

@keyframes targetPulse {
  0%, 100% { transform: scale(1); box-shadow: 0 0 15px #f43f5e; }
  50% { transform: scale(1.05); box-shadow: 0 0 30px #f43f5e, 0 0 10px white; }
}

.floor-label {
  position: absolute;
  left: -40px;
  bottom: 10px;
  font-size: 0.8rem;
  font-weight: 800;
  color: #94a3b8;
}

.has-target-book .floor-label {
  color: #e11d48;
}

/* Transiciones */
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.4s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>

