using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;

namespace GitExtensions.AITools;

internal interface IAiFeature
{
    IEnumerable<ISetting> GetSettings();

    void Register(IGitUICommands gitUiCommands);

    void Unregister(IGitUICommands gitUiCommands);
}
