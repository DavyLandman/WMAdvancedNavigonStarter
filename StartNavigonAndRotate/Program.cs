using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.WindowsCE.Forms;
using Microsoft.Win32;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Threading;


namespace StartNavigonAndRotate
{
	class Program
	{
		static void Main(string[] args)
		{
			Boolean shouldRotate = MessageBox.Show("Do you want to rotate the screen?", "Rotate screen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes;
			Boolean useExternalGPS = MessageBox.Show("Do you want to use external GPS?", "External GPS", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes;
			Boolean blueToothEnabled = GetBluetoothStatus() != RadioMode.Off;
			try
			{
				if (isManilaPresent())
				{
					StopManila();
				}
				if (isTitaniumPresent())
				{
					StopTitanium();
				}
				if (!blueToothEnabled && useExternalGPS)
				{
					SetBluetoothStatus(RadioMode.Connectable);
				}
				SetNavigonComPort(useExternalGPS ? 8 : 4);
				if (shouldRotate)
				{
					SystemSettings.ScreenOrientation = ScreenOrientation.Angle270;
				}
				using (var proc = Process.Start("/Internal Storage/Navigon/MobileNavigator.exe", ""))
				{
					while (!proc.WaitForExit(1000)) ;
				}
			}
			finally
			{
				if (shouldRotate)
				{
					SystemSettings.ScreenOrientation = ScreenOrientation.Angle0;
				}
				if (isManilaPresent())
				{
					StartManila();
				}
				if (isTitaniumPresent())
				{
					StartTitanium();
				}
				if (!blueToothEnabled && useExternalGPS)
				{
					SetBluetoothStatus(RadioMode.Off);
				}
			}
		}
		private static String userSettingsFile = @"\Internal Storage\Navigon\Settings\UserSettings.xml";

		private static void SetNavigonComPort(int newPort)
		{
			//Internal Storage\Navigon\Settings\UserSettings.xml
			var settingsFile = OpenSettingsFile();
			var gpsPortNode = settingsFile.SelectNodes("/Settings/GPS/GPSPort");
			if (gpsPortNode.Count == 1)
			{
				gpsPortNode[0].InnerText = newPort.ToString();
			}
			SaveSettingsFile(settingsFile);
		}

		private static void SaveSettingsFile(XmlDocument settingsFile)
		{
			var newFileContents = new MemoryStream();
			settingsFile.Save(newFileContents);
			using (var newFile = new FileStream(userSettingsFile, FileMode.Create))
			{
				var data = HeaderFromUTFtoISO(newFileContents).ToArray();
				newFile.Write(data, 0, data.Length);
				newFile.Close();
			}
			newFileContents.Close();
		}

		private static XmlDocument OpenSettingsFile()
		{
			var settingsFile = new XmlDocument();
			File.Copy(userSettingsFile, userSettingsFile + ".bak", true);
			var inputFile = new FileStream(userSettingsFile, FileMode.Open);
			var fixedStream = HeaderFromISOToUTF(inputFile);
			inputFile.Close();
			settingsFile.Load(fixedStream);
			return settingsFile;
		}
		/// <summary>
		/// Navigon XML file is encoded in ISO-8859-1, but CF doesn't like that, so this function creates a in memory modification to that header.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static Stream HeaderFromISOToUTF(Stream input)
		{
			//ISO-8859-1 -> UTF-8
			var result = new StringBuilder();
			input.Seek(0, SeekOrigin.Begin);
			var reader = new StreamReader(input);
			var firstLine = reader.ReadLine();
			firstLine = firstLine.Replace("ISO-8859-1", "UTF-8");
			result.AppendLine(firstLine);
			result.Append(reader.ReadToEnd());
			return new MemoryStream(UTF8Encoding.UTF8.GetBytes(result.ToString()));
		}

		private static MemoryStream HeaderFromUTFtoISO(Stream input)
		{
			//UTF-8 -> ISO-8859-1
			var result = new MemoryStream();
			input.Seek(0, SeekOrigin.Begin);
			var reader = new StreamReader(input);
			var writer = new StreamWriter(result);
			var firstLine = reader.ReadLine();
			firstLine = firstLine.Replace("UTF-8", "ISO-8859-1");
			writer.WriteLine(firstLine);
			writer.Write(reader.ReadToEnd());
			writer.Flush();
			return new MemoryStream(result.ToArray());

		}


		public enum RadioMode
		{
			Off = 0,
			Connectable = 1,
			Discoverable = 2
		}

		[DllImport("BthUtil.dll")]
		public static extern int BthGetMode(ref RadioMode dwMode);

		[DllImport("BthUtil.dll")]
		public static extern int BthSetMode(RadioMode dwMode);

		private static RadioMode GetBluetoothStatus()
		{
			RadioMode result = RadioMode.Off;
			BthGetMode(ref result);
			return result;
		}

		private static void SetBluetoothStatus(RadioMode newMode)
		{
			BthSetMode(newMode);
		}

		public const int HWND_BROADCAST = 0xffff;
		public const int WM_WININICHANGE = 0x001A;

		[DllImport("coredll.dll")]
		private static extern int PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

		private static String titaniumLocation = @"Software\Microsoft\Today\Items\" + "\"Windows Default\"";

		private static Boolean isTitaniumPresent()
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\CHome", false);
				return true;
			}
			catch { return false; }

		}

		private static Boolean titaniumWasRunning = false;
		private static void StopTitanium()
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(titaniumLocation, true);
				titaniumWasRunning = ((int)key.GetValue("Enabled")) == 1;
				key.SetValue("Enabled", 0);

				PostMessage((IntPtr)HWND_BROADCAST, WM_WININICHANGE, 0xF2, 0);
			}
			catch { }
		}
		private static void StartTitanium()
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(titaniumLocation, true);
				if (titaniumWasRunning)
				{
					key.SetValue("Enabled", 1);
				}

				PostMessage((IntPtr)HWND_BROADCAST, WM_WININICHANGE, 0xF2, 0);
			}
			catch { }
		}


		private static Boolean isManilaPresent()
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Today\Items\HTC Sense", false);
				return true;
			}
			catch { return false; }
		}


		private static Boolean manilaWasRunning = false;
		private static void StopManila()
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Today\Items\HTC Sense", true);
				manilaWasRunning = ((int)key.GetValue("Enabled")) == 1;
				key.SetValue("Enabled", 0);

				PostMessage((IntPtr)HWND_BROADCAST, WM_WININICHANGE, 0xF2, 0);
			}
			catch { }
		}
		private static void StartManila()
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Today\Items\HTC Sense", true);
				if (manilaWasRunning)
				{
					key.SetValue("Enabled", 1);
				}

				PostMessage((IntPtr)HWND_BROADCAST, WM_WININICHANGE, 0xF2, 0);
			}
			catch { }
		}

	}

}
