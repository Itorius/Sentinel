using System;
using System.Linq;
using Gdk;
using Gtk;
using X11;
using Screen = Gdk.Screen;
using Window = Gdk.Window;

namespace Sentinel.ScreenCapture
{
	public abstract class Selection
	{
		public Action<Window, Rectangle>? Callback;
	}

	public class WindowSelection : Selection
	{
		private IntPtr Display;

		private Window? _window;
		private Rectangle? selectedRect;

		public WindowSelection()
		{
			Display = Xlib.XOpenDisplay(null);
			Overlay.Show();

			void OnOverlayOnOnButtonPressed(ButtonPressEventArgs args)
			{
				Overlay.Hide();

				if (_window != null) Callback?.Invoke(_window, selectedRect.Value);
			}

			Overlay.OnButtonPressed += OnOverlayOnOnButtonPressed;
			Overlay.OnMouseMoved += args => GetWindowAtCursor((int)args.Event.X, (int)args.Event.Y);

			Gdk.Display.Default.DefaultSeat.Pointer.GetPosition(null, out int x, out int y);
			GetWindowAtCursor(x, y);
		}

		public void GetWindows(IntPtr display, int pX, int pY)
		{
			selectedRect = null;

			Xlib.XQueryTree(display, Xlib.XDefaultRootWindow(Display), out _, out _, out var children);

			foreach (X11.Window window in Enumerable.Reverse(children))
			{
				if (window == (X11.Window)Overlay.XID)
					continue;

				Xlib.XGetWindowAttributes(display, window, out XWindowAttributes attributes);

				Utility.FindWindowClient(display, window, SearchDirection.FindChildren, out var ww);

				ww = window;

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

					_window = new Window(X11Wrapper.gdk_x11_window_foreign_new_for_display(Gdk.Display.Default.Handle, ww));

					selectedRect = new Rectangle(x, y, (int)cAttr.width, (int)cAttr.height);

					break;
				}
			}
		}

		public void GetWindowAtCursor(int x, int y)
		{
			Application.Invoke(delegate
			{
				GetWindows(Display, x, y);

				if (selectedRect != null) Overlay.SetRectangle(selectedRect.Value);
			});
		}
	}
}