import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import ActiveSessions from '../ActiveSessions.vue'

vi.mock('axios')

describe('ActiveSessions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders title and empty state when there are no active sessions', async () => {
    axios.get.mockResolvedValueOnce({ data: [] })

    const wrapper = mount(ActiveSessions)
    await flushPromises()

    expect(wrapper.text()).toContain('Estudiantes en Línea')
    expect(wrapper.text()).toContain('No hay sesiones activas en este momento.')
  })

  it('renders list of active sessions with student and computer details', async () => {
    axios.get.mockResolvedValueOnce({
      data: [
        {
          sesionID: 101,
          equipo: 'PC-LAB-01',
          alias: 'Fila A-01',
          alumno: 'Juan Perez Gomez',
          horaInicio: '2026-03-10T14:30:00Z',
          limiteDiarioSegundos: 10800
        },
        {
          sesionID: 102,
          equipo: 'PC-LAB-02',
          alias: null,
          alumno: 'Maria Rodriguez',
          horaInicio: '2026-03-10T15:00:00Z',
          limiteDiarioSegundos: 21600
        }
      ]
    })

    const wrapper = mount(ActiveSessions)
    await flushPromises()

    expect(wrapper.text()).toContain('PC-LAB-01')
    expect(wrapper.text()).toContain('Fila A-01')
    expect(wrapper.text()).toContain('Juan Perez Gomez')
    expect(wrapper.text()).toContain('PC-LAB-02')
    expect(wrapper.text()).toContain('Maria Rodriguez')
    expect(wrapper.findAll('tbody tr')).toHaveLength(2)
  })
})
