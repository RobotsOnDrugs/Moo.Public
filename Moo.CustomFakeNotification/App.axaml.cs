using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Moo.CustomFakeNotification;

public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow();
			//MainWindow mwindow = (MainWindow)desktop.MainWindow;
		}

		base.OnFrameworkInitializationCompleted();
	}
}
