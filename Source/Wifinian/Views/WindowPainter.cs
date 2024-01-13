using System;
using System.Windows;

using ScreenFrame.Painter;

namespace Wifinian.Views;

public class WindowPainter : ScreenFrame.Painter.WindowPainter
{
	public WindowPainter() : base(Environment.GetCommandLineArgs())
	{
		ThemeService.AdjustResourceColors(Application.Current.Resources);

		RespondsThemeChanged = true;
	}

	protected override string TranslucentBrushKey { get; } = "App.Background.Translucent";

	protected override void ChangeThemes(ColorTheme oldTheme, ColorTheme newTheme)
	{
		// Changing color themes is not implemented.
	}

	public string GetIconPath()
	{
		return Theme switch
		{
			ColorTheme.Light => "pack://application:,,,/Resources/Icons/LightTrayIcon.ico",
			ColorTheme.Dark or _ => "pack://application:,,,/Resources/Icons/DarkTrayIcon.ico",
		};
	}
}