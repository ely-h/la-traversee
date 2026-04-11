const express = require('express');
const http = require('http');
const path = require('path');
const { Server } = require('socket.io');

const app = express();
const server = http.createServer(app);
const io = new Server(server);

app.use(express.static(path.join(__dirname, '../Web')));
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, '../Web/index.html'));
});

io.on('connection', (socket) => {
    console.log('Un joueur s’est connecté ! ID:', socket.id);

    socket.on('disconnect', () => {
        console.log('Joueur déconnecté:', socket.id);
        socket.broadcast.emit('playerDisconnect', { id: socket.id });
    });

    socket.on('playerJoin', (data) => {
        data.id = socket.id;
        console.log(`[Join] ${data.pseudo} a rejoint la partie avec la couleur ${data.color}`);
        socket.broadcast.emit('playerJoin', data);
    });

    socket.on('playerMove', (data) => {
        //toFixed(2) sert à limiter l'affichage à 2 décimales pour pas que il y ait trop de chiffres dans la console
        data.id = socket.id;
        console.log(`[Move] Joueur ${socket.id} : x=${data.x.toFixed(2)}, y=${data.y.toFixed(2)}`);
        socket.broadcast.emit('playerMove', data);
    });

    //Réception du dash
    socket.on('playerAction', (data) => {
        if (data.type === 'DASH') {
            data.id = socket.id;
            console.log(`[Action] Joueur ${socket.id} a déclenché un DASH`);
            socket.broadcast.emit('playerAction', data);
        }
    });

    //Réception du cooldown
    socket.on('dashCooldown', (data) => {
        const targetSocket = io.sockets.sockets.get(data.id);
        if (targetSocket) {
            targetSocket.emit('dashCooldown', data);
        }
    });

    //Réception de l'infection
    socket.on('playerInfected', (data) => {
        console.log(`[Infection] Le joueur ${data.id} a été touché !`); 
        //env msg au joueur infecté
        io.to(data.id).emit('youAreInfected');
    });
    
    //Réception de la fin de partie
    socket.on('gameOver', (data) => {
        console.log(`[Game Over] ${data.message}`);
        // io.emit envoie le message absolument à TOUT LE MONDE (tous les téléphones connectés)
        io.emit('gameOver', data);
    });
});

const PORT = 4242;
server.listen(PORT, '0.0.0.0', () => {
    console.log(`Serveur La Traversée lancé sur le port ${PORT}`);
    console.log(`Accessible en local sur http://localhost:${PORT}`);   
});
