
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace LibreHardwareService
{
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			if (Environment.UserInteractive && args.Length == 1 && args[0] != null)
			{
				if (args[0] == "--install")
				{
					ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
				}
				else if (args[0] == "--uninstall")
				{
					ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
				}
				return;
			}

			if (Environment.UserInteractive && Debugger.IsAttached)
			{
				Debug.WriteLine("STARTING IN DEBUG MODE");
				LibreHardwareService debugService = new LibreHardwareService { IsDebug = true, Interval = 1000 };
				debugService.StartService();
				Thread.Sleep(3000000);
				return;
			}

			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[]
			{
				new LibreHardwareService()
			};
			ServiceBase.Run(ServicesToRun);
		}
	}
}
