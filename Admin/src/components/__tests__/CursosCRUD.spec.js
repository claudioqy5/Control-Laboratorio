import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import CursosCRUD from '../CursosCRUD.vue'

vi.mock('axios')

const mockCursos = [
  {
    idCurso: 1,
    codigoCurso: 'MH-0101',
    nombreCurso: 'Biología Celular y Molecular',
    ciclo: 1,
    estadoActivo: true,
    totalBasesAsociadas: 2,
    basesAsociadas: ['UpToDate', 'ClinicalKey']
  },
  {
    idCurso: 2,
    codigoCurso: 'MH-0201',
    nombreCurso: 'Bioestadística y Demografía Médica',
    ciclo: 2,
    estadoActivo: false,
    totalBasesAsociadas: 0,
    basesAsociadas: []
  }
]

describe('CursosCRUD.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockResolvedValue({ data: JSON.parse(JSON.stringify(mockCursos)) })
  })

  it('renders header, course count, and table with courses', async () => {
    const wrapper = mount(CursosCRUD)
    await flushPromises()

    expect(wrapper.find('.title').text()).toBe('Malla Curricular de Medicina')
    expect(wrapper.find('.subtitle').text()).toContain('2 Asignaturas Registradas')

    const rows = wrapper.findAll('tbody tr')
    expect(rows.length).toBe(2)

    const firstRowText = rows[0].text()
    expect(firstRowText).toContain('MH-0101')
    expect(firstRowText).toContain('Biología Celular y Molecular')
    expect(firstRowText).toContain('2 bases')

    const secondRowText = rows[1].text()
    expect(secondRowText).toContain('MH-0201')
    expect(secondRowText).toContain('Bioestadística y Demografía Médica')
    expect(secondRowText).toContain('0 bases')
  })

  it('filters courses by cycle and search text', async () => {
    const wrapper = mount(CursosCRUD)
    await flushPromises()

    const searchInput = wrapper.find('.search-input')
    await searchInput.setValue('Celular')

    const rows = wrapper.findAll('tbody tr')
    expect(rows.length).toBe(1)
    expect(rows[0].text()).toContain('Biología Celular y Molecular')

    await searchInput.setValue('')
    const cycleSelect = wrapper.find('.cycle-filter')
    await cycleSelect.setValue(2)

    const filteredRows = wrapper.findAll('tbody tr')
    expect(filteredRows.length).toBe(1)
    expect(filteredRows[0].text()).toContain('Bioestadística')
  })

  it('toggles course status via PATCH request', async () => {
    axios.patch.mockResolvedValueOnce({ data: { id: 1, estadoActivo: false } })
    const wrapper = mount(CursosCRUD)
    await flushPromises()

    const switchInput = wrapper.findAll('.switch input')[0]
    await switchInput.trigger('change')

    expect(axios.patch).toHaveBeenCalledWith(expect.stringContaining('/api/admin/cursos/1/toggle-activo'))
  })

  it('opens create modal and sends POST request on valid submit', async () => {
    axios.post.mockResolvedValueOnce({
      data: {
        idCurso: 3,
        codigoCurso: 'MH-0301',
        nombreCurso: 'Anatomía Humana I',
        ciclo: 3,
        estadoActivo: true,
        totalBasesAsociadas: 0,
        basesAsociadas: []
      }
    })

    const wrapper = mount(CursosCRUD)
    await flushPromises()

    const newBtn = wrapper.find('.btn-primary')
    await newBtn.trigger('click')

    expect(wrapper.find('.modal-card').exists()).toBe(true)

    const inputs = wrapper.findAll('.premium-input')
    await inputs[0].setValue('MH-0301')
    await inputs[1].setValue('Anatomía Humana I')
    await inputs[2].setValue(3)

    const saveBtn = wrapper.findAll('.modal-footer .btn-primary')[0]
    await saveBtn.trigger('click')
    await flushPromises()

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/cursos'),
      expect.objectContaining({
        codigoCurso: 'MH-0301',
        nombreCurso: 'Anatomía Humana I',
        ciclo: 3,
        estadoActivo: true
      })
    )
  })

  it('displays referential protection warning when deleting course with associated databases', async () => {
    const wrapper = mount(CursosCRUD)
    await flushPromises()

    const deleteBtn = wrapper.findAll('.delete-btn')[0]
    await deleteBtn.trigger('click')

    expect(wrapper.find('.banner-warning').exists()).toBe(true)
    expect(wrapper.find('.banner-warning').text()).toContain('vinculada a 2 bases de datos médicas')
    expect(wrapper.find('.recommendation-card').exists()).toBe(true)
    expect(wrapper.find('.linked-resources-list').text()).toContain('UpToDate')
    expect(wrapper.find('.linked-resources-list').text()).toContain('ClinicalKey')
  })

  it('deletes course directly when it has 0 associated databases', async () => {
    axios.delete.mockResolvedValueOnce({})
    const wrapper = mount(CursosCRUD)
    await flushPromises()

    const deleteBtn = wrapper.findAll('.delete-btn')[1]
    await deleteBtn.trigger('click')

    expect(wrapper.find('.delete-confirm-text').exists()).toBe(true)
    expect(wrapper.find('.btn-danger').exists()).toBe(true)

    await wrapper.find('.btn-danger').trigger('click')
    await flushPromises()

    expect(axios.delete).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/cursos/2'),
      expect.objectContaining({ params: { force: false } })
    )
  })
})
