<script setup>
import { ref, onMounted, onUnmounted, defineEmits } from 'vue';
import { API_BASE_URL } from '../config.js';

const emit = defineEmits(['scanned', 'close']);
const videoRef = ref(null);
const canvasRef = ref(null);
const stream = ref(null);
const scanning = ref(true);
const statusMessage = ref('Inicializando cámara...');
let scanInterval = null;

const initCamera = async () => {
  try {
    stream.value = await navigator.mediaDevices.getUserMedia({ 
      video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } } 
    });
    if (videoRef.value) {
      videoRef.value.srcObject = stream.value;
    }
    statusMessage.value = 'Enfoca el carnet universitario en la cámara...';
    startAutoScanning();
  } catch (err) {
    statusMessage.value = 'Error al acceder a la cámara: ' + err.message;
    scanning.value = false;
  }
};

const stopCamera = () => {
  if (stream.value) {
    stream.value.getTracks().forEach(track => track.stop());
  }
  if (scanInterval) clearInterval(scanInterval);
};

const captureAndAnalyze = async () => {
  if (!scanning.value || !videoRef.value || !canvasRef.value) return;

  const video = videoRef.value;
  const canvas = canvasRef.value;
  const ctx = canvas.getContext('2d');

  // Calcular las coordenadas del rectángulo guía
  const vw = video.videoWidth;
  const vh = video.videoHeight;
  const sx = vw * 0.10;
  const sy = vh * 0.15;
  const sw = vw * 0.80;
  const sh = vh * 0.70;

  canvas.width = sw;
  canvas.height = sh;

  ctx.drawImage(video, sx, sy, sw, sh, 0, 0, sw, sh);

  try {
    statusMessage.value = 'Analizando imagen con Google Vision...';
    
    const base64Image = canvas.toDataURL('image/jpeg', 0.9);

    const response = await fetch(`${API_BASE_URL}/api/alumnos/scan-carnet`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ imageBase64: base64Image })
    });

    if (response.ok) {
      const extracted = await response.json();
      if (extracted && extracted.codigoUniversitario && extracted.nombres && extracted.apellidos) {
        scanning.value = false;
        stopCamera();
        emit('scanned', extracted);
        statusMessage.value = '¡Datos extraídos con éxito!';
      } else {
        statusMessage.value = 'Datos incompletos. Acerca más el carnet o mejora la iluminación...';
      }
    } else {
       statusMessage.value = 'Buscando datos legibles. Mantén el enfoque...';
    }
  } catch (error) {
    console.error("Error OCR:", error);
    statusMessage.value = 'Verificando conexión con el servidor...';
  }
};

const startAutoScanning = () => {
  // Escanear cada 2.5 segundos para no saturar la API gratuita y dar tiempo de respuesta
  scanInterval = setInterval(() => {
    if (scanning.value && videoRef.value?.readyState === 4) {
      captureAndAnalyze();
    }
  }, 2500);
};

onMounted(() => {
  initCamera();
});

onUnmounted(() => {
  stopCamera();
});
</script>

<template>
  <div class="camera-scanner-wrapper">
    <div class="scanner-card">
      <div class="scanner-header">
        <h3>Escanear Carnet Universitario</h3>
        <button class="close-btn" @click="emit('close')">×</button>
      </div>
      
      <div class="scanner-body">
        <div class="video-container">
          <video ref="videoRef" autoplay playsinline muted></video>
          <canvas ref="canvasRef" style="display: none;"></canvas>
          <div class="camera-overlay"></div>
          <div class="scan-guideline">
            <div class="corner top-left"></div>
            <div class="corner top-right"></div>
            <div class="corner bottom-left"></div>
            <div class="corner bottom-right"></div>
            <div class="scan-hint">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-bottom: 8px; opacity: 0.8;"><rect x="3" y="4" width="18" height="16" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
              <span>Alinea el carnet dentro de este recuadro</span>
            </div>
            <div class="scanning-laser"></div>
          </div>
        </div>
        <p class="status-message">{{ statusMessage }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.camera-scanner-wrapper {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(15, 23, 42, 0.7);
  backdrop-filter: blur(4px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
}

.scanner-card {
  background: white;
  border-radius: 16px;
  width: 100%;
  max-width: 500px;
  overflow: hidden;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  animation: popIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.scanner-header {
  padding: 1rem 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #f1f5f9;
  background: #f8fafc;
}

.scanner-header h3 {
  margin: 0;
  color: #0f172a;
  font-size: 1.1rem;
}

.close-btn {
  background: transparent;
  border: none;
  font-size: 1.5rem;
  color: #64748b;
  cursor: pointer;
}

.scanner-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.video-container {
  width: 100%;
  aspect-ratio: 4/3;
  background: #000;
  border-radius: 12px;
  overflow: hidden;
  position: relative;
}

video {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.scan-guideline {
  position: absolute;
  top: 15%; left: 10%; right: 10%; bottom: 15%;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 12px;
  pointer-events: none;
  box-shadow: 0 0 0 9999px rgba(0, 0, 0, 0.5); /* Crea el overlay oscuro alrededor */
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.scan-hint {
  color: white;
  text-align: center;
  font-size: 0.9rem;
  font-weight: 600;
  text-shadow: 0 2px 4px rgba(0,0,0,0.8);
  display: flex;
  flex-direction: column;
  align-items: center;
  z-index: 10;
}

.scanning-laser {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 2px;
  background: #10b981;
  box-shadow: 0 0 10px #10b981, 0 0 20px #10b981;
  animation: scanLaser 3s infinite linear;
}

@keyframes scanLaser {
  0% { top: 0; opacity: 0; }
  10% { opacity: 1; }
  90% { opacity: 1; }
  100% { top: 100%; opacity: 0; }
}

.corner {
  position: absolute;
  width: 30px; height: 30px;
  border-color: #10b981;
  border-style: solid;
  z-index: 5;
}

.top-left { top: -2px; left: -2px; border-width: 4px 0 0 4px; border-top-left-radius: 12px; }
.top-right { top: -2px; right: -2px; border-width: 4px 4px 0 0; border-top-right-radius: 12px; }
.bottom-left { bottom: -2px; left: -2px; border-width: 0 0 4px 4px; border-bottom-left-radius: 12px; }
.bottom-right { bottom: -2px; right: -2px; border-width: 0 4px 4px 0; border-bottom-right-radius: 12px; }

.status-message {
  margin: 0;
  color: #475569;
  font-weight: 500;
  font-size: 0.9rem;
  text-align: center;
}

@keyframes popIn {
  from { opacity: 0; transform: scale(0.95); }
  to { opacity: 1; transform: scale(1); }
}
</style>
