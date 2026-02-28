const socket = io();

// éléments du DOM
const loginContainer = document.getElementById('login-container');
const uiContainer = document.getElementById('ui-container');
const joinButton = document.getElementById('join-button');
const pseudoInput = document.getElementById('pseudo-input');
const colorButtons = document.querySelectorAll('.color-btn');
const statusText = document.getElementById('status-text');
const dashButton = document.getElementById('dash-button');
const joystickZone = document.getElementById('joystick-zone');

let selectedColor = '#ff4757'; // Couleur par défaut

// Gestion choix de couleur
colorButtons.forEach(btn => {
    btn.addEventListener('pointerdown', (e) => {
        colorButtons.forEach(b => b.classList.remove('selected'));
        e.target.classList.add('selected');
        selectedColor = e.target.getAttribute('data-color');
    });
});

// Gestion du bouton "Rejoindre"
joinButton.addEventListener('pointerdown', () => {
    let pseudo = pseudoInput.value.trim();
    if (pseudo === "") pseudo = "Anonyme";
    // Envoi des infos du joueur au serveur
    socket.emit('playerJoin', { pseudo: pseudo, color: selectedColor });

    // Changement d'interface
    loginContainer.style.display = 'none';
    uiContainer.style.display = 'flex';
    
    // Couleur de fond = couleur choisie (pour se retrouver plus facilement dans la foule)
    document.body.style.backgroundColor = selectedColor;
});

socket.on('connect', () => {
    statusText.innerText = "STATUT: Survivant";
    console.log("Connecté au serveur!");
});

dashButton.addEventListener('pointerdown', () => {
    console.log("Dash activé");
    socket.emit('playerAction', { type: 'DASH' });
});

const joystick = nipplejs.create({
    zone: joystickZone,
    mode: 'static',
    position: { left: '50%', top: '50%' },
    color: 'white'
});

joystick.on('move', (evt, data) => {
    socket.emit('playerMove', {
        x: data.vector.x,
        y: data.vector.y * -1 // Inversion car l'axe Y web est inversé par rapport à Unity
    });
});

joystick.on('end', () => {
    socket.emit('playerMove', { x: 0, y: 0 });
});

socket.on('disconnect', () => {
    statusText.innerText = "STATUT: Déconnecté";
    console.log("Déconnecté du serveur!");
});