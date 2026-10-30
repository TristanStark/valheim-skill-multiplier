# Valheim Skill Multiplier

Mod léger pour **Valheim 1.0+** : multiplie le gain d'expérience (XP) de **toutes les compétences** par un facteur configurable, sans modifier les niveaux déjà acquis.

- **x1** : progression normale (vanilla).
- **x2** : deux fois plus d'XP (valeur par défaut).
- **x3** : trois fois plus d'XP.
- **x5** : cinq fois plus d'XP.
- **x0** : aucun gain d'XP.

Le facteur s'applique aux gains habituels de compétences via `Skills.RaiseSkill` (course, saut, armes, magie, etc.). Il ne change ni le niveau maximal, ni la perte de compétence à la mort, ni les commandes de console.

## Prérequis

- Valheim 1.0+ sur PC (version Mono).
- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) 5.x, installé et fonctionnel.

## Installation

1. Installer BepInExPack Valheim, puis lancer et fermer le jeu une première fois.
2. Récupérer le fichier `ValheimSkillMultiplier.dll` (depuis l'artefact de la [GitHub Action Build](../../actions/workflows/build.yml), ou après compilation locale).
3. Copier **uniquement** `ValheimSkillMultiplier.dll` dans le dossier `<Valheim>/BepInEx/plugins/`.
4. Lancer le jeu pour générer le fichier de configuration.

Il n'est pas nécessaire de copier les DLL de BepInEx, Harmony ou Unity depuis le dossier de compilation : BepInEx les fournit déjà au lancement du jeu.

## Configuration

Au premier démarrage du mod, BepInEx crée automatiquement :

```text
<Valheim>/BepInEx/config/com.tristanstark.valheim.skillmultiplier.cfg
```

Modifier par exemple :

```ini
[General]

## Multiplier applied to XP gained by every skill. 1 = vanilla; 2 = double XP; 3 = triple XP; 0 = no XP.
# Setting type: Single
# Acceptable value range: From 0 to 100
Multiplier = 3
```

Les décimales sont acceptées (`1.5`, par exemple). Valeurs autorisées : **0 à 100**. Pour appliquer une modification du fichier, relancer le jeu.

## Multijoueur

Le mod fonctionne **côté joueur (client)** : chaque personne qui souhaite multiplier ses propres gains d'XP doit l'installer et régler son fichier de configuration. L'installation sur le serveur dédié n'est pas nécessaire pour ce comportement. Respecter les règles des serveurs communautaires ; le mod n'impose pas ses paramètres aux autres joueurs.

## Compilation

Le projet utilise le SDK .NET 8 et cible `netstandard2.0`, compatible avec le runtime Mono de Valheim. Les références de compilation proviennent de NuGet ; **les fichiers propriétaires du jeu ne sont pas nécessaires pour compiler**.

```bash
dotnet restore src/ValheimSkillMultiplier/ValheimSkillMultiplier.csproj
dotnet build src/ValheimSkillMultiplier/ValheimSkillMultiplier.csproj -c Release --no-restore
```

DLL produite :

```text
src/ValheimSkillMultiplier/bin/Release/netstandard2.0/ValheimSkillMultiplier.dll
```

Chaque push sur `main` déclenche une GitHub Action qui compile et fournit la DLL comme artefact téléchargeable (archive ZIP).

## Fonctionnement technique

Le plugin BepInEx 5 applique, via Harmony, un préfixe à la méthode `Skills.RaiseSkill(Skills.SkillType, float)`. Avant chaque gain d'expérience positif, son argument `factor` est multiplié par `Multiplier`.

La recherche de la méthode cible se fait par réflexion : il n'est pas nécessaire de distribuer `assembly_valheim.dll` avec le mod. Si une mise à jour du jeu change la signature de la méthode, le plugin signale clairement l'échec dans les logs plutôt que d'appliquer un correctif au hasard.

**Compatibilité :** code conçu pour l'API de Valheim 1.0, mais le comportement en jeu doit être vérifié sur la version de Valheim et de BepInEx installée. Il peut interagir avec d'autres mods modifiant les gains d'XP (multiplicateurs cumulés selon l'ordre des patches).

## Diagnostic

Consulter `<Valheim>/BepInEx/LogOutput.log` et chercher `Valheim Skill Multiplier`. Un message doit confirmer que la méthode `Skills.RaiseSkill` a été patchée et indiquer le multiplicateur courant.

En cas d'absence d'effet, vérifier que BepInEx charge bien la DLL, que le fichier `.cfg` existe, que `Multiplier` est supérieur à 1 et qu'aucun mod tiers ne neutralise le patch.
