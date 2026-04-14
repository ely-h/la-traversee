const express = require('express');
const http = require('http');
const path = require('path');
const { Server } = require('socket.io');

// --- Anti-Profanity Hybrid Filter Setup (Global Scope) ---
const CUSTOM_WORDS = [
    // --- English Extreme Slurs & Hate Speech ---
    ['nig', 'ger'], ['nig', 'ga'], ['fag', 'got'], ['fag', 'g'], ['re', 'tard'],
    ['dy', 'ke'], ['tran', 'ny'], ['chi', 'nk'], ['sp', 'ic'], ['ki', 'ke'], ['go', 'ok'],
    ['c', 'o', 'o', 'n'], ['p', 'a', 'k', 'i'], ['n', 'e', 'g', 'r', 'o'],
    ['s', 'h', 'e', 'm', 'a', 'l', 'e'], ['m', 'o', 'n', 'g', 'o', 'l'],
    ['t', 'a', 'r', 'd'], ['q', 'u', 'e', 'e', 'r'], ['h', 'o', 'm', 'o'], ['l', 'e', 's', 'b', 'o'],
    ['w', 'o', 'p'], ['d', 'a', 'g', 'o'], ['g', 'u', 'i', 'd', 'o'], ['b', 'e', 'a', 'n', 'e', 'r'],
    ['n', 'i', 'p'], ['k', 'r', 'a', 'u', 't'], ['g', 'o', 'y', 'i', 'm'], ['m', 'u', 't', 't'],
    ['n', 'i', 'b', 'b', 'a'], ['w', 'i', 'g', 'g', 'e', 'r'], ['c', 'r', 'a', 'c', 'k', 'e', 'r'],
    ['p', 'e', 'c', 'k', 'e', 'r', 'w', 'o', 'o', 'd'], ['z', 'i', 'p', 'p', 'e', 'r', 'h', 'e', 'a', 'd'],
    ['w', 'e', 't', 'b', 'a', 'c', 'k'], ['d', 'a', 'r', 'k', 'i', 'e'], ['t', 'a', 'r', 'b', 'a', 'b', 'y'],
    ['j', 'i', 'g', 'a', 'b', 'o', 'o'], ['m', 'u', 'z', 'z', 'i', 'e'],

    // --- English Harassment & Profanity ---
    'whore', 'slut', 'bitch', 'cunt', 'rape', 'rapist', 'pedo', 'pedophile', 'kys', 'suicide',
    'motherfucker', 'mofo', 'twat', 'dick', 'cock', 'pussy', 'wank', 'jizz', 'cum',
    'dildo', 'masturbate', 'smegma', 'bastard', 'douche', 'asshole', 'blowjob',
    'shit', 'fuck', 'fucker', 'fucking', 'motherfucking', 'arsehole', 'dumbass', 'jackass',
    'skank', 'bimbo', 'prick', 'knob', 'bellend', 'wanker', 'tosser', 'chode', 'nutsack',
    'scrotum', 'vagina', 'penis', 'tits', 'titties', 'boobs', 'booty', 'incest', 'bestiality',
    'bullshit', 'dogshit', 'horseshit', 'crap', 'fudgepacker', 'spastic', 'spaz', 'cuntface',
    'dickhead', 'shithead', 'shitbag', 'scumbag', 'dirtbag', 'kill urself', 'commit suicide',
    'slutbag', 'ho', 'hoe', 'slag', 'sket', 'minger', 'munter', 'pecker', 'schlong', 'dong',
    'butthole', 'anus', 'sphincter', 'clit', 'clitoris', 'labia', 'pegging', 'squirter',
    'fisting', 'bukkake', 'gangbang', 'threesome', 'orgy', 'bdsm', 'sadist', 'masochist',
    'asshat', 'ass clown', 'buttplug', 'buttfuck', 'cameltoe', 'cocksucker', 'coochie', 'cooter',
    'redneck', 'hillbilly', 'fuckboy', 'fuckery', 'goddamn', 'jerkoff', 'jizzbag', 'milf',
    'piss', 'piss off', 'poon', 'poontang', 'shart', 'shitbrick', 'snatch', 'taint', 'thot',
    'topless', 'turd', 'dipshit', 'tosspot', 'scroat', 'bell end', 'gobshite', 'bint', 'wankstain',
    'cuntbag', 'fucktard', 'fuckwit', 'douchebag', 'douchecanoe', 'thundercunt',
    'shite', 'arsebadger', 'knobhead', 'cockwomble', 'buttmunch', 'carpetmuncher',
    'cumdumpster', 'cumguzzler', 'jizz trumpet', 'twatwaffle', 'nymphomaniac',
    'lynch', 'cuck', 'cuckold',

    // --- French Extreme Slurs & Hate Speech ---
    ['boug', 'noule'], ['bi', 'cot'], ['nè', 'gre'], ['ne', 'gre'], ['né', 'gre'], ['bam', 'bou', 'la'],
    ['you', 'pin'], ['fe', 'uj'], ['nia', 'koue'], ['gou', 'dou'],
    ['pé', 'dé'], ['pe', 'de'], 'pd', ['ta', 'pette'], ['tar', 'louze'], ['pe', 'dale'],
    ['gou', 'ine'], ['en', 'culé'], ['en', 'cule'], ['n', 'g', 'u', 'l', 'o'],
    ['y', 'o', 'u', 't', 'r', 'e'], ['c', 'h', 't', 'o', 'n'], ['n', 'a', 'g', 'n', 'a', 'd'],
    ['b', 'o', 'u', 's', 'o', 'u', 'm', 'a', 'k'], ['g', 'n', 'o', 'u', 'l'], ['r', 'a', 't', 'o', 'n'],
    ['t', 'a', 'n', 't', 'o', 'u', 'z', 'e'], ['c', 'r', 'o', 'u', 'i', 'l', 'l', 'e'], ['n', 'g', 'o', 'k'],
    ['t', 'r', 'a', 'v', 'e', 'l', 'o'], ['c', 'l', 'a', 'n', 'd', 'o'], ['g', 'n', 'i', 'a', 'k'],
    ['m', 'a', 'c', 'a', 'k'], ['m', 'a', 'c', 'a', 'q', 'u', 'e'], ['b', 'e', 'u', 'r', 'e', 't', 't', 'e'],

    // --- French Severe Insults & Harassment ---
    'pute', 'salope', 'connard', 'connasse', 'pétasse', 'petasse', 'meurt', 'creve', 'viol', 'violeur',
    'putain', 'batard', 'bâtard', 'fdp', 'fils de pute', 'fiotte', 'nique', 'niquer', 'ntm',
    'trouduc', 'trou du cul', 'suceur', 'poufiasse', 'gueuse', 'abrutis',
    'bite', 'chatte', 'chibre', 'couille', 'foutre', 'teucha', 'tapin', 'zizi', 'zob',
    'con', 'conne', 'merde', 'merdique', 'emmerde', 'emmerdeur', 'chier', 'chiant', 'chieur',
    'bouffon', 'teube', 'trisomique', 'gogol', 'baiser', 'defoncer',
    'fellation', 'pipe', 'sucer', 'branler', 'branleur', 'salop', 'salopard', 'greluche', 'grognasse',
    'garce', 'pointeur', 'grosse vache', 'sac a merde', 'sac à merde', 'mange merde', 'mange tes morts',
    'nique ta mere', 'nique ta mère', 'nique ta race', 'fils de chien', 'fille de joie', 'catapulte a merde',
    'foune', 'burne', 'valseuses', 'kiki', 'kekette', 'quequette',
    'gland', 'prepice', 'sperme', 'jute', 'giclade', 'partouze', 'tournante', 'echangisme',
    'catin', 'pouffiasse', 'chaudasse', 'michto', 'baltringue', 'tafiole', 'lopette',
    'flicard', 'chiure', 'enflure', 'pourriture',
    'enculer', 'sodo', 'sodomie', 'fion', 'trou de balle', 'prepuce', 'bouseux', 'péquenaud',
    'foutaise', 'casses toi', 'pend toi', 'trimard', 'pouf', 'tepu', 'conasse',
    'chbeb', 'zezette', 'schneck', 'chatounette', 'cagole', 'pochtron', 'poivrot',
    'raclure', 'fiente', 'casse couilles', 'casse-couilles', 'pute borgne',
    'saligaud', 'fumier', 'crevard', 'chiennasse', 'michetonneuse', 'couillon',
    'taret', 'taré', 'facho', 'gauchiasse', 'merdiapart', 'suicidons',
    'enculeur', 'trouffion', 'biff biff', 'pompeur', 'broute minou', 'lécheur',
    'sale noir', 'sale blanc', 'sale arabe', 'sale juif',

    // --- Extremism & Historical ---
    'hitler', 'nazi', 'kkk', 'stalin', 'osama', 'laden', 'terrorist', 'isis', 'daesh', 'taliban',
    'jihad', 'djihad', 'al qaeda', 'boko haram', 'gestapo', 'furer', 'führer', 'mein kampf', 'swastika',
    'ss', 'waffen', 'holocaust', 'shoah', 'apartheid', 'goulag', 'gulag', 'neonazi', 'neo-nazi',
    'fascist', 'white power', 'wpww', '1488', 'blood and soil'
];

let hasObscenity = false;
let profanityMatcher = null;

try {
    // Try to load Plan A (NPM Package)
    const { DataSet, RegExpMatcher, englishDataset, englishRecommendedTransformers } = require('obscenity');

    const filterDataset = new DataSet().addAll(englishDataset);
    for (const item of CUSTOM_WORDS) {
        const word = Array.isArray(item) ? item.join('') : item;
        filterDataset.addPhrase(phrase => phrase.addWord(word));
    }

    profanityMatcher = new RegExpMatcher({ ...filterDataset.build(), ...englishRecommendedTransformers });
    hasObscenity = true;
    console.log('[Filter] Plan A Activated: Obscenity NPM package loaded successfully.');
} catch (err) {
    // Plan B (Fallback Native Regex)
    console.log('[Filter] Plan B Activated: Obscenity package missing. Falling back to native Regex.');
}

function containsProfanity(text) {
    if (!text) return false;
    const lowerText = text.toLowerCase();

    if (hasObscenity && profanityMatcher) {
        return profanityMatcher.hasMatch(text);
    } else {
        return CUSTOM_WORDS.some(item => {
            const word = Array.isArray(item) ? item.join('') : item;
            const regex = new RegExp(word.split('').join('[^a-z0-9]*'), 'i');
            return regex.test(lowerText);
        });
    }
}
// ---------------------------------------------------------

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

        // Profanity Check validation
        if (containsProfanity(pseudo)) {
            console.log(`[Filter] Rejected player join due to profanity in pseudo: ${pseudo}`);
            socket.emit('invalid_username', { message: "Pseudo non autorisé. Veuillez en choisir un autre." });
            return; // Stop the join process, but do NOT disconnect the socket
        }

        const color = data.color || '#ff5757';
        const player = {
            id: socket.id,
            pseudo,
            color,
            team: 'survivor'
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
        if (players.has(data.id)) players.get(data.id).team = 'infected';
        io.to(data.id).emit('youAreInfected');
    });

    socket.on('playerSafe', (data) => {
        if (players.has(data.id)) players.get(data.id).team = 'survivor';
        io.to(data.id).emit('youAreSafe');
    });

    socket.on('playerReset', (data) => {
        if (players.has(data.id)) players.get(data.id).team = 'survivor';
        io.to(data.id).emit('resetUI');
    });

    socket.on('gameOver', (data) => {
        console.log(`[Game Over] ${data.message}`);

        let winningTeam = 'inconnu';
        let winners = [];
        const messageUpper = (data.message || '').toUpperCase();
        
        if (messageUpper.includes('SURVIVANT')) {
            winningTeam = 'Survivants';
            winners = Array.from(players.values()).filter(p => p.team === 'survivor').map(p => p.pseudo);
        } else if (messageUpper.includes('ZOMBIE') || messageUpper.includes('INFECT')) {
            winningTeam = 'Infectés';
            winners = Array.from(players.values()).filter(p => p.team === 'infected').map(p => p.pseudo);
        }

        data.winningTeam = winningTeam;
        data.winners = winners;
        
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
