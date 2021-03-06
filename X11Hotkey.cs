using System;
using System.Runtime.InteropServices;
using Gdk;

namespace Sentinel
{
	public class X11Hotkey
	{
		private const int KeyPress = 2;
		private const int GrabModeAsync = 1;
		private Key key;
		private ModifierType modifiers;
		private int keycode;

		public X11Hotkey(Key key, ModifierType modifiers = (ModifierType)(1 << 15))
		{
			this.key = key;
			this.modifiers = modifiers;

			Window rootWin = Global.DefaultRootWindow;
			IntPtr xDisplay = GetXDisplay(rootWin);
			keycode = XKeysymToKeycode(xDisplay, (int)this.key);
			rootWin.AddFilter(FilterFunction);
		}

		public event EventHandler Pressed;

		public void Register()
		{
			Window rootWin = Global.DefaultRootWindow;
			IntPtr xDisplay = GetXDisplay(rootWin);

			XGrabKey(xDisplay, keycode, (uint)modifiers, GetXWindow(rootWin), false, GrabModeAsync, GrabModeAsync);
		}

		public void Unregister()
		{
			Window rootWin = Global.DefaultRootWindow;
			IntPtr xDisplay = GetXDisplay(rootWin);

			XUngrabKey(xDisplay, keycode, (uint)modifiers, GetXWindow(rootWin));
		}

		private FilterReturn FilterFunction(IntPtr xEvent, Event evnt)
		{
			XKeyEvent xKeyEvent = Marshal.PtrToStructure<XKeyEvent>(xEvent);

			if (xKeyEvent.type == KeyPress)
			{
				if (xKeyEvent.keycode == keycode)
				{
					Pressed?.Invoke(this, EventArgs.Empty);
				}
			}

			return FilterReturn.Continue;
		}

		private static IntPtr GetXWindow(Window window)
		{
			return gdk_x11_window_get_xid(window.Handle);
		}

		private static IntPtr GetXDisplay(Window window)
		{
			return gdk_x11_display_get_xdisplay(Screen.Default.Display.Handle);
		}

		[DllImport("libgdk-3.so.0")]
		private static extern IntPtr gdk_x11_window_get_xid(IntPtr gdkWindow);

		[DllImport("libgdk-3.so.0")]
		private static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDrawable);

		[DllImport("libX11")]
		private static extern int XKeysymToKeycode(IntPtr display, int key);

		[DllImport("libX11")]
		private static extern int XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grab_window, bool owner_events, int pointer_mode, int keyboard_mode);

		[DllImport("libX11")]
		private static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grab_window);

		[StructLayout(LayoutKind.Sequential)]
		internal struct XKeyEvent
		{
			public int type; // short
			public nuint serial;
			public int send_event; // short
			public IntPtr display;
			public nuint window;
			public nuint root;
			public nuint subwindow;
			public nuint time;
			public int x, y;
			public int x_root, y_root;
			public uint state;
			public uint keycode;
			public int same_screen; // short
		}
	}
}