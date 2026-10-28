// Only the game-owned boundaries are replaced in this harness. The selection and
// patch adapter are compiled directly from src; SAIN's settings/enum assembly is real.
namespace EFT
{
    public enum WildSpawnType { assault = 1, pmcBot = 9, exUsec = 24, pmcBEAR = 51, pmcUSEC = 52 }
    public sealed class BotOwner { public string ProfileId { get; set; } = "test"; }
}

namespace SAIN.SAINComponent.Classes.Info
{
    public sealed class BotProfile
    {
        public EFT.WildSpawnType WildSpawnType;
        public int PlayerLevel;
        public float PowerLevel;
    }
    public sealed class SAINBotInfoClass
    {
        public BotProfile Profile { get; set; } = new();
        public EFT.BotOwner BotOwner { get; set; } = new();
    }
}

namespace SAIN.Models.Preset.Personalities
{
    public sealed class PersonalityDictionary : Dictionary<
        SAIN.Preset.Shared.Models.Preset.Personalities.EPersonality,
        SAIN.Preset.Shared.Personalities.BasePersonality.PersonalitySettingsClass> { }
}
