import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import Dashboard from '../Dashboard.vue'

vi.mock('axios')

const mockDashboardData = {
  sesionesActivas: 5,
  totalEstaciones: 20,
  sesionesHoy: 18,
  tiempoPromedioMinutos: 45,
  afluenciaPorHora: [0, 1, 2, 3, 2, 1, 0],
  afluenciaPorDia: [
    { dia: 'Lun', cantidad: 10, sesiones: [] },
    { dia: 'Mar', cantidad: 15, sesiones: [] }
  ],
  distribucionCarrera: [{ carrera: 'Medicina Humana', cantidad: 10 }],
  topAlumnos: [{ nombreCompleto: 'Juan Perez', totalMinutos: 120 }],
  topEquipos: [{ alias: 'PC-01', nombreRed: 'PC01', totalMinutos: 300 }]
}

describe('Dashboard (Resumen Unificado)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockImplementation((url) => {
      if (url.includes('/api/stats/dashboard')) {
        return Promise.resolve({ data: mockDashboardData })
      }
      if (url.includes('/api/reportes/escaneos-stats')) {
        return Promise.resolve({
          data: { escaneosEsteMes: 150, limiteMensual: 1000, limiteSeguridad: 950, diasRestantes: 20 }
        })
      }
      return Promise.resolve({ data: {} })
    })
  })

  const mountDashboard = (props = {}) => {
    return mount(Dashboard, {
      props,
      global: {
        stubs: {
          EstadisticasPrestamos: {
            name: 'EstadisticasPrestamos',
            props: ['hideMainHeader'],
            template: '<div data-test="stub-estadisticas-prestamos">Componente Estadisticas Prestamos</div>'
          },
          Line: true,
          Bar: true,
          Doughnut: true
        }
      }
    })
  }

  it('renderiza la cabecera de Resumen con los botones de Estadísticas de Préstamos y Computadoras', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    expect(wrapper.find('.resumen-main-title').text()).toBe('Resumen')
    const tabPrestamos = wrapper.find('[data-test="tab-prestamos"]')
    const tabComputadoras = wrapper.find('[data-test="tab-computadoras"]')

    expect(tabPrestamos.exists()).toBe(true)
    expect(tabComputadoras.exists()).toBe(true)
    expect(tabPrestamos.text()).toContain('Estadísticas de Préstamos')
    expect(tabComputadoras.text()).toContain('Estadísticas de Computadoras')
  })

  it('muestra por defecto las Estadísticas de Préstamos', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    const tabPrestamos = wrapper.find('[data-test="tab-prestamos"]')
    const tabComputadoras = wrapper.find('[data-test="tab-computadoras"]')

    expect(tabPrestamos.classes()).toContain('active')
    expect(tabComputadoras.classes()).not.toContain('active')

    const viewPrestamos = wrapper.find('[data-test="view-prestamos"]')
    const viewComputadoras = wrapper.find('[data-test="view-computadoras"]')

    expect(viewPrestamos.isVisible()).toBe(true)
    expect(viewComputadoras.isVisible()).toBe(false)
    expect(wrapper.find('[data-test="stub-estadisticas-prestamos"]').exists()).toBe(true)
  })

  it('cambia a Estadísticas de Computadoras al hacer clic en el botón de computadoras', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    const tabComputadoras = wrapper.find('[data-test="tab-computadoras"]')
    await tabComputadoras.trigger('click')

    const tabPrestamos = wrapper.find('[data-test="tab-prestamos"]')
    expect(tabComputadoras.classes()).toContain('active')
    expect(tabPrestamos.classes()).not.toContain('active')

    const viewPrestamos = wrapper.find('[data-test="view-prestamos"]')
    const viewComputadoras = wrapper.find('[data-test="view-computadoras"]')

    expect(viewPrestamos.isVisible()).toBe(false)
    expect(viewComputadoras.isVisible()).toBe(true)
    expect(wrapper.text()).toContain('Laboratorio de Cómputo')
  })

  it('permite alternar de regreso a Estadísticas de Préstamos con un clic', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    // Cambiar a computadoras
    await wrapper.find('[data-test="tab-computadoras"]').trigger('click')
    expect(wrapper.find('[data-test="tab-computadoras"]').classes()).toContain('active')

    // Volver a préstamos
    await wrapper.find('[data-test="tab-prestamos"]').trigger('click')
    expect(wrapper.find('[data-test="tab-prestamos"]').classes()).toContain('active')
    expect(wrapper.find('[data-test="view-prestamos"]').isVisible()).toBe(true)
    expect(wrapper.find('[data-test="view-computadoras"]').isVisible()).toBe(false)
  })

  it('respeta la prop initialTab cuando se especifica computadoras', async () => {
    const wrapper = mountDashboard({ initialTab: 'computadoras' })
    await flushPromises()

    expect(wrapper.find('[data-test="tab-computadoras"]').classes()).toContain('active')
    expect(wrapper.find('[data-test="view-computadoras"]').isVisible()).toBe(true)
    expect(wrapper.find('[data-test="view-prestamos"]').isVisible()).toBe(false)
  })
})
