(() => {
    'use strict';

    const app = document.querySelector('#app');
    const toastRegion = document.querySelector('#toast-region');
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const state = {
        cocktails: [],
        ingredients: [],
        pumps: [],
        sizes: [],
        system: null,
        version: '',
        update: null,
        activeTab: 'cocktails',
        editing: null,
        cocktailDraft: null,
        selectedCocktail: null,
        selectedSizeId: null,
        pollTimer: null,
        completionTimer: null,
        primingPollTimer: null
    };

    const icons = {
        sun: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/></svg>',
        moon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M20.5 14.1A8.5 8.5 0 0 1 9.9 3.5 8.5 8.5 0 1 0 20.5 14.1Z"/></svg>',
        logo: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M5 3h14l-6 7v7.5"/><path d="M8.5 21h7M12 17.5V21M7.2 6h9.6"/></svg>',
        settings: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-2.83 2.83-.06-.06a1.7 1.7 0 0 0-1.88-.34 1.7 1.7 0 0 0-1.03 1.55V21h-4v-.08A1.7 1.7 0 0 0 8.97 19.4a1.7 1.7 0 0 0-1.88.34l-.06.06-2.83-2.83.06-.06A1.7 1.7 0 0 0 4.6 15a1.7 1.7 0 0 0-1.52-1.03H3v-4h.08A1.7 1.7 0 0 0 4.6 8.94a1.7 1.7 0 0 0-.34-1.88L4.2 7l2.83-2.83.06.06a1.7 1.7 0 0 0 1.88.34A1.7 1.7 0 0 0 10 3.05V3h4v.08a1.7 1.7 0 0 0 1.03 1.52 1.7 1.7 0 0 0 1.88-.34l.06-.06L19.8 7l-.06.06a1.7 1.7 0 0 0-.34 1.88A1.7 1.7 0 0 0 20.92 10H21v4h-.08A1.7 1.7 0 0 0 19.4 15Z"/></svg>',
        close: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m6 6 12 12M18 6 6 18"/></svg>',
        play: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m8 5 11 7-11 7z"/></svg>',
        stop: '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><rect x="5" y="5" width="14" height="14" rx="2"/></svg>',
        back: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m15 18-6-6 6-6"/></svg>',
        edit: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="m4 16-.8 4.8L8 20l11-11-4-4zM13.5 6.5l4 4"/></svg>',
        trash: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 7h16M9 3h6l1 4H8zM6 7l1 14h10l1-14M10 11v6M14 11v6"/></svg>',
        plus: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M12 5v14M5 12h14"/></svg>',
        clean: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 3S6.5 9.1 6.5 14a5.5 5.5 0 0 0 11 0C17.5 9.1 12 3 12 3Z"/><path d="M9.4 15.2a2.8 2.8 0 0 0 2.8 2.2"/></svg>',
        prime: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M6 20V8.5a6 6 0 0 1 12 0V20"/><path d="M3 20h18M9 12h6M12 4v8"/><path d="M12 16v2"/></svg>',
        raspberryPi: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="4" y="4" width="16" height="16" rx="3"/><path d="M8 8h8v8H8zM2 9h2M2 15h2M20 9h2M20 15h2M9 2v2M15 2v2M9 20v2M15 20v2"/></svg>',
        download: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 3v12M7 10l5 5 5-5"/><path d="M5 21h14"/></svg>',
        calibrate: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 17a8 8 0 1 1 16 0"/><path d="m12 13 4-4"/><path d="M6 17h12"/><circle cx="12" cy="17" r="1" fill="currentColor" stroke="none"/></svg>',
        lock: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/><path d="M12 14v3"/></svg>',
        refill: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 3S6.5 9.1 6.5 14a5.5 5.5 0 0 0 11 0C17.5 9.1 12 3 12 3Z"/><path d="M12 10v7M8.5 13.5h7"/></svg>',
        cocktail: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 4h16l-7 8v7"/><path d="M8 22h8M12 19v3M7 8h10"/></svg>',
        ingredient: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 3S6.5 9.1 6.5 14a5.5 5.5 0 0 0 11 0C17.5 9.1 12 3 12 3Z"/><path d="M9.5 15.5a3 3 0 0 0 2.8 2"/></svg>',
        size: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M6 3h12l-1 18H7L6 3Z"/><path d="M7 8h10M8 13h8"/></svg>'
    };

    const raspberryPiHeader = [
        [{ board: 1, label: '3v3' }, { board: 2, label: '5v' }],
        [{ board: 3, bcm: 2 }, { board: 4, label: '5v' }],
        [{ board: 5, bcm: 3 }, { board: 6, label: 'gnd' }],
        [{ board: 7, bcm: 4 }, { board: 8, bcm: 14 }],
        [{ board: 9, label: 'gnd' }, { board: 10, bcm: 15 }],
        [{ board: 11, bcm: 17 }, { board: 12, bcm: 18 }],
        [{ board: 13, bcm: 27 }, { board: 14, label: 'gnd' }],
        [{ board: 15, bcm: 22 }, { board: 16, bcm: 23 }],
        [{ board: 17, label: '3v3' }, { board: 18, bcm: 24 }],
        [{ board: 19, bcm: 10 }, { board: 20, label: 'gnd' }],
        [{ board: 21, bcm: 9 }, { board: 22, bcm: 25 }],
        [{ board: 23, bcm: 11 }, { board: 24, bcm: 8 }],
        [{ board: 25, label: 'gnd' }, { board: 26, bcm: 7 }],
        [{ board: 27, bcm: 0 }, { board: 28, bcm: 1 }],
        [{ board: 29, bcm: 5 }, { board: 30, label: 'gnd' }],
        [{ board: 31, bcm: 6 }, { board: 32, bcm: 12 }],
        [{ board: 33, bcm: 13 }, { board: 34, label: 'gnd' }],
        [{ board: 35, bcm: 19 }, { board: 36, bcm: 16 }],
        [{ board: 37, bcm: 26 }, { board: 38, bcm: 20 }],
        [{ board: 39, label: 'gnd' }, { board: 40, bcm: 21 }]
    ];

    document.addEventListener('DOMContentLoaded', initialize);
    window.addEventListener('hashchange', renderRoute);

    async function initialize() {
        renderLoading();
        try {
            const networkAccess = await api('/api/network-access/status');
            if (networkAccess.theme) applyTheme(networkAccess.theme);
            if (networkAccess.requiresPin && !networkAccess.isAuthenticated) {
                renderNetworkPinPrompt();
                return;
            }
            await loadAll();
            renderRoute();
            const current = await api('/api/dispenses/current');
            if (current.status === 'running') {
                if (current.mode === 'cleaning') showCleaningProgress(current);
                else if (current.mode === 'priming') {
                    await api('/api/priming/current/stop', { method: 'POST' });
                    showToast('Die laufende Pumpenvorbereitung wurde aus Sicherheitsgründen gestoppt.');
                }
                else if (current.mode === 'calibration') {
                    const pumpId = current.steps?.[0]?.pumpId;
                    if (pumpId) openCalibrationDialog(pumpId, current);
                }
                else showDispense(current);
            }
        } catch (error) {
            renderFatal(error.message);
        }
    }

    function pinEntryMarkup(id, label, autocomplete = 'off') {
        return `<div class="pin-entry" id="${id}" role="group" aria-label="${escapeHtml(label)}">${[0, 1, 2, 3].map(index => `<input type="password" inputmode="numeric" pattern="[0-9]" maxlength="1" data-pin-digit aria-label="${escapeHtml(label)}, Ziffer ${index + 1}" autocomplete="${index === 0 ? autocomplete : 'off'}">`).join('')}</div>`;
    }

    function pinKeypadMarkup(label) {
        return `<div class="pin-keypad" role="group" aria-label="${escapeHtml(label)}">${[1, 2, 3, 4, 5, 6, 7, 8, 9].map(digit => `<button type="button" data-pin-key="${digit}" aria-label="Ziffer ${digit}">${digit}</button>`).join('')}<span aria-hidden="true"></span><button type="button" data-pin-key="0" aria-label="Ziffer 0">0</button><button type="button" class="pin-keypad-delete" data-pin-delete aria-label="Letzte Ziffer löschen">⌫</button></div>`;
    }

    function bindPinEntry(container) {
        const inputs = [...container.querySelectorAll('[data-pin-digit]')];
        const writeDigits = (digits, startIndex = 0) => {
            [...digits.replace(/\D/g, '')].slice(0, inputs.length - startIndex).forEach((digit, offset) => { inputs[startIndex + offset].value = digit; });
            return Math.min(inputs.length - 1, startIndex + digits.replace(/\D/g, '').length);
        };
        const value = () => inputs.map(input => input.value).join('');
        const notify = () => container.dispatchEvent(new CustomEvent('pinchange', { bubbles: true, detail: { value: value() } }));
        inputs.forEach((input, index) => {
            input.addEventListener('input', () => {
                const digits = input.value.replace(/\D/g, '');
                if (digits.length > 1) {
                    const nextIndex = writeDigits(digits, index);
                    inputs[nextIndex]?.focus();
                } else {
                    input.value = digits;
                    if (digits && index < inputs.length - 1) inputs[index + 1].focus();
                }
                notify();
            });
            input.addEventListener('keydown', event => {
                if (event.key === 'Backspace' && !input.value && index > 0) {
                    event.preventDefault();
                    inputs[index - 1].value = '';
                    inputs[index - 1].focus();
                    notify();
                }
                if (event.key === 'ArrowLeft' && index > 0) inputs[index - 1].focus();
                if (event.key === 'ArrowRight' && index < inputs.length - 1) inputs[index + 1].focus();
            });
            input.addEventListener('paste', event => {
                event.preventDefault();
                const nextIndex = writeDigits(event.clipboardData?.getData('text') || '', index);
                inputs[nextIndex]?.focus();
                notify();
            });
        });
        return {
            value,
            clear: () => { inputs.forEach(input => { input.value = ''; }); notify(); },
            append: digit => {
                const index = inputs.findIndex(input => !input.value);
                if (index < 0 || !/^\d$/.test(String(digit))) return;
                inputs[index].value = String(digit);
                inputs[Math.min(index + 1, inputs.length - 1)].focus();
                notify();
            },
            backspace: () => {
                const target = inputs.map(input => input.value).map((value, index) => value ? index : -1).filter(index => index >= 0).at(-1);
                if (target === undefined) return;
                inputs[target].value = '';
                inputs[target].focus();
                notify();
            },
            focus: () => inputs[0]?.focus()
        };
    }

    function bindPinKeypad(keypad, pinEntry) {
        keypad.addEventListener('click', event => {
            const button = event.target.closest('button');
            if (!button) return;
            if (button.hasAttribute('data-pin-delete')) pinEntry.backspace();
            else pinEntry.append(button.dataset.pinKey);
        });
    }

    function renderNetworkPinPrompt() {
        app.innerHTML = `${headerTemplate()}<main class="network-pin-gate" id="app-main"><form class="network-pin-form"><section class="network-pin-gate-card"><div class="network-pin-login-copy"><span class="network-pin-gate-icon" aria-hidden="true">${icons.lock}</span><h1>PIN eingeben</h1><p>Diese CocktailOS-Station ist über das Netzwerk geschützt.</p><span class="pin-entry-label">4-stelliger PIN</span>${pinEntryMarkup('network-login-pin', 'Netzwerk-PIN', 'one-time-code')}<p class="network-pin-error" role="alert" hidden></p><span class="network-pin-auto-hint">Nach der vierten Ziffer wird der Zugang geöffnet.</span></div><div class="network-pin-login-keypad">${pinKeypadMarkup('Zahlenfeld für Netzwerk-PIN')}</div></section></form></main>`;
        const form = app.querySelector('.network-pin-form');
        const pinEntry = bindPinEntry(form.querySelector('.pin-entry'));
        bindPinKeypad(form.querySelector('.pin-keypad'), pinEntry);
        const error = form.querySelector('.network-pin-error');
        const controls = [...form.querySelectorAll('input, .pin-keypad button')];
        let isAuthenticating = false;
        const authenticate = async () => {
            const pin = pinEntry.value();
            if (!/^\d{4}$/.test(pin) || isAuthenticating) return;
            isAuthenticating = true;
            controls.forEach(control => { control.disabled = true; });
            error.hidden = true;
            try {
                await api('/api/network-access/authenticate', { method: 'POST', body: { pin } });
                await initialize();
            } catch {
                controls.forEach(control => { control.disabled = false; });
                isAuthenticating = false;
                pinEntry.clear();
                pinEntry.focus();
                error.textContent = 'PIN ist nicht korrekt.';
                error.hidden = false;
            }
        };
        form.addEventListener('pinchange', () => {
            error.hidden = true;
            if (pinEntry.value().length === 4) void authenticate();
        });
        form.addEventListener('submit', async event => {
            event.preventDefault();
            await authenticate();
        });
        pinEntry.focus();
    }

    function openNetworkPinSetupDialog({ onSave, onCancel }) {
        const layer = createSettingsDialog('network-pin-setup-dialog', { dismissible: false });
        const body = `<form class="network-pin-setup-form"><div class="network-pin-step"><span class="network-pin-step-count">Schritt <span data-pin-step-number>1</span> von 2</span><span class="pin-entry-label" data-pin-step-label>PIN festlegen</span>${pinEntryMarkup('network-setup-pin', 'Neuer Netzwerk-PIN', 'new-password')}${pinKeypadMarkup('Zahlenfeld für neuen Netzwerk-PIN')}</div><p class="network-pin-error" role="alert" hidden></p><div class="network-pin-setup-actions"><span>Nach der vierten Ziffer geht es automatisch weiter.</span><button type="button" class="secondary-button network-pin-cancel">Abbrechen</button></div></form>`;
        setSettingsDialogContent(layer, 'Netzwerk-PIN festlegen', 'Lege einen vierstelligen PIN fest. Geräte im Netzwerk brauchen ihn, bevor sie CocktailOS öffnen können.', body, { showClose: false, headingIcon: icons.lock });
        const form = layer.querySelector('.network-pin-setup-form');
        const error = form.querySelector('.network-pin-error');
        const pinEntry = bindPinEntry(form.querySelector('#network-setup-pin'));
        bindPinKeypad(form.querySelector('.pin-keypad'), pinEntry);
        let firstPin = '';
        let confirming = false;
        let isAdvancing = false;
        const advance = () => {
            const pin = pinEntry.value();
            if (!/^\d{4}$/.test(pin) || isAdvancing) return;
            isAdvancing = true;
            error.hidden = true;
            if (!confirming) {
                firstPin = pin;
                confirming = true;
                pinEntry.clear();
                form.querySelector('[data-pin-step-number]').textContent = '2';
                form.querySelector('[data-pin-step-label]').textContent = 'PIN wiederholen';
                form.querySelector('.pin-entry').setAttribute('aria-label', 'Netzwerk-PIN wiederholen');
                isAdvancing = false;
                pinEntry.focus();
                return;
            }
            if (pin !== firstPin) {
                error.textContent = 'Die PIN-Eingaben stimmen nicht überein. Bitte erneut versuchen.';
                error.hidden = false;
                confirming = false;
                firstPin = '';
                pinEntry.clear();
                form.querySelector('[data-pin-step-number]').textContent = '1';
                form.querySelector('[data-pin-step-label]').textContent = 'PIN festlegen';
                form.querySelector('.pin-entry').setAttribute('aria-label', 'Neuer Netzwerk-PIN');
                isAdvancing = false;
                pinEntry.focus();
                return;
            }
            onSave(firstPin);
            dismissSettingsDialog(layer);
        };
        form.addEventListener('pinchange', advance);
        form.querySelector('.network-pin-cancel').addEventListener('click', () => {
            dismissSettingsDialog(layer);
            onCancel?.();
        });
        pinEntry.focus();
    }

    async function loadAll() {
        const [cocktails, ingredients, pumps, sizes, system, appInfo] = await Promise.all([
            api('/api/cocktails'), api('/api/ingredients'), api('/api/pumps'), api('/api/sizes'), api('/api/system'), api('/api/app-info')
        ]);
        Object.assign(state, { cocktails, ingredients, pumps, sizes, system, version: appInfo.version });
        try {
            state.update = await api('/api/app-update');
        } catch {
            state.update = null;
        }
        applyTheme(system.theme);
    }

    function applyTheme(theme) {
        document.documentElement.dataset.theme = theme === 'Light' ? 'light' : 'dark';
    }

    function renderRoute() {
        clearTimers();
        if (location.hash.startsWith('#/settings')) renderSettings();
        else renderHome();
    }

    function renderLoading() {
        app.innerHTML = `${headerTemplate('Cocktailmaschine wird geladen …')}<main class="loading-state" id="app-main" aria-busy="true"><div class="loading-indicator" aria-hidden="true"></div><span>Cocktails werden vorbereitet …</span></main>`;
    }

    function renderFatal(message) {
        app.innerHTML = `${headerTemplate('Nicht verbunden', 'error')}<main class="loading-state" id="app-main"><div><h1>Verbindung fehlgeschlagen</h1><p>${escapeHtml(message)}</p><button class="primary-button" onclick="location.reload()">Erneut versuchen</button></div></main>`;
    }

    function headerTemplate(label = '', statusClass = '', showSettings = false) {
        const status = label ? `<div class="machine-state" role="status"><span class="state-dot ${statusClass}"></span><span>${escapeHtml(label)}</span></div>` : '';
        const networkWarning = state.system?.networkAccessEnabled
            ? `<div class="network-access-warning" role="alert"><span aria-hidden="true">!</span><span>Im Netzwerk erreichbar unter: ${escapeHtml(state.system.networkAddress || 'IP-Adresse:PORT')}</span></div>`
            : '';
        return `<header class="topbar">
            <div class="brand"><div class="brand-mark">${icons.logo}</div><div class="brand-copy"><strong>CocktailOS</strong><span>Mixstation</span></div></div>
            <div class="header-actions">${networkWarning}${status}${showSettings ? `<button class="icon-button settings-button" aria-label="Einstellungen öffnen" title="Einstellungen öffnen">${icons.settings}</button>` : ''}</div>
        </header>`;
    }

    function getCocktailAvailability(cocktail, size) {
        const scale = size.volumeMl / cocktail.standardSize.volumeMl;
        const missingPump = cocktail.ingredients.filter(item => !item.hasActivePump).map(item => item.name);
        const insufficient = cocktail.ingredients
            .filter(item => Number(item.remainingVolumeMl) + .001 < Number(item.amountMl) * scale)
            .map(item => item.name);
        const unavailable = [...new Set([...missingPump, ...insufficient])];
        const lowIngredients = cocktail.ingredients
            .filter(item => Number(item.remainingVolumeMl) > 0 && Number(item.remainingVolumeMl) <= 150)
            .map(item => item.name);
        const reasons = [];
        if (missingPump.length) reasons.push(`keine aktive Pumpe für ${missingPump.join(', ')}`);
        if (insufficient.length) reasons.push(`zu wenig ${insufficient.join(', ')}`);
        return {
            status: unavailable.length ? 'unavailable' : lowIngredients.length ? 'low' : 'available',
            reason: reasons.join(' · '),
            unavailableIngredients: unavailable,
            lowIngredients
        };
    }

    function getCocktailCardAvailability(cocktail) {
        const standard = getCocktailAvailability(cocktail, cocktail.standardSize);
        if (standard.status !== 'unavailable') return standard;
        const hasAvailableSize = state.sizes.some(size => getCocktailAvailability(cocktail, size).status !== 'unavailable');
        return hasAvailableSize ? { ...standard, status: 'limited' } : standard;
    }

    function renderHome() {
        const pages = [];
        for (let index = 0; index < state.cocktails.length; index += 5) {
            const cards = state.cocktails.slice(index, index + 5).map((cocktail, cardIndex) => {
                const availability = getCocktailCardAvailability(cocktail);
                const stockPill = availability.status === 'low'
                    ? '<span class="meta-pill stock-pill low">Vorrat niedrig</span>'
                    : availability.status === 'unavailable'
                        ? '<span class="meta-pill stock-pill unavailable">Nicht verfügbar</span>'
                        : availability.status === 'limited'
                            ? '<span class="meta-pill stock-pill low">Nur kleinere Größe</span>'
                            : '';
                return `<button class="cocktail-card ${cardIndex === 0 ? 'cocktail-card-featured' : ''} ${availability.status === 'unavailable' ? 'is-unavailable' : ''}" data-cocktail-id="${cocktail.id}" aria-label="${escapeHtml(cocktail.name)} auswählen${availability.status === 'unavailable' ? ', derzeit nicht verfügbar' : ''}">
                <span class="card-media"><img class="card-image" src="${safeImage(cocktail.imagePath)}" alt="" width="400" height="520" loading="${index + cardIndex < 3 ? 'eager' : 'lazy'}" decoding="async"></span>
                <span class="card-shade" aria-hidden="true"></span>
                <span class="card-content"><span class="card-meta"><span class="meta-pill">${cocktail.standardSize.volumeMl} ml</span>${cocktail.alcoholPercentage > 0 ? `<span class="meta-pill">${formatNumber(cocktail.alcoholPercentage)} % Vol.</span>` : '<span class="meta-pill">Alkoholfrei</span>'}${stockPill}</span><h2>${escapeHtml(cocktail.name)}</h2><p>${escapeHtml(cocktail.description || ingredientSummary(cocktail))}</p></span>
            </button>`;
            }).join('');
            pages.push(`<section class="cocktail-page" aria-label="Cocktailseite ${pages.length + 1}">${cards}</section>`);
        }

        const content = pages.length
            ? pages.join('')
            : '<section class="cocktail-page cocktail-page-empty"><div class="empty-state">Noch keine Cocktails angelegt.</div></section>';

        app.innerHTML = `${headerTemplate('', '', true)}<main class="home-main" id="app-main"><header class="home-heading"><div><h1>Cocktail wählen</h1><p>Rezept antippen, Größe festlegen, mixen.</p></div></header><div class="cocktail-scroller" aria-label="Cocktails">${content}</div></main>`;

        app.querySelectorAll('[data-cocktail-id]').forEach(button => button.addEventListener('click', () => openCocktail(Number(button.dataset.cocktailId))));
        app.querySelector('.settings-button').addEventListener('click', () => { location.hash = '#/settings'; });
        animateCards();
    }

    function animateCards() {
        if (!window.gsap || reduceMotion) return;
        gsap.from('.home-main', { opacity: 0, duration: .2, ease: 'power1.out' });
    }

    function openCocktail(id) {
        state.selectedCocktail = state.cocktails.find(x => x.id === id);
        const standardAvailability = getCocktailAvailability(state.selectedCocktail, state.selectedCocktail.standardSize);
        const firstAvailableSize = state.sizes.find(size => getCocktailAvailability(state.selectedCocktail, size).status !== 'unavailable');
        state.selectedSizeId = standardAvailability.status !== 'unavailable'
            ? state.selectedCocktail.standardSize.id
            : firstAvailableSize?.id ?? state.selectedCocktail.standardSize.id;
        const layer = document.createElement('div');
        layer.className = 'modal-layer';
        layer.innerHTML = cocktailModalTemplate();
        app.append(layer);
        bindCocktailModal(layer);
        if (window.gsap && !reduceMotion) {
            gsap.from(layer.querySelector('.modal-scrim'), { opacity: 0, duration: .2 });
            gsap.from(layer.querySelector('.cocktail-modal'), { opacity: 0, y: 28, scale: .97, duration: .38, ease: 'power3.out' });
        }
    }

    function cocktailModalTemplate() {
        const cocktail = state.selectedCocktail;
        const size = state.sizes.find(x => x.id === state.selectedSizeId) || cocktail.standardSize;
        const scale = size.volumeMl / cocktail.standardSize.volumeMl;
        const ingredients = cocktail.ingredients.map(item => `<li><span>${escapeHtml(item.name)}</span><span>${formatNumber(item.amountMl * scale)} ml</span></li>`).join('');
        const availability = getCocktailAvailability(cocktail, size);
        const sizes = state.sizes.map(item => {
            const sizeAvailability = getCocktailAvailability(cocktail, item);
            const disabled = sizeAvailability.status === 'unavailable';
            return `<button class="size-option" data-size-id="${item.id}" aria-pressed="${item.id === size.id}" ${disabled ? 'disabled' : ''} title="${disabled ? escapeHtml(sizeAvailability.reason) : ''}"><strong>${escapeHtml(item.name)}</strong><br><small>${item.volumeMl} ml</small></button>`;
        }).join('');
        const availabilityNote = availability.status === 'low'
            ? `<p class="availability-note low">Vorrat niedrig: ${escapeHtml(availability.lowIngredients.join(', '))}</p>`
            : availability.status === 'unavailable'
                ? `<p class="availability-note unavailable">Nicht verfügbar: ${escapeHtml(availability.reason)}</p>`
                : '';
        const startLabel = availability.status === 'unavailable' ? 'Derzeit nicht verfügbar' : `${icons.play} Cocktail starten`;
        return `<div class="modal-scrim"></div><section class="cocktail-modal" role="dialog" aria-modal="true" aria-labelledby="cocktail-title">
            <button class="modal-close" aria-label="Dialog schließen">${icons.close}</button>
            <div class="modal-media"><img src="${safeImage(cocktail.imagePath)}" alt="${escapeHtml(cocktail.name)}" width="400" height="520"></div>
            <div class="modal-body"><h2 id="cocktail-title">${escapeHtml(cocktail.name)}</h2><p class="modal-description">${escapeHtml(cocktail.description || '')}</p><ul class="ingredient-list">${ingredients}</ul><span class="field-label">Größe auswählen</span><div class="size-selector">${sizes}</div>${availabilityNote}<button class="primary-button wide-button start-dispense" ${availability.status === 'unavailable' ? 'disabled' : ''}>${startLabel}</button></div>
        </section>`;
    }

    function bindCocktailModal(layer) {
        const close = () => closeLayer(layer);
        layer.querySelector('.modal-close').addEventListener('click', close);
        layer.querySelector('.modal-scrim').addEventListener('click', close);
        layer.querySelectorAll('[data-size-id]').forEach(button => button.addEventListener('click', () => {
            state.selectedSizeId = Number(button.dataset.sizeId);
            layer.innerHTML = cocktailModalTemplate();
            bindCocktailModal(layer);
        }));
        layer.querySelector('.start-dispense')?.addEventListener('click', startDispense);
        layer.querySelector('.modal-close').focus();
    }

    async function startDispense(event) {
        const button = event.currentTarget;
        button.disabled = true;
        button.textContent = 'Wird vorbereitet …';
        try {
            const status = await api('/api/dispenses', { method: 'POST', body: { cocktailId: state.selectedCocktail.id, sizeId: state.selectedSizeId } });
            showDispense(status);
        } catch (error) {
            button.disabled = false;
            button.innerHTML = `${icons.play} Cocktail starten`;
            showToast(error.message, true);
        }
    }

    function showDispense(status) {
        document.querySelector('.modal-layer')?.remove();
        const layer = document.createElement('div');
        layer.className = 'modal-layer';
        layer.innerHTML = `<div class="modal-scrim"></div><section class="dispense-panel" role="dialog" aria-modal="true" aria-labelledby="dispense-title"><div class="glass-stage"><div class="glass" aria-hidden="true"><div class="liquid"><i class="bubble one"></i><i class="bubble two"></i><i class="bubble three"></i></div></div></div><div class="dispense-copy"><span class="eyebrow">Wird gemixt</span><h2 id="dispense-title">${escapeHtml(status.cocktailName || 'Dein Cocktail')}</h2><p>${escapeHtml(status.sizeName || '')} · ${status.steps.length} Pumpen arbeiten parallel</p><div class="progress-track"><div class="progress-bar"></div></div><div class="progress-row"><span class="progress-label">0 %</span><span class="time-label">Noch einen Moment</span></div><button class="danger-button wide-button stop-dispense">${icons.stop} Sofort stoppen</button><p class="stop-hint">Stop schaltet alle Pumpen unmittelbar aus.</p></div></section>`;
        app.append(layer);
        layer.querySelector('.stop-dispense').addEventListener('click', stopDispense);
        updateDispenseUi(status);
        if (window.gsap && !reduceMotion) gsap.from('.dispense-panel', { opacity: 0, scale: .97, duration: .35, ease: 'power3.out' });
        startPolling();
    }

    function startPolling() {
        clearInterval(state.pollTimer);
        state.pollTimer = setInterval(async () => {
            try {
                const status = await api('/api/dispenses/current');
                if (status.status === 'running') updateDispenseUi(status);
                else if (status.status === 'completed') {
                    if (status.mode === 'cleaning') showCleaningSuccess();
                    else showSuccess(status);
                }
                else if (status.status === 'stopped') finishStopped(status.mode);
                else if (status.status === 'failed') finishFailed(status.error);
            } catch (error) {
                showToast(error.message, true);
            }
        }, 250);
    }

    function updateDispenseUi(status) {
        const progress = Math.max(.03, status.progress || 0);
        const liquid = document.querySelector('.liquid');
        const bar = document.querySelector('.progress-bar');
        if (window.gsap && !reduceMotion) {
            if (liquid) gsap.to(liquid, { scaleY: progress, duration: .28, ease: 'power1.out', overwrite: true });
            if (bar) gsap.to(bar, { width: `${progress * 100}%`, duration: .28, ease: 'power1.out', overwrite: true });
            if (liquid && !liquid.dataset.bubblesStarted) {
                liquid.dataset.bubblesStarted = 'true';
                gsap.to('.bubble', { y: -180, opacity: 0, duration: 2.2, stagger: .4, repeat: -1, ease: 'none' });
            }
        } else {
            if (liquid) liquid.style.transform = `scaleY(${progress})`;
            if (bar) bar.style.width = `${progress * 100}%`;
        }
        const percent = document.querySelector('.progress-label');
        const remaining = Math.max(0, status.estimatedDurationSeconds * (1 - status.progress));
        if (percent) percent.textContent = `${Math.round(status.progress * 100)} %`;
        const time = document.querySelector('.time-label');
        if (time) time.textContent = remaining > 1 ? `ca. ${Math.ceil(remaining)} Sek.` : 'Gleich fertig';
    }

    async function stopDispense(event) {
        const button = event.currentTarget;
        button.disabled = true;
        button.textContent = 'Pumpen werden gestoppt …';
        try {
            await api('/api/dispenses/current/stop', { method: 'POST' });
            await finishStopped(button.closest('[data-operation-mode]')?.dataset.operationMode);
        } catch (error) {
            button.disabled = false;
            button.innerHTML = `${icons.stop} Sofort stoppen`;
            showToast(error.message, true);
        }
    }

    function showSuccess(status) {
        clearInterval(state.pollTimer);
        const layer = document.querySelector('.modal-layer');
        if (!layer || layer.querySelector('.success-state')) return;
        layer.innerHTML = `<div class="modal-scrim"></div><section class="success-state" role="status"><div><svg viewBox="0 0 120 120" fill="none" stroke="currentColor" stroke-width="8" aria-hidden="true"><circle class="check-circle" cx="60" cy="60" r="48"/><path class="check-path" d="m36 61 16 16 34-38"/></svg><h2>Fertig gemixt</h2><p>${escapeHtml(status.cocktailName || 'Dein Cocktail')} ist bereit. Prost!</p></div></section>`;
        if (window.gsap && !reduceMotion) {
            gsap.set('.check-circle', { strokeDasharray: 320, strokeDashoffset: 320 });
            gsap.timeline()
                .from('.success-state', { opacity: 0, scale: .96, duration: .24 })
                .to('.check-circle', { strokeDashoffset: 0, duration: .42, ease: 'power2.out' })
                .from('.check-path', { opacity: 0, scale: .35, transformOrigin: '50% 50%', duration: .28, ease: 'back.out(2)' }, '-=.2');
        }
        state.completionTimer = setTimeout(async () => {
            layer.remove();
            await reloadDataAndRender();
        }, 5000);
    }

    async function finishStopped(mode) {
        clearInterval(state.pollTimer);
        document.querySelector('.modal-layer')?.remove();
        showToast(mode === 'cleaning' ? 'Reinigung gestoppt. Alle Pumpen sind aus.' : 'Ausschank gestoppt. Alle Pumpen sind aus.');
        await reloadDataAndRender();
    }

    async function finishFailed(message) {
        clearInterval(state.pollTimer);
        document.querySelector('.modal-layer')?.remove();
        showToast(message || 'Ausschank fehlgeschlagen.', true);
        await reloadDataAndRender();
    }

    async function reloadDataAndRender() {
        try {
            await loadAll();
        } catch (error) {
            showToast(error.message, true);
        }
        renderRoute();
    }

    function closeLayer(layer) {
        if (window.gsap && !reduceMotion) gsap.to(layer, { opacity: 0, duration: .16, onComplete: () => layer.remove() });
        else layer.remove();
    }

    function renderSettings() {
        const routeTab = location.hash.split('/')[2];
        const tabs = [
            ['cocktails', 'Cocktails', icons.cocktail],
            ['ingredients', 'Zutaten', icons.ingredient],
            ['pumps', 'Pumpen', icons.prime],
            ['sizes', 'Größen', icons.size],
            ['system', 'System', icons.settings]
        ];
        if (tabs.some(([id]) => id === routeTab)) state.activeTab = routeTab;
        app.innerHTML = `<section class="settings-page"><nav class="settings-nav" aria-label="Einstellungsbereiche"><button class="icon-button back-home" aria-label="Zurück zur Startseite" title="Zurück zur Startseite">${icons.back}</button><div class="settings-tabs" role="tablist" aria-label="Einstellungsbereiche">${tabs.map(([id, label, icon]) => `<button class="tab-button" data-tab="${id}" role="tab" aria-selected="${state.activeTab === id}" aria-controls="app-main">${icon}<span>${label}</span></button>`).join('')}</div><span class="settings-version" aria-label="Anwendungsversion">v${escapeHtml(state.version || '–')}</span></nav><main class="settings-main" id="app-main" tabindex="-1"></main></section>`;
        const currentTheme = state.system?.theme || 'Dark';
        const version = app.querySelector('.settings-version');
        const tools = document.createElement('div');
        tools.className = 'settings-tools';
        const nextTheme = currentTheme === 'Light' ? 'Dark' : 'Light';
        const themeIcon = nextTheme === 'Light' ? icons.sun : icons.moon;
        const themeLabel = nextTheme === 'Light' ? 'Hellen Modus aktivieren' : 'Dunklen Modus aktivieren';
        const updateButton = state.update?.isAvailable
            ? `<button type="button" class="update-button" data-app-update aria-label="Update auf ${escapeHtml(state.update.latestVersion)} installieren" title="Update auf ${escapeHtml(state.update.latestVersion)} installieren">${icons.download}</button>`
            : '';
        tools.innerHTML = `${updateButton}<button type="button" class="theme-toggle" data-theme-toggle aria-label="${themeLabel}" title="${themeLabel}">${themeIcon}</button>`;
        version.before(tools);
        tools.append(version);
        app.querySelector('.back-home').addEventListener('click', () => { location.hash = '#/'; });
        app.querySelectorAll('[data-tab]').forEach(button => button.addEventListener('click', () => {
            state.activeTab = button.dataset.tab;
            state.editing = null;
            state.cocktailDraft = null;
            location.hash = `#/settings/${state.activeTab}`;
        }));
        app.querySelector('[data-theme-toggle]').addEventListener('click', () => setTheme(nextTheme));
        app.querySelector('[data-app-update]')?.addEventListener('click', openApplicationUpdateDialog);
        renderActiveSettingsTab();
        requestAnimationFrame(() => app.querySelector('#app-main')?.focus({ preventScroll: true }));
    }

    function openApplicationUpdateDialog() {
        const version = state.update?.latestVersion;
        if (!version) return;
        const layer = createSettingsDialog('update-confirmation-dialog');
        const content = `<p class="update-dialog-copy">Version <strong>${escapeHtml(version)}</strong> wird heruntergeladen und installiert. Die App startet danach automatisch neu.</p><div class="form-actions"><button type="button" class="secondary-button" data-dialog-close>Abbrechen</button><button type="button" class="primary-button confirm-app-update">Update installieren</button></div>`;
        setSettingsDialogContent(layer, 'Update installieren?', 'Die Bedienung ist während der Aktualisierung kurz nicht verfügbar.', content);
        layer.querySelector('.confirm-app-update').addEventListener('click', () => {
            closeLayer(layer);
            startApplicationUpdate(showApplicationUpdateProgress(version));
        });
    }

    function showApplicationUpdateProgress(version) {
        const layer = createSettingsDialog('update-progress-dialog', { dismissible: false });
        const content = `<div class="update-progress" aria-live="polite"><div class="update-spinner" aria-hidden="true"></div><div><h3>Update läuft</h3><p>Version <strong>${escapeHtml(version)}</strong> wird installiert. Die App startet danach automatisch neu.</p><span>Bitte dieses Fenster geöffnet lassen.</span></div></div>`;
        setSettingsDialogContent(layer, 'Update wird vorbereitet', '', content, { showClose: false });
        const dialog = layer.querySelector('.settings-dialog');
        dialog.tabIndex = -1;
        requestAnimationFrame(() => dialog.focus());
        return layer;
    }

    async function startApplicationUpdate(progressLayer) {
        const button = app.querySelector('[data-app-update]');
        if (button) {
            button.disabled = true;
            button.classList.add('is-updating');
        }
        try {
            await api('/api/app-update', { method: 'POST' });
            waitForUpdatedApplication();
        } catch (error) {
            closeLayer(progressLayer);
            if (button) {
                button.disabled = false;
                button.classList.remove('is-updating');
            }
            showToast(error.message, true);
        }
    }

    async function waitForUpdatedApplication() {
        try {
            const appInfo = await api('/api/app-info');
            if (appInfo.version !== state.version) {
                location.reload();
                return;
            }
        } catch {
            // Der Dienst wird während des Updates kurz nicht erreichbar sein.
        }
        window.setTimeout(waitForUpdatedApplication, 3000);
    }

    async function setTheme(theme) {
        const previousTheme = state.system.theme;
        applyTheme(theme);
        try {
            state.system = await api('/api/system/theme', { method: 'PUT', body: { theme } });
            applyTheme(state.system.theme);
            renderSettings();
        } catch (error) {
            applyTheme(previousTheme);
            showToast(error.message, true);
        }
    }

    function renderActiveSettingsTab() {
        const main = app.querySelector('.settings-main');
        const renderers = { cocktails: renderCocktailSettings, ingredients: renderIngredientSettings, pumps: renderPumpSettings, sizes: renderSizeSettings, system: renderSystemSettings };
        renderers[state.activeTab](main);
        if (window.gsap && !reduceMotion) gsap.from(main.children, { opacity: 0, y: 10, duration: .28, ease: 'power2.out' });
    }

    function renderCocktailSettings(main) {
        const list = state.cocktails.map(item => dataRow(item.name, `${item.standardSize.volumeMl} ml · ${item.ingredients.length} Zutaten`, item.id)).join('');
        main.innerHTML = settingsListTemplate('Cocktails', 'Rezepte und Standardgrößen verwalten.', 'Cocktail anlegen', list || emptyList('Noch keine Cocktails vorhanden.'), icons.cocktail);
        main.querySelector('.add-entity').addEventListener('click', () => openCocktailEditor());
        bindRowActions(main, id => openCocktailEditor(id), id => {
            const item = state.cocktails.find(x => x.id === id);
            openDeleteDialog('cocktails', id, 'Cocktail', item?.name || 'Cocktail');
        });
    }

    function openCocktailEditor(id = null) {
        state.editing = id;
        state.cocktailDraft = cocktailToDraft(id ? state.cocktails.find(x => x.id === id) : null);
        const layer = createSettingsDialog('settings-dialog-wide');
        renderCocktailEditorDialog(layer);
    }

    function renderCocktailEditorDialog(layer) {
        const scrollTop = layer.querySelector('.settings-dialog-body')?.scrollTop ?? 0;
        const editing = state.editing ? state.cocktails.find(x => x.id === state.editing) : null;
        const draft = state.cocktailDraft;
        const total = draft.ingredients.reduce((sum, row) => sum + (Number(row.amountMl) || 0), 0);
        const target = state.sizes.find(x => x.id === Number(draft.standardSizeId))?.volumeMl || 0;
        const recipeRows = draft.ingredients.map((row, index) => recipeRowTemplate(row, index, target)).join('');
        const form = `<form id="cocktail-form" class="form-grid"><div class="form-field full"><label for="cocktail-name">Name</label><input id="cocktail-name" name="name" required maxlength="100" value="${escapeHtml(draft.name)}"></div><div class="form-field full"><label for="cocktail-description">Beschreibung</label><textarea id="cocktail-description" name="description" maxlength="500">${escapeHtml(draft.description)}</textarea></div><div class="form-field"><label for="cocktail-size">Standardgröße</label><select id="cocktail-size" name="standardSizeId">${optionList(state.sizes, draft.standardSizeId, x => `${x.name} · ${x.volumeMl} ml`)}</select></div><div class="form-field"><label for="cocktail-image">Bild hochladen</label><input id="cocktail-image" type="file" accept="image/jpeg,image/png,image/webp,image/gif"></div>${draft.imagePath ? `<div class="form-field full"><img class="image-preview" src="${safeImage(draft.imagePath)}" alt="Bildvorschau"></div>` : ''}<div class="form-field full"><label>Zutaten und ml</label><div class="recipe-rows">${recipeRows}</div><button type="button" class="small-action add-recipe-row">${icons.plus} Zutat hinzufügen</button><div class="recipe-total ${Math.abs(total-target) > .5 ? 'invalid' : ''}">${formatNumber(total)} / ${target} ml</div></div>${dialogFormActions()}</form>`;

        setSettingsDialogContent(layer, editing ? 'Cocktail bearbeiten' : 'Cocktail anlegen', 'Das Rezept bezieht sich auf die gewählte Standardgröße.', form);
        layer.querySelector('.settings-dialog-body').scrollTop = scrollTop;
        layer.querySelector('#cocktail-form').addEventListener('submit', saveCocktail);
        layer.querySelector('.add-recipe-row').addEventListener('click', () => {
            captureCocktailDraft(layer);
            if (state.cocktailDraft.ingredients.length < 8) state.cocktailDraft.ingredients.push({ ingredientId: state.ingredients[0]?.id || '', amountMl: '' });
            renderCocktailEditorDialog(layer);
        });
        layer.querySelector('#cocktail-size').addEventListener('change', () => { captureCocktailDraft(layer); renderCocktailEditorDialog(layer); });
        layer.querySelectorAll('.remove-recipe-row').forEach(button => button.addEventListener('click', () => {
            captureCocktailDraft(layer);
            state.cocktailDraft.ingredients.splice(Number(button.dataset.index), 1);
            renderCocktailEditorDialog(layer);
        }));
        layer.querySelector('#cocktail-image').addEventListener('change', uploadCocktailImage);
        layer.querySelectorAll('.recipe-row').forEach(row => {
            const slider = row.querySelector('.amount-slider');
            const input = row.querySelector('.amount-input');
            updateAmountSliderVisual(slider);
            slider.addEventListener('input', () => {
                input.value = slider.value;
                updateAmountSliderVisual(slider);
                updateRecipeTotal(layer);
            });
            input.addEventListener('input', () => {
                const value = Number(input.value);
                if (Number.isFinite(value)) slider.value = Math.min(Number(slider.max), Math.max(Number(slider.min), value));
                updateAmountSliderVisual(slider);
                updateRecipeTotal(layer);
            });
        });
    }

    function cocktailToDraft(cocktail) {
        return cocktail ? { name: cocktail.name, description: cocktail.description || '', imagePath: cocktail.imagePath || '', standardSizeId: cocktail.standardSize.id, ingredients: cocktail.ingredients.map(x => ({ ingredientId: x.ingredientId, amountMl: x.amountMl })) } : { name: '', description: '', imagePath: '', standardSizeId: state.sizes[0]?.id || '', ingredients: [{ ingredientId: state.ingredients[0]?.id || '', amountMl: '' }] };
    }

    function recipeRowTemplate(row, index, target) {
        const maximum = Math.max(1, target);
        const amount = Number(row.amountMl);
        const sliderValue = Number.isFinite(amount) && amount > 0 ? Math.min(maximum, amount) : 1;
        return `<div class="recipe-row"><select aria-label="Zutat ${index + 1}">${optionList(state.ingredients, row.ingredientId, x => x.name)}</select><div class="amount-control"><input class="amount-slider" type="range" min="0.5" max="${maximum}" step="0.5" value="${sliderValue}" aria-label="Menge der Zutat ${index + 1} in ml"><label class="amount-value"><input class="amount-input" type="number" min="0.5" max="${maximum}" step="0.5" inputmode="decimal" aria-label="Exakte Menge der Zutat ${index + 1} in ml" value="${escapeHtml(row.amountMl)}"><span>ml</span></label></div><button type="button" class="remove-recipe-row" data-index="${index}" aria-label="Zutat entfernen">${icons.close}</button></div>`;
    }

    function captureCocktailDraft(main) {
        const form = main.querySelector('#cocktail-form');
        state.cocktailDraft.name = form.elements.name.value;
        state.cocktailDraft.description = form.elements.description.value;
        state.cocktailDraft.standardSizeId = Number(form.elements.standardSizeId.value);
        state.cocktailDraft.ingredients = [...main.querySelectorAll('.recipe-row')].map(row => ({ ingredientId: Number(row.querySelector('select').value), amountMl: row.querySelector('.amount-input').value }));
    }

    function updateRecipeTotal(main) {
        const total = [...main.querySelectorAll('.amount-input')].reduce((sum, input) => sum + (Number(input.value) || 0), 0);
        const target = state.sizes.find(x => x.id === Number(main.querySelector('#cocktail-size').value))?.volumeMl || 0;
        const label = main.querySelector('.recipe-total');
        label.textContent = `${formatNumber(total)} / ${target} ml`;
        label.classList.toggle('invalid', Math.abs(total - target) > .5);
    }

    function updateAmountSliderVisual(slider) {
        const range = Number(slider.max) - Number(slider.min);
        const progress = range > 0 ? ((Number(slider.value) - Number(slider.min)) / range) * 100 : 0;
        slider.style.setProperty('--slider-progress', `${Math.max(0, Math.min(100, progress))}%`);
    }

    function valueSliderField(id, name, label, unit, minimum, maximum, step, value, expandMaximum = false) {
        return `<div class="form-field full"><label for="${id}">${escapeHtml(label)} in ${escapeHtml(unit)}</label><div class="value-slider-control"><input class="amount-slider value-slider" type="range" min="${minimum}" max="${maximum}" step="${step}" value="${value}" data-input-id="${id}" ${expandMaximum ? 'data-expand-maximum="true"' : ''} aria-label="${escapeHtml(label)} in ${escapeHtml(unit)} einstellen"><div class="value-number"><input class="value-input" id="${id}" name="${name}" type="number" min="${minimum}" ${expandMaximum ? '' : `max="${maximum}"`} step="${step}" required inputmode="decimal" value="${value}"><span>${escapeHtml(unit)}</span></div></div></div>`;
    }

    function bindValueSliders(root) {
        root.querySelectorAll('.value-slider').forEach(slider => {
            const input = root.querySelector(`#${slider.dataset.inputId}`);
            updateAmountSliderVisual(slider);
            slider.addEventListener('input', () => {
                input.value = slider.value;
                updateAmountSliderVisual(slider);
            });
            input.addEventListener('input', () => {
                const value = Number(input.value);
                if (!Number.isFinite(value)) return;
                if (slider.dataset.expandMaximum === 'true' && value > Number(slider.max)) slider.max = Math.ceil(value / 10) * 10;
                slider.value = Math.min(Number(slider.max), Math.max(Number(slider.min), value));
                updateAmountSliderVisual(slider);
            });
        });
    }

    async function uploadCocktailImage(event) {
        const file = event.target.files[0];
        if (!file) return;
        const layer = event.target.closest('.modal-layer');
        captureCocktailDraft(layer);
        const formData = new FormData();
        formData.append('file', file);
        try {
            const result = await api('/api/images', { method: 'POST', body: formData });
            state.cocktailDraft.imagePath = result.path;
            renderCocktailEditorDialog(layer);
            showToast('Bild wurde hochgeladen.');
        } catch (error) { showToast(error.message, true); }
    }

    async function saveCocktail(event) {
        event.preventDefault();
        const submit = event.currentTarget.querySelector('[type="submit"]');
        const originalContent = submit.innerHTML;
        submit.disabled = true;
        submit.classList.add('is-loading');
        submit.textContent = 'Wird gespeichert …';
        captureCocktailDraft(event.currentTarget.closest('.modal-layer'));
        const draft = state.cocktailDraft;
        const payload = { name: draft.name, description: draft.description || null, imagePath: draft.imagePath || null, standardSizeId: Number(draft.standardSizeId), ingredients: draft.ingredients.map(x => ({ ingredientId: Number(x.ingredientId), amountMl: Number(x.amountMl) })) };
        const saved = await saveEntity('cocktails', state.editing, payload, 'Cocktail');
        if (!saved && submit.isConnected) {
            submit.disabled = false;
            submit.classList.remove('is-loading');
            submit.innerHTML = originalContent;
        }
    }

    function renderIngredientSettings(main) {
        const list = state.ingredients.map(ingredientDataRow).join('');
        main.innerHTML = settingsListTemplate('Zutaten', 'Flaschenfüllstände, Alkoholwerte und Pumpenzuordnung verwalten.', 'Zutat anlegen', list || emptyList('Noch keine Zutaten vorhanden.'), icons.ingredient);
        main.querySelector('.add-entity').addEventListener('click', () => openIngredientEditor());
        bindRowActions(main, id => openIngredientEditor(id), id => {
            const item = state.ingredients.find(x => x.id === id);
            openDeleteDialog('ingredients', id, 'Zutat', item?.name || 'Zutat');
        });
        main.querySelectorAll('[data-refill]').forEach(button => button.addEventListener('click', () => openIngredientRefillDialog(Number(button.dataset.refill))));
    }

    function openIngredientEditor(id = null) {
        const item = id ? state.ingredients.find(x => x.id === id) : null;
        state.editing = id;
        const bottleSize = item?.bottleSizeMl ?? 1000;
        const remaining = item?.remainingVolumeMl ?? bottleSize;
        const form = `<form id="entity-form" class="form-grid"><div class="form-field full"><label for="ingredient-name">Name</label><input id="ingredient-name" name="name" required maxlength="100" value="${escapeHtml(item?.name || '')}"></div><div class="form-field"><label for="bottle-size">Flaschengröße</label><div class="unit-input"><input id="bottle-size" name="bottleSizeMl" type="number" min="1" max="10000" step="1" required inputmode="numeric" value="${bottleSize}"><span>ml</span></div></div><div class="form-field"><label for="remaining-volume">Aktueller Restbestand</label><div class="unit-input"><input id="remaining-volume" name="remainingVolumeMl" type="number" min="0" max="10000" step="1" required inputmode="numeric" value="${remaining}"><span>ml</span></div></div><div class="form-field full"><p class="inventory-form-note">Unter 150 ml wird der Vorrat als niedrig markiert. „Flasche auffüllen“ setzt den Restbestand später wieder auf die Flaschengröße.</p></div><div class="form-field full"><label for="alcohol">Alkohol in % Vol.</label><input id="alcohol" name="alcoholPercentage" type="number" min="0" max="100" step="0.1" inputmode="decimal" value="${item?.alcoholPercentage ?? ''}"></div>${dialogFormActions()}</form>`;
        openSimpleEditorDialog(item ? 'Zutat bearbeiten' : 'Zutat anlegen', 'Flaschengröße und Restbestand werden für die Cocktail-Verfügbarkeit verwendet.', form, 'ingredients', 'Zutat', formElement => ({
            name: formElement.elements.name.value,
            alcoholPercentage: formElement.elements.alcoholPercentage.value === '' ? null : Number(formElement.elements.alcoholPercentage.value),
            bottleSizeMl: Number(formElement.elements.bottleSizeMl.value),
            remainingVolumeMl: Number(formElement.elements.remainingVolumeMl.value)
        }));
    }

    function openIngredientRefillDialog(id) {
        const ingredient = state.ingredients.find(x => x.id === id);
        if (!ingredient) return;
        const layer = createSettingsDialog('delete-dialog');
        const content = `<div class="refill-summary">${icons.refill}<div><strong>${escapeHtml(ingredient.name)}</strong><span>${formatNumber(ingredient.remainingVolumeMl)} ml → ${formatNumber(ingredient.bottleSizeMl)} ml</span></div></div><p class="delete-dialog-copy">Bestätige, dass eine volle Flasche angeschlossen wurde. Der Restbestand wird auf die hinterlegte Flaschengröße gesetzt.</p><div class="form-actions"><button type="button" class="secondary-button" data-dialog-close>Abbrechen</button><button type="button" class="primary-button confirm-refill">Flasche auffüllen</button></div>`;
        setSettingsDialogContent(layer, 'Flasche auffüllen?', '', content);
        layer.querySelector('.confirm-refill').addEventListener('click', async event => {
            event.currentTarget.disabled = true;
            event.currentTarget.classList.add('is-loading');
            event.currentTarget.textContent = 'Wird aufgefüllt …';
            try {
                await api(`/api/ingredients/${id}/refill`, { method: 'POST' });
                showToast(`${ingredient.name} wurde auf ${formatNumber(ingredient.bottleSizeMl)} ml aufgefüllt.`);
                dismissSettingsDialog(layer);
                await loadAll();
                renderSettings();
            } catch (error) {
                event.currentTarget.disabled = false;
                event.currentTarget.classList.remove('is-loading');
                event.currentTarget.textContent = 'Flasche auffüllen';
                showToast(error.message, true);
            }
        });
    }

    function renderPumpSettings(main) {
        const list = state.pumps.map(pumpDataRow).join('');
        main.innerHTML = settingsListTemplate('Pumpen', 'GPIO, Förderrate und zugeordnete Zutaten verwalten.', 'Pumpe anlegen', list || emptyList('Noch keine Pumpen vorhanden.'), icons.prime);
        const cleaningButton = document.createElement('button');
        cleaningButton.type = 'button';
        cleaningButton.className = 'secondary-button cleaning-mode-button';
        cleaningButton.disabled = !state.pumps.some(x => x.isEnabled);
        cleaningButton.innerHTML = `${icons.clean} Reinigungsmodus`;
        main.querySelector('.settings-header-actions').prepend(cleaningButton);
        cleaningButton.addEventListener('click', openCleaningDialog);
        main.querySelector('.add-entity').addEventListener('click', () => openPumpEditor());
        bindRowActions(main, id => openPumpEditor(id), id => {
            const item = state.pumps.find(x => x.id === id);
            openDeleteDialog('pumps', id, 'Pumpe', item?.name || 'Pumpe');
        });
        bindPumpPrimingButtons(main);
        bindPumpCalibrationButtons(main);
    }

    function openPumpEditor(id = null) {
        const item = id ? state.pumps.find(x => x.id === id) : null;
        state.editing = id;
        const flowRate = Math.round(Number(item?.flowRateMlPerSecond ?? 10) * 10) / 10;
        const flowMaximum = Math.max(100, Math.ceil(flowRate / 10) * 10);
        const form = `<form id="entity-form" class="form-grid"><div class="form-field full"><label for="pump-name">Name</label><input id="pump-name" name="name" required maxlength="100" value="${escapeHtml(item?.name || '')}"></div><div class="form-field full"><label for="gpio-pin">GPIO-Pin</label><div class="gpio-input-control"><input id="gpio-pin" name="gpioPin" type="number" min="0" max="40" required inputmode="numeric" value="${item?.gpioPin ?? ''}"><button type="button" class="gpio-map-trigger" aria-label="Raspberry-Pi-Pinbelegung öffnen" title="Raspberry-Pi-Pinbelegung öffnen">${icons.raspberryPi}</button></div></div>${valueSliderField('flow-rate', 'flowRate', 'Förderrate (Expertenoption)', 'ml/s', .1, flowMaximum, .1, flowRate, true)}<div class="form-field full"><p class="expert-option-note">Eine manuelle Änderung setzt den Kalibrierstatus auf „Manuell“.</p></div><div class="form-field full"><label for="pump-ingredient">Zutat</label><select id="pump-ingredient" name="ingredientId"><option value="">Keine Zuordnung</option>${optionList(state.ingredients, item?.ingredientId, x => x.name)}</select></div><div class="form-field full checkbox-row"><label class="checkbox-field"><input name="activeHigh" type="checkbox" ${item ? (item.activeHigh ? 'checked' : '') : 'checked'}> Relais ist active high</label><label class="checkbox-field"><input name="isEnabled" type="checkbox" ${item ? (item.isEnabled ? 'checked' : '') : 'checked'}> Pumpe ist aktiviert</label></div>${dialogFormActions()}</form>`;
        const layer = openSimpleEditorDialog(item ? 'Pumpe bearbeiten' : 'Pumpe anlegen', 'Maximal 8 Pumpen. Für genaue Ergebnisse den Kalibrierungsassistenten verwenden.', form, 'pumps', 'Pumpe', formElement => ({ name: formElement.elements.name.value, gpioPin: Number(formElement.elements.gpioPin.value), flowRateMlPerSecond: Number(formElement.elements.flowRate.value), activeHigh: formElement.elements.activeHigh.checked, isEnabled: formElement.elements.isEnabled.checked, ingredientId: formElement.elements.ingredientId.value ? Number(formElement.elements.ingredientId.value) : null }));
        layer.querySelector('.gpio-map-trigger').addEventListener('click', () => openGpioPinMap(layer.querySelector('#gpio-pin'), item?.id ?? null));
    }

    function openGpioPinMap(pinInput, currentPumpId) {
        const usesBoardNumbering = state.system?.pinNumberingScheme === 'Board';
        const occupiedPins = new Set(state.pumps.filter(pump => pump.id !== currentPumpId).map(pump => pump.gpioPin));
        const selectedPin = pinInput.value === '' ? null : Number(pinInput.value);
        const pinLabel = pin => usesBoardNumbering ? `Pin ${pin.board}` : `GPIO ${pin.bcm}`;
        const pinDescription = pin => `Pin ${pin.board} · GPIO ${pin.bcm}`;
        const pinMarkup = pin => {
            if (!Number.isInteger(pin.bcm)) return `<div class="gpio-header-pin is-disabled" aria-label="Pin ${pin.board}: ${pin.label}, nicht als GPIO nutzbar"><strong>Pin ${pin.board}</strong><span>${pin.label}</span></div>`;
            const configuredPin = usesBoardNumbering ? pin.board : pin.bcm;
            const occupied = occupiedPins.has(configuredPin);
            const selected = selectedPin === configuredPin;
            return `<button type="button" class="gpio-header-pin is-gpio ${selected ? 'is-selected' : ''}" data-gpio-select data-gpio-pin="${configuredPin}" ${occupied ? 'disabled' : ''} aria-label="${pinDescription(pin)}${occupied ? ', bereits belegt' : ''}" title="${pinDescription(pin)}"><strong>${pinLabel(pin)}</strong><span>${usesBoardNumbering ? `GPIO ${pin.bcm}` : `Pin ${pin.board}`}</span></button>`;
        };
        const headerRows = [
            raspberryPiHeader.map(pair => pinMarkup(pair[0])).join(''),
            raspberryPiHeader.map(pair => pinMarkup(pair[1])).join('')
        ].join('');
        const layer = createSettingsDialog('gpio-map-dialog');
        layer.classList.add('gpio-map-layer');
        layer.dataset.preserveEditing = 'true';
        const numberingLabel = usesBoardNumbering ? 'Physische Pin-Nummerierung (Board)' : 'BCM / logische GPIO-Nummerierung';
        const selectedDescription = selectedPin === null ? 'Noch kein Pin ausgewählt' : `Ausgewählt: ${usesBoardNumbering ? `Pin ${selectedPin}` : `GPIO ${selectedPin}`}`;
        const body = `<div class="gpio-map-intro"><div><span class="gpio-map-kicker">Raspberry Pi · 40-Pin-Header</span><strong>${escapeHtml(numberingLabel)}</strong><p>Nur GPIO-Pins sind auswählbar. GND, 3V3 und 5V sind deaktiviert; bereits zugeordnete GPIO-Pins ebenfalls.</p></div><div class="gpio-map-selected" data-gpio-selected>${escapeHtml(selectedDescription)}</div></div><div class="gpio-header-shell" role="group" aria-label="Pinbelegung des Raspberry Pi 40-Pin-Headers"><div class="gpio-header-labels"><span>Obere Reihe · ungerade Pins (1 → 39)</span><span>Untere Reihe · gerade Pins (2 → 40)</span></div><div class="gpio-header-grid">${headerRows}</div></div>`;
        setSettingsDialogContent(layer, 'GPIO-Pin auswählen', 'Die Auswahl wird direkt in die Pumpenkonfiguration übernommen.', body);
        layer.querySelectorAll('[data-gpio-select]').forEach(button => button.addEventListener('click', () => {
            const value = Number(button.dataset.gpioPin);
            pinInput.value = String(value);
            pinInput.dispatchEvent(new Event('input', { bubbles: true }));
            pinInput.dispatchEvent(new Event('change', { bubbles: true }));
            layer.querySelectorAll('[data-gpio-select]').forEach(candidate => candidate.classList.toggle('is-selected', candidate === button));
            layer.querySelector('[data-gpio-selected]').textContent = `Ausgewählt: ${usesBoardNumbering ? `Pin ${value}` : `GPIO ${value}`}`;
        }));
    }

    function openPrimingDialog() {
        const activePumps = state.pumps.filter(x => x.isEnabled);
        if (!activePumps.length) {
            showToast('Aktiviere zuerst mindestens eine Pumpe.', true);
            return;
        }

        const pumpOptions = activePumps.map(pump => `<label class="priming-pump-option"><input type="radio" name="primingPump" value="${pump.id}"><span><strong>${escapeHtml(pump.name)}</strong><small>GPIO ${pump.gpioPin} · ${escapeHtml(pump.ingredientName || 'Keine Zutat')}</small></span></label>`).join('');
        const layer = document.createElement('div');
        layer.className = 'modal-layer settings-dialog-layer';
        layer.returnFocus = document.activeElement;
        layer.innerHTML = `<div class="modal-scrim"></div><section class="settings-dialog settings-dialog-wide priming-dialog" role="dialog" aria-modal="true" aria-labelledby="priming-dialog-title"><div class="settings-dialog-content"><header class="settings-dialog-header"><div><h2 id="priming-dialog-title">Pumpe vorbereiten</h2><p>Schläuche füllen: Taste gedrückt halten, zum Stoppen loslassen.</p></div></header><div class="settings-dialog-body"><div class="priming-note"><strong>Hinweis</strong><span>Die Pumpe stoppt beim Loslassen. Zusätzlich schaltet sie sich nach spätestens 60 Sekunden automatisch ab.</span></div><fieldset class="priming-pump-fieldset"><legend>Pumpe auswählen</legend><div class="priming-pump-grid">${pumpOptions}</div></fieldset><button type="button" class="priming-hold-button" disabled aria-describedby="priming-hold-hint">${icons.prime}<span class="priming-hold-label">Pumpe auswählen</span></button><p class="priming-hold-hint" id="priming-hold-hint">Gedrückt halten, bis Flüssigkeit am Auslass ankommt.</p><div class="form-actions"><button type="button" class="secondary-button priming-cancel">Abbrechen</button></div></div></div></section>`;
        app.append(layer);

        const priming = {
            selectedPumpId: null,
            holding: false,
            starting: false,
            running: false,
            stopping: false,
            stopAfterStart: false,
            closed: false
        };
        const holdButton = layer.querySelector('.priming-hold-button');
        const label = layer.querySelector('.priming-hold-label');
        const cancelButton = layer.querySelector('.priming-cancel');
        const pumpInputs = [...layer.querySelectorAll('[name="primingPump"]')];
        const clearPrimingPoll = () => {
            clearInterval(state.primingPollTimer);
            state.primingPollTimer = null;
        };
        const setReady = (message = 'Gedrückt halten zum Fördern') => {
            clearPrimingPoll();
            priming.starting = false;
            priming.running = false;
            priming.stopping = false;
            priming.stopAfterStart = false;
            holdButton.classList.remove('is-active', 'is-starting');
            holdButton.disabled = !priming.selectedPumpId || priming.closed;
            label.textContent = message;
            cancelButton.disabled = false;
            pumpInputs.forEach(input => { input.disabled = false; });
        };
        const stopPriming = async (showMessage = false) => {
            priming.holding = false;
            if (priming.starting) {
                priming.stopAfterStart = true;
                return;
            }
            if (!priming.running || priming.stopping) return;
            priming.stopping = true;
            clearPrimingPoll();
            holdButton.classList.remove('is-active');
            holdButton.classList.add('is-starting');
            label.textContent = 'Pumpe wird gestoppt …';
            try {
                await api('/api/priming/current/stop', { method: 'POST' });
                setReady();
                if (showMessage) showToast('Pumpe gestoppt.');
            } catch (error) {
                setReady('Stopp fehlgeschlagen – erneut versuchen');
                showToast(error.message, true);
            }
        };
        const startPrimingPoll = () => {
            clearPrimingPoll();
            state.primingPollTimer = setInterval(async () => {
                try {
                    const status = await api('/api/priming/current');
                    if (status.status !== 'running' || status.mode !== 'priming') {
                        setReady();
                        showToast('Die Vorbereitung wurde automatisch beendet.');
                    }
                } catch (error) {
                    clearPrimingPoll();
                    showToast(error.message, true);
                }
            }, 300);
        };
        const startPriming = async () => {
            if (!priming.selectedPumpId || priming.starting || priming.running || priming.closed) return;
            priming.holding = true;
            priming.starting = true;
            cancelButton.disabled = true;
            pumpInputs.forEach(input => { input.disabled = true; });
            holdButton.classList.add('is-starting');
            label.textContent = 'Pumpe startet …';
            try {
                const status = await api('/api/priming', { method: 'POST', body: { pumpId: priming.selectedPumpId } });
                priming.starting = false;
                priming.running = status.status === 'running';
                if (!priming.running || priming.stopAfterStart || !priming.holding) {
                    await stopPriming();
                    return;
                }
                holdButton.classList.remove('is-starting');
                holdButton.classList.add('is-active');
                label.textContent = 'Pumpe läuft – gedrückt halten';
                startPrimingPoll();
            } catch (error) {
                setReady();
                showToast(error.message, true);
            }
        };
        const releasePriming = () => {
            priming.holding = false;
            void stopPriming();
        };
        const closeDialog = () => {
            priming.closed = true;
            clearPrimingPoll();
            document.removeEventListener('visibilitychange', onVisibilityChange);
            closeLayer(layer);
            setTimeout(() => { if (layer.returnFocus?.isConnected) layer.returnFocus.focus(); }, 180);
        };
        const onVisibilityChange = () => {
            if (document.visibilityState === 'hidden') releasePriming();
        };

        pumpInputs.forEach(input => input.addEventListener('change', () => {
            priming.selectedPumpId = Number(input.value);
            setReady();
        }));
        holdButton.addEventListener('pointerdown', event => {
            event.preventDefault();
            if (holdButton.disabled) return;
            holdButton.setPointerCapture?.(event.pointerId);
            void startPriming();
        });
        ['pointerup', 'pointercancel', 'lostpointercapture'].forEach(type => holdButton.addEventListener(type, releasePriming));
        holdButton.addEventListener('keydown', event => {
            if ((event.key === ' ' || event.key === 'Enter') && !event.repeat) {
                event.preventDefault();
                void startPriming();
            }
        });
        holdButton.addEventListener('keyup', event => {
            if (event.key === ' ' || event.key === 'Enter') {
                event.preventDefault();
                releasePriming();
            }
        });
        cancelButton.addEventListener('click', closeDialog);
        document.addEventListener('visibilitychange', onVisibilityChange);
        requestAnimationFrame(() => pumpInputs[0]?.focus());
    }

    function openCleaningDialog() {
        const activePumps = state.pumps.filter(x => x.isEnabled);
        if (!activePumps.length) {
            showToast('Aktiviere zuerst mindestens eine Pumpe.', true);
            return;
        }

        const pumpOptions = activePumps.map(pump => `<label class="cleaning-pump-option"><input type="checkbox" name="pumpId" value="${pump.id}" checked><span><strong>${escapeHtml(pump.name)}</strong><small>GPIO ${pump.gpioPin} · ${escapeHtml(pump.ingredientName || 'Keine Zutat')}</small></span></label>`).join('');
        const layer = createSettingsDialog('settings-dialog-wide cleaning-dialog');
        const body = `<form id="cleaning-form"><div class="cleaning-note"><strong>Vor dem Start</strong><span>Zuläufe in die geeignete Reinigungsflüssigkeit und Ausläufe sicher in einen Auffangbehälter legen.</span></div><fieldset class="cleaning-pump-fieldset"><legend>Aktive Pumpen auswählen</legend><button type="button" class="small-action cleaning-toggle-all">Alle abwählen</button><div class="cleaning-pump-grid">${pumpOptions}</div></fieldset>${valueSliderField('cleaning-duration', 'durationSeconds', 'Laufzeit', 'Sek.', 5, 300, 5, 30)}<div class="form-actions"><button type="button" class="secondary-button" data-dialog-close>Abbrechen</button><button type="submit" class="primary-button start-cleaning">${icons.clean} Reinigung starten</button></div></form>`;
        setSettingsDialogContent(layer, 'Pumpen reinigen', 'Ausgewählte Pumpen laufen gleichzeitig und stoppen automatisch.', body);
        bindValueSliders(layer);

        const form = layer.querySelector('#cleaning-form');
        const toggleAll = layer.querySelector('.cleaning-toggle-all');
        const submit = layer.querySelector('.start-cleaning');
        const checkboxes = [...form.querySelectorAll('[name="pumpId"]')];
        const updateSelection = () => {
            const selectedCount = checkboxes.filter(x => x.checked).length;
            submit.disabled = selectedCount === 0;
            toggleAll.textContent = selectedCount === checkboxes.length ? 'Alle abwählen' : 'Alle auswählen';
        };

        toggleAll.addEventListener('click', () => {
            const selectAll = checkboxes.some(x => !x.checked);
            checkboxes.forEach(x => { x.checked = selectAll; });
            updateSelection();
        });
        checkboxes.forEach(x => x.addEventListener('change', updateSelection));
        form.addEventListener('submit', async event => {
            event.preventDefault();
            const pumpIds = checkboxes.filter(x => x.checked).map(x => Number(x.value));
            if (!pumpIds.length) return;
            submit.disabled = true;
            submit.textContent = 'Reinigung wird gestartet …';
            try {
                const status = await api('/api/cleaning', {
                    method: 'POST',
                    body: { pumpIds, durationSeconds: Number(form.elements.durationSeconds.value) }
                });
                showCleaningProgress(status);
            } catch (error) {
                submit.disabled = false;
                submit.innerHTML = `${icons.clean} Reinigung starten`;
                showToast(error.message, true);
            }
        });
    }

    function showCleaningProgress(status) {
        document.querySelector('.modal-layer')?.remove();
        const layer = document.createElement('div');
        layer.className = 'modal-layer';
        const pumpSummary = status.steps.length === 1 ? '1 Pumpe läuft' : `${status.steps.length} Pumpen laufen gleichzeitig`;
        layer.innerHTML = `<div class="modal-scrim"></div><section class="dispense-panel cleaning-progress-panel" data-operation-mode="cleaning" role="dialog" aria-modal="true" aria-labelledby="cleaning-progress-title"><div class="glass-stage cleaning-stage"><div class="glass" aria-hidden="true"><div class="liquid cleaning-liquid"><i class="bubble one"></i><i class="bubble two"></i><i class="bubble three"></i></div></div></div><div class="dispense-copy"><span class="eyebrow">Reinigungsmodus aktiv</span><h2 id="cleaning-progress-title">Pumpen werden gespült</h2><p>${pumpSummary} · ${escapeHtml(status.sizeName || '')}</p><div class="progress-track"><div class="progress-bar cleaning-progress-bar"></div></div><div class="progress-row"><span class="progress-label">0 %</span><span class="time-label">Noch einen Moment</span></div><button class="danger-button wide-button stop-dispense">${icons.stop} Sofort stoppen</button><p class="stop-hint">Stop schaltet alle Pumpen unmittelbar aus.</p></div></section>`;
        app.append(layer);
        layer.querySelector('.stop-dispense').addEventListener('click', stopDispense);
        updateDispenseUi(status);
        if (window.gsap && !reduceMotion) gsap.from('.cleaning-progress-panel', { opacity: 0, scale: .97, duration: .35, ease: 'power3.out' });
        startPolling();
    }

    function showCleaningSuccess() {
        clearInterval(state.pollTimer);
        const layer = document.querySelector('.modal-layer');
        if (!layer || layer.querySelector('.success-state')) return;
        layer.innerHTML = `<div class="modal-scrim"></div><section class="success-state cleaning-success" role="status"><div><svg viewBox="0 0 120 120" fill="none" stroke="currentColor" stroke-width="8" aria-hidden="true"><circle class="check-circle" cx="60" cy="60" r="48"/><path class="check-path" d="m36 61 16 16 34-38"/></svg><h2>Reinigung abgeschlossen</h2><p>Alle ausgewählten Pumpen sind gespült und ausgeschaltet.</p><button type="button" class="primary-button cleaning-done">Zurück zu den Pumpen</button></div></section>`;
        layer.querySelector('.cleaning-done').addEventListener('click', () => {
            layer.remove();
            state.activeTab = 'pumps';
            if (location.hash === '#/settings/pumps') renderSettings();
            else location.hash = '#/settings/pumps';
        });
        layer.querySelector('.cleaning-done').focus();
    }

    function renderSizeSettings(main) {
        const list = state.sizes.map(x => dataRow(x.name, `${x.volumeMl} ml · Sortierung ${x.sortOrder}`, x.id)).join('');
        main.innerHTML = settingsListTemplate('Größen', 'Cocktailgrößen und deren Reihenfolge verwalten.', 'Größe anlegen', list || emptyList('Noch keine Größen vorhanden.'), icons.size);
        main.querySelector('.add-entity').addEventListener('click', () => openSizeEditor());
        bindRowActions(main, id => openSizeEditor(id), id => {
            const item = state.sizes.find(x => x.id === id);
            openDeleteDialog('sizes', id, 'Größe', item?.name || 'Größe');
        });
    }

    function openSizeEditor(id = null) {
        const item = id ? state.sizes.find(x => x.id === id) : null;
        state.editing = id;
        const volume = Number(item?.volumeMl ?? 250);
        const form = `<form id="entity-form" class="form-grid"><div class="form-field full"><label for="size-name">Name</label><input id="size-name" name="name" required maxlength="50" value="${escapeHtml(item?.name || '')}"></div>${valueSliderField('volume', 'volumeMl', 'Volumen', 'ml', 10, 2000, 1, volume)}<div class="form-field full"><label for="sort-order">Sortierung</label><input id="sort-order" name="sortOrder" type="number" required inputmode="numeric" value="${item?.sortOrder ?? state.sizes.length * 10 + 10}"></div>${dialogFormActions()}</form>`;
        openSimpleEditorDialog(item ? 'Größe bearbeiten' : 'Größe anlegen', 'Cocktails werden proportional auf dieses Volumen skaliert.', form, 'sizes', 'Größe', formElement => ({ name: formElement.elements.name.value, volumeMl: Number(formElement.elements.volumeMl.value), sortOrder: Number(formElement.elements.sortOrder.value) }));
    }

    function renderSystemSettings(main) {
        const config = state.system;
        const update = state.update;
        const updateContent = update?.isAvailable
            ? `<span class="system-update-available">v${escapeHtml(update.latestVersion)} ist verfügbar</span><button type="button" class="primary-button system-update-button" data-app-update>${icons.download}<span>Installieren</span></button>`
            : `<span class="system-update-current">${update ? 'Auf dem neuesten Stand' : 'Update-Status nicht verfügbar'}</span>`;
        main.innerHTML = `<section class="settings-card system-hero"><header class="system-page-heading"><span class="system-heading-icon" aria-hidden="true">${icons.settings}</span><div><h1>System</h1><p>Hardware, Netzwerkzugriff und Software dieser Mixstation konfigurieren.</p></div></header><form id="system-form"><div class="system-config-grid"><section class="system-section system-hardware-section" aria-labelledby="system-hardware-title"><header class="system-section-heading"><span class="system-section-icon" aria-hidden="true">${icons.raspberryPi}</span><div><h2 id="system-hardware-title">Hardware</h2><p>Lege fest, wie CocktailOS die angeschlossenen Pumpen ansteuert.</p></div></header><div class="system-choice-grid"><fieldset class="system-choice-group"><legend>Pumpentreiber</legend><label class="option-card system-option-card"><input type="radio" name="pumpDriver" value="Dummy" ${config.pumpDriver === 'Dummy' ? 'checked' : ''}><strong>Dummy</strong><span>Sicher testen, ohne GPIO-Ausgänge zu schalten.</span></label><label class="option-card system-option-card"><input type="radio" name="pumpDriver" value="Gpio" ${config.pumpDriver === 'Gpio' ? 'checked' : ''}><strong>Raspberry Pi GPIO</strong><span>Steuert reale Relais über System.Device.Gpio.</span></label></fieldset><fieldset class="system-choice-group"><legend>Pin-Nummerierung</legend><label class="option-card system-option-card"><input type="radio" name="pinNumberingScheme" value="Logical" ${config.pinNumberingScheme === 'Logical' ? 'checked' : ''}><strong>Logical / BCM</strong><span>GPIO-Nummern, beispielsweise GPIO17.</span></label><label class="option-card system-option-card"><input type="radio" name="pinNumberingScheme" value="Board" ${config.pinNumberingScheme === 'Board' ? 'checked' : ''}><strong>Board / physisch</strong><span>Positionen 1 bis 40 auf dem Raspberry-Pi-Header.</span></label></fieldset></div><p class="system-inline-note">Die Relaispolarität wird direkt für jede Pumpe festgelegt.</p></section><section class="system-section system-network-section" aria-labelledby="system-network-title"><header class="system-section-heading"><span class="system-section-icon" aria-hidden="true">${icons.lock}</span><div><h2 id="system-network-title">Netzwerkzugriff</h2><p>Steuere CocktailOS von einem anderen Gerät im lokalen Netzwerk.</p></div></header><label class="option-card network-access-option system-network-toggle"><input type="checkbox" name="networkAccessEnabled" ${config.networkAccessEnabled ? 'checked' : ''}><span class="network-switch" aria-hidden="true"><span></span></span><strong>Fernzugriff aktivieren</strong><span>Der Zugang ist durch einen vierstelligen PIN geschützt.</span></label><div class="network-access-note"><span>Nur in vertrauenswürdigen Netzwerken aktivieren.</span><button type="button" class="text-button network-pin-change" ${config.networkAccessEnabled ? '' : 'hidden'}>PIN ändern</button></div></section><section class="system-section system-update-section" aria-labelledby="application-update-title"><header class="system-section-heading"><span class="system-section-icon" aria-hidden="true">${icons.download}</span><div><h2 id="application-update-title">Software</h2><p>Installierte Version und verfügbare Aktualisierungen.</p></div></header><div class="application-update-card"><div><span class="system-meta-label">Installierte Version</span><strong>v${escapeHtml(state.version || '–')}</strong></div><div class="application-update-status">${updateContent}</div></div></section></div><aside class="system-safety-note"><span aria-hidden="true">!</span><p>Bei active LOW bleibt der Ausgang im Ruhezustand HIGH und schaltet zum Pumpen auf LOW.</p></aside><footer class="system-form-footer"><p>Änderungen werden erst nach dem Speichern übernommen.</p><button class="primary-button" type="submit">Hardwarekonfiguration speichern</button></footer></form></section>`;
        const networkToggle = main.querySelector('[name="networkAccessEnabled"]');
        const form = main.querySelector('#system-form');
        const pinChangeButton = main.querySelector('.network-pin-change');
        const setNetworkPin = pin => { form.dataset.networkAccessPin = pin; pinChangeButton.hidden = !networkToggle.checked; };
        networkToggle.addEventListener('change', () => {
            if (!networkToggle.checked) {
                form.dataset.networkAccessPin = '';
                pinChangeButton.hidden = true;
                return;
            }
            openNetworkPinSetupDialog({
                onSave: setNetworkPin,
                onCancel: () => { networkToggle.checked = false; pinChangeButton.hidden = true; }
            });
        });
        pinChangeButton.addEventListener('click', () => openNetworkPinSetupDialog({ onSave: setNetworkPin }));
        main.querySelector('[data-app-update]')?.addEventListener('click', openApplicationUpdateDialog);
        form.addEventListener('submit', async event => {
            event.preventDefault();
            const submit = form.querySelector('[type="submit"]');
            const originalContent = submit.innerHTML;
            submit.disabled = true;
            submit.classList.add('is-loading');
            submit.textContent = 'Wird gespeichert …';
            try {
                const pin = form.dataset.networkAccessPin || '';
                if (form.elements.networkAccessEnabled.checked && !config.networkAccessPinConfigured && !pin) throw new Error('Lege zuerst einen vierstelligen Netzwerk-PIN fest.');
                state.system = await api('/api/system', { method: 'PUT', body: { pumpDriver: form.elements.pumpDriver.value, pinNumberingScheme: form.elements.pinNumberingScheme.value, theme: state.system.theme, networkAccessEnabled: form.elements.networkAccessEnabled.checked, networkAccessPin: pin || null } });
                showToast(state.system.networkAccessEnabled ? 'Hardwarekonfiguration gespeichert. Netzwerkzugriff ist aktiv.' : 'Hardwarekonfiguration gespeichert. Netzwerkzugriff ist deaktiviert.');
                renderActiveSettingsTab();
            } catch (error) {
                submit.disabled = false;
                submit.classList.remove('is-loading');
                submit.innerHTML = originalContent;
                showToast(error.message, true);
            }
        });
    }

    function settingsListTemplate(title, intro, addLabel, content, icon) {
        return `<div class="settings-list-view"><section class="settings-card settings-workbench" aria-label="${escapeHtml(title)}"><header class="settings-list-header"><div class="settings-list-title"><span class="settings-list-icon" aria-hidden="true">${icon}</span><div><h1>${escapeHtml(title)}</h1><p>${escapeHtml(intro)}</p></div></div><div class="settings-header-actions"><button type="button" class="primary-button add-entity">${icons.plus}<span>${escapeHtml(addLabel)}</span></button></div></header><div class="list-card"><div class="data-list">${content}</div></div></section></div>`;
    }

    function createSettingsDialog(className = '', options = {}) {
        const dismissible = options.dismissible !== false;
        const layer = document.createElement('div');
        layer.className = 'modal-layer settings-dialog-layer';
        layer.innerHTML = `<div class="modal-scrim"></div><section class="settings-dialog ${className}" role="dialog" aria-modal="true" aria-labelledby="settings-dialog-title"><div class="settings-dialog-content"></div></section>`;
        layer.returnFocus = document.activeElement;
        app.append(layer);
        if (dismissible) layer.querySelector('.modal-scrim').addEventListener('click', () => dismissSettingsDialog(layer));
        layer.addEventListener('keydown', event => {
            if (dismissible && event.key === 'Escape') dismissSettingsDialog(layer);
            if (event.key !== 'Tab') return;
            const focusable = [...layer.querySelectorAll('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled])')];
            if (!focusable.length) return;
            const first = focusable[0];
            const last = focusable.at(-1);
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        });
        if (window.gsap && !reduceMotion) gsap.from(layer.querySelector('.settings-dialog'), { opacity: 0, y: 12, scale: .985, duration: .22, ease: 'power2.out' });
        return layer;
    }

    function setSettingsDialogContent(layer, title, intro, body, options = {}) {
        const closeButton = options.showClose === false ? '' : `<button type="button" class="icon-button" data-dialog-close aria-label="Dialog schließen">${icons.close}</button>`;
        const headingIcon = options.headingIcon ? `<span class="settings-dialog-heading-icon" aria-hidden="true">${options.headingIcon}</span>` : '';
        layer.querySelector('.settings-dialog-content').innerHTML = `<header class="settings-dialog-header"><div class="settings-dialog-heading">${headingIcon}<div><h2 id="settings-dialog-title">${escapeHtml(title)}</h2>${intro ? `<p>${escapeHtml(intro)}</p>` : ''}</div></div>${closeButton}</header><div class="settings-dialog-body">${body}</div>`;
        layer.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => dismissSettingsDialog(layer)));
        if (!layer.dataset.focused) {
            layer.dataset.focused = 'true';
            requestAnimationFrame(() => (layer.querySelector('.settings-dialog-body input, .settings-dialog-body select, .settings-dialog-body textarea') || layer.querySelector('button'))?.focus());
        }
    }

    function dismissSettingsDialog(layer) {
        if (!layer?.isConnected) return;
        const returnFocus = layer.returnFocus;
        if (!layer.dataset.preserveEditing) {
            state.editing = null;
            state.cocktailDraft = null;
        }
        closeLayer(layer);
        setTimeout(() => { if (returnFocus?.isConnected) returnFocus.focus(); }, 180);
    }

    function openSimpleEditorDialog(title, intro, formMarkup, resource, label, createPayload) {
        const layer = createSettingsDialog();
        setSettingsDialogContent(layer, title, intro, formMarkup);
        bindValueSliders(layer);
        const form = layer.querySelector('#entity-form');
        form.addEventListener('submit', async event => {
            event.preventDefault();
            const submit = form.querySelector('[type="submit"]');
            const originalContent = submit.innerHTML;
            submit.disabled = true;
            submit.classList.add('is-loading');
            submit.textContent = 'Wird gespeichert …';
            const saved = await saveEntity(resource, state.editing, createPayload(form), label);
            if (!saved && submit.isConnected) {
                submit.disabled = false;
                submit.classList.remove('is-loading');
                submit.innerHTML = originalContent;
            }
        });
        return layer;
    }

    function openDeleteDialog(resource, id, label, name) {
        const layer = createSettingsDialog('delete-dialog');
        const content = `<p class="delete-dialog-copy"><strong>${escapeHtml(name)}</strong> wird dauerhaft gelöscht. Dieser Vorgang kann nicht rückgängig gemacht werden.</p><div class="form-actions"><button type="button" class="secondary-button" data-dialog-close>Abbrechen</button><button type="button" class="danger-button confirm-delete">${escapeHtml(label)} löschen</button></div>`;
        setSettingsDialogContent(layer, `${label} löschen?`, '', content);
        layer.querySelector('.confirm-delete').addEventListener('click', async event => {
            event.currentTarget.disabled = true;
            event.currentTarget.classList.add('is-loading');
            event.currentTarget.textContent = 'Wird gelöscht …';
            const removed = await removeEntity(resource, id, label);
            if (!removed && event.currentTarget.isConnected) {
                event.currentTarget.disabled = false;
                event.currentTarget.classList.remove('is-loading');
                event.currentTarget.textContent = `${label} löschen`;
            }
        });
    }

    async function saveEntity(resource, id, payload, label) {
        try {
            await api(`/api/${resource}${id ? `/${id}` : ''}`, { method: id ? 'PUT' : 'POST', body: payload });
            showToast(`${label} wurde gespeichert.`);
            state.editing = null;
            state.cocktailDraft = null;
            await loadAll();
            renderSettings();
            return true;
        } catch (error) { showToast(error.message, true); return false; }
    }

    async function removeEntity(resource, id, label) {
        try {
            await api(`/api/${resource}/${id}`, { method: 'DELETE' });
            showToast(`${label} wurde gelöscht.`);
            state.editing = null;
            state.cocktailDraft = null;
            await loadAll();
            renderSettings();
            return true;
        } catch (error) { showToast(error.message, true); return false; }
    }

    function bindRowActions(main, edit, remove) {
        main.querySelectorAll('[data-edit]').forEach(button => button.addEventListener('click', () => edit(Number(button.dataset.edit))));
        main.querySelectorAll('[data-delete]').forEach(button => button.addEventListener('click', () => remove(Number(button.dataset.delete))));
    }

    function bindPumpPrimingButtons(main) {
        const buttons = [...main.querySelectorAll('[data-prime]')];
        let current = null;
        const restoreButton = button => {
            button.disabled = button.dataset.enabled !== 'true';
            button.classList.remove('is-active', 'is-starting');
            button.innerHTML = `${icons.prime}<span>Füllen</span>`;
        };
        const onVisibilityChange = () => {
            if (document.visibilityState === 'hidden') releasePriming();
        };
        const clearPriming = () => {
            clearInterval(state.primingPollTimer);
            state.primingPollTimer = null;
            document.removeEventListener('visibilitychange', onVisibilityChange);
            buttons.forEach(restoreButton);
            current = null;
        };
        const stopPriming = async (session = current) => {
            if (!session || session !== current) return;
            session.holding = false;
            if (session.starting) {
                session.stopAfterStart = true;
                return;
            }
            if (!session.running || session.stopping) return;
            session.stopping = true;
            clearInterval(state.primingPollTimer);
            session.button.classList.remove('is-active');
            session.button.classList.add('is-starting');
            session.button.innerHTML = `${icons.stop}<span>Stoppt …</span>`;
            try {
                await api('/api/priming/current/stop', { method: 'POST' });
                if (current === session) clearPriming();
            } catch (error) {
                if (current === session) clearPriming();
                showToast(error.message, true);
            }
        };
        const releasePriming = button => {
            if (button && current?.button !== button) return;
            void stopPriming();
        };
        const startPrimingPoll = session => {
            clearInterval(state.primingPollTimer);
            state.primingPollTimer = setInterval(async () => {
                try {
                    const status = await api('/api/priming/current');
                    if (status.status !== 'running' || status.mode !== 'priming') {
                        if (current === session) clearPriming();
                        showToast('Die Vorbereitung wurde automatisch beendet.');
                    }
                } catch (error) {
                    if (current === session) clearPriming();
                    showToast(error.message, true);
                }
            }, 300);
        };
        const startPriming = async button => {
            if (current || button.disabled) return;
            const session = { button, pumpId: Number(button.dataset.prime), holding: true, starting: true, running: false, stopping: false, stopAfterStart: false };
            current = session;
            buttons.forEach(item => { item.disabled = true; });
            button.disabled = false;
            button.classList.add('is-starting');
            button.innerHTML = `${icons.prime}<span>Startet …</span>`;
            document.addEventListener('visibilitychange', onVisibilityChange);
            try {
                const status = await api('/api/priming', { method: 'POST', body: { pumpId: session.pumpId } });
                session.starting = false;
                session.running = status.status === 'running';
                if (!session.running) {
                    if (current === session) clearPriming();
                    return;
                }
                if (session.stopAfterStart || !session.holding) {
                    await stopPriming(session);
                    return;
                }
                button.classList.remove('is-starting');
                button.classList.add('is-active');
                button.innerHTML = `${icons.prime}<span>Läuft – halten</span>`;
                startPrimingPoll(session);
            } catch (error) {
                if (current === session) clearPriming();
                showToast(error.message, true);
            }
        };

        buttons.forEach(button => {
            button.addEventListener('pointerdown', event => {
                event.preventDefault();
                if (button.disabled) return;
                button.setPointerCapture?.(event.pointerId);
                void startPriming(button);
            });
            ['pointerup', 'pointercancel', 'lostpointercapture'].forEach(type => button.addEventListener(type, () => releasePriming(button)));
            button.addEventListener('keydown', event => {
                if ((event.key === ' ' || event.key === 'Enter') && !event.repeat) {
                    event.preventDefault();
                    void startPriming(button);
                }
            });
            button.addEventListener('keyup', event => {
                if (event.key === ' ' || event.key === 'Enter') {
                    event.preventDefault();
                    releasePriming(button);
                }
            });
        });
    }

    function bindPumpCalibrationButtons(main) {
        main.querySelectorAll('[data-calibrate]').forEach(button => button.addEventListener('click', () => {
            if (!button.disabled) openCalibrationDialog(Number(button.dataset.calibrate));
        }));
    }

    function openCalibrationDialog(pumpId, resumedStatus = null) {
        const pump = state.pumps.find(item => item.id === pumpId);
        if (!pump) {
            showToast('Die ausgewählte Pumpe wurde nicht gefunden.', true);
            return;
        }

        const storageKey = `cocktailos-calibration-${pumpId}`;
        if (!resumedStatus) sessionStorage.removeItem(storageKey);
        let measurements = [];
        try {
            const stored = JSON.parse(sessionStorage.getItem(storageKey) || '[]');
            if (Array.isArray(stored)) measurements = stored.filter(value => Number.isInteger(value) && value >= 1 && value <= 10000).slice(0, 3);
        } catch {
            sessionStorage.removeItem(storageKey);
        }

        const layer = createSettingsDialog('settings-dialog-wide calibration-dialog', { dismissible: false });
        layer.dataset.preserveEditing = 'true';
        let phase = resumedStatus?.status === 'running' ? 'running' : 'intro';
        let closed = false;
        let pollBusy = false;
        const priming = { starting: false, running: false, stopping: false, holding: false, stopAfterStart: false };

        const persistMeasurements = () => sessionStorage.setItem(storageKey, JSON.stringify(measurements));
        const clearPoll = () => {
            clearInterval(state.pollTimer);
            state.pollTimer = null;
        };
        const stopPriming = async () => {
            priming.holding = false;
            if (priming.starting) {
                priming.stopAfterStart = true;
                return;
            }
            if (!priming.running || priming.stopping) return;
            priming.stopping = true;
            try {
                await api('/api/priming/current/stop', { method: 'POST' });
            } catch (error) {
                if (!closed) showToast(error.message, true);
            } finally {
                priming.running = false;
                priming.stopping = false;
                if (!closed && phase === 'intro') renderIntro();
            }
        };
        const closeWizard = async (clearMeasurements = true) => {
            if (closed || phase === 'running') return;
            closed = true;
            clearPoll();
            document.removeEventListener('visibilitychange', onVisibilityChange);
            await stopPriming();
            if (clearMeasurements) sessionStorage.removeItem(storageKey);
            dismissSettingsDialog(layer);
        };
        const onVisibilityChange = () => {
            if (document.visibilityState === 'hidden' && priming.running) void stopPriming();
        };

        const updatePrimingControls = () => {
            const button = layer.querySelector('.calibration-prime');
            const startButton = layer.querySelector('.calibration-start');
            if (!button || !startButton) return;
            const label = priming.starting ? 'Pumpe startet …' : priming.running ? 'Läuft – gedrückt halten' : priming.stopping ? 'Pumpe stoppt …' : 'Optional: Schlauch füllen';
            button.classList.toggle('is-active', priming.running);
            button.classList.toggle('is-starting', priming.starting || priming.stopping);
            button.disabled = priming.stopping;
            button.querySelector('span').textContent = label;
            startButton.disabled = priming.starting || priming.running || priming.stopping;
        };

        const startPriming = async () => {
            if (priming.starting || priming.running || priming.stopping || closed) return;
            priming.holding = true;
            priming.starting = true;
            updatePrimingControls();
            try {
                const status = await api('/api/priming', { method: 'POST', body: { pumpId } });
                priming.starting = false;
                priming.running = status.status === 'running';
                if (!priming.running || priming.stopAfterStart || !priming.holding) {
                    await stopPriming();
                    return;
                }
                if (!closed) updatePrimingControls();
            } catch (error) {
                priming.starting = false;
                priming.running = false;
                if (!closed) {
                    renderIntro();
                    showToast(error.message, true);
                }
            }
        };

        const bindPrimingButton = button => {
            button.addEventListener('pointerdown', event => {
                event.preventDefault();
                if (button.disabled) return;
                button.setPointerCapture?.(event.pointerId);
                void startPriming();
            });
            ['pointerup', 'pointercancel', 'lostpointercapture'].forEach(type => button.addEventListener(type, () => void stopPriming()));
            button.addEventListener('keydown', event => {
                if ((event.key === ' ' || event.key === 'Enter') && !event.repeat) {
                    event.preventDefault();
                    void startPriming();
                }
            });
            button.addEventListener('keyup', event => {
                if (event.key === ' ' || event.key === 'Enter') {
                    event.preventDefault();
                    void stopPriming();
                }
            });
        };

        function renderIntro() {
            if (closed) return;
            phase = 'intro';
            const primeLabel = priming.starting ? 'Pumpe startet …' : priming.running ? 'Läuft – gedrückt halten' : priming.stopping ? 'Pumpe stoppt …' : 'Optional: Schlauch füllen';
            const primeClass = priming.running ? 'is-active' : priming.starting || priming.stopping ? 'is-starting' : '';
            const body = `<div class="calibration-summary"><div class="calibration-icon">${icons.calibrate}</div><div><span>Pumpe</span><strong>${escapeHtml(pump.name)}</strong><small>${escapeHtml(pump.ingredientName)} · aktuell ${formatFlowRate(pump.flowRateMlPerSecond)} ml/s</small></div></div>
                <div class="calibration-note"><strong>Vorbereitung</strong><span>Messbecher unter den Auslass stellen. Der Schlauch muss vollständig mit der zugeordneten Zutat (${escapeHtml(pump.ingredientName)}) gefüllt sein.</span></div>
                <button type="button" class="priming-hold-button calibration-prime ${primeClass}" ${priming.stopping ? 'disabled' : ''}>${icons.prime}<span>${primeLabel}</span></button>
                <p class="priming-hold-hint">Nur bei Bedarf gedrückt halten. Beim Loslassen stoppt die Pumpe.</p>
                <div class="calibration-duration"><span>Messdauer</span><strong>10 Sekunden</strong><small>Die Pumpe stoppt automatisch.</small></div>
                <div class="form-actions"><button type="button" class="secondary-button calibration-cancel">Abbrechen</button><button type="button" class="primary-button calibration-start" ${priming.starting || priming.running || priming.stopping ? 'disabled' : ''}>${icons.calibrate} Messung starten</button></div>`;
            setSettingsDialogContent(layer, 'Pumpe kalibrieren', 'Förderrate mit einer kontrollierten Messung bestimmen.', body, { showClose: false });
            bindPrimingButton(layer.querySelector('.calibration-prime'));
            layer.querySelector('.calibration-cancel').addEventListener('click', () => void closeWizard());
            layer.querySelector('.calibration-start').addEventListener('click', startCalibration);
        }

        async function startCalibration() {
            if (phase === 'running' || priming.starting || priming.running || priming.stopping) return;
            try {
                const status = await api('/api/calibrations', { method: 'POST', body: { pumpId } });
                renderRunning(status);
            } catch (error) {
                showToast(error.message, true);
                renderIntro();
            }
        }

        function updateCalibrationProgress(status) {
            const progress = Math.max(0, Math.min(1, status.progress || 0));
            const bar = layer.querySelector('.calibration-progress-bar');
            const percent = layer.querySelector('.calibration-progress-percent');
            const seconds = layer.querySelector('.calibration-progress-time');
            const countdown = layer.querySelector('.calibration-countdown');
            if (bar) bar.style.width = `${progress * 100}%`;
            if (percent) percent.textContent = `${Math.round(progress * 100)} %`;
            if (seconds) seconds.textContent = `${Math.max(0, Math.ceil(10 * (1 - progress)))} Sek.`;
            if (countdown) countdown.textContent = String(Math.max(0, Math.ceil(10 * (1 - progress))));
        }

        function handleCalibrationStatus(status) {
            if (closed) return;
            if (status.mode !== 'calibration') {
                clearPoll();
                showToast('Der Kalibrierungsvorgang ist nicht mehr aktiv.', true);
                measurements.length ? renderResult() : renderIntro();
            } else if (status.status === 'running') {
                updateCalibrationProgress(status);
            } else if (status.status === 'completed') {
                clearPoll();
                renderMeasurement();
            } else if (status.status === 'stopped') {
                clearPoll();
                showToast('Kalibrierung gestoppt. Die Förderrate wurde nicht verändert.');
                measurements.length ? renderResult() : renderIntro();
            } else if (status.status === 'failed') {
                clearPoll();
                showToast(status.error || 'Kalibrierung fehlgeschlagen.', true);
                measurements.length ? renderResult() : renderIntro();
            }
        }

        function renderRunning(status) {
            phase = 'running';
            const body = `<div class="calibration-running" aria-live="polite"><div class="calibration-countdown">10</div><div><span class="eyebrow">Messung läuft</span><h3>${escapeHtml(pump.name)}</h3><p>${escapeHtml(pump.ingredientName)} wird exakt zehn Sekunden gefördert.</p></div></div>
                <div class="progress-track calibration-track"><div class="progress-bar calibration-progress-bar"></div></div>
                <div class="progress-row"><span class="calibration-progress-percent">0 %</span><span class="calibration-progress-time">10 Sek.</span></div>
                <button type="button" class="danger-button wide-button calibration-stop">${icons.stop} Sofort stoppen</button>
                <p class="stop-hint">Der Dialog bleibt während der Messung gesperrt. Die Pumpe stoppt auch ohne Browser nach zehn Sekunden.</p>`;
            setSettingsDialogContent(layer, 'Kalibrierungsmessung', 'Messbecher und Auslass während des Laufs nicht bewegen.', body, { showClose: false });
            layer.querySelector('.calibration-stop').addEventListener('click', async event => {
                event.currentTarget.disabled = true;
                try {
                    handleCalibrationStatus(await api('/api/calibrations/current/stop', { method: 'POST' }));
                } catch (error) {
                    event.currentTarget.disabled = false;
                    showToast(error.message, true);
                }
            });
            updateCalibrationProgress(status);
            clearPoll();
            state.pollTimer = setInterval(async () => {
                if (pollBusy) return;
                pollBusy = true;
                try {
                    handleCalibrationStatus(await api('/api/calibrations/current'));
                } catch (error) {
                    clearPoll();
                    showToast(error.message, true);
                } finally {
                    pollBusy = false;
                }
            }, 250);
        }

        function renderMeasurement() {
            phase = 'measurement';
            const runNumber = measurements.length + 1;
            const body = `<div class="calibration-measurement"><div class="calibration-run-complete">${icons.calibrate}<span><strong>Messlauf ${runNumber} abgeschlossen</strong><small>Die Pumpe ist ausgeschaltet.</small></span></div>
                <form class="calibration-volume-form"><label for="calibration-volume">Gemessene Menge</label><div class="calibration-volume-input"><input id="calibration-volume" name="volume" type="number" inputmode="numeric" min="1" max="10000" step="1" required autofocus aria-describedby="calibration-volume-hint"><span>ml</span></div><p id="calibration-volume-hint">Füllstand am Messbecher in ganzen Millilitern ablesen.</p>
                <div class="form-actions"><button type="button" class="secondary-button calibration-discard">Abbrechen</button><button type="submit" class="primary-button">Messwert übernehmen</button></div></form></div>`;
            setSettingsDialogContent(layer, 'Menge eintragen', `${pump.name} · Messlauf ${runNumber}`, body, { showClose: false });
            layer.querySelector('.calibration-discard').addEventListener('click', () => void closeWizard());
            layer.querySelector('.calibration-volume-form').addEventListener('submit', event => {
                event.preventDefault();
                const value = Number(event.currentTarget.elements.volume.value);
                if (!Number.isInteger(value) || value < 1 || value > 10000) {
                    showToast('Gib eine ganze Menge zwischen 1 und 10.000 ml ein.', true);
                    return;
                }
                measurements.push(value);
                persistMeasurements();
                renderResult();
            });
            requestAnimationFrame(() => layer.querySelector('#calibration-volume')?.focus());
        }

        function getResult() {
            const averageVolume = measurements.reduce((sum, value) => sum + value, 0) / measurements.length;
            const flowRate = averageVolume / 10;
            const deviation = measurements.length > 1
                ? ((Math.max(...measurements) - Math.min(...measurements)) / averageVolume) * 100
                : 0;
            return { flowRate, deviation };
        }

        function renderResult() {
            phase = 'result';
            const result = getResult();
            const warning = result.deviation > 10;
            const canMeasureAgain = measurements.length === 1 || (warning && measurements.length < 3);
            const values = measurements.map((value, index) => `<span><small>Lauf ${index + 1}</small><strong>${value} ml</strong></span>`).join('');
            const warningMarkup = warning ? `<div class="calibration-warning" role="alert"><strong>Messwerte weichen ${formatNumber(result.deviation)} % ab</strong><span>Prüfe Messbecher und Schlauch. Ein weiterer Lauf wird empfohlen; bewusstes Speichern bleibt möglich.</span></div>` : '';
            const body = `<div class="calibration-result"><div class="calibration-result-rate"><span>Berechnete Förderrate</span><strong>${formatNumber(result.flowRate)} <small>ml/s</small></strong><p>Bisher: ${formatFlowRate(pump.flowRateMlPerSecond)} ml/s</p></div><div class="calibration-values">${values}</div>${warningMarkup}</div>
                <div class="form-actions calibration-result-actions"><button type="button" class="secondary-button calibration-restart">Neu beginnen</button>${canMeasureAgain ? '<button type="button" class="secondary-button calibration-repeat">Kontrolllauf</button>' : ''}<button type="button" class="primary-button calibration-save">Förderrate speichern</button></div>
                <button type="button" class="text-button calibration-close">Abbrechen</button>`;
            setSettingsDialogContent(layer, 'Kalibrierung auswerten', `${measurements.length} ${measurements.length === 1 ? 'Messlauf' : 'Messläufe'} erfasst`, body, { showClose: false });
            layer.querySelector('.calibration-restart').addEventListener('click', () => {
                measurements = [];
                sessionStorage.removeItem(storageKey);
                renderIntro();
            });
            layer.querySelector('.calibration-repeat')?.addEventListener('click', startCalibration);
            layer.querySelector('.calibration-close').addEventListener('click', () => void closeWizard());
            layer.querySelector('.calibration-save').addEventListener('click', saveCalibration);
        }

        async function saveCalibration(event) {
            const button = event.currentTarget;
            button.disabled = true;
            button.textContent = 'Wird gespeichert …';
            try {
                const updatedPump = await api(`/api/pumps/${pumpId}/calibration`, { method: 'PUT', body: { measuredVolumesMl: measurements } });
                sessionStorage.removeItem(storageKey);
                closed = true;
                clearPoll();
                document.removeEventListener('visibilitychange', onVisibilityChange);
                await loadAll();
                state.activeTab = 'pumps';
                if (location.hash === '#/settings/pumps') renderSettings();
                else location.hash = '#/settings/pumps';
                showToast(`${updatedPump.name} ist mit ${formatFlowRate(updatedPump.flowRateMlPerSecond)} ml/s kalibriert · ${formatDateTime(updatedPump.lastCalibratedAt)}.`);
            } catch (error) {
                button.disabled = false;
                button.textContent = 'Förderrate speichern';
                showToast(error.message, true);
            }
        }

        layer.addEventListener('keydown', event => {
            if (event.key === 'Escape' && phase !== 'running') {
                event.preventDefault();
                void closeWizard();
            }
        });
        document.addEventListener('visibilitychange', onVisibilityChange);
        if (resumedStatus?.status === 'running') renderRunning(resumedStatus);
        else renderIntro();
    }

    function dataRow(title, subtitle, id, status = null) {
        const statusTag = status ? `<span class="status-tag ${status.isOff ? 'off' : ''}">${escapeHtml(status.label)}</span>` : '';
        return `<article class="data-row"><div class="data-row-main"><div class="data-row-heading"><strong>${escapeHtml(title)}</strong>${statusTag}</div><span class="data-row-subtitle">${escapeHtml(subtitle)}</span></div><div class="row-actions"><button type="button" data-edit="${id}" aria-label="${escapeHtml(title)} bearbeiten">${icons.edit}</button><button type="button" class="delete" data-delete="${id}" aria-label="${escapeHtml(title)} löschen">${icons.trash}</button></div></article>`;
    }

    function ingredientDataRow(ingredient) {
        const stockLabels = { available: `${formatNumber(ingredient.stockPercentage)} %`, low: 'Vorrat niedrig', unavailable: 'Leer' };
        const stockLabel = stockLabels[ingredient.stockStatus] || `${formatNumber(ingredient.stockPercentage)} %`;
        const alcohol = ingredient.alcoholPercentage == null ? 'Kein Alkoholwert' : `${formatNumber(ingredient.alcoholPercentage)} % Vol.`;
        const pump = ingredient.hasPump ? 'Pumpe zugeordnet' : 'Keine Pumpe';
        const refillDisabled = Number(ingredient.remainingVolumeMl) >= Number(ingredient.bottleSizeMl);
        const refillTitle = refillDisabled ? `${ingredient.name} ist bereits voll` : `${ingredient.name} auf ${formatNumber(ingredient.bottleSizeMl)} ml auffüllen`;
        return `<article class="data-row ingredient-data-row"><div class="data-row-main"><div class="data-row-heading"><strong>${escapeHtml(ingredient.name)}</strong><span class="stock-status ${escapeHtml(ingredient.stockStatus)}">${escapeHtml(stockLabel)}</span></div><span class="data-row-subtitle">${formatNumber(ingredient.remainingVolumeMl)} / ${formatNumber(ingredient.bottleSizeMl)} ml · ${escapeHtml(alcohol)} · ${escapeHtml(pump)}</span></div><div class="row-actions"><button type="button" class="refill-button" data-refill="${ingredient.id}" ${refillDisabled ? 'disabled' : ''} title="${escapeHtml(refillTitle)}" aria-label="${escapeHtml(refillTitle)}">${icons.refill}</button><button type="button" data-edit="${ingredient.id}" aria-label="${escapeHtml(ingredient.name)} bearbeiten">${icons.edit}</button><button type="button" class="delete" data-delete="${ingredient.id}" aria-label="${escapeHtml(ingredient.name)} löschen">${icons.trash}</button></div></article>`;
    }

    function pumpDataRow(pump) {
        const status = `<span class="status-tag ${pump.isEnabled ? '' : 'off'}">${pump.isEnabled ? 'Aktiv' : 'Deaktiviert'}</span>`;
        const calibrationLabels = { manual: 'Manuell', current: 'Kalibriert', stale: 'Neu kalibrieren' };
        const calibrationLabel = calibrationLabels[pump.calibrationStatus] || 'Manuell';
        const calibrationTitle = pump.lastCalibratedAt ? `Zuletzt kalibriert: ${formatDateTime(pump.lastCalibratedAt)}` : 'Förderrate wurde manuell eingetragen';
        const calibrationStatus = `<span class="calibration-status ${escapeHtml(pump.calibrationStatus || 'manual')}" title="${escapeHtml(calibrationTitle)}">${escapeHtml(calibrationLabel)}</span>`;
        const subtitle = `GPIO ${pump.gpioPin} · ${formatFlowRate(pump.flowRateMlPerSecond)} ml/s · ${pump.ingredientName || 'Keine Zutat'}`;
        const disabled = pump.isEnabled ? '' : 'disabled';
        const title = pump.isEnabled ? `${pump.name} gedrückt halten, um den Schlauch zu füllen` : `${pump.name} ist deaktiviert`;
        const canCalibrate = state.system?.pumpDriver === 'Gpio' && pump.isEnabled && pump.ingredientId;
        const calibrationReason = state.system?.pumpDriver !== 'Gpio'
            ? 'Kalibrierung ist nur mit dem GPIO-Treiber möglich'
            : !pump.isEnabled ? `${pump.name} ist deaktiviert`
                : !pump.ingredientId ? 'Vor der Kalibrierung eine Zutat zuordnen'
                    : `${pump.name} kalibrieren`;
        return `<article class="data-row pump-data-row"><div class="data-row-main"><div class="data-row-heading"><strong>${escapeHtml(pump.name)}</strong>${status}${calibrationStatus}</div><span class="data-row-subtitle">${escapeHtml(subtitle)}</span></div><div class="row-actions"><button type="button" class="prime-pump-button" data-prime="${pump.id}" data-enabled="${pump.isEnabled}" ${disabled} title="${escapeHtml(title)}" aria-label="${escapeHtml(title)}">${icons.prime}<span>Füllen</span></button><button type="button" class="calibrate-pump-button" data-calibrate="${pump.id}" ${canCalibrate ? '' : 'disabled'} title="${escapeHtml(calibrationReason)}" aria-label="${escapeHtml(calibrationReason)}">${icons.calibrate}<span>Kalibrieren</span></button><button type="button" data-edit="${pump.id}" aria-label="${escapeHtml(pump.name)} bearbeiten">${icons.edit}</button><button type="button" class="delete" data-delete="${pump.id}" aria-label="${escapeHtml(pump.name)} löschen">${icons.trash}</button></div></article>`;
    }

    function dialogFormActions() { return '<div class="form-field full form-actions"><button type="button" class="secondary-button" data-dialog-close>Abbrechen</button><button type="submit" class="primary-button">Speichern</button></div>'; }
    function emptyList(message) { return `<div class="empty-state">${escapeHtml(message)}</div>`; }

    function optionList(items, selected, label) {
        return items.map(item => `<option value="${item.id}" ${Number(selected) === item.id ? 'selected' : ''}>${escapeHtml(label(item))}</option>`).join('');
    }

    async function api(url, options = {}) {
        const init = { method: options.method || 'GET', headers: { Accept: 'application/json' } };
        if (options.body instanceof FormData) init.body = options.body;
        else if (options.body !== undefined) { init.headers['Content-Type'] = 'application/json'; init.body = JSON.stringify(options.body); }
        const response = await fetch(url, init);
        if (response.status === 204) return null;
        const contentType = response.headers.get('content-type') || '';
        const payload = contentType.includes('application/json') ? await response.json() : null;
        if (!response.ok) {
            const validationMessage = payload?.errors ? Object.values(payload.errors).flat().join(' ') : null;
            throw new Error(validationMessage || payload?.detail || payload?.title || `HTTP ${response.status}`);
        }
        return payload;
    }

    function showToast(message, isError = false) {
        const toast = document.createElement('div');
        toast.className = `toast ${isError ? 'error' : ''}`;
        toast.textContent = message;
        toastRegion.append(toast);
        if (window.gsap && !reduceMotion) gsap.from(toast, { opacity: 0, x: 20, duration: .25 });
        setTimeout(() => {
            if (window.gsap && !reduceMotion) gsap.to(toast, { opacity: 0, x: 20, duration: .2, onComplete: () => toast.remove() });
            else toast.remove();
        }, 4500);
    }

    function clearTimers() {
        clearInterval(state.pollTimer);
        clearInterval(state.primingPollTimer);
        clearTimeout(state.completionTimer);
        state.pollTimer = null;
        state.primingPollTimer = null;
        state.completionTimer = null;
    }

    function safeImage(path) {
        const value = path || '/assets/sunset-breeze.svg';
        return /^\/(assets|uploads)\/[a-zA-Z0-9._/-]+$/.test(value) ? value : '/assets/sunset-breeze.svg';
    }

    function ingredientSummary(cocktail) { return cocktail.ingredients.map(x => x.name).join(', '); }
    function formatNumber(value) { return new Intl.NumberFormat('de-DE', { maximumFractionDigits: 2 }).format(value); }
    function formatFlowRate(value) { return new Intl.NumberFormat('de-DE', { maximumFractionDigits: 3 }).format(value); }
    function formatDateTime(value) { return value ? new Intl.DateTimeFormat('de-DE', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : 'gerade eben'; }
    function escapeHtml(value) { return String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[char]); }
})();
