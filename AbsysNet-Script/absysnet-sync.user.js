// ==UserScript==
// @name         AbsysNet -> Control Laboratorio & Biblioteca URP Sync
// @namespace    https://medicina.urp.edu.pe/bve
// @version      1.8.0
// @description  Captura automática de préstamos y devoluciones en AbsysNet y sincronización en tiempo real con Control-Laboratorio.
// @author       Facultad de Medicina Humana - URP
// @match        https://biblioteca.urp.edu.pe/abnet/*
// @match        http://biblioteca.urp.edu.pe/abnet/*
// @grant        GM_xmlhttpRequest
// @grant        GM_setValue
// @grant        GM_getValue
// @connect      bvefamurp.helifyferdigital.cloud
// @connect      localhost
// @connect      *
// @run-at       document-end
// ==/UserScript==

(function () {
    'use strict';

    // URL oficial de producción
    const DEFAULT_API_URL = 'https://bvefamurp.helifyferdigital.cloud/api/Prestamos/SincronizarAbsys';
    
    let API_URL = DEFAULT_API_URL;
    try {
        if (typeof GM_getValue !== 'undefined') {
            API_URL = GM_getValue('SYNC_API_URL', DEFAULT_API_URL);
        } else if (typeof localStorage !== 'undefined') {
            API_URL = localStorage.getItem('SYNC_API_URL') || DEFAULT_API_URL;
        }
    } catch (e) {
        API_URL = DEFAULT_API_URL;
    }

    console.log('[BVE Sync] Iniciando sincronizador BVE Medicina URP ->', API_URL);

    const syncCache = new Set();
    let isWidgetCollapsed = false;

    // ========================================================
    // ICONOS SVG (ESTRICTAMENTE SIN EMOJIS)
    // ========================================================
    const ICONS = {
        book: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1-2.5-2.5Z"/><path d="M6 6h10"/><path d="M6 10h10"/></svg>`,
        sync: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16"/><path d="M16 16h5v5"/></svg>`,
        test: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>`,
        gear: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>`,
        chevron: `<svg class="bve-chevron-icon" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" style="transition: transform 0.2s ease;"><path d="m6 9 6 6 6-6"/></svg>`,
        success: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#059669" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/></svg>`,
        warning: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#d97706" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
        error: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#dc2626" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`,
        info: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#2563eb" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>`
    };

    // ========================================================
    // OBTENER TODOS LOS DOCUMENTOS (FRAMES / FRAMESETS)
    // ========================================================
    function getAllDocuments() {
        const docs = [];
        try {
            function inspectWin(w) {
                try {
                    if (w && w.document && !docs.includes(w.document)) {
                        docs.push(w.document);
                    }
                    if (w.frames) {
                        for (let i = 0; i < w.frames.length; i++) {
                            inspectWin(w.frames[i]);
                        }
                    }
                } catch (e) {}
            }
            inspectWin(window.top || window);
        } catch (e) {
            docs.push(document);
        }
        return docs;
    }

    // ========================================================
    // LISTA BLANCA ESTRICTA: ÚNICAS 4 PANTALLAS DONDE DEBE OPERAR EL SCRIPT
    // Préstamos | Devoluciones | Préstamos en sala | Devoluciones en sala
    // ========================================================

    /**
     * Clasifica el documento según cuál de las 4 pantallas autorizadas es.
     * Retorna 'PRESTAMO', 'DEVOLUCION' o null si no es ninguna de las 4.
     */
    function clasificarPantalla(doc) {
        if (!doc || !doc.body) return null;

        // Buscar el título prominente en la cabecera del frame o en el texto
        // AbsysNet muestra el título en la barra superior (ej: "PRÉSTAMOS | PRÉSTAMOS")
        // y también puede estar en el <title> del documento o en un encabezado visible.
        const title = (doc.title || '').toUpperCase().trim();
        const header = (() => {
            // Intentar leer la cabecera de la barra superior de AbsysNet
            const h = doc.querySelector('td.cabecera, div.titulo, h1, h2, .header-title, [class*="titulo"], [class*="cabecera"]');
            return h ? h.innerText.toUpperCase().trim() : '';
        })();
        const bodyText = (doc.body.innerText || '').toUpperCase();

        // ── Función auxiliar: detecta si alguno de los textos candidatos contiene
        //    la clave requerida y NO contiene ninguna de las claves excluidas ──
        const matches = (key, ...excludes) => {
            const sources = [title, header, bodyText];
            const found = sources.some(s => s.includes(key));
            if (!found) return false;
            // Verificar exclusiones (evitar falsos positivos de otras pantallas)
            const excluded = excludes.some(ex => sources.some(s => s.includes(ex)));
            return !excluded;
        };

        // ── PANTALLAS DE PRÉSTAMOS ──────────────────────────────────────────
        // "PRÉSTAMOS EN SALA" primero para que no colisione con la regla general
        if (matches('PRÉSTAMOS EN SALA', 'CONSULTA')) return 'PRESTAMO';
        // "PRÉSTAMOS" (normal, domicilio) → excluir "EN SALA" y "CONSULTA"
        if (matches('PRÉSTAMOS', 'EN SALA', 'CONSULTA')) return 'PRESTAMO';

        // ── PANTALLAS DE DEVOLUCIONES ───────────────────────────────────────
        // "DEVOLUCIONES EN SALA" primero para que no colisione con la regla general
        if (matches('DEVOLUCIONES EN SALA', 'CONSULTA')) return 'DEVOLUCION';
        // "DEVOLUCIONES" (normal, domicilio) → excluir "EN SALA" y "CONSULTA"
        if (matches('DEVOLUCIONES', 'EN SALA', 'CONSULTA')) return 'DEVOLUCION';

        // Ninguna de las 4 pantallas autorizadas
        return null;
    }

    function findTargetDocument() {
        const allDocs = getAllDocuments();

        for (const doc of allDocs) {
            if (!doc.body) continue;
            const modo = clasificarPantalla(doc);
            if (modo !== null) return doc;
        }

        // No estamos en ninguna de las 4 pantallas autorizadas → ocultar widget
        return null;
    }

    // ========================================================
    // WIDGET FLOTANTE VISIBLE SOBRE LOS MARCOS DE ABSYSNET (SINGLETON)
    // ========================================================
    function createWidget() {
        try {
            const allDocs = getAllDocuments();
            const targetDoc = findTargetDocument();

            // Si no estamos en la pestaña de préstamos o devoluciones, retirar cualquier widget existente
            if (!targetDoc) {
                allDocs.forEach(d => {
                    const existing = d.getElementById('bve-sync-widget');
                    if (existing) existing.remove();
                });
                return;
            }

            // Limpiar widgets huérfanos en otros frames para que solo haya exactamente UNO
            allDocs.forEach(d => {
                if (d !== targetDoc) {
                    const existing = d.getElementById('bve-sync-widget');
                    if (existing) existing.remove();
                }
            });

            // Si ya existe en el targetDoc, no recrearlo
            if (targetDoc.getElementById('bve-sync-widget')) return;

            const widget = targetDoc.createElement('div');
            widget.id = 'bve-sync-widget';
            widget.innerHTML = `
                <div class="bve-container" style="
                    position: fixed !important;
                    bottom: 16px !important;
                    right: 20px !important;
                    background: #ffffff !important;
                    color: #0f172a !important;
                    border-radius: 10px !important;
                    box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.12), 0 8px 10px -6px rgba(15, 23, 42, 0.08), 0 0 0 1px rgba(226, 232, 240, 0.85) !important;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
                    font-size: 12px !important;
                    z-index: 2147483647 !important;
                    user-select: none !important;
                    min-width: 250px !important;
                    overflow: hidden !important;
                    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1) !important;
                ">
                    <!-- Barra de acento institucional -->
                    <div style="height: 3px; background: linear-gradient(90deg, #9f1239 0%, #be123c 60%, #e11d48 100%); width: 100%;"></div>

                    <div style="padding: 10px 14px 12px 14px; display: flex; flex-direction: column; gap: 9px;">
                        <!-- Cabecera -->
                        <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
                            <div style="display: flex; align-items: center; gap: 8px;">
                                <div style="
                                    width: 26px;
                                    height: 26px;
                                    border-radius: 6px;
                                    background: #fff1f2;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    color: #9f1239;
                                    flex-shrink: 0;
                                ">${ICONS.book}</div>
                                <div>
                                    <div style="font-weight: 700; color: #0f172a; font-size: 11.5px; line-height: 1.2; letter-spacing: -0.01em;">BVE Medicina URP</div>
                                    <div style="display: flex; align-items: center; gap: 5px; margin-top: 2px;">
                                        <div class="bve-status-dot" style="width: 7px; height: 7px; background: #10b981; border-radius: 50%; box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.25);"></div>
                                        <span class="bve-status-text" style="color: #64748b; font-size: 10.5px; font-weight: 500;">Sincronizador Activo</span>
                                    </div>
                                </div>
                            </div>

                            <!-- Botones superiores -->
                            <div style="display: flex; align-items: center; gap: 2px;">
                                <button class="bve-cfg-btn" title="Configurar URL del Servidor" style="
                                    background: transparent;
                                    border: 1px solid transparent;
                                    border-radius: 6px;
                                    cursor: pointer;
                                    color: #64748b;
                                    width: 24px;
                                    height: 24px;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    padding: 0;
                                    transition: all 0.15s ease;
                                ">${ICONS.gear}</button>

                                <button class="bve-collapse-btn" title="Minimizar / Expandir" style="
                                    background: transparent;
                                    border: 1px solid transparent;
                                    border-radius: 6px;
                                    cursor: pointer;
                                    color: #64748b;
                                    width: 24px;
                                    height: 24px;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    padding: 0;
                                    transition: all 0.15s ease;
                                ">${ICONS.chevron}</button>
                            </div>
                        </div>

                        <!-- Acciones principales -->
                        <div class="bve-actions-wrapper" style="display: flex; gap: 7px; align-items: center; ${isWidgetCollapsed ? 'display: none !important;' : ''}">
                            <button class="bve-capture-btn" style="
                                flex: 1;
                                background: #9f1239;
                                color: #ffffff;
                                border: none;
                                border-radius: 6px;
                                padding: 6px 10px;
                                font-size: 11px;
                                font-weight: 600;
                                cursor: pointer;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                gap: 6px;
                                box-shadow: 0 1px 3px rgba(159, 18, 57, 0.25);
                                transition: background 0.15s ease, box-shadow 0.15s ease;
                            ">
                                ${ICONS.sync}
                                <span>Sincronizar</span>
                            </button>

                            <button class="bve-test-btn" title="Verificar conectividad con el servidor (Ping)" style="
                                background: #f8fafc;
                                color: #475569;
                                border: 1px solid #e2e8f0;
                                border-radius: 6px;
                                padding: 6px 10px;
                                font-size: 11px;
                                font-weight: 600;
                                cursor: pointer;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                gap: 5px;
                                transition: all 0.15s ease;
                            ">
                                ${ICONS.test}
                                <span>Probar</span>
                            </button>
                        </div>
                    </div>
                </div>
            `;

            targetDoc.body.appendChild(widget);

            // Listeners y Efectos Hover
            const captureBtn = widget.querySelector('.bve-capture-btn');
            if (captureBtn) {
                captureBtn.addEventListener('mouseenter', () => { captureBtn.style.background = '#881337'; });
                captureBtn.addEventListener('mouseleave', () => { captureBtn.style.background = '#9f1239'; });
                captureBtn.addEventListener('click', () => {
                    showToast('Escaneando pantalla de AbsysNet...', 'info');
                    const encontrado = inspeccionarPantalla(true);
                    if (!encontrado) {
                        showToast('No se encontró un libro o alumno en la pantalla actual.', 'error');
                    }
                });
            }

            const testBtn = widget.querySelector('.bve-test-btn');
            if (testBtn) {
                testBtn.addEventListener('mouseenter', () => {
                    testBtn.style.background = '#f1f5f9';
                    testBtn.style.borderColor = '#cbd5e1';
                    testBtn.style.color = '#0f172a';
                });
                testBtn.addEventListener('mouseleave', () => {
                    testBtn.style.background = '#f8fafc';
                    testBtn.style.borderColor = '#e2e8f0';
                    testBtn.style.color = '#475569';
                });
                testBtn.addEventListener('click', () => {
                    probarConexionServidor();
                });
            }

            const cfgBtn = widget.querySelector('.bve-cfg-btn');
            if (cfgBtn) {
                cfgBtn.addEventListener('mouseenter', () => {
                    cfgBtn.style.background = '#f1f5f9';
                    cfgBtn.style.color = '#0f172a';
                });
                cfgBtn.addEventListener('mouseleave', () => {
                    cfgBtn.style.background = 'transparent';
                    cfgBtn.style.color = '#64748b';
                });
                cfgBtn.addEventListener('click', () => {
                    const nueva = prompt('URL de la API de Control-Laboratorio:', API_URL);
                    if (nueva && nueva.trim()) {
                        API_URL = nueva.trim();
                        try {
                            if (typeof GM_setValue !== 'undefined') GM_setValue('SYNC_API_URL', API_URL);
                            else localStorage.setItem('SYNC_API_URL', API_URL);
                        } catch(e){}
                        showToast('URL actualizada: ' + API_URL, 'info');
                    }
                });
            }

            const collapseBtn = widget.querySelector('.bve-collapse-btn');
            const actionsWrapper = widget.querySelector('.bve-actions-wrapper');
            const chevronIcon = widget.querySelector('.bve-chevron-icon');

            if (collapseBtn && actionsWrapper) {
                collapseBtn.addEventListener('mouseenter', () => {
                    collapseBtn.style.background = '#f1f5f9';
                    collapseBtn.style.color = '#0f172a';
                });
                collapseBtn.addEventListener('mouseleave', () => {
                    collapseBtn.style.background = 'transparent';
                    collapseBtn.style.color = '#64748b';
                });
                collapseBtn.addEventListener('click', () => {
                    isWidgetCollapsed = !isWidgetCollapsed;
                    actionsWrapper.style.setProperty('display', isWidgetCollapsed ? 'none' : 'flex', 'important');
                    if (chevronIcon) {
                        chevronIcon.style.transform = isWidgetCollapsed ? 'rotate(180deg)' : 'rotate(0deg)';
                    }
                });
            }
        } catch (e) {
            console.error('[BVE Sync] Error al crear widget:', e);
        }
    }

    // ========================================================
    // TOAST NOTIFICACIÓN MODERNA (ESTRICTAMENTE CON ICONOS SVG)
    // ========================================================
    function showToast(mensaje, tipo = 'success') {
        try {
            const allDocs = getAllDocuments();
            let targetDoc = null;
            for (const doc of allDocs) {
                if (doc.getElementById('bve-sync-widget')) {
                    targetDoc = doc;
                    break;
                }
            }
            if (!targetDoc) {
                targetDoc = allDocs.find(d => d.body && (d.defaultView?.innerHeight || 0) > 300) || allDocs.find(d => d.body);
            }
            if (!targetDoc || !targetDoc.body) return;

            const oldToast = targetDoc.getElementById('bve-sync-toast');
            if (oldToast) oldToast.remove();

            const toast = targetDoc.createElement('div');
            toast.id = 'bve-sync-toast';

            let iconSvg = ICONS.success;
            let accentColor = '#059669';
            if (tipo === 'info') {
                iconSvg = ICONS.info;
                accentColor = '#2563eb';
            } else if (tipo === 'warning') {
                iconSvg = ICONS.warning;
                accentColor = '#d97706';
            } else if (tipo === 'error') {
                iconSvg = ICONS.error;
                accentColor = '#dc2626';
            }

            toast.style.cssText = `
                position: fixed !important;
                top: 20px !important;
                right: 20px !important;
                background: #ffffff !important;
                color: #0f172a !important;
                padding: 10px 14px !important;
                border-radius: 8px !important;
                box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.15), 0 4px 10px -3px rgba(0, 0, 0, 0.08), 0 0 0 1px rgba(226, 232, 240, 0.85) !important;
                border-left: 4px solid ${accentColor} !important;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
                font-size: 12px !important;
                font-weight: 500 !important;
                z-index: 2147483647 !important;
                display: flex !important;
                align-items: center !important;
                gap: 9px !important;
                max-width: 380px !important;
                line-height: 1.4 !important;
                opacity: 0 !important;
                transform: translateY(-8px) !important;
                transition: opacity 0.25s cubic-bezier(0.16, 1, 0.3, 1), transform 0.25s cubic-bezier(0.16, 1, 0.3, 1) !important;
            `;

            toast.innerHTML = `
                <div style="flex-shrink: 0; display: flex; align-items: center;">${iconSvg}</div>
                <div style="flex: 1;">${mensaje}</div>
            `;

            targetDoc.body.appendChild(toast);

            // Transición suave de entrada
            requestAnimationFrame(() => {
                toast.style.opacity = '1';
                toast.style.transform = 'translateY(0)';
            });

            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transform = 'translateY(-8px)';
                setTimeout(() => toast.remove(), 250);
            }, 3800);
        } catch(e){}
    }

    // ========================================================
    // PROBAR CONEXIÓN (PING SEGURO SIN CREAR PRÉSTAMOS FALSOS)
    // ========================================================
    function probarConexionServidor() {
        const pingUrl = API_URL.replace(/\/SincronizarAbsys\/?$/, '/Ping');
        showToast('Comprobando conexión con servidor...', 'info');

        const onPingSuccess = (status) => {
            if (status >= 200 && status < 300) {
                showToast('Conexión con servidor exitosa (200 OK)', 'success');
            } else {
                showToast('Respuesta del servidor: HTTP ' + status, 'warning');
            }
        };

        const onPingError = () => {
            showToast('Error al conectar con ' + pingUrl, 'error');
        };

        if (typeof GM_xmlhttpRequest !== 'undefined') {
            GM_xmlhttpRequest({
                method: 'GET',
                url: pingUrl,
                onload: (res) => onPingSuccess(res.status),
                onerror: onPingError
            });
        } else {
            fetch(pingUrl)
                .then(r => onPingSuccess(r.status))
                .catch(onPingError);
        }
    }

    // ========================================================
    // DETECCIÓN DE MODO (PRESTAMO / DEVOLUCION) — basada en la lista blanca
    // ========================================================
    function detectarModo(targetDoc) {
        if (!targetDoc || !targetDoc.body) return 'PRESTAMO';

        // Prioridad 1: usar clasificarPantalla() que ya es estricta y correcta
        const modoClasificado = clasificarPantalla(targetDoc);
        if (modoClasificado) return modoClasificado;

        // Prioridad 2: Fallback por texto visible (casos extremos de carga parcial)
        const upper = (targetDoc.body.innerText || '').toUpperCase();
        if (upper.includes('DEVOLUCIÓN') || upper.includes('DEVOLUCIONES') || upper.includes('FECHA DE DEVOL')) {
            return 'DEVOLUCION';
        }

        return 'PRESTAMO';
    }

    // ========================================================
    // BUSCAR ALUMNO EN EL FRAME ACTIVO
    // ========================================================
    function obtenerDatosAlumno(targetDoc) {
        let codigo = '';
        let nombre = '';

        if (!targetDoc) return { codigo, nombre };

        // 1. Buscar en todos los inputs
        const inputs = Array.from(targetDoc.querySelectorAll('input, textarea, select'));

        // A) Buscar inputs cuyo valor contenga un código de 8 a 10 dígitos (URP)
        for (const input of inputs) {
            const rawVal = input.value || input.getAttribute('value') || '';
            const digits = rawVal.replace(/\D/g, '');
            if (digits.length >= 8 && digits.length <= 10) {
                codigo = digits;
                break;
            }
        }

        // B) Si no se encontró en inputs por valor, buscar en el texto de la pantalla (Detalles del Lector)
        if (!codigo) {
            const docText = (targetDoc.body ? targetDoc.body.innerText : '') || '';
            const matchLector = docText.match(/N[º°\.]?\s*lector[:\s]*(\d{8,10})/i) || 
                                docText.match(/Detalles del Lector[\s\S]{1,120}?(\d{8,10})/i);
            if (matchLector) {
                codigo = matchLector[1];
            } else {
                const matchGeneral = docText.match(/\b(20\d{6,8})\b/);
                if (matchGeneral) {
                    codigo = matchGeneral[1];
                }
            }
        }

        // 2. Buscar nombre del alumno (formato: "APELLIDOS, NOMBRES")
        for (const input of inputs) {
            const v = (input.value || input.getAttribute('value') || '').trim();
            if (v.length > 5 && v.includes(',') && /[A-Z]/.test(v) && !v.includes('URP') && !v.includes('MEDICINA')) {
                nombre = v;
                break;
            }
        }

        // Si el nombre no está en un input con coma, buscar en texto
        if (!nombre) {
            const docText = (targetDoc.body ? targetDoc.body.innerText : '') || '';
            const matchNom = docText.match(/([A-ZÁÉÍÓÚÑ\s]{3,30},\s*[A-ZÁÉÍÓÚÑ\s]{3,30})/);
            if (matchNom && !matchNom[1].includes('URP') && !matchNom[1].includes('MEDICINA')) {
                nombre = matchNom[1].trim();
            }
        }

        return { codigo, nombre };
    }

    // ========================================================
    // INSPECCIÓN DE PANTALLA Y TABLAS DE LIBROS
    // ========================================================
    function inspeccionarPantalla(esManual = false) {
        const targetDoc = findTargetDocument();
        if (!targetDoc) {
            console.log('[BVE Sync] No se encontró marco de circulación activo.');
            return false;
        }

        const modo = detectarModo(targetDoc);
        const { codigo: codAlumno, nombre: nomAlumno } = obtenerDatosAlumno(targetDoc);

        console.log('[BVE Sync] Inspeccionando pantalla... Modo:', modo, 'Alumno:', codAlumno, nomAlumno);

        let encontrado = false;

        const tablas = targetDoc.querySelectorAll('table');
        tablas.forEach(tabla => {
            const rows = Array.from(tabla.rows || []);
            if (rows.length < 2) return;

            const headerTexts = Array.from(rows[0].cells).map(c => (c.innerText || '').toLowerCase().trim());
            const colBarras = headerTexts.findIndex(h => h.includes('barras') || h.includes('c. barras'));
            const colTitulo = headerTexts.findIndex(h => h.includes('título') || h.includes('titulo'));
            const colFechaDev = headerTexts.findIndex(h => h.includes('fecha devol') || h.includes('devol'));

            const esTablaValida = headerTexts.some(h => h.includes('barras') || h.includes('ejemp') || h.includes('título'));

            if (esTablaValida) {
                for (let i = 1; i < rows.length; i++) {
                    const cells = Array.from(rows[i].cells || []);
                    if (cells.length < 3) continue;

                    let cBarras = colBarras >= 0 && cells[colBarras] ? cells[colBarras].innerText.trim() : '';
                    let titulo = colTitulo >= 0 && cells[colTitulo] ? cells[colTitulo].innerText.trim() : '';
                    let fechaDevStr = colFechaDev >= 0 && cells[colFechaDev] ? cells[colFechaDev].innerText.trim() : '';

                    // Heurística de respaldo si las columnas no están bien alineadas
                    if (!cBarras) {
                        for (const cell of cells) {
                            const txt = cell.innerText.trim();
                            if (/^[0-9]{5,15}$/.test(txt) && !cBarras) {
                                cBarras = txt;
                            } else if (txt.length > 6 && !titulo && !txt.includes('/')) {
                                titulo = txt;
                            } else if (/\d{2}\/\d{2}\/\d{4}/.test(txt) && !fechaDevStr) {
                                fechaDevStr = txt;
                            }
                        }
                    }

                    if (cBarras) {
                        encontrado = true;
                        const fechaDev = parseFecha(fechaDevStr);
                        sincronizarLibro(modo, codAlumno, nomAlumno, cBarras, titulo, fechaDev, esManual);
                    }
                }
            }
        });

        // Fallback si no hay tabla pero hay código de barras en un input
        if (!encontrado) {
            const inputs = Array.from(targetDoc.querySelectorAll('input'));
            for (const input of inputs) {
                const name = (input.name || '').toLowerCase();
                if (name.includes('barras') || name.includes('ejemplar')) {
                    const val = (input.value || '').trim();
                    if (/^[0-9]{5,15}$/.test(val)) {
                        encontrado = true;
                        sincronizarLibro(modo, codAlumno, nomAlumno, val, '', null, esManual);
                        break;
                    }
                }
            }
        }

        return encontrado;
    }

    function parseFecha(str) {
        if (!str) return null;
        const m = str.match(/(\d{2})\/(\d{2})\/(\d{4})(?:\s+(\d{2}):(\d{2}))?/);
        if (m) {
            const hh = m[4] || '18';
            const mm = m[5] || '00';
            return `${m[3]}-${m[2]}-${m[1]}T${hh}:${mm}:00`;
        }
        return null;
    }

    // ========================================================
    // ENVIAR AL SERVIDOR
    // ========================================================
    function sincronizarLibro(modo, codAlumno, nomAlumno, cBarras, titulo, fechaDevol, forzar = false) {
        const cacheKey = `${modo}_${codAlumno}_${cBarras}`;
        if (!forzar && syncCache.has(cacheKey)) {
            console.log('[BVE Sync] Ya sincronizado recientemente:', cacheKey);
            return;
        }

        syncCache.add(cacheKey);
        setTimeout(() => syncCache.delete(cacheKey), 25000);

        const payload = {
            tipoOperacion: modo,
            codigoAlumno: codAlumno || '',
            nombreAlumno: nomAlumno || '',
            codigoBarrasLibro: cBarras,
            tituloLibro: titulo || '',
            fechaDevolucion: fechaDevol
        };

        enviarAlServidor(payload);
    }

    function enviarAlServidor(payload) {
        console.log('[BVE Sync] Enviando datos a API:', payload);

        const allDocs = getAllDocuments();
        allDocs.forEach(doc => {
            const dot = doc.querySelector('.bve-status-dot');
            const txt = doc.querySelector('.bve-status-text');
            if (dot) dot.style.background = '#f59e0b';
            if (txt) txt.innerText = 'Enviando...';
        });

        const jsonStr = JSON.stringify(payload);

        const onSuccess = (status, responseText) => {
            console.log('[BVE Sync] Respuesta servidor (' + status + '):', responseText);
            allDocs.forEach(doc => {
                const dot = doc.querySelector('.bve-status-dot');
                const txt = doc.querySelector('.bve-status-text');
                if (dot) dot.style.background = '#10b981';
                if (txt) txt.innerText = 'Sincronizado';
            });

            try {
                const res = JSON.parse(responseText);
                if (status >= 200 && status < 300) {
                    const extra = res.alumno || payload.codigoAlumno;
                    const msg = res.mensaje || 'Préstamo guardado con éxito';
                    showToast(`${msg}${extra ? ' (' + extra + ')' : ''}`, 'success');
                } else {
                    showToast(res.mensaje || res.error || responseText, 'error');
                }
            } catch(e) {
                showToast('Sincronizado correctamente', 'success');
            }
        };

        const onError = (err) => {
            console.error('[BVE Sync] Error al conectar:', err);
            allDocs.forEach(doc => {
                const dot = doc.querySelector('.bve-status-dot');
                const txt = doc.querySelector('.bve-status-text');
                if (dot) dot.style.background = '#ef4444';
                if (txt) txt.innerText = 'Error conexión';
            });
            showToast('Error conectando con ' + API_URL, 'error');
        };

        if (typeof GM_xmlhttpRequest !== 'undefined') {
            GM_xmlhttpRequest({
                method: 'POST',
                url: API_URL,
                headers: { 'Content-Type': 'application/json' },
                data: jsonStr,
                onload: (res) => onSuccess(res.status, res.responseText),
                onerror: onError
            });
        } else {
            fetch(API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: jsonStr
            })
            .then(res => res.text().then(t => onSuccess(res.status, t)))
            .catch(onError);
        }
    }

    // ========================================================
    // ESCUCHA DE TECLADO Y MUTACIONES EN TODOS LOS FRAMES
    // ========================================================
    function setupListeners() {
        const allDocs = getAllDocuments();
        let mutationTimeout = null;

        for (const doc of allDocs) {
            doc.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    setTimeout(() => inspeccionarPantalla(), 400);
                    setTimeout(() => inspeccionarPantalla(), 1200);
                }
            });

            const observer = new MutationObserver(() => {
                clearTimeout(mutationTimeout);
                mutationTimeout = setTimeout(() => {
                    inspeccionarPantalla();
                }, 300);
            });

            if (doc.body) {
                observer.observe(doc.body, {
                    childList: true,
                    subtree: true
                });
            }
        }
    }

    function init() {
        createWidget();
        setupListeners();
        inspeccionarPantalla();
        setInterval(createWidget, 2500);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
