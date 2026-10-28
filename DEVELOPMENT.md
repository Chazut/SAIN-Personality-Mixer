# Build and validate

Requires the .NET 10 SDK for the regression tests and the local game/SAIN assemblies (not included). The plugin targets .NET Standard 2.1.

```powershell
dotnet build SAINPersonalityMixer.csproj -c Release -p:SptRoot="C:\Games\SPT-4.1"
dotnet run --project tests/Regression/Regression.csproj -c Release
```

Pass `-p:SptRoot="path\to\SPT"` and, if needed, `-p:SainDirectory="path\to\SAIN"` to either command to use a different installation. Building does not install files into the game.

Run `./scripts/Package.ps1` to build and create the installation ZIP in `dist/`. It accepts the same installation paths as PowerShell parameters (`-SptRoot` and `-SainDirectory`). Use `-SkipBuild` to package an existing Release build.

## Validation

Automated tests cover weighted assignment, PMC-only filtering, preset reloads, disabled/invalid configurations and cache lifetime. A short solo raid confirmed that the plugin loads and assigns varied personalities. The revised default mix needs longer raid testing; multiplayer has not been validated.

Enable **F12 > SAIN Personality Mixer > Log assignments** to record each newly mixed PMC in `BepInEx/LogOutput.log`. Configuration is stored in `BepInEx/config/com.chazut.sainpersonalitymixer.cfg`.

## Integration notes

- Only `pmcUSEC` and `pmcBEAR` roles use the mix.
- The mixer bypasses SAIN's level, equipment, nickname and forced-personality assignment rules, including assignment enable/chance settings. Behavior settings still come from the active SAIN preset.
- Preset reloads refresh behavior settings while preserving each bot's selected personality.
- Missing personalities are excluded and the remaining choices are reweighted, with a warning. If a cached personality disappears after a preset reload, SAIN handles that bot's assignment until it becomes available again.
- Other mods patching the same assignment method can affect the result. In Fika, the AI authority must run the plugin; multiplayer needs in-game validation.
- Assignment happens when SAIN configures bots. There is no per-frame scan, and no SAIN files or presets are replaced.

SAIN is a separate required download maintained by its respective authors. Its assemblies and the game's assemblies are not bundled.
