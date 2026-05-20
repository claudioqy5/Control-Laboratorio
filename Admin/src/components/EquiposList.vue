<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const equipos = ref([])
const searchQuery = ref('')
const showModal = ref(false)
const currentEquipo = ref({
  equipoID: 0,
  nombreRed: '',
  ubicacion: '',
  estado: true,
  posicionMapa: null
})

const fetchEquipos = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/stats/map`)
    equipos.value = res.data
  } catch (error) {
    console.error("Error cargando equipos:", error)
  }
}

const filteredEquipos = computed(() => {
  if (!searchQuery.value) return equipos.value
  const query = searchQuery.value.toLowerCase()
  return equipos.value.filter(e => 
    (e.nombreRed && e.nombreRed.toLowerCase().includes(query)) ||
    (e.ubicacion && e.ubicacion.toLowerCase().includes(query))
  )
})

const toggleEstado = async (equipo) => {
  if (confirm(`¿${equipo.estado ? 'Desactivar' : 'Activar'} el equipo ${equipo.nombreRed}?`)) {
    const updatedEquipo = { ...equipo, estado: !equipo.estado }
    try {
      await axios.put(`${API_BASE_URL}/api/equipos/${equipo.equipoID}`, updatedEquipo)
      fetchEquipos()
    } catch (error) {
      console.error("Error al cambiar estado:", error)
    }
  }
}

const editEquipo = (equipo) => {
  currentEquipo.value = { ...equipo }
  showModal.value = true
}

const deleteEquipo = async (id) => {
  if (confirm("¿Estás seguro de eliminar este equipo? Esta acción no se puede deshacer.")) {
    try {
      await axios.delete(`${API_BASE_URL}/api/equipos/${id}`)
      fetchEquipos()
    } catch (error) {
      console.error("Error al eliminar equipo:", error)
      alert("Error al eliminar el equipo. Asegúrate de que no tenga sesiones asociadas.")
    }
  }
}

const saveEquipo = async () => {
  if (!currentEquipo.value.nombreRed) {
    alert("El nombre de red es obligatorio.")
    return
  }
  
  try {
    await axios.put(`${API_BASE_URL}/api/equipos/${currentEquipo.value.equipoID}`, currentEquipo.value)
    showModal.value = false
    fetchEquipos()
  } catch (error) {
    console.error("Error al guardar:", error)
    alert("Ocurrió un error al guardar.")
  }
}

onMounted(fetchEquipos)
</script>

<template>
  <div>
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem;">
      <div>        
        <h2 style="color: #111827; font-size: 2rem; font-weight: 700; margin-bottom: 0.25rem;">Computadoras</h2>
        <div style="color: #6b7280; font-size: 0.875rem;">{{ equipos.length }} Equipos registrados</div>
      </div>
      <div style="display: flex; gap: 1rem; align-items: center;">
        <div style="position: relative;">
          <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: #9ca3af;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
          <input type="text" v-model="searchQuery" placeholder="Buscar por nombre o ubicación..." style="padding: 0.5rem 1rem 0.5rem 2rem; border-radius: 0.5rem; border: 1px solid #e5e7eb; width: 300px; font-size: 0.875rem; color: #111827;">
        </div>
      </div>
    </div>

    <table class="centered-table">
      <thead>
        <tr>
          <th>Nº</th>
          <th>Nombre de Red</th>
          <th>Ubicación</th>
          <th>Estado del Equipo</th>
          <th>Asignación en Mapa</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(e, index) in filteredEquipos" :key="e.equipoID">
          <td style="color: #6b7280; font-weight: 600;">{{ index + 1 }}</td>
          <td>
            <div style="display: flex; align-items: center; justify-content: center; gap: 8px;">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#6366f1" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
              <strong style="color: #111827;">{{ e.nombreRed }}</strong>
            </div>
          </td>
          <td style="color: #475569;">{{ e.ubicacion || '-' }}</td>
          <td>
            <span :style="{ background: e.estado ? '#f0fdf4' : '#fef2f2', color: e.estado ? '#166534' : '#991b1b', padding: '4px 8px', borderRadius: '4px', fontSize: '0.85rem', fontWeight: '600' }">
              {{ e.estado ? 'Operativo' : 'Inactivo / Mantenimiento' }}
            </span>
          </td>
          <td>
            <div v-if="e.posicionMapa !== null" style="display: inline-flex; align-items: center; gap: 6px; background: #e0f2fe; color: #0369a1; padding: 4px 10px; border-radius: 6px; font-size: 0.85rem; font-weight: 600;">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
              Posición {{ e.posicionMapa }}
            </div>
            <span v-else style="color: #94a3b8; font-size: 0.85rem; font-style: italic;">
              Sin asignar
            </span>
          </td>
          <td style="white-space: nowrap;">
            <button class="icon-btn" :class="e.estado ? 'suspend-btn' : 'activate-btn'" @click="toggleEstado(e)" :title="e.estado ? 'Desactivar' : 'Activar'">
              <svg v-if="e.estado" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>
              <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
            </button>
            <button class="icon-btn edit-btn" @click="editEquipo(e)" title="Editar">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </button>
            <button class="icon-btn delete-btn" @click="deleteEquipo(e.equipoID)" title="Eliminar">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
            </button>
          </td>
        </tr>
      </tbody>
    </table>
    
    <!-- Modal de Edición -->
    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal-card">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
            </div>
            <h3>Editar Equipo</h3>
          </div>
          <button class="close-btn" @click="showModal = false">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          </button>
        </div>
        
        <div class="modal-body">
          <div class="input-group">
            <label>Nombre de Red</label>
            <input type="text" v-model="currentEquipo.nombreRed" class="input-field" disabled style="background: #f1f5f9; cursor: not-allowed; opacity: 0.7;">
            <small style="color: #94a3b8; font-size: 0.75rem; display: block; margin-top: 4px;">El nombre de red es asignado automáticamente por el agente y no se puede editar.</small>
          </div>
          <div class="input-group" style="margin-top: 1rem;">
            <label>Ubicación</label>
            <input type="text" v-model="currentEquipo.ubicacion" class="input-field" placeholder="Ej: Laboratorio Central">
          </div>
        </div>
        
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Cancelar</button>
          <button class="btn btn-primary premium-btn" @click="saveEquipo">Guardar Cambios</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.centered-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03);
}

.centered-table th {
  background: #f8fafc;
  padding: 1rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: #475569;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: center;
  border-bottom: 2px solid #e2e8f0;
}

.centered-table td {
  padding: 1rem;
  text-align: center;
  border-bottom: 1px solid #f1f5f9;
  color: #1e293b;
  font-size: 0.9rem;
}

.centered-table tbody tr:hover {
  background: #f8fafc;
}

.icon-btn {
  background: transparent;
  border: none;
  padding: 6px;
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.edit-btn { color: #6366f1; margin-right: 8px; }
.edit-btn:hover { background: #e0e7ff; }
.delete-btn { color: #ef4444; }
.delete-btn:hover { background: #fee2e2; }
.suspend-btn { color: #f59e0b; margin-right: 8px; }
.suspend-btn:hover { background: #fef3c7; }
.activate-btn { color: #10b981; margin-right: 8px; }
.activate-btn:hover { background: #d1fae5; }

/* Modal Styles */
.modal-backdrop {
  position: fixed;
  top: 0; left: 0; width: 100vw; height: 100vh;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(4px);
  display: flex; justify-content: center; align-items: center;
  z-index: 1000;
}
.modal-card {
  width: 100%;
  max-width: 500px;
  background: #ffffff;
  border-radius: 16px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  display: flex; flex-direction: column;
}
.modal-header {
  padding: 1.5rem;
  border-bottom: 1px solid #f1f5f9;
  display: flex; justify-content: space-between; align-items: center;
  background: #f8fafc; border-radius: 16px 16px 0 0;
}
.modal-title-wrapper {
  display: flex; align-items: center; gap: 12px;
}
.modal-icon {
  background: #e0e7ff; color: #4f46e5; padding: 8px; border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
}
.modal-header h3 {
  margin: 0; color: #0f172a; font-size: 1.25rem; font-weight: 700;
}
.close-btn {
  background: transparent; border: none; color: #94a3b8; cursor: pointer;
  padding: 8px; border-radius: 8px; transition: all 0.2s;
}
.close-btn:hover { background: #f1f5f9; color: #0f172a; }
.modal-body { padding: 1.5rem; }
.input-group { display: flex; flex-direction: column; gap: 8px; }
.input-group label {
  font-size: 0.875rem; font-weight: 600; color: #475569;
}
.input-field {
  padding: 0.75rem 1rem; border: 1px solid #cbd5e1; border-radius: 8px;
  font-size: 0.95rem; color: #1e293b; transition: all 0.2s;
}
.input-field:focus {
  outline: none; border-color: #6366f1; box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
}
.modal-footer {
  padding: 1.5rem; border-top: 1px solid #f1f5f9; display: flex;
  justify-content: flex-end; gap: 12px; background: #f8fafc;
  border-radius: 0 0 16px 16px;
}
.btn {
  padding: 0.6rem 1.2rem; border-radius: 8px; font-weight: 600;
  font-size: 0.9rem; border: none; cursor: pointer; transition: all 0.2s;
}
.btn-secondary {
  background: #ffffff; color: #475569; border: 1px solid #cbd5e1;
}
.btn-secondary:hover { background: #f1f5f9; }
.btn-primary {
  background: #6366f1; color: white; display: flex; align-items: center; gap: 8px;
}
.btn-primary:hover { background: #4f46e5; }
</style>
