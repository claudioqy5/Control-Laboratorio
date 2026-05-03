<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const equipos = ref([])
const selectedPC = ref(null)
const currentTime = ref(new Date().toLocaleTimeString())
const newTimeLimit = ref('')
let intervalId = null
let clockIntervalId = null

const getEquipoAtSlot = (index) => {
  return equipos.value.find(e => e.posicionMapa === index)
}

const unassignedEquipos = computed(() => {
  return equipos.value.filter(e => e.posicionMapa == null)
})

const assignModalOpen = ref(false)
const selectedSlotIndex = ref(-1)

const openAssignModal = (index) => {
  selectedSlotIndex.value = index
  assignModalOpen.value = true
}

const passwordPromptOpen = ref(false)
const passwordInput = ref('')
const pendingAction = ref(null)
const pendingEquipoId = ref(null)

const requestAssignEquipo = (equipoId) => {
  pendingAction.value = 'assign'
  pendingEquipoId.value = equipoId
  passwordInput.value = ''
  passwordPromptOpen.value = true
}

const requestUnassignEquipo = (equipoId) => {
  if (!confirm("¿Seguro que deseas desvincular este equipo de esta posición?")) return
  pendingAction.value = 'unassign'
  pendingEquipoId.value = equipoId
  passwordInput.value = ''
  passwordPromptOpen.value = true
}

const submitPassword = async () => {
  if (!passwordInput.value) {
    alert("Ingresa una contraseña.")
    return
  }

  const payload = {
    equipoId: pendingEquipoId.value,
    posicionMapa: pendingAction.value === 'assign' ? selectedSlotIndex.value : null,
    password: passwordInput.value
  }

  try {
    await axios.post(`${API_BASE_URL}/api/stats/assign-map-slot`, payload)
    
    passwordPromptOpen.value = false
    if (pendingAction.value === 'assign') {
      assignModalOpen.value = false
    } else {
      selectedPC.value = null
    }
    fetchMap()
    
  } catch (error) {
    if (error.response && error.response.status === 401) {
      alert("Contraseña incorrecta. Acción denegada.")
    } else {
      console.error("Error asignando/desvinculando equipo:", error)
      alert("Ocurrió un error al intentar modificar el mapa.")
    }
  }
}

// Mapeo exacto de posiciones según la imagen (11 columnas x 8 filas)
const layoutPositions = [
  [1,1],[1,2],[1,3],[1,4],[1,5],[1,6],[1,7],[1,8],[1,9],[1,10],[1,11], // Fila 1
  [2,1],                                                      [2,11], // Fila 2
  [3,1],                                                      [3,11], // Fila 3
  [4,1],       [4,3],[4,4],[4,5],[4,6],[4,7],[4,8],[4,9],       [4,11], // Fila 4 (Removido [4,10])
  [5,1],       [5,3],[5,4],[5,5],[5,6],[5,7],[5,8],[5,9],       [5,11], // Fila 5 (Removido [5,10])
  [6,11],                                                             // Fila 6
  [8,1],                                                      [8,11]  // Fila 8
]

const fetchMap = async () => {
  try {
    const res = await axios.get(`${API_BASE_URL}/api/stats/map`)
    equipos.value = res.data
  } catch (error) {
    console.error("Error fetching map:", error)
  }
}

onMounted(() => {
  fetchMap()
  intervalId = setInterval(fetchMap, 5000) // Fast refresh for map
  clockIntervalId = setInterval(() => {
    currentTime.value = new Date().toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      second: '2-digit',
      hour12: true
    })
  }, 1000)
})

const formatTime = (isoString) => {
  if (!isoString) return ''
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}

// Convert Date to HH:MM format for the input
const getLocalTimeFormat = (isoString) => {
  if (!isoString) return ''
  const d = new Date(isoString)
  return d.toTimeString().slice(0, 8) // "HH:MM:SS"
}

const openDetails = (pc) => {
  selectedPC.value = pc
  if (pc.sesionActiva && pc.sesionActiva.horaLimite) {
    newTimeLimit.value = getLocalTimeFormat(pc.sesionActiva.horaLimite)
  }
}

const updateSessionLimit = async () => {
  if (!selectedPC.value?.sesionActiva || !newTimeLimit.value) return
  
  try {
    const [hours, minutes, seconds] = newTimeLimit.value.split(':')
    const limitDate = new Date()
    limitDate.setHours(parseInt(hours, 10))
    limitDate.setMinutes(parseInt(minutes, 10))
    limitDate.setSeconds(seconds ? parseInt(seconds, 10) : 0)

    // Formatear como fecha local para evitar conversiones UTC
    const localISO = limitDate.getFullYear() + '-' + 
      String(limitDate.getMonth() + 1).padStart(2, '0') + '-' + 
      String(limitDate.getDate()).padStart(2, '0') + 'T' + 
      String(limitDate.getHours()).padStart(2, '0') + ':' + 
      String(limitDate.getMinutes()).padStart(2, '0') + ':' + 
      String(limitDate.getSeconds()).padStart(2, '0');

    const res = await axios.post(`${API_BASE_URL}/api/auth/set-limit`, {
      sesionId: selectedPC.value.sesionActiva.sesionID,
      nuevaHoraLimite: localISO
    })
    
    selectedPC.value.sesionActiva.horaLimite = res.data.horaLimite
    alert("Hora límite actualizada correctamente.")
    fetchMap()
  } catch (error) {
    console.error("Error setting session limit", error)
    alert("No se pudo actualizar el tiempo.")
  }
}

const forceLogout = async () => {
  if (!selectedPC.value?.sesionActiva) return
  if (!confirm("¿Estás seguro de bloquear esta PC inmediatamente?")) return

  try {
    await axios.post(`${API_BASE_URL}/api/auth/logout`, {
      sesionId: selectedPC.value.sesionActiva.sesionID
    })
    selectedPC.value = null
    fetchMap()
  } catch (error) {
    console.error("Error forcing logout", error)
  }
}

const triggerRemoteUnlock = async () => {
  if (!selectedPC.value) return
  try {
    await axios.post(`${API_BASE_URL}/api/auth/trigger-remote-unlock/${selectedPC.value.nombreRed}`)
    alert("Se envió la orden de desbloqueo de emergencia al equipo.")
    selectedPC.value = null
    fetchMap()
  } catch (error) {
    console.error("Error triggering remote unlock", error)
  }
}

onUnmounted(() => {
  if (intervalId) clearInterval(intervalId)
  if (clockIntervalId) clearInterval(clockIntervalId)
})
</script>

<template>
  <div class="card">
    <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #e2e8f0; padding-bottom: 15px; margin-bottom: 15px;">
      <h2 style="color: rgb(17, 24, 39);">Computadoras de Laboratorio</h2>
      <div style="background: #f0fdfa; padding: 10px 20px; border-radius: 8px; border: 1px solid #99f6e4;">
        <span style="color: #64748b; font-size: 0.85rem; margin-right: 10px;">HORA ACTUAL:</span>
        <span style="color: #0f766e; font-size: 1.2rem; font-weight: bold; font-family: Consolas;">{{ currentTime }}</span>
      </div>
    </div>
    
    <div class="pc-map-container">
      <div class="pc-grid">
        <!-- Renderizamos cada posición del mapa -->
        <div v-for="(pos, index) in layoutPositions" :key="index"
             class="pc-card-wrapper"
             :style="{ gridRow: pos[0], gridColumn: pos[1] }">
          
          <!-- Si existe un equipo para este índice, lo mostramos -->
          <div v-if="getEquipoAtSlot(index)" 
               class="pc-card" :class="{ occupied: getEquipoAtSlot(index).sesionActiva }"
               @click="openDetails(getEquipoAtSlot(index))">
            <div class="monitor">
              <div class="screen">
                <span v-if="getEquipoAtSlot(index).sesionActiva" class="user-icon">👤</span>
                <span v-else class="power-icon">⏻</span>
              </div>
              <div class="stand"></div>
            </div>
            <span class="pc-name">{{ getEquipoAtSlot(index).nombreRed }}</span>
          </div>

          <!-- Si no hay equipo aún en DB para esta posición, mostramos un placeholder -->
          <div v-else class="pc-card empty-slot" @click="openAssignModal(index)">
            <div class="monitor" style="background: #f1f5f9; border: 1px dashed #cbd5e1; box-shadow: none;">
              <div class="screen" style="background: transparent; border: none; font-size: 1.5rem; color: #94a3b8; font-weight: 200;">+</div>
              <div class="stand" style="background: #e2e8f0;"></div>
            </div>
            <span class="pc-name" style="color: #94a3b8;">Asignar</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Detalle de PC seleccionada -->
    <div v-if="selectedPC" class="detail-overlay" @click="selectedPC = null">
      <div class="card detail-card" @click.stop>
        <!-- Header de la Card -->
        <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f3f4f6; padding-bottom: 15px; margin-bottom: 20px;">
          <h3 style="margin: 0; color: rgb(17, 24, 39); font-weight: 700; font-size: 1.25rem; display: flex; align-items: center; gap: 10px;">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>
            {{ selectedPC.nombreRed }}
          </h3>
          <span :style="{ 
            background: selectedPC.sesionActiva ? '#fff1f2' : '#f0fdf4', 
            color: selectedPC.sesionActiva ? '#e11d48' : '#10b981', 
            padding: '4px 12px', 
            borderRadius: '9999px', 
            fontSize: '0.75rem', 
            fontWeight: '800',
            border: `1px solid ${selectedPC.sesionActiva ? '#ffe4e6' : '#dcfce7'}`
          }">
            {{ selectedPC.sesionActiva ? 'OCUPADO' : 'LIBRE' }}
          </span>
        </div>
        
        <!-- Contenido de Sesión Activa -->
        <div v-if="selectedPC.sesionActiva" style="background: #ffffff; border-radius: 12px;">
          <div style="display: grid; grid-template-columns: 1fr; gap: 12px; margin-bottom: 20px;">
            <div style="background: #f9fafb; padding: 12px; border-radius: 10px; border: 1px solid #f3f4f6;">
              <span style="display: block; font-size: 0.7rem; color: #9ca3af; text-transform: uppercase; font-weight: 700; margin-bottom: 4px;">Usuario Actual</span>
              <span style="display: block; color: #111827; font-weight: 600; font-size: 0.9rem;">{{ selectedPC.sesionActiva.alumno }}</span>
            </div>
            
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
              <div style="background: #f9fafb; padding: 12px; border-radius: 10px; border: 1px solid #f3f4f6;">
                <span style="display: block; font-size: 0.7rem; color: #9ca3af; text-transform: uppercase; font-weight: 700; margin-bottom: 4px;">Hora Ingreso</span>
                <span style="display: block; color: #111827; font-weight: 600; font-size: 0.9rem;">{{ formatTime(selectedPC.sesionActiva.horaInicio) }}</span>
              </div>
              <div style="background: #fffcf0; padding: 12px; border-radius: 10px; border: 1px solid #fef3c7;">
                <span style="display: block; font-size: 0.7rem; color: #d97706; text-transform: uppercase; font-weight: 700; margin-bottom: 4px;">Límite</span>
                <span style="display: block; color: #b45309; font-weight: 700; font-size: 0.9rem;">{{ formatTime(selectedPC.sesionActiva.horaLimite) }}</span>
              </div>
            </div>
          </div>
          
          <!-- Panel de Ajustes -->
          <div style="border-top: 2px dashed #f3f4f6; padding-top: 20px;">
            <label style="font-size: 0.8rem; color: #6b7280; font-weight: 600; display: block; margin-bottom: 10px;">Ajustar Hora Límite Exácta</label>
            <div style="display: flex; gap: 8px; margin-bottom: 15px;">
              <input type="time" step="1" v-model="newTimeLimit" 
                style="flex: 1; background: #f9fafb; color: #111827; border: 1px solid #e5e7eb; padding: 10px; border-radius: 8px; font-weight: 600; font-family: inherit;">
              <button class="btn btn-primary" style="padding: 0 20px; border-radius: 8px;" @click="updateSessionLimit">Fijar</button>
            </div>
            
            <button class="btn btn-danger" style="width: 100%; padding: 12px; border-radius: 8px; display: flex; align-items: center; justify-content: center; gap: 8px; box-shadow: 0 4px 6px -1px rgba(225, 29, 72, 0.2);" @click="forceLogout">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>
              BLOQUEAR INMEDIATAMENTE
            </button>
          </div>
        </div>
        
        <!-- Estado Libre -->
        <div v-else>
          <div style="background: #f8fafc; padding: 25px 15px; border-radius: 12px; text-align: center; margin-bottom: 20px; border: 1px solid #f1f5f9;">
            <div style="background: #ffffff; width: 50px; height: 50px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 15px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#94a3b8" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>
            </div>
            <p style="color: #64748b; font-size: 0.9rem; margin: 0; line-height: 1.5;">El equipo se encuentra bloqueado y esperando a un usuario.</p>
          </div>
          
          <button class="btn" style="width: 100%; padding: 12px; background: #ffffff; color: #0d9488; border: 1px solid #0d9488; border-radius: 8px; font-weight: 700; display: flex; align-items: center; justify-content: center; gap: 8px; transition: all 0.2s;" @click="triggerRemoteUnlock">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 9.9-1"></path></svg>
            DESBLOQUEAR (EMERGENCIA)
          </button>
        </div>

        <div style="display: flex; gap: 10px; margin-top: 15px;">
          <button class="btn" style="flex: 1; background: #fef2f2; color: #ef4444; font-size: 0.85rem; font-weight: 600; padding: 10px; border-radius: 8px; border: 1px solid #fee2e2;" @click="requestUnassignEquipo(selectedPC.equipoID)">Desvincular Equipo</button>
          <button class="btn" style="flex: 1; background: transparent; color: #9ca3af; font-size: 0.85rem; font-weight: 600; padding: 10px; border-radius: 8px;" @click="selectedPC = null">Cerrar ventana</button>
        </div>
      </div>
    </div>

    <!-- Modal de Asignación -->
    <div v-if="assignModalOpen" class="detail-overlay" @click="assignModalOpen = false">
      <div class="card detail-card" style="max-width: 450px;" @click.stop>
        <div style="border-bottom: 1px solid #f3f4f6; padding-bottom: 15px; margin-bottom: 20px;">
          <h3 style="margin: 0; color: rgb(17, 24, 39); font-weight: 700; font-size: 1.25rem;">Asignar Computadora Físicamente</h3>
          <p style="color: #6b7280; font-size: 0.85rem; margin-top: 5px;">Selecciona qué PC está ubicada en esta posición del mapa.</p>
        </div>

        <div v-if="unassignedEquipos.length === 0" style="background: #f8fafc; padding: 20px; border-radius: 10px; text-align: center; border: 1px dashed #cbd5e1;">
          <p style="color: #64748b; margin: 0; font-size: 0.9rem;">No hay equipos nuevos pendientes de asignación.</p>
          <p style="color: #94a3b8; font-size: 0.8rem; margin-top: 5px;">Asegúrate de haber instalado e iniciado sesión en la computadora para que aparezca aquí.</p>
        </div>
        
        <div v-else style="display: flex; flex-direction: column; gap: 10px; max-height: 300px; overflow-y: auto;">
          <div v-for="eq in unassignedEquipos" :key="eq.equipoID" 
               @click="requestAssignEquipo(eq.equipoID)"
               style="padding: 12px 15px; border: 1px solid #e2e8f0; border-radius: 8px; cursor: pointer; display: flex; align-items: center; justify-content: space-between; transition: all 0.2s;"
               onmouseover="this.style.borderColor='#0ea5e9'; this.style.background='#f0f9ff';"
               onmouseout="this.style.borderColor='#e2e8f0'; this.style.background='transparent';">
            <div>
              <span style="font-weight: 700; color: #0f172a; display: block;">{{ eq.nombreRed }}</span>
              <span style="font-size: 0.75rem; color: #64748b;">{{ eq.ubicacion }}</span>
            </div>
            <button style="background: #0ea5e9; color: white; border: none; padding: 6px 12px; border-radius: 6px; font-weight: 600; font-size: 0.8rem;">Asignar</button>
          </div>
        </div>

        <button class="btn" style="margin-top: 20px; width: 100%; background: transparent; color: #9ca3af; font-size: 0.85rem; font-weight: 600;" @click="assignModalOpen = false">Cancelar</button>
      </div>
    </div>

    <!-- Modal de Contraseña -->
    <div v-if="passwordPromptOpen" class="detail-overlay" @click.self="passwordPromptOpen = false">
      <div class="card detail-card" style="max-width: 400px; text-align: center;" @click.stop>
        <div style="background: #fef2f2; width: 60px; height: 60px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 15px;">
          <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="#ef4444" stroke-width="2.5"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>
        </div>
        <h3 style="margin: 0 0 10px; color: rgb(17, 24, 39); font-weight: 700; font-size: 1.25rem;">Acción Protegida</h3>
        <p style="color: #64748b; font-size: 0.9rem; margin-bottom: 20px;">Ingresa la contraseña de administrador para confirmar esta modificación en el mapa.</p>
        
        <input type="password" v-model="passwordInput" placeholder="Contraseña..." @keyup.enter="submitPassword"
          style="width: 100%; padding: 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 1rem; margin-bottom: 20px; box-sizing: border-box; text-align: center; letter-spacing: 2px;">
        
        <div style="display: flex; gap: 10px;">
          <button class="btn" style="flex: 1; background: transparent; color: #64748b; border: 1px solid #cbd5e1; padding: 10px; border-radius: 8px; font-weight: 600;" @click="passwordPromptOpen = false">Cancelar</button>
          <button class="btn" style="flex: 1; background: #ef4444; color: white; padding: 10px; border-radius: 8px; font-weight: 700; border: none;" @click="submitPassword">Confirmar</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pc-map-container {
  background: #fdfdfd;
  padding: 3rem;
  border-radius: 16px;
  border: 1px dashed #e2e8f0;
  overflow-x: auto;
}
.pc-grid {
  display: grid;
  grid-template-columns: repeat(11, 75px);
  grid-template-rows: repeat(8, 100px);
  gap: 45px;
  justify-content: center;
}
.pc-card-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
}
.pc-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: pointer;
  transition: all 0.2s ease;
}
.pc-card:hover { 
  transform: translateY(-5px); 
}
.monitor {
  width: 70px;
  height: 50px;
  background: #cbd5e1;
  border-radius: 6px;
  padding: 4px;
  margin-bottom: 5px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  transition: all 0.3s ease;
}
.screen {
  width: 100%;
  height: 100%;
  background: #f8fafc;
  border-radius: 3px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  transition: all 0.3s ease;
  border: 1px solid #e2e8f0;
}
.stand {
  width: 24px;
  height: 12px;
  background: #cbd5e1;
  margin: -2px auto 0;
  border-radius: 0 0 4px 4px;
  transition: all 0.3s ease;
}
.occupied .monitor { 
  background: #5eead4; 
  box-shadow: 0 0 15px rgba(20, 184, 166, 0.3);
}
.occupied .stand {
  background: #14b8a6;
}
.occupied .screen { 
  background: #f0fdfa; 
  color: #0d9488; 
  border-color: #5eead4;
}
.power-icon {
  color: #94a3b8;
  font-size: 18px;
}
.user-icon {
  text-shadow: 0 2px 4px rgba(0,0,0,0.1);
}
.pc-name { 
  font-size: 0.7rem; 
  color: #334155; 
  margin-top: 5px; 
  font-weight: 700;
  background: #f1f5f9;
  padding: 2px 6px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
}
.empty-slot {
  opacity: 0.4;
  cursor: default;
}
.monitor.gray, .stand.gray { background: #e2e8f0; }
.screen.gray { background: #f8fafc; }
.pc-name.gray { color: #94a3b8; border-color: #f1f5f9; }

.detail-overlay {
  position: fixed; 
  top: 0; 
  left: 0; 
  width: 100%; 
  height: 100%;
  background: rgba(192, 192, 192, 0.7); 
  backdrop-filter: blur(4px);
  display: flex; 
  align-items: center; 
  justify-content: center;
  z-index: 2000;
}
.detail-card { 
  width: 400px; 
  text-align: left;
  border-radius: 20px;
  padding: 25px;
  border: 1px solid #ffffff;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.15);
  background: #ffffff;
  animation: modalEnter 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}
@keyframes modalEnter {
  from { transform: scale(0.9); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}
input[type="time"]::-webkit-calendar-picker-indicator {
  cursor: pointer;
}
</style>
