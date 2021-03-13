using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Cairo;
using Gdk;
using Gtk;
using X11;
using Rectangle = Gdk.Rectangle;
using Screen = Gdk.Screen;
using Window = Gdk.Window;

namespace Sentinel.ScreenCapture
{
	public class WindowSelection
	{
		private Gtk.Window SelectionOverlay;
		private IntPtr X11Display;

		public WindowSelection()
		{
			Screen screen = Screen.Default;
			Visual visual = screen.RgbaVisual;
			SelectionOverlay = new Gtk.Window(Gtk.WindowType.Popup);

			CssProvider styleProvider = new();
			styleProvider.LoadFromPath("theme.css");

			if (screen.IsComposited && visual != null)
			{
				SelectionOverlay.Visual = visual;
				SelectionOverlay.AppPaintable = true;
			}

			X11Display = X11Wrapper.XOpenDisplay(IntPtr.Zero);

			Timer timer = new() { Interval = 100 };
			timer.Elapsed += (sender, args) => { GetWindowAtCursor(); };
			timer.Start();

			SelectionOverlay.ButtonPressEvent += (o, args) =>
			{
				timer.Stop();
				SelectionOverlay.Dispose();
			};

			SelectionOverlay.StyleContext.AddProvider(styleProvider, StyleProviderPriority.User);
			SelectionOverlay.Name = "Overlay";
			SelectionOverlay.Drawn += (o, args) =>
			{
				args.RetVal = false;

				Widget widget = (Widget)o;
				var style = widget.StyleContext;
				var ctx = args.Cr;

				if (widget.AppPaintable)
				{
					ctx.Operator = Operator.Source;
					ctx.SetSourceRGBA(0, 0, 0, 0);
					ctx.Paint();

					style.Save();
					style.AddClass("rubberband");

					foreach (Rectangle rect in rects)
					{
						style.RenderBackground(ctx, rect.X, rect.Y, rect.Width, rect.Height);
						style.RenderFrame(ctx, rect.X, rect.Y, rect.Width, rect.Height);
					}

					style.Restore();
				}
			};
			SelectionOverlay.Move(0, 0);
			SelectionOverlay.Resize(screen.Width, screen.Height);

			SelectionOverlay.Show();
		}

		private List<Rectangle> rects = new();

		public void GetWindows(IntPtr display)
		{
			rects.Clear();

			var pointer = Display.Default.DefaultSeat.Pointer;
			pointer.GetPosition(null, out int pX, out int pY);

			Xlib.XQueryTree(display, Xlib.XDefaultRootWindow(X11Display), out _, out _, out var children);

			foreach (X11.Window window in Enumerable.Reverse(children))
			{
				Xlib.XGetWindowAttributes(display, window, out XWindowAttributes attributes);

				Utility.FindWindowClient(display, window, SearchDirection.FindChildren, out var ww);

				System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(attributes.x, attributes.y, (int)attributes.width, (int)attributes.height);

				if (ww != 0 && rectangle.Contains(pX, pY))
				{
					Xlib.XGetWindowAttributes(display, ww, out XWindowAttributes cAttr);

					int x, y;
					if (window == ww)
					{
						x = attributes.x;
						y = attributes.y;
					}
					else
					{
						x = cAttr.x + attributes.x;
						y = cAttr.y + attributes.y;
					}

					rects.Add(new Rectangle(x, y, (int)cAttr.width, (int)cAttr.height));
					break;
				}
			}
		}

		public Window GetWindowAtCursor()
		{
			Application.Invoke(delegate
			{
				GetWindows(X11Display);

				SelectionOverlay.QueueDraw();
			});

			return null;
		}
	}
}