// ==UserScript==
// @name         AbsysNet -> Control Laboratorio & Biblioteca URP Sync
// @namespace    https://medicina.urp.edu.pe/bve
// @version      1.6.0
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
    // BUSCAR EL DOCUMENTO DESTINO ADECUADO (SOLO EN PRÉSTAMOS / DEVOLUCIONES)
    // ========================================================
    function findTargetDocument() {
        const allDocs = getAllDocuments();

        // Solo inyectar en el frame que contenga el formulario de Préstamos o Devoluciones
        for (const doc of allDocs) {
            if (!doc.body) continue;

            const txt = (doc.body.innerText || '').toLowerCase();
            const hasCirculationTerms = txt.includes('detalles del lector') || 
                                        txt.includes('detalles del ejemplar') || 
                                        txt.includes('fecha de préstamo') || 
                                        txt.includes('fecha de devolución') || 
                                        txt.includes('nº lector') || 
                                        txt.includes('n° lector');

            const hasInputs = Array.from(doc.querySelectorAll('input')).some(i => {
                const n = (i.name || '').toLowerCase();
                const id = (i.id || '').toLowerCase();
                return n.includes('lector') || n.includes('barras') || id.includes('lector') || id.includes('barras');
            });

            if (hasCirculationTerms || hasInputs) {
                return doc;
            }
        }

        // Si estamos en el "Escritorio", "Notas", "Enlaces", etc., no mostramos el widget para no saturar la pantalla
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
                <div style="
                    position: fixed !important;
                    bottom: 18px !important;
                    right: 25px !important;
                    background: #ffffff !important;
                    color: #111827 !important;
                    padding: 10px 14px !important;
                    border-radius: 12px !important;
                    box-shadow: 0 10px 25px rgba(0,0,0,0.25), 0 4px 8px rgba(159,18,57,0.15) !important;
                    border: 2px solid #9f1239 !important;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
                    font-size: 12px !important;
                    z-index: 2147483647 !important;
                    display: flex !important;
                    flex-direction: column !important;
                    gap: 8px !important;
                    user-select: none !important;
                    min-width: 220px !important;
                ">
                    <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
                        <div style="display: flex; align-items: center; gap: 6px;">
                            <div class="bve-status-dot" style="width: 10px; height: 10px; background: #10b981; border-radius: 50%; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.3);"></div>
                            <div>
                                <div style="font-weight: 700; color: #9f1239; font-size: 11px; line-height: 1.2;">BVE Medicina URP</div>
                                <div class="bve-status-text" style="color: #6b7280; font-size: 10px;">Sincronizador Activo</div>
                            </div>
                        </div>
                        <button class="bve-cfg-btn" title="Configurar URL del Servidor" style="background: transparent; border: none; cursor: pointer; font-size: 13px; padding: 2px;">⚙️</button>
                    </div>

                    <div style="display: flex; gap: 6px;">
                        <button class="bve-capture-btn" style="
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
                        ">📥 Sincronizar Ahora</button>

                        <button class="bve-test-btn" style="
                            background: #f3f4f6;
                            color: #374151;
                            border: 1px solid #d1d5db;
                            border-radius: 6px;
                            padding: 6px 8px;
                            font-size: 10px;
                            font-weight: 600;
                            cursor: pointer;
                        " title="Verificar conectividad con el servidor (sin registrar préstamos)">⚡ Probar</button>
                    </div>
                </div>
            `;

            targetDoc.body.appendChild(widget);

            // Listeners
            const captureBtn = widget.querySelector('.bve-capture-btn');
            if (captureBtn) {
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
                testBtn.addEventListener('click', () => {
                    probarConexionServidor();
                });
            }

            const cfgBtn = widget.querySelector('.bve-cfg-btn');
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
    // TOAST NOTIFICACIÓN EN LA PANTALLA (SINGLETON)
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
            const color = tipo === 'success' ? '#065f46' : (tipo === 'info' ? '#1e40af' : '#991b1b');
            toast.style.cssText = `
                position: fixed !important;
                top: 20px !important;
                right: 20px !important;
                background: ${color} !important;
                color: #ffffff !important;
                padding: 12px 18px !important;
                border-radius: 8px !important;
                box-shadow: 0 10px 25px rgba(0,0,0,0.35) !important;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
                font-size: 13px !important;
                font-weight: 600 !important;
                z-index: 2147483647 !important;
                transition: all 0.3s ease !important;
                max-width: 360px !important;
            `;
            toast.innerText = mensaje;
            targetDoc.body.appendChild(toast);

            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transform = 'translateY(-10px)';
                setTimeout(() => toast.remove(), 300);
            }, 4000);
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
                showToast('✅ Conexión con servidor exitosa (200 OK)', 'success');
            } else {
                showToast('⚠️ Respuesta del servidor: HTTP ' + status, 'error');
            }
        };

        const onPingError = () => {
            showToast('❌ Error al conectar con ' + pingUrl, 'error');
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
    // DETECCIÓN DE MODO (PRESTAMO / DEVOLUCION)
    // ========================================================
    function detectarModo(targetDoc) {
        if (!targetDoc || !targetDoc.body) return 'PRESTAMO';
        const txt = targetDoc.body.innerText || '';

        // 1. Verificación precisa en la cabecera de la ventana activa
        if (txt.includes('Fecha de préstamo') || txt.includes('PRÉSTAMOS |') || txt.includes('PRÉSTAMO')) {
            return 'PRESTAMO';
        }
        if (txt.includes('Fecha de devolución') || txt.includes('DEVOLUCIONES |') || txt.includes('DEVOLUCIÓN')) {
            return 'DEVOLUCION';
        }

        // 2. Por minúsculas
        const lower = txt.toLowerCase();
        if (lower.includes('fecha de préstamo') || lower.includes('préstamo')) {
            return 'PRESTAMO';
        }
        if (lower.includes('fecha de devolución') || lower.includes('devolución')) {
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

        const inputs = Array.from(targetDoc.querySelectorAll('input'));

        // 1. Buscar código universitario de alumno:
        // En la URP tiene entre 8 y 10 dígitos (ej. 201712043)
        for (const input of inputs) {
            const v = (input.value || '').trim();
            if (/^\d{8,10}$/.test(v)) {
                codigo = v;
                break;
            }
        }

        // 2. Si no lo encontramos por valor directo, buscar inputs junto a la etiqueta "lector"
        if (!codigo) {
            const elements = targetDoc.querySelectorAll('td, th, span, div, label, b');
            for (const el of elements) {
                const t = (el.innerText || '').toLowerCase();
                if (t.includes('lector') && !t.includes('tipo')) {
                    const tr = el.closest('tr') || el.parentElement;
                    if (tr) {
                        const inps = tr.querySelectorAll('input');
                        for (const inp of inps) {
                            const v = (inp.value || '').trim();
                            if (/^\d{6,12}$/.test(v)) {
                                codigo = v;
                                break;
                            }
                        }
                    }
                }
                if (codigo) break;
            }
        }

        // 3. Buscar nombre del alumno:
        // En AbsysNet, al lado del código de lector hay una caja con el nombre en mayúsculas (ej. "QUELLO YAPU, CLAUDIO FERNANDO")
        for (const input of inputs) {
            const v = (input.value || '').trim();
            if (v.length > 5 && v.includes(',') && /[A-Z]/.test(v) && !v.includes('URP') && !v.includes('MEDICINA')) {
                nombre = v;
                break;
            }
        }

        if (!nombre) {
            for (const input of inputs) {
                if (input.readOnly || input.disabled) {
                    const v = (input.value || '').trim();
                    if (v.length > 6 && v.includes(' ') && /[A-Z]/.test(v) && !v.includes('URP') && !v.includes('MEDICINA')) {
                        nombre = v;
                        break;
                    }
                }
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
                if (txt) txt.innerText = 'Sincronizado ✓';
            });

            try {
                const res = JSON.parse(responseText);
                if (status >= 200 && status < 300) {
                    showToast(`✅ ${res.mensaje || 'Préstamo guardado'} (${res.alumno || payload.codigoAlumno})`, 'success');
                } else {
                    showToast(`⚠️ Servidor: ${res.mensaje || res.error || responseText}`, 'error');
                }
            } catch(e) {
                showToast(`✅ Sincronizado correctamente`, 'success');
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
