# Changelog

Toutes les modifications notables de DCS Panel Manager sont documentées dans ce fichier.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le projet
utilisera [Semantic Versioning](https://semver.org/lang/fr/).

## [Unreleased]

### Prévu

- connexion DCS-BIOS en lecture seule ;
- détection de DCS World et de l'avion actif ;
- import des métadonnées DCS-BIOS ;
- affichage des données DCS-BIOS dans Live Monitor.

## [0.1.0] - 2026-09-14

Première milestone fonctionnelle centrée sur la détection et la lecture des panneaux.

### Ajouté

- solution .NET 10 composée de modules Core, Hardware, Logitech, DCSBIOS, Profiles et App ;
- interface Avalonia avec Dashboard, Devices et Live Monitor ;
- navigation préparée pour Profiles, Mappings, DCS-BIOS et Settings ;
- détection du PZ55 avec `VID 06A3 / PID 0D67` ;
- détection du PZ70 avec `VID 06A3 / PID 0D06` ;
- identité individuelle basée sur le chemin HID et le numéro de série disponible ;
- surveillance du branchement et du débranchement à chaud ;
- lecture et affichage des rapports HID bruts ;
- décodage des switches et sélecteurs PZ55 ;
- décodage des boutons, sélecteurs et encodeurs PZ70 ;
- événements génériques de connexion, déconnexion et entrée ;
- filtres Hardware, Mapping, DCS-BIOS et Error dans Live Monitor ;
- moteur de mapping indépendant de DCS-BIOS ;
- modèles de profils JSON avec version de schéma ;
- stratégies `NoSync`, `HardwareWins` et `SimulatorWins` ;
- profil générique utilisant `NoSync` ;
- abstraction du client et des métadonnées DCS-BIOS ;
- client DCS-BIOS inactif empêchant tout envoi pendant cette milestone ;
- injection de dépendances et logs structurés ;
- tests unitaires du Core, des profils et du protocole Logitech ;
- outil `DCSPanel.Hardware.Probe` pour les diagnostics matériels.

### Validé sur matériel réel

- détection simultanée d'un PZ55 et d'un PZ70 sous Windows ;
- ouverture des deux flux HID ;
- réception et décodage des changements du PZ55 ;
- réception et décodage des états du PZ70 ;
- exécution prolongée sans erreur lorsqu'aucune commande n'est manipulée ;
- fermeture propre des flux HID.

### Corrigé

- les expirations de lecture HidSharp après trois secondes d'inactivité ne sont plus
  considérées comme des erreurs de périphérique ;
- la fermeture normale d'un flux HID n'est plus journalisée comme une erreur ;
- le front descendant d'une impulsion d'encodeur ne génère plus un second cran ;
- l'arrêt du service libère correctement les flux et tâches de lecture.

### Non implémenté

- connexion réseau à DCS-BIOS ;
- envoi de commandes vers DCS World ;
- écriture des LED du PZ55 ;
- écriture du LCD et des LED du PZ70 ;
- éditeurs graphiques de profils et mappings ;
- backend clavier et prise en charge du Stream Deck.
