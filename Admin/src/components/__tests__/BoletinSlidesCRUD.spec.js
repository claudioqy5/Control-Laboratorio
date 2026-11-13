import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import BoletinSlidesCRUD from '../BoletinSlidesCRUD.vue'

vi.mock('axios')

const mockSlides = [
  {
    idSlide: 1,
    titulo: 'Campaña de Vacunación 2026',
    descripcion: null,
    imagenUrl: '/api/static/boletines/vacuna.webp',
    formato: 1,
    enlaceUrl: 'https://medicina.urp.edu.pe/vacunacion'
  },
  {
    idSlide: 2,
    titulo: 'Taller Quirúrgico de Simulación',
    descripcion: 'Capacitación práctica en el laboratorio de simulación clínica.',
    imagenUrl: '/api/static/boletines/taller.jpg',
    formato: 2,
    enlaceUrl: null
  }
]

describe('BoletinSlidesCRUD.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockResolvedValue({ data: JSON.parse(JSON.stringify(mockSlides)) })
  })

  it('renders header, quota badge and slide table correctly', async () => {
    const wrapper = mount(BoletinSlidesCRUD)
    await flushPromises()

    expect(wrapper.find('.header-title').text()).toBe('Boletín Informativo & Novedades')
    expect(wrapper.find('.quota-text').text()).toBe('2 / 6 afiches activos')

    const rows = wrapper.findAll('tbody tr')
    expect(rows.length).toBe(2)

    const firstRowText = rows[0].text()
    expect(firstRowText).toContain('Campaña de Vacunación 2026')
    expect(firstRowText).toContain('Vertical')
    expect(firstRowText).toContain('Abrir')

    const secondRowText = rows[1].text()
    expect(secondRowText).toContain('Taller Quirúrgico de Simulación')
    expect(secondRowText).toContain('Bipartito')
    expect(secondRowText).toContain('Capacitación práctica')
  })

  it('opens and closes the create modal, and switches format tabs', async () => {
    const wrapper = mount(BoletinSlidesCRUD)
    await flushPromises()

    expect(wrapper.find('.modal-card').exists()).toBe(true)
    expect(wrapper.findAll('.modal-overlay')[0].classes()).not.toContain('active')

    await wrapper.find('.btn-primary-add').trigger('click')
    expect(wrapper.findAll('.modal-overlay')[0].classes()).toContain('active')

    const formatCards = wrapper.findAll('.format-card')
    expect(formatCards[0].classes()).toContain('selected')

    await formatCards[1].trigger('click')
    expect(formatCards[1].classes()).toContain('selected')
    expect(formatCards[0].classes()).not.toContain('selected')

    await wrapper.find('.btn-modal-cancel').trigger('click')
    expect(wrapper.findAll('.modal-overlay')[0].classes()).not.toContain('active')
  })

  it('alerts user and blocks opening modal when quota is full (6 slides)', async () => {
    const fullSlides = Array.from({ length: 6 }, (_, i) => ({
      idSlide: i + 1,
      titulo: `Afiche ${i + 1}`,
      descripcion: null,
      imagenUrl: `/img${i + 1}.webp`,
      formato: 1,
      enlaceUrl: null
    }))

    axios.get.mockResolvedValueOnce({ data: fullSlides })

    const wrapper = mount(BoletinSlidesCRUD)
    await flushPromises()

    expect(wrapper.find('.quota-badge').classes()).toContain('quota-badge-full')
    expect(wrapper.find('.quota-text').text()).toBe('6 / 6 afiches activos')

    await wrapper.find('.btn-primary-add').trigger('click')

    expect(wrapper.findAll('.modal-overlay')[0].classes()).not.toContain('active')
    expect(wrapper.find('.toast-notification').exists()).toBe(true)
    expect(wrapper.find('.toast-notification').text()).toContain('Límite de 6 afiches activos alcanzado')
  })

  it('opens zoom modal when clicking thumbnail or zoom action button', async () => {
    const wrapper = mount(BoletinSlidesCRUD)
    await flushPromises()

    const zoomOverlay = wrapper.findAll('.modal-overlay')[2]
    expect(zoomOverlay.classes()).not.toContain('active')

    const zoomButton = wrapper.find('.btn-icon-zoom')
    await zoomButton.trigger('click')

    expect(wrapper.findAll('.modal-overlay')[2].classes()).toContain('active')
    expect(wrapper.find('.modal-card-zoom').text()).toContain('Campaña de Vacunación 2026')

    await wrapper.find('.modal-card-zoom .modal-close-btn').trigger('click')
    expect(wrapper.findAll('.modal-overlay')[2].classes()).not.toContain('active')
  })

  it('opens delete confirmation modal and calls delete endpoint', async () => {
    axios.delete.mockResolvedValueOnce({ data: {} })
    const wrapper = mount(BoletinSlidesCRUD)
    await flushPromises()

    const deleteOverlay = wrapper.findAll('.modal-overlay')[1]
    expect(deleteOverlay.classes()).not.toContain('active')

    const deleteButton = wrapper.find('.btn-icon-delete')
    await deleteButton.trigger('click')

    expect(wrapper.findAll('.modal-overlay')[1].classes()).toContain('active')
    expect(wrapper.find('.delete-slide-summary').text()).toContain('Campaña de Vacunación 2026')

    await wrapper.find('.btn-modal-danger').trigger('click')
    await flushPromises()

    expect(axios.delete).toHaveBeenCalledWith(expect.stringContaining('/api/admin/boletin-slides/1'))
  })
})
