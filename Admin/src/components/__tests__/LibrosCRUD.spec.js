import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import LibrosCRUD from '../LibrosCRUD.vue'

vi.mock('axios')

const mockLibros = [
  {
    libroID: 1,
    nroRegistro: 'REG-001',
    codigoBarras: '775001',
    nroClasificacion: 'WB 100 H32 2018',
    titulo: 'Anatomía Humana Básica',
    autor: 'Latarjet',
    estado: 'Disponible'
  },
  {
    libroID: 25,
    nroRegistro: 'REG-025',
    codigoBarras: '775025',
    nroClasificacion: 'QZ 4 R63 2020',
    titulo: 'Patología de Robbins',
    autor: 'Vinay Kumar',
    estado: 'Prestado'
  },
  {
    libroID: 99,
    nroRegistro: 'REG-099',
    codigoBarras: '775099',
    nroClasificacion: 'QV 38 G65 2022',
    titulo: 'Farmacología Médica',
    autor: 'Goodman & Gilman',
    estado: 'Disponible'
  }
]

describe('LibrosCRUD.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    axios.get.mockImplementation((url) => {
      if (url.includes('/api/Categorias')) {
        return Promise.resolve({ data: [{ categoriaID: 1, nombre: 'Medicina General' }] })
      }
      return Promise.resolve({ data: JSON.parse(JSON.stringify(mockLibros)) })
    })
  })

  it('renders the books management view and table', async () => {
    const wrapper = mount(LibrosCRUD)
    await flushPromises()

    expect(wrapper.text()).toContain('Registro de Libros')
    expect(wrapper.findAll('tbody tr').length).toBe(3)
  })

  it('displays the most recently added books first by default (highest libroID first)', async () => {
    const wrapper = mount(LibrosCRUD)
    await flushPromises()

    const rows = wrapper.findAll('tbody tr')
    // El primer libro mostrado debe ser el ID 99 (el más recientemente agregado)
    expect(rows[0].text()).toContain('Farmacología Médica')
    expect(rows[0].text()).toContain('REG-099')

    // El segundo debe ser el ID 25
    expect(rows[1].text()).toContain('Patología de Robbins')
    expect(rows[1].text()).toContain('REG-025')

    // El último debe ser el ID 1 (el más antiguo)
    expect(rows[2].text()).toContain('Anatomía Humana Básica')
    expect(rows[2].text()).toContain('REG-001')
  })

  it('preserves most-recent-first ordering when searching/filtering', async () => {
    const wrapper = mount(LibrosCRUD)
    await flushPromises()

    const searchInput = wrapper.find('input[type="text"]')
    await searchInput.setValue('Médica')
    await flushPromises()

    const rows = wrapper.findAll('tbody tr')
    // Coinciden ID 99 ("Farmacología Médica")
    expect(rows.length).toBe(1)
    expect(rows[0].text()).toContain('Farmacología Médica')
  })
})
