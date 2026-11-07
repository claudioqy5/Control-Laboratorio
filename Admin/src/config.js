import axios from 'axios'

export const API_BASE_URL = 
  import.meta.env?.VITE_API_URL || 
  'https://bvefamurp.helifyferdigital.cloud'

axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('adminToken')
    if (token) {
      config.headers = config.headers || {}
      config.headers.Authorization = 'Bearer ' + token
    }
    return config
  },
  (error) => Promise.reject(error)
)

axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    const url = originalRequest?.url || ''
    const isAuthUrl = url.includes('/api/auth/admin-login') || url.includes('/api/auth/refresh-token')

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isAuthUrl) {
      const refreshToken = localStorage.getItem('adminRefreshToken')
      if (refreshToken) {
        originalRequest._retry = true
        try {
          const res = await axios.post(`${API_BASE_URL}/api/auth/refresh-token`, { refreshToken })
          const nuevoToken = res.data?.token
          if (nuevoToken) {
            localStorage.setItem('adminToken', nuevoToken)
          }
          if (res.data?.refreshToken) {
            localStorage.setItem('adminRefreshToken', res.data.refreshToken)
          }
          if (res.data?.usuario) {
            localStorage.setItem('adminUser', JSON.stringify(res.data.usuario))
          }
          originalRequest.headers = originalRequest.headers || {}
          originalRequest.headers.Authorization = 'Bearer ' + nuevoToken
          return axios(originalRequest)
        } catch (refreshError) {
          localStorage.removeItem('adminToken')
          localStorage.removeItem('adminRefreshToken')
          localStorage.removeItem('adminUser')
          return Promise.reject(refreshError)
        }
      }
    }
    return Promise.reject(error)
  }
)
