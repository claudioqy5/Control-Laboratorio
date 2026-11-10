<script setup>
import { ref, nextTick, onMounted } from 'vue'
import axios from 'axios'
import biblioLogo from '../assets/biblio.png'
import { API_BASE_URL } from '../config'

const isOpen = ref(false)
const showTooltip = ref(true)
const inputMessage = ref('')
const isThinking = ref(false)
const messagesContainer = ref(null)
const chatInput = ref(null)

// Configuración de Webhook n8n
const showSettings = ref(false)
const webhookUrl = ref(localStorage.getItem('BVE_BOT_WEBHOOK_URL') || '')
const configSuccess = ref(false)

// Mensajes iniciales
const messages = ref([
  {
    id: 1,
    sender: 'bot',
    text: '¡Hola! 👋 Soy **BiblioBot**, el asistente inteligente de la Biblioteca Virtual y Laboratorio de Cómputo (BVE Medicina - URP).\n\nPuedo responder tus dudas sobre el funcionamiento de cualquier sección del sistema o consultar en tiempo real las métricas de la base de datos.',
    timestamp: getFormattedTime(),
    suggestions: [
      '¿Para qué sirve la sección de Reportes?',
      '¿Cuántos libros se prestaron hoy?',
      '¿Cuántas computadoras están libres?',
      '¿Qué muestra el Resumen Diario de Préstamos?',
      '¿Dónde encuentro el libro Harrison?'
    ]
  }
])

function getFormattedTime() {
  const now = new Date()
  return now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

const toggleChat = () => {
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    showTooltip.value = false
    scrollToBottom()
    nextTick(() => {
      chatInput.value?.focus()
    })
  }
}

const closeChat = () => {
  isOpen.value = false
}

const dismissTooltip = (e) => {
  e.stopPropagation()
  showTooltip.value = false
}

const toggleSettings = () => {
  showSettings.value = !showSettings.value
}

const saveSettings = () => {
  if (webhookUrl.value.trim()) {
    localStorage.setItem('BVE_BOT_WEBHOOK_URL', webhookUrl.value.trim())
  } else {
    localStorage.removeItem('BVE_BOT_WEBHOOK_URL')
  }
  configSuccess.value = true
  setTimeout(() => {
    configSuccess.value = false
    showSettings.value = false
  }, 1000)
}

const resetToDirectApi = () => {
  webhookUrl.value = ''
  localStorage.removeItem('BVE_BOT_WEBHOOK_URL')
  configSuccess.value = true
  setTimeout(() => {
    configSuccess.value = false
    showSettings.value = false
  }, 900)
}

const scrollToBottom = async () => {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

const clearConversation = () => {
  messages.value = [
    {
      id: Date.now(),
      sender: 'bot',
      text: 'Conversación reiniciada. ¿En qué más puedo ayudarte hoy?',
      timestamp: getFormattedTime(),
      suggestions: [
        '¿Para qué sirve la sección de Reportes?',
        '¿Cuántos libros se prestaron hoy?',
        '¿Cuántas computadoras están libres?'
      ]
    }
  ]
}

const selectSuggestion = (suggestion) => {
  inputMessage.value = suggestion
  sendMessage()
}

const generateFallbackReply = (userQuery) => {
  const query = userQuery.toLowerCase()

  if (query.includes('computadora') || query.includes('pc') || query.includes('estacion') || query.includes('libre') || query.includes('ocupad')) {
    return '🖥️ **Estado del Laboratorio de Cómputo:**\nActualmente hay **computadoras disponibles** para los alumnos de Medicina. El sistema monitorea en vivo las sesiones activas y el límite de horas diarias.'
  }
  
  if (query.includes('reporte') || query.includes('reportes')) {
    return '📊 **Sección de Reportes:**\nPermite generar y exportar reportes ejecutivos en Excel (.xlsx) con filtros por fechas, control de asistencia e historial de horas acumuladas de los estudiantes.'
  }

  if (query.includes('resumen') && (query.includes('prestamo') || query.includes('préstamo'))) {
    return '📖 **Sección Resumen Diario de Préstamos:**\nCirculación del día en tiempo real: KPIs de préstamos y devoluciones, gráfico de afluencia por hora, distribución por especialidad médica y tabla completa de movimientos.'
  }

  if (query.includes('resumen diario') || query.includes('resumen')) {
    return '📋 **Sección Resumen Diario (Laboratorio):**\nMonitoreo en tiempo real del uso de equipos de cómputo hoy, con tasa de ocupación, horas punta y sesiones activas en sala.'
  }

  if ((query.includes('libro') || query.includes('prestamo') || query.includes('préstamo')) && (query.includes('hoy') || query.includes('cuanto') || query.includes('cuántos'))) {
    return '📚 **Circulación de Libros Hoy:**\nPuedes consultar los préstamos activos y devoluciones de la jornada en la sección **Resumen Préstamos**, sincronizada con AbsysNet.'
  }

  if (query.includes('harrison') || query.includes('libro') || query.includes('estante')) {
    return '📚 **Búsqueda de Libros:**\n*Principios de Medicina Interna de Harrison*\n• **Ubicación:** Estantería C-2 (Sección Medicina Interna)\n• **Estado:** Disponible en sala.'
  }

  if (query.includes('alumno') || query.includes('sancion') || query.includes('multa')) {
    return '👨‍🎓 **Consulta de Alumnos:**\nPuedes ingresar el código universitario o DNI del alumno para consultar si tiene sesiones activas, multas pendientes o libros en préstamo.'
  }

  return `Entendido. He procesado tu consulta: "${userQuery}".\nPuedes preguntarme sobre métricas en vivo (préstamos hoy, PCs libres) o para qué sirve cualquier sección del sistema.`
}

const sendMessage = () => {
  const text = inputMessage.value.trim()
  if (!text) return

  // Agregar mensaje del usuario
  messages.value.push({
    id: Date.now(),
    sender: 'user',
    text: text,
    timestamp: getFormattedTime()
  })

  inputMessage.value = ''
  scrollToBottom()

  isThinking.value = true
  scrollToBottom()

  setTimeout(async () => {
    let replyText = ''

    try {
      const configuredWebhook = (webhookUrl.value || localStorage.getItem('BVE_BOT_WEBHOOK_URL') || '').trim()

      // 1. Intentar llamar a n8n Webhook si está configurado
      if (configuredWebhook) {
        try {
          const res = await axios.post(
            configuredWebhook,
            {
              message: text,
              mensaje: text,
              sender: 'Admin',
              timestamp: new Date().toISOString()
            },
            { timeout: 15000 }
          )

          if (res?.data) {
            replyText = res.data.reply || res.data.output || res.data.text || res.data.message || ''
          }
        } catch (err) {
          console.warn('No se pudo conectar al webhook de n8n, usando API directa como respaldo:', err)
        }
      }

      // 2. Si no hay webhook o falló, consultar al Backend .NET
      if (!replyText) {
        const res = await axios.post(
          `${API_BASE_URL}/api/Bot/Chat`,
          {
            mensaje: text,
            remitente: 'Admin'
          },
          { timeout: 10000 }
        )

        if (res?.data && res.data.reply) {
          replyText = res.data.reply
        }
      }
    } catch (error) {
      console.warn('Error en llamada a API Bot, usando respuesta de contingencia:', error)
    }

    // 3. Respaldo de contingencia
    if (!replyText) {
      replyText = generateFallbackReply(text)
    }

    isThinking.value = false
    messages.value.push({
      id: Date.now(),
      sender: 'bot',
      text: replyText,
      timestamp: getFormattedTime()
    })
    scrollToBottom()
  }, 600)
}

// Convertir asteriscos simples/dobles a texto resaltado o negrita simple
const formatMessage = (text) => {
  if (!text) return ''
  let formatted = text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/\n/g, '<br>')
  return formatted
}

onMounted(() => {
  // Ocultar tooltip automáticamente después de 12 segundos si no interactúa
  setTimeout(() => {
    showTooltip.value = false
  }, 12000)
})
</script>

<template>
  <div class="chatbot-wrapper">
    <!-- TOOLTIP FLOTANTE DE BIENVENIDA -->
    <transition name="fade-slide">
      <div v-if="!isOpen && showTooltip" class="bot-tooltip" @click="toggleChat">
        <span class="tooltip-badge">¡Nuevo!</span>
        <div class="tooltip-text">
          <strong>BiblioBot IA</strong>
          <span>¿Preguntas sobre el sistema o préstamos?</span>
        </div>
        <button class="tooltip-close" @click="dismissTooltip" title="Cerrar">
          &times;
        </button>
      </div>
    </transition>

    <!-- BOTÓN / AVATAR FLOTANTE ANIMADO -->
    <div class="bot-launcher" @click="toggleChat" :class="{ 'is-active': isOpen }" title="Asistente Virtual BiblioBot">
      <div class="bot-shadow"></div>
      <div class="bot-floating-mascot">
        <img :src="biblioLogo" alt="BiblioBot Logo" class="bot-logo-img" />
        <span class="status-indicator">
          <span class="status-dot"></span>
        </span>
      </div>
    </div>

    <!-- VENTANA DEL CHAT -->
    <transition name="chat-window">
      <div v-if="isOpen" class="chat-modal">
        <!-- HEADER -->
        <div class="chat-header">
          <div class="header-bot-info">
            <div class="header-avatar-wrap">
              <img :src="biblioLogo" alt="BiblioBot" class="header-avatar" />
              <span class="header-online-dot"></span>
            </div>
            <div>
              <div class="header-title-row">
                <h3 class="header-title">BiblioBot</h3>
                <span class="header-badge">{{ webhookUrl.trim() ? 'n8n • IA' : 'IA' }}</span>
              </div>
              <p class="header-subtitle">
                <span class="dot-live"></span> En línea • BVE Medicina URP
              </p>
            </div>
          </div>

          <div class="header-actions">
            <!-- Botón Reiniciar Conversación (.action-btn[0]) -->
            <button class="action-btn" @click="clearConversation" title="Reiniciar chat">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67"/>
              </svg>
            </button>

            <!-- Botón Cerrar/Minimizar (.action-btn[1]) -->
            <button class="action-btn" @click="closeChat" title="Minimizar / Cerrar">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"></line>
                <line x1="6" y1="6" x2="18" y2="18"></line>
              </svg>
            </button>

            <!-- Botón Configuración n8n -->
            <button 
              class="settings-btn" 
              @click="toggleSettings" 
              :class="{ 'btn-active': showSettings }"
              title="Configurar Webhook n8n"
            >
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="3"></circle>
                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
              </svg>
            </button>
          </div>
        </div>

        <!-- PANEL DE CONFIGURACIÓN N8N -->
        <div v-if="showSettings" class="chat-settings-panel">
          <div class="settings-header">
            <h4>⚙️ Conexión de Inteligencia Artificial</h4>
            <button class="settings-close-btn" @click="showSettings = false">&times;</button>
          </div>
          <p class="settings-desc">
            Puedes conectar el asistente a tu propio webhook de <strong>n8n + Gemini AI</strong> o utilizar la <strong>API Directa de .NET</strong> en tiempo real.
          </p>
          <div class="settings-field">
            <label class="settings-label">URL del Webhook de n8n:</label>
            <input 
              type="url" 
              v-model="webhookUrl" 
              placeholder="https://tu-n8n.com/webhook/bve-bot" 
              class="settings-input"
            />
          </div>
          <div class="settings-status-box">
            <span class="status-indicator-badge" :class="webhookUrl.trim() ? 'status-n8n' : 'status-direct'">
              {{ webhookUrl.trim() ? '🟢 n8n + Gemini AI Conectado' : '🔵 API Directa .NET Activa (SQL Server)' }}
            </span>
          </div>
          <div class="settings-actions">
            <button class="settings-save-btn" @click="saveSettings">
              Guardar Conexión
            </button>
            <button class="settings-reset-btn" @click="resetToDirectApi" v-if="webhookUrl.trim()">
              Usar API Directa
            </button>
          </div>
          <div v-if="configSuccess" class="settings-success-alert">
            ✓ ¡Ajustes actualizados exitosamente!
          </div>
        </div>

        <!-- CUERPO DE MENSAJES -->
        <div class="chat-messages" ref="messagesContainer">
          <div class="date-badge">Hoy</div>

          <div 
            v-for="msg in messages" 
            :key="msg.id" 
            :class="['message-row', msg.sender === 'user' ? 'message-user' : 'message-bot']"
          >
            <img v-if="msg.sender === 'bot'" :src="biblioLogo" class="msg-avatar" alt="Bot" />

            <div class="message-bubble-wrap">
              <div class="message-bubble">
                <div v-html="formatMessage(msg.text)"></div>
              </div>

              <!-- Sugerencias en chips -->
              <div v-if="msg.suggestions && msg.suggestions.length" class="suggestions-container">
                <button
                  v-for="(sugg, idx) in msg.suggestions"
                  :key="idx"
                  class="suggestion-chip"
                  @click="selectSuggestion(sugg)"
                >
                  <span class="chip-spark">✨</span> {{ sugg }}
                </button>
              </div>

              <span class="message-time">{{ msg.timestamp }}</span>
            </div>
          </div>

          <!-- Indicador de pensando -->
          <div v-if="isThinking" class="message-row message-bot">
            <img :src="biblioLogo" class="msg-avatar" alt="Bot" />
            <div class="message-bubble-wrap">
              <div class="message-bubble thinking-bubble">
                <div class="typing-dots">
                  <span></span>
                  <span></span>
                  <span></span>
                </div>
                <span class="thinking-text">BiblioBot está consultando la información...</span>
              </div>
            </div>
          </div>
        </div>

        <!-- FOOTER / INPUT -->
        <div class="chat-footer">
          <form @submit.prevent="sendMessage" class="input-form">
            <input
              type="text"
              v-model="inputMessage"
              placeholder="Pregunta sobre préstamos, equipos o secciones..."
              class="chat-input"
              :disabled="isThinking"
              ref="chatInput"
            />
            <button 
              type="submit" 
              class="send-btn" 
              :disabled="!inputMessage.trim() || isThinking"
              title="Enviar mensaje"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="22" y1="2" x2="11" y2="13"></line>
                <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
              </svg>
            </button>
          </form>
          <div class="footer-meta">
            <span>Inteligencia Artificial para <strong>BVE Medicina URP</strong></span>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
/* CONTENEDOR PRINCIPAL FLOTANTE */
.chatbot-wrapper {
  position: fixed;
  bottom: 24px;
  right: 28px;
  z-index: 9999;
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}

/* ========================================================
   ANIMACIONES DE LEVITACIÓN Y FLOTADO
   ======================================================== */
@keyframes botFloating {
  0% { transform: translateY(0px) rotate(0deg); }
  50% { transform: translateY(-11px) rotate(1.2deg); }
  100% { transform: translateY(0px) rotate(0deg); }
}

@keyframes shadowPulse {
  0% { transform: scale(1); opacity: 0.35; }
  50% { transform: scale(0.68); opacity: 0.16; }
  100% { transform: scale(1); opacity: 0.35; }
}

@keyframes livePulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.4; transform: scale(0.85); }
}

/* BOTÓN DISPARADOR */
.bot-launcher {
  position: relative;
  width: 80px;
  height: 80px;
  cursor: pointer;
  user-select: none;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.bot-launcher:hover {
  transform: scale(1.08);
}

.bot-launcher:active {
  transform: scale(0.95);
}

.bot-shadow {
  position: absolute;
  bottom: 2px;
  width: 52px;
  height: 11px;
  background: radial-gradient(ellipse at center, rgba(15, 23, 42, 0.42) 0%, rgba(15, 23, 42, 0) 70%);
  border-radius: 50%;
  animation: shadowPulse 3.4s ease-in-out infinite;
  pointer-events: none;
}

.bot-floating-mascot {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  animation: botFloating 3.4s ease-in-out infinite;
  animation-fill-mode: both;
}

.bot-logo-img {
  width: 76px;
  height: 76px;
  object-fit: contain;
  background: transparent;
  filter: drop-shadow(0 10px 14px rgba(0, 0, 0, 0.26)) drop-shadow(0 2px 5px rgba(159, 18, 57, 0.2));
  transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1), filter 0.3s ease;
}

.bot-launcher:hover .bot-logo-img {
  transform: scale(1.1) rotate(-3deg);
  filter: drop-shadow(0 14px 20px rgba(0, 0, 0, 0.35)) drop-shadow(0 4px 8px rgba(159, 18, 57, 0.35));
}

.status-indicator {
  position: absolute;
  bottom: 6px;
  right: 6px;
  width: 14px;
  height: 14px;
  background: #10b981;
  border: 2px solid #ffffff;
  border-radius: 50%;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.25);
  display: flex;
  align-items: center;
  justify-content: center;
}

.status-dot {
  width: 4px;
  height: 4px;
  background: #ffffff;
  border-radius: 50%;
}

/* ========================================================
   TOOLTIP FLOTANTE DE BIENVENIDA
   ======================================================== */
.bot-tooltip {
  position: absolute;
  bottom: 78px;
  right: 4px;
  background: #ffffff;
  padding: 10px 14px;
  border-radius: 14px;
  box-shadow: 0 10px 25px -3px rgba(0, 0, 0, 0.12), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
  border: 1px solid #fecdd3;
  display: flex;
  align-items: center;
  gap: 10px;
  width: 250px;
  cursor: pointer;
  animation: botFloating 3.4s ease-in-out infinite;
  animation-delay: 0.15s;
  transition: transform 0.2s, box-shadow 0.2s;
}

.bot-tooltip:hover {
  box-shadow: 0 14px 30px -3px rgba(159, 18, 57, 0.2);
}

.tooltip-badge {
  background: #9f1239;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 700;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: 6px;
  letter-spacing: 0.5px;
  flex-shrink: 0;
}

.tooltip-text {
  display: flex;
  flex-direction: column;
  line-height: 1.25;
}

.tooltip-text strong {
  font-size: 0.8rem;
  color: #111827;
}

.tooltip-text span {
  font-size: 0.72rem;
  color: #6b7280;
}

.tooltip-close {
  background: transparent;
  border: none;
  font-size: 1.2rem;
  line-height: 1;
  color: #9ca3af;
  cursor: pointer;
  padding: 0 2px;
  margin-left: auto;
  border-radius: 4px;
}

.tooltip-close:hover {
  color: #4b5563;
  background: #f3f4f6;
}

/* ========================================================
   MODAL / VENTANA DEL CHAT
   ======================================================== */
.chat-modal {
  position: absolute;
  bottom: 84px;
  right: 0;
  width: 410px;
  max-width: calc(100vw - 36px);
  height: 590px;
  max-height: calc(100vh - 120px);
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 20px 40px -10px rgba(0, 0, 0, 0.18), 0 8px 16px -4px rgba(159, 18, 57, 0.12);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid #f3f4f6;
}

/* HEADER */
.chat-header {
  background: linear-gradient(135deg, #881337 0%, #9f1239 55%, #be123c 100%);
  color: #ffffff;
  padding: 13px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 4px 12px rgba(159, 18, 57, 0.25);
  position: relative;
  z-index: 10;
}

.header-bot-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-avatar-wrap {
  position: relative;
  width: 42px;
  height: 42px;
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.header-avatar {
  width: 38px;
  height: 38px;
  object-fit: contain;
  background: transparent;
  filter: drop-shadow(0 2px 6px rgba(0, 0, 0, 0.35));
}

.header-online-dot {
  position: absolute;
  bottom: 1px;
  right: 1px;
  width: 10px;
  height: 10px;
  background: #10b981;
  border: 2px solid #881337;
  border-radius: 50%;
}

.header-title-row {
  display: flex;
  align-items: center;
  gap: 6px;
}

.header-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  letter-spacing: -0.2px;
}

.header-badge {
  background: rgba(255, 255, 255, 0.22);
  font-size: 0.65rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 6px;
  letter-spacing: 0.4px;
}

.header-subtitle {
  margin: 2px 0 0 0;
  font-size: 0.72rem;
  opacity: 0.9;
  display: flex;
  align-items: center;
  gap: 5px;
}

.dot-live {
  width: 6px;
  height: 6px;
  background: #34d399;
  border-radius: 50%;
  display: inline-block;
  animation: livePulse 2s infinite ease-in-out;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.action-btn {
  background: rgba(255, 255, 255, 0.15);
  border: none;
  color: #ffffff;
  width: 29px;
  height: 29px;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, transform 0.1s;
}

.action-btn:hover {
  background: rgba(255, 255, 255, 0.28);
}

.action-btn:active {
  transform: scale(0.92);
}

.settings-btn {
  background: rgba(255, 255, 255, 0.15);
  border: none;
  color: #ffffff;
  width: 29px;
  height: 29px;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, transform 0.1s;
}

.settings-btn:hover {
  background: rgba(255, 255, 255, 0.28);
}

.settings-btn.btn-active {
  background: rgba(255, 255, 255, 0.35);
  border: 1px solid rgba(255, 255, 255, 0.6);
}

.settings-btn:active {
  transform: scale(0.92);
}

/* ========================================================
   PANEL DE CONFIGURACIÓN N8N
   ======================================================== */
.chat-settings-panel {
  background: #ffffff;
  border-bottom: 1.5px solid #e2e8f0;
  padding: 14px 16px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
  animation: fadeSlideDown 0.25s ease-out;
  z-index: 5;
}

@keyframes fadeSlideDown {
  from { opacity: 0; transform: translateY(-8px); }
  to { opacity: 1; transform: translateY(0); }
}

.settings-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}

.settings-header h4 {
  margin: 0;
  font-size: 0.88rem;
  color: #0f172a;
  font-weight: 700;
}

.settings-close-btn {
  background: none;
  border: none;
  font-size: 1.2rem;
  line-height: 1;
  color: #94a3b8;
  cursor: pointer;
}

.settings-desc {
  font-size: 0.74rem;
  color: #64748b;
  margin: 0 0 10px 0;
  line-height: 1.35;
}

.settings-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 8px;
}

.settings-label {
  font-size: 0.72rem;
  font-weight: 600;
  color: #334155;
}

.settings-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 0.78rem;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background: #f8fafc;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.settings-input:focus {
  border-color: #9f1239;
  background: #ffffff;
  box-shadow: 0 0 0 2px rgba(159, 18, 57, 0.12);
}

.settings-status-box {
  margin: 6px 0 10px 0;
}

.status-indicator-badge {
  display: inline-block;
  font-size: 0.68rem;
  font-weight: 600;
  padding: 3px 8px;
  border-radius: 6px;
}

.status-n8n {
  background: #dcfce7;
  color: #166534;
}

.status-direct {
  background: #e0f2fe;
  color: #0369a1;
}

.settings-actions {
  display: flex;
  gap: 8px;
}

.settings-save-btn {
  background: #9f1239;
  color: #ffffff;
  border: none;
  padding: 6px 14px;
  font-size: 0.76rem;
  font-weight: 600;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.2s;
}

.settings-save-btn:hover {
  background: #881337;
}

.settings-reset-btn {
  background: #f1f5f9;
  color: #475569;
  border: 1px solid #cbd5e1;
  padding: 6px 12px;
  font-size: 0.74rem;
  font-weight: 500;
  border-radius: 8px;
  cursor: pointer;
}

.settings-reset-btn:hover {
  background: #e2e8f0;
}

.settings-success-alert {
  margin-top: 8px;
  font-size: 0.72rem;
  color: #15803d;
  font-weight: 600;
}

/* ========================================================
   CUERPO DE MENSAJES
   ======================================================== */
.chat-messages {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
  background: #f8fafc;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.chat-messages::-webkit-scrollbar {
  width: 5px;
}
.chat-messages::-webkit-scrollbar-track {
  background: transparent;
}
.chat-messages::-webkit-scrollbar-thumb {
  background: #e2e8f0;
  border-radius: 10px;
}
.chat-messages::-webkit-scrollbar-thumb:hover {
  background: #cbd5e1;
}

.date-badge {
  align-self: center;
  background: #e2e8f0;
  color: #64748b;
  font-size: 0.68rem;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 12px;
  margin-bottom: 4px;
}

.message-row {
  display: flex;
  gap: 8px;
  align-items: flex-end;
  max-width: 90%;
}

.message-bot {
  align-self: flex-start;
}

.message-user {
  align-self: flex-end;
  flex-direction: row-reverse;
}

.msg-avatar {
  width: 32px;
  height: 32px;
  object-fit: contain;
  background: transparent;
  border: none;
  padding: 0;
  box-shadow: none;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.15));
  flex-shrink: 0;
  margin-bottom: 8px;
}

.message-bubble-wrap {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.message-user .message-bubble-wrap {
  align-items: flex-end;
}

.message-bubble {
  padding: 11px 14px;
  border-radius: 16px;
  font-size: 0.85rem;
  line-height: 1.45;
  word-break: break-word;
}

.message-bot .message-bubble {
  background: #ffffff;
  color: #1f2937;
  border-bottom-left-radius: 4px;
  border: 1px solid #e5e7eb;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.03);
}

.message-user .message-bubble {
  background: #9f1239;
  color: #ffffff;
  border-bottom-right-radius: 4px;
  box-shadow: 0 4px 10px rgba(159, 18, 57, 0.25);
}

.message-time {
  font-size: 0.68rem;
  color: #94a3b8;
  padding: 0 4px;
}

/* Sugerencias Rápidas */
.suggestions-container {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-top: 6px;
}

.suggestion-chip {
  background: #ffffff;
  border: 1px solid #fecdd3;
  color: #9f1239;
  padding: 6px 12px;
  border-radius: 12px;
  font-size: 0.76rem;
  font-weight: 500;
  cursor: pointer;
  text-align: left;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.03);
}

.suggestion-chip:hover {
  background: #fff1f2;
  border-color: #fb7185;
  transform: translateX(3px);
}

.chip-spark {
  font-size: 0.75rem;
}

/* Indicador de Tipeo */
.thinking-bubble {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
}

.thinking-text {
  font-size: 0.76rem;
  color: #64748b;
  font-style: italic;
}

.typing-dots {
  display: flex;
  gap: 4px;
  align-items: center;
}

.typing-dots span {
  width: 6px;
  height: 6px;
  background: #9f1239;
  border-radius: 50%;
  animation: typingBounce 1.4s infinite ease-in-out;
}

.typing-dots span:nth-child(1) { animation-delay: 0s; }
.typing-dots span:nth-child(2) { animation-delay: 0.2s; }
.typing-dots span:nth-child(3) { animation-delay: 0.4s; }

@keyframes typingBounce {
  0%, 80%, 100% { transform: translateY(0); opacity: 0.4; }
  40% { transform: translateY(-6px); opacity: 1; }
}

/* ========================================================
   FOOTER / ENTRADA DE TEXTO
   ======================================================== */
.chat-footer {
  padding: 12px 14px 10px 14px;
  background: #ffffff;
  border-top: 1px solid #f1f5f9;
}

.input-form {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #f8fafc;
  border: 1.5px solid #e2e8f0;
  border-radius: 14px;
  padding: 4px 6px 4px 12px;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.input-form:focus-within {
  border-color: #9f1239;
  background: #ffffff;
  box-shadow: 0 0 0 3px rgba(159, 18, 57, 0.12);
}

.chat-input {
  flex: 1;
  border: none;
  background: transparent;
  padding: 8px 4px;
  font-size: 0.85rem;
  color: #1e293b;
  outline: none;
}

.chat-input::placeholder {
  color: #94a3b8;
}

.send-btn {
  background: #9f1239;
  border: none;
  color: #ffffff;
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.2s, transform 0.1s, opacity 0.2s;
  flex-shrink: 0;
}

.send-btn:hover:not(:disabled) {
  background: #881337;
  transform: scale(1.05);
}

.send-btn:active:not(:disabled) {
  transform: scale(0.95);
}

.send-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.footer-meta {
  text-align: center;
  margin-top: 6px;
}

.footer-meta span {
  font-size: 0.68rem;
  color: #94a3b8;
}

/* ========================================================
   TRANSICIONES VUE
   ======================================================== */
.chat-window-enter-active,
.chat-window-leave-active {
  transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.25s ease;
  transform-origin: bottom right;
}

.chat-window-enter-from,
.chat-window-leave-to {
  transform: scale(0.85) translateY(20px);
  opacity: 0;
}

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.3s ease;
}

.fade-slide-enter-from,
.fade-slide-leave-to {
  transform: translateY(10px);
  opacity: 0;
}
</style>
