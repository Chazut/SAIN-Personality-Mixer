# SAIN Personality Mixer

![SAIN Personality Mixer](https://raw.githubusercontent.com/Chazut/SAIN-Personality-Mixer/main/assets/branding/SAIN-Personality-Mixer-logo-preview.png)

Choose the chance of each SAIN personality for PMCs, regardless of their level or equipment. Configure everything in **F12**.

## Why I made this

SAIN uses level and equipment when assigning personalities. At higher levels, this can mean lots of Chads and less variety, depending on your preset.

I wanted to keep a mix of aggressive, cautious and inexperienced PMCs throughout a playthrough. This also brings more variety to ORBIT, which uses SAIN personalities to shape bot behavior. **ORBIT is optional.**

## Install

Requires **SPT 4.1.x**, **SAIN 4.5.1** and its dependencies. The F12 menu requires BepInEx ConfigurationManager.

1. Extract the mod ZIP into your SPT folder.
2. Open **F12 > SAIN Personality Mixer** and adjust the chances.
3. Start a new raid to see your mix in action.

The DLL belongs in `BepInEx/plugins/SAINPersonalityMixer/`.

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

Keep the total at 100 for percentages, or let the mod balance it automatically. **Effective chances** shows the result. Zero excludes a personality; all zeros restores SAIN's normal assignment for new bots.

## Good to know

- **PMCs only.** Scavs, bosses and other bots keep their usual SAIN assignment.
- Your SAIN preset still controls combat behavior. The mixer only chooses the personality.
- Each PMC gets a random roll, so one raid won't necessarily match the percentages.
- Changes apply to new bots. Existing bots keep their personality.
- Fika multiplayer has not been validated yet.

To uninstall, close the game and remove the `SAINPersonalityMixer` plugin folder.

[Changelog](CHANGELOG.md) · [Build instructions](DEVELOPMENT.md) · [MIT License](LICENSE)
