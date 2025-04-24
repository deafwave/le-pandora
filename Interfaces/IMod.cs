namespace LastEpochPandora.Mods
{
    public interface IMod
    {
        string Name { get; }
        string Version { get; }
        void Initialize();
        void Deinitialize();
    }
}