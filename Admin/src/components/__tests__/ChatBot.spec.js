import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import ChatBot from '../ChatBot.vue'

vi.mock('axios')

describe('ChatBot', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.clearAllMocks()
    axios.post = vi.fn().mockResolvedValue({
      data: {
        reply: '🖥️ **Estado del Laboratorio:**\nActualmente hay computadoras disponibles.'
      }
    })
  })

  it('renders launcher button with logo image', () => {
    const wrapper = mount(ChatBot)
    const logoImg = wrapper.find('.bot-logo-img')
    expect(logoImg.exists()).toBe(true)
    expect(wrapper.find('.bot-launcher').exists()).toBe(true)
  })

  it('toggles chat window when launcher is clicked', async () => {
    const wrapper = mount(ChatBot)
    expect(wrapper.find('.chat-modal').exists()).toBe(false)

    await wrapper.find('.bot-launcher').trigger('click')
    expect(wrapper.find('.chat-modal').exists()).toBe(true)
    expect(wrapper.text()).toContain('BiblioBot')

    // Close button (.action-btn[1])
    const closeBtn = wrapper.findAll('.action-btn')[1]
    await closeBtn.trigger('click')
    expect(wrapper.find('.chat-modal').exists()).toBe(false)
  })

  it('allows typing and sending a message with animated reply', async () => {
    const wrapper = mount(ChatBot)
    await wrapper.find('.bot-launcher').trigger('click')

    const input = wrapper.find('.chat-input')
    await input.setValue('¿Hay computadoras disponibles?')
    await wrapper.find('.input-form').trigger('submit')

    expect(wrapper.text()).toContain('¿Hay computadoras disponibles?')
    expect(wrapper.find('.thinking-bubble').exists()).toBe(true)

    // Advance timers for simulated bot reply
    vi.advanceTimersByTime(1500)
    await flushPromises()

    expect(wrapper.find('.thinking-bubble').exists()).toBe(false)
    expect(wrapper.text()).toContain('computadoras disponibles')
  })

  it('restarts conversation when clear button is clicked', async () => {
    const wrapper = mount(ChatBot)
    await wrapper.find('.bot-launcher').trigger('click')

    const clearBtn = wrapper.findAll('.action-btn')[0]
    await clearBtn.trigger('click')

    expect(wrapper.text()).toContain('Conversación reiniciada')
  })

  it('toggles n8n settings panel and saves webhook URL', async () => {
    const wrapper = mount(ChatBot)
    await wrapper.find('.bot-launcher').trigger('click')

    // Settings panel hidden initially
    expect(wrapper.find('.chat-settings-panel').exists()).toBe(false)

    // Click settings button
    await wrapper.find('.settings-btn').trigger('click')
    expect(wrapper.find('.chat-settings-panel').exists()).toBe(true)

    // Set webhook url
    const urlInput = wrapper.find('.settings-input')
    await urlInput.setValue('https://mi-n8n.cloud/webhook/bve-bot')
    await wrapper.find('.settings-save-btn').trigger('click')

    expect(localStorage.getItem('BVE_BOT_WEBHOOK_URL')).toBe('https://mi-n8n.cloud/webhook/bve-bot')
  })

  it('responds with system module description for reportes', async () => {
    axios.post = vi.fn().mockResolvedValue({
      data: {
        reply: '📊 **Sección de Reportes:** Permite generar y descargar reportes oficiales en Excel.'
      }
    })

    const wrapper = mount(ChatBot)
    await wrapper.find('.bot-launcher').trigger('click')

    const input = wrapper.find('.chat-input')
    await input.setValue('¿Para qué sirve la sección de reportes?')
    await wrapper.find('.input-form').trigger('submit')

    vi.advanceTimersByTime(1500)
    await flushPromises()

    expect(wrapper.text()).toContain('Sección de Reportes')
  })
})
