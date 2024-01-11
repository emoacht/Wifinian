using System;
using System.Windows;

using ScreenFrame.Painter;

namespace Wifinian.Views;

public class WindowPainter : ScreenFrame.Painter.WindowPainter
{
	public WindowPainter() : base(Environment.GetCommandLineArgs())
	{
		ThemeService.AdjustResourceColors(Application.Current.Resources);

		RespondsThemeChanged = false;
	}

	protected override string TranslucentBrushKey { get; } = "App.Background.Translucent";

	protected override void ChangeThemes(ColorTheme oldTheme, ColorTheme newTheme)
	{
		// Changing color themes is not implemented.
	}
}