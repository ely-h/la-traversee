const socket = io();

const statusText = document.getElementById('status-text');
const dashButton = document.getElementById('dash-button');
const joystickZone = document.getElementById('joystick-zone');

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