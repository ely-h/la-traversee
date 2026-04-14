const socket = io();

const loginContainer = document.getElementById('login-container');
const waitingContainer = document.getElementById('waiting-container');
const uiContainer = document.getElementById('ui-container');
const gameOverContainer = document.getElementById('game-over-container');
const joinButton = document.getElementById('join-button');
const pseudoInput = document.getElementById('pseudo-input');
const colorPickerInput = document.getElementById('color-picker-input');
const statusText = document.getElementById('status-text');
const dashButton = document.getElementById('dash-button');
const joystickZone = document.getElementById('joystick-zone');
const waitingStatus = document.getElementById('waiting-status');
const waitingPlayerName = document.getElementById('waiting-player-name');
const waitingCount = document.getElementById('waiting-count');
const errorMsg = document.getElementById('error-message');
const gameOverTitle = document.getElementById('game-over-title');
const winningTeamText = document.getElementById('winning-team-text');
const winnersList = document.getElementById('winners-list');
const gameOverMessage = document.getElementById('game-over-message');
const reconnectOverlay = document.getElementById('reconnect-overlay');

let selectedColor = '#ff5757';
let joinedPlayer = null;
let joystick = null;
let isInGame = false;
let isCountingDown = false;
let disconnectTimeout = null;

function showScreen(screen) {
    loginContainer.style.display = screen === 'login' ? 'flex' : 'none';
    waitingContainer.style.display = screen === 'waiting' ? 'flex' : 'none';
    uiContainer.style.display = screen === 'game' ? 'flex' : 'none';
    gameOverContainer.style.display = screen === 'gameover' ? 'flex' : 'none';
}

function ensureJoystick() {
    if (joystick) {
        return;
    }

    joystick = nipplejs.create({
        zone: joystickZone,
        mode: 'static',
        position: { left: '50%', top: '50%' },
        color: 'white'
    });

    let isThrottled = false;
    let pendingMoveEvent = null;
    const THROTTLE_MS = 80; // ~12.5 Hz (1000 / 80)

    function sendMove(x, y) {
        socket.emit('playerMove', { x, y });
    }

    function stopMove() {
        pendingMoveEvent = null; // Clear any pending queued moves
        sendMove(0, 0); // Send final move immediately to prevent floating
    }

    joystick.on('move', (evt, data) => {
        const x = data.vector.x;
        const y = data.vector.y * -1; // Invert Y axis

        if (!isThrottled) {
            sendMove(x, y);
            isThrottled = true;

            setTimeout(() => {
                isThrottled = false;
                if (pendingMoveEvent) {
                    sendMove(pendingMoveEvent.x, pendingMoveEvent.y);
                    pendingMoveEvent = null;
                }
            }, THROTTLE_MS);
        } else {
            // Keep track of the freshest move while waiting for the cooldown
            pendingMoveEvent = { x, y };
        }
    });

    joystick.on('end', stopMove);
    joystickZone.addEventListener('touchend', stopMove);
    joystickZone.addEventListener('touchcancel', stopMove);
}

function updateWaitingRoom(payload) {
    if (!joinedPlayer) {
        return;
    }

    const playerCount = payload.players.length;
    if (!isCountingDown) {
        waitingStatus.innerText = payload.state === 'lobby'
            ? 'En attente du lancement par l\'hote...'
            : 'La partie est en cours.';
    }
    waitingPlayerName.innerText = `Joueur: ${joinedPlayer.pseudo}`;
    waitingCount.innerText = `${playerCount} joueur(s) connecté(s)`;
}

function showController() {
    isInGame = true;
    isCountingDown = false;
    showScreen('game');
    ensureJoystick();
    statusText.innerText = 'STATUT: Survivant';
    document.body.style.backgroundColor = joinedPlayer ? joinedPlayer.color : selectedColor;
}

selectedColor = colorPickerInput.value; // Store currently selected native color

joinButton.addEventListener('pointerdown', () => {
    let pseudo = pseudoInput.value.trim();
    if (pseudo === '') {
        pseudo = 'Anonyme';
    }

    if (errorMsg) {
        errorMsg.style.display = 'none';
    }

    selectedColor = colorPickerInput.value;

    joinedPlayer = {
        pseudo,
        color: selectedColor
    };

    socket.emit('playerJoin', { pseudo, color: selectedColor });
    showScreen('waiting');
    document.body.style.backgroundColor = selectedColor;
    waitingStatus.innerText = 'Connexion au lobby...';
    waitingPlayerName.innerText = `Joueur: ${pseudo}`;
    waitingCount.innerText = '';
});

socket.on('connect', () => {
    console.log('Connecte au serveur');
    if (reconnectOverlay) {
        reconnectOverlay.style.display = 'none';

        // Restore default text and style for future disconnects
        const p = reconnectOverlay.querySelector('p');
        const h2 = reconnectOverlay.querySelector('h2');
        if (h2) h2.innerText = 'Connexion perdue...';
        if (p) {
            p.innerText = 'Reconnexion en cours';
            p.style.animation = '';
            p.style.color = '';
        }

        clearTimeout(disconnectTimeout);
    }
});

socket.on('connect_error', () => {
    if (reconnectOverlay) {
        reconnectOverlay.style.display = 'flex';
    }
});

socket.on('player_registered', (player) => {
    joinedPlayer = player;
    waitingPlayerName.innerText = `Joueur: ${player.pseudo}`;
});

socket.on('lobby_state', (payload) => {
    updateWaitingRoom(payload);

    if (joinedPlayer && payload.state === 'lobby' && !isInGame) {
        showScreen('waiting');
    }
});

socket.on('game_started', () => {
    showController();
});

socket.on('game_countdown', (payload) => {
    isCountingDown = true;
    showScreen('waiting');
    waitingStatus.innerText = payload.remaining > 0
        ? `La partie commence dans ${payload.remaining}...`
        : 'GO !!!';
});

socket.on('join_rejected', () => {
    showScreen('login');
    waitingStatus.innerText = 'La partie a deja commencé.';
});

socket.on('invalid_username', (data) => {
    showScreen('login');
    if (errorMsg) {
        errorMsg.innerText = data.message || "Pseudo non autorisé.";
        errorMsg.style.display = 'block';
    }
    document.body.style.backgroundColor = 'var(--marron-gris)'; // Reset background
});

dashButton.addEventListener('pointerdown', () => {
    if (dashButton.disabled) {
        return;
    }

    socket.emit('playerAction', { type: 'DASH' });

    let secondesRestantes = 5;
    dashButton.disabled = true;
    dashButton.textContent = `${secondesRestantes}s...`;

    const interval = setInterval(() => {
        secondesRestantes--;
        if (secondesRestantes <= 0) {
            clearInterval(interval);
            dashButton.disabled = false;
            dashButton.textContent = 'Dash';
        } else {
            dashButton.textContent = `${secondesRestantes}s...`;
        }
    }, 1000);
});

socket.on('disconnect', () => {
    statusText.innerText = 'STATUT: Déconnecté';
    waitingStatus.innerText = 'Connexion perdue.';
    if (reconnectOverlay) {
        reconnectOverlay.style.display = 'flex';

        clearTimeout(disconnectTimeout);
        disconnectTimeout = setTimeout(() => {
            const h2 = reconnectOverlay.querySelector('h2');
            const p = reconnectOverlay.querySelector('p');

            if (h2) h2.innerText = 'Erreur Critique';
            if (p) {
                p.innerText = 'Fin de partie ou erreur de connexion.';
                p.style.animation = 'none';
                p.style.color = '#ff5757';
            }

            socket.disconnect(); // Prevent infinite reconnection loop if it's considered definitive
        }, 30000);
    }
});

socket.on('youAreInfected', () => {
    statusText.innerText = 'STATUT: INFECTÉ (CHASSEZ LES AUTRES!)';
    document.body.style.backgroundColor = '#4f6920';

    if ('vibrate' in navigator) {
        navigator.vibrate(300);
    }
});

socket.on('youAreSafe', () => {
    statusText.innerText = "STATUT: A L'ABRI !";
    document.body.style.backgroundColor = 'var(--menthe)';
});

socket.on('gameOver', (data) => {
    socket.emit('playerMove', { x: 0, y: 0 });
    showScreen('gameover');

    document.body.style.backgroundColor = 'var(--marron-gris)'; // Default nice background

    if (data.winningTeam) {
        winningTeamText.innerText = `Les ${data.winningTeam} ont gagné !`;
        if (data.winningTeam === 'Survivants') {
            document.body.style.backgroundColor = 'var(--menthe)';
        } else if (data.winningTeam === 'Infectés') {
            document.body.style.backgroundColor = '#4f6920';
        }
    } else {
        winningTeamText.innerText = "Partie Terminée";
    }

    winnersList.innerHTML = '';
    if (data.winners && data.winners.length > 0) {
        data.winners.forEach(pseudo => {
            const li = document.createElement('li');
            li.innerText = pseudo;
            winnersList.appendChild(li);
        });
    } else {
        const li = document.createElement('li');
        li.innerText = "Aucun survivant (ou information indisponible).";
        winnersList.appendChild(li);
    }

    gameOverMessage.innerText = data.message;
});

socket.on('resetUI', () => {
    statusText.innerText = 'STATUT: Survivant';
    dashButton.disabled = false;
    dashButton.textContent = 'Dash';
    showScreen('game');
    document.body.style.backgroundColor = joinedPlayer ? joinedPlayer.color : selectedColor;
});

// Play Again: Server tells all clients the game restarted, go back to lobby/waiting
socket.on('game_restarted', () => {
    isInGame = false;
    isCountingDown = false;
    dashButton.disabled = false;
    dashButton.textContent = 'Dash';
    document.body.style.backgroundColor = joinedPlayer ? joinedPlayer.color : selectedColor;

    if (joinedPlayer) {
        showScreen('waiting');
        waitingStatus.innerText = 'En attente du lancement par l\'hote...';
        waitingPlayerName.innerText = `Joueur: ${joinedPlayer.pseudo}`;
    } else {
        showScreen('login');
    }
});

// iOS Safari Fix: Prevent native scroll/rubber-banding to ensure NippleJS gets exclusive touch processing
uiContainer.addEventListener('touchmove', function (e) {
    e.preventDefault();
}, { passive: false });

