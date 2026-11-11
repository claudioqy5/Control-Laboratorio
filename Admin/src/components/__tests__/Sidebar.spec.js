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

  it('renders workspace tabs and disabled modules with disabled class and title', async () => {
    const wrapper = mount(Sidebar)

    const tabs = wrapper.findAll('.workspace-tab')
    expect(tabs.length).toBe(3)
    const tabTexts = tabs.map(tab => tab.text())
    expect(tabTexts).toContain('Biblioteca')
    expect(tabTexts).toContain('Página Web')
    expect(tabTexts).toContain('Sistema')

    const webTab = tabs.find(tab => tab.text().includes('Página Web'))
    await webTab.trigger('click')

    const webDisabled = wrapper.findAll('.disabled')
    expect(webDisabled.length).toBe(4)
    webDisabled.forEach(item => {
      expect(item.attributes('title')).toBe('Próximamente')
    })
    const webLabels = webDisabled.map(item => item.text())
    expect(webLabels).not.toContain('Gestión de Conferencias')
    expect(webLabels).not.toContain('Estadísticas Conferencias')
    expect(webLabels).not.toContain('Objetos Perdidos')
    expect(webLabels).toContain('Bases de Datos')
    expect(webLabels).toContain('Matriz Hexagonal')
    expect(webLabels).toContain('Boletín Informativo')
    expect(webLabels).toContain('Preguntas Frecuentes')

    const sistemaTab = tabs.find(tab => tab.text().includes('Sistema'))
    await sistemaTab.trigger('click')

    const sistemaDisabled = wrapper.findAll('.disabled')
    expect(sistemaDisabled.length).toBe(4)
    sistemaDisabled.forEach(item => {
      expect(item.attributes('title')).toBe('Próximamente')
    })
    const sistemaLabels = sistemaDisabled.map(item => item.text())
    expect(sistemaLabels).toContain('Pineo de Equipos')
    expect(sistemaLabels).toContain('Tickets de Reportes')
    expect(sistemaLabels).toContain('Log de Acciones')
    expect(sistemaLabels).toContain('Horario de Atención')
  })

  it('emits change-view event when active modules are clicked', async () => {
    const wrapper = mount(Sidebar, {
      props: { currentView: 'dashboard' }
    })

    const allLinks = wrapper.findAll('a:not(.disabled)')

    const estadisticasLink = allLinks.find(link => link.text().includes('Estadísticas'))
    await estadisticasLink.trigger('click')
    expect(wrapper.emitted('change-view')).toBeTruthy()
    expect(wrapper.emitted('change-view')[0]).toEqual(['estadisticas'])

    const mapLink = allLinks.find(link => link.text().includes('Mapa de Estaciones'))
    await mapLink.trigger('click')
    expect(wrapper.emitted('change-view')[1]).toEqual(['map'])

    const reportesLink = allLinks.find(link => link.text().includes('Reportes'))
    await reportesLink.trigger('click')
    expect(wrapper.emitted('change-view')[2]).toEqual(['conexiones'])

    const librosLink = allLinks.find(link => link.text().includes('Libros'))
    await librosLink.trigger('click')
    expect(wrapper.emitted('change-view')[3]).toEqual(['libros'])
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

    const tabs = wrapper.findAll('.workspace-tab')
    const sistemaTab = tabs.find(tab => tab.text().includes('Sistema'))
    await sistemaTab.trigger('click')

    const configLink = wrapper.findAll('a').find(link => link.text().includes('Configuración / Accesos'))
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

    const tabs = wrapper.findAll('.workspace-tab')
    const sistemaTab = tabs.find(tab => tab.text().includes('Sistema'))
    await sistemaTab.trigger('click')

    const configLink = wrapper.findAll('a').find(link => link.text().includes('Configuración / Accesos'))
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

  it('hides workspace tabs and labels when collapsed and shows them when expanded', async () => {
    const wrapper = mount(Sidebar)

    expect(wrapper.classes()).toContain('collapsed')
    expect(wrapper.find('.workspace-tabs').attributes('style')).toContain('display: none')

    const brandBtn = wrapper.find('.brand-icon')
    await brandBtn.trigger('click')
    expect(wrapper.classes()).not.toContain('collapsed')

    const style = wrapper.find('.workspace-tabs').attributes('style')
    expect(!style || !style.includes('display: none')).toBe(true)
  })

  it('activates web tab and emits view changes for conferencias modules', async () => {
    const wrapper = mount(Sidebar, {
      props: { currentView: 'conferencias' }
    })

    const tabs = wrapper.findAll('.workspace-tab')
    const webTab = tabs.find(tab => tab.text().includes('Página Web'))
    expect(webTab.classes()).toContain('active')

    const links = wrapper.findAll('a:not(.disabled)')
    const confLink = links.find(l => l.text().includes('Gestión de Conferencias'))
    expect(confLink.classes()).toContain('active')

    const statsLink = links.find(l => l.text().includes('Estadísticas Conferencias'))
    await statsLink.trigger('click')
    expect(wrapper.emitted('change-view')).toBeTruthy()
    expect(wrapper.emitted('change-view')[0]).toEqual(['conferencias-reportes'])

    const objetosLink = links.find(l => l.text().includes('Objetos Perdidos'))
    await objetosLink.trigger('click')
    expect(wrapper.emitted('change-view')[1]).toEqual(['objetos-perdidos'])
  })
})
