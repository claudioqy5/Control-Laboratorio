<script setup>
import { ref, onMounted } from 'vue'
import axios, { API_BASE_URL } from '../config'

const slides = ref([])
const loading = ref(false)

const showCreateModal = ref(false)
const guardando = ref(false)
const showDeleteModal = ref(false)
const slideToDelete = ref(null)
const eliminando = ref(false)
const showZoomModal = ref(false)
const zoomSlide = ref(null)

const toast = ref({ show: false, message: '', type: 'success' })
let toastTimer = null

const showToast = (message, type = 'success') => {
  if (toastTimer) clearTimeout(toastTimer)
  toast.value = { show: true, message, type }
  toastTimer = setTimeout(() => {
    toast.value.show = false
  }, 3500)
}

const getImageUrl = (path) => {
  if (!path) return ''
  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path
  }
  return `${API_BASE_URL}${path.startsWith('/') ? '' : '/'}${path}`
}

const loadSlides = async () => {
  loading.value = true
  try {
    const res = await axios.get(`${API_BASE_URL}/api/admin/boletin-slides`)
    slides.value = Array.isArray(res.data) ? res.data : []
  } catch {
    showToast('Error al conectar con el servidor para obtener los afiches.', 'error')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadSlides()
})

const fileInputRef = ref(null)
const selectedFile = ref(null)
const imagePreviewUrl = ref('')

const form = ref({
  titulo: '',
  descripcion: '',
  formato: 1,
  enlaceUrl: ''
})

const formErrors = ref({
  image: '',
  titulo: '',
  descripcion: ''
})

const resetForm = () => {
  form.value = {
    titulo: '',
    descripcion: '',
    formato: 1,
    enlaceUrl: ''
  }
  selectedFile.value = null
  if (imagePreviewUrl.value) {
    URL.revokeObjectURL(imagePreviewUrl.value)
    imagePreviewUrl.value = ''
  }
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
  formErrors.value = {
    image: '',
    titulo: '',
    descripcion: ''
  }
}

const handleOpenCreateModal = () => {
  if (slides.value.length >= 6) {
    showToast('Límite de 6 afiches activos alcanzado. Elimina uno previo para liberar cupo.', 'error')
    return
  }
  resetForm()
  showCreateModal.value = true
}

const handleCloseCreateModal = () => {
  if (guardando.value) return
  showCreateModal.value = false
  resetForm()
}

const handleFileChange = (e) => {
  formErrors.value.image = ''
  const file = e.target.files?.[0]
  if (!file) {
    selectedFile.value = null
    imagePreviewUrl.value = ''
    return
  }

  const allowedExts = ['.webp', '.jpg', '.jpeg', '.png']
  const fileName = file.name.toLowerCase()
  const hasValidExt = allowedExts.some(ext => fileName.endsWith(ext))

  if (!hasValidExt) {
    formErrors.value.image = 'Formato no permitido. Solo se aceptan extensiones .webp, .jpg, .jpeg y .png.'
    selectedFile.value = null
    imagePreviewUrl.value = ''
    if (fileInputRef.value) fileInputRef.value.value = ''
    return
  }

  const maxBytes = 5 * 1024 * 1024
  if (file.size > maxBytes) {
    formErrors.value.image = 'La imagen excede el límite máximo de 5 MB.'
    selectedFile.value = null
    imagePreviewUrl.value = ''
    if (fileInputRef.value) fileInputRef.value.value = ''
    return
  }

  selectedFile.value = file
  if (imagePreviewUrl.value) {
    URL.revokeObjectURL(imagePreviewUrl.value)
  }
  imagePreviewUrl.value = URL.createObjectURL(file)
}

const removeSelectedImage = () => {
  selectedFile.value = null
  if (imagePreviewUrl.value) {
    URL.revokeObjectURL(imagePreviewUrl.value)
    imagePreviewUrl.value = ''
  }
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

const setFormato = (f) => {
  form.value.formato = f
  if (f === 1) {
    formErrors.value.descripcion = ''
  }
}

const handleSubmit = async () => {
  formErrors.value = {
    image: '',
    titulo: '',
    descripcion: ''
  }

  let hasErrors = false

  if (!selectedFile.value) {
    formErrors.value.image = 'Debes seleccionar una imagen para el afiche.'
    hasErrors = true
  }

  if (!form.value.titulo?.trim()) {
    formErrors.value.titulo = 'El título del afiche es obligatorio.'
    hasErrors = true
  }

  if (form.value.formato === 2 && !form.value.descripcion?.trim()) {
    formErrors.value.descripcion = 'La descripción es obligatoria cuando el formato es Bipartito.'
    hasErrors = true
  }

  if (hasErrors) return

  guardando.value = true
  try {
    const formData = new FormData()
    formData.append('image', selectedFile.value)
    formData.append('titulo', form.value.titulo.trim())
    formData.append('formato', String(form.value.formato))
    if (form.value.descripcion?.trim()) {
      formData.append('descripcion', form.value.descripcion.trim())
    }
    if (form.value.enlaceUrl?.trim()) {
      formData.append('enlaceUrl', form.value.enlaceUrl.trim())
    }

    await axios.post(`${API_BASE_URL}/api/admin/boletin-slides`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })

    showToast('Afiche publicado exitosamente en el boletín.', 'success')
    showCreateModal.value = false
    resetForm()
    await loadSlides()
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al guardar el afiche.'
    showToast(msg, 'error')
  } finally {
    guardando.value = false
  }
}

const handleConfirmDelete = (slide) => {
  slideToDelete.value = slide
  showDeleteModal.value = true
}

const handleCancelDelete = () => {
  if (eliminando.value) return
  showDeleteModal.value = false
  slideToDelete.value = null
}

const handleExecuteDelete = async () => {
  if (!slideToDelete.value) return
  eliminando.value = true
  try {
    await axios.delete(`${API_BASE_URL}/api/admin/boletin-slides/${slideToDelete.value.idSlide}`)
    showToast('Afiche eliminado correctamente.', 'success')
    showDeleteModal.value = false
    slideToDelete.value = null
    await loadSlides()
  } catch (err) {
    const msg = err.response?.data?.message || 'Error al eliminar el afiche.'
    showToast(msg, 'error')
  } finally {
    eliminando.value = false
  }
}

const handleOpenZoom = (slide) => {
  zoomSlide.value = slide
  showZoomModal.value = true
}

const handleCloseZoom = () => {
  showZoomModal.value = false
  zoomSlide.value = null
}
</script>

<template>
  <div class="boletin-view">
    <div class="header-section">
      <div class="header-left">
        <h2 class="header-title">Boletín Informativo & Novedades</h2>
        <p class="header-subtitle">
          Gestión de afiches y comunicados visuales para el carrusel de novedades en la Biblioteca Virtual FAMURP.
        </p>
      </div>

      <div class="header-right">
        <div :class="['quota-badge', slides.length >= 6 ? 'quota-badge-full' : 'quota-badge-ok']">
          <span class="quota-dot"></span>
          <span class="quota-text">{{ slides.length }} / 6 afiches activos</span>
        </div>

        <button
          type="button"
          class="btn-primary-add"
          @click="handleOpenCreateModal"
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          <span>+ Nuevo Afiche</span>
        </button>
      </div>
    </div>

    <div class="table-container">
      <div v-if="loading" class="state-loading">
        <div class="spinner"></div>
        <span>Cargando afiches del boletín...</span>
      </div>

      <div v-else-if="slides.length === 0" class="state-empty">
        <div class="empty-icon-wrap">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#94A3B8" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
            <circle cx="8.5" cy="8.5" r="1.5" />
            <polyline points="21 15 16 10 5 21" />
          </svg>
        </div>
        <h3 class="empty-title">No hay afiches activos en el boletín</h3>
        <p class="empty-text">
          Publica el primer afiche informativo o evento para visualizarlo en el carrusel principal.
        </p>
        <button type="button" class="btn-primary-add" @click="handleOpenCreateModal">
          <span>+ Publicar Afiche</span>
        </button>
      </div>

      <div v-else class="table-responsive">
        <table class="boletin-table">
          <thead>
            <tr>
              <th class="th-num">Nº</th>
              <th class="th-thumb">Miniatura</th>
              <th class="th-title">Título</th>
              <th class="th-format">Formato</th>
              <th class="th-desc">Descripción</th>
              <th class="th-link">Enlace Externo</th>
              <th class="th-actions">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(slide, index) in slides" :key="slide.idSlide">
              <td class="td-num">{{ index + 1 }}</td>
              <td class="td-thumb">
                <div
                  :class="['thumb-box', slide.formato === 1 ? 'thumb-box-vertical' : 'thumb-box-bipartito']"
                  @click="handleOpenZoom(slide)"
                  title="Clic para ampliar imagen"
                >
                  <img
                    :src="getImageUrl(slide.imagenUrl)"
                    :alt="slide.titulo"
                    :class="['thumb-img', slide.formato === 1 ? 'thumb-cover' : 'thumb-contain']"
                    loading="lazy"
                  />
                  <div class="thumb-hover-overlay">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#FFFFFF" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                      <circle cx="11" cy="11" r="8" />
                      <line x1="21" y1="21" x2="16.65" y2="16.65" />
                    </svg>
                  </div>
                </div>
              </td>
              <td class="td-title">
                <div class="title-cell">
                  <span class="slide-title-text">{{ slide.titulo }}</span>
                  <span v-if="slide.enlaceUrl" class="badge-enlace" title="Tiene enlace configurado">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                      <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
                      <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
                    </svg>
                    Enlace
                  </span>
                </div>
              </td>
              <td class="td-format">
                <span v-if="slide.formato === 1" class="badge badge-vertical">
                  Vertical
                </span>
                <span v-else class="badge badge-bipartito">
                  Bipartito
                </span>
              </td>
              <td class="td-desc">
                <span v-if="slide.formato === 1" class="text-muted">-</span>
                <span v-else class="desc-content">{{ slide.descripcion || '-' }}</span>
              </td>
              <td class="td-link">
                <a
                  v-if="slide.enlaceUrl"
                  :href="slide.enlaceUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="external-anchor"
                  title="Abrir enlace en pestaña nueva"
                >
                  <span>Abrir</span>
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
                    <polyline points="15 3 21 3 21 9" />
                    <line x1="10" y1="14" x2="21" y2="3" />
                  </svg>
                </a>
                <span v-else class="text-muted">-</span>
              </td>
              <td class="td-actions">
                <div class="action-btn-group">
                  <button
                    type="button"
                    class="btn-icon btn-icon-zoom"
                    @click="handleOpenZoom(slide)"
                    title="Ver imagen en tamaño completo"
                  >
                    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <circle cx="11" cy="11" r="8" />
                      <line x1="21" y1="21" x2="16.65" y2="16.65" />
                      <line x1="11" y1="8" x2="11" y2="14" />
                      <line x1="8" y1="11" x2="14" y2="11" />
                    </svg>
                  </button>

                  <button
                    type="button"
                    class="btn-icon btn-icon-delete"
                    @click="handleConfirmDelete(slide)"
                    title="Eliminar afiche"
                  >
                    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="3 6 5 6 21 6" />
                      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                      <line x1="10" y1="11" x2="10" y2="17" />
                      <line x1="14" y1="11" x2="14" y2="17" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div :class="['modal-overlay', { active: showCreateModal }]">
      <div class="modal-card">
        <div class="modal-header">
          <div>
            <h3 class="modal-title">Nuevo Afiche para el Boletín</h3>
            <p class="modal-subtitle">Sube el arte promocional y define su formato de presentación.</p>
          </div>
          <button
            type="button"
            class="modal-close-btn"
            @click="handleCloseCreateModal"
            :disabled="guardando"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <form class="modal-body" @submit.prevent="handleSubmit">
          <div class="form-group">
            <label class="form-label">
              <span>Imagen del Afiche</span>
              <span class="required-star">*</span>
            </label>

            <div v-if="!imagePreviewUrl" class="upload-dropzone" @click="fileInputRef?.click()">
              <input
                ref="fileInputRef"
                type="file"
                class="file-input-hidden"
                accept=".webp,.jpg,.jpeg,.png,image/webp,image/jpeg,image/png"
                @change="handleFileChange"
              />
              <div class="upload-icon-circle">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#008744" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                  <polyline points="17 8 12 3 7 8" />
                  <line x1="12" y1="3" x2="12" y2="15" />
                </svg>
              </div>
              <div class="upload-instructions">
                <span class="upload-primary-text">Haz clic para seleccionar una imagen</span>
                <span class="upload-secondary-text">Formatos permitidos: WEBP, JPG o PNG (máximo 5 MB)</span>
              </div>
            </div>

            <div v-else class="preview-card">
              <div class="preview-thumb-wrap">
                <img :src="imagePreviewUrl" alt="Vista previa del afiche" class="preview-img" />
              </div>
              <div class="preview-info">
                <span class="preview-filename">{{ selectedFile?.name }}</span>
                <span class="preview-filesize">{{ (selectedFile?.size ? (selectedFile.size / 1024 / 1024).toFixed(2) : '0') }} MB</span>
                <button type="button" class="preview-remove-btn" @click="removeSelectedImage">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                  <span>Cambiar archivo</span>
                </button>
              </div>
            </div>

            <span v-if="formErrors.image" class="field-error-msg">{{ formErrors.image }}</span>
          </div>

          <div class="form-group">
            <label class="form-label">
              <span>Formato de Visualización</span>
              <span class="required-star">*</span>
            </label>

            <div class="format-grid">
              <div
                :class="['format-card', { selected: form.formato === 1 }]"
                @click="setFormato(1)"
              >
                <div class="format-card-header">
                  <div class="format-radio">
                    <div v-if="form.formato === 1" class="format-radio-dot"></div>
                  </div>
                  <span class="format-card-title">Vertical Completo</span>
                </div>
                <p class="format-card-desc">
                  El afiche ocupa el 100% de la tarjeta visual (ideal afiches 4:5 con texto ya integrado en la gráfica).
                </p>
                <div class="format-preview-pill format-pill-green">Formato 1</div>
              </div>

              <div
                :class="['format-card', { selected: form.formato === 2 }]"
                @click="setFormato(2)"
              >
                <div class="format-card-header">
                  <div class="format-radio">
                    <div v-if="form.formato === 2" class="format-radio-dot"></div>
                  </div>
                  <span class="format-card-title">Bipartito</span>
                </div>
                <p class="format-card-desc">
                  Muestra la imagen a un lado y un panel editorial con descripción detallada al otro.
                </p>
                <div class="format-preview-pill format-pill-blue">Formato 2</div>
              </div>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">
              <span>Título del Afiche</span>
              <span class="required-star">*</span>
            </label>
            <input
              type="text"
              v-model="form.titulo"
              class="form-control"
              placeholder="Ej. Taller Clínico de Habilidades Médicas 2026"
              maxlength="200"
            />
            <span v-if="formErrors.titulo" class="field-error-msg">{{ formErrors.titulo }}</span>
          </div>

          <div class="form-group">
            <label class="form-label">
              <span>Descripción Editorial</span>
              <span v-if="form.formato === 2" class="required-star">*</span>
              <span v-else class="form-label-hint">(Solo aplicable en formato Bipartito)</span>
            </label>
            <textarea
              v-model="form.descripcion"
              class="form-control form-textarea"
              rows="3"
              placeholder="Describe el objetivo del comunicado, fecha límite de registro o detalles clave..."
              maxlength="1000"
              :disabled="form.formato === 1"
            ></textarea>
            <span v-if="formErrors.descripcion" class="field-error-msg">{{ formErrors.descripcion }}</span>
          </div>

          <div class="form-group">
            <label class="form-label">
              <span>Enlace Externo / Formulario</span>
              <span class="form-label-hint">(Opcional)</span>
            </label>
            <div class="input-with-icon">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#94A3B8" stroke-width="2" class="input-left-icon">
                <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
                <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
              </svg>
              <input
                type="url"
                v-model="form.enlaceUrl"
                class="form-control pl-icon"
                placeholder="https://medicina.urp.edu.pe/inscripcion"
                maxlength="500"
              />
            </div>
            <span class="field-help-text">Si se define, los estudiantes verán un botón de acción en el carrusel de novedades.</span>
          </div>

          <div class="modal-footer">
            <button
              type="button"
              class="btn-modal-cancel"
              @click="handleCloseCreateModal"
              :disabled="guardando"
            >
              Cancelar
            </button>
            <button
              type="submit"
              class="btn-modal-submit"
              :disabled="guardando"
            >
              <span v-if="guardando">Publicando...</span>
              <span v-else>Publicar Afiche</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <div :class="['modal-overlay', { active: showDeleteModal }]">
      <div class="modal-card modal-card-sm">
        <div class="modal-header">
          <h3 class="modal-title text-danger">Confirmar Eliminación</h3>
          <button
            type="button"
            class="modal-close-btn"
            @click="handleCancelDelete"
            :disabled="eliminando"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <div class="modal-body delete-body">
          <div class="delete-icon-wrap">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#DC2626" stroke-width="2">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
          </div>

          <p class="delete-lead-text">
            ¿Deseas eliminar permanentemente este afiche del carrusel de novedades?
          </p>

          <div v-if="slideToDelete" class="delete-slide-summary">
            <img
              :src="getImageUrl(slideToDelete.imagenUrl)"
              :alt="slideToDelete.titulo"
              class="delete-thumb"
            />
            <div class="delete-info">
              <span class="delete-title">{{ slideToDelete.titulo }}</span>
              <span class="delete-badge">
                Formato {{ slideToDelete.formato === 1 ? 'Vertical' : 'Bipartito' }}
              </span>
            </div>
          </div>

          <p class="delete-warning-note">
            Esta acción eliminará el archivo de imagen del servidor y liberará el cupo en el boletín.
          </p>

          <div class="modal-footer">
            <button
              type="button"
              class="btn-modal-cancel"
              @click="handleCancelDelete"
              :disabled="eliminando"
            >
              Cancelar
            </button>
            <button
              type="button"
              class="btn-modal-danger"
              @click="handleExecuteDelete"
              :disabled="eliminando"
            >
              <span v-if="eliminando">Eliminando...</span>
              <span v-else>Sí, Eliminar</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <div :class="['modal-overlay modal-overlay-lightbox', { active: showZoomModal }]" @click.self="handleCloseZoom">
      <div v-if="zoomSlide" class="modal-card modal-card-zoom">
        <button
          type="button"
          class="lightbox-close-btn"
          @click="handleCloseZoom"
          title="Cerrar vista previa"
        >
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <line x1="18" y1="6" x2="6" y2="18" />
            <line x1="6" y1="6" x2="18" y2="18" />
          </svg>
        </button>
        <img
          :src="getImageUrl(zoomSlide.imagenUrl)"
          :alt="zoomSlide.titulo"
          class="lightbox-image"
        />
      </div>
    </div>

    <div v-if="toast.show" :class="['toast-notification', `toast-${toast.type}`]">
      <div class="toast-content">
        <svg v-if="toast.type === 'success'" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#00A859" stroke-width="2.5">
          <polyline points="20 6 9 17 4 12" />
        </svg>
        <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#DC2626" stroke-width="2.5">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        <span>{{ toast.message }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.boletin-view {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  font-family: 'Inter', system-ui, sans-serif;
  color: #0F172A;
}

.header-section {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.header-title {
  font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif;
  font-size: 1.65rem;
  font-weight: 700;
  color: #0F172A;
  margin: 0;
  letter-spacing: -0.02em;
}

.header-subtitle {
  font-size: 0.88rem;
  color: #64748B;
  margin: 0.35rem 0 0 0;
  max-width: 680px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.quota-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 0.45rem 0.85rem;
  border-radius: 9999px;
  font-size: 0.82rem;
  font-weight: 600;
  border: 1px solid transparent;
}

.quota-badge-ok {
  background: #E8F5E9;
  color: #008744;
  border-color: #C8E6C9;
}

.quota-badge-full {
  background: #FEF3C7;
  color: #B45309;
  border-color: #FDE68A;
}

.quota-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: currentColor;
}

.btn-primary-add {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #008744;
  color: #FFFFFF;
  border: none;
  border-radius: 8px;
  padding: 0.55rem 1.15rem;
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.15s;
}

.btn-primary-add:hover {
  background: #007A3D;
  transform: translateY(-1px);
}

.table-container {
  background: #FFFFFF;
  border: 1px solid #E2E8F0;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.table-responsive {
  width: 100%;
  overflow-x: auto;
}

.boletin-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.boletin-table th {
  padding: 0.9rem 1rem;
  background: #F8FAFC;
  color: #475569;
  font-size: 0.82rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  border-bottom: 1px solid #E2E8F0;
  white-space: nowrap;
}

.boletin-table td {
  padding: 0.9rem 1rem;
  border-bottom: 1px solid #F1F5F9;
  font-size: 0.875rem;
  color: #0F172A;
  vertical-align: middle;
}

.boletin-table tr:hover td {
  background: #F8FAFC;
}

.boletin-table tr:last-child td {
  border-bottom: none;
}

.th-num, .td-num {
  width: 48px;
  text-align: center;
  color: #94A3B8;
  font-weight: 600;
}

.th-thumb, .td-thumb {
  width: 90px;
}

.th-title {
  min-width: 200px;
}

.th-format {
  width: 120px;
}

.th-desc {
  min-width: 240px;
}

.th-link {
  width: 110px;
}

.th-actions, .td-actions {
  width: 110px;
  text-align: right;
}

.thumb-box {
  border-radius: 6px;
  overflow: hidden;
  background: #F1F5F9;
  border: 1px solid #E2E8F0;
  cursor: pointer;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.thumb-box-vertical {
  width: 52px;
  height: 68px;
}

.thumb-box-bipartito {
  width: 72px;
  height: 48px;
}

.thumb-img {
  width: 100%;
  height: 100%;
  display: block;
}

.thumb-cover {
  object-fit: cover;
}

.thumb-contain {
  object-fit: contain;
  background: #0F172A;
}

.thumb-hover-overlay {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.thumb-box:hover .thumb-hover-overlay {
  opacity: 1;
}

.title-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.slide-title-text {
  font-weight: 600;
  color: #0F172A;
  line-height: 1.35;
}

.badge-enlace {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: #F1F5F9;
  color: #008744;
  font-size: 0.72rem;
  font-weight: 600;
  padding: 2px 7px;
  border-radius: 4px;
  width: fit-content;
}

.badge {
  display: inline-flex;
  align-items: center;
  padding: 0.3rem 0.65rem;
  border-radius: 6px;
  font-size: 0.76rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.badge-vertical {
  background: #E8F5E9;
  color: #008744;
  border: 1px solid #C8E6C9;
}

.badge-bipartito {
  background: #EFF6FF;
  color: #1D4ED8;
  border: 1px solid #BFDBFE;
}

.desc-content {
  color: #475569;
  font-size: 0.83rem;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.external-anchor {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: #008744;
  text-decoration: none;
  font-size: 0.82rem;
  font-weight: 600;
  transition: color 0.15s;
}

.external-anchor:hover {
  color: #007A3D;
  text-decoration: underline;
}

.text-muted {
  color: #94A3B8;
}

.action-btn-group {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 6px;
}

.btn-icon {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  border: 1px solid #E2E8F0;
  background: #FFFFFF;
  color: #475569;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-icon-zoom:hover {
  border-color: #008744;
  color: #008744;
  background: #E8F5E9;
}

.btn-icon-delete:hover {
  border-color: #EF4444;
  color: #DC2626;
  background: #FEE2E2;
}

.state-loading, .state-empty {
  padding: 3.5rem 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  color: #64748B;
  gap: 0.75rem;
}

.empty-icon-wrap {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: #F1F5F9;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.5rem;
}

.empty-title {
  font-size: 1.1rem;
  font-weight: 700;
  color: #0F172A;
  margin: 0;
}

.empty-text {
  font-size: 0.86rem;
  color: #64748B;
  max-width: 440px;
  margin: 0 0 0.5rem 0;
}

.spinner {
  width: 28px;
  height: 28px;
  border: 3px solid #E2E8F0;
  border-top-color: #008744;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(4px);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.2s ease;
}

.modal-overlay.active {
  opacity: 1;
  pointer-events: auto;
}

.modal-card {
  background: #FFFFFF;
  border-radius: 12px;
  max-width: 580px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  display: flex;
  flex-direction: column;
}

.modal-card-sm {
  max-width: 460px;
}

.modal-card-zoom {
  background: transparent;
  box-shadow: none;
  max-width: 90vw;
  max-height: 90vh;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: visible;
}

.modal-overlay-lightbox {
  background: rgba(15, 23, 42, 0.95);
  padding: 1.5rem;
}

.lightbox-image {
  max-width: 85vw;
  max-height: 85vh;
  object-fit: contain;
  border-radius: 8px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
  display: block;
}

.lightbox-close-btn {
  position: absolute;
  top: -16px;
  right: -16px;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #0F172A;
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background-color 0.15s, transform 0.15s;
  z-index: 20;
}

.lightbox-close-btn:hover {
  background: #020617;
  transform: scale(1.08);
}

.modal-header {
  padding: 1.15rem 1.4rem;
  border-bottom: 1px solid #E2E8F0;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  position: sticky;
  top: 0;
  background: #FFFFFF;
  z-index: 10;
}

.modal-title {
  font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif;
  font-size: 1.15rem;
  font-weight: 700;
  color: #0F172A;
  margin: 0;
}

.modal-subtitle {
  font-size: 0.8rem;
  color: #64748B;
  margin: 0.25rem 0 0 0;
}

.modal-close-btn {
  background: transparent;
  border: none;
  color: #94A3B8;
  cursor: pointer;
  padding: 4px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: color 0.15s, background-color 0.15s;
}

.modal-close-btn:hover {
  color: #0F172A;
  background: #F1F5F9;
}

.modal-body {
  padding: 1.4rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.form-label {
  font-size: 0.83rem;
  font-weight: 600;
  color: #334155;
  display: flex;
  align-items: center;
  gap: 4px;
}

.required-star {
  color: #DC2626;
  font-weight: 700;
}

.form-label-hint {
  font-size: 0.74rem;
  color: #94A3B8;
  font-weight: 400;
}

.upload-dropzone {
  border: 2px dashed #CBD5E1;
  border-radius: 10px;
  padding: 1.75rem 1rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  cursor: pointer;
  background: #F8FAFC;
  transition: border-color 0.2s, background-color 0.2s;
}

.upload-dropzone:hover {
  border-color: #008744;
  background: #F0FDF4;
}

.file-input-hidden {
  display: none;
}

.upload-icon-circle {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: #E8F5E9;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.6rem;
}

.upload-instructions {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.upload-primary-text {
  font-size: 0.85rem;
  font-weight: 600;
  color: #0F172A;
}

.upload-secondary-text {
  font-size: 0.75rem;
  color: #64748B;
}

.preview-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
}

.preview-thumb-wrap {
  width: 64px;
  height: 64px;
  border-radius: 6px;
  overflow: hidden;
  background: #0F172A;
  flex-shrink: 0;
}

.preview-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.preview-info {
  display: flex;
  flex-direction: column;
  gap: 3px;
  flex: 1;
  min-width: 0;
}

.preview-filename {
  font-size: 0.82rem;
  font-weight: 600;
  color: #0F172A;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.preview-filesize {
  font-size: 0.74rem;
  color: #64748B;
}

.preview-remove-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: transparent;
  border: none;
  color: #DC2626;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0;
  margin-top: 4px;
  width: fit-content;
}

.preview-remove-btn:hover {
  text-decoration: underline;
}

.format-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
}

.format-card {
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  padding: 0.85rem;
  background: #FFFFFF;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  transition: all 0.15s ease;
}

.format-card:hover {
  border-color: #94A3B8;
}

.format-card.selected {
  border-color: #008744;
  background: #F0FDF4;
  box-shadow: 0 0 0 1px #008744;
}

.format-card-header {
  display: flex;
  align-items: center;
  gap: 8px;
}

.format-radio {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  border: 2px solid #CBD5E1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.format-card.selected .format-radio {
  border-color: #008744;
}

.format-radio-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #008744;
}

.format-card-title {
  font-size: 0.84rem;
  font-weight: 600;
  color: #0F172A;
}

.format-card-desc {
  font-size: 0.73rem;
  color: #64748B;
  margin: 0;
  line-height: 1.35;
}

.format-preview-pill {
  font-size: 0.68rem;
  font-weight: 600;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: 4px;
  width: fit-content;
  margin-top: auto;
}

.format-pill-green {
  background: #E8F5E9;
  color: #008744;
}

.format-pill-blue {
  background: #EFF6FF;
  color: #1D4ED8;
}

.form-control {
  width: 100%;
  padding: 0.6rem 0.85rem;
  border: 1px solid #CBD5E1;
  border-radius: 8px;
  font-size: 0.85rem;
  color: #0F172A;
  background: #FFFFFF;
  outline: none;
  box-sizing: border-box;
  font-family: inherit;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-control:focus {
  border-color: #008744;
  box-shadow: 0 0 0 3px rgba(0, 135, 68, 0.12);
}

.form-control:disabled {
  background: #F1F5F9;
  color: #94A3B8;
  cursor: not-allowed;
}

.form-textarea {
  resize: vertical;
  min-height: 72px;
}

.input-with-icon {
  position: relative;
  display: flex;
  align-items: center;
}

.input-left-icon {
  position: absolute;
  left: 10px;
  pointer-events: none;
}

.pl-icon {
  padding-left: 2.2rem;
}

.field-error-msg {
  font-size: 0.74rem;
  color: #DC2626;
  font-weight: 500;
}

.field-help-text {
  font-size: 0.73rem;
  color: #94A3B8;
}

.modal-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 0.5rem;
  padding-top: 1rem;
  border-top: 1px solid #E2E8F0;
}

.btn-modal-cancel {
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  color: #475569;
  padding: 0.55rem 1.1rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s, border-color 0.15s;
}

.btn-modal-cancel:hover {
  background: #F8FAFC;
  border-color: #94A3B8;
  color: #0F172A;
}

.btn-modal-submit {
  background: #008744;
  border: none;
  color: #FFFFFF;
  padding: 0.55rem 1.25rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn-modal-submit:hover:not(:disabled) {
  background: #007A3D;
}

.btn-modal-submit:disabled, .btn-modal-cancel:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-modal-danger {
  background: #DC2626;
  border: none;
  color: #FFFFFF;
  padding: 0.55rem 1.25rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn-modal-danger:hover:not(:disabled) {
  background: #B91C1C;
}

.delete-body {
  text-align: center;
  align-items: center;
}

.delete-icon-wrap {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: #FEE2E2;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto;
}

.delete-lead-text {
  font-size: 0.95rem;
  font-weight: 600;
  color: #0F172A;
  margin: 0;
}

.delete-slide-summary {
  display: flex;
  align-items: center;
  gap: 12px;
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  padding: 0.75rem 1rem;
  width: 100%;
  box-sizing: border-box;
  text-align: left;
}

.delete-thumb {
  width: 48px;
  height: 48px;
  border-radius: 6px;
  object-fit: cover;
  flex-shrink: 0;
}

.delete-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow: hidden;
}

.delete-title {
  font-size: 0.85rem;
  font-weight: 600;
  color: #0F172A;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.delete-badge {
  font-size: 0.72rem;
  color: #64748B;
}

.delete-warning-note {
  font-size: 0.78rem;
  color: #64748B;
  margin: 0;
}

.toast-notification {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: #0F172A;
  color: #FFFFFF;
  padding: 0.75rem 1.25rem;
  border-radius: 8px;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.2);
  z-index: 2000;
  animation: toastFade 0.25s ease;
}

.toast-content {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
  font-weight: 500;
}

.toast-success {
  border-left: 4px solid #00A859;
}

.toast-error {
  border-left: 4px solid #DC2626;
}

@keyframes toastFade {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 768px) {
  .format-grid {
    grid-template-columns: 1fr;
  }
  .header-section {
    flex-direction: column;
    align-items: stretch;
  }
  .header-right {
    justify-content: space-between;
  }
}
</style>