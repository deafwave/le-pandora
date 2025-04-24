using MelonLoader;

namespace LastEpochPandora
{
    public class LastEpochPandora : MelonMod
    {
        private ModManager _modManager;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("LastEpochPandora: OnInitializeMelon called.");
            _modManager = ModManager.Instance;
            _modManager.Initialize();
        }

        public override void OnDeinitializeMelon()
        {
            MelonLogger.Msg("LastEpochPandora: OnDeinitializeMelon called.");
            _modManager.Deinitialize();
        }
    }
}