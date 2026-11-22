using System;
using CommunityToolkit.Mvvm.Input;
using PdfBooklet.Interfaces;

namespace PdfBooklet.ViewModels;

public sealed class AboutDialogViewModel : ViewModelBase
{
    public const string ProjectUrl = "https://github.com/fuszenecker/PdfBooklet";
    public const string ReleasesUrl = ProjectUrl + "/releases";
    public const string LicenseUrl = "https://www.gnu.org/licenses/agpl-3.0.html";

    public string Name { get; }
    public string VersionText { get; }
    public string Copyright { get; }
    public string Runtime { get; }

    public Uri ProjectUri { get; } = new(ProjectUrl);
    public Uri ReleasesUri { get; } = new(ReleasesUrl);
    public Uri LicenseUri { get; } = new(LicenseUrl);

    public IRelayCommand OkCommand { get; }

    public event EventHandler? CloseRequested;

    public AboutDialogViewModel(IApplicationInfo applicationInfo)
    {
        Name = applicationInfo.Name;
        VersionText = $"Version {applicationInfo.Version}";
        Copyright = applicationInfo.Copyright;
        Runtime = applicationInfo.Runtime;
        OkCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}
