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

    socket.on('playerMove', (data) => {
        //toFixed(2) sert à limiter l'affichage à 2 décimales pour pas que il y ait trop de chiffres dans la console
        console.log(`[Move] Joueur ${socket.id} : x=${data.x.toFixed(2)}, y=${data.y.toFixed(2)}`);
        socket.broadcast.emit('playerMove', data);
    });

    //Réception du dash
    socket.on('playerAction', (data) => {
        if (data.type === 'DASH') {
            console.log(`[Action] Joueur ${socket.id} a déclenché un DASH`);
            socket.broadcast.emit('playerAction', data);
        }
    });
});

const PORT = 3000;
server.listen(PORT, '0.0.0.0', () => {
    console.log(`Serveur La Traversée lancé sur le port ${PORT}`);
    console.log(`Accessible en local sur http://localhost:${PORT}`);   
});
