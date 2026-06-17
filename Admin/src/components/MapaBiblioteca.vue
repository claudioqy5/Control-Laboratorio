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

      <!-- VISTA MAPA (2D Top-Down Blueprint) -->
      <transition name="fade">
        <div v-if="currentView === 'map'" class="topdown-scene">
          <div class="topdown-floor">
            <!-- Etiqueta de la puerta/entrada -->
            <div class="topdown-entrance">PUERTA PRINCIPAL</div>
            
            <div 
              v-for="estante in estantes" 
              :key="estante.id"
              class="topdown-shelf"
              :class="[ 'td-shelf-' + estante.id, { 'is-target': isBookInShelf(estante.id) } ]"
              @click="openShelfDetail(estante.id)"
            >
              <div class="shelf-label">ESTANTE {{ estante.id }}</div>
              
              <!-- Indicador animado si el libro está aquí -->
              <div v-if="isBookInShelf(estante.id)" class="target-indicator">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="#ef4444" stroke="white" stroke-width="2"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3" fill="white"></circle></svg>
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
   VISTA MAPA PLANO 2D (TOP-DOWN)
   ========================================= */
.topdown-scene {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
}

.topdown-floor {
  width: 800px;
  height: 550px;
  background: white;
  border: 3px solid #cbd5e1;
  border-radius: 16px;
  position: relative;
  box-shadow: 0 15px 35px rgba(0,0,0,0.05);
  /* Blueprint Grid */
  background-image: 
    linear-gradient(#f1f5f9 1px, transparent 1px), 
    linear-gradient(90deg, #f1f5f9 1px, transparent 1px);
  background-size: 20px 20px;
}

.topdown-entrance {
  position: absolute;
  bottom: -3px;
  left: 50%;
  transform: translateX(-50%);
  background: white;
  padding: 10px 40px;
  border: 3px solid #cbd5e1;
  border-bottom: none;
  border-radius: 12px 12px 0 0;
  color: #64748b;
  font-weight: 800;
  font-size: 1rem;
  letter-spacing: 2px;
  box-shadow: 0 -4px 10px rgba(0,0,0,0.02);
}

/* Rectángulo del Estante */
.topdown-shelf {
  position: absolute;
  width: 60px;
  height: 200px;
  background: #e2e8f0;
  border: 2px solid #94a3b8;
  border-radius: 8px;
  display: flex;
  justify-content: center;
  align-items: center;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 4px 6px rgba(0,0,0,0.05);
}

.topdown-shelf:hover {
  background: #cbd5e1;
  transform: scale(1.05) translateY(-2px);
  box-shadow: 0 10px 15px rgba(0,0,0,0.1);
  border-color: #64748b;
}

.shelf-label {
  writing-mode: vertical-rl;
  text-orientation: mixed;
  transform: rotate(180deg);
  color: #475569;
  font-weight: 800;
  letter-spacing: 3px;
  font-size: 0.95rem;
}

/* Layout de los estantes en el piso 2D */
.td-shelf-1 { top: 60px; left: 80px; } 
.td-shelf-2 { top: 60px; left: 240px; }
.td-shelf-3 { top: 60px; left: 440px; }
.td-shelf-4 { top: 60px; left: 640px; }
.td-shelf-5 { top: 300px; left: 240px; }
.td-shelf-6 { top: 300px; left: 440px; }
.td-shelf-7 { top: 300px; left: 640px; }

/* Estante Objetivo (Efecto Destacado Carmesí) */
.topdown-shelf.is-target {
  background: #fee2e2;
  border-color: #ef4444;
  box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.2), 0 10px 15px rgba(239, 68, 68, 0.2);
  z-index: 10;
  transform: scale(1.05);
}

.topdown-shelf.is-target .shelf-label {
  color: #9f1239;
}

/* Pin Marcador */
.target-indicator {
  position: absolute;
  top: -20px;
  animation: mapBounce 1s infinite alternate cubic-bezier(0.4, 0, 0.2, 1);
  filter: drop-shadow(0 4px 6px rgba(239, 68, 68, 0.4));
}

@keyframes mapBounce {
  from { transform: translateY(0) scale(1); }
  to { transform: translateY(-15px) scale(1.1); }
}


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

