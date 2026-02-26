const express = require('express');
const http = require('http');
const { Server } = require('socket.io');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

io.on('connection', (socket) => {
    console.log('Un joueur s’est connecté ! ID:', socket.id);

    socket.on('disconnect', () => {
        console.log('Joueur déconnecté');
    });
});

const PORT = 3000;
server.listen(PORT, () => {
    console.log(`Serveur de relais lancé sur http://localhost:${PORT}`);
});