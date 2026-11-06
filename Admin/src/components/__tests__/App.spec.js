import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import App from '../../App.vue'
import AdminLogin from '../AdminLogin.vue'
import Sidebar from '../Sidebar.vue'
import Dashboard from '../Dashboard.vue'
import ReportesConexiones from '../ReportesConexiones.vue'
import ConfiguracionAccesos from '../ConfiguracionAccesos.vue'

describe('App Integration', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    localStorage.clear()
  })

  const mountApp = () => {
    return mount(App, {
      global: {
        stubs: {
          Dashboard: true,
          LaboratoryMap: true,
          EquiposList: true,
          AlumnosCRUD: true,
          LibrosCRUD: true,
          PrestamosCRUD: true,
          CategoriasCRUD: true,
          MapaBiblioteca: true,
          ActiveSessions: true,
          ReportesConexiones: {
            name: 'ReportesConexiones',
            props: ['initialTab'],
            template: '<div data-test="reportes-conexiones"></div>'
          },
          UsoEstadisticas: true,
          ConfiguracionAccesos: {
            name: 'ConfiguracionAccesos',
            props: ['currentUser'],
            template: '<div data-test="configuracion-accesos"></div>'
          },
          ChatBot: true
        }
      }
    })
  }

  it('muestra AdminLogin si no esta autenticado sin adminToken en localStorage', () => {
    const wrapper = mountApp()

    expect(wrapper.findComponent(AdminLogin).exists()).toBe(true)
    expect(wrapper.findComponent(Sidebar).exists()).toBe(false)
  })

  it('inicializa autenticado y renderiza Sidebar si existe token y usuario en localStorage', () => {
    const mockUser = {
      idUsuarioAdmin: 1,
      username: 'superadmin',
      rol: 1,
      rolNombre: 'SuperAdmin'
    }
    localStorage.setItem('adminToken', 'fake-jwt-token')
    localStorage.setItem('adminUser', JSON.stringify(mockUser))

    const wrapper = mountApp()

    expect(wrapper.findComponent(AdminLogin).exists()).toBe(false)
    const sidebar = wrapper.findComponent(Sidebar)
    expect(sidebar.exists()).toBe(true)
    expect(sidebar.props('user')).toEqual(mockUser)
    expect(sidebar.props('currentView')).toBe('dashboard')
    expect(wrapper.findComponent(Dashboard).exists()).toBe(true)
  })

  it('actualiza isAuthenticated y currentUser al recibir login-success de AdminLogin', async () => {
    const wrapper = mountApp()
    expect(wrapper.findComponent(AdminLogin).exists()).toBe(true)

    const userLoggedIn = {
      idUsuarioAdmin: 2,
      username: 'fvalero',
      rol: 2,
      rolNombre: 'Biblioteca'
    }

    await wrapper.findComponent(AdminLogin).vm.$emit('login-success', userLoggedIn)
    await flushPromises()

    expect(wrapper.findComponent(AdminLogin).exists()).toBe(false)
    const sidebar = wrapper.findComponent(Sidebar)
    expect(sidebar.exists()).toBe(true)
    expect(sidebar.props('user')).toEqual(userLoggedIn)
  })

  it('cambia la vista activa y propaga props al emitir change-view desde Sidebar', async () => {
    const superAdminUser = {
      idUsuarioAdmin: 1,
      username: 'admin',
      rol: 1,
      rolNombre: 'SuperAdmin'
    }
    localStorage.setItem('adminToken', 'fake-jwt-token')
    localStorage.setItem('adminUser', JSON.stringify(superAdminUser))

    const wrapper = mountApp()
    const sidebar = wrapper.findComponent(Sidebar)

    await sidebar.vm.$emit('change-view', 'accesos')
    await flushPromises()
    const configAccesos = wrapper.findComponent(ConfiguracionAccesos)
    expect(configAccesos.exists()).toBe(true)
    expect(configAccesos.props('currentUser')).toEqual(superAdminUser)

    await sidebar.vm.$emit('change-view', 'escaneos')
    await flushPromises()
    const reportesEscaneos = wrapper.findComponent(ReportesConexiones)
    expect(reportesEscaneos.exists()).toBe(true)
    expect(reportesEscaneos.props('initialTab')).toBe('escaneos')

    await sidebar.vm.$emit('change-view', 'conexiones')
    await flushPromises()
    const reportesConexiones = wrapper.findComponent(ReportesConexiones)
    expect(reportesConexiones.exists()).toBe(true)
    expect(reportesConexiones.props('initialTab')).toBe('conexiones')
  })

  it('purga localStorage y vuelve al login al emitir logout desde Sidebar', async () => {
    localStorage.setItem('adminToken', 'token-to-purge')
    localStorage.setItem('adminRefreshToken', 'refresh-to-purge')
    localStorage.setItem('adminUser', JSON.stringify({ id: 99, username: 'test' }))

    const wrapper = mountApp()
    const sidebar = wrapper.findComponent(Sidebar)
    expect(sidebar.exists()).toBe(true)

    await sidebar.vm.$emit('logout')
    await flushPromises()

    expect(localStorage.getItem('adminToken')).toBeNull()
    expect(localStorage.getItem('adminRefreshToken')).toBeNull()
    expect(localStorage.getItem('adminUser')).toBeNull()
    expect(wrapper.findComponent(AdminLogin).exists()).toBe(true)
    expect(wrapper.findComponent(Sidebar).exists()).toBe(false)
  })
})
