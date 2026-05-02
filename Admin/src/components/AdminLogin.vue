<script setup>
import { ref } from 'vue'
import axios from 'axios'

const emit = defineEmits(['login-success'])
const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const login = async () => {
  error.value = ''
  loading.value = true
  try {
    const res = await axios.post('https://localhost:7215/api/auth/admin-login', {
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
        <label style="color: #374151; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em;">Usuario</label>
        <input v-model="username" type="text" class="form-input" placeholder="nombre.apellido" style="width:100%; margin-top: 6px;">
      </div>

      <div style="margin-bottom: 2rem;">
        <label style="color: #374151; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em;">Contraseña</label>
        <input v-model="password" type="password" class="form-input" placeholder="••••••••" style="width:100%; margin-top: 6px;" @keyup.enter="login">
      </div>

      <button class="btn btn-primary" style="width: 100%; padding: 0.875rem; font-weight: 700; font-size: 1rem; border-radius: 0.75rem; box-shadow: 0 4px 6px -1px rgba(159, 18, 57, 0.2);" @click="login" :disabled="loading">
        {{ loading ? 'Iniciando sesión...' : 'Ingresar al Panel' }}
      </button>

      <p v-if="error" style="color: #e11d48; margin-top: 1.25rem; text-align: center; font-size: 0.875rem; font-weight: 600;">{{ error }}</p>
      
      <div style="margin-top: 2rem; text-align: center; border-top: 1px solid #f3f4f6; padding-top: 1.5rem;">
        <p style="color: #9ca3af; font-size: 0.75rem;">© 2024 Facultad de Medicina Humana - URP</p>
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
  max-width: 420px;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  padding: 3rem 2.5rem;
  border-radius: 1.5rem;
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
}

.form-input {
  padding: 0.875rem 1rem;
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 0.75rem;
  color: #111827;
  font-family: inherit;
  transition: all 0.2s;
}
.form-input:focus {
  outline: none;
  border-color: #9f1239;
  box-shadow: 0 0 0 4px rgba(159, 18, 57, 0.1);
}
</style>
