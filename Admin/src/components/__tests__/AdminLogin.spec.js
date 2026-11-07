import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import axios from 'axios'
import AdminLogin from '../AdminLogin.vue'

vi.mock('axios')

describe('AdminLogin', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('renders form inputs and submit button', () => {
    const wrapper = mount(AdminLogin)

    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.find('input[type="password"]').exists()).toBe(true)
    expect(wrapper.find('button.btn-login').exists()).toBe(true)
  })

  it('updates form model bindings when inputs change', async () => {
    const wrapper = mount(AdminLogin)

    const usernameInput = wrapper.find('input[type="text"]')
    const passwordInput = wrapper.find('input[type="password"]')

    await usernameInput.setValue('adminUser')
    await passwordInput.setValue('secretPass')

    expect(usernameInput.element.value).toBe('adminUser')
    expect(passwordInput.element.value).toBe('secretPass')
  })

  it('saves token and emits login-success on successful authentication', async () => {
    axios.post.mockResolvedValueOnce({
      data: { token: 'mock-jwt-admin-token' }
    })

    const wrapper = mount(AdminLogin)

    await wrapper.find('input[type="text"]').setValue('admin')
    await wrapper.find('input[type="password"]').setValue('admin123')
    await wrapper.find('button.btn-login').trigger('click')

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining('/api/auth/admin-login'),
      { username: 'admin', password: 'admin123' }
    )

    expect(localStorage.getItem('adminToken')).toBe('mock-jwt-admin-token')
    expect(wrapper.emitted('login-success')).toBeTruthy()
  })

  it('saves token, refreshToken and usuario in localStorage and emits user on successful authentication', async () => {
    const mockUser = { id: 1, username: 'admin', rol: 'Administrador' }
    axios.post.mockResolvedValueOnce({
      data: {
        token: 'mock-jwt-admin-token',
        refreshToken: 'mock-jwt-refresh-token',
        usuario: mockUser
      }
    })

    const wrapper = mount(AdminLogin)

    await wrapper.find('input[type="text"]').setValue('admin')
    await wrapper.find('input[type="password"]').setValue('admin123')
    await wrapper.find('button.btn-login').trigger('click')

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining('/api/auth/admin-login'),
      { username: 'admin', password: 'admin123' }
    )

    expect(localStorage.getItem('adminToken')).toBe('mock-jwt-admin-token')
    expect(localStorage.getItem('adminRefreshToken')).toBe('mock-jwt-refresh-token')
    expect(localStorage.getItem('adminUser')).toBe(JSON.stringify(mockUser))
    expect(wrapper.emitted('login-success')).toBeTruthy()
    expect(wrapper.emitted('login-success')[0]).toEqual([mockUser])
  })

  it('triggers login on enter key in password input', async () => {
    axios.post.mockResolvedValueOnce({
      data: { token: 'token-from-enter' }
    })

    const wrapper = mount(AdminLogin)

    await wrapper.find('input[type="text"]').setValue('admin')
    const passwordInput = wrapper.find('input[type="password"]')
    await passwordInput.setValue('admin123')
    await passwordInput.trigger('keyup.enter')

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining('/api/auth/admin-login'),
      { username: 'admin', password: 'admin123' }
    )
    expect(localStorage.getItem('adminToken')).toBe('token-from-enter')
    expect(wrapper.emitted('login-success')).toBeTruthy()
  })

  it('displays error message when login fails', async () => {
    axios.post.mockRejectedValueOnce(new Error('Unauthorized'))

    const wrapper = mount(AdminLogin)

    await wrapper.find('input[type="text"]').setValue('admin')
    await wrapper.find('input[type="password"]').setValue('wrongPass')
    await wrapper.find('button.btn-login').trigger('click')

    expect(wrapper.text()).toContain('Credenciales inválidas.')
    expect(wrapper.emitted('login-success')).toBeFalsy()
  })

  it('disables submit button and shows loading text during login request', async () => {
    let resolvePromise
    axios.post.mockReturnValueOnce(
      new Promise(resolve => {
        resolvePromise = resolve
      })
    )

    const wrapper = mount(AdminLogin)

    await wrapper.find('input[type="text"]').setValue('admin')
    await wrapper.find('input[type="password"]').setValue('admin123')
    await wrapper.find('button.btn-login').trigger('click')

    const button = wrapper.find('button.btn-login')
    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toBe('Iniciando sesión...')

    resolvePromise({ data: { token: 'mock-token' } })
  })
})
