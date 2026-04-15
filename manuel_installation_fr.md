# Manuel d'Installation : La Traversée

Ce guide vous explique comment installer et lancer "La Traversée" selon votre profil : joueur ou développeur.

---

## I. Prérequis Communs

1.  **Un PC (Hôte)** : Windows (version recommandée pour l'exécutable).
2.  **Des Smartphones (Manettes)** : Connectés au **même réseau Wi-Fi** que le PC.
3.  **Node.js** (v16 ou supérieur) : Nécessaire pour faire tourner le serveur de communication.

---

## II.a. Option A : Installation via le ZIP (Recommandé pour jouer)
*Idéal si vous voulez simplement lancer une partie rapidement sans installer d'outils de développement.*

1.  **Téléchargement** : Téléchargez le fichier `Final_La_Traversee.zip` depuis la [Release v1.0.0](https://github.com/ely-h/la-traversee/releases/tag/v1.0.0).
2.  **Extraction** : Extrayez (_Dézippez_) complètement le dossier sur votre ordinateur. 
    > [!IMPORTANT]
    > **Ne lancez pas le jeu directement depuis le ZIP**. Le serveur et les fichiers de données ne pourront pas s'exécuter correctement.
3.  **Dépendances serveur** : 
    Ouvrez un terminal dans le dossier `La Traversée_Data/StreamingAssets/Server/` du dossier extrait et lancez :
    ```bash
    npm install
    ```
4.  **Lancement** : Double-cliquez sur l'exécutable du jeu (`.exe`) à la racine du dossier extrait.
5.  **Connexion** : Une fois sur l'écran d'accueil, cliquez sur **PLAY**. Les joueurs peuvent scanner le QR code affiché pour rejoindre.

---

## II.b. Option B : Installation via le Code Source (Pour les développeurs)
*Idéal si vous souhaitez modifier le jeu ou le compiler vous-même.*

### Prérequis supplémentaires :
- **Unity 2022.3 LTS** (ou supérieur).
- **Git** (dans votre PATH).

### Étapes :
1.  **Récupérer le projet** :
    ```bash
    git clone https://github.com/ely-h/la-traversee.git
    ```
2.  **Configurer le serveur** :
    Ouvrez un terminal dans `Game/LaTraversee/Assets/StreamingAssets/Server/` et lancez :
    ```bash
    npm install
    ```
3.  **Ouvrir sous Unity** :
    Ajoutez le dossier `Game/LaTraversee` dans votre Unity Hub et ouvrez-le.
4.  **Lancement** :
    Ouvrez la scène **base** (dans `Assets/Scenes/`) et appuyez sur le **petit triangle "Play"** en haut au centre de l'écran.

---

## III. Configuration Réseau (Important)

Si les smartphones n'arrivent pas à se connecter :
1.  **Même réseau** : PC et téléphones doivent être sur le même Wi-Fi.
2.  **Pare-feu (Firewall)** : Autorisez les connexions sur le port **4242**.
3.  **Partage de connexion** : Si le Wi-Fi local bloque les échanges, un partage de connexion mobile depuis un smartphone fonctionne parfaitement.

---

## IV. FAQ / Dépannage

*   **Bruit de fond / Audio** : Au passage du titre au lobby, les musiques peuvent se superposer quelques secondes. C'est un bug connu de cette version.
*   **Serveur invisible** : En version exécutable, le serveur se lance automatiquement en arrière-plan. Vous n'avez rien à faire.
*   **Le QR Code ne s'affiche pas** : Vérifiez que la librairie `ZXing` est bien présente (pour la version code source). Le QR Code est généré localement et n'a **pas besoin d'internet**, mais il nécessite un réseau local actif pour identifier votre adresse IP.
