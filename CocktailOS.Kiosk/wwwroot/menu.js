const menu = document.querySelector('#menu');
const escapeHtml = value => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[char]);
const ml = value => new Intl.NumberFormat('de-DE', { maximumFractionDigits: 0 }).format(value);
fetch('/api/menu/cocktails').then(response => response.ok ? response.json() : Promise.reject()).then(cocktails => {
    menu.innerHTML = cocktails.map(cocktail => `<article><img src="${escapeHtml(cocktail.imagePath || '/assets/placeholder-cocktail.svg')}" alt="" onerror="this.hidden=true"><div><h2>${escapeHtml(cocktail.name)}</h2><p>${escapeHtml(cocktail.description || '')}</p><dl>${cocktail.ingredients.map(item => `<div><dt>${escapeHtml(item.name)}</dt><dd>${ml(item.amountMl)} ml</dd></div>`).join('')}</dl><small>${ml(cocktail.volumeMl)} ml · ${Number(cocktail.alcoholPercentage || 0).toFixed(1).replace('.', ',')} % Vol.</small></div></article>`).join('') || '<p>Noch keine Cocktails angelegt.</p>';
}).catch(() => { menu.textContent = 'Die Cocktailkarte ist gerade nicht erreichbar.'; });
