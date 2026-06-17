<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const searchQuery = ref('')
const selectedBook = ref(null)
const libros = ref([])

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
}

// 7 Estantes: El 1 es de una cara (A), 2-7 de dos caras (A y B)
const estantes = Array.from({ length: 7 }, (_, i) => ({
  id: i + 1,
  caras: i === 0 ? ['A'] : ['A', 'B'],
  pisos: 6
}))

const isBookHere = (estante, cara, piso) => {
  if (!selectedBook.value) return false
  return selectedBook.value.estante === estante && 
         selectedBook.value.cara === cara && 
         selectedBook.value.piso === piso
}
</script>

<template>
  <div class="library-map-container">
    <div class="header">
      <h2>Mapa de la Biblioteca</h2>
      <p>Busca un libro para ubicarlo físicamente en los estantes.</p>
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
        <p><strong>Registro:</strong> {{ selectedBook.nroRegistro }}</p>
      </div>
      <div class="location-badge">
        <div class="badge-title">UBICACIÓN</div>
        <div class="badge-value">Estante {{ selectedBook.estante }} • Cara {{ selectedBook.cara }} • Piso {{ selectedBook.piso }}</div>
      </div>
      <button class="clear-btn" @click="selectedBook = null">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
      </button>
    </div>

    <!-- Mapa visual -->
    <div class="map-visualizer">
      <div class="library-room">
        <div class="entrance">Entrada</div>
        <div class="shelves-container">
          <div v-for="estante in estantes" :key="estante.id" class="shelf-wrapper" :class="{ 'is-active-shelf': selectedBook?.estante === estante.id }">
            <div class="shelf-title">ESTANTE {{ estante.id }}</div>
            <div class="faces-container">
              <div v-for="cara in estante.caras" :key="cara" class="shelf-face" :class="{ 'is-active-face': selectedBook?.estante === estante.id && selectedBook?.cara === cara }">
                <div class="face-title">Cara {{ cara }}</div>
                <div class="floors">
                  <div 
                    v-for="piso in estante.pisos" 
                    :key="piso" 
                    class="floor"
                    :class="{ 'has-book': isBookHere(estante.id, cara, 7 - piso) }"
                  >
                    Piso {{ 7 - piso }}
                    <div v-if="isBookHere(estante.id, cara, 7 - piso)" class="book-indicator">
                      <div class="pulse-ring"></div>
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="white" stroke="currentColor" stroke-width="2"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path></svg>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
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
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
}

.search-input {
  width: 100%;
  padding: 1rem 1rem 1rem 3rem;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 1.1rem;
  transition: all 0.2s;
  background: #f8fafc;
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
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  z-index: 10;
  max-height: 300px;
  overflow-y: auto;
}

.result-item {
  padding: 1rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
  border-bottom: 1px solid #f1f5f9;
  transition: background 0.2s;
}

.result-item:hover {
  background: #f8fafc;
}

.book-title {
  display: block;
  font-weight: 600;
  color: #0f172a;
}

.book-author {
  font-size: 0.85rem;
  color: #64748b;
}

.book-location {
  background: #e0e7ff;
  color: #4f46e5;
  padding: 0.25rem 0.75rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 700;
}

/* Tarjeta Seleccionada */
.selected-book-card {
  background: linear-gradient(135deg, #fdf2f8 0%, #fff1f2 100%);
  border: 1px solid #fecdd3;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  position: relative;
  box-shadow: 0 4px 6px -1px rgba(159, 18, 57, 0.05);
}

.selected-info h3 {
  color: #9f1239;
  margin-bottom: 0.5rem;
  font-size: 1.4rem;
}

.selected-info p {
  color: #4c1d95;
  margin: 0.25rem 0;
}

.location-badge {
  background: white;
  padding: 1rem 1.5rem;
  border-radius: 12px;
  box-shadow: 0 4px 6px rgba(0,0,0,0.05);
  text-align: center;
}

.badge-title {
  font-size: 0.75rem;
  font-weight: 800;
  color: #94a3b8;
  letter-spacing: 0.1em;
  margin-bottom: 0.25rem;
}

.badge-value {
  font-size: 1.25rem;
  font-weight: 700;
  color: #0f172a;
}

.clear-btn {
  position: absolute;
  top: 1rem;
  right: 1rem;
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 50%;
}

.clear-btn:hover {
  background: rgba(0,0,0,0.05);
  color: #0f172a;
}

/* Mapa Visual */
.map-visualizer {
  background: #f8fafc;
  padding: 2rem;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  overflow-x: auto;
}

.library-room {
  min-width: 900px;
  border: 4px solid #cbd5e1;
  padding: 2rem;
  border-radius: 8px;
  position: relative;
}

.entrance {
  position: absolute;
  bottom: -4px;
  left: 50px;
  background: #f8fafc;
  padding: 0 20px;
  color: #94a3b8;
  font-weight: 700;
  letter-spacing: 0.1em;
}

.shelves-container {
  display: flex;
  justify-content: space-between;
  gap: 2rem;
}

.shelf-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  opacity: 0.6;
  transition: all 0.3s;
}

.shelf-wrapper.is-active-shelf {
  opacity: 1;
  transform: scale(1.02);
}

.shelf-title {
  text-align: center;
  font-weight: 800;
  color: #475569;
  font-size: 0.9rem;
}

.faces-container {
  display: flex;
  background: #cbd5e1;
  padding: 4px;
  border-radius: 8px;
  gap: 4px;
}

.shelf-face {
  flex: 1;
  background: white;
  border-radius: 4px;
  padding: 0.5rem;
  transition: all 0.3s;
}

.shelf-face.is-active-face {
  box-shadow: 0 0 0 3px #9f1239;
}

.face-title {
  text-align: center;
  font-size: 0.75rem;
  font-weight: 700;
  color: #94a3b8;
  margin-bottom: 0.5rem;
}

.floors {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.floor {
  background: #f1f5f9;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
  color: #64748b;
  position: relative;
  transition: all 0.3s;
}

.floor.has-book {
  background: #9f1239;
  color: white;
  font-weight: bold;
}

.book-indicator {
  position: absolute;
  right: -10px;
  top: 50%;
  transform: translateY(-50%);
  color: #f43f5e;
  z-index: 10;
}

.pulse-ring {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: rgba(244, 63, 94, 0.4);
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0% { transform: translate(-50%, -50%) scale(0.5); opacity: 1; }
  100% { transform: translate(-50%, -50%) scale(2); opacity: 0; }
}
</style>
