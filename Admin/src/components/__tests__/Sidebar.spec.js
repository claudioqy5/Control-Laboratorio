import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import Sidebar from '../Sidebar.vue'

describe('Sidebar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('renders all 5 thematic sections and inactive modules with disabled class and title', () => {
    const wrapper = mount(Sidebar)

    const text = wrapper.text()
    expect(text).toContain('General')
    expect(text).toContain('Laboratorio de Cómputo')
    expect(text).toContain('Biblioteca')
    expect(text).toContain('Servicios & Atención')
    expect(text).toContain('Sistema')

    const disabledItems = wrapper.findAll('.disabled')
    expect(disabledItems.length).toBe(11)

    disabledItems.forEach(item => {
      expect(item.attributes('title')).toBe('Próximamente')
    })

    const disabledLabels = disabledItems.map(item => item.text())
    expect(disabledLabels).toContain('Pineo de Equipos')
    expect(disabledLabels).toContain('Directorio de Bases')
    expect(disabledLabels).toContain('Matriz Hexagonal')
    expect(disabledLabels).toContain('Gestión de Conferencias')
    expect(disabledLabels).toContain('Estadísticas Conferencia')
    expect(disabledLabels).toContain('Tickets de Reportes')
    expect(disabledLabels).toContain('Objetos Perdidos')
    expect(disabledLabels).toContain('Boletín Informativo')
    expect(disabledLabels).toContain('Horario de Atención')
    expect(disabledLabels).toContain('Preguntas Frecuentes')
    expect(disabledLabels).toContain('Log de Acciones')
  })

  it('emits change-view event when active modules are clicked', async () => {
    const wrapper = mount(Sidebar, {
      props: { currentView: 'dashboard' }
    })

    const allLinks = wrapper.findAll('a:not(.disabled)')

    const estadisticasLink = allLinks.find(link => link.text().includes('Estadísticas Generales'))
    await estadisticasLink.trigger('click')
    expect(wrapper.emitted('change-view')).toBeTruthy()
    expect(wrapper.emitted('change-view')[0]).toEqual(['estadisticas'])

    const mapLink = allLinks.find(link => link.text().includes('Mapa de Estaciones'))
    await mapLink.trigger('click')
    expect(wrapper.emitted('change-view')[1]).toEqual(['map'])

    const librosLink = allLinks.find(link => link.text().includes('Libros'))
    await librosLink.trigger('click')
    expect(wrapper.emitted('change-view')[2]).toEqual(['libros'])
  })

  it('denies access to Configuración / Accesos for non-SuperAdmin user and displays toast alert', async () => {
    vi.useFakeTimers()
    const nonAdminUser = {
      nombres: 'Francisca Valero',
      rol: 2,
      rolNombre: 'Biblioteca'
    }

    const wrapper = mount(Sidebar, {
      props: { user: nonAdminUser }
    })

    expect(wrapper.find('.toast-alert').exists()).toBe(false)

    const allLinks = wrapper.findAll('a')
    const configLink = allLinks.find(link => link.text().includes('Configuración / Accesos'))
    await configLink.trigger('click')

    expect(wrapper.emitted('change-view')).toBeFalsy()

    const toast = wrapper.find('.toast-alert')
    expect(toast.exists()).toBe(true)
    expect(toast.text()).toContain('Acceso denegado: Se requiere rol SuperAdmin para acceder a esta pestaña.')

    const closeBtn = wrapper.find('.toast-close')
    await closeBtn.trigger('click')
    expect(wrapper.find('.toast-alert').exists()).toBe(false)

    await configLink.trigger('click')
    expect(wrapper.find('.toast-alert').exists()).toBe(true)

    vi.advanceTimersByTime(4000)
    await wrapper.vm.$nextTick()
    expect(wrapper.find('.toast-alert').exists()).toBe(false)
  })

  it('authorizes access to Configuración / Accesos for SuperAdmin user and emits change-view', async () => {
    const adminUser = {
      nombres: 'Super Administrador',
      rol: 1,
      rolNombre: 'SuperAdmin'
    }

    const wrapper = mount(Sidebar, {
      props: { user: adminUser }
    })

    const allLinks = wrapper.findAll('a')
    const configLink = allLinks.find(link => link.text().includes('Configuración / Accesos'))
    await configLink.trigger('click')

    expect(wrapper.emitted('change-view')).toBeTruthy()
    expect(wrapper.emitted('change-view')[0]).toEqual(['accesos'])
    expect(wrapper.find('.toast-alert').exists()).toBe(false)
  })

  it('emits logout event when clicking logout button', async () => {
    const wrapper = mount(Sidebar, {
      props: {
        user: { nombres: 'Juan Perez', rolNombre: 'Laboratorio' }
      }
    })

    const logoutButton = wrapper.find('.logout-button')
    await logoutButton.trigger('click')

    expect(wrapper.emitted('logout')).toBeTruthy()
  })

  it('renders user details directly from props without HTTP requests', () => {
    const user = {
      nombres: 'Lic. Francisca Valero',
      rolNombre: 'Biblioteca'
    }

    const wrapper = mount(Sidebar, {
      props: { user }
    })

    expect(wrapper.find('.user-name').text()).toBe('Lic. Francisca Valero')
    expect(wrapper.find('.user-role').text()).toBe('Biblioteca')
  })

  it('toggles collapse state on brand icon click and mouse events', async () => {
    const wrapper = mount(Sidebar)

    expect(wrapper.classes()).toContain('collapsed')

    const brandBtn = wrapper.find('.brand-icon')
    await brandBtn.trigger('click')
    expect(wrapper.classes()).not.toContain('collapsed')

    await brandBtn.trigger('click')
    expect(wrapper.classes()).toContain('collapsed')

    await wrapper.trigger('mouseenter')
    expect(wrapper.classes()).not.toContain('collapsed')

    await wrapper.trigger('mouseleave')
    expect(wrapper.classes()).toContain('collapsed')
  })

  it('keeps sidebar expanded when pinned and unpins properly', async () => {
    const wrapper = mount(Sidebar)

    const brandBtn = wrapper.find('.brand-icon')
    await brandBtn.trigger('click')
    expect(wrapper.classes()).not.toContain('collapsed')

    const pinBtn = wrapper.find('.pin-button')
    expect(pinBtn.exists()).toBe(true)
    expect(pinBtn.classes()).not.toContain('active')

    await pinBtn.trigger('click')
    expect(pinBtn.classes()).toContain('active')
    expect(wrapper.classes()).toContain('pinned')

    await wrapper.trigger('mouseleave')
    expect(wrapper.classes()).not.toContain('collapsed')

    await pinBtn.trigger('click')
    expect(pinBtn.classes()).not.toContain('active')
    expect(wrapper.classes()).not.toContain('pinned')

    await wrapper.trigger('mouseleave')
    expect(wrapper.classes()).toContain('collapsed')
  })

  it('hides nested sub-nav-group items when collapsed and shows them when expanded', async () => {
    const wrapper = mount(Sidebar)

    expect(wrapper.classes()).toContain('collapsed')
    expect(wrapper.find('.sub-nav-group').attributes('style')).toContain('display: none')

    const brandBtn = wrapper.find('.brand-icon')
    await brandBtn.trigger('click')
    expect(wrapper.classes()).not.toContain('collapsed')

    const style = wrapper.find('.sub-nav-group').attributes('style')
    expect(!style || !style.includes('display: none')).toBe(true)
  })
})
