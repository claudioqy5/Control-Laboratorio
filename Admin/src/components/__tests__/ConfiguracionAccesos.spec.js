import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import ConfiguracionAccesos from '../ConfiguracionAccesos.vue'

vi.mock('axios')

const getMockAdmins = () => [
  {
    idUsuarioAdmin: 1,
    username: 'fvalero',
    nombres: 'FRANCISCA VALERO',
    correo: 'fvalero@urp.edu.pe',
    rol: 1,
    rolNombre: 'SuperAdmin',
    estadoActivo: true,
    fechaCreacion: '2026-10-12T08:30:00',
    ultimoAcceso: '2026-11-05T15:42:00'
  },
  {
    idUsuarioAdmin: 2,
    username: 'amanguinuri',
    nombres: 'ALESHKA MANGUINURI',
    correo: 'amanguinuri@urp.edu.pe',
    rol: 2,
    rolNombre: 'Biblioteca',
    estadoActivo: true,
    fechaCreacion: '2026-10-14T10:15:00',
    ultimoAcceso: '2026-11-05T16:10:00'
  },
  {
    idUsuarioAdmin: 3,
    username: 'gseguro',
    nombres: 'GRACE SEGURO',
    correo: 'gseguro@urp.edu.pe',
    rol: 3,
    rolNombre: 'Laboratorio',
    estadoActivo: false,
    fechaCreacion: '2026-10-25T12:10:00',
    ultimoAcceso: null
  }
]

describe('ConfiguracionAccesos', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('carga y renderiza la lista de administradores desde la API', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/api/admin/usuarios'))
    expect(wrapper.text()).toContain('fvalero')
    expect(wrapper.text()).toContain('amanguinuri')
    expect(wrapper.text()).toContain('gseguro')
    expect(wrapper.text()).toContain('3 Registrados')
  })

  it('filtra en tiempo real por texto de búsqueda', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    const searchInput = wrapper.find('#searchInput')

    await searchInput.setValue('amanguinuri')
    expect(wrapper.text()).toContain('amanguinuri')
    expect(wrapper.text()).not.toContain('fvalero')
    expect(wrapper.text()).not.toContain('gseguro')

    await searchInput.setValue('SEGURO')
    expect(wrapper.text()).toContain('gseguro')
    expect(wrapper.text()).not.toContain('amanguinuri')

    await searchInput.setValue('no-coincide-nada')
    expect(wrapper.text()).toContain('No se encontraron registros.')
  })

  it('filtra por selector de estado', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    const statusSelect = wrapper.find('#statusFilter')

    await statusSelect.setValue('Activo')
    expect(wrapper.text()).toContain('fvalero')
    expect(wrapper.text()).toContain('amanguinuri')
    expect(wrapper.text()).not.toContain('gseguro')

    await statusSelect.setValue('Inactivo')
    expect(wrapper.text()).toContain('gseguro')
    expect(wrapper.text()).not.toContain('fvalero')
    expect(wrapper.text()).not.toContain('amanguinuri')

    await statusSelect.setValue('Todos')
    expect(wrapper.text()).toContain('fvalero')
    expect(wrapper.text()).toContain('amanguinuri')
    expect(wrapper.text()).toContain('gseguro')
  })

  it('regla de auto-protección: deshabilita el botón de desactivar para el currentUser', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })

    const wrapper = mount(ConfiguracionAccesos, {
      props: {
        currentUser: { idUsuarioAdmin: 1, username: 'fvalero' }
      }
    })
    await flushPromises()

    const toggleButtons = wrapper.findAll('[data-testid="toggle-status-btn"]')
    expect(toggleButtons.length).toBe(3)

    const ownButton = toggleButtons[0]
    expect(ownButton.attributes('disabled')).toBeDefined()
    expect(ownButton.attributes('title')).toBe('No puedes desactivar tu propia cuenta')

    const otherButton = toggleButtons[1]
    expect(otherButton.attributes('disabled')).toBeUndefined()
    expect(otherButton.attributes('title')).toBe('Desactivar')

    await ownButton.trigger('click')
    expect(axios.patch).not.toHaveBeenCalled()

    axios.patch.mockResolvedValueOnce({
      data: { message: 'Estado actualizado.', estadoActivo: false }
    })
    await otherButton.trigger('click')
    expect(axios.patch).toHaveBeenCalledWith(expect.stringContaining('/api/admin/usuarios/2/estado'))
  })

  it('abre el modal y realiza la creación de un nuevo administrador', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })
    axios.post.mockResolvedValueOnce({
      data: {
        idUsuarioAdmin: 4,
        username: 'nusuario',
        nombres: 'NUEVO USUARIO',
        correo: 'nuevo@urp.edu.pe',
        rol: 2,
        rolNombre: 'Biblioteca',
        estadoActivo: true,
        fechaCreacion: '2026-11-06T10:00:00',
        ultimoAcceso: null
      }
    })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    await wrapper.find('button.btn-primary').trigger('click')
    expect(wrapper.find('#modalTitle').text()).toBe('Nuevo Usuario')

    const inputs = wrapper.findAll('.modal-card input')
    await inputs[0].setValue('nusuario')
    await inputs[1].setValue('NUEVO USUARIO')
    await inputs[2].setValue('nuevo@urp.edu.pe')
    await inputs[3].setValue('password123')

    const selectRol = wrapper.find('.modal-card select')
    await selectRol.setValue(2)

    await wrapper.find('.modal-card form').trigger('submit.prevent')
    await flushPromises()

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/usuarios'),
      {
        username: 'nusuario',
        password: 'password123',
        nombres: 'NUEVO USUARIO',
        correo: 'nuevo@urp.edu.pe',
        rol: 2
      }
    )

    expect(wrapper.text()).toContain('nusuario')
    expect(wrapper.text()).toContain('Usuario creado exitosamente.')
  })

  it('abre el modal de edición y actualiza los datos de un administrador', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })
    axios.put.mockResolvedValueOnce({
      data: {
        idUsuarioAdmin: 2,
        username: 'amanguinuri',
        nombres: 'ALESHKA EDITADA',
        correo: 'editada@urp.edu.pe',
        rol: 3,
        rolNombre: 'Laboratorio',
        estadoActivo: true,
        fechaCreacion: '2026-10-14T10:15:00',
        ultimoAcceso: '2026-11-05T16:10:00'
      }
    })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    const editBtns = wrapper.findAll('[data-testid="edit-admin-btn"]')
    await editBtns[1].trigger('click')

    expect(wrapper.find('#modalTitle').text()).toBe('Editar Usuario')

    const inputs = wrapper.findAll('.modal-card input')
    expect(inputs[0].attributes('disabled')).toBeDefined()

    await inputs[1].setValue('ALESHKA EDITADA')
    await inputs[2].setValue('editada@urp.edu.pe')

    await wrapper.find('.modal-card form').trigger('submit.prevent')
    await flushPromises()

    expect(axios.put).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/usuarios/2'),
      {
        nombres: 'ALESHKA EDITADA',
        correo: 'editada@urp.edu.pe',
        rol: 2
      }
    )

    expect(wrapper.text()).toContain('ALESHKA EDITADA')
    expect(wrapper.text()).toContain('Usuario actualizado exitosamente.')
  })

  it('abre el modal de contraseña y realiza la actualización de contraseña', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })
    axios.put.mockResolvedValueOnce({
      data: { message: 'Contraseña actualizada exitosamente.' }
    })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    const keyBtns = wrapper.findAll('[data-testid="key-password-btn"]')
    await keyBtns[0].trigger('click')

    expect(wrapper.text()).toContain('Cambiar Contraseña')
    expect(wrapper.text()).toContain('fvalero')

    const passwordInputs = wrapper.findAll('.modal-card input[type="password"]')
    expect(passwordInputs.length).toBe(2)

    await passwordInputs[0].setValue('nuevaClave123')
    await passwordInputs[1].setValue('nuevaClave123')

    await wrapper.find('.modal-card form').trigger('submit.prevent')
    await flushPromises()

    expect(axios.put).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/usuarios/1/password'),
      {
        nuevaPassword: 'nuevaClave123'
      }
    )

    expect(wrapper.text()).toContain('Contraseña actualizada exitosamente.')
  })

  it('valida que las contraseñas coincidan antes de enviar la actualización', async () => {
    axios.get.mockResolvedValueOnce({ data: getMockAdmins() })

    const wrapper = mount(ConfiguracionAccesos)
    await flushPromises()

    const keyBtns = wrapper.findAll('[data-testid="key-password-btn"]')
    await keyBtns[0].trigger('click')

    const passwordInputs = wrapper.findAll('.modal-card input[type="password"]')
    await passwordInputs[0].setValue('claveValida123')
    await passwordInputs[1].setValue('otraClaveDiferente')

    await wrapper.find('.modal-card form').trigger('submit.prevent')
    await flushPromises()

    expect(axios.put).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Las contraseñas no coinciden.')
  })
})
