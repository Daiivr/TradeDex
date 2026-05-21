const state = {
    authenticated: false,
    polling: null,
    language: 'English',
    mode: 'SV',
    lastTradeState: null,
    pkmFile: null,
    composerOpen: false,
    terminalSignature: '',
    terminalTypingRun: 0,
    terminalTradeKey: '',
    terminalTypingQueue: Promise.resolve(),
    confettiTradeKey: '',
    profileRefreshTradeKey: '',
};

const $ = (id) => document.getElementById(id);

const ES = {
    pageTitle: 'Trades TradeDex',
    brandSuffix: 'Trades Web',
    controlPanel: 'Control',
    webTrades: 'Trades web',
    heroTitle: 'Tradea sin estar en Discord',
    heroText: 'Inicia sesion una vez, pega tu set de Pokemon Showdown y sigue las instrucciones en vivo hasta completar el trade.',
    queue: 'Cola',
    checking: 'Revisando',
    session: 'Sesion',
    guest: 'Invitado',
    mode: 'Modo',
    request: 'Solicitud',
    requestTitle: 'Crear un trade',
    terminalTitle: 'Sigue el trade',
    discordLogin: 'Login de Discord requerido',
    loginText: 'Tu Discord ID se usa para codigos de trade, datos guardados de entrenador y seguimiento de cola.',
    loginButton: 'Login con Discord',
    trainerName: 'Nombre de entrenador',
    trainerPlaceholder: 'Tu nombre dentro del juego',
    tradeCode: 'Codigo de trade',
    showdownSet: 'Set de Pokemon Showdown',
    showdownPlaceholder: 'Flutter Mane @ Booster Energy\nAbility: Protosynthesis\nTera Type: Fairy\nEVs: 4 HP / 252 SpA / 252 Spe\nTimid Nature\n- Moonblast\n- Shadow Ball\n- Protect\n- Icy Wind',
    pkhexUpload: 'Subir archivo PKHeX',
    pkhexHelp: 'Opcional: usa un archivo .pk/.pb/.pa en lugar de un set Showdown.',
    clearFile: 'Quitar',
    fileSelected: 'Archivo seleccionado: {name}',
    fileTooLarge: 'El archivo PKHeX es demasiado grande.',
    fileReadFailed: 'No se pudo leer el archivo.',
    legalToastTitle: 'Pokemon no legal',
    legalToastIntro: 'PKHeX encontro estos problemas:',
    legalMoveInvalid: 'Movimiento {slot}: no es valido para este Pokemon.',
    legalEncounterMismatch: 'No coincide con ningun encuentro valido del juego de origen.',
    legalEggLanguageMismatch: 'El nombre de huevo no coincide con el idioma del archivo.',
    legalEggHeldItem: 'Los huevos no pueden llevar objetos.',
    legalIssueUnknown: 'Revisa el archivo en PKHeX antes de enviarlo.',
    ignoreAutoOt: 'No aplicar AutoOT guardado',
    joinQueue: 'Entrar a la cola',
    liveStatus: 'Estado en vivo',
    currentTrade: 'Trade actual',
    cancel: 'Cancelar',
    emptyTrade: 'Tu trade en cola aparecera aqui.',
    guide: 'Guia',
    tradeConsole: 'Guia del website',
    guideButtonTitle: 'Abrir guia de trade',
    linkCode: 'Codigo',
    requestAnotherTrade: 'Solicitar otro trade',
    profile: 'Perfil',
    totalTrades: 'Trades totales',
    trainer: 'Entrenador',
    discordUser: 'Discord',
    lastTrade: 'Ultimo trade',
    editTradeCode: 'Editar codigo de trade',
    tradeCodePlaceholder: '0000 0000',
    saveCode: 'Guardar codigo',
    deleteCode: 'Borrar',
    tradeCodeHelp: 'Usa ocho digitos. Este codigo se guarda en tu Discord ID.',
    invalidTradeCode: 'Ingresa un codigo de trade de ocho digitos.',
    tradeCodeSaved: 'Codigo de trade guardado.',
    tradeCodeDeleted: 'Codigo de trade borrado.',
    tradeCodeSaveFailed: 'No se pudo guardar el codigo de trade.',
    tradeCodeDeleteFailed: 'No se pudo borrar el codigo de trade.',
    guideLogin: 'Inicia sesion con Discord para que el website use tu perfil, codigo guardado y datos de entrenador.',
    guideCreate: 'Pega un set Showdown o sube un archivo PKHeX. No necesitas escribir nombre ni codigo.',
    guideQueue: 'Pulsa Entrar a la cola. Cuando el bot tome tu trade, el formulario cambiara a la terminal.',
    guideTerminal: 'Sigue la terminal en vivo: ahi veras cuando buscar el codigo, el entrenador encontrado y que confirmar.',
    guideProfile: 'Usa Perfil para revisar tu historial y editar o borrar tu codigo de trade guardado.',
    open: 'Abierta',
    closed: 'Cerrada',
    unavailable: 'No disponible',
    loggedIn: 'Conectado',
    queued: 'En cola',
    searching: 'Buscando',
    initializing: 'Preparando',
    partner: 'Encontrado',
    processing: 'Procesando',
    finished: 'Finalizado',
    cancelled: 'Cancelado',
    working: 'Procesando...',
    loginUnconfigured: 'Discord OAuth aun no esta configurado. Agrega Client ID, Client Secret y Redirect URI en los ajustes de WebServer.',
    loginCouldNotStart: 'No se pudo iniciar el login.',
    discordLoginComplete: 'Login de Discord completado.',
    discordLoginFailed: 'Fallo el login de Discord.',
    profileLoadFailed: 'No se pudo cargar el perfil.',
    pasteShowdownFirst: 'Pega primero un set de Pokemon Showdown.',
    pasteShowdownOrFileFirst: 'Pega un set Showdown o sube un archivo PKHeX primero.',
    tradeQueueFailed: 'No se pudo agregar el trade a la cola.',
    queueClosedOrNoBots: 'La cola de trade esta cerrada o no hay bots listos.',
    tradeRunnerNotReady: 'El bot de trades aun no esta listo.',
    alreadyInQueue: 'Ya tienes un trade en la cola.',
    queueFull: 'La cola de trade esta llena.',
    notAllowedItem: 'Ese Pokemon lleva un objeto que no se puede intercambiar.',
    tradeAdded: 'Trade agregado a la cola.',
    cancelFailed: 'No se pudo cancelar el trade.',
    tradeCancelled: 'Trade cancelado.',
    positionLine: 'Posicion {position} de {total}',
    waitingLatest: 'Esperando el ultimo estado.',
    noTradeConsole: 'Pega un set, entra a la cola y esta consola te dira cuando buscar el codigo.',
    loginConsole: 'Inicia sesion con Discord para usar tu codigo guardado y tu perfil.',
    submitConsole: 'Cuando tu trade entre a la cola, prepara tu consola y espera tu turno.',
    queuedConsole: 'Estas en cola. Mantente listo y no cierres esta pagina.',
    initializingConsole: 'El bot esta preparando el menu de intercambio. Mantente listo.',
    searchConsole: 'Busca intercambio por codigo ahora: {code}.',
    partnerConsole: 'Entrenador encontrado. Verifica la informacion antes de confirmar.',
    processingConsole: 'Trade encontrado. Sigue las indicaciones del juego y confirma cuando corresponda.',
    finishedConsole: 'Trade completado. Gracias por usar TradeDex.',
    cancelledConsole: 'El trade fue cancelado.',
    cancelledWithReason: 'Trade cancelado: {reason}',
    terminalConnect: '[TradeDex] Sesion web conectada con Discord.',
    terminalQueue: '[COLA] Tu trade de {pokemon} fue agregado. Codigo asignado: {code}.',
    terminalQueuePosition: '[COLA] Posicion actual: {position} de {total}. Mantente listo y no cierres esta pagina.',
    terminalPrepare: '[BOT] Preparando el menu de intercambio. Abre Poke Portal > Link Trade en tu consola.',
    terminalSearch: '[BUSCAR] Busca intercambio por codigo ahora: {code}.',
    terminalFound: '[BOT] Entrenador encontrado: {trainer}. TID: {tid}. SID: {sid}. Confirma solo si la informacion coincide.',
    terminalOffer: '[BOT] Esperando que ofrezcas un Pokemon en el juego.',
    terminalComplete: '[LISTO] Trade completado. Disfruta tu {pokemon}.',
    terminalCancel: '[CANCELADO] {reason}',
    terminalWait: '[INFO] Esperando la siguiente actualizacion del bot...',
    terminalFallbackCancel: 'El trade fue cancelado.',
};

const EN = {
    pageTitle: 'TradeDex Trades',
    brandSuffix: 'Web Trades',
    controlPanel: 'Control',
    webTrades: 'Web trades',
    heroTitle: 'Trade without Discord chat',
    heroText: 'Log in once, paste your Pokemon Showdown set or upload a PKHeX file, then follow the live instructions until the trade is complete.',
    queue: 'Queue',
    checking: 'Checking',
    session: 'Session',
    guest: 'Guest',
    mode: 'Mode',
    request: 'Request',
    requestTitle: 'Create a trade',
    terminalTitle: 'Follow the trade',
    discordLogin: 'Discord login required',
    loginText: 'Your Discord ID is used for trade codes, saved trainer data, and queue tracking.',
    loginButton: 'Login with Discord',
    trainerName: 'Trainer name',
    trainerPlaceholder: 'Your in-game name',
    tradeCode: 'Trade code',
    showdownSet: 'Pokemon Showdown set',
    showdownPlaceholder: 'Flutter Mane @ Booster Energy\nAbility: Protosynthesis\nTera Type: Fairy\nEVs: 4 HP / 252 SpA / 252 Spe\nTimid Nature\n- Moonblast\n- Shadow Ball\n- Protect\n- Icy Wind',
    pkhexUpload: 'Upload PKHeX file',
    pkhexHelp: 'Optional: use a .pk/.pb/.pa file instead of a Showdown set.',
    clearFile: 'Clear',
    fileSelected: 'Selected file: {name}',
    fileTooLarge: 'The PKHeX file is too large.',
    fileReadFailed: 'Could not read the file.',
    legalToastTitle: 'Pokemon is not legal',
    legalToastIntro: 'PKHeX found these issues:',
    legalMoveInvalid: 'Move {slot}: not valid for this Pokemon.',
    legalEncounterMismatch: 'No valid encounter matches this Pokemon in its origin game.',
    legalEggLanguageMismatch: 'The egg name does not match the file language.',
    legalEggHeldItem: 'Eggs cannot hold held items.',
    legalIssueUnknown: 'Check the file in PKHeX before submitting it.',
    ignoreAutoOt: 'Do not apply cached AutoOT',
    joinQueue: 'Join queue',
    open: 'Open',
    closed: 'Closed',
    unavailable: 'Unavailable',
    loggedIn: 'Logged in',
    queued: 'Queued',
    searching: 'Searching',
    initializing: 'Preparing',
    partner: 'Found',
    processing: 'Processing',
    finished: 'Finished',
    cancelled: 'Cancelled',
    working: 'Working...',
    loginUnconfigured: 'Discord OAuth is not configured yet. Add Client ID, Client Secret, and Redirect URI in WebServer settings.',
    loginCouldNotStart: 'Login could not start.',
    discordLoginComplete: 'Discord login complete.',
    discordLoginFailed: 'Discord login failed.',
    profileLoadFailed: 'Could not load profile.',
    pasteShowdownFirst: 'Paste a Pokemon Showdown set first.',
    pasteShowdownOrFileFirst: 'Paste a Showdown set or upload a PKHeX file first.',
    tradeQueueFailed: 'The trade could not be queued.',
    queueClosedOrNoBots: 'The trade queue is closed or no trade bots are ready.',
    tradeRunnerNotReady: 'The trade runner is not ready yet.',
    alreadyInQueue: 'You already have a trade in the queue.',
    queueFull: 'The trade queue is full.',
    notAllowedItem: 'That Pokemon is holding an item that cannot be traded.',
    tradeAdded: 'Trade added to the queue.',
    cancelFailed: 'Could not cancel the trade.',
    tradeCancelled: 'Trade cancelled.',
    liveStatus: 'Live status',
    currentTrade: 'Current trade',
    cancel: 'Cancel',
    emptyTrade: 'Your queued trade will appear here.',
    guide: 'Guide',
    tradeConsole: 'Website guide',
    guideButtonTitle: 'Open trade guide',
    linkCode: 'Link code',
    requestAnotherTrade: 'Request another trade',
    profile: 'Profile',
    totalTrades: 'Total trades',
    trainer: 'Trainer',
    discordUser: 'Discord',
    lastTrade: 'Last trade',
    editTradeCode: 'Edit trade code',
    tradeCodePlaceholder: '0000 0000',
    saveCode: 'Save code',
    deleteCode: 'Delete',
    tradeCodeHelp: 'Use eight digits. This code is saved to your Discord ID.',
    invalidTradeCode: 'Enter an eight-digit trade code.',
    tradeCodeSaved: 'Trade code saved.',
    tradeCodeDeleted: 'Trade code deleted.',
    tradeCodeSaveFailed: 'Could not save the trade code.',
    tradeCodeDeleteFailed: 'Could not delete the trade code.',
    guideLogin: 'Log in with Discord so the website can use your profile, saved code, and trainer data.',
    guideCreate: 'Paste a Showdown set or upload a PKHeX file. You do not need to type trainer name or code.',
    guideQueue: 'Press Join queue. When the bot takes your trade, the form changes into the live terminal.',
    guideTerminal: 'Follow the live terminal: it tells you when to search the code, who was found, and what to confirm.',
    guideProfile: 'Use Profile to review your history and edit or delete your saved trade code.',
    positionLine: 'Position {position} of {total}',
    waitingLatest: 'Waiting for the latest status.',
    noTradeConsole: 'Paste a set, join the queue, and this console will tell you when to search the code.',
    loginConsole: 'Login with Discord to use your saved code and profile.',
    submitConsole: 'When your trade enters the queue, prepare your console and wait for your turn.',
    queuedConsole: 'You are in queue. Stay ready and keep this page open.',
    initializingConsole: 'The bot is loading the trade menu. Stay ready.',
    searchConsole: 'Search for a link trade with this code now: {code}.',
    partnerConsole: 'Trainer found. Check the info before confirming.',
    processingConsole: 'Trade partner found. Follow the game prompts and confirm when needed.',
    finishedConsole: 'Trade complete. Thanks for using TradeDex.',
    cancelledConsole: 'The trade was cancelled.',
    cancelledWithReason: 'Trade cancelled: {reason}',
    terminalConnect: '[TradeDex] Web session connected with Discord.',
    terminalQueue: '[QUEUE] Your {pokemon} trade was added. Assigned code: {code}.',
    terminalQueuePosition: '[QUEUE] Current position: {position} of {total}. Stay ready and keep this page open.',
    terminalPrepare: '[BOT] Loading the trade menu. Open Poke Portal > Link Trade on your console.',
    terminalSearch: '[SEARCH] Search for a link trade with this code now: {code}.',
    terminalFound: '[BOT] Trainer found: {trainer}. TID: {tid}. SID: {sid}. Confirm only if the info matches.',
    terminalOffer: '[BOT] Waiting for you to offer a Pokemon in game.',
    terminalComplete: '[DONE] Trade complete. Enjoy your {pokemon}.',
    terminalCancel: '[CANCELLED] {reason}',
    terminalWait: '[INFO] Waiting for the next bot update...',
    terminalFallbackCancel: 'The trade was cancelled.',
};

const t = (key, values = {}) => {
    const table = isSpanish() ? ES : EN;
    let text = table[key] || key;
    for (const [name, value] of Object.entries(values)) {
        text = text.replaceAll(`{${name}}`, value);
    }
    return text;
};

const isSpanish = () => ['Spanish', 'SpanishLatinAmerica', 'es'].includes(state.language);

async function api(path, options = {}) {
    const response = await fetch(path, {
        cache: 'no-cache',
        headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
        ...options,
    });

    const contentType = response.headers.get('content-type') || '';
    if (!contentType.includes('application/json')) {
        return { success: false, error: await response.text() };
    }

    return response.json();
}

async function initLocalization() {
    try {
        const info = await api('/api/bot/localization');
        state.language = info.language || 'English';
    } catch {
        state.language = document.documentElement.lang?.startsWith('es') ? 'Spanish' : 'English';
    }

    document.documentElement.lang = isSpanish() ? 'es' : 'en';
    document.querySelectorAll('[data-i18n]').forEach((node) => {
        node.textContent = t(node.dataset.i18n);
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach((node) => {
        node.placeholder = t(node.dataset.i18nPlaceholder);
    });
    document.querySelectorAll('[data-i18n-title]').forEach((node) => {
        node.title = t(node.dataset.i18nTitle);
    });
    document.title = t('pageTitle');
    renderSiteGuide();
    await initMode();
}

async function initMode() {
    try {
        const info = await api('/api/bot/instances');
        const mode = info.instances?.[0]?.mode || info.Instances?.[0]?.Mode || 'SV';
        state.mode = String(mode || 'SV').toUpperCase();
    } catch {
        state.mode = 'SV';
    }

    $('mode-state').textContent = modeLabel(state.mode);
    const image = $('mode-image');
    const imageMap = {
        SV: '/sv_mode_image.png',
        PLZA: '/plza_mode_image.png',
        SWSH: '/swsh_mode_image.png',
    };
    image.src = imageMap[state.mode] || '/sv_mode_image.png';
    image.alt = modeLabel(state.mode);
}

function modeLabel(mode) {
    return {
        SV: 'Scarlet / Violet',
        PLZA: 'Legends Z-A',
        SWSH: 'Sword / Shield',
        BDSP: 'Brilliant Diamond / Shining Pearl',
        LA: 'Legends Arceus',
        LGPE: "Let's Go",
    }[mode] || mode || 'Trade';
}

function showToast(message, options = {}) {
    const toast = $('toast');
    toast.className = `toast ${options.type || 'info'} ${options.className || ''}`.trim();
    toast.replaceChildren();

    const body = document.createElement('div');
    body.className = 'toast-body';

    if (options.title) {
        const title = document.createElement('strong');
        title.className = 'toast-title';
        title.textContent = options.title;
        body.appendChild(title);
    }

    const text = document.createElement(options.title ? 'p' : 'span');
    text.className = 'toast-message';
    text.textContent = message || options.message || '';
    body.appendChild(text);

    if (Array.isArray(options.items) && options.items.length) {
        const list = document.createElement('ul');
        list.className = 'toast-list';
        for (const item of options.items) {
            const li = document.createElement('li');
            li.textContent = item;
            list.appendChild(li);
        }
        body.appendChild(list);
    }

    toast.appendChild(body);
    toast.hidden = false;
    clearTimeout(showToast.timer);
    showToast.timer = setTimeout(() => { toast.hidden = true; }, options.duration || 4200);
}

function showApiError(result, fallbackKey) {
    if (result?.errorCode === 'pkm_legal') {
        showLegalityToast(result);
        return;
    }

    showToast(translateApiError(result?.error) || t(fallbackKey), { type: 'error' });
}

function translateApiError(message) {
    const text = String(message || '').trim();
    if (!text) return '';

    const normalized = text
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLowerCase();

    if (
        normalized.includes('queue is currently closed') ||
        normalized.includes('cola de trade is currently closed') ||
        normalized.includes('cola de trades is currently closed') ||
        normalized.includes('no trade bots are ready')
    ) {
        return t('queueClosedOrNoBots');
    }

    if (normalized.includes('trade runner is not ready') || normalized.includes('bot de trades aun no esta listo')) {
        return t('tradeRunnerNotReady');
    }

    if (normalized.includes('already have') && normalized.includes('trade') && normalized.includes('queue')) {
        return t('alreadyInQueue');
    }

    if (normalized.includes('queue is full') || normalized.includes('cola de trade esta llena')) {
        return t('queueFull');
    }

    if (normalized.includes('holding an item') || normalized.includes('item that cannot be traded') || normalized.includes('objeto que no se puede intercambiar')) {
        return t('notAllowedItem');
    }

    return text;
}

function showLegalityToast(result) {
    const issues = Array.isArray(result?.issues) ? result.issues : [];
    const translated = issues.map(translateLegalityIssue).filter(Boolean);
    showToast(t('legalToastIntro'), {
        type: 'error',
        className: 'legality-toast',
        title: t('legalToastTitle'),
        items: translated.length ? translated : [t('legalIssueUnknown')],
        duration: 11000,
    });
}

function translateLegalityIssue(issue) {
    const clean = String(issue || '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/^invalid:\s*/i, '')
        .replace(/^invalido:\s*/i, '')
        .replace(/^invalid\s+/i, '')
        .replace(/^invalido\s+/i, '')
        .trim();
    const lower = clean.toLowerCase();
    const move = clean.match(/move\s*(\d+)/i) || clean.match(/movimiento\s*(\d+)/i);

    if (move && (lower.includes('invalid') || lower.includes('invalido'))) {
        return t('legalMoveInvalid', { slot: move[1] });
    }

    if (lower.includes('unable to match an encounter') || lower.includes('encounter from origin game')) {
        return t('legalEncounterMismatch');
    }

    if (lower.includes('egg name') && lower.includes('language')) {
        return t('legalEggLanguageMismatch');
    }

    if (lower.includes('eggs cannot hold items')) {
        return t('legalEggHeldItem');
    }

    return clean || t('legalIssueUnknown');
}

function setBusy(button, busy) {
    button.disabled = busy;
    button.dataset.originalText ??= button.textContent;
    button.textContent = busy ? t('working') : button.dataset.originalText;
}

async function initAuth() {
    const me = await api('/api/trade/auth/me');
    state.authenticated = Boolean(me.authenticated);

    $('control-link').hidden = !me.isAdmin;
    $('login-panel').hidden = state.authenticated;
    $('trade-panel').hidden = !state.authenticated;
    $('trade-terminal').hidden = true;
    $('user-actions').hidden = !state.authenticated;
    toggleCodePopover(false);
    $('session-state').textContent = state.authenticated ? t('loggedIn') : t('guest');

    if (state.authenticated) {
        $('user-name').textContent = me.user.username;
        if (me.user.avatar) {
            $('user-avatar').src = me.user.avatar;
            $('user-avatar').hidden = false;
        }
        await refreshProfile();
        startPolling();
    } else {
        renderConsole(null);
        const config = await api('/api/trade/auth/config');
        if (!config.discordConfigured) {
            $('login-help').textContent = t('loginUnconfigured');
            $('login-button').disabled = true;
        }
    }
}

async function login() {
    const button = $('login-button');
    setBusy(button, true);
    try {
        const result = await api('/api/trade/auth/login');
        if (!result.success) {
            showToast(result.error || t('loginCouldNotStart'));
            return;
        }

        const popup = window.open(result.url, 'tradedex-discord-login', 'width=520,height=760');
        if (!popup) {
            window.location.href = result.url;
        }
    } finally {
        setBusy(button, false);
    }
}

async function logout() {
    await api('/api/trade/auth/logout');
    stopPolling();
    state.authenticated = false;
    await initAuth();
    renderQueue({ success: true, queueOpen: false, activeTrade: null });
}

async function refreshProfile() {
    const profile = await api('/api/trade/profile');
    if (!profile.success) {
        showToast(profile.error || t('profileLoadFailed'));
        return;
    }

    const code = formatCode(profile.tradeCode);
    $('profile-code').textContent = code || '-';
    $('profile-code').disabled = !state.authenticated;
    $('profile-code').classList.toggle('is-empty', !code);
    $('trade-code-input').value = code;
    $('profile-count').textContent = profile.tradeCount ?? 0;
    setTrainerProfile(profile);
    $('profile-last').textContent = cleanPokemonName(profile.lastTrade) || '-';
}

function setTrainerProfile(profile) {
    const name = profile.ot || '-';
    const button = $('profile-trainer');
    button.textContent = name;
    button.disabled = !profile.ot;
    button.classList.toggle('is-empty', !profile.ot);

    $('trainer-popover-name').textContent = name;
    $('trainer-popover-tid').textContent = profile.tid ? String(profile.tid) : '-';
    $('trainer-popover-sid').textContent = profile.sid ? String(profile.sid) : '-';
    $('trainer-popover-discord').textContent = profile.username || profile.discordId || '-';
}

function formatCode(value) {
    if (!value) return '';
    const digits = String(value).replace(/\D/g, '').padStart(8, '0').slice(-8);
    return `${digits.slice(0, 4)} ${digits.slice(4)}`;
}

function cleanPokemonName(value) {
    const text = String(value || '').trim();
    if (!text) return '';
    return text.replace(/\s+\([^)]*\.(pk|pb|pa)\d?[^)]*\)$/i, '').trim();
}

function normalizeCodeInput(value) {
    return String(value || '').replace(/\D/g, '').slice(0, 8);
}

function formatCodeInput(value) {
    const digits = normalizeCodeInput(value);
    return digits.length > 4 ? `${digits.slice(0, 4)} ${digits.slice(4)}` : digits;
}

async function saveTradeCode() {
    const digits = normalizeCodeInput($('trade-code-input').value);
    if (digits.length !== 8) {
        showToast(t('invalidTradeCode'));
        $('trade-code-input').focus();
        return;
    }

    const button = $('save-code-button');
    setBusy(button, true);
    try {
        const result = await api('/api/trade/code', {
            method: 'POST',
            body: JSON.stringify({ tradeCode: digits }),
        });

        if (!result.success) {
            showToast(result.error || t('tradeCodeSaveFailed'));
            return;
        }

        showToast(t('tradeCodeSaved'));
        await refreshProfile();
    } finally {
        setBusy(button, false);
    }
}

async function deleteTradeCode() {
    const button = $('delete-code-button');
    setBusy(button, true);
    try {
        const result = await api('/api/trade/code/delete', { method: 'POST', body: '{}' });
        if (!result.success) {
            showToast(result.error || t('tradeCodeDeleteFailed'));
            return;
        }

        $('trade-code-input').value = '';
        showToast(t('tradeCodeDeleted'));
        await refreshProfile();
    } finally {
        setBusy(button, false);
    }
}

async function submitTrade(event) {
    event.preventDefault();

    const showdownSet = $('showdown-set').value.trim();
    if (!showdownSet && !state.pkmFile) {
        showToast(t('pasteShowdownOrFileFirst'));
        $('showdown-set').focus();
        return;
    }

    const button = $('submit-button');
    setBusy(button, true);
    try {
        const result = await api('/api/trade/submit', {
            method: 'POST',
            body: JSON.stringify({
                showdownSet,
                trainerName: '',
                ignoreAutoOT: $('ignore-autoot').checked,
                pkmFileName: state.pkmFile?.name || '',
                pkmFileBase64: state.pkmFile?.base64 || '',
            }),
        });

        if (!result.success) {
            showApiError(result, 'tradeQueueFailed');
            return;
        }

        showToast(t('tradeAdded'));
        state.composerOpen = false;
        renderQueue(result);
        await refreshProfile();
    } finally {
        setBusy(button, false);
    }
}

async function handlePkmFileChange(event) {
    const file = event.target.files?.[0];
    if (!file) {
        clearPkmFile();
        return;
    }

    if (file.size > 4096) {
        showToast(t('fileTooLarge'));
        clearPkmFile();
        return;
    }

    try {
        const buffer = await file.arrayBuffer();
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (const byte of bytes) {
            binary += String.fromCharCode(byte);
        }

        state.pkmFile = {
            name: file.name,
            base64: btoa(binary),
        };
        $('pkm-file-label').textContent = t('fileSelected', { name: file.name });
        $('clear-file-button').hidden = false;
    } catch {
        showToast(t('fileReadFailed'));
        clearPkmFile();
    }
}

function clearPkmFile() {
    state.pkmFile = null;
    $('pkm-file').value = '';
    $('pkm-file-label').textContent = t('pkhexHelp');
    $('clear-file-button').hidden = true;
}

async function cancelTrade() {
    const result = await api('/api/trade/cancel', { method: 'POST', body: '{}' });
    if (!result.success) {
        showToast(result.error || t('cancelFailed'));
        return;
    }

    showToast(result.message || t('tradeCancelled'));
    await pollQueue();
}

async function pollQueue() {
    if (!state.authenticated) return;
    const result = await api('/api/trade/queue');
    renderQueue(result);
    await maybeRefreshProfileAfterCompletedTrade(result.activeTrade);
}

function renderQueue(result) {
    if (!result.success) {
        $('queue-state').textContent = t('unavailable');
        return;
    }

    $('queue-state').textContent = result.queueOpen ? t('open') : t('closed');
    const trade = result.activeTrade;
    renderPrimaryTradePanel(trade);

    $('empty-trade').hidden = Boolean(trade);
    $('active-trade').hidden = !trade;
    const stateKey = getEffectiveTradeState(trade);
    $('cancel-button').hidden = !trade || ['finished', 'cancelled'].includes(stateKey);

    renderConsole(trade);

    if (!trade) return;

    applyStateBadge($('trade-state'), stateKey);
    $('trade-pokemon').textContent = cleanPokemonName(trade.pokemon) || 'Pokemon';
    const image = $('trade-pokemon-image');
    if (trade.spriteUrl) {
        image.src = trade.spriteUrl;
        image.hidden = false;
    } else {
        image.hidden = true;
    }
    $('trade-code').textContent = formatCode(trade.code);
    $('trade-message').textContent = '';
    $('trade-message').hidden = true;

    const inQueue = Boolean(trade.inQueue) && stateKey === 'queued';
    const position = trade.position > 0 ? trade.position : 1;
    const total = trade.total > 0 ? trade.total : position;
    const pct = getTradeProgressPercent(trade, stateKey, inQueue, position, total);
    $('queue-progress').style.width = `${pct}%`;
    $('trade-position').textContent = getTradeProgressText(trade, stateKey, inQueue, position, total);
}

function renderPrimaryTradePanel(trade) {
    const stateKey = trade ? getEffectiveTradeState(trade) : null;
    const done = ['finished', 'cancelled'].includes(stateKey);
    const showTerminal = state.authenticated && trade && (!done || !state.composerOpen);
    const showComposer = state.authenticated && (!trade || state.composerOpen);

    $('login-panel').hidden = state.authenticated;
    $('trade-panel').hidden = !showComposer;
    $('trade-terminal').hidden = !showTerminal;
    $('request-title').textContent = showTerminal ? t('terminalTitle') : t('requestTitle');

    if (!showTerminal) return;

    applyStateBadge($('terminal-state'), stateKey);
    $('terminal-pokemon').textContent = cleanPokemonName(trade.pokemon) || 'Pokemon';
    $('terminal-code').textContent = formatCode(trade.code) || '0000 0000';
    $('new-trade-button').hidden = !done;
    renderTerminal(trade);
    maybeCelebrateTrade(trade, stateKey);
}

function applyStateBadge(element, stateKey) {
    element.textContent = t(stateKey);
    element.className = `state-badge state-${stateKey}`;
}

async function maybeRefreshProfileAfterCompletedTrade(trade) {
    if (!trade) return;

    const stateKey = getEffectiveTradeState(trade);
    if (stateKey !== 'finished') return;

    const tradeKey = getTradeKey(trade);
    if (!tradeKey || state.profileRefreshTradeKey === tradeKey) return;

    state.profileRefreshTradeKey = tradeKey;
    await refreshProfile();
    scheduleProfileRefreshRetry(tradeKey, 1500);
    scheduleProfileRefreshRetry(tradeKey, 4000);
}

function scheduleProfileRefreshRetry(tradeKey, delayMs) {
    window.setTimeout(async () => {
        if (!state.authenticated || state.profileRefreshTradeKey !== tradeKey) return;
        await refreshProfile();
    }, delayMs);
}

function getTradeKey(trade) {
    return `${trade?.uniqueTradeId || trade?.UniqueTradeId || cleanPokemonName(trade?.pokemon) || ''}:${trade?.code || ''}`;
}

function normalizeTradeState(value) {
    const text = String(value || 'queued').toLowerCase();
    if (text.includes('partner') || text.includes('found')) return 'partner';
    if (text.includes('search')) return 'searching';
    if (text.includes('initial') || text.includes('prepar')) return 'initializing';
    if (text.includes('process')) return 'processing';
    if (text.includes('finish') || text.includes('complete')) return 'finished';
    if (text.includes('cancel')) return 'cancelled';
    return 'queued';
}

function getEffectiveTradeState(trade) {
    if (!trade) return 'queued';
    const raw = normalizeTradeState(trade.state);
    if (trade.inQueue && ['queued', 'processing'].includes(raw) && !hasLiveBotUpdate(trade)) {
        return 'queued';
    }
    return raw;
}

function hasLiveBotUpdate(trade) {
    const raw = normalizeTradeState(trade?.state);
    const message = String(trade?.message || '');
    return ['initializing', 'searching', 'partner'].includes(raw)
        || isTrainerFoundMessage(message)
        || /loading trade menu|initializing trade|searching with link code|bot trainer/i.test(message);
}

function localizeTradeMessage(message, trade) {
    const stateKey = getEffectiveTradeState(trade);
    const key = getConsoleMessageKey(stateKey);
    if (stateKey === 'cancelled') {
        return t('cancelledWithReason', { reason: cleanTerminalReason(message, stateKey) });
    }
    if (!isSpanish() && message && !looksLikeInternalKey(message)) {
        return cleanStatusMessage(message);
    }
    return t(key, { code: formatCode(trade?.code) });
}

function cleanTerminalReason(message, stateKey) {
    const fallback = stateKey === 'cancelled' ? t('terminalFallbackCancel') : t(getConsoleMessageKey(stateKey));
    const text = String(message || fallback || '').trim();
    if (!text) return fallback;

    const cleaned = text
        .replace(/^[-\s]*(trade\s*)?(cancelled|canceled|complete|completed|finished)[:.\s-]*/i, '')
        .trim();

    return humanizeRawReason(cleanStatusMessage(cleaned || fallback));
}

function getConsoleMessageKey(stateKey) {
    return {
        queued: 'queuedConsole',
        initializing: 'initializingConsole',
        searching: 'searchConsole',
        partner: 'partnerConsole',
        processing: 'processingConsole',
        finished: 'finishedConsole',
        cancelled: 'cancelledConsole',
    }[stateKey] || 'waitingLatest';
}

function looksLikeInternalKey(message) {
    return /^[a-z]+Console$/i.test(String(message || '').trim());
}

function cleanStatusMessage(message) {
    return String(message || '').replace(/[*_`▲▼]/g, '').replace(/\s+/g, ' ').trim();
}

function humanizeRawReason(reason) {
    const text = String(reason || '').trim();
    const known = {
        TrainerTooSlow: isSpanish() ? 'El compañero de intercambio fue demasiado lento.' : 'The trade partner was too slow.',
        NoTrainerFound: isSpanish() ? 'No se encontro ningun entrenador.' : 'No trainer was found.',
        TrainerLeft: isSpanish() ? 'El entrenador salio del intercambio.' : 'The trainer left the trade.',
        TrainerOfferCanceledQuick: isSpanish() ? 'El entrenador cancelo la oferta demasiado rapido.' : 'The trainer cancelled the offer too quickly.',
        TrainerRequestBad: isSpanish() ? 'La solicitud del entrenador no fue valida.' : 'The trainer request was not valid.',
        IllegalTrade: isSpanish() ? 'El intercambio no fue permitido por el juego.' : 'The trade was not allowed by the game.',
        SuspiciousActivity: isSpanish() ? 'Se detecto actividad sospechosa.' : 'Suspicious activity was detected.',
        UserCanceled: isSpanish() ? 'El usuario cancelo el trade.' : 'The user cancelled the trade.',
        RoutineCancel: isSpanish() ? 'La rutina fue cancelada.' : 'The routine was cancelled.',
        ExceptionConnection: isSpanish() ? 'Hubo un problema de conexion.' : 'There was a connection problem.',
        ExceptionInternal: isSpanish() ? 'Hubo un error interno.' : 'There was an internal error.',
        RecoverStart: isSpanish() ? 'El bot tuvo que recuperarse al iniciar.' : 'The bot had to recover while starting.',
        RecoverPostLinkCode: isSpanish() ? 'El bot tuvo que recuperarse despues del codigo.' : 'The bot had to recover after entering the link code.',
        RecoverOpenBox: isSpanish() ? 'El bot tuvo que recuperarse al abrir la caja.' : 'The bot had to recover while opening the box.',
        RecoverReturnOverworld: isSpanish() ? 'El bot tuvo que volver al mapa para recuperarse.' : 'The bot had to return to the overworld to recover.',
        RecoverEnterUnionRoom: isSpanish() ? 'El bot tuvo que recuperarse al entrar al Union Room.' : 'The bot had to recover while entering the Union Room.',
        TradeEvolveNotAllowed: isSpanish() ? 'No puedes intercambiar un Pokemon que va a evolucionar.' : 'Pokemon that will evolve cannot be traded.',
    };

    if (known[text]) return known[text];
    return text.replace(/([a-z])([A-Z])/g, '$1 $2');
}

function getTradeProgressPercent(trade, stateKey, inQueue, position, total) {
    if (inQueue) {
        return Math.max(10, Math.min(28, (1 - ((position - 1) / total)) * 28));
    }

    return {
        queued: 18,
        initializing: 36,
        searching: 54,
        partner: 72,
        processing: 88,
        finished: 100,
        cancelled: 100,
    }[stateKey] || 18;
}

function getTradeProgressText(trade, stateKey, inQueue, position, total) {
    if (inQueue) {
        return t('positionLine', { position, total });
    }

    return t(getConsoleMessageKey(stateKey), { code: formatCode(trade?.code) });
}

function cleanTerminalValue(value) {
    const text = String(value || '').replace(/[*_`▲▼]/g, '').trim();
    return text || '-';
}

function extractTrainerFound(message) {
    const text = String(message || '');
    const trainerMatch = text.match(/Entrenador(?:\s+[\p{L}\w]+){0,4}\s+encontrado:\s*\**([^*\n.]+)\**/iu)
        || text.match(/Link trade trainer found:\s*([^,\n.]+)/i)
        || text.match(/Trainer found:\s*([^,\n.]+)/i);
    const tidMatch = text.match(/\bTID(?:7)?\**\s*:?\s*_*([0-9]+)_*/i);
    const sidMatch = text.match(/\bSID(?:7)?\**\s*:?\s*_*([0-9]+)_*/i);
    return {
        name: cleanTerminalValue(trainerMatch?.[1]),
        tid: cleanTerminalValue(tidMatch?.[1]),
        sid: cleanTerminalValue(sidMatch?.[1]),
    };
}

function isTrainerFoundMessage(message) {
    return extractTrainerFound(message).name !== '-';
}

function localizeRuntimeInstruction(message) {
    const text = String(message || '').trim();
    if (!text || isTrainerFoundMessage(text)) return '';
    return text.replace(/[*_`▲▼]/g, '').replace(/\s+/g, ' ').trim();
}

function renderConsole(trade) {
    const led = $('console-led');
    const stateKey = trade ? getEffectiveTradeState(trade) : null;
    led.classList.toggle('live', Boolean(trade) && !['finished', 'cancelled'].includes(stateKey));
}

function renderSiteGuide() {
    const list = $('site-guide-list');
    if (!list) return;

    const steps = getWebsiteGuideSteps();

    list.innerHTML = steps.map((step, index) => {
        return `<li data-step="${index + 1}">${t(step.key)}</li>`;
    }).join('');
}

function getWebsiteGuideSteps() {
    return [
        { key: 'guideLogin' },
        { key: 'guideCreate' },
        { key: 'guideQueue' },
        { key: 'guideTerminal' },
        { key: 'guideProfile' },
    ];
}

function renderTerminal(trade) {
    const list = $('terminal-list');
    const tradeKey = getTradeKey(trade);
    if (tradeKey !== state.terminalTradeKey) {
        state.terminalTradeKey = tradeKey;
        state.terminalSignature = '';
        state.terminalTypingRun++;
        list.innerHTML = '';
        state.terminalTypingQueue = Promise.resolve();
    }

    const lines = getTerminalLines(trade);
    const signature = JSON.stringify(lines);

    if (signature === state.terminalSignature && list.children.length) {
        return;
    }

    state.terminalSignature = signature;
    syncTerminalLines(lines);
}

function maybeCelebrateTrade(trade, stateKey) {
    if (stateKey !== 'finished') return;

    const tradeKey = getTradeKey(trade);
    if (!tradeKey || state.confettiTradeKey === tradeKey) return;

    state.confettiTradeKey = tradeKey;
    launchTerminalConfetti();
}

function launchTerminalConfetti() {
    const terminal = $('trade-terminal');
    if (!terminal) return;

    const burst = document.createElement('div');
    burst.className = 'terminal-confetti';
    burst.setAttribute('aria-hidden', 'true');

    const colors = ['#58bcff', '#69f5d4', '#ff4d7d', '#9b7bff', '#ffd166', '#f8fbff'];
    const pieces = 54;
    for (let i = 0; i < pieces; i++) {
        const piece = document.createElement('span');
        const angle = -118 + Math.random() * 96;
        const distance = 120 + Math.random() * 250;
        const drift = -80 + Math.random() * 160;
        const size = 6 + Math.random() * 7;
        const duration = 900 + Math.random() * 900;
        const delay = Math.random() * 160;

        piece.style.setProperty('--x', `${Math.cos(angle * Math.PI / 180) * distance + drift}px`);
        piece.style.setProperty('--y', `${Math.sin(angle * Math.PI / 180) * distance - 40}px`);
        piece.style.setProperty('--r', `${Math.random() * 720 - 360}deg`);
        piece.style.setProperty('--s', `${size}px`);
        piece.style.setProperty('--d', `${duration}ms`);
        piece.style.setProperty('--delay', `${delay}ms`);
        piece.style.background = colors[i % colors.length];
        piece.style.left = `${42 + Math.random() * 18}%`;
        piece.style.top = `${36 + Math.random() * 14}%`;
        burst.append(piece);
    }

    terminal.append(burst);
    setTimeout(() => burst.remove(), 2100);
}

function getTerminalLines(trade) {
    const stateKey = getEffectiveTradeState(trade);
    const pokemon = cleanPokemonName(trade?.pokemon) || 'Pokemon';
    const code = formatCode(trade?.code) || '0000 0000';
    const inQueue = Boolean(trade?.inQueue);
    const position = trade?.position > 0 ? trade.position : 1;
    const total = trade?.total > 0 ? trade.total : position;
    const trainer = cleanTerminalValue(trade?.partnerTrainerName || trade?.trainerName || extractTrainerFound(trade?.message).name || '-');
    const tid = cleanTerminalValue(trade?.partnerTid || extractTrainerFound(trade?.message).tid || '-');
    const sid = cleanTerminalValue(trade?.partnerSid || extractTrainerFound(trade?.message).sid || '-');
    const lines = [
        { id: 'connect', text: t('terminalConnect'), kind: 'system' },
        { id: 'queue', text: t('terminalQueue', { pokemon, code }), kind: 'ok' },
    ];

    if (stateKey === 'queued') {
        lines.push({ id: 'position', text: t('terminalQueuePosition', { position, total }), kind: stateKey === 'queued' ? 'active' : 'ok' });
    }

    if (['initializing', 'searching', 'partner', 'processing', 'finished'].includes(stateKey)) {
        lines.push({ id: 'prepare', text: t('terminalPrepare'), kind: stateKey === 'initializing' ? 'active' : 'system' });
    }

    if (['searching', 'partner', 'processing', 'finished'].includes(stateKey)) {
        lines.push({ id: 'search', text: t('terminalSearch', { code }), kind: stateKey === 'searching' ? 'active' : 'ok', important: true });
    }

    if (['partner', 'processing', 'finished'].includes(stateKey)) {
        lines.push({ id: 'found', text: t('terminalFound', { trainer, tid, sid }), kind: stateKey === 'partner' ? 'active' : 'ok', important: true });
    }

    if (stateKey === 'processing') {
        lines.push({ id: 'offer', text: localizeRuntimeInstruction(trade?.message) || t('terminalOffer'), kind: 'active' });
    }

    if (stateKey === 'finished') {
        lines.push({ id: 'finished', text: t('terminalComplete', { pokemon }), kind: 'ok' });
    } else if (stateKey === 'cancelled') {
        lines.push({ id: 'cancelled', text: t('terminalCancel', { reason: cleanTerminalReason(trade?.message, stateKey) }), kind: 'error' });
    } else if (!['queued', 'initializing', 'searching', 'partner', 'processing'].includes(stateKey)) {
        lines.push({ id: 'wait', text: t('terminalWait'), kind: 'muted' });
    }

    return lines;
}

function syncTerminalLines(lines) {
    const list = $('terminal-list');
    const current = new Map([...list.children].map((item) => [item.dataset.lineId, item]));
    const activeIds = new Set(lines.map((line) => line.id));

    for (const item of [...list.children]) {
        if (!activeIds.has(item.dataset.lineId)) {
            item.remove();
        }
    }

    for (const line of lines) {
        const existing = current.get(line.id);
        if (existing) {
            existing.className = `terminal-line ${line.kind || 'system'}${line.important ? ' important' : ''}`;
            const text = existing.querySelector('.terminal-text');
            if (text && text.textContent !== line.text && !text.classList.contains('typing')) {
                text.textContent = line.text;
            }
            list.append(existing);
            continue;
        }

        const item = document.createElement('li');
        item.className = `terminal-line ${line.kind || 'system'}${line.important ? ' important' : ''}`;
        item.dataset.lineId = line.id;
        const text = document.createElement('span');
        text.className = 'terminal-text';
        item.append(text);
        list.append(item);
        queueTerminalTyping(text, line.text);
    }
}

function queueTerminalTyping(node, text) {
    const run = state.terminalTypingRun;
    state.terminalTypingQueue = state.terminalTypingQueue.then(async () => {
        if (run !== state.terminalTypingRun) return;
        node.classList.add('typing');
        await typeText(node, text, run);
        node.classList.remove('typing');
        const screen = $('terminal-list').closest('.terminal-screen');
        screen.scrollTop = screen.scrollHeight;
        await delay(90);
    });
}

async function typeText(node, text, run) {
    node.textContent = '';
    const speed = text.length > 90 ? 8 : 14;
    for (const char of text) {
        if (run !== state.terminalTypingRun) return;
        node.textContent += char;
        await delay(char === '.' || char === ':' ? speed * 4 : speed);
    }
}

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

function startPolling() {
    stopPolling();
    pollQueue();
    state.polling = setInterval(pollQueue, 5000);
}

function stopPolling() {
    if (state.polling) {
        clearInterval(state.polling);
        state.polling = null;
    }
}

function toggleGuide(forceOpen = null) {
    const popover = $('guide-popover');
    const button = $('guide-button');
    const open = forceOpen ?? popover.hidden;
    popover.hidden = !open;
    button.setAttribute('aria-expanded', String(open));
}

function toggleTrainerPopover(forceOpen = null) {
    const popover = $('trainer-popover');
    const button = $('profile-trainer');
    const open = forceOpen ?? popover.hidden;
    popover.hidden = !open;
    button.setAttribute('aria-expanded', String(open));
    button.closest('.trainer-row')?.classList.toggle('popover-open', open);
}

function toggleCodePopover(forceOpen = null) {
    const popover = $('code-editor');
    const button = $('profile-code');
    const open = forceOpen ?? popover.hidden;
    popover.hidden = !open;
    button.setAttribute('aria-expanded', String(open));
    button.closest('.trade-code-row')?.classList.toggle('popover-open', open);
    if (open) {
        window.setTimeout(() => $('trade-code-input').focus(), 0);
    }
}

window.addEventListener('message', async (event) => {
    if (event.origin !== window.location.origin || event.data?.type !== 'tradedex-auth') {
        return;
    }

    if (event.data.success) {
        showToast(t('discordLoginComplete'));
        await initAuth();
    } else {
        showToast(t('discordLoginFailed'));
    }
});

document.addEventListener('DOMContentLoaded', async () => {
    await initLocalization();
    $('login-button').addEventListener('click', login);
    $('logout-button').addEventListener('click', logout);
    $('trade-form').addEventListener('submit', submitTrade);
    $('cancel-button').addEventListener('click', cancelTrade);
    $('pkm-file').addEventListener('change', handlePkmFileChange);
    $('trade-code-input').addEventListener('input', (event) => {
        event.target.value = formatCodeInput(event.target.value);
    });
    $('save-code-button').addEventListener('click', saveTradeCode);
    $('delete-code-button').addEventListener('click', deleteTradeCode);
    $('profile-code').addEventListener('click', (event) => {
        event.stopPropagation();
        if (!$('profile-code').disabled) {
            toggleTrainerPopover(false);
            toggleCodePopover();
        }
    });
    $('profile-trainer').addEventListener('click', (event) => {
        event.stopPropagation();
        if (!$('profile-trainer').disabled) {
            toggleCodePopover(false);
            toggleTrainerPopover();
        }
    });
    $('new-trade-button').addEventListener('click', () => {
        state.composerOpen = true;
        clearPkmFile();
        $('showdown-set').focus();
        pollQueue();
    });
    $('guide-button').addEventListener('click', () => toggleGuide());
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            toggleGuide(false);
            toggleTrainerPopover(false);
            toggleCodePopover(false);
        }
    });
    document.addEventListener('click', (event) => {
        const widget = $('guide-widget');
        if (!widget.contains(event.target)) {
            toggleGuide(false);
        }
        const trainerPopover = $('trainer-popover');
        const trainerButton = $('profile-trainer');
        if (!trainerPopover.contains(event.target) && event.target !== trainerButton) {
            toggleTrainerPopover(false);
        }
        const codePopover = $('code-editor');
        const codeButton = $('profile-code');
        if (!codePopover.contains(event.target) && event.target !== codeButton) {
            toggleCodePopover(false);
        }
    });
    $('clear-file-button').addEventListener('click', (event) => {
        event.preventDefault();
        event.stopPropagation();
        clearPkmFile();
    });
    await initAuth();
});
