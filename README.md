# Royal Bestowing in Pocket Dimensions (RimWorld 1.6)

Petit patch de compatibilité ciblé pour **Royalty** et **Dimensions RePocketed**.

Il autorise une cérémonie d’octroi de titre lorsque le bénéficiaire se trouve dans une pocket dimension DR, sans modifier globalement `Map.IsPlayerHome` :

1. l’exigence d’acceptation reconnaît cette pocket dimension ;
2. le fallback « gathering spot / extérieur » de la cérémonie y est autorisé ;
3. seule une arrivée de shuttle portant le quest tag vanilla `Quest&lt;id&gt;.Bestowing` conserve la pocket dimension comme destination.

Lorsque **Shuttle Dock** est installé, une shuttle de Bestowing destinée à une pocket dimension attend qu’un dock assez grand soit ouvert, puis atterrit au centre de ce dock. Elle n’utilise plus de cellule aléatoire. Les autres `ShipJob_Arrive` et les autres pocket maps ne sont pas modifiés.

## Installation

Copier le dossier `RoyalBestowingPocketDimensions` dans le dossier `Mods` de RimWorld, puis charger dans cet ordre :

1. Harmony
2. Core / Royalty
3. Dimensions RePocketed
4. Royal Bestowing in Pocket Dimensions

La DLL compilée se trouve déjà dans `Assemblies`. Le projet source est fourni sous `Source`.

Pour compiler ailleurs, définir les propriétés MSBuild `RimWorldDir` et `HarmonyDir` si Steam n’utilise pas les chemins Windows par défaut.

## Test Dev Mode — Freeholder

1. Faire une sauvegarde dédiée et activer le mode développeur.
2. Placer le futur Freeholder dans une pocket dimension DR contenant un gathering spot valide et assez d’espace pour une shuttle.
3. Générer la quête de cérémonie (ou donner l’honneur requis), puis l’accepter normalement.
4. Vérifier que l’acceptation n’affiche plus « pawn not on colony map ».
5. Avec Shuttle Dock installé, garder le toit du dock fermé : la shuttle doit rester en attente sans apparaître ailleurs.
6. Ouvrir le toit d’un dock assez grand : le bestower et son escorte doivent arriver **sur ce dock dans la pocket dimension**.
7. Lancer et terminer la cérémonie, puis vérifier que la shuttle peut repartir.

Résultat attendu : aucun `Cannot find ceremony spot`, arrivée dans la pocket dimension, cérémonie et départ fonctionnels.

## Test Dev Mode — titre avec throne room

1. Donner au même pawn assez d’honneur pour un titre dont le `RoyalTitleDef` impose une salle du trône.
2. Construire une salle conforme dans la pocket dimension et assigner son trône au pawn.
3. Générer puis accepter la cérémonie.
4. Vérifier que la cérémonie utilise le trône assigné (branche vanilla), et non le fallback gathering spot.
5. Vérifier l’arrivée et le départ de la shuttle dans la pocket dimension.
6. Refaire un essai avec une salle volontairement non conforme : les exigences vanilla doivent toujours bloquer l’acceptation.

## Tests de non-régression

- Cérémonie sur une colonie normale : comportement vanilla inchangé.
- Arrivée d’une shuttle non liée à Bestowing vers une pocket map : redirection vanilla inchangée.
- Shuttle Dock absent : placement vanilla conservé après suppression de la redirection DR.
- Shuttle Dock présent mais fermé ou trop petit : l’arrivée Bestowing reste en attente.
- Autre type de pocket map : non reconnu par ce mod.
- Pocket dimension DR sans pawn joueur : non reconnue comme destination éligible.

## Adapter la détection

Toute la détection DR est isolée dans `SupportedPocketMap.cs`. Elle vérifie actuellement :

- le type `KB_PocketDimension.MapParent_PocketDimension` ;
- le world object def `KB_WorldObject_PocketDimension` ;
- le map generator def `KB_PocketDimensionMapGenerator` ;
- la présence d’un pawn de la faction joueur sur la map.

Si DR renomme un de ces éléments, seules les constantes de ce helper sont à mettre à jour.

