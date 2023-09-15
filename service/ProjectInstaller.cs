using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareService
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : System.Configuration.Install.Installer
	{
		public ProjectInstaller()
		{
			InitializeComponent();
		}

		private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
		{
			using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController(serviceInstaller1.ServiceName))
			{
				if (serviceController.CanStop && serviceController.Status != ServiceControllerStatus.Stopped)
				{
					serviceController.Stop();
					serviceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped);
				}
				serviceController.Start();
			}
		}

		private void serviceInstaller1_BeforeInstall(object sender, InstallEventArgs e)
		{
			using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController(serviceInstaller1.ServiceName))
			{
				if (serviceController.CanStop && serviceController.Status != ServiceControllerStatus.Stopped)
				{
					serviceController.Stop();
					serviceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped);
				}
			}
		}

		private void serviceInstaller1_BeforeUninstall(object sender, InstallEventArgs e)
		{
			using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController(serviceInstaller1.ServiceName))
			{
				if (serviceController.CanStop && serviceController.Status != ServiceControllerStatus.Stopped)
				{
					serviceController.Stop();
					serviceController.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped);
				}
			}
		}
	}
}
