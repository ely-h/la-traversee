# La Traversée

La Traversée est un jeu massivement multijoueur colocalisé de type "Party Game". Il s'agit d'une adaptation du célèbre jeu de la cour de récréation, l'Épervier, transposé dans un univers post-apocalyptique en 2D.

L'expérience est conçue pour être jouée en groupe dans une même pièce : le jeu principal tourne sur un écran géant (comme un vidéoprojecteur), et chaque joueur utilise son smartphone comme manette via son navigateur web. Un simple scan de QR code permet de rejoindre la partie instantanément, sans aucune application à installer.

## Concept du jeu

* 
**Deux équipes :** Les Survivants doivent traverser l'arène pour atteindre un bunker sécurisé, tandis que les Infectés tentent de les intercepter.


* 
**Zéro temps mort (Non-élimination) :** Un Survivant touché se transforme instantanément en Infecté et continue la partie pour chasser ses anciens alliés.


* 
**Parties rapides :** Le jeu se déroule en deux manches intenses de 1 minute 30. Les Infectés gagnent s'ils contaminent au moins 70% des joueurs.


* **Mécaniques clés :**
* 
**Dash :** Une capacité d'accélération (vitesse x2) pour esquiver ou attaquer, avec un temps de recharge de 3 secondes.


* **Zones de quarantaine :** Des zones de protection temporaires qui forcent le mouvement. Elles disparaissent après 5 secondes : tout joueur restant à l'intérieur est automatiquement infecté (anti-camping).





## Stack Technique & Architecture

Le projet repose sur une architecture distribuée de type Client-Serveur asymétrique.

* 
**Moteur de jeu (Client Lourd) :** Développé sur **Unity 2022 LTS (C#)**. Il gère la boucle de jeu principale, la physique (Box Colliders, Circle Colliders 2D) et le rendu visuel affiché sur l'écran principal.


* 
**Serveur de Communication :** Un serveur **Node.js** agissant comme un "broker" de messages. Il est conçu pour gérer un très grand nombre de connexions simultanées.


* 
**Réseau / Temps réel :** L'envoi des commandes (joystick virtuel, bouton d'action) se fait via le protocole WebSocket avec la librairie **Socket.IO**. Cela garantit une communication bidirectionnelle persistante avec une latence quasi nulle.


* 
**Clients Web (Manettes) :** Interfaces développées en **HTML5, CSS3 et JS**, accessibles directement depuis les navigateurs mobiles (Chrome, Safari, Firefox) pour garantir une compatibilité cross-platform absolue. Un système de retour haptique (vibrations) et des changements de couleurs dynamiques informent le joueur de son statut (ex: infection).

