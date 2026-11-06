<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config.js'

const props = defineProps({
  currentUser: {
    type: Object,
    default: null
  }
})

const admins = ref([])
const isLoading = ref(false)
const errorMsg = ref('')
const successMsg = ref('')
const searchQuery = ref('')
const statusFilter = ref('Todos')

const showModal = ref(false)
const isEditMode = ref(false)
const isSaving = ref(false)
const modalError = ref('')
const formAdmin = ref({
  idUsuarioAdmin: null,
  username: '',
  password: '',
  nombres: '',
  correo: '',
  rol: 1
})

const showPasswordModal = ref(false)
const isSavingPassword = ref(false)
const passwordModalError = ref('')
const passwordTargetAdmin = ref(null)
const formPassword = ref({
  nuevaPassword: '',
  confirmarPassword: ''
})

const getAdminId = (admin) => admin?.idUsuarioAdmin ?? admin?.id ?? admin?.IdUsuarioAdmin
const getAdminUsername = (admin) => admin?.username ?? admin?.Username ?? ''
const getAdminNombres = (admin) => admin?.nombres ?? admin?.Nombres ?? ''
const getAdminCorreo = (admin) => admin?.correo ?? admin?.Correo ?? ''
const getAdminRol = (admin) => admin?.rol ?? admin?.Rol ?? 1
const getAdminEstado = (admin) => Boolean(admin?.estadoActivo ?? admin?.estado ?? admin?.EstadoActivo)
const getAdminFechaCreacion = (admin) => admin?.fechaCreacion ?? admin?.FechaCreacion ?? ''
const getAdminUltimoAcceso = (admin) => admin?.ultimoAcceso ?? admin?.UltimoAcceso ?? null

const getRoleName = (rol) => {
  const r = Number(rol)
  if (r === 1) return 'SuperAdmin'
  if (r === 2) return 'Biblioteca'
  if (r === 3) return 'Laboratorio'
  return 'SuperAdmin'
}

const formatDate = (val) => {
  if (!val) return '-'
  try {
    const d = new Date(val)
    if (isNaN(d.getTime())) return String(val)
    const pad = (n) => String(n).padStart(2, '0')
    const y = d.getFullYear()
    const m = pad(d.getMonth() + 1)
    const day = pad(d.getDate())
    const h = pad(d.getHours())
    const min = pad(d.getMinutes())
    return `${y}-${m}-${day} ${h}:${min}`
  } catch {
    return String(val)
  }
}

const isCurrentSessionUser = (admin) => {
  if (!props.currentUser || !admin) return false
  const currentId = props.currentUser.idUsuarioAdmin ?? props.currentUser.id ?? props.currentUser.idUsuario ?? props.currentUser.IdUsuarioAdmin
  const adminId = getAdminId(admin)
  if (currentId != null && adminId != null && String(currentId) === String(adminId)) {
    return true
  }
  const currentUsername = (props.currentUser.username ?? props.currentUser.Username ?? '').trim().toLowerCase()
  const adminUsername = getAdminUsername(admin).trim().toLowerCase()
  if (currentUsername && adminUsername && currentUsername === adminUsername) {
    return true
  }
  return false
}

const filteredAdmins = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  const status = statusFilter.value
  return admins.value.filter((admin) => {
    const username = getAdminUsername(admin).toLowerCase()
    const nombres = getAdminNombres(admin).toLowerCase()
    const correo = getAdminCorreo(admin).toLowerCase()
    const matchesQ = !q || username.includes(q) || nombres.includes(q) || correo.includes(q)

    const estado = getAdminEstado(admin)
    const matchesStatus = status === 'Todos' || (status === 'Activo' ? estado : !estado)

    return matchesQ && matchesStatus
  })
})

const fetchAdmins = async () => {
  isLoading.value = true
  errorMsg.value = ''
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/usuarios`)
    admins.value = Array.isArray(res.data) ? res.data.map((a) => ({ ...a })) : []
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Error al cargar los administradores.'
  } finally {
    isLoading.value = false
  }
}

const toggleStatus = async (admin) => {
  if (isCurrentSessionUser(admin)) return
  const id = getAdminId(admin)
  errorMsg.value = ''
  try {
    const res = await axios.patch(`${API_BASE_URL}/api/admin/usuarios/${id}/estado`)
    const nuevoEstado = res.data?.estadoActivo
    if (nuevoEstado !== undefined) {
      if (admin.estadoActivo !== undefined) admin.estadoActivo = nuevoEstado
      if (admin.estado !== undefined) admin.estado = nuevoEstado
      if (admin.EstadoActivo !== undefined) admin.EstadoActivo = nuevoEstado
    } else {
      const invertido = !getAdminEstado(admin)
      if (admin.estadoActivo !== undefined) admin.estadoActivo = invertido
      if (admin.estado !== undefined) admin.estado = invertido
      if (admin.EstadoActivo !== undefined) admin.EstadoActivo = invertido
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Error al cambiar el estado del usuario.'
  }
}

const openNewModal = () => {
  modalError.value = ''
  isEditMode.value = false
  formAdmin.value = {
    idUsuarioAdmin: null,
    username: '',
    password: '',
    nombres: '',
    correo: '',
    rol: 1
  }
  showModal.value = true
}

const openEditModal = (admin) => {
  modalError.value = ''
  isEditMode.value = true
  formAdmin.value = {
    idUsuarioAdmin: getAdminId(admin),
    username: getAdminUsername(admin),
    password: '',
    nombres: getAdminNombres(admin),
    correo: getAdminCorreo(admin),
    rol: Number(getAdminRol(admin))
  }
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  modalError.value = ''
}

const handleSave = async () => {
  modalError.value = ''
  const nombres = formAdmin.value.nombres.trim()
  if (!nombres) {
    modalError.value = 'Los nombres son requeridos.'
    return
  }

  if (!isEditMode.value) {
    const username = formAdmin.value.username.trim()
    const password = formAdmin.value.password.trim()
    if (!username || username.length < 3) {
      modalError.value = 'El nombre de usuario debe tener al menos 3 caracteres.'
      return
    }
    if (!password || password.length < 6) {
      modalError.value = 'La contraseña debe tener al menos 6 caracteres.'
      return
    }
  }

  isSaving.value = true
  try {
    if (isEditMode.value) {
      const id = formAdmin.value.idUsuarioAdmin
      const payload = {
        nombres: formAdmin.value.nombres.trim(),
        correo: formAdmin.value.correo?.trim() || null,
        rol: Number(formAdmin.value.rol)
      }
      const res = await axios.put(`${API_BASE_URL}/api/admin/usuarios/${id}`, payload)
      const updated = res.data
      const idx = admins.value.findIndex((a) => getAdminId(a) === id)
      if (idx !== -1 && updated) {
        admins.value[idx] = { ...admins.value[idx], ...updated }
      }
      successMsg.value = 'Usuario actualizado exitosamente.'
    } else {
      const payload = {
        username: formAdmin.value.username.trim(),
        password: formAdmin.value.password.trim(),
        nombres: formAdmin.value.nombres.trim(),
        correo: formAdmin.value.correo?.trim() || null,
        rol: Number(formAdmin.value.rol)
      }
      const res = await axios.post(`${API_BASE_URL}/api/admin/usuarios`, payload)
      if (res.data) {
        admins.value.unshift(res.data)
      }
      successMsg.value = 'Usuario creado exitosamente.'
    }
    closeModal()
  } catch (err) {
    modalError.value = err.response?.data?.message || 'Error al guardar datos del usuario.'
  } finally {
    isSaving.value = false
  }
}

const openPasswordModal = (admin) => {
  passwordModalError.value = ''
  passwordTargetAdmin.value = admin
  formPassword.value = {
    nuevaPassword: '',
    confirmarPassword: ''
  }
  showPasswordModal.value = true
}

const closePasswordModal = () => {
  showPasswordModal.value = false
  passwordTargetAdmin.value = null
  passwordModalError.value = ''
}

const handleSavePassword = async () => {
  passwordModalError.value = ''
  const pass = formPassword.value.nuevaPassword.trim()
  const conf = formPassword.value.confirmarPassword.trim()

  if (!pass || pass.length < 6) {
    passwordModalError.value = 'La nueva contraseña debe tener al menos 6 caracteres.'
    return
  }
  if (pass !== conf) {
    passwordModalError.value = 'Las contraseñas no coinciden.'
    return
  }

  isSavingPassword.value = true
  try {
    const id = getAdminId(passwordTargetAdmin.value)
    const res = await axios.put(`${API_BASE_URL}/api/admin/usuarios/${id}/password`, {
      nuevaPassword: pass
    })
    successMsg.value = res.data?.message || 'Contraseña actualizada exitosamente.'
    closePasswordModal()
  } catch (err) {
    passwordModalError.value = err.response?.data?.message || 'Error al cambiar la contraseña.'
  } finally {
    isSavingPassword.value = false
  }
}

onMounted(() => {
  fetchAdmins()
})
</script>

<template>
  <div class="accesos-container">
    <div v-if="errorMsg" class="alert-banner alert-error">
      <span>{{ errorMsg }}</span>
      <button class="alert-close" @click="errorMsg = ''">✕</button>
    </div>

    <div v-if="successMsg" class="alert-banner alert-success">
      <span>{{ successMsg }}</span>
      <button class="alert-close" @click="successMsg = ''">✕</button>
    </div>

    <div class="page-header">
      <div class="header-text">
        <h2>Administradores</h2>
        <div class="count-sub">{{ admins.length }} Registrados</div>
      </div>

      <div class="toolbar-right">
        <div class="select-wrapper">
          <select id="statusFilter" v-model="statusFilter" class="filter-select">
            <option value="Todos">Todos los Estados</option>
            <option value="Activo">Solo Activos</option>
            <option value="Inactivo">Solo Inactivos</option>
          </select>
          <svg class="select-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="6 9 12 15 18 9"></polyline>
          </svg>
        </div>

        <div class="search-wrapper">
          <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <input
            id="searchInput"
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="Buscar por usuario, nombre o correo..."
          />
        </div>

        <button class="btn btn-primary" @click="openNewModal">Nuevo</button>
      </div>
    </div>

    <div class="table-responsive">
      <table>
        <thead>
          <tr>
            <th>Nº</th>
            <th>Usuario</th>
            <th>Nombres Completos</th>
            <th>Estado</th>
            <th>Correo</th>
            <th>Rol Asignado</th>
            <th>Fecha Creación</th>
            <th>Último Acceso</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="isLoading">
            <td colspan="9" class="empty-cell">Cargando administradores...</td>
          </tr>
          <tr v-else-if="filteredAdmins.length === 0">
            <td colspan="9" class="empty-cell">No se encontraron registros.</td>
          </tr>
          <tr v-for="(admin, index) in filteredAdmins" :key="getAdminId(admin) || index">
            <td class="col-index">{{ index + 1 }}</td>
            <td><strong class="col-username">{{ getAdminUsername(admin) }}</strong></td>
            <td>{{ getAdminNombres(admin) }}</td>
            <td>
              <span :class="['badge-status', getAdminEstado(admin) ? 'status-active' : 'status-inactive']">
                {{ getAdminEstado(admin) ? 'Activo' : 'Inactivo' }}
              </span>
            </td>
            <td class="col-email">{{ getAdminCorreo(admin) || '-' }}</td>
            <td>
              <span class="badge-role">{{ getRoleName(getAdminRol(admin)) }}</span>
            </td>
            <td class="col-date">{{ formatDate(getAdminFechaCreacion(admin)) }}</td>
            <td class="col-date">{{ formatDate(getAdminUltimoAcceso(admin)) }}</td>
            <td class="col-actions">
              <button
                data-testid="toggle-status-btn"
                :class="['icon-btn', getAdminEstado(admin) ? 'suspend-btn' : 'activate-btn']"
                :disabled="isCurrentSessionUser(admin)"
                :title="isCurrentSessionUser(admin) ? 'No puedes desactivar tu propia cuenta' : (getAdminEstado(admin) ? 'Desactivar' : 'Activar')"
                @click="toggleStatus(admin)"
              >
                <svg v-if="getAdminEstado(admin)" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="10"></circle>
                  <line x1="15" y1="9" x2="9" y2="15"></line>
                  <line x1="9" y1="9" x2="15" y2="15"></line>
                </svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                  <polyline points="22 4 12 14.01 9 11.01"></polyline>
                </svg>
              </button>
              <button
                data-testid="edit-admin-btn"
                class="icon-btn edit-btn"
                title="Editar"
                @click="openEditModal(admin)"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                </svg>
              </button>
              <button
                data-testid="key-password-btn"
                class="icon-btn key-btn"
                title="Cambiar contraseña"
                @click="openPasswordModal(admin)"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                </svg>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pagination-bar">
      <span class="pagination-info">Mostrando 1 a {{ filteredAdmins.length }} de {{ admins.length }} registros</span>
      <div class="pagination-controls">
        <button class="page-btn" disabled>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <polyline points="15 18 9 12 15 6"></polyline>
          </svg>
          Anterior
        </button>
        <span class="page-indicator">Página 1 de 1</span>
        <button class="page-btn" disabled>
          Siguiente
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <polyline points="9 18 15 12 9 6"></polyline>
          </svg>
        </button>
      </div>
    </div>

    <div v-if="showModal" class="modal-backdrop" @click.self="closeModal">
      <div class="modal-card">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                <circle cx="8.5" cy="7" r="4"></circle>
                <line x1="20" y1="8" x2="20" y2="14"></line>
                <line x1="23" y1="11" x2="17" y2="11"></line>
              </svg>
            </div>
            <h3 id="modalTitle">{{ isEditMode ? 'Editar Usuario' : 'Nuevo Usuario' }}</h3>
          </div>
          <button class="close-btn" @click="closeModal">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        <form @submit.prevent="handleSave">
          <div class="modal-body">
            <div v-if="modalError" class="modal-alert-error">
              {{ modalError }}
            </div>

            <div class="form-grid-2">
              <div class="input-group">
                <label>USUARIO (USERNAME) *</label>
                <input
                  v-model="formAdmin.username"
                  type="text"
                  class="premium-input"
                  placeholder="Ej. fvalero"
                  :disabled="isEditMode"
                  required
                />
              </div>
              <div class="input-group">
                <label>ROL ASIGNADO *</label>
                <select v-model="formAdmin.rol" class="premium-input" required>
                  <option :value="1">SuperAdmin</option>
                  <option :value="2">Biblioteca</option>
                  <option :value="3">Laboratorio</option>
                </select>
              </div>
            </div>

            <div class="input-group">
              <label>NOMBRES COMPLETOS *</label>
              <input
                v-model="formAdmin.nombres"
                type="text"
                class="premium-input"
                placeholder="Nombre completo institucional"
                required
              />
            </div>

            <div :class="isEditMode ? '' : 'form-grid-2'">
              <div class="input-group">
                <label>CORREO</label>
                <input
                  v-model="formAdmin.correo"
                  type="email"
                  class="premium-input"
                  placeholder="usuario@urp.edu.pe"
                />
              </div>
              <div v-if="!isEditMode" class="input-group">
                <label>CONTRASEÑA *</label>
                <input
                  v-model="formAdmin.password"
                  type="password"
                  class="premium-input"
                  placeholder="••••••••••••"
                  required
                />
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="closeModal">Cancelar</button>
            <button type="submit" class="premium-btn" :disabled="isSaving">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                <polyline points="17 21 17 13 7 13 7 21"></polyline>
                <polyline points="7 3 7 8 15 8"></polyline>
              </svg>
              {{ isSaving ? 'Guardando...' : 'Guardar Datos' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <div v-if="showPasswordModal" class="modal-backdrop" @click.self="closePasswordModal">
      <div class="modal-card">
        <div class="modal-header">
          <div class="modal-title-wrapper">
            <div class="modal-icon key-icon-bg">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
            </div>
            <h3>Cambiar Contraseña</h3>
          </div>
          <button class="close-btn" @click="closePasswordModal">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        <form @submit.prevent="handleSavePassword">
          <div class="modal-body">
            <div v-if="passwordModalError" class="modal-alert-error">
              {{ passwordModalError }}
            </div>

            <p class="target-user-info">
              Estableciendo nueva contraseña para el usuario
              <strong>{{ getAdminUsername(passwordTargetAdmin) }}</strong>.
            </p>

            <div class="input-group">
              <label>NUEVA CONTRASEÑA *</label>
              <input
                v-model="formPassword.nuevaPassword"
                type="password"
                class="premium-input"
                placeholder="Mínimo 6 caracteres"
                required
              />
            </div>

            <div class="input-group">
              <label>CONFIRMAR NUEVA CONTRASEÑA *</label>
              <input
                v-model="formPassword.confirmarPassword"
                type="password"
                class="premium-input"
                placeholder="Repite la contraseña"
                required
              />
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn-secondary" @click="closePasswordModal">Cancelar</button>
            <button type="submit" class="premium-btn" :disabled="isSavingPassword">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                <polyline points="17 21 17 13 7 13 7 21"></polyline>
                <polyline points="7 3 7 8 15 8"></polyline>
              </svg>
              {{ isSavingPassword ? 'Actualizando...' : 'Actualizar Contraseña' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.accesos-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  width: 100%;
}

.alert-banner {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 1.25rem;
  border-radius: 8px;
  font-size: 0.88rem;
  font-weight: 500;
}

.alert-error {
  background: #fef2f2;
  color: #991b1b;
  border: 1px solid #fecaca;
}

.alert-success {
  background: #f0fdf4;
  color: #166534;
  border: 1px solid #bbf7d0;
}

.alert-close {
  background: transparent;
  border: none;
  cursor: pointer;
  font-size: 1rem;
  color: inherit;
  font-weight: 700;
  line-height: 1;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 1rem;
  flex-wrap: wrap;
}

.header-text h2 {
  color: #111827;
  font-size: 2rem;
  font-weight: 700;
  margin-bottom: 0.25rem;
  line-height: 1.2;
}

.header-text .count-sub {
  color: #6b7280;
  font-size: 0.875rem;
}

.toolbar-right {
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
}

.select-wrapper {
  position: relative;
}

.filter-select {
  padding: 0.5rem 2.5rem 0.5rem 1rem;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
  font-size: 0.875rem;
  color: #111827;
  background: #ffffff;
  appearance: none;
  cursor: pointer;
  outline: none;
  font-family: inherit;
}

.select-arrow {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  pointer-events: none;
  color: #9ca3af;
}

.search-wrapper {
  position: relative;
}

.search-icon {
  position: absolute;
  left: 10px;
  top: 50%;
  transform: translateY(-50%);
  color: #9ca3af;
}

.search-input {
  padding: 0.5rem 1rem 0.5rem 2rem;
  border-radius: 0.5rem;
  border: 1px solid #e5e7eb;
  width: 320px;
  font-size: 0.875rem;
  color: #111827;
  outline: none;
  font-family: inherit;
  background: #ffffff;
}

.search-input:focus {
  border-color: #6366f1;
}

.btn {
  padding: 0.5rem 1rem;
  border-radius: 0.375rem;
  border: none;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.2s;
  font-family: inherit;
}

.btn-primary {
  background: #9f1239;
  color: #ffffff;
}

.btn-primary:hover {
  background: #881337;
}

.table-responsive {
  width: 100%;
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 0.5rem;
}

th {
  padding: 1rem 0.5rem;
  color: #6b7280;
  border-bottom: 1px solid #e5e7eb;
  font-weight: 500;
  font-size: 0.875rem;
  text-align: center;
}

td {
  padding: 1rem 0.5rem;
  border-bottom: 1px solid #f3f4f6;
  font-size: 0.875rem;
  color: #111827;
  font-weight: 600;
  text-align: center;
  vertical-align: middle;
}

tr:hover td {
  background: #f9fafb;
}

.empty-cell {
  padding: 2.5rem;
  color: #6b7280;
  font-weight: 500;
}

.col-index {
  color: #6b7280;
  font-weight: 600;
}

.col-username {
  color: #111827;
}

.col-email {
  color: #0ea5e9;
  font-weight: 500;
}

.col-date {
  color: #6b7280;
  font-weight: 500;
}

.col-actions {
  white-space: nowrap;
}

.badge-status {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
  display: inline-block;
}

.status-active {
  background: #f0fdf4;
  color: #166534;
}

.status-inactive {
  background: #fef2f2;
  color: #991b1b;
}

.badge-role {
  background: #eff6ff;
  color: #1d4ed8;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
  white-space: nowrap;
  display: inline-block;
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
  margin: 0 4px;
}

.icon-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.edit-btn {
  color: #6366f1;
}

.edit-btn:hover:not(:disabled) {
  background: #e0e7ff;
}

.suspend-btn {
  color: #f59e0b;
}

.suspend-btn:hover:not(:disabled) {
  background: #fef3c7;
}

.activate-btn {
  color: #10b981;
}

.activate-btn:hover:not(:disabled) {
  background: #d1fae5;
}

.key-btn {
  color: #8b5cf6;
}

.key-btn:hover:not(:disabled) {
  background: #ede9fe;
}

.pagination-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 1.5rem;
  background: #ffffff;
  padding: 1rem;
  border-radius: 12px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
}

.pagination-info {
  color: #64748b;
  font-size: 0.875rem;
  font-weight: 500;
}

.pagination-controls {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.page-btn {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  color: #475569;
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 0.4rem 0.8rem;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
}

.page-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.page-indicator {
  color: #111827;
  font-size: 0.875rem;
  font-weight: 600;
  padding: 0 0.5rem;
}

.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-card {
  width: 100%;
  max-width: 650px;
  max-height: 95vh;
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.modal-header {
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #f1f5f9;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #f8fafc;
}

.modal-title-wrapper {
  display: flex;
  align-items: center;
  gap: 12px;
}

.modal-icon {
  background: #e0e7ff;
  color: #4f46e5;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.key-icon-bg {
  background: #ede9fe;
  color: #7c3aed;
}

.modal-header h3 {
  margin: 0;
  color: #0f172a;
  font-size: 1.25rem;
  font-weight: 800;
}

.close-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  padding: 8px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.close-btn:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.modal-body {
  padding: 2rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  overflow-y: auto;
}

.modal-alert-error {
  background: #fef2f2;
  color: #991b1b;
  border: 1px solid #fecaca;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 500;
}

.target-user-info {
  color: #475569;
  font-size: 0.9rem;
  line-height: 1.4;
}

.form-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.25rem;
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.input-group label {
  font-size: 0.75rem;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: left;
}

.premium-input {
  padding: 12px 16px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  color: #1e293b;
  font-size: 0.95rem;
  font-family: inherit;
  transition: all 0.2s;
  width: 100%;
  box-sizing: border-box;
  outline: none;
}

.premium-input:focus {
  border-color: #6366f1;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(99, 102, 241, 0.1);
}

.premium-input:disabled {
  background: #f1f5f9;
  color: #94a3b8;
  cursor: not-allowed;
}

.premium-input::placeholder {
  color: #94a3b8;
}

.modal-footer {
  padding: 1.25rem 2rem;
  border-top: 1px solid #f1f5f9;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  background: #f8fafc;
}

.btn-secondary {
  background: #ffffff;
  color: #475569;
  border: 1px solid #cbd5e1;
  font-weight: 600;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.88rem;
  font-family: inherit;
  transition: all 0.2s;
}

.btn-secondary:hover {
  background: #f1f5f9;
}

.premium-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 24px;
  font-weight: 700;
  background: #4f46e5;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.88rem;
  font-family: inherit;
  box-shadow: 0 4px 6px -1px rgba(79, 70, 229, 0.3);
  transition: all 0.2s;
}

.premium-btn:hover:not(:disabled) {
  background: #4338ca;
  transform: translateY(-1px);
  box-shadow: 0 6px 8px -1px rgba(79, 70, 229, 0.4);
}

.premium-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
