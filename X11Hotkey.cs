using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gdk;
using X11;
using Event = Gdk.Event;
using EventMask = X11.EventMask;
using Screen = Gdk.Screen;
using Window = Gdk.Window;

namespace Sentinel
{
	public class X11Hotkey
	{
		private const int KeyPress = 2;
		private const int KeyRelease = 3;
		private const int GrabModeAsync = 1;
		private Key key;
		private ModifierType modifiers;
		private int keycode;

		public X11Hotkey(Key key, ModifierType modifiers = ModifierType.None)
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

			List<ModifierType> mod_masks = new()
			{
				0, /* modifier only */
				ModifierType.Mod2Mask,
				ModifierType.LockMask,
				ModifierType.Mod5Mask,
				ModifierType.Mod2Mask | ModifierType.LockMask,
				ModifierType.Mod2Mask | ModifierType.Mod5Mask,
				ModifierType.LockMask | ModifierType.Mod5Mask,
				ModifierType.Mod2Mask | ModifierType.LockMask | ModifierType.Mod5Mask
			};

			foreach (ModifierType mask in mod_masks)
			{
				XGrabKey(xDisplay, keycode, (uint)(modifiers | mask), GetXWindow(rootWin), false, GrabModeAsync, GrabModeAsync);
			}
		}

		public void Unregister()
		{
			Window rootWin = Global.DefaultRootWindow;
			IntPtr xDisplay = GetXDisplay(rootWin);

			List<ModifierType> mod_masks = new()
			{
				0, /* modifier only */
				ModifierType.Mod2Mask,
				ModifierType.LockMask,
				ModifierType.Mod5Mask,
				ModifierType.Mod2Mask | ModifierType.LockMask,
				ModifierType.Mod2Mask | ModifierType.Mod5Mask,
				ModifierType.LockMask | ModifierType.Mod5Mask,
				ModifierType.Mod2Mask | ModifierType.LockMask | ModifierType.Mod5Mask
			};

			foreach (ModifierType mask in mod_masks)
			{
				XUngrabKey(xDisplay, keycode, (uint)(modifiers | mask), GetXWindow(rootWin));
			}
		}

		private unsafe FilterReturn FilterFunction(IntPtr xEvent, Event evnt)
		{
			XKeyEvent xKeyEvent = Marshal.PtrToStructure<XKeyEvent>(xEvent);
			Window rootWin = Global.DefaultRootWindow;
			IntPtr xDisplay = GetXDisplay(rootWin);

			if (xKeyEvent.send_event != 0) return FilterReturn.Continue;
			if (xKeyEvent.keycode != keycode) return FilterReturn.Continue;
			
			if (xKeyEvent.type == KeyPress)
			{
				var p = stackalloc byte[32];

				Xlib.XQueryKeymap(xDisplay, p);

				int spaceKey = XKeysymToKeycode(xDisplay, 0xFF61);

				if ((p[spaceKey / 8] & (0x1 << (spaceKey % 8))) != 0)
				{
					Pressed?.Invoke(this, EventArgs.Empty);
					return FilterReturn.Continue;
				}
			}

			Xlib.XGetInputFocus(xDisplay, out var focus, out _);

			XKeyEvent k = new XKeyEvent
			{
				display = xDisplay,
				window = (nuint)focus,
				root = (nuint)rootWin.Handle.ToInt64(),
				subwindow = 0,
				time = 0,
				x = 1,
				y = 1,
				x_root = 1,
				y_root = 1,
				same_screen = 1,
				type = xKeyEvent.type == KeyPress ? KeyPress : KeyRelease,
				keycode = (uint)keycode,
				state = (uint)modifiers
			};

			Xlib.XSendEvent(xDisplay, focus, true, (long)(EventMask.KeyPressMask | EventMask.KeyReleaseMask), (IntPtr)(&k));

			return FilterReturn.Continue;
		}

		internal static IntPtr GetXWindow(Window window)
		{
			return gdk_x11_window_get_xid(window.Handle);
		}

		internal static IntPtr GetXDisplay(Window window)
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