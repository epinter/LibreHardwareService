
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareService
{
	internal class Config
	{
		/// <summary>
		/// Format of the index that is written on the memory map
		/// 1 = JSON (easier to parse and highly compatible)
		/// 2 - MessagePack (best performance)
		/// </summary>
		internal static int IndexFormat
		{
			get
			{
				return Properties.Settings.Default.indexFormat;
			}
		}

		/// <summary>
		/// Minimum interval (minutes) to log warning when data written is above limit. To avoid logspam.
		/// </summary>
		internal static int MemoryMapLimitLogIntervalMinutes { get
			{
				return Properties.Settings.Default.memoryMapLimitLogIntervalMinutes;
			}
		}

		/// <summary>
		/// Time interval in seconds to collect the sensor data and write to memory map.
		/// </summary>
		internal static int UpdateIntervalSeconds
		{
			get
			{
				return Properties.Settings.Default.updateIntervalSeconds;
			}
		}

		/// <summary>
		/// Time interval in minutes to collect the hardware status (like storage smart attributes) and write to memory map.
		/// </summary>
		internal static int HwStatusUpdateIntervalMinutes
		{
			get
			{
				return Properties.Settings.Default.hwStatusUpdateIntervalMinutes;
			}
		}

		/// <summary>
		/// Time window to keep sensor values. The number of values kept in memory will increase when the window is increased.
		/// Increases CPU usage and memory usage.
		/// </summary>
		internal static int SensorsTimeWindowSeconds
		{
			get
			{
				return Properties.Settings.Default.sensorsTimeWindowSeconds;
			}
		}

		/// <summary>
		/// Limit of the memory map. The default is 1MB.
		/// </summary>
		internal static int MemoryMapLimitKb
		{
			get
			{
				return Properties.Settings.Default.memoryMapLimitKb;
			}
		}

		internal class Feature
		{
			/// <summary>
			/// Then enabled, all hardware data tree and sensors will be sent. If disabled, only the sensors data will be written.
			/// The sensors data is sufficient for most usages. Only enable if you need the tree in json format.
			/// Increases CPU and memory usage.
			/// </summary>
			internal static bool EnableMemoryMapAllHardwareData { get {
					return Properties.Settings.Default.featureEnableMemoryMapAllHardwareData;
				}
			}
		}
	}
}
