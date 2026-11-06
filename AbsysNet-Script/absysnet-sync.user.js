// ==UserScript==
// @name         AbsysNet -> Control Laboratorio & Biblioteca URP Sync
// @namespace    https://medicina.urp.edu.pe/bve
// @version      1.0.0
// @description  Captura automática de préstamos y devoluciones en AbsysNet y sincronización en tiempo real con Control-Laboratorio.
// @author       Facultad de Medicina Humana - URP
// @match        https://biblioteca.urp.edu.pe/abnet/*
// @match        http://biblioteca.urp.edu.pe/abnet/*
// @grant        GM_xmlhttpRequest
// @grant        GM_setValue
// @grant        GM_getValue
// @connect      localhost
// @connect      *
// @run-at       document-end
// ==/UserScript==

(function () {
    'use strict';

    // URL de la API de Control-Laboratorio (configurable)
    const DEFAULT_API_URL = 'http://localhost:5000/api/Prestamos/SincronizarAbsys';
    let API_URL = (typeof GM_getValue !== 'undefined' ? GM_getValue('SYNC_API_URL', DEFAULT_API_URL) : localStorage.getItem('SYNC_API_URL')) || DEFAULT_API_URL;

    // Cache para evitar envíos duplicados en la misma sesión
    const processedEvents = new Set();

    // ========================================================
    // WIDGET FLOTANTE DE ESTADO EN ABSYSNET
    // ========================================================
    function createSyncWidget() {
        if (document.getElementById('bve-sync-widget')) return;

        const widget = document.createElement('div');
        widget.id = 'bve-sync-widget';
        widget.innerHTML = `
            <div style="
                position: fixed;
                bottom: 18px;
                right: 20px;
                background: #ffffff;
                color: #111827;
                padding: 10px 14px;
                border-radius: 12px;
                box-shadow: 0 10px 25px -3px rgba(0,0,0,0.18), 0 4px 6px -2px rgba(0,0,0,0.05);
                border: 1.5px solid #9f1239;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                font-size: 12px;
                z-index: 999999;
                display: flex;
                align-items: center;
                gap: 10px;
                user-select: none;
            ">
                <div style="
                    width: 10px; 
                    height: 10px; 
                    background: #10b981; 
                    border-radius: 50%; 
                    box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25);
                " id="bve-status-dot"></div>
                <div>
                    <div style="font-weight: 700; color: #9f1239; font-size: 12px; line-height: 1.2;">BVE Medicina URP</div>
                    <div style="color: #6b7280; font-size: 11px;" id="bve-status-text">Sincronizador activo</div>
                </div>
                <button id="bve-cfg-btn" title="Configurar URL del Servidor" style="
                    background: #f3f4f6;
                    border: 1px solid #d1d5db;
                    border-radius: 6px;
                    padding: 3px 6px;
                    font-size: 10px;
                    cursor: pointer;
                    color: #4b5563;
                ">⚙️</button>
            </div>
        `;

        document.body.appendChild(widget);

        document.getElementById('bve-cfg-btn').addEventListener('click', () => {
            const nuevaUrl = prompt('Configura la URL de tu API de Control-Laboratorio:', API_URL);
            if (nuevaUrl && nuevaUrl.trim()) {
                API_URL = nuevaUrl.trim();
                if (typeof GM_setValue !== 'undefined') {
                    GM_setValue('SYNC_API_URL', API_URL);
                } else {
                    localStorage.setItem('SYNC_API_URL', API_URL);
                }
                showToast('URL guardada: ' + API_URL, 'info');
            }
        });
    }

    // ========================================================
    // TOAST NOTIFICACIÓN DENTRO DE ABSYSNET
    // ========================================================
    function showToast(mensaje, tipo = 'success') {
        const toast = document.createElement('div');
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${tipo === 'success' ? '#065f46' : (tipo === 'info' ? '#1e40af' : '#991b1b')};
            color: #ffffff;
            padding: 12px 18px;
            border-radius: 8px;
            box-shadow: 0 10px 25px rgba(0,0,0,0.2);
            font-family: sans-serif;
            font-size: 13px;
            font-weight: 600;
            z-index: 1000000;
            transition: all 0.3s ease;
        `;
        toast.innerText = mensaje;
        document.body.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-10px)';
            setTimeout(() => toast.remove(), 300);
        }, 4000);
    }

    // ========================================================
    // DETECCIÓN DE PANTALLA (PRÉSTAMOS / DEVOLUCIONES)
    // ========================================================
    function detectarModo() {
        const bodyText = document.body.innerText || '';
        if (bodyText.includes('PRÉSTAMOS | PRÉSTAMOS') || bodyText.includes('Fecha de préstamo')) {
            return 'PRESTAMO';
        }
        if (bodyText.includes('DEVOLUCIONES | DEVOLUCIONES') || bodyText.includes('Fecha de devolución')) {
            return 'DEVOLUCION';
        }
        return null;
    }

    // ========================================================
    // BÚSQUEDA INTELIGENTE DE CAMPOS EN ABSYSNET
    // ========================================================
    function obtenerCodigoLector() {
        // 1. Buscar input cuyo id o name contenga 'lector'
        const inputs = Array.from(document.querySelectorAll('input[type="text"], input:not([type])'));
        
        for (const input of inputs) {
            const name = (input.name || '').toLowerCase();
            const id = (input.id || '').toLowerCase();
            if (name.includes('lector') || id.includes('lector')) {
                if (input.value && input.value.trim()) return input.value.trim();
            }
        }

        // 2. Buscar por proximidad a la etiqueta "Nº lector" o "Lector"
        const allElements = document.querySelectorAll('td, span, div, label');
        for (const el of allElements) {
            if (el.innerText && (el.innerText.includes('Nº lector') || el.innerText.includes('N° lector'))) {
                const parentRow = el.closest('tr') || el.parentElement;
                if (parentRow) {
                    const input = parentRow.querySelector('input');
                    if (input && input.value && input.value.trim()) {
                        return input.value.trim();
                    }
                }
            }
        }

        return '';
    }

    function obtenerNombreLector() {
        // En AbsysNet, al costado del código de lector aparece un cuadro de texto deshabilitado con el nombre
        const inputs = Array.from(document.querySelectorAll('input[type="text"], input[disabled], input[readonly]'));
        for (const input of inputs) {
            if (input.disabled || input.readOnly) {
                const val = (input.value || '').trim();
                // Si contiene letras y espacios (nombre)
                if (val.length > 5 && /[A-Z]/.test(val) && val.includes(' ')) {
                    return val;
                }
            }
        }
        return null;
    }

    // ========================================================
    // INSPECCIÓN DE LA TABLA "DETALLES DEL EJEMPLAR"
    // ========================================================
    function inspeccionarTablaEjemplares() {
        const modo = detectarModo();
        if (!modo) return;

        const tablas = document.querySelectorAll('table');
        tablas.forEach(tabla => {
            const rows = Array.from(tabla.rows || []);
            if (rows.length < 2) return;

            // Verificar si es la tabla de ejemplares (contiene columnas C. Barras, Título, etc.)
            const headerText = rows[0].innerText || '';
            if (headerText.includes('C. Barras') || headerText.includes('Título') || headerText.includes('Fecha Prést')) {
                // Revisar cada fila de datos (a partir de la fila 1)
                for (let i = 1; i < rows.length; i++) {
                    const cells = rows[i].cells;
                    if (!cells || cells.length < 5) continue;

                    let cBarras = '';
                    let titulo = '';
                    let fechaDevol = null;

                    // Extraer según posición típica de AbsysNet
                    // Col 4: C. Barras, Col 5: Título, Col 8: Fecha Devol.
                    for (let c = 0; c < cells.length; c++) {
                        const txt = cells[c].innerText.trim();
                        if (/^[A-Z0-9\-_]{4,20}$/i.test(txt) && !cBarras) {
                            cBarras = txt;
                        } else if (txt.length > 4 && !titulo && !/\d{2}\/\d{2}\/\d{4}/.test(txt)) {
                            titulo = txt;
                        } else if (/\d{2}\/\d{2}\/\d{4}/.test(txt)) {
                            fechaDevol = parseFechaPeruana(txt);
                        }
                    }

                    if (cBarras) {
                        const codLector = obtenerCodigoLector();
                        const nombreLector = obtenerNombreLector();
                        const eventKey = `${modo}_${cBarras}_${codLector}`;

                        if (!processedEvents.has(eventKey)) {
                            processedEvents.add(eventKey);
                            enviarSincronizacion({
                                tipoOperacion: modo,
                                codigoAlumno: codLector,
                                nombreAlumno: nombreLector,
                                codigoBarrasLibro: cBarras,
                                tituloLibro: titulo,
                                fechaDevolucion: fechaDevol
                            });
                        }
                    }
                }
            }
        });
    }

    function parseFechaPeruana(fechaStr) {
        try {
            // Formato DD/MM/AAAA o DD/MM/AAAA HH:mm
            const match = fechaStr.match(/(\d{2})\/(\d{2})\/(\d{4})/);
            if (match) {
                return `${match[3]}-${match[2]}-${match[1]}T18:00:00`;
            }
        } catch (e) {}
        return null;
    }

    // ========================================================
    // ENVÍO DE DATOS HACIA CONTROL-LABORATORIO
    // ========================================================
    function enviarSincronizacion(payload) {
        console.log('[BVE Sync] Enviando evento a Control-Laboratorio:', payload);

        const statusText = document.getElementById('bve-status-text');
        if (statusText) statusText.innerText = 'Enviando...';

        const jsonBody = JSON.stringify(payload);

        // Usar GM_xmlhttpRequest para evitar restricciones CORS de extensiones
        if (typeof GM_xmlhttpRequest !== 'undefined') {
            GM_xmlhttpRequest({
                method: 'POST',
                url: API_URL,
                headers: { 'Content-Type': 'application/json' },
                data: jsonBody,
                onload: function (response) {
                    procesarRespuesta(response.status, response.responseText, payload);
                },
                onerror: function (err) {
                    console.error('[BVE Sync] Error de conexión:', err);
                    showToast('Error conectando a ' + API_URL, 'error');
                }
            });
        } else {
            // Fallback con fetch normal
            fetch(API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: jsonBody
            })
            .then(res => res.json().then(data => procesarRespuesta(res.status, JSON.stringify(data), payload)))
            .catch(err => {
                console.error('[BVE Sync] Error:', err);
                showToast('Error conectando con API', 'error');
            });
        }
    }

    function procesarRespuesta(status, responseText, payload) {
        const statusText = document.getElementById('bve-status-text');
        if (statusText) statusText.innerText = 'Sincronizado ✓';

        if (status >= 200 && status < 300) {
            const accion = payload.tipoOperacion === 'PRESTAMO' ? 'Préstamo registrado' : 'Devolución registrada';
            showToast(`✅ ${accion}: ${payload.codigoAlumno || ''} - Libro: ${payload.codigoBarrasLibro}`, 'success');
        } else {
            console.warn('[BVE Sync] Respuesta no-200:', status, responseText);
            showToast(`⚠️ Alerta (${status}): ${responseText}`, 'error');
        }
    }

    // ========================================================
    // ESCUCHA DE EVENTOS POR TECLADO / PISTOLA LECTORA
    // ========================================================
    function setupInputListeners() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                const target = e.target;
                if (target && target.tagName === 'INPUT') {
                    // Dar 400ms para que AbsysNet actualice la tabla o procese la respuesta
                    setTimeout(inspeccionarTablaEjemplares, 400);
                    setTimeout(inspeccionarTablaEjemplares, 1200);
                }
            }
        });
    }

    // ========================================================
    // MUTATION OBSERVER PARA DETECTAR CAMBIOS EN EL DOM
    // ========================================================
    function observeDOM() {
        const observer = new MutationObserver((mutations) => {
            inspeccionarTablaEjemplares();
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // INICIALIZACIÓN
    function init() {
        createSyncWidget();
        setupInputListeners();
        observeDOM();
        inspeccionarTablaEjemplares();
        console.log('[BVE Sync] Script cargado en AbsysNet URP');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

