# Architecture

Les dépendances pointent vers les abstractions, jamais vers l'interface graphique :

```text
DCSPanel.App
 ├─ DCSPanel.Core
 ├─ DCSPanel.Hardware ──> DCSPanel.Core
 ├─ DCSPanel.Hardware.Logitech ──> Hardware + Core
 ├─ DCSPanel.DCSBIOS ──> Core
 └─ DCSPanel.Profiles ──> Core
```

Décisions principales :

1. `DCSPanel.Core` ne connaît ni Logitech, ni HID, ni DCS-BIOS.
2. `IHardwareService` représente l'énumération, le hot-plug et les flux d'entrée.
3. Le driver Logitech transforme les rapports HID en événements génériques, tout en
   publiant aussi le rapport brut pour le Live Monitor.
4. Le moteur de mapping sélectionne un backend par nom. DCS-BIOS, le clavier ou un
   futur backend peuvent donc être ajoutés sans modifier le driver.
5. Les profils JSON sont versionnés et validés. `NoSync` est le défaut explicite du
   profil générique.
6. Le client DCS-BIOS de la milestone 1 est une frontière inactive : aucune socket et
   aucune commande ne sont émises.
