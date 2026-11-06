import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import PrestamosCRUD from '../PrestamosCRUD.vue'

vi.mock('axios')

describe('PrestamosCRUD', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders loans list with student and book info', async () => {
    axios.get.mockResolvedValueOnce({
      data: [
        {
          prestamoID: 1,
          alumnoID: 10,
          alumnoCodigo: '202110452',
          alumnoNombre: 'Juan Perez Gomez',
          alumnoCarrera: 'Medicina Humana',
          libroID: 5,
          libroCodigoBarras: 'MED-009842',
          libroTitulo: 'Principios de Medicina Interna de Harrison',
          libroUbicacion: 'Estante 3-A P2',
          fechaPrestamo: '2026-11-06T10:00:00',
          fechaDevolucion: '2026-11-09T18:00:00',
          fechaEntregado: null,
          estado: 'Activo',
          esVencido: false,
          diasRestantes: 3
        }
      ]
    })

    const wrapper = mount(PrestamosCRUD)
    await flushPromises()

    expect(wrapper.text()).toContain('Control de Préstamos')
    expect(wrapper.text()).toContain('Juan Perez Gomez')
    expect(wrapper.text()).toContain('202110452')
    expect(wrapper.text()).toContain('Principios de Medicina Interna de Harrison')
    expect(wrapper.text()).toContain('MED-009842')
    expect(wrapper.text()).toContain('Activo')
  })

  it('renders empty state when there are no loans', async () => {
    axios.get.mockResolvedValueOnce({ data: [] })

    const wrapper = mount(PrestamosCRUD)
    await flushPromises()

    expect(wrapper.text()).toContain('No se encontraron registros de préstamos')
  })
})

