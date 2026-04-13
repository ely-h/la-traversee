const express = require('express');
const http = require('http');
const path = require('path');
const { Server } = require('socket.io');

const app = express();
const server = http.createServer(app);
const io = new Server(server);
const PORT = 4242;

let isShuttingDown = false;
let hostSocketId = null;
let gameState = 'lobby';
const players = new Map();

app.use(express.static(path.join(__dirname, '../Web')));
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, '../Web/index.html'));
});

function getPlayersPayload() {
    return Array.from(players.values());
}

function broadcastLobbyState() {
    io.emit('lobby_state', {
        state: gameState,
        players: getPlayersPayload(),
        port: PORT
    });
}

function shutdownServer() {
    if (isShuttingDown) {
        return;
    }

    isShuttingDown = true;
    console.log('Arret propre du serveur La Traversee...');

    io.close(() => {
        server.close(() => {
            console.log('Serveur La Traversee arrete proprement.');
            process.exit(0);
        });
    });
}

process.on('SIGINT', shutdownServer);
process.on('SIGTERM', shutdownServer);
process.stdin.on('data', (data) => {
    if (data.toString().trim().toLowerCase() === 'shutdown') {
        shutdownServer();
    }
});

io.on('connection', (socket) => {
    console.log('Connexion socket:', socket.id);
    socket.emit('lobby_state', {
        state: gameState,
        players: getPlayersPayload(),
        port: PORT
    });

    socket.on('registerHost', () => {
        hostSocketId = socket.id;
        console.log(`[Host] Hote enregistre: ${socket.id}`);
        socket.emit('host_registered', { ok: true, port: PORT });
        socket.emit('lobby_state', {
            state: gameState,
            players: getPlayersPayload(),
            port: PORT
        });
    });

    socket.on('playerJoin', (data = {}) => {
        if (gameState !== 'lobby') {
            socket.emit('join_rejected', { reason: 'game_already_started' });
            return;
        }

        const pseudo = (data.pseudo || 'Anonyme').trim().slice(0, 16) || 'Anonyme';
        const color = data.color || '#ff5757';
        const player = {
            id: socket.id,
            pseudo,
            color
        };

        players.set(socket.id, player);
        console.log(`[Join] ${pseudo} a rejoint le lobby avec la couleur ${color}`);

        socket.emit('player_registered', player);
        socket.broadcast.emit('playerJoin', player);

        if (hostSocketId) {
            io.to(hostSocketId).emit('player_joined', {
                state: gameState,
                players: getPlayersPayload(),
                port: PORT
            });
        }

        broadcastLobbyState();
    });

    socket.on('start_game', () => {
        if (socket.id !== hostSocketId) {
            socket.emit('start_game_denied', { reason: 'host_only' });
            return;
        }

        if (gameState !== 'lobby') {
            return;
        }

        gameState = 'in_game';
        console.log(`[Lobby] Transition vers l'arene avec ${players.size} joueur(s).`);
        io.emit('game_started', {
            players: getPlayersPayload()
        });
        broadcastLobbyState();
    });

    socket.on('playerMove', (data = {}) => {
        data.id = socket.id;
        socket.broadcast.emit('playerMove', data);
    });

    socket.on('playerAction', (data = {}) => {
        if (data.type === 'DASH') {
            data.id = socket.id;
            socket.broadcast.emit('playerAction', data);
        }
    });

    socket.on('playerInfected', (data) => {
        io.to(data.id).emit('youAreInfected');
    });

    socket.on('playerSafe', (data) => {
        io.to(data.id).emit('youAreSafe');
    });

    socket.on('playerReset', (data) => {
        io.to(data.id).emit('resetUI');
    });

    socket.on('gameOver', (data) => {
        console.log(`[Game Over] ${data.message}`);
        io.emit('gameOver', data);
    });

    socket.on('disconnect', () => {
        console.log('Deconnexion socket:', socket.id);

        if (socket.id === hostSocketId) {
            hostSocketId = null;
        }

        if (players.has(socket.id)) {
            players.delete(socket.id);
            socket.broadcast.emit('playerDisconnect', { id: socket.id });
            broadcastLobbyState();
        }
    });
});

server.listen(PORT, '0.0.0.0', () => {
    console.log(`Serveur La Traversee lance sur le port ${PORT}`);
    console.log(`Accessible en local sur http://localhost:${PORT}`);
});
