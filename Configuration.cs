using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace Sentinel
{
	internal static class Configuration
	{
		internal static string SavePath = null!;
		internal const string TimeFormat = "dd-MM-yyyy_HH:mm:ss";

		internal static void Initialize()
		{
			var configuration = new ConfigurationBuilder().AddJsonFile("config.json").Build();

			var envHome = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "HOMEPATH" : "HOME";
			var home = Environment.GetEnvironmentVariable(envHome);
			SavePath = $"{home}/Sentinel/Screenshots/";

			if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
		}
	}
}