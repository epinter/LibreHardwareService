
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareService
{
	internal class Log
	{
		public static string eventLogSource = "LibreHardwareService";

		public static void Info(string message, params object[] args)
		{
			Info(String.Format(message, args));
		}

		public static void Error(string message, params object[] args)
		{
			Error(String.Format(message, args));
		}

		public static void Warning(string message, params object[] args)
		{
			Warning(String.Format(message, args));
		}

		public static void Info(string message)
		{
			EventLog.WriteEntry(eventLogSource, String.Format(message), EventLogEntryType.Information);
		}

		public static void Error(string message)
		{
			EventLog.WriteEntry(eventLogSource, String.Format(message), EventLogEntryType.Error);
		}

		public static void Warning(string message)
		{
			EventLog.WriteEntry(eventLogSource, String.Format(message), EventLogEntryType.Warning);
		}

	}
}
