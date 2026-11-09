import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import Dashboard from '../Dashboard.vue'

vi.mock('axios')
vi.mock('vue-chartjs', () => ({
  Line: { name: 'Line', template: '<div data-test="line-chart">Line</div>' },
  Bar: { name: 'Bar', template: '<div data-test="bar-chart">Bar</div>' },
  Doughnut: { name: 'Doughnut', template: '<div data-test="doughnut-chart">Doughnut</div>' }
}))

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

describe('Dashboard (Resumen Diario del Laboratorio)', () => {
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

  const mountDashboard = () => {
    return mount(Dashboard)
  }

  it('renderiza la cabecera del Resumen Diario con la fecha y estado del sistema', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    expect(wrapper.text()).toContain('Resumen Diario')
    expect(wrapper.text()).toContain('Control en tiempo real')
    expect(wrapper.find('input[type="date"]').exists()).toBe(true)
    expect(wrapper.find('.status-pill').exists()).toBe(true)
  })

  it('muestra las métricas clave de sesiones activas, ingresos de hoy y tiempo promedio', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    expect(wrapper.text()).toContain('Sesiones Activas')
    expect(wrapper.text()).toContain('5')
    expect(wrapper.text()).toContain('/ 20')
    expect(wrapper.text()).toContain('Ingresos Hoy')
    expect(wrapper.text()).toContain('18')
    expect(wrapper.text()).toContain('Tiempo Promedio')
    expect(wrapper.text()).toContain('45')
  })

  it('calcula y muestra correctamente el porcentaje de ocupación', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    // 5 sesiones activas de 20 estaciones = 25%
    expect(wrapper.text()).toContain('Ocupación')
    expect(wrapper.text()).toContain('25%')
  })

  it('muestra las estadísticas mensuales de escaneos de carnets', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    expect(wrapper.text()).toContain('Escaneos del Mes')
    expect(wrapper.text()).toContain('150')
    expect(wrapper.text()).toContain('/ 1000')
    expect(wrapper.text()).toContain('Faltan 20 días')
  })

  it('permite cambiar la fecha seleccionada y recarga las estadísticas', async () => {
    const wrapper = mountDashboard()
    await flushPromises()

    const initialCallCount = axios.get.mock.calls.length

    const dateInput = wrapper.find('input[type="date"]')
    await dateInput.setValue('2026-11-01')
    await dateInput.trigger('change')
    await flushPromises()

    expect(axios.get.mock.calls.length).toBeGreaterThan(initialCallCount)
    const dashboardCall = axios.get.mock.calls.find(call => call[0].includes('date=2026-11-01'))
    expect(dashboardCall).toBeTruthy()
  })
})
