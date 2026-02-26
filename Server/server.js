const express = require('express');
const http = require('http');
const path = require('path');
const { Server } = require('socket.io');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(express.static(path.join(__dirname, '../Web')));

io.on('connection', (socket) => {
    console.log('Un joueur s’est connecté ! ID:', socket.id);

    socket.on('disconnect', () => {
        console.log('Joueur déconnecté:', socket.id);
    });
});

const PORT = 3000;
server.listen(PORT, '0.0.0.0', () => {
    console.log(`Serveur La Traversée lancé sur le port ${PORT}`);
    console.log(`Accessible en local sur http://localhost:${PORT}`);   
});