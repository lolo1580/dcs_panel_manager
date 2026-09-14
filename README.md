# DCS Panel Manager

Application Windows moderne permettant de détecter, surveiller et, à terme, configurer
des panneaux de simulation de vol pour DCS World.

La première version prend en charge :

- Logitech/Saitek Pro Flight Switch Panel **PZ55** ;
- Logitech/Saitek Pro Flight Multi Panel **PZ70**.

Le projet utilise C#, .NET 10 et Avalonia UI. DCS-BIOS sera l'interface principale avec
DCS World, sans réimplémenter DCS-BIOS.

> Le projet est actuellement en phase initiale. La lecture HID fonctionne, mais aucune
> commande n'est encore envoyée à DCS World ou aux sorties LED/LCD des panneaux.

## Fonctionnalités disponibles

- détection automatique des PZ55 et PZ70 ;
- connexion, déconnexion et reconnexion à chaud ;
- prise en charge de plusieurs périphériques et instances ;
- identification par chemin HID complet et numéro de série lorsqu'il est disponible ;
- affichage du VID, PID, chemin d'instance et état de connexion ;
- lecture des rapports HID bruts ;
- décodage des switches, boutons, sélecteurs et encodeurs ;
- Live Monitor avec filtres Hardware, Mapping, DCS-BIOS et Error ;
- moteur de mapping indépendant du backend ;
- profils JSON versionnés et validés ;
- stratégie sûre `NoSync` activée par défaut ;
- injection de dépendances et logs structurés ;
- outil de diagnostic matériel en ligne de commande.

## Validation matérielle

La lecture a été testée avec du matériel réel sous Windows :

| Panneau | USB | État |
|---|---|---|
| PZ55 Switch Panel | `VID 06A3 / PID 0D67` | Détection, rapports et switches validés |
| PZ70 Multi Panel | `VID 06A3 / PID 0D06` | Détection, rapports et contrôles validés |

Les tests ont notamment permis de valider les changements d'état du PZ55 et les
positions de sélecteur du PZ70. Les flux restent ouverts lorsque les panneaux sont
inactifs et se ferment proprement à l'arrêt de l'application.

## Interface

L'application contient les pages suivantes :

- **Dashboard** : état de DCS World, DCS-BIOS, des périphériques et du profil actif ;
- **Devices** : liste détaillée des panneaux détectés ;
- **Live Monitor** : rapports HID et événements décodés en temps réel ;
- **Profiles**, **Mappings**, **DCS-BIOS** et **Settings** : structure préparée pour les
  prochaines milestones.

## Prérequis

- Windows 10 ou Windows 11 ;
- SDK [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) ;
- un PZ55 ou PZ70 pour les fonctions matérielles ;
- DCS World et DCS-BIOS ne sont pas encore requis pour la milestone actuelle.

Vérifier le SDK installé :

```powershell
dotnet --version
```

## Compiler et tester

Depuis PowerShell :

```powershell
git clone https://github.com/lolo1580/dcs_panel_manager.git
cd dcs_panel_manager

dotnet restore .\DCSPanelManager.sln
dotnet build .\DCSPanelManager.sln -c Release
dotnet test .\DCSPanelManager.sln -c Release --no-build
```

État actuel de référence : **0 avertissement, 0 erreur, 9 tests réussis**.

## Lancer l'application

```powershell
dotnet run --project .\src\DCSPanel.App\DCSPanel.App.csproj -c Release
```

Branchez les panneaux, puis ouvrez **Devices** ou **Live Monitor**.

## Diagnostic matériel

Le probe utilise exactement le même service HID que l'application. Le nombre final est
la durée du test en secondes :

```powershell
dotnet run --project .\tools\DCSPanel.Hardware.Probe\DCSPanel.Hardware.Probe.csproj -c Release -- 20
```

Pendant le test, actionnez les switches, boutons et molettes. Un test réussi affiche les
deux connexions, les rapports bruts, les entrées décodées et `erreurs=0`.

Si l'ouverture échoue, fermez Logitech Flight Panels, DCSFlightpanels ou toute autre
application susceptible d'utiliser le périphérique de façon exclusive.

## Architecture

```text
src/
├── DCSPanel.Core/               Abstractions, événements et moteur de mapping
├── DCSPanel.Hardware/           Modèles et contrats matériels génériques
├── DCSPanel.Hardware.Logitech/  Énumération HID et protocole PZ55/PZ70
├── DCSPanel.DCSBIOS/            Frontière DCS-BIOS, inactive pour le moment
├── DCSPanel.Profiles/           Profils JSON, stockage et validation
└── DCSPanel.App/                Interface Avalonia et composition DI

tests/
├── DCSPanel.Core.Tests/
└── DCSPanel.Hardware.Tests/

tools/
└── DCSPanel.Hardware.Probe/
```

Le Core ne dépend ni de Logitech, ni de HID, ni de DCS-BIOS. Les détails du protocole
Logitech restent confinés au module `DCSPanel.Hardware.Logitech`.

Documentation complémentaire :

- [Décisions d'architecture](docs/architecture.md)
- [Recherche sur les protocoles HID](docs/hid-research.md)
- [Historique des versions](CHANGELOG.md)

## Dépendances principales

- **Avalonia 12** : interface graphique multiplateforme demandée par le projet ;
- **HidSharp** : accès aux périphériques et rapports HID bruts ;
- **Microsoft.Extensions.DependencyInjection** : composition modulaire ;
- **Microsoft.Extensions.Logging** : logs structurés ;
- **xUnit** : tests unitaires.

Les projets Core, Hardware, Profiles et DCSBIOS n'ajoutent aucune dépendance externe
inutile.

## Roadmap

### Milestone 2 — DCS-BIOS en lecture seule

- connexion et détection de déconnexion ;
- détection de l'avion actif ;
- import des métadonnées de contrôles ;
- affichage des données reçues dans Live Monitor.

### Milestone 3 — Mappings et commandes

- éditeur de profils et mappings ;
- envoi contrôlé de commandes DCS-BIOS ;
- profils initiaux F-16C et F/A-18C ;
- maintien de `NoSync` comme stratégie sûre par défaut.

### Milestone 4 — Feedback matériel

- LED du PZ55 ;
- LCD et LED du PZ70 ;
- mappings de retour DCS State → Hardware Output.

### Plus tard

- modifiers, layers et actions multiples ;
- backend clavier ;
- prise en charge du Stream Deck et d'autres périphériques.

## Sécurité de fonctionnement

La version actuelle n'envoie aucune commande à DCS-BIOS et n'écrit aucune donnée dans
les LED ou LCD. Charger un profil n'envoie pas automatiquement la position des switches
physiques vers DCS.

## Licence

Ce projet est distribué sous licence [MIT](LICENSE).
