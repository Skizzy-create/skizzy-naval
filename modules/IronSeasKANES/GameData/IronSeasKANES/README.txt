Iron Seas KANES (Balao class fleet boat) - unofficial update for KSP 1.12.5 / BDArmory Plus 1.13

Original mod: Iron Seas KANES FIGHTING SHIPS 1 by SkunkWorksCommand
  https://spacedock.info/mod/2528/Iron%20Seas%20KANES%20FIGHTING%20SHIPS%201
This folder is a modified version (2026-11), not made or endorsed by the original author.
License: GNU General Public License v2 (LICENSE.txt in this folder).

What changed and why: CHANGES.md in the release package (next to the GameData folder),
which also contains the full diff against the original release and the scripts that made it.

Deck guns: the six gun models have no turret/fire transforms (each is one merged mesh).
Plugins/SkizzyRig.dll adds invisible yaw/pitch/fire transforms at load time, so BDArmory aims
the guns and fires from their muzzles, but the gun meshes do not turn or elevate.
SkizzyRig was written for this update: MIT licence (Plugins/SkizzyRig-LICENSE.txt), source in
src/SkizzyRig in the release package.
