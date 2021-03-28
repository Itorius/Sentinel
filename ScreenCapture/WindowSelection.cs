using System;
using System.Linq;
using Gdk;
using Gtk;
using X11;
using Window = Gdk.Window;

namespace Sentinel.ScreenCapture
{
	public class WindowSelection
	{
		private IntPtr X11Display;

		public event Action<Window, Rectangle>? Callback;
		private Window? _window;
		private Rectangle? selectedRect;

		public WindowSelection()
		{
			X11Display = Xlib.XOpenDisplay(null);
			Overlay.Show();

			void OnOverlayOnOnButtonPressed(ButtonPressEventArgs args)
			{
				Overlay.Hide();

				if (_window != null) Callback?.Invoke(_window, selectedRect.Value);
			}

			Overlay.OnButtonPressed += OnOverlayOnOnButtonPressed;
			Overlay.OnMouseMoved += GetWindowAtCursor;
		}

		public void GetWindows(IntPtr display, double pX, double pY)
		{
			selectedRect = null;

			Xlib.XQueryTree(display, Xlib.XDefaultRootWindow(X11Display), out _, out _, out var children);

			foreach (X11.Window window in Enumerable.Reverse(children))
			{
				if (window == (X11.Window)Overlay.XID)
					continue;

				Xlib.XGetWindowAttributes(display, window, out XWindowAttributes attributes);

				Utility.FindWindowClient(display, window, SearchDirection.FindChildren, out var ww);

				ww = window;

				System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(attributes.x, attributes.y, (int)attributes.width, (int)attributes.height);

				if (ww != 0 && rectangle.Contains((int)pX, (int)pY))
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

					_window = new Window(X11Wrapper.gdk_x11_window_foreign_new_for_display(Display.Default.Handle, ww));

					selectedRect = new Rectangle(x, y, (int)cAttr.width, (int)cAttr.height);

					break;
				}
			}
		}

		public void GetWindowAtCursor(MotionNotifyEventArgs args)
		{
			Application.Invoke(delegate
			{
				GetWindows(X11Display, args.Event.X, args.Event.Y);

				if (selectedRect != null) Overlay.SetRectangle(selectedRect.Value);
			});
		}
	}
}