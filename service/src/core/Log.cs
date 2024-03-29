
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

namespace LibreHardwareService {
    internal class Log {
        internal static ILoggerFactory LoggerFactory = new LoggerFactory();
        internal static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
        internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
        private static ILogger logger = CreateLogger("LibreHardwareService");

        public static string eventLogSource = "LibreHardwareService";

#pragma warning disable CA1416  // Validate platform compatibility
        public static void Info(string message, params object[] args) {
            logger.LogInformation(String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Information);
        }

        public static void Error(string message, params object[] args) {
            logger.LogError(String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Error);
        }

        public static void Warning(string message, params object[] args) {
            logger.LogWarning(String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Warning);
        }
#pragma warning restore CA1416  // Validate platform compatibility
    }
}
