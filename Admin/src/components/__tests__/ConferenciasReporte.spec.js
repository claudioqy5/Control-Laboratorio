import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import ConferenciasReporte from '../ConferenciasReporte.vue'

vi.mock('axios')

const mockConferences = [
  {
    idConferencia: 1,
    tituloEvento: 'Avances en Cardiología 2026',
    expositorPonente: 'Dr. Roberto Mendoza',
    entidadEditorial: 'Sociedad Peruana de Cardiología',
    modalidad: 'Presencial',
    ubicacion_o_Enlace: 'Auditorio Mayor',
    fechaHoraInicio: '2026-11-15T09:00:00Z',
    fechaHoraFin: '2026-11-15T11:00:00Z',
    asistenciaAbierta: true,
    estadoEvento: 'En Vivo',
    autoPurgar30Dias: true
  },
  {
    idConferencia: 2,
    tituloEvento: 'Simposio de Neurología Clínica',
    expositorPonente: 'Dra. Elena Silva',
    entidadEditorial: 'FAMURP Conferencias',
    modalidad: 'Virtual',
    ubicacion_o_Enlace: 'Zoom Meeting',
    fechaHoraInicio: '2026-10-10T15:00:00Z',
    fechaHoraFin: '2026-10-10T17:00:00Z',
    asistenciaAbierta: false,
    estadoEvento: 'Finalizada',
    autoPurgar30Dias: false
  }
]

const mockReport = {
  kpis: {
    totalPreInscritos: 15,
    acreditados: 12,
    inasistencias: 3,
    espontaneos: 2,
    totalAsistentesReales: 14,
    tasaAsistencia: 80.0
  },
  participantes: [
    {
      idParticipacion: 101,
      idConferencia: 1,
      tipoParticipante: 'Estudiante',
      tipoDocumento: 'DNI',
      numeroDocumento: '71234567',
      codigoURP: '20211045',
      nombres: 'Carlos',
      apellidoPaterno: 'Sanchez',
      apellidoMaterno: 'Perez',
      correo: '20211045@urp.edu.pe',
      ciclo: 'X',
      fechaHoraInscripcion: '2026-11-10T08:00:00Z',
      fechaHoraAsistencia: '2026-11-15T09:15:00Z',
      estadoParticipacion: 2,
      alumnoID: 55
    },
    {
      idParticipacion: 102,
      idConferencia: 1,
      tipoParticipante: 'Externo',
      tipoDocumento: 'DNI',
      numeroDocumento: '44556677',
      codigoURP: null,
      nombres: 'María',
      apellidoPaterno: 'Lopez',
      apellidoMaterno: 'Rios',
      correo: 'mlopez@gmail.com',
      ciclo: null,
      fechaHoraInscripcion: '2026-11-15T09:20:00Z',
      fechaHoraAsistencia: '2026-11-15T09:20:00Z',
      estadoParticipacion: 2,
      alumnoID: null
    },
    {
      idParticipacion: 103,
      idConferencia: 1,
      tipoParticipante: 'Estudiante',
      tipoDocumento: 'DNI',
      numeroDocumento: '72334455',
      codigoURP: '20220199',
      nombres: 'Jorge',
      apellidoPaterno: 'Diaz',
      apellidoMaterno: 'Vega',
      correo: '20220199@urp.edu.pe',
      ciclo: 'VIII',
      fechaHoraInscripcion: '2026-11-10T08:30:00Z',
      fechaHoraAsistencia: null,
      estadoParticipacion: 1,
      alumnoID: 88
    }
  ]
}

describe('ConferenciasReporte.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockImplementation((url) => {
      if (url.includes('/report')) {
        return Promise.resolve({ data: JSON.parse(JSON.stringify(mockReport)) })
      }
      return Promise.resolve({ data: JSON.parse(JSON.stringify(mockConferences)) })
    })
  })

  it('renders header elements and title correctly', async () => {
    const wrapper = mount(ConferenciasReporte)
    await flushPromises()

    expect(wrapper.find('.header-badge-tag').text()).toContain('AUDITORÍA DE ASISTENCIAS & ACREDITACIÓN')
    expect(wrapper.find('.header-title').text()).toContain('Estadísticas y Reportes de Conferencias')
    expect(wrapper.find('.btn-export-excel').exists()).toBe(true)
  })

  it('selects initialConferenciaId when passed as prop', async () => {
    const wrapper = mount(ConferenciasReporte, {
      props: {
        initialConferenciaId: 2
      }
    })
    await flushPromises()

    expect(wrapper.find('.banner-title').text()).toContain('Simposio de Neurología Clínica')
  })

  it('displays the 6 KPI metric cards with formatted numbers', async () => {
    const wrapper = mount(ConferenciasReporte)
    await flushPromises()

    const cards = wrapper.findAll('.kpi-card')
    expect(cards.length).toBe(6)

    expect(wrapper.text()).toContain('TOTAL PRE-INSCRITOS')
    expect(wrapper.text()).toContain('15')
    expect(wrapper.text()).toContain('TOTAL ASISTENTES')
    expect(wrapper.text()).toContain('14')
    expect(wrapper.text()).toContain('ACREDITADOS (OFICIAL)')
    expect(wrapper.text()).toContain('12')
    expect(wrapper.text()).toContain('ESPONTÁNEOS')
    expect(wrapper.text()).toContain('2')
    expect(wrapper.text()).toContain('INASISTENCIAS')
    expect(wrapper.text()).toContain('3')
    expect(wrapper.text()).toContain('EFECTIVIDAD')
    expect(wrapper.text()).toContain('80%')
  })

  it('filters participants by search query and tabs', async () => {
    const wrapper = mount(ConferenciasReporte)
    await flushPromises()

    expect(wrapper.findAll('.table-row').length).toBe(3)

    const tabs = wrapper.findAll('.tab-btn')
    await tabs[1].trigger('click')
    expect(wrapper.findAll('.table-row').length).toBe(1)
    expect(wrapper.text()).toContain('Carlos Sanchez Perez')

    await tabs[0].trigger('click')
    const searchInput = wrapper.find('.search-text-field')
    await searchInput.setValue('Lopez')
    expect(wrapper.findAll('.table-row').length).toBe(1)
    expect(wrapper.text()).toContain('María Lopez Rios')
  })

  it('shows empadronado badge for registered student and action button for unlinked', async () => {
    const wrapper = mount(ConferenciasReporte)
    await flushPromises()

    expect(wrapper.find('.badge-empadronado').exists()).toBe(true)
    expect(wrapper.find('.btn-empadronar').exists()).toBe(true)
  })

  it('opens quick edit modal and sends PUT request', async () => {
    axios.put = vi.fn().mockResolvedValue({ data: { ok: true } })
    const wrapper = mount(ConferenciasReporte)
    await flushPromises()

    const pencilBtn = wrapper.find('.btn-pencil')
    await pencilBtn.trigger('click')
    expect(wrapper.find('.modal-box').exists()).toBe(true)

    const inputs = wrapper.findAll('.form-input')
    await inputs[0].setValue('Carlos Alberto')
    await wrapper.find('.btn-save').trigger('click')
    await flushPromises()

    expect(axios.put).toHaveBeenCalled()
    expect(wrapper.find('.modal-box').exists()).toBe(false)
  })
})
