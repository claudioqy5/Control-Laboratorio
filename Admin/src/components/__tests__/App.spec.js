import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import App from '../../App.vue'
import AdminLogin from '../AdminLogin.vue'
import Sidebar from '../Sidebar.vue'
import Dashboard from '../Dashboard.vue'
import ReportesConexiones from '../ReportesConexiones.vue'
import ConfiguracionAccesos from '../ConfiguracionAccesos.vue'
import ConferenciasCRUD from '../ConferenciasCRUD.vue'
import ConferenciasReporte from '../ConferenciasReporte.vue'

vi.mock('axios')

describe('App Integration', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
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
          ConferenciasCRUD: {
            name: 'ConferenciasCRUD',
            template: '<div data-test="conferencias-crud"></div>'
          },
          ConferenciasReporte: {
            name: 'ConferenciasReporte',
            props: ['initialConferenciaId'],
            template: '<div data-test="conferencias-reporte"></div>'
          },
          ChatBot: true
        }
      }
    })
  }

  it('muestra AdminLogin si no esta autenticado sin adminToken en localStorage', async () => {
    const wrapper = mountApp()
    await flushPromises()

    expect(wrapper.findComponent(AdminLogin).exists()).toBe(true)
    expect(wrapper.findComponent(Sidebar).exists()).toBe(false)
  })

  it('inicializa autenticado y renderiza Sidebar si existe token y usuario en localStorage', async () => {
    const mockUser = {
      idUsuarioAdmin: 1,
      username: 'superadmin',
      rol: 1,
      rolNombre: 'SuperAdmin'
    }
    localStorage.setItem('adminToken', 'fake-jwt-token')
    localStorage.setItem('adminRefreshToken', 'fake-refresh-token')
    localStorage.setItem('adminUser', JSON.stringify(mockUser))
    axios.get.mockResolvedValueOnce({ data: mockUser })

    const wrapper = mountApp()
    await flushPromises()

    expect(wrapper.findComponent(AdminLogin).exists()).toBe(false)
    const sidebar = wrapper.findComponent(Sidebar)
    expect(sidebar.exists()).toBe(true)
    expect(sidebar.props('user')).toEqual(mockUser)
    expect(sidebar.props('currentView')).toBe('dashboard')
    expect(wrapper.findComponent(Dashboard).exists()).toBe(true)
  })

  it('actualiza isAuthenticated y currentUser al recibir login-success de AdminLogin', async () => {
    const wrapper = mountApp()
    await flushPromises()
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
    localStorage.setItem('adminRefreshToken', 'fake-refresh-token')
    localStorage.setItem('adminUser', JSON.stringify(superAdminUser))
    axios.get.mockResolvedValueOnce({ data: superAdminUser })

    const wrapper = mountApp()
    await flushPromises()
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

    await sidebar.vm.$emit('change-view', 'conferencias')
    await flushPromises()
    const conferenciasCrud = wrapper.findComponent(ConferenciasCRUD)
    expect(conferenciasCrud.exists()).toBe(true)

    await conferenciasCrud.vm.$emit('ver-estadisticas', 42)
    await flushPromises()
    const conferenciasReporte = wrapper.findComponent(ConferenciasReporte)
    expect(conferenciasReporte.exists()).toBe(true)
    expect(conferenciasReporte.props('initialConferenciaId')).toBe(42)
  })

  it('purga localStorage y vuelve al login al emitir logout desde Sidebar', async () => {
    const mockUser = { idUsuarioAdmin: 99, username: 'test' }
    localStorage.setItem('adminToken', 'token-to-purge')
    localStorage.setItem('adminRefreshToken', 'refresh-to-purge')
    localStorage.setItem('adminUser', JSON.stringify(mockUser))
    axios.get.mockResolvedValueOnce({ data: mockUser })

    const wrapper = mountApp()
    await flushPromises()
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

  it('cierra sesion y purga datos si /api/auth/me falla con 401 o error de red', async () => {
    localStorage.setItem('adminToken', 'expired-token')
    localStorage.setItem('adminRefreshToken', 'some-refresh')
    localStorage.setItem('adminUser', JSON.stringify({ idUsuarioAdmin: 1 }))
    axios.get.mockRejectedValueOnce(new Error('Unauthorized'))

    const wrapper = mountApp()
    await flushPromises()

    expect(localStorage.getItem('adminToken')).toBeNull()
    expect(localStorage.getItem('adminRefreshToken')).toBeNull()
    expect(localStorage.getItem('adminUser')).toBeNull()
    expect(wrapper.findComponent(AdminLogin).exists()).toBe(true)
    expect(wrapper.findComponent(Sidebar).exists()).toBe(false)
  })

  it('ejecuta handleLogout al recibir el evento global auth:expired', async () => {
    const mockUser = { idUsuarioAdmin: 1, username: 'admin' }
    localStorage.setItem('adminToken', 'valid-token')
    localStorage.setItem('adminRefreshToken', 'valid-refresh')
    localStorage.setItem('adminUser', JSON.stringify(mockUser))
    axios.get.mockResolvedValueOnce({ data: mockUser })

    const wrapper = mountApp()
    await flushPromises()
    expect(wrapper.findComponent(Sidebar).exists()).toBe(true)

    window.dispatchEvent(new CustomEvent('auth:expired'))
    await flushPromises()

    expect(localStorage.getItem('adminToken')).toBeNull()
    expect(localStorage.getItem('adminRefreshToken')).toBeNull()
    expect(localStorage.getItem('adminUser')).toBeNull()
    expect(wrapper.findComponent(AdminLogin).exists()).toBe(true)
    expect(wrapper.findComponent(Sidebar).exists()).toBe(false)
  })
})
