# Recherche HID PZ55 / PZ70

La milestone 1 s'appuie sur le code source de
[DCSFlightpanels](https://github.com/DCS-Skunkworks/DCSFlightpanels), sans en copier
l'architecture ni réimplémenter DCS-BIOS.

Faits utilisés :

- constructeur PZ55 : VID `0x06A3`, PID `0x0D67` ;
- constructeur PZ70 : VID `0x06A3`, PID `0x0D06` ;
- les deux panneaux exposent trois octets d'entrée ;
- chaque contrôle correspond à un bit documenté par `SwitchPanelKey.cs` et
  `MultiPanelKnob.cs` ;
- le chemin HID complet est utilisé comme identité d'instance. Le numéro de série,
  lorsqu'il existe, complète cette identité ; le VID/PID seul ne distingue pas deux
  exemplaires du même modèle.

Sources primaires :

- [SwitchPanelPZ55.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Panels/SwitchPanelPZ55.cs)
- [SwitchPanelKey.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Switches/SwitchPanelKey.cs)
- [MultiPanelPZ70.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Panels/MultiPanelPZ70.cs)
- [MultiPanelKnob.cs](https://github.com/DCS-Skunkworks/DCSFlightpanels/blob/main/src/NonVisuals/Panels/Saitek/Switches/MultiPanelKnob.cs)
- [HidSharp](https://github.com/IntergatedCircuits/HidSharp)

Les formats de sortie LED/LCD ont été documentés dans les sources consultées mais ne
sont volontairement pas implémentés : cette milestone ne doit rien envoyer au matériel
ni à DCS.
