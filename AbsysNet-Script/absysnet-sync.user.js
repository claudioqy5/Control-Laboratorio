// ==UserScript==
// @name         AbsysNet -> Control Laboratorio & Biblioteca URP Sync
// @namespace    https://medicina.urp.edu.pe/bve
// @version      1.2.0
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
    
    // Obtener URL configurada o usar la por defecto
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

    console.log('[BVE Sync] Inicializando sincronizador BVE Medicina URP ->', API_URL);

    // Registro de préstamos ya sincronizados en esta sesión para evitar duplicados
    const syncCache = new Set();

    // ========================================================
    // WIDGET FLOTANTE DE CONTROL EN ABSYSNET
    // ========================================================
    function createWidget() {
        if (document.getElementById('bve-sync-widget')) return;

        const widget = document.createElement('div');
        widget.id = 'bve-sync-widget';
        widget.innerHTML = `
            <div style="
                position: fixed;
                bottom: 16px;
                right: 20px;
                background: #ffffff;
                color: #111827;
                padding: 10px 14px;
                border-radius: 12px;
                box-shadow: 0 10px 25px -3px rgba(0,0,0,0.22), 0 4px 6px -2px rgba(0,0,0,0.05);
                border: 2px solid #9f1239;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                font-size: 12px;
                z-index: 999999;
                display: flex;
                flex-direction: column;
                gap: 8px;
                user-select: none;
                min-width: 210px;
            ">
                <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px;">
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <div id="bve-status-dot" style="width: 10px; height: 10px; background: #10b981; border-radius: 50%; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25);"></div>
                        <div>
                            <div style="font-weight: 700; color: #9f1239; font-size: 12px; line-height: 1.2;">BVE Medicina URP</div>
                            <div id="bve-status-text" style="color: #6b7280; font-size: 10px;">Sincronizador Activo</div>
                        </div>
                    </div>
                    <button id="bve-cfg-btn" title="Configurar URL del Servidor" style="background: transparent; border: none; cursor: pointer; font-size: 14px; padding: 2px;">⚙️</button>
                </div>

                <div style="display: flex; gap: 6px;">
                    <button id="bve-capture-btn" style="
                        flex: 1;
                        background: #9f1239;
                        color: #ffffff;
                        border: none;
                        border-radius: 6px;
                        padding: 5px 8px;
                        font-size: 11px;
                        font-weight: 600;
                        cursor: pointer;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        gap: 4px;
                    ">📥 Capturar Ahora</button>

                    <button id="bve-test-btn" style="
                        background: #f3f4f6;
                        color: #374151;
                        border: 1px solid #d1d5db;
                        border-radius: 6px;
                        padding: 5px 8px;
                        font-size: 11px;
                        font-weight: 500;
                        cursor: pointer;
                    " title="Probar conexión con el servidor">⚡ Probar</button>
                </div>
            </div>
        `;

        document.body.appendChild(widget);

        // Botón Capturar Manual
        document.getElementById('bve-capture-btn').addEventListener('click', () => {
            showToast('Escaneando pantalla de AbsysNet...', 'info');
            const encontrado = inspeccionarPantalla(true);
            if (!encontrado) {
                showToast('No se encontró un libro o alumno en la pantalla actual.', 'error');
            }
        });

        // Botón Probar Conexión
        document.getElementById('bve-test-btn').addEventListener('click', () => {
            probarConexionServidor();
        });

        // Botón Configurar URL
        document.getElementById('bve-cfg-btn').addEventListener('click', () => {
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

    // ========================================================
    // TOAST NOTIFICACIÓN
    // ========================================================
    function showToast(mensaje, tipo = 'success') {
        const toast = document.createElement('div');
        const color = tipo === 'success' ? '#065f46' : (tipo === 'info' ? '#1e40af' : '#991b1b');
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${color};
            color: #ffffff;
            padding: 12px 18px;
            border-radius: 8px;
            box-shadow: 0 10px 25px rgba(0,0,0,0.25);
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            font-size: 13px;
            font-weight: 600;
            z-index: 1000000;
            transition: all 0.3s ease;
            max-width: 350px;
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
    // PROBAR CONEXIÓN CON EL SERVIDOR
    // ========================================================
    function probarConexionServidor() {
        showToast('Verificando conexión con el servidor...', 'info');
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
    // DETECCIÓN DE MODO (PRÉSTAMOS / DEVOLUCIONES)
    // ========================================================
    function detectarModo() {
        const texto = document.body.innerText || '';
        if (texto.includes('PRÉSTAMOS') || texto.includes('Fecha de préstamo')) {
            return 'PRESTAMO';
        }
        if (texto.includes('DEVOLUCIONES') || texto.includes('Fecha de devolución')) {
            return 'DEVOLUCION';
        }
        return 'PRESTAMO'; // Por defecto
    }

    // ========================================================
    // BUSCADOR INTELIGENTE DE DATOS DEL ALUMNO / LECTOR
    // ========================================================
    function obtenerDatosAlumno() {
        let codigo = '';
        let nombre = '';

        const inputs = Array.from(document.querySelectorAll('input'));

        // 1. Buscar campo por nombre/id que contenga "lector"
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

        // 2. Si no se encontró por name, buscar por proximidad a la etiqueta "Nº lector" o "Lector"
        if (!codigo) {
            const tags = document.querySelectorAll('td, span, div, label, b, th');
            for (const tag of tags) {
                const t = tag.innerText || '';
                if (t.includes('Nº lector') || t.includes('N° lector') || t.includes('Lector:')) {
                    const row = tag.closest('tr') || tag.parentElement;
                    if (row) {
                        const inp = row.querySelector('input[type="text"], input:not([type])');
                        if (inp && inp.value && inp.value.trim()) {
                            codigo = inp.value.trim();
                            break;
                        }
                    }
                }
            }
        }

        // 3. Obtener el nombre del alumno (suele estar en un input deshabilitado al lado)
        for (const input of inputs) {
            if (input.disabled || input.readOnly) {
                const v = (input.value || '').trim();
                // Si parece un nombre (contiene letras y espacios)
                if (v.length > 5 && /[A-Za-z]/.test(v) && v.includes(' ') && !v.includes('URP')) {
                    nombre = v;
                    break;
                }
            }
        }

        // 4. Buscar en el texto general si hay código numérico del lector (7 a 9 dígitos)
        if (!codigo) {
            const matchLector = document.body.innerText.match(/N[º°]\s*lector[:\s]*([0-9]{7,10})/i);
            if (matchLector) {
                codigo = matchLector[1];
            }
        }

        return { codigo, nombre };
    }

    // ========================================================
    // INSPECCIÓN DE LA TABLA Y CAMPOS DE LIBROS
    // ========================================================
    function inspeccionarPantalla(esManual = false) {
        const modo = detectarModo();
        const { codigo: codAlumno, nombre: nomAlumno } = obtenerDatosAlumno();

        console.log('[BVE Sync] Modo detectado:', modo, '| Alumno:', codAlumno, nomAlumno);

        let encontrado = false;

        // ESTRATEGIA 1: Revisar todas las tablas que contengan "C. Barras" o "Título"
        const tablas = document.querySelectorAll('table');
        tablas.forEach(tabla => {
            const rows = Array.from(tabla.rows || []);
            if (rows.length < 2) return;

            const encabezado = (rows[0].innerText || '').toLowerCase();
            if (encabezado.includes('barras') || encabezado.includes('título') || encabezado.includes('ejemp') || encabezado.includes('fecha')) {
                
                // Recorrer las filas de datos
                for (let i = 1; i < rows.length; i++) {
                    const cells = Array.from(rows[i].cells || []);
                    if (cells.length < 4) continue;

                    let cBarras = '';
                    let titulo = '';
                    let fechaDevol = null;

                    for (const cell of cells) {
                        const txt = (cell.innerText || '').trim();
                        // Si parece un código de barras (números o alfanumérico corto 4-20 chars)
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

        // ESTRATEGIA 2: Si no hay tabla pero el usuario tiene escrito en "código de barras"
        if (!encontrado) {
            const inputs = Array.from(document.querySelectorAll('input'));
            for (const input of inputs) {
                const name = (input.name || '').toLowerCase();
                if (name.includes('barras') || name.includes('ejemplar')) {
                    const val = (input.value || '').trim();
                    if (val.length >= 4) {
                        encontrado = true;
                        sincronizarLibro(modo, codAlumno, nomAlumno, val, 'Libro Escaneado en AbsysNet', null);
                        break;
                    }
                }
            }
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
    // ENVÍO AL BACKEND
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
        console.log('[BVE Sync] Enviando payload a API:', payload);

        const dot = document.getElementById('bve-status-dot');
        const txt = document.getElementById('bve-status-text');
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
    // ESCUCHA DE EVENTOS (TECLADO / ENTER / SCANNER)
    // ========================================================
    function setupListeners() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                setTimeout(() => inspeccionarPantalla(), 500);
                setTimeout(() => inspeccionarPantalla(), 1500);
            }
        });
    }

    // ========================================================
    // MUTATION OBSERVER
    // ========================================================
    function setupObserver() {
        const observer = new MutationObserver(() => {
            inspeccionarPantalla();
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // INICIAR
    function init() {
        createWidget();
        setupListeners();
        setupObserver();
        inspeccionarPantalla();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
