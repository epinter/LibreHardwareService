
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Hardware.Storage;
using LibreHardwareService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowSensors
{
	internal class HardwareUpdateVisitor : IVisitor
	{

		public void VisitComputer(IComputer computer)
		{
			computer.Traverse(this);
		}
		public void VisitHardware(IHardware hardware)
		{
			try
			{
				hardware.Update();
				foreach (IHardware subHardware in hardware.SubHardware)
				{
					subHardware.Accept(this);
				}
			}
			catch (Exception)
			{
				//ignored
			}
		}
		public void VisitSensor(ISensor sensor) { }
		public void VisitParameter(IParameter parameter) { }
	}

	internal class Program
	{
		static void Main(string[] args)
		{
			Computer computer = new Computer
			{
				IsCpuEnabled = true,
				IsGpuEnabled = true,
				IsBatteryEnabled = true,
				IsPsuEnabled = true,
				IsMemoryEnabled = true,
				IsMotherboardEnabled = true,
				IsControllerEnabled = true,
				IsNetworkEnabled = true,
				IsStorageEnabled = true,
			};
			HardwareUpdateVisitor updateVisitor = new HardwareUpdateVisitor();
			computer.Open();
			computer.Accept(updateVisitor);

			foreach (IHardware h in computer.Hardware)
			{
				Console.WriteLine("Hardware: name='{0}'; type='{1}'; identifier='{2}';", h.Name, h.HardwareType, h.Identifier);

				if (h.HardwareType.Equals(HardwareType.Storage))
				{
					if (h is AtaStorage)
					{
						List<DataSmartAttribute> attrList = new List<DataSmartAttribute>();
						HwStatusInfo hwStatus = new HwStatusInfo
						{
							Identifier = h.Identifier.ToString(),
							Name = h.Name,
							HardwareType = h.HardwareType.ToString(),
							HwStatusType = HwStatusType.STORAGE_SMART_ATA,
						};
						AtaStorage storage = (AtaStorage)h;
						LibreHardwareMonitor.Interop.Kernel32.SMART_ATTRIBUTE[] attrs = storage.Smart.ReadSmartData();
						LibreHardwareMonitor.Interop.Kernel32.SMART_THRESHOLD[] thresholds = storage.Smart.ReadSmartThresholds();

						foreach (LibreHardwareMonitor.Interop.Kernel32.SMART_ATTRIBUTE a in attrs)
						{
							if (a.Id == 0x00)
							{
								break;
							}

							SmartAttribute attr = storage.SmartAttributes.FirstOrDefault(s => s.Id == a.Id);
							string attrName = "Unknown";
							if (attr != null)
							{
								attrName = attr.Name;
							}

							byte threshold = 0;
							foreach (LibreHardwareMonitor.Interop.Kernel32.SMART_THRESHOLD t in thresholds)
							{
								if (t.Id == a.Id)
								{
									threshold = t.Threshold;
								}
							}
							DataSmartAttribute d = new DataSmartAttribute
							{
								Id = a.Id,
								Name = attrName,
								Threshold = threshold,
								Flags = a.Flags,
								RawValue = new List<Byte>(a.RawValue),
								CurrentValue = a.CurrentValue,
								WorstValue = a.WorstValue,
							};
							Console.WriteLine("\t\tsmart-attribute: [[name:'{0}'; id:'{1}'; rawValue:'{2}';\n\t\t\tcurrentValue:'{3}'; threshold:'{4}'; worst:'{5}'; prefail:'{6}'; advisory:'{7}';]]",
								d.Name, "0x"+Convert.ToString(d.Id, 16), BitConverter.ToString(a.RawValue), d.CurrentValue, d.Threshold, d.WorstValue, d.IsPreFail, d.IsAdvisory);

						}
					}
					else if (h is NVMeGeneric)
					{
						NVMeGeneric n = (NVMeGeneric)h;
						NVMeHealthInfo nh = n.Smart.GetHealthInfo();
						HwStatusInfo hwStatus = new HwStatusInfo
						{
							Identifier = h.Identifier.ToString(),
							Name = h.Name,
							HardwareType = h.HardwareType.ToString(),
							HwStatusType = HwStatusType.STORAGE_SMART_NVME
						};
						DataNvmeSmart nvmeSmart = new DataNvmeSmart
						{
							AvailableSpare = nh.AvailableSpare,
							AvailableSpareThreshold = nh.AvailableSpareThreshold,
							ControllerBusyTime = nh.ControllerBusyTime,
							CriticalCompositeTemperatureTime = nh.CriticalCompositeTemperatureTime,
							CriticalWarning = (byte)nh.CriticalWarning,
							DataUnitRead = nh.DataUnitRead,
							DataUnitWritten = nh.DataUnitWritten,
							ErrorInfoLogEntryCount = nh.ErrorInfoLogEntryCount,
							HostReadCommands = nh.HostReadCommands,
							HostWriteCommands = nh.HostWriteCommands,
							MediaErrors = nh.MediaErrors,
							PercentageUsed = nh.PercentageUsed,
							PowerCycle = nh.PowerCycle,
							PowerOnHours = nh.PowerOnHours,
							Temperature = nh.Temperature,
							TemperatureSensors = nh.TemperatureSensors,
							UnsafeShutdowns = nh.UnsafeShutdowns,
							WarningCompositeTemperatureTime = nh.WarningCompositeTemperatureTime
						};
						Console.WriteLine("\tsmart-attribute: {0}", nvmeSmart);

					}
				}

                foreach (IHardware sh in h.SubHardware)
				{
					Console.WriteLine("\tSubHardware: name='{0}'; type='{1}'; identifier='{2}';", sh.Name, sh.HardwareType, sh.Identifier);
					foreach (ISensor s in sh.Sensors)
					{
						Console.WriteLine("\t\tSensor: name='{0}'; value='{1}'; type='{2}'; identifier='{3}';", s.Name, s.Value, s.SensorType, s.Identifier);
					}

				};
				foreach (ISensor s in h.Sensors)
				{
					Console.WriteLine("\tSensor: name='{0}'; value='{1}'; type='{2}'; identifier='{3}';", s.Name, s.Value, s.SensorType, s.Identifier);
				}
			}
			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}
	}
}
