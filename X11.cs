using System;
using System.Runtime.InteropServices;
using X11;

namespace Sentinel
{
	public static class X11Wrapper
	{
		[DllImport("libgdk-3.so.0")]
		public static extern ulong gdk_x11_window_get_xid(IntPtr window);

		[DllImport("libgdk-3.so.0")]
		public static extern IntPtr gdk_x11_window_foreign_new_for_display(IntPtr display, Window window);
	}
}