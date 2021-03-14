using System;
using System.Runtime.InteropServices;
using Gdk;

namespace Sentinel
{
	public struct X11Window
	{
		public readonly IntPtr Handle;

		public string GetName(IntPtr display)
		{
			X11Wrapper.XFetchName(display, Handle, out string name);
			return name;
		}
	}

	public static class X11Wrapper
	{
		private const string Library = "libX11.so";

		[DllImport(Library)]
		public static extern IntPtr XDefaultRootWindow(IntPtr display);

		[DllImport(Library)]
		public static extern IntPtr XOpenDisplay(IntPtr name);

		[DllImport(Library)]
		public static extern void XFree(IntPtr data);

		[DllImport(Library)]
		public static extern unsafe int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return, out IntPtr parent_return, out X11Window* children_return, out uint nchildren_return);

		[DllImport(Library)]
		public static extern int XFetchName(IntPtr display, IntPtr w, out string window_name_return);

		[DllImport(Library)]
		public static extern int XGetGeometry(IntPtr display, IntPtr window, out IntPtr root, out int x, out int y, out uint width, out uint height, out uint border, out uint depth);

		[DllImport(Library)]
		public static extern bool XTranslateCoordinates(IntPtr display, IntPtr sourceWindow, IntPtr destinationWindow, int srcX, int srcY, out int destX, out int destY, out X11Window child);

		[DllImport("libgdk-3.so.0")]
		public static extern ulong gdk_x11_window_get_xid(IntPtr window);

		[DllImport("libgdk-3.so.0")]
		public static extern IntPtr gdk_x11_window_foreign_new_for_display(IntPtr display, X11.Window window);
	}
}