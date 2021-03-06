using System;
using Cairo;
using Gdk;
using Gtk;
using CairoHelper = Gdk.CairoHelper;
using Window = Gtk.Window;

namespace Sentinel
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Application.Init();

			X11Hotkey hotkey = new(Gdk.Key.Print);
			hotkey.Pressed += (sender, eventArgs) => { Console.WriteLine("PRINT"); };
			hotkey.Register();

			Window window = new("CaptureX") { Icon = new Pixbuf("icon.png") };

			window.DeleteEvent += (o, eventArgs) =>
			{
				Application.Quit();
				hotkey.Unregister();
			};
			window.ShowAll();

			Application.Run();
		}

		private static Widget GetSelectWindow()
		{
			Screen screen = Screen.Default;
			Visual visual = screen.RgbaVisual;
			Widget window = new Window(Gtk.WindowType.Popup);

			if (screen.IsComposited && visual != null)
			{
				window.Visual = visual;
				window.AppPaintable = true;
			}

			((Window)window).Move(-100, -100);
			((Window)window).Resize(10, 10);
			// ((Window)window).Move(100, 100);
			// ((Window)window).Resize(100, 100);
			window.Drawn += SelectDraw;

			window.ShowAll();
			return window;
		}

		private static void SelectDraw(object sender, DrawnArgs args)
		{
			Widget widget = (Widget)sender;
			var style = widget.StyleContext;

			if (widget.AppPaintable)
			{
				Context ctx = CairoHelper.Create(widget.Window);
				ctx.Operator = Operator.Source;
				ctx.SetSourceRGBA(0, 0, 0, 0);
				ctx.Paint();

				style.Save();
				style.AddClass("rubberband");

				style.RenderBackground(ctx, 0, 0, widget.AllocatedWidth, widget.AllocatedHeight);
				style.RenderFrame(ctx, 0, 0, widget.AllocatedWidth, widget.AllocatedHeight);

				style.Restore();
			}
		}
	}
}