import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import EstadisticasPrestamos from '../EstadisticasPrestamos.vue'

vi.mock('axios')
vi.mock('vue-chartjs', () => ({
  Line: { name: 'Line', template: '<div data-test="line-chart">Line</div>' },
  Bar: { name: 'Bar', template: '<div data-test="bar-chart">Bar</div>' },
  Doughnut: { name: 'Doughnut', template: '<div data-test="doughnut-chart">Doughnut</div>' }
}))

const mockStatsData = {
  kpis: {
    totalPrestamosPeriodo: 42,
    totalDevueltosPeriodo: 38,
    totalActivos: 14,
    totalVencidos: 3,
    porVencerHoy: 2,
    tasaATiempo: 92,
    totalCatalogo: 400,
    librosPrestados: 14
  },
  tendenciaFlujo: [
    { etiqueta: 'Día 1', prestamos: 2, devoluciones: 1 },
    { etiqueta: 'Día 2', prestamos: 5, devoluciones: 4 },
    { etiqueta: 'Día 3', prestamos: 4, devoluciones: 3 }
  ],
  topLibros: [
    { libroID: 101, titulo: 'Harrison Medicina Interna', codigoBarras: 'MED-101', total: 10 },
    { libroID: 102, titulo: 'Netter Atlas de Anatomía Humana', codigoBarras: 'MED-102', total: 8 }
  ],
  porCategoria: [
    { categoria: 'Anatomía', cantidad: 12 },
    { categoria: 'Medicina Interna', cantidad: 10 }
  ],
  horasPunta: [
    { hora: '09:00', totalMovimientos: 12 },
    { hora: '10:00', totalMovimientos: 15 }
  ],
  cumplimiento: {
    devueltosATiempo: 35,
    devueltosConRetraso: 3,
    activosEnPlazo: 11,
    activosVencidos: 3
  },
  topAlumnos: [
    { alumnoID: 1, codigoUniversitario: '20211045', nombre: 'Carlos Sanchez', carrera: 'Medicina Humana', totalPrestamos: 5 },
    { alumnoID: 2, codigoUniversitario: '20220199', nombre: 'Ana Gomez', carrera: 'Medicina Humana', totalPrestamos: 4 }
  ]
}

describe('EstadisticasPrestamos.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockResolvedValue({ data: JSON.parse(JSON.stringify(mockStatsData)) })
  })

  it('renders the header title and period filter pills (Semana, Mes, Año)', async () => {
    const wrapper = mount(EstadisticasPrestamos)
    await flushPromises()

    expect(wrapper.find('.main-title').text()).toContain('Estadísticas de Préstamos y Circulación')
    expect(wrapper.find('[data-test="btn-filter-week"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="btn-filter-month"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="btn-filter-year"]').exists()).toBe(true)
  })

  it('renders 5 KPI cards with the exact same styles as ResumenPrestamos (.stats-grid and .stat-card)', async () => {
    const wrapper = mount(EstadisticasPrestamos)
    await flushPromises()

    const statsGrid = wrapper.find('.stats-grid')
    expect(statsGrid.exists()).toBe(true)

    const cards = wrapper.findAll('.stat-card')
    expect(cards.length).toBe(5)

    // Verificar valores computados
    expect(wrapper.find('[data-test="kpi-prestamos-periodo"]').text()).toBe('42')
    expect(wrapper.find('[data-test="kpi-devoluciones-periodo"]').text()).toBe('38')
    expect(wrapper.find('[data-test="kpi-activos"]').text()).toContain('14')
    expect(wrapper.find('[data-test="kpi-activos"]').text()).toContain('/ 400')
    expect(wrapper.find('[data-test="kpi-vencidos"]').text()).toBe('3')
    expect(wrapper.find('[data-test="kpi-tasa"]').text()).toBe('92%')
  })

  it('switches to Semana mode and queries API with mode=week', async () => {
    const wrapper = mount(EstadisticasPrestamos)
    await flushPromises()

    const btnWeek = wrapper.find('[data-test="btn-filter-week"]')
    await btnWeek.trigger('click')
    await flushPromises()

    expect(wrapper.vm.filterMode).toBe('week')
    // Verificar que se haya llamado con mode=week
    expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('mode=week'))

    // Verificar que el label de la card cambia a Préstamos Semana
    const cards = wrapper.findAll('.stat-card')
    expect(cards[0].find('.stat-label').text()).toBe('Préstamos Semana')
    expect(cards[1].find('.stat-label').text()).toBe('Devoluciones Semana')
  })

  it('switches to Año mode and queries API with mode=year', async () => {
    const wrapper = mount(EstadisticasPrestamos)
    await flushPromises()

    const btnYear = wrapper.find('[data-test="btn-filter-year"]')
    await btnYear.trigger('click')
    await flushPromises()

    expect(wrapper.vm.filterMode).toBe('year')
    expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('mode=year'))

    const cards = wrapper.findAll('.stat-card')
    expect(cards[0].find('.stat-label').text()).toBe('Préstamos Año')
    expect(cards[1].find('.stat-label').text()).toBe('Devoluciones Año')
  })

  it('allows period navigation with previous and next buttons', async () => {
    const wrapper = mount(EstadisticasPrestamos)
    await flushPromises()

    const prevMonth = wrapper.vm.selectedMonth
    const btnPrev = wrapper.find('[data-test="btn-period-prev"]')
    await btnPrev.trigger('click')
    await flushPromises()

    // El mes anterior debe ser diferente
    expect(wrapper.vm.selectedMonth).not.toBe(prevMonth)
    expect(axios.get).toHaveBeenCalled()
  })

  it('reloads stats when clicking the refresh button', async () => {
    const wrapper = mount(EstadisticasPrestamos)
    await flushPromises()

    const initialCalls = axios.get.mock.calls.length
    const btnRefresh = wrapper.find('[data-test="btn-refresh-stats"]')
    await btnRefresh.trigger('click')
    await flushPromises()

    expect(axios.get.mock.calls.length).toBeGreaterThan(initialCalls)
  })
})
