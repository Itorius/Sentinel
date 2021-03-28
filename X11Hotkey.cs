using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gdk;
using X11;
using Event = Gdk.Event;
using Screen = Gdk.Screen;
using Window = Gdk.Window;

namespace Sentinel
{
	public class X11Hotkey
	{
		private const int KeyPress = 2;
		private const int KeyRelease = 3;
		private Key key;
		private ModifierType modifiers;
		private KeyCode keycode;
		private IntPtr XDisplay;

		public X11Hotkey(Key key, ModifierType modifiers = ModifierType.None)
		{
			this.key = key;
			this.modifiers = modifiers;

			Window rootWin = Global.DefaultRootWindow;
			XDisplay = GetXDisplay(rootWin);
			keycode = Xlib.XKeysymToKeycode(XDisplay, (KeySym)this.key);
			rootWin.AddFilter(FilterFunction);
		}

		public event EventHandler Pressed;

		private static readonly HashSet<ModifierType> mod_masks = new()
		{
			0,
			ModifierType.Mod2Mask,
			ModifierType.LockMask,
			ModifierType.Mod5Mask,
			ModifierType.Mod2Mask | ModifierType.LockMask,
			ModifierType.Mod2Mask | ModifierType.Mod5Mask,
			ModifierType.LockMask | ModifierType.Mod5Mask,
			ModifierType.Mod2Mask | ModifierType.LockMask | ModifierType.Mod5Mask
		};

		public void Register()
		{
			Window rootWin = Global.DefaultRootWindow;

			foreach (ModifierType mask in mod_masks)
			{
				Xlib.XGrabKey(XDisplay, keycode, (KeyButtonMask)(modifiers | mask), (X11.Window)GetXWindow(rootWin), false, GrabMode.Async, GrabMode.Async);
			}
		}

		public void Unregister()
		{
			Window rootWin = Global.DefaultRootWindow;

			foreach (ModifierType mask in mod_masks)
			{
				Xlib.XUngrabKey(XDisplay, keycode, (KeyButtonMask)(modifiers | mask), (X11.Window)GetXWindow(rootWin));
			}
		}

		private unsafe FilterReturn FilterFunction(IntPtr xEvent, Event evnt)
		{
			XKeyEvent xKeyEvent = Marshal.PtrToStructure<XKeyEvent>(xEvent);
			Window rootWin = Global.DefaultRootWindow;

			if (xKeyEvent.send_event) return FilterReturn.Continue;
			if (xKeyEvent.keycode != (ulong)keycode) return FilterReturn.Continue;

			if (xKeyEvent.type == KeyPress)
			{
				var p = stackalloc byte[32];

				Xlib.XQueryKeymap(XDisplay, p);

				int spaceKey = (int)Xlib.XKeysymToKeycode(XDisplay, (KeySym)0xFF61);

				if ((p[spaceKey / 8] & (0x1 << (spaceKey % 8))) != 0)
				{
					Pressed?.Invoke(this, EventArgs.Empty);
					return FilterReturn.Continue;
				}
			}

			Xlib.XGetInputFocus(XDisplay, out var focus, out _);

			XKeyEvent k = new XKeyEvent
			{
				display = XDisplay,
				window = focus,
				root = (X11.Window)rootWin.Handle,
				subwindow = 0,
				time = 0,
				x = 1,
				y = 1,
				x_root = 1,
				y_root = 1,
				same_screen = true,
				type = xKeyEvent.type == KeyPress ? KeyPress : KeyRelease,
				keycode = (uint)keycode,
				state = (uint)modifiers
			};

			Xlib.XSendEvent(XDisplay, focus, true, k.type, (IntPtr)(&k));

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
	}
}