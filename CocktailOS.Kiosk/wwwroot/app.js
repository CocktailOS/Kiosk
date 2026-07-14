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
        download: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 3v12M7 10l5 5 5-5"/><path d="M5 21h14"/></svg>'
    };

    document.addEventListener('DOMContentLoaded', initialize);
    window.addEventListener('hashchange', renderRoute);

    async function initialize() {
        renderLoading();
        try {
            await loadAll();
            renderRoute();
            const current = await api('/api/dispenses/current');
            if (current.status === 'running') {
                if (current.mode === 'cleaning') showCleaningProgress(current);
                else if (current.mode === 'priming') {
                    await api('/api/priming/current/stop', { method: 'POST' });
                    showToast('Die laufende Pumpenvorbereitung wurde aus Sicherheitsgründen gestoppt.');
                }
                else showDispense(current);
            }
        } catch (error) {
            renderFatal(error.message);
        }
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
        app.innerHTML = `${headerTemplate('Cocktailmaschine wird geladen …')}<main class="loading-state" id="app-main">Cocktails werden vorbereitet …</main>`;
    }

    function renderFatal(message) {
        app.innerHTML = `${headerTemplate('Nicht verbunden', 'error')}<main class="loading-state" id="app-main"><div><h1>Verbindung fehlgeschlagen</h1><p>${escapeHtml(message)}</p><button class="primary-button" onclick="location.reload()">Erneut versuchen</button></div></main>`;
    }

    function headerTemplate(label = 'Bereit zum Mixen', statusClass = '', showSettings = false) {
        return `<header class="topbar">
            <div class="brand"><div class="brand-mark">${icons.logo}</div><div class="brand-copy"><strong>CocktailOS</strong></div></div>
            <div class="header-actions"><div class="machine-state"><span class="state-dot ${statusClass}"></span><span>${escapeHtml(label)}</span></div>${showSettings ? `<button class="icon-button settings-button" aria-label="Einstellungen öffnen">${icons.settings}</button>` : ''}</div>
        </header>`;
    }

    function renderHome() {
        const pages = [];
        for (let index = 0; index < state.cocktails.length; index += 6) {
            const cards = state.cocktails.slice(index, index + 6).map(cocktail => `<button class="cocktail-card" data-cocktail-id="${cocktail.id}" aria-label="${escapeHtml(cocktail.name)} auswählen">
                <img class="card-image" src="${safeImage(cocktail.imagePath)}" alt="" width="400" height="520">
                <span class="card-shade"></span>
                <span class="card-content"><span class="card-meta"><span class="meta-pill">${cocktail.standardSize.volumeMl} ml</span>${cocktail.alcoholPercentage > 0 ? `<span class="meta-pill">${formatNumber(cocktail.alcoholPercentage)} % Vol.</span>` : '<span class="meta-pill">Alkoholfrei</span>'}</span><h2>${escapeHtml(cocktail.name)}</h2><p>${escapeHtml(cocktail.description || ingredientSummary(cocktail))}</p></span>
            </button>`).join('');
            pages.push(`<section class="cocktail-page" aria-label="Cocktailseite ${pages.length + 1}">${cards}</section>`);
        }

        const content = pages.length
            ? pages.join('')
            : '<section class="cocktail-page cocktail-page-empty"><div class="empty-state">Noch keine Cocktails angelegt.</div></section>';

        app.innerHTML = `${headerTemplate('Bereit zum Mixen', '', true)}<main class="home-main" id="app-main"><div class="cocktail-scroller" aria-label="Cocktails">${content}</div></main>`;

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
        state.selectedSizeId = state.selectedCocktail.standardSize.id;
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
        const sizes = state.sizes.map(item => `<button class="size-option" data-size-id="${item.id}" aria-pressed="${item.id === size.id}"><strong>${escapeHtml(item.name)}</strong><br><small>${item.volumeMl} ml</small></button>`).join('');
        return `<div class="modal-scrim"></div><section class="cocktail-modal" role="dialog" aria-modal="true" aria-labelledby="cocktail-title">
            <button class="modal-close" aria-label="Dialog schließen">${icons.close}</button>
            <div class="modal-media"><img src="${safeImage(cocktail.imagePath)}" alt="${escapeHtml(cocktail.name)}" width="400" height="520"></div>
            <div class="modal-body"><span class="eyebrow">Deine Auswahl</span><h2 id="cocktail-title">${escapeHtml(cocktail.name)}</h2><p class="modal-description">${escapeHtml(cocktail.description || '')}</p><ul class="ingredient-list">${ingredients}</ul><span class="field-label">Größe auswählen</span><div class="size-selector">${sizes}</div><button class="primary-button wide-button start-dispense">${icons.play} Cocktail starten</button></div>
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
        layer.querySelector('.start-dispense').addEventListener('click', startDispense);
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
            finishStopped(button.closest('[data-operation-mode]')?.dataset.operationMode);
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
        state.completionTimer = setTimeout(() => { layer.remove(); renderHome(); }, 5000);
    }

    function finishStopped(mode) {
        clearInterval(state.pollTimer);
        document.querySelector('.modal-layer')?.remove();
        showToast(mode === 'cleaning' ? 'Reinigung gestoppt. Alle Pumpen sind aus.' : 'Ausschank gestoppt. Alle Pumpen sind aus.');
        renderRoute();
    }

    function finishFailed(message) {
        clearInterval(state.pollTimer);
        document.querySelector('.modal-layer')?.remove();
        showToast(message || 'Ausschank fehlgeschlagen.', true);
        renderRoute();
    }

    function closeLayer(layer) {
        if (window.gsap && !reduceMotion) gsap.to(layer, { opacity: 0, duration: .16, onComplete: () => layer.remove() });
        else layer.remove();
    }

    function renderSettings() {
        const tabs = [
            ['cocktails', 'Cocktails'], ['ingredients', 'Zutaten'], ['pumps', 'Pumpen'], ['sizes', 'Größen'], ['system', 'System']
        ];
        app.innerHTML = `<section class="settings-page"><nav class="settings-nav" aria-label="Einstellungsbereiche"><button class="icon-button back-home" aria-label="Zurück zur Startseite">${icons.back}</button>${tabs.map(([id, label]) => `<button class="tab-button" data-tab="${id}" role="tab" aria-selected="${state.activeTab === id}">${label}</button>`).join('')}<span class="settings-version" aria-label="Anwendungsversion">v${escapeHtml(state.version || '–')}</span></nav><main class="settings-main" id="app-main"></main></section>`;
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
            renderSettings();
        }));
        app.querySelector('[data-theme-toggle]').addEventListener('click', () => setTheme(nextTheme));
        app.querySelector('[data-app-update]')?.addEventListener('click', startApplicationUpdate);
        renderActiveSettingsTab();
    }

    async function startApplicationUpdate() {
        const button = app.querySelector('[data-app-update]');
        if (!button) return;
        button.disabled = true;
        button.classList.add('is-updating');
        try {
            const update = await api('/api/app-update', { method: 'POST' });
            showToast(`Update auf ${update.version} wird installiert. Die App startet danach automatisch neu.`);
            waitForUpdatedApplication();
        } catch (error) {
            button.disabled = false;
            button.classList.remove('is-updating');
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
        main.innerHTML = settingsListTemplate('Cocktails', 'Rezepte und Standardgrößen verwalten.', 'Cocktail anlegen', list || emptyList('Noch keine Cocktails vorhanden.'));
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
        submit.disabled = true;
        captureCocktailDraft(event.currentTarget.closest('.modal-layer'));
        const draft = state.cocktailDraft;
        const payload = { name: draft.name, description: draft.description || null, imagePath: draft.imagePath || null, standardSizeId: Number(draft.standardSizeId), ingredients: draft.ingredients.map(x => ({ ingredientId: Number(x.ingredientId), amountMl: Number(x.amountMl) })) };
        const saved = await saveEntity('cocktails', state.editing, payload, 'Cocktail');
        if (!saved && submit.isConnected) submit.disabled = false;
    }

    function renderIngredientSettings(main) {
        const list = state.ingredients.map(x => dataRow(x.name, x.alcoholPercentage == null ? 'Kein Alkoholwert' : `${formatNumber(x.alcoholPercentage)} % Vol.${x.hasPump ? ' · Pumpe zugeordnet' : ''}`, x.id)).join('');
        main.innerHTML = settingsListTemplate('Zutaten', 'Alkoholwerte und Pumpenzuordnung im Blick behalten.', 'Zutat anlegen', list || emptyList('Noch keine Zutaten vorhanden.'));
        main.querySelector('.add-entity').addEventListener('click', () => openIngredientEditor());
        bindRowActions(main, id => openIngredientEditor(id), id => {
            const item = state.ingredients.find(x => x.id === id);
            openDeleteDialog('ingredients', id, 'Zutat', item?.name || 'Zutat');
        });
    }

    function openIngredientEditor(id = null) {
        const item = id ? state.ingredients.find(x => x.id === id) : null;
        state.editing = id;
        const form = `<form id="entity-form" class="form-grid"><div class="form-field full"><label for="ingredient-name">Name</label><input id="ingredient-name" name="name" required maxlength="100" value="${escapeHtml(item?.name || '')}"></div><div class="form-field full"><label for="alcohol">Alkohol in % Vol.</label><input id="alcohol" name="alcoholPercentage" type="number" min="0" max="100" step="0.1" inputmode="decimal" value="${item?.alcoholPercentage ?? ''}"></div>${dialogFormActions()}</form>`;
        openSimpleEditorDialog(item ? 'Zutat bearbeiten' : 'Zutat anlegen', 'Der Alkoholwert ist optional und dient der Anzeige.', form, 'ingredients', 'Zutat', formElement => ({ name: formElement.elements.name.value, alcoholPercentage: formElement.elements.alcoholPercentage.value === '' ? null : Number(formElement.elements.alcoholPercentage.value) }));
    }

    function renderPumpSettings(main) {
        const list = state.pumps.map(pumpDataRow).join('');
        main.innerHTML = settingsListTemplate('Pumpen', 'GPIO, Förderrate und zugeordnete Zutaten verwalten.', 'Pumpe anlegen', list || emptyList('Noch keine Pumpen vorhanden.'));
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
    }

    function openPumpEditor(id = null) {
        const item = id ? state.pumps.find(x => x.id === id) : null;
        state.editing = id;
        const flowRate = Math.round(Number(item?.flowRateMlPerSecond ?? 10) * 10) / 10;
        const flowMaximum = Math.max(100, Math.ceil(flowRate / 10) * 10);
        const form = `<form id="entity-form" class="form-grid"><div class="form-field full"><label for="pump-name">Name</label><input id="pump-name" name="name" required maxlength="100" value="${escapeHtml(item?.name || '')}"></div><div class="form-field full"><label for="gpio-pin">GPIO-Pin</label><input id="gpio-pin" name="gpioPin" type="number" min="0" max="40" required inputmode="numeric" value="${item?.gpioPin ?? ''}"></div>${valueSliderField('flow-rate', 'flowRate', 'Förderrate', 'ml/s', .1, flowMaximum, .1, flowRate, true)}<div class="form-field full"><label for="pump-ingredient">Zutat</label><select id="pump-ingredient" name="ingredientId"><option value="">Keine Zuordnung</option>${optionList(state.ingredients, item?.ingredientId, x => x.name)}</select></div><div class="form-field full checkbox-row"><label class="checkbox-field"><input name="activeHigh" type="checkbox" ${item?.activeHigh ? 'checked' : ''}> Relais ist active HIGH</label><label class="checkbox-field"><input name="isEnabled" type="checkbox" ${item ? (item.isEnabled ? 'checked' : '') : 'checked'}> Pumpe ist aktiviert</label></div>${dialogFormActions()}</form>`;
        openSimpleEditorDialog(item ? 'Pumpe bearbeiten' : 'Pumpe anlegen', 'Maximal 8 Pumpen. Förderrate bitte vorher kalibrieren.', form, 'pumps', 'Pumpe', formElement => ({ name: formElement.elements.name.value, gpioPin: Number(formElement.elements.gpioPin.value), flowRateMlPerSecond: Number(formElement.elements.flowRate.value), activeHigh: formElement.elements.activeHigh.checked, isEnabled: formElement.elements.isEnabled.checked, ingredientId: formElement.elements.ingredientId.value ? Number(formElement.elements.ingredientId.value) : null }));
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
            if (location.hash.startsWith('#/settings')) renderSettings();
            else location.hash = '#/settings';
        });
        layer.querySelector('.cleaning-done').focus();
    }

    function renderSizeSettings(main) {
        const list = state.sizes.map(x => dataRow(x.name, `${x.volumeMl} ml · Sortierung ${x.sortOrder}`, x.id)).join('');
        main.innerHTML = settingsListTemplate('Größen', 'Cocktailgrößen und deren Reihenfolge verwalten.', 'Größe anlegen', list || emptyList('Noch keine Größen vorhanden.'));
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
        main.innerHTML = `<section class="settings-card system-hero"><h2>Hardwaresteuerung</h2><p class="card-intro">Der Dummy-Treiber simuliert Pumpen. GPIO steuert die angeschlossenen Relais auf dem Raspberry Pi.</p><form id="system-form"><div class="system-options"><div><span class="field-label">Pumpentreiber</span><label class="option-card"><input type="radio" name="pumpDriver" value="Dummy" ${config.pumpDriver === 'Dummy' ? 'checked' : ''}><strong>Dummy</strong><span>Sicher testen, ohne GPIO-Ausgänge zu schalten.</span></label><label class="option-card" style="margin-top:10px"><input type="radio" name="pumpDriver" value="Gpio" ${config.pumpDriver === 'Gpio' ? 'checked' : ''}><strong>Raspberry Pi GPIO</strong><span>Steuert reale Relais über System.Device.Gpio.</span></label></div><div><span class="field-label">Pin-Nummerierung</span><label class="option-card"><input type="radio" name="pinNumberingScheme" value="Logical" ${config.pinNumberingScheme === 'Logical' ? 'checked' : ''}><strong>Logical / BCM</strong><span>GPIO-Nummern, beispielsweise GPIO17.</span></label><label class="option-card" style="margin-top:10px"><input type="radio" name="pinNumberingScheme" value="Board" ${config.pinNumberingScheme === 'Board' ? 'checked' : ''}><strong>Board / physisch</strong><span>Positionen 1 bis 40 auf dem Raspberry-Pi-Header.</span></label></div></div><div class="system-note">High/Low wird für jede Pumpe einzeln konfiguriert. Bei active Low bleibt der Ausgang im Ruhezustand High und wird zum Pumpen Low geschaltet.</div><div class="form-actions"><button class="primary-button" type="submit">Hardwarekonfiguration speichern</button></div></form></section>`;
        main.querySelector('#system-form').addEventListener('submit', async event => {
            event.preventDefault();
            const form = event.currentTarget;
            try {
                state.system = await api('/api/system', { method: 'PUT', body: { pumpDriver: form.elements.pumpDriver.value, pinNumberingScheme: form.elements.pinNumberingScheme.value, theme: state.system.theme } });
                showToast('Hardwarekonfiguration gespeichert.');
                renderActiveSettingsTab();
            } catch (error) { showToast(error.message, true); }
        });
    }

    function settingsListTemplate(title, intro, addLabel, content) {
        return `<div class="settings-list-view"><div class="settings-list-header"><div><h2>${escapeHtml(title)}</h2><p>${escapeHtml(intro)}</p></div><div class="settings-header-actions"><button type="button" class="primary-button add-entity">${icons.plus} ${escapeHtml(addLabel)}</button></div></div><section class="settings-card list-card"><div class="data-list">${content}</div></section></div>`;
    }

    function createSettingsDialog(className = '') {
        const layer = document.createElement('div');
        layer.className = 'modal-layer settings-dialog-layer';
        layer.innerHTML = `<div class="modal-scrim"></div><section class="settings-dialog ${className}" role="dialog" aria-modal="true" aria-labelledby="settings-dialog-title"><div class="settings-dialog-content"></div></section>`;
        layer.returnFocus = document.activeElement;
        app.append(layer);
        layer.querySelector('.modal-scrim').addEventListener('click', () => dismissSettingsDialog(layer));
        layer.addEventListener('keydown', event => {
            if (event.key === 'Escape') dismissSettingsDialog(layer);
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

    function setSettingsDialogContent(layer, title, intro, body) {
        layer.querySelector('.settings-dialog-content').innerHTML = `<header class="settings-dialog-header"><div><h2 id="settings-dialog-title">${escapeHtml(title)}</h2>${intro ? `<p>${escapeHtml(intro)}</p>` : ''}</div><button type="button" class="icon-button" data-dialog-close aria-label="Dialog schließen">${icons.close}</button></header><div class="settings-dialog-body">${body}</div>`;
        layer.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => dismissSettingsDialog(layer)));
        if (!layer.dataset.focused) {
            layer.dataset.focused = 'true';
            requestAnimationFrame(() => (layer.querySelector('.settings-dialog-body input, .settings-dialog-body select, .settings-dialog-body textarea') || layer.querySelector('button'))?.focus());
        }
    }

    function dismissSettingsDialog(layer) {
        if (!layer?.isConnected) return;
        const returnFocus = layer.returnFocus;
        state.editing = null;
        state.cocktailDraft = null;
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
            submit.disabled = true;
            const saved = await saveEntity(resource, state.editing, createPayload(form), label);
            if (!saved && submit.isConnected) submit.disabled = false;
        });
    }

    function openDeleteDialog(resource, id, label, name) {
        const layer = createSettingsDialog('delete-dialog');
        const content = `<p class="delete-dialog-copy"><strong>${escapeHtml(name)}</strong> wird dauerhaft gelöscht. Dieser Vorgang kann nicht rückgängig gemacht werden.</p><div class="form-actions"><button type="button" class="secondary-button" data-dialog-close>Abbrechen</button><button type="button" class="danger-button confirm-delete">${escapeHtml(label)} löschen</button></div>`;
        setSettingsDialogContent(layer, `${label} löschen?`, '', content);
        layer.querySelector('.confirm-delete').addEventListener('click', async event => {
            event.currentTarget.disabled = true;
            const removed = await removeEntity(resource, id, label);
            if (!removed && event.currentTarget.isConnected) event.currentTarget.disabled = false;
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

    function dataRow(title, subtitle, id, status = null) {
        const statusTag = status ? `<span class="status-tag ${status.isOff ? 'off' : ''}">${escapeHtml(status.label)}</span>` : '';
        return `<article class="data-row"><div class="data-row-main"><div class="data-row-heading"><strong>${escapeHtml(title)}</strong>${statusTag}</div><span class="data-row-subtitle">${escapeHtml(subtitle)}</span></div><div class="row-actions"><button type="button" data-edit="${id}" aria-label="${escapeHtml(title)} bearbeiten">${icons.edit}</button><button type="button" class="delete" data-delete="${id}" aria-label="${escapeHtml(title)} löschen">${icons.trash}</button></div></article>`;
    }

    function pumpDataRow(pump) {
        const status = `<span class="status-tag ${pump.isEnabled ? '' : 'off'}">${pump.isEnabled ? 'Aktiv' : 'Deaktiviert'}</span>`;
        const subtitle = `GPIO ${pump.gpioPin} · ${formatFlowRate(pump.flowRateMlPerSecond)} ml/s · ${pump.ingredientName || 'Keine Zutat'}`;
        const disabled = pump.isEnabled ? '' : 'disabled';
        const title = pump.isEnabled ? `${pump.name} gedrückt halten, um den Schlauch zu füllen` : `${pump.name} ist deaktiviert`;
        return `<article class="data-row pump-data-row"><div class="data-row-main"><div class="data-row-heading"><strong>${escapeHtml(pump.name)}</strong>${status}</div><span class="data-row-subtitle">${escapeHtml(subtitle)}</span></div><div class="row-actions"><button type="button" class="prime-pump-button" data-prime="${pump.id}" data-enabled="${pump.isEnabled}" ${disabled} title="${escapeHtml(title)}" aria-label="${escapeHtml(title)}">${icons.prime}<span>Füllen</span></button><button type="button" data-edit="${pump.id}" aria-label="${escapeHtml(pump.name)} bearbeiten">${icons.edit}</button><button type="button" class="delete" data-delete="${pump.id}" aria-label="${escapeHtml(pump.name)} löschen">${icons.trash}</button></div></article>`;
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
    function formatFlowRate(value) { return new Intl.NumberFormat('de-DE', { maximumFractionDigits: 1 }).format(value); }
    function escapeHtml(value) { return String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[char]); }
})();
