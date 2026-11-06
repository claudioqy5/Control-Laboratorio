// ==UserScript==
// @name         AbsysNet -> Control Laboratorio & Biblioteca URP Sync
// @namespace    https://medicina.urp.edu.pe/bve
// @version      1.3.0
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

    // ========================================================
    // OBTENER TODOS LOS DOCUMENTOS (FRAMES / FRAMESETS)
    // ========================================================
    function getAllDocuments() {
        const docs = [document];
        try {
            const rootWin = window.top || window;
            if (rootWin && rootWin.document && !docs.includes(rootWin.document)) {
                docs.push(rootWin.document);
            }
            function inspectWin(w) {
                try {
                    for (let i = 0; i < w.frames.length; i++) {
                        const subWin = w.frames[i];
                        try {
                            if (subWin && subWin.document && !docs.includes(subWin.document)) {
                                docs.push(subWin.document);
                                inspectWin(subWin);
                            }
                        } catch (e) {}
                    }
                } catch (e) {}
            }
            if (rootWin) inspectWin(rootWin);
        } catch (e) {}
        return docs;
    }

    // ========================================================
    // WIDGET FLOTANTE VISIBLE SOBRE TODOS LOS MARCOS (FRAMES)
    // ========================================================
    function createWidget() {
        try {
            // Colocar en el documento superior (window.top) para que no quede atrapado en ningún frame
            const topDoc = (window.top && window.top.document) ? window.top.document : document;
            if (topDoc.getElementById('bve-sync-widget')) return;

            const widget = topDoc.createElement('div');
            widget.id = 'bve-sync-widget';
            widget.innerHTML = `
                <div style="
                    position: fixed !important;
                    bottom: 20px !important;
                    right: 25px !important;
                    background: #ffffff !important;
                    color: #111827 !important;
                    padding: 12px 16px !important;
                    border-radius: 14px !important;
                    box-shadow: 0 12px 30px rgba(0,0,0,0.3), 0 4px 8px rgba(159,18,57,0.15) !important;
                    border: 2px solid #9f1239 !important;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
                    font-size: 12px !important;
                    z-index: 2147483647 !important;
                    display: flex !important;
                    flex-direction: column !important;
                    gap: 10px !important;
                    user-select: none !important;
                    min-width: 230px !important;
                ">
                    <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
                        <div style="display: flex; align-items: center; gap: 8px;">
                            <div id="bve-status-dot" style="width: 10px; height: 10px; background: #10b981; border-radius: 50%; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.3);"></div>
                            <div>
                                <div style="font-weight: 700; color: #9f1239; font-size: 12px; line-height: 1.2;">BVE Medicina URP</div>
                                <div id="bve-status-text" style="color: #6b7280; font-size: 10px;">Sincronizador Conectado</div>
                            </div>
                        </div>
                        <button id="bve-cfg-btn" title="Configurar URL del Servidor" style="background: transparent; border: none; cursor: pointer; font-size: 14px; padding: 2px;">⚙️</button>
                    </div>

                    <div style="display: flex; gap: 8px;">
                        <button id="bve-capture-btn" style="
                            flex: 1;
                            background: #9f1239;
                            color: #ffffff;
                            border: none;
                            border-radius: 6px;
                            padding: 6px 10px;
                            font-size: 11px;
                            font-weight: 700;
                            cursor: pointer;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            gap: 4px;
                            box-shadow: 0 2px 4px rgba(159,18,57,0.3);
                        ">📥 Capturar Ahora</button>

                        <button id="bve-test-btn" style="
                            background: #f3f4f6;
                            color: #374151;
                            border: 1px solid #d1d5db;
                            border-radius: 6px;
                            padding: 6px 10px;
                            font-size: 11px;
                            font-weight: 600;
                            cursor: pointer;
                        " title="Probar conexión con el servidor">⚡ Probar</button>
                    </div>
                </div>
            `;

            // En un frameset, document.body puede ser null, usamos documentElement
            const container = topDoc.body || topDoc.documentElement;
            if (container) {
                container.appendChild(widget);
                console.log('[BVE Sync] Widget insertado exitosamente en window.top');
            }

            // Listeners de los botones
            const captureBtn = topDoc.getElementById('bve-capture-btn');
            if (captureBtn) {
                captureBtn.addEventListener('click', () => {
                    showToast('Escaneando pantalla de AbsysNet...', 'info');
                    const encontrado = inspeccionarPantalla(true);
                    if (!encontrado) {
                        showToast('No se encontró un libro o alumno en la pantalla actual.', 'error');
                    }
                });
            }

            const testBtn = topDoc.getElementById('bve-test-btn');
            if (testBtn) {
                testBtn.addEventListener('click', () => {
                    probarConexionServidor();
                });
            }

            const cfgBtn = topDoc.getElementById('bve-cfg-btn');
            if (cfgBtn) {
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
        } catch (e) {
            console.error('[BVE Sync] Error al crear widget:', e);
        }
    }

    // ========================================================
    // TOAST NOTIFICACIÓN EN LA PANTALLA
    // ========================================================
    function showToast(mensaje, tipo = 'success') {
        try {
            const topDoc = (window.top && window.top.document) ? window.top.document : document;
            const toast = topDoc.createElement('div');
            const color = tipo === 'success' ? '#065f46' : (tipo === 'info' ? '#1e40af' : '#991b1b');
            toast.style.cssText = `
                position: fixed !important;
                top: 25px !important;
                right: 25px !important;
                background: ${color} !important;
                color: #ffffff !important;
                padding: 14px 20px !important;
                border-radius: 10px !important;
                box-shadow: 0 10px 25px rgba(0,0,0,0.35) !important;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
                font-size: 13px !important;
                font-weight: 600 !important;
                z-index: 2147483647 !important;
                transition: all 0.3s ease !important;
                max-width: 380px !important;
            `;
            toast.innerText = mensaje;
            const container = topDoc.body || topDoc.documentElement;
            if (container) container.appendChild(toast);

            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transform = 'translateY(-10px)';
                setTimeout(() => toast.remove(), 300);
            }, 4500);
        } catch(e){}
    }

    // ========================================================
    // PROBAR CONEXIÓN
    // ========================================================
    function probarConexionServidor() {
        showToast('Enviando prueba a ' + API_URL, 'info');
        const testPayload = {
            tipoOperacion: 'PRESTAMO',
            codigoAlumno: '202110452',
            nombreAlumno: 'Verificacion Conectividad',
            codigoBarrasLibro: '1106597',
            tituloLibro: 'Histología Ross Atlas'
        };

        enviarAlServidor(testPayload, true);
    }

    // ========================================================
    // DETECCIÓN DE MODO
    // ========================================================
    function detectarModo() {
        const allDocs = getAllDocuments();
        for (const doc of allDocs) {
            const txt = (doc.body ? doc.body.innerText : '') || (doc.documentElement ? doc.documentElement.innerText : '');
            if (txt.includes('PRÉSTAMOS') || txt.includes('Fecha de préstamo')) {
                return 'PRESTAMO';
            }
            if (txt.includes('DEVOLUCIONES') || txt.includes('Fecha de devolución')) {
                return 'DEVOLUCION';
            }
        }
        return 'PRESTAMO';
    }

    // ========================================================
    // BUSCAR ALUMNO EN TODOS LOS FRAMES
    // ========================================================
    function obtenerDatosAlumno() {
        let codigo = '';
        let nombre = '';

        const allDocs = getAllDocuments();
        for (const doc of allDocs) {
            const inputs = Array.from(doc.querySelectorAll('input'));

            // 1. Por name o id
            for (const input of inputs) {
                const name = (input.name || '').toLowerCase();
                const id = (input.id || '').toLowerCase();
                if (name.includes('lector') || id.includes('lector')) {
                    if (input.value && input.value.trim()) {
                        codigo = input.value.trim();
                        break;
                    }
                }
            }

            // 2. Por etiqueta "Nº lector"
            if (!codigo) {
                const tags = doc.querySelectorAll('td, span, div, label, b, th');
                for (const tag of tags) {
                    const t = tag.innerText || '';
                    if (t.includes('Nº lector') || t.includes('N° lector') || t.includes('Lector:')) {
                        const row = tag.closest('tr') || tag.parentElement;
                        if (row) {
                            const inp = row.querySelector('input');
                            if (inp && inp.value && inp.value.trim()) {
                                codigo = inp.value.trim();
                                break;
                            }
                        }
                    }
                }
            }

            // 3. Nombre del alumno
            for (const input of inputs) {
                if (input.disabled || input.readOnly) {
                    const v = (input.value || '').trim();
                    if (v.length > 5 && /[A-Za-z]/.test(v) && v.includes(' ') && !v.includes('URP')) {
                        nombre = v;
                        break;
                    }
                }
            }

            if (codigo) break;
        }

        return { codigo, nombre };
    }

    // ========================================================
    // INSPECCIÓN DE PANTALLA Y TABLAS DE LIBROS
    // ========================================================
    function inspeccionarPantalla(esManual = false) {
        const modo = detectarModo();
        const { codigo: codAlumno, nombre: nomAlumno } = obtenerDatosAlumno();

        console.log('[BVE Sync] Inspeccionando pantalla... Modo:', modo, 'Alumno:', codAlumno);

        let encontrado = false;
        const allDocs = getAllDocuments();

        for (const doc of allDocs) {
            const tablas = doc.querySelectorAll('table');
            tablas.forEach(tabla => {
                const rows = Array.from(tabla.rows || []);
                if (rows.length < 2) return;

                const encabezado = (rows[0].innerText || '').toLowerCase();
                if (encabezado.includes('barras') || encabezado.includes('título') || encabezado.includes('ejemp') || encabezado.includes('fecha')) {
                    for (let i = 1; i < rows.length; i++) {
                        const cells = Array.from(rows[i].cells || []);
                        if (cells.length < 4) continue;

                        let cBarras = '';
                        let titulo = '';
                        let fechaDevol = null;

                        for (const cell of cells) {
                            const txt = (cell.innerText || '').trim();
                            if (/^[A-Za-z0-9\-_]{4,20}$/.test(txt) && !cBarras && !txt.includes('/')) {
                                cBarras = txt;
                            } else if (txt.length > 5 && !titulo && !/\d{2}\/\d{2}\/\d{4}/.test(txt)) {
                                titulo = txt;
                            } else if (/\d{2}\/\d{2}\/\d{4}/.test(txt) && !fechaDevol) {
                                fechaDevol = parseFecha(txt);
                            }
                        }

                        if (cBarras) {
                            encontrado = true;
                            sincronizarLibro(modo, codAlumno, nomAlumno, cBarras, titulo, fechaDevol);
                        }
                    }
                }
            });

            // Si no hay tabla con filas pero hay un código de barras en un input
            if (!encontrado) {
                const inputs = Array.from(doc.querySelectorAll('input'));
                for (const input of inputs) {
                    const name = (input.name || '').toLowerCase();
                    if (name.includes('barras') || name.includes('ejemplar')) {
                        const val = (input.value || '').trim();
                        if (val.length >= 4) {
                            encontrado = true;
                            sincronizarLibro(modo, codAlumno, nomAlumno, val, 'Material Bibliográfico URP', null);
                            break;
                        }
                    }
                }
            }

            if (encontrado) break;
        }

        return encontrado;
    }

    function parseFecha(str) {
        const m = str.match(/(\d{2})\/(\d{2})\/(\d{4})/);
        if (m) {
            return `${m[3]}-${m[2]}-${m[1]}T18:00:00`;
        }
        return null;
    }

    // ========================================================
    // ENVIAR AL SERVIDOR
    // ========================================================
    function sincronizarLibro(modo, codAlumno, nomAlumno, cBarras, titulo, fechaDevol) {
        const cacheKey = `${modo}_${codAlumno}_${cBarras}`;
        if (syncCache.has(cacheKey)) {
            console.log('[BVE Sync] Ya sincronizado recientemente:', cacheKey);
            return;
        }

        syncCache.add(cacheKey);

        const payload = {
            tipoOperacion: modo,
            codigoAlumno: codAlumno || 'ALUMNO-URP',
            nombreAlumno: nomAlumno || 'Estudiante Medicina',
            codigoBarrasLibro: cBarras,
            tituloLibro: titulo || 'Material Bibliográfico URP',
            fechaDevolucion: fechaDevol
        };

        enviarAlServidor(payload);
    }

    function enviarAlServidor(payload, esPrueba = false) {
        console.log('[BVE Sync] Enviando datos a API:', payload);

        const topDoc = (window.top && window.top.document) ? window.top.document : document;
        const dot = topDoc.getElementById('bve-status-dot');
        const txt = topDoc.getElementById('bve-status-text');
        if (dot) dot.style.background = '#f59e0b';
        if (txt) txt.innerText = 'Enviando...';

        const jsonStr = JSON.stringify(payload);

        const onSuccess = (status, responseText) => {
            console.log('[BVE Sync] Respuesta servidor (' + status + '):', responseText);
            if (dot) dot.style.background = '#10b981';
            if (txt) txt.innerText = 'Sincronizado ✓';

            try {
                const res = JSON.parse(responseText);
                if (status >= 200 && status < 300) {
                    const msg = esPrueba ? '✅ Conexión con servidor exitosa (200 OK)' : `✅ ${res.mensaje || 'Préstamo guardado'}`;
                    showToast(msg, 'success');
                } else {
                    showToast(`⚠️ Servidor: ${res.mensaje || res.error || responseText}`, 'error');
                }
            } catch(e) {
                showToast(`✅ Sincronizado correctamente`, 'success');
            }
        };

        const onError = (err) => {
            console.error('[BVE Sync] Error al conectar:', err);
            if (dot) dot.style.background = '#ef4444';
            if (txt) txt.innerText = 'Error de conexión';
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
        for (const doc of allDocs) {
            doc.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    setTimeout(() => inspeccionarPantalla(), 500);
                    setTimeout(() => inspeccionarPantalla(), 1500);
                }
            });

            const observer = new MutationObserver(() => {
                inspeccionarPantalla();
            });

            if (doc.body || doc.documentElement) {
                observer.observe(doc.body || doc.documentElement, {
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
        // Reintentar crear widget cada 2s en caso de que los frames carguen tarde
        setInterval(createWidget, 2000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
