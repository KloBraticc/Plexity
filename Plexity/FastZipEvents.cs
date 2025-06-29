// To debug the automatic updater:
// - Uncomment the definition below
// - Publish the executable
// - Launch the executable (click no when it asks you to upgrade)
// - Launch Roblox (for testing web launches, run it from the command prompt)
// - To re-test the same executable, delete it from the installation folder

namespace Plexity
{
    internal class FastZipEvents
    {
        public Func<object, object, object> FileFailure { get; internal set; }
        public Func<object, object, object> DirectoryFailure { get; internal set; }
        public Func<object, object, object> ProcessFile { get; internal set; }
    }
}