<script setup>
import { ref } from 'vue'
import axios from 'axios'
import { API_BASE_URL } from '../config'

const emit = defineEmits(['login-success'])
const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const login = async () => {
  error.value = ''
  loading.value = true
  try {
    const res = await axios.post(`${API_BASE_URL}/api/auth/admin-login`, {
      username: username.value,
      password: password.value
    })
    localStorage.setItem('adminToken', res.data.token)
    emit('login-success')
  } catch (err) {
    error.value = 'Credenciales inválidas.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-container">
    <!-- Animated Background -->
    <div class="animated-bg"></div>
    <!-- Overlay Gradient -->
    <div class="bg-overlay"></div>

    <div class="login-box">
      <div style="text-align: center; margin-bottom: 2rem;">
        <div style="background: #9f1239; width: 50px; height: 50px; border-radius: 12px; display: flex; align-items: center; justify-content: center; margin: 0 auto 1.5rem auto;">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>
        </div>
        <h1 style="color: #111827; margin-bottom: 0.5rem; font-size: 1.75rem; font-weight: 800;">Biblioteca Virtual y Especializada</h1>
        <p style="color: #6b7280; font-size: 0.875rem;">Panel de Administración FAMURP</p>
      </div>
      
      <div style="margin-bottom: 1.25rem;">
        <label style="color: #374151; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; display: block; margin-bottom: 6px;">Usuario</label>
        <input v-model="username" type="text" class="form-input" placeholder="Ingrese su usuario">
      </div>

      <div style="margin-bottom: 2rem;">
        <label style="color: #374151; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; display: block; margin-bottom: 6px;">Contraseña</label>
        <input v-model="password" type="password" class="form-input" placeholder="••••••••" @keyup.enter="login">
      </div>

      <button class="btn-login" @click="login" :disabled="loading">
        {{ loading ? 'Iniciando sesión...' : 'Ingresar al Panel' }}
      </button>

      <p v-if="error" style="color: #e11d48; margin-top: 1.25rem; text-align: center; font-size: 0.875rem; font-weight: 600;">{{ error }}</p>
      
      <div style="margin-top: 2.5rem; text-align: center; border-top: 1px solid rgba(0,0,0,0.08); padding-top: 1.5rem;">
        <p style="color: #64748b; font-size: 0.75rem; font-weight: 600;">© 2024 Facultad de Medicina Humana - URP</p>
        <p style="color: #cbd5e1; font-size: 0.6rem; margin-top: 6px; letter-spacing: 0.05em; text-transform: uppercase;">Designed & Developed by Claudio Quello</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  background: #0f172a;
}

.animated-bg {
  position: absolute;
  top: 0;
  left: 0;
  width: 130%;
  height: 100%;
  background-image: url('../assets/FondoAdmin.jpg');
  background-size: cover;
  background-position: center;
  animation: slideBg 40s linear infinite alternate;
  z-index: 1;
}

@keyframes slideBg {
  from { transform: translateX(0); }
  to { transform: translateX(-15%); }
}

.bg-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: linear-gradient(289deg, rgb(0 0 0 / 45%) 0%, rgb(0 0 0 / 45%) 100%);
  
  z-index: 2;  
}

.login-box {
  position: relative;
  z-index: 10;
  width: 100%;
  max-width: 380px;
  background: rgba(255, 255, 255, 0.55);
  backdrop-filter: blur(8px);  
  padding: 2.5rem;
  border-radius: 1.5rem;
  border: 1px solid rgba(255, 255, 255, 0.6);
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5), inset 0 0 20px rgba(255, 255, 255, 0.5);
}

.form-input {
  width: 100%;
  box-sizing: border-box;
  padding: 0.875rem 1.25rem;
  background: rgba(255, 255, 255, 0.9);
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  color: #1e293b;
  font-family: inherit;
  transition: all 0.3s ease;
}
.form-input:focus {
  outline: none;
  border-color: #9f1239;
  background: #ffffff;
  box-shadow: 0 0 0 4px rgba(159, 18, 57, 0.1);
}

.btn-login {
  width: 100%;
  padding: 1rem;
  font-weight: 700;
  font-size: 1.05rem;
  color: white;
  background: linear-gradient(135deg, #be123c 0%, #881337 100%);
  border: none;
  border-radius: 0.75rem;
  cursor: pointer;
  box-shadow: 0 10px 15px -3px rgba(159, 18, 57, 0.3), 0 4px 6px -4px rgba(159, 18, 57, 0.3);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.btn-login:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 20px 25px -5px rgba(159, 18, 57, 0.4), 0 8px 10px -6px rgba(159, 18, 57, 0.4);
}
.btn-login:active:not(:disabled) {
  transform: translateY(0);
}
.btn-login:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style>
