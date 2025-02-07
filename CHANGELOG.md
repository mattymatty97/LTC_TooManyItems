## v1.4.1
- g-bye OG OutOfBounds code
  - as there should not be any ( non-intentional ) way for items to end up below the ship floor anymore  
code for teleporting items back to the shipCenter have been removed

## v1.4.0
- rewrite OutOfBounds patch with code from MattyFixes v2
  - support for cruiser and BeltBag ( or anything that holds items by parenting them itself )
  - improved lobby join detection for item positions

## v1.3.1
- fix items slowly sinking into the floor on rehost

## v1.3.0
- import OutOfBounds rewrite from MattyFixes
- add support for cruiser and add compatibility for pre-cruiser versions

## v1.2.0
- update patches for v56

## v1.1.2
- do not touch stickyNote nor clipboard manual ( thanks to @1a3 )
- only check OutOfBounds on load and on ship departure
- remove packet size limit for scrap value sync
- fix radar icons still showing for scrap in ship
- hopefully fix lamps falling below the ship

## v1.1.1
- use different logic on clients to mark items as collected on join

## v1.1.0
- RadarPatches from [Matty's Fixes](https://thunderstore.io/c/lethal-company/p/mattymatty/Matty_Fixes/)
- fixed CHANGELOG
- changed description

## v1.0.0
- release
- ItemLimit from [Lobby Control](https://thunderstore.io/c/lethal-company/p/mattymatty/LobbyControl/)
- OutOfBounds from [Matty's Fixes](https://thunderstore.io/c/lethal-company/p/mattymatty/Matty_Fixes/)
