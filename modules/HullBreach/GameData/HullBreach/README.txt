HullBreach 1.8.0 (unofficial port for KSP 1.12.5 + BDArmory Plus 1.13)
=======================================================================

Damage-driven flooding and sinking for ships and submarines.

Original mod: HullBreach by gomker (Derek Lane), with contributions from SpannerMonkey (Spanner),
Gedas-S and zzhwudiaa - https://github.com/gomker/HullBreach - licensed CC BY-NC-SA 3.0 (see LICENSE.txt).

This is a MODIFIED work. Changes from 1.7.0.5: rebuilt for KSP 1.12 and BDArmory Plus 1.13,
new flooding model driven by BDArmory hitpoints and armour, submarine support, one simulation
driver per vessel instead of per-part Update loops, and new ModuleManager patches. See CHANGES.md
in the source repository for the full list. This port is not endorsed by the original author.
The Large Sea Water Pump model and textures are by Spanner, unchanged.

Install: copy this HullBreach folder into KSP/GameData. Requires ModuleManager and BDArmory Plus 1.13.

Contents
  Plugins/HullBreach.dll   the plugin (needs BDArmory.dll)
  Patches/                 ModuleManager patches, each guarded with :NEEDS for its part pack
  Settings.cfg             global tuning (HullBreachSettings node)
  Resources.cfg            SeaWater (flood water) and, if nobody else defines it, BallastWater
  Parts/                   Universal Large Sea Water Pump
  PluginData/              in-flight "Save" writes settings.cfg here
