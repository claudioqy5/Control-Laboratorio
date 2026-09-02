<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'

const props = defineProps({
  initialMonth: {
    type: Number,
    default: () => new Date().getMonth() + 1
  },
  initialYear: {
    type: Number,
    default: () => new Date().getFullYear()
  },
  initialDate: {
    type: String,
    default: () => {
      const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' }
      return new Intl.DateTimeFormat('en-CA', options).format(new Date())
    }
  },
  initialMode: {
    type: String,
    default: 'month' // 'month' | 'date' | 'year'
  }
})

const emit = defineEmits(['update:month', 'update:year', 'update:date', 'update:mode', 'change'])

// Nombres de meses
const months = [
  { value: 1, name: 'Enero', short: 'Ene' },
  { value: 2, name: 'Febrero', short: 'Feb' },
  { value: 3, name: 'Marzo', short: 'Mar' },
  { value: 4, name: 'Abril', short: 'Abr' },
  { value: 5, name: 'Mayo', short: 'May' },
  { value: 6, name: 'Junio', short: 'Jun' },
  { value: 7, name: 'Julio', short: 'Jul' },
  { value: 8, name: 'Agosto', short: 'Ago' },
  { value: 9, name: 'Septiembre', short: 'Set' },
  { value: 10, name: 'Octubre', short: 'Oct' },
  { value: 11, name: 'Noviembre', short: 'Nov' },
  { value: 12, name: 'Diciembre', short: 'Dic' }
]

// Rango de años
const startYear = 2024
const currentSysYear = new Date().getFullYear()
const years = Array.from({ length: Math.max(5, currentSysYear - startYear + 3) }, (_, i) => startYear + i)

// Estado del popover
const isOpen = ref(false)
const popoverRef = ref(null)
const triggerRef = ref(null)

// Nivel activo en el selector: 'year' | 'month' | 'day'
const currentStep = ref('month')

// Valores seleccionados (temporales durante la selección en el popover)
const tempYear = ref(props.initialYear)
const tempMonth = ref(props.initialMonth)
const tempDay = ref(props.initialDate ? parseInt(props.initialDate.split('-')[2], 10) || 1 : 1)
const tempMode = ref(props.initialMode)

// Valores confirmados (los que están actualmente activos y emitidos)
const activeYear = ref(props.initialYear)
const activeMonth = ref(props.initialMonth)
const activeDate = ref(props.initialDate)
const activeMode = ref(props.initialMode)

// Generar días del mes para el calendario (tempYear, tempMonth)
const calendarDays = computed(() => {
  const y = tempYear.value
  const m = tempMonth.value
  const firstDay = new Date(y, m - 1, 1).getDay() // 0 = Dom, 1 = Lun...
  const startingCol = (firstDay === 0 ? 6 : firstDay - 1) // 0 = Lun ... 6 = Dom
  const daysInMonth = new Date(y, m, 0).getDate()
  
  const days = []
  // Días vacíos previos
  for (let i = 0; i < startingCol; i++) {
    days.push({ day: null, isCurrentMonth: false })
  }
  // Días del mes
  for (let d = 1; d <= daysInMonth; d++) {
    days.push({ day: d, isCurrentMonth: true })
  }
  return days
})

// Etiqueta legible del filtro activo para el botón principal
const activeDisplayLabel = computed(() => {
  if (activeMode.value === 'year') {
    return `Año ${activeYear.value}`
  }
  const mName = months.find(x => x.value === activeMonth.value)?.name || ''
  if (activeMode.value === 'date') {
    if (!activeDate.value) return 'Fecha exacta'
    const [, , d] = activeDate.value.split('-')
    return `${d} ${mName} ${activeYear.value}`
  }
  return `${mName} ${activeYear.value}`
})

// Acciones de selección dentro del calendario
const selectYear = (y) => {
  tempYear.value = y
  // No avanzar automáticamente; se avanza con 'Elegir Mes →' o con la barra superior
}

const selectMonth = (m) => {
  tempMonth.value = m
  // No avanzar automáticamente; se avanza con 'Elegir Día Exacto →' o con la barra superior
}

const selectDay = (d) => {
  tempDay.value = d
  tempMode.value = 'date'
}

// Botones de aplicación ("Aceptar")
const applyYear = () => {
  activeMode.value = 'year'
  activeYear.value = tempYear.value
  activeMonth.value = tempMonth.value
  activeDate.value = `${tempYear.value}-01-01`
  emitState()
  isOpen.value = false
}

const applyMonth = () => {
  activeMode.value = 'month'
  activeYear.value = tempYear.value
  activeMonth.value = tempMonth.value
  const dStr = tempDay.value.toString().padStart(2, '0')
  const mStr = tempMonth.value.toString().padStart(2, '0')
  activeDate.value = `${tempYear.value}-${mStr}-${dStr}`
  emitState()
  isOpen.value = false
}

const applyDate = () => {
  activeMode.value = 'date'
  activeYear.value = tempYear.value
  activeMonth.value = tempMonth.value
  const dStr = tempDay.value.toString().padStart(2, '0')
  const mStr = tempMonth.value.toString().padStart(2, '0')
  activeDate.value = `${tempYear.value}-${mStr}-${dStr}`
  emitState()
  isOpen.value = false
}

const goToToday = () => {
  const options = { timeZone: 'America/Lima', year: 'numeric', month: '2-digit', day: '2-digit' }
  const todayStr = new Intl.DateTimeFormat('en-CA', options).format(new Date())
  const [y, m, d] = todayStr.split('-').map(Number)

  tempYear.value = y
  tempMonth.value = m
  tempDay.value = d
  tempMode.value = 'date'

  activeYear.value = y
  activeMonth.value = m
  activeDate.value = todayStr
  activeMode.value = 'date'

  emitState()
  isOpen.value = false
}

const emitState = () => {
  emit('update:year', activeYear.value)
  emit('update:month', activeMonth.value)
  emit('update:date', activeDate.value)
  emit('update:mode', activeMode.value)
  emit('change', {
    mode: activeMode.value,
    year: activeYear.value,
    month: activeMonth.value,
    date: activeDate.value,
    label: activeDisplayLabel.value
  })
}

// Control de apertura y cierre
const togglePopover = () => {
  if (!isOpen.value) {
    tempYear.value = activeYear.value
    tempMonth.value = activeMonth.value
    if (activeDate.value) {
      tempDay.value = parseInt(activeDate.value.split('-')[2], 10) || 1
    }
    // Abrir en la vista según el modo activo
    currentStep.value = activeMode.value === 'year' ? 'year' : (activeMode.value === 'date' ? 'day' : 'month')
  }
  isOpen.value = !isOpen.value
}

// Cerrar al hacer clic fuera
const handleClickOutside = (e) => {
  if (isOpen.value && popoverRef.value && !popoverRef.value.contains(e.target) && !triggerRef.value.contains(e.target)) {
    isOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside)
  emitState()
})

onUnmounted(() => {
  document.removeEventListener('mousedown', handleClickOutside)
})
</script>

<template>
  <div class="calendar-filter-wrapper">
    <!-- Botón Principal Compacto (Trigger) -->
    <button 
      ref="triggerRef"
      type="button" 
      class="filter-trigger-btn"
      :class="{ 'is-open': isOpen }"
      @click="togglePopover"
      title="Filtrar estadísticas por año, mes o fecha exacta"
    >
      <div class="trigger-icon-box">
        <svg v-if="activeMode === 'year'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"></circle>
          <polyline points="12 6 12 12 16 14"></polyline>
        </svg>
        <svg v-else-if="activeMode === 'date'" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
          <line x1="16" y1="2" x2="16" y2="6"></line>
          <line x1="8" y1="2" x2="8" y2="6"></line>
          <line x1="3" y1="10" x2="21" y2="10"></line>
        </svg>
        <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
          <circle cx="12" cy="10" r="3"></circle>
        </svg>
      </div>

      <div class="trigger-content">
        <span class="trigger-mode-badge">{{ activeMode === 'year' ? 'Año' : (activeMode === 'date' ? 'Día' : 'Mes') }}</span>
        <span class="trigger-label">{{ activeDisplayLabel }}</span>
      </div>

      <svg class="trigger-chevron" :class="{ rotated: isOpen }" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
        <polyline points="6 9 12 15 18 9"></polyline>
      </svg>
    </button>

    <!-- Popover Flotante Tipo Calendario -->
    <div v-if="isOpen" ref="popoverRef" class="calendar-popover">
      <!-- Breadcrumb / Pasos de selección -->
      <div class="popover-header">
        <div class="stepper-nav">
          <button 
            type="button" 
            class="step-nav-btn" 
            :class="{ active: currentStep === 'year' }" 
            @click="currentStep = 'year'"
          >
            {{ tempYear }}
          </button>
          <span class="step-separator">›</span>
          <button 
            type="button" 
            class="step-nav-btn" 
            :class="{ active: currentStep === 'month' }" 
            @click="currentStep = 'month'"
          >
            {{ months.find(x => x.value === tempMonth)?.name || 'Mes' }}
          </button>
          <span class="step-separator">›</span>
          <button 
            type="button" 
            class="step-nav-btn" 
            :class="{ active: currentStep === 'day' }" 
            @click="currentStep = 'day'"
          >
            Día {{ tempDay }}
          </button>
        </div>

        <button type="button" class="btn-quick-today" @click="goToToday" title="Seleccionar hoy">
          Hoy
        </button>
      </div>

      <!-- VISTA 1: SELECCIONAR AÑO -->
      <div v-if="currentStep === 'year'" class="step-content">
        <div class="step-instruction">
          <span>Selecciona un Año</span>
          <span class="step-hint">o aplica para ver todo el año</span>
        </div>
        <div class="years-grid">
          <button 
            v-for="y in years" 
            :key="y" 
            type="button" 
            class="year-chip" 
            :class="{ selected: tempYear === y }"
            @click="selectYear(y)"
          >
            {{ y }}
          </button>
        </div>
        <div class="step-actions">
          <button type="button" class="btn-primary" @click="applyYear">
            ✓ Aceptar Todo el Año {{ tempYear }}
          </button>
          <button type="button" class="btn-secondary" @click="currentStep = 'month'">
            Elegir Mes →
          </button>
        </div>
      </div>

      <!-- VISTA 2: SELECCIONAR MES -->
      <div v-else-if="currentStep === 'month'" class="step-content">
        <div class="step-instruction">
          <div class="nav-row">
            <button type="button" class="nav-arrow" @click="tempYear--">‹</button>
            <span class="step-title-bold">{{ tempYear }}</span>
            <button type="button" class="nav-arrow" @click="tempYear++">›</button>
          </div>
          <span class="step-hint">Selecciona un mes</span>
        </div>
        <div class="months-grid">
          <button 
            v-for="m in months" 
            :key="m.value" 
            type="button" 
            class="month-chip" 
            :class="{ selected: tempMonth === m.value }"
            @click="selectMonth(m.value)"
          >
            {{ m.short }}
          </button>
        </div>
        <div class="step-actions">
          <button type="button" class="btn-primary" @click="applyMonth">
            ✓ Aceptar Mes ({{ months.find(x => x.value === tempMonth)?.name }} {{ tempYear }})
          </button>
          <button type="button" class="btn-secondary" @click="currentStep = 'day'">
            Elegir Día Exacto →
          </button>
        </div>
      </div>

      <!-- VISTA 3: SELECCIONAR FECHA EXACTA (DÍA) -->
      <div v-else-if="currentStep === 'day'" class="step-content">
        <div class="step-instruction">
          <div class="nav-row">
            <button type="button" class="nav-arrow" @click="tempMonth > 1 ? tempMonth-- : (tempMonth = 12, tempYear--)">‹</button>
            <span class="step-title-bold">{{ months.find(x => x.value === tempMonth)?.name }} {{ tempYear }}</span>
            <button type="button" class="nav-arrow" @click="tempMonth < 12 ? tempMonth++ : (tempMonth = 1, tempYear++)">›</button>
          </div>
          <span class="step-hint">Selecciona un día del calendario</span>
        </div>

        <div class="calendar-table">
          <div class="weekdays-row">
            <span>Lu</span><span>Ma</span><span>Mi</span><span>Ju</span><span>Vi</span><span>Sá</span><span>Do</span>
          </div>
          <div class="days-grid">
            <div 
              v-for="(item, idx) in calendarDays" 
              :key="idx" 
              class="day-cell-wrapper"
            >
              <button 
                v-if="item.isCurrentMonth"
                type="button" 
                class="day-cell"
                :class="{ selected: tempDay === item.day }"
                @click="selectDay(item.day)"
              >
                {{ item.day }}
              </button>
              <span v-else class="day-cell empty"></span>
            </div>
          </div>
        </div>

        <div class="step-actions">
          <button type="button" class="btn-primary" @click="applyDate">
            ✓ Aceptar Fecha ({{ tempDay }} de {{ months.find(x => x.value === tempMonth)?.name }})
          </button>
          <button type="button" class="btn-secondary" @click="applyMonth">
            Aceptar Mes Entero
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.calendar-filter-wrapper {
  position: relative;
  display: inline-block;
  font-family: inherit;
}

/* Trigger Button */
.filter-trigger-btn {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  padding: 6px 14px 6px 10px;
  border-radius: 10px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: all 0.2s ease;
  user-select: none;
}

.filter-trigger-btn:hover {
  border-color: #d1d5db;
  box-shadow: 0 3px 6px -1px rgba(0, 0, 0, 0.07);
}

.filter-trigger-btn.is-open {
  border-color: #9f1239;
  box-shadow: 0 0 0 2px rgba(159, 18, 57, 0.12);
}

.trigger-icon-box {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  background: #fdf2f4;
  color: #9f1239;
  display: flex;
  align-items: center;
  justify-content: center;
}

.trigger-content {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  line-height: 1.15;
}

.trigger-mode-badge {
  font-size: 0.65rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #9f1239;
}

.trigger-label {
  font-size: 0.88rem;
  font-weight: 700;
  color: #111827;
}

.trigger-chevron {
  color: #6b7280;
  transition: transform 0.2s ease;
}

.trigger-chevron.rotated {
  transform: rotate(180deg);
}

/* Popover Flotante */
.calendar-popover {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 320px;
  background: #ffffff;
  border-radius: 14px;
  border: 1px solid #e5e7eb;
  box-shadow: 0 14px 30px -4px rgba(0, 0, 0, 0.12), 0 6px 12px -3px rgba(0, 0, 0, 0.08);
  padding: 14px;
  z-index: 1000;
  animation: popoverFadeIn 0.18s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes popoverFadeIn {
  from {
    opacity: 0;
    transform: translateY(-6px) scale(0.98);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Header del Popover */
.popover-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #f3f4f6;
  padding-bottom: 10px;
  margin-bottom: 12px;
}

.stepper-nav {
  display: flex;
  align-items: center;
  gap: 4px;
}

.step-nav-btn {
  background: transparent;
  border: none;
  font-size: 0.78rem;
  font-weight: 600;
  color: #6b7280;
  padding: 3px 6px;
  border-radius: 5px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.step-nav-btn:hover {
  color: #111827;
  background: #f3f4f6;
}

.step-nav-btn.active {
  color: #9f1239;
  font-weight: 700;
  background: #fdf2f4;
}

.step-separator {
  color: #9ca3af;
  font-size: 0.75rem;
}

.btn-quick-today {
  border: 1px solid #e5e7eb;
  background: #f9fafb;
  color: #374151;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-quick-today:hover {
  background: #9f1239;
  color: #ffffff;
  border-color: #9f1239;
}

/* Contenido de Pasos */
.step-content {
  display: flex;
  flex-direction: column;
}

.step-instruction {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 12px;
}

.nav-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.nav-arrow {
  background: #f3f4f6;
  border: none;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  font-size: 1rem;
  font-weight: bold;
  color: #4b5563;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
}

.nav-arrow:hover {
  background: #e5e7eb;
  color: #111827;
}

.step-title-bold {
  font-size: 0.95rem;
  font-weight: 800;
  color: #111827;
}

.step-hint {
  font-size: 0.72rem;
  color: #6b7280;
  margin-top: 2px;
}

/* Grillas */
.years-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
  margin-bottom: 14px;
}

.year-chip {
  padding: 10px 0;
  border: 1px solid #e5e7eb;
  background: #f9fafb;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 700;
  color: #374151;
  cursor: pointer;
  transition: all 0.15s;
}

.year-chip:hover {
  background: #ffffff;
  border-color: #d1d5db;
  color: #111827;
}

.year-chip.selected {
  background: #9f1239;
  color: #ffffff;
  border-color: #9f1239;
  box-shadow: 0 2px 4px rgba(159, 18, 57, 0.25);
}

.months-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 6px;
  margin-bottom: 14px;
}

.month-chip {
  padding: 9px 0;
  border: 1px solid #e5e7eb;
  background: #f9fafb;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 700;
  color: #374151;
  cursor: pointer;
  transition: all 0.15s;
}

.month-chip:hover {
  background: #ffffff;
  border-color: #d1d5db;
  color: #111827;
}

.month-chip.selected {
  background: #9f1239;
  color: #ffffff;
  border-color: #9f1239;
  box-shadow: 0 2px 4px rgba(159, 18, 57, 0.25);
}

/* Tabla de Calendario Diario */
.calendar-table {
  margin-bottom: 14px;
}

.weekdays-row {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  text-align: center;
  font-size: 0.7rem;
  font-weight: 700;
  color: #9ca3af;
  margin-bottom: 6px;
}

.days-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 4px;
}

.day-cell-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
}

.day-cell {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
  background: transparent;
  border-radius: 50%;
  font-size: 0.8rem;
  font-weight: 600;
  color: #374151;
  cursor: pointer;
  transition: all 0.15s;
}

.day-cell:hover {
  background: #f3f4f6;
  color: #111827;
}

.day-cell.selected {
  background: #9f1239;
  color: #ffffff;
  font-weight: 700;
  box-shadow: 0 2px 5px rgba(159, 18, 57, 0.3);
}

.day-cell.empty {
  pointer-events: none;
}

/* Botones de acción */
.step-actions {
  display: flex;
  flex-direction: column;
  gap: 6px;
  border-top: 1px solid #f3f4f6;
  padding-top: 10px;
}

.btn-primary {
  background: #9f1239;
  color: #ffffff;
  border: none;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-primary:hover {
  background: #881337;
  box-shadow: 0 2px 4px rgba(159, 18, 57, 0.25);
}

.btn-secondary {
  background: #f3f4f6;
  color: #374151;
  border: 1px solid #e5e7eb;
  padding: 7px 12px;
  border-radius: 8px;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-secondary:hover {
  background: #e5e7eb;
  color: #111827;
}
</style>
