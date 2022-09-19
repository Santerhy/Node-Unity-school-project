const io = require('socket.io')(8000, {
    pingInterval: 30000,
    pingTimeout: 5000,
    upgradeTimeout: 3000,
    allowUpgrades: true,
    cookie: false,
    serverClient: true,
    allowEI03: false,
    cors: {
        origin: "*"
    }
})

var tileStatus = [];
var players = [];
var player = {};
var currentPlayer = 0;
var nextPlayer;
var playerScores = [0,0,0,0];
var spawnPointsIndexes = [0, 14, 135, 149];
var rounds = 15;
//teams: 0 = red, 1 = blue, 2 = yellow, 3 = green

console.log("käynnistetään socket.io");

io.on('connection', (socket) => {
    console.log(new Date().toUTCString() + 'Unity yhdistää ja socket ID on ' + socket.id);

    socket.on("CREATEPLAYER", (data) => {
        players.forEach((item) => {
            socket.emit('INSTANCEOTHERS', JSON.stringify(item));
        });

        player = {
            socketId: socket.id,
            spawnIndex: spawnPointsIndexes[players.length],
            team: players.length,
            round: rounds,
        };
        
        players.push(player);
        console.log("Pelaajia: " + players.length);

        if (players.length == 1) {
            io.to(players[0].socketId).emit("INSTANTIATEFIELD");
            io.to(players[0].socketId).emit("STARTTURN", JSON.stringify(rounds));
        }
            
        io.emit('INSTANCEPLAYER', JSON.stringify(player));

        if (players.length != 1)
            io.emit("UPDATETILESFROMSERVER", JSON.stringify(tileStatus))

    })

    socket.on("MOVE", (data) => {
        console.log("Pelaaja liikkuu");
        io.emit("MOVEPLAYER", JSON.stringify(data));
    })

    socket.on("TURNENDED", (data) => {
        if (currentPlayer == players.length - 1)
            nextPlayer = 0;
        else
            nextPlayer = currentPlayer + 1;

        if (nextPlayer == 0) {
            rounds--;
            io.emit("UPDATEROUNDS", JSON.stringify(rounds))
        }

        io.emit("UPDATETILESFROMSERVER", JSON.stringify(tileStatus))

        if (rounds == 0)
            io.emit("GAMEOVER", JSON.stringify(playerScores));
        else {
        console.log("Seuraavan pelaajan vuoro alkaa " + nextPlayer)

        io.to(players[nextPlayer].socketId).emit("STARTTURN", JSON.stringify(rounds));
        currentPlayer = nextPlayer;
        console.log("current player " + currentPlayer);
        io.emit('CHANGETURNTEXT', (currentPlayer));
        }
    })

    socket.on('PLAYERENDTURN', (data) => {
        console.log("Vuoro lopetettu");
        io.to(players[currentPlayer].socketId).emit("ENDTURN", JSON.stringify(players[currentPlayer]));
    })

    socket.on("UPDATEFIELDTOLIST", (data) => {
        console.log("Päivitetään pisteet ja pelikenttä serverille")
        var tiles = JSON.parse(data)
        tileStatus.length = 0;
        ClearPlayerScores();
        for (var i = 0; i < tiles.length; i++) {
            tileStatus.push(tiles[i]);

            if (tileStatus[i] == 0)
                playerScores[0] = playerScores[0] + 1;
            else if (tileStatus[i] == 1)
                playerScores[1] = playerScores[1] + 1;
            else if (tileStatus[i] == 2)
                playerScores[2] = playerScores[2] + 1;
            else if (tileStatus[i] == 3)
                playerScores[3] = playerScores[3] + 1;
                
            if (i > 145) {
                console.log(i + " tilestatus: " + tileStatus[i] + " nodeData: " + tiles[i])
            }
        }
        console.log("==Pisteet==");
        for (var i = 0; i <players.length; i++) {
            console.log("Pelaaja " + (i+1) + ": " + playerScores[i])
        }
        console.log("Päivitetään pisteet ja pelikenttä pelaajille")
        io.emit('UPDATETILESFROMSERVER', JSON.stringify(tileStatus));
        io.emit('UPDATESCORES', JSON.stringify(playerScores));
    })
})

function ClearPlayerScores() {
    playerScores[0] = 0;
    playerScores[1] = 0;
    playerScores[2] = 0;
    playerScores[3] = 0;
}