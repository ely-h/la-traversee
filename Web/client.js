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

let selectedColor = '#ff5757';
let joinedPlayer = null;
let joystick = null;
let isInGame = false;
let isCountingDown = false;

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

    joystick.on('move', (evt, data) => {
        socket.emit('playerMove', {
            x: data.vector.x,
            y: data.vector.y * -1
        });
    });

    joystick.on('end', () => {
        socket.emit('playerMove', { x: 0, y: 0 });
    });

    joystickZone.addEventListener('touchend', () => {
        socket.emit('playerMove', { x: 0, y: 0 });
    });

    joystickZone.addEventListener('touchcancel', () => {
        socket.emit('playerMove', { x: 0, y: 0 });
    });
}

function updateWaitingRoom(payload) {
    if (!joinedPlayer) {
        return;
    }

    const playerCount = payload.players.length;
    if (!isCountingDown) {
        waitingStatus.innerText = payload.state === 'lobby'
            ? 'En attente du lancement par l hote...'
            : 'La partie est en cours.';
    }
    waitingPlayerName.innerText = `Joueur: ${joinedPlayer.pseudo}`;
    waitingCount.innerText = `${playerCount} joueur(s) connecte(s)`;
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
    waitingStatus.innerText = 'La partie a deja commence.';
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
    statusText.innerText = 'STATUT: Deconnecte';
    waitingStatus.innerText = 'Connexion perdue.';
});

socket.on('youAreInfected', () => {
    statusText.innerText = 'STATUT: INFECTE (CHASSEZ LES AUTRES!)';
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
