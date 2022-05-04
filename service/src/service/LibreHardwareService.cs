
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace LibreHardwareService
{
	public enum ServiceState
	{
		SERVICE_STOPPED = 0x00000001,
		SERVICE_START_PENDING = 0x00000002,
		SERVICE_STOP_PENDING = 0x00000003,
		SERVICE_RUNNING = 0x00000004,
		SERVICE_CONTINUE_PENDING = 0x00000005,
		SERVICE_PAUSE_PENDING = 0x00000006,
		SERVICE_PAUSED = 0x00000007,
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ServiceStatus
	{
		public int dwServiceType;
		public ServiceState dwCurrentState;
		public int dwControlsAccepted;
		public int dwWin32ExitCode;
		public int dwServiceSpecificExitCode;
		public int dwCheckPoint;
		public int dwWaitHint;
	};

	public partial class LibreHardwareService : ServiceBase
	{
		private readonly BackgroundWorker worker;
		private readonly SensorsManager sensorsManager;
		private readonly Timer timer;
		private int interval = 1000;

		private bool debug;

		public bool IsDebug
		{
			get
			{
				return debug;
			}
			internal set
			{
				sensorsManager.IsDebug = true;
				debug = true;
			}
		}

		public int Interval
		{
			get { return interval; }
			internal set { interval = value; }
		}

		public LibreHardwareService()
		{
			InitializeComponent();
			eventLog.Source = Log.eventLogSource;
			if (!EventLog.SourceExists(Log.eventLogSource))
			{
				EventLog.CreateEventSource(Log.eventLogSource, eventLog.Log);
			}

			sensorsManager = new SensorsManager();
			worker = new BackgroundWorker();
			timer = new Timer();
			worker.WorkerSupportsCancellation = true;
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

		protected override void OnStart(string[] args)
		{
			// Update the service state to Start Pending.
			ServiceStatus serviceStatus = new ServiceStatus
			{
				dwCurrentState = ServiceState.SERVICE_START_PENDING,
				dwWaitHint = 10000
			};
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);

			StartService();

			// Update the service state to Running.
			serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);
		}

		public void StartService()
		{
			if (IsDebug)
			{
				Debug.WriteLine("{0}: DEBUG ENABLED, Interval set to {1}ms", ServiceName, interval);
			} else
			{
				interval = ReadUpdateIntervalSetting();
			}
			worker.DoWork += UpdateSensors;

			worker.RunWorkerAsync();
			timer.Interval = interval;
			timer.Elapsed += new ElapsedEventHandler(OnTimeInterval);
			timer.Enabled = true;
			EventLog.WriteEntry(String.Format("Starting '{0}' with interval set to {1}ms, sensors time-window to {2} minutes", ServiceName, interval, sensorsManager.GetSensorsTimeWindow()));
		}

		protected override void OnStop()
		{
			// Update the service state to Stop Pending.
			ServiceStatus serviceStatus = new ServiceStatus
			{
				dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
				dwWaitHint = 100000
			};
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);

			if (IsDebug)
			{
				Debug.WriteLine("{0}: Stopping LibreHardwareService", ServiceName);
			}

			sensorsManager.Close();
			timer.Stop();
			timer.Dispose();
			worker?.CancelAsync();
			worker?.Dispose();

			// Update the service state to Stopped.
			serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
			SetServiceStatus(this.ServiceHandle, ref serviceStatus);
		}

		private void EventLog_EntryWritten(object sender, EntryWrittenEventArgs e)
		{

		}

		public void OnTimeInterval(object sender, ElapsedEventArgs args)
		{
			Debug.WriteLine("{0}: OnTimeInterval", ServiceName);

			if (!worker.IsBusy)
			{
				worker.RunWorkerAsync();
			}
		}

		private void UpdateSensors(object sender, DoWorkEventArgs e)
		{
			Debug.WriteLine("{0}: UpdateSensors", ServiceName);

			sensorsManager.UpdateHardwareSensors();
		}

		private int ReadUpdateIntervalSetting()
		{
			if (Config.UpdateIntervalSeconds > 0)
			{
				return Config.UpdateIntervalSeconds * 1000;
			}

			return interval;
		}
	}
}
