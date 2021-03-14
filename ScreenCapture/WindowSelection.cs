using System;
using System.Collections.Generic;
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

		public event Action<Rectangle> Callback;
		private Window _window;

		public WindowSelection()
		{
			X11Display = X11Wrapper.XOpenDisplay(IntPtr.Zero);
			Overlay.Show();

			void OnOverlayOnOnButtonPressed(ButtonPressEventArgs args)
			{
				Overlay.Hide();

				Callback?.Invoke(rects[0]);

				if (_window != null)
				{
					_window = _window.Toplevel;
					var pixbuf = new Pixbuf(_window, 0, 0, _window.Width, _window.Height);
					pixbuf.Save("test.png", "png");
				}
			}

			Overlay.OnButtonPressed += OnOverlayOnOnButtonPressed;
			Overlay.OnMouseMoved += args => GetWindowAtCursor();
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

					_window = new Window(X11Wrapper.gdk_x11_window_foreign_new_for_display(Display.Default.Handle, ww));

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

				if (rects.Count > 0)
				{
					Overlay.SetRectangle(rects[0]);
				}
			});

			return null;
		}
	}
}