import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import ResumenPrestamos from '../ResumenPrestamos.vue'

vi.mock('axios')
vi.mock('vue-chartjs', () => ({
  Line: { name: 'Line', template: '<div data-test="line-chart">Line</div>' },
  Doughnut: { name: 'Doughnut', template: '<div data-test="doughnut-chart">Doughnut</div>' }
}))

const mockDailyPrestamosData = {
  kpis: {
    totalPrestamosPeriodo: 8,
    totalDevueltosPeriodo: 5,
    totalActivos: 12,
    totalVencidos: 2,
    porVencerHoy: 1,
    tasaATiempo: 95,
    totalCatalogo: 350,
    librosPrestados: 12
  },
  tendenciaFlujo: [
    { etiqueta: '07:00', prestamos: 0, devoluciones: 0 },
    { etiqueta: '08:00', prestamos: 2, devoluciones: 1 },
    { etiqueta: '09:00', prestamos: 3, devoluciones: 2 }
  ],
  topLibros: [
    { libroID: 101, titulo: 'Harrison Principios de Medicina Interna', codigoBarras: 'MED-101', total: 4 },
    { libroID: 102, titulo: 'Robbins Patología Estructural y Funcional', codigoBarras: 'MED-102', total: 2 }
  ],
  porCategoria: [
    { categoria: 'Medicina Interna', cantidad: 5 },
    { categoria: 'Patología', cantidad: 3 }
  ],
  horasPunta: [
    { hora: '08:00', totalMovimientos: 3 },
    { hora: '09:00', totalMovimientos: 5 }
  ],
  cumplimiento: {
    devueltosATiempo: 5,
    devueltosConRetraso: 0,
    activosEnPlazo: 10,
    activosVencidos: 2
  },
  topAlumnos: [
    { alumnoID: 1, codigoUniversitario: '20211045', nombre: 'Carlos Sanchez', carrera: 'Medicina Humana', totalPrestamos: 2 }
  ],
  movimientos: [
    {
      tipo: 'Préstamo',
      hora: '08:30',
      libroTitulo: 'Harrison Principios de Medicina Interna',
      libroCodigo: 'MED-101',
      alumnoNombre: 'Carlos Sanchez',
      alumnoCodigo: '20211045',
      alumnoCarrera: 'Medicina Humana',
      fechaDevolucion: '2026-11-15T23:59:59',
      estado: 'Activo'
    },
    {
      tipo: 'Devolución',
      hora: '09:15',
      libroTitulo: 'Robbins Patología',
      libroCodigo: 'MED-102',
      alumnoNombre: 'Ana Lopez',
      alumnoCodigo: '20220912',
      alumnoCarrera: 'Medicina Humana',
      fechaDevolucion: '2026-11-09T23:59:59',
      estado: 'Devuelto'
    }
  ]
}

describe('ResumenPrestamos Component', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockImplementation((url) => {
      if (url.includes('/api/Prestamos/Estadisticas')) {
        return Promise.resolve({ data: mockDailyPrestamosData })
      }
      return Promise.resolve({ data: {} })
    })
  })

  it('renderiza la cabecera con el título de Resumen Diario de Préstamos y selector de fecha', async () => {
    const wrapper = mount(ResumenPrestamos)
    await flushPromises()

    expect(wrapper.text()).toContain('Resumen Diario de Préstamos')
    expect(wrapper.find('input[type="date"]').exists()).toBe(true)
    expect(wrapper.find('.status-pill').exists()).toBe(true)
  })

  it('muestra las métricas clave del día (préstamos, devoluciones, activos, vencidos, tasa a tiempo)', async () => {
    const wrapper = mount(ResumenPrestamos)
    await flushPromises()

    expect(wrapper.find('[data-test="kpi-prestamos-hoy"]').text()).toBe('8')
    expect(wrapper.find('[data-test="kpi-devoluciones-hoy"]').text()).toBe('5')
    expect(wrapper.find('[data-test="kpi-activos"]').text()).toContain('12')
    expect(wrapper.find('[data-test="kpi-vencidos"]').text()).toBe('2')
    expect(wrapper.find('[data-test="kpi-tasa"]').text()).toBe('95%')
  })

  it('muestra la tabla de movimientos registrados en el día', async () => {
    const wrapper = mount(ResumenPrestamos)
    await flushPromises()

    expect(wrapper.text()).toContain('Movimientos Registrados en la Fecha')
    expect(wrapper.text()).toContain('Harrison Principios de Medicina Interna')
    expect(wrapper.text()).toContain('Robbins Patología')
    expect(wrapper.text()).toContain('Carlos Sanchez')
    expect(wrapper.text()).toContain('Ana Lopez')
  })

  it('permite filtrar movimientos por texto de búsqueda', async () => {
    const wrapper = mount(ResumenPrestamos)
    await flushPromises()

    const searchInput = wrapper.find('.search-input')
    await searchInput.setValue('Harrison')
    await flushPromises()

    const tableText = wrapper.find('.styled-table').text()
    expect(tableText).toContain('Harrison Principios de Medicina Interna')
    expect(tableText).not.toContain('Robbins Patología')
  })

  it('permite alternar filtros de tipo (Solo Préstamos / Solo Devoluciones)', async () => {
    const wrapper = mount(ResumenPrestamos)
    await flushPromises()

    const buttons = wrapper.findAll('.type-filter-group .filter-btn')
    const btnPrestamos = buttons.find(b => b.text().includes('Solo Préstamos'))
    const btnDevoluciones = buttons.find(b => b.text().includes('Solo Devoluciones'))

    // Filtrar solo devoluciones
    await btnDevoluciones.trigger('click')
    await flushPromises()

    let tableText = wrapper.find('.styled-table').text()
    expect(tableText).toContain('Robbins Patología')
    expect(tableText).not.toContain('Harrison Principios de Medicina Interna')

    // Filtrar solo préstamos
    await btnPrestamos.trigger('click')
    await flushPromises()

    tableText = wrapper.find('.styled-table').text()
    expect(tableText).toContain('Harrison Principios de Medicina Interna')
    expect(tableText).not.toContain('Robbins Patología')
  })

  it('permite cambiar la fecha seleccionada y recarga los datos desde la API', async () => {
    const wrapper = mount(ResumenPrestamos)
    await flushPromises()

    const dateInput = wrapper.find('[data-test="date-picker"]')
    await dateInput.setValue('2026-11-05')
    await dateInput.trigger('change')
    await flushPromises()

    const calledUrl = axios.get.mock.calls[axios.get.mock.calls.length - 1][0]
    expect(calledUrl).toContain('date=2026-11-05')
    expect(calledUrl).toContain('mode=date')
  })
})
