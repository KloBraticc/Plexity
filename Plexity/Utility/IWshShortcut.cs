namespace Plexity.Utility
{
    internal interface IWshShortcut
    {
        string TargetPath { get; set; }
        string Arguments { get; set; }
        string? WorkingDirectory { get; set; }
        string IconLocation { get; set; }

        void Save();
    }
}