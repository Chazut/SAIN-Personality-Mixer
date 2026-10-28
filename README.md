# SAIN Personality Mixer

![SAIN Personality Mixer](https://raw.githubusercontent.com/Chazut/SAIN-Personality-Mixer/main/assets/branding/SAIN-Personality-Mixer-logo-preview.png)

Choose how often each SAIN personality appears among PMCs, independently of level and equipment.

Requires **SPT 4.1.x and SAIN 4.5.1**. Install SAIN and its dependencies first. ORBIT is optional.

## Install

Extract the release ZIP into your SPT game folder. The plugin belongs at:

```text
BepInEx/plugins/SAINPersonalityMixer/SAINPersonalityMixer.dll
```

Open **F12 > SAIN Personality Mixer** to change the mix. BepInEx ConfigurationManager must be installed to display the F12 menu. The configuration file is `BepInEx/config/com.chazut.sainpersonalitymixer.cfg`.

## Default mix

| Personality | Chance |
|---|---:|
| Normal | 30% |
| Chad | 10% |
| Wreckless | 10% |
| Rat | 20% |
| Snapping Turtle | 10% |
| Coward | 10% |
| Timmy | 5% |
| GigaChad | 5% |

Values totaling 100 are percentages. Other totals are normalized automatically; **Effective chances** shows the result. Zero excludes a personality. If every value is zero, new bots use SAIN's normal assignment.

The selection is random for each bot, not a quota per raid. Small raids can differ considerably from the target mix.

## What changes

- Only the `pmcUSEC` and `pmcBEAR` bot roles use the mix. Scavs, player Scavs, bosses, followers, raiders, rogues and other roles retain SAIN's assignment.
- When the mixer selects a PMC's personality, it bypasses SAIN's level, equipment, nickname and forced-personality assignment rules, including SAIN's assignment enable/chance settings. The probabilities are controlled here.
- The selected personality's combat settings still come from your active SAIN preset. Reloading that preset updates the settings while preserving the personality.
- Changes to the mix or Enabled switch apply to **new bots**. Existing mixed bots keep their personality until the raid ends. Start another raid to apply a new mix to everyone.
- A personality absent from a custom SAIN preset cannot be selected; the remaining choices are reweighted and a warning is logged. If a cached personality disappears after a preset reload, SAIN handles that bot's assignment until that personality is available again.
- **Log assignments**, off by default, records one line per newly mixed PMC in `BepInEx/LogOutput.log`.

The plugin does not replace SAIN files or edit its presets. ORBIT reads the personality chosen by SAIN, so it can use this mix without an ORBIT update. Assignment runs when SAIN configures bots; there is no per-frame scan.

Only one mod should control personality assignment at a time. Other Harmony patches targeting the same method can affect the result. In Fika, the AI authority must run the plugin; multiplayer behavior still needs in-game validation.

## Uninstall

Close the game and remove `BepInEx/plugins/SAINPersonalityMixer`. Keep or remove the `.cfg` file as desired. SAIN's normal assignment resumes on the next launch.

## Build and validate

Requires the .NET 10 SDK for the regression tests and the local game/SAIN assemblies (not included). The plugin targets .NET Standard 2.1.

```powershell
dotnet build SAINPersonalityMixer.csproj -c Release -p:SptRoot="C:\Games\SPT-4.1"
dotnet run --project tests/Regression/Regression.csproj -c Release
```

Pass `-p:SptRoot="path\to\SPT"` and, if needed, `-p:SainDirectory="path\to\SAIN"` to either command to use a different installation. Building does not install files into the game.

Run `./scripts/Package.ps1` to build and create the installation ZIP in `dist/`. It accepts the same installation paths as PowerShell parameters (`-SptRoot` and `-SainDirectory`). Use `-SkipBuild` to package an existing Release build.

Automated tests cover weighted assignment, PMC-only filtering, preset reloads, disabled/invalid configurations and cache lifetime. A short solo raid confirmed that the plugin loads and assigns varied personalities. The revised default mix still needs longer raid testing; multiplayer has not been validated.

SAIN is maintained by its respective authors and is a separate required download. This package contains only Personality Mixer.

## License

SAIN Personality Mixer is released under the [MIT License](LICENSE). Game assemblies and other mods are not included and retain their own licenses.
