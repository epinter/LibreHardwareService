
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace LibreHardwareService {
    internal static class ConfigHelper {
#pragma warning disable CS8618
        private static IConfiguration configuration;
#pragma warning restore CS8618
        public static class Config {
            public static void initialize(IConfiguration configuration) {
                ConfigHelper.configuration = configuration;
            }

            // public Config(IConfiguration configuration) {
            //     this.configuration = configuration;
            // }

            /// <summary>
            /// Format of the index that is written on the memory map
            /// 1 = JSON (easier to parse and highly compatible)
            /// 2 - MessagePack (best performance)
            /// </summary>
            public static int IndexFormat => configuration.GetValue<int>("Settings:indexFormat");

            /// <summary>
            /// Minimum interval (minutes) to log warning when data written is above limit. To avoid logspam.
            /// </summary>
            public static int MemoryMapLimitLogIntervalMinutes =>
                configuration.GetValue<int>("Settings:memoryMapLimitLogIntervalMinutes");

            /// <summary>
            /// Time interval in milliseconds to collect the sensor data and write to memory map.
            /// </summary>
            public static int UpdateIntervalMilliseconds => configuration.GetValue<int>("Settings:updateIntervalMilliseconds");

            /// <summary>
            /// Time interval in minutes to collect the hardware status (like storage smart attributes) and write to memory map.
            /// </summary>
            public static int HwStatusUpdateIntervalMinutes =>
                configuration.GetValue<int>("Settings:hwStatusUpdateIntervalMinutes");

            /// <summary>
            /// Time window to keep sensor values. The number of values kept in memory will increase when the window is increased.
            /// Increases CPU usage and memory usage.
            /// </summary>
            public static int SensorsTimeWindowSeconds => configuration.GetValue<int>("Settings:sensorsTimeWindowSeconds");

            /// <summary>
            /// Limit of the memory map. The default is 1MB.
            /// </summary>
            public static int MemoryMapLimitKb => configuration.GetValue<int>("Settings:memoryMapLimitKb");

            public static bool FeatureEnableMemoryMapAllHardwareData =>
                configuration.GetValue<bool>("Settings:Feature:featureEnableMemoryMapAllHardwareData");
        }
    }
}
