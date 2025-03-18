
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;

namespace LibreHardwareService {
    internal class Log {
        internal static ILoggerFactory loggerFactory = new LoggerFactory();
        internal static ILogger createLogger<T>() => loggerFactory.CreateLogger<T>();
        internal static ILogger createLogger(string categoryName) => loggerFactory.CreateLogger(categoryName);
        private static ILogger logger = createLogger("LibreHardwareService");

        public static string eventLogSource = "LibreHardwareService";

#pragma warning disable CA1416  // Validate platform compatibility
        public static void info(string message, params object[] args) {
            logger.LogInformation(String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Information);
        }

        public static void error(Exception? exception, string message, params object[] args) {
            logger.LogError(exception, String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Error);
        }

        public static void error(string message, params object[] args) {
            logger.LogError(String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Error);
        }

        public static void warning(string message, params object[] args) {
            logger.LogWarning(String.Format(message, args));
            EventLog.WriteEntry(eventLogSource, string.Format(message, args), EventLogEntryType.Warning);
        }
#pragma warning restore CA1416  // Validate platform compatibility
    }
}
