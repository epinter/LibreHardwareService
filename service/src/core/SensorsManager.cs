
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Hardware.Storage;
using System.Diagnostics;
using System.Text;
using MessagePack;
using Microsoft.IO;
using static LibreHardwareService.ConfigHelper;

namespace LibreHardwareService {
    internal class SensorsManager {
        private readonly Computer computer;
        private readonly HardwareUpdateVisitor updateVisitor;
        private TimeSpan sensorsTimeWindow = TimeSpan.Zero;
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;
        private DateTime lastHwStatusUpdate = DateTime.MinValue;

        public bool IsDebug { get; internal set; }

        public SensorsManager() {
            computer = new Computer {
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
            updateVisitor = new HardwareUpdateVisitor();
            if (Config.SensorsTimeWindowSeconds > 0) {
                sensorsTimeWindow = TimeSpan.FromSeconds(Config.SensorsTimeWindowSeconds);
            }

            computer.Open();
            recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public TimeSpan getSensorsTimeWindow() {
            return sensorsTimeWindow;
        }

        public void updateHardwareSensors() {
            computer.Accept(updateVisitor);

            computer.Accept(new SensorVisitor(delegate (ISensor sensor) { sensor.ValuesTimeWindow = sensorsTimeWindow; }));

            if (lastHwStatusUpdate < DateTime.Now.AddMinutes(-1 * Config.HwStatusUpdateIntervalMinutes)) {
                updateHardwareStatus();
                lastHwStatusUpdate = DateTime.Now;
            }

            long lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<DataIndex> index = new List<DataIndex>();

            List<DataHardware> root = new List<DataHardware>();
            using (MemoryStream stream = recyclableMemoryStreamManager.GetStream()) {
                foreach (IHardware h in computer.Hardware) {
                    if(h == null) {
                        continue;
                    }
#pragma warning disable CS8601  // Possible null reference assignment.
                    DataHardware hardware = new DataHardware {
                        Name = h.Name.Trim(),
                        HardwareType = h.HardwareType.ToString(),
                        Identifier = h.Identifier.ToString(),
                        Parent = h.Parent?.Identifier.ToString(),
                        Sensors = new List<DataSensor>(),
                        SubHardware = new List<DataHardware>()
                    };
                    if (Config.FeatureEnableMemoryMapAllHardwareData) {
                        root.Add(hardware);
                        byte[] hardwareData = new byte[0];
                        hardwareData = Utf8Json.JsonSerializer.Serialize(hardware);
                    }
                    foreach (IHardware sh in h.SubHardware) {
                        if(sh == null) {
                            continue;
                        }
                        DataHardware subHardware = new DataHardware {
                            Name = sh.Name.Trim(),
                            HardwareType = sh.HardwareType.ToString(),
                            Identifier = sh.Identifier.ToString(),
                            Parent = sh.Parent?.Identifier.ToString(),
                            Sensors = new List<DataSensor>(),
                            SubHardware = new List<DataHardware>()
                        };
#pragma warning restore CS8601  // Possible null reference assignment.
                        if (Config.FeatureEnableMemoryMapAllHardwareData) {
                            hardware.SubHardware.Add(subHardware);

                            byte[] subHardwareData = new byte[0];

                            subHardwareData = Utf8Json.JsonSerializer.Serialize(subHardware);
                        }
                        foreach (ISensor s in sh.Sensors) {
                            if(s == null) {
                                continue;
                            }
#pragma warning disable CS8629  // Nullable value type may be null.
                            DataSensor sensor = new DataSensor {
                                Name = s.Name.Trim(),
                                HardwareId = subHardware.Identifier.ToString(),
                                HardwareName = subHardware.Name,
                                HardwareType = subHardware.HardwareType.ToString(),
                                Identifier = s.Identifier.ToString(),
                                SensorType = s.SensorType.ToString(),
                                Value = (s.Value != null && !float.IsNaN((float)s.Value)) ? (float)s.Value : 0.0f,
                                Max = (s.Max != null && !float.IsNaN((float)s.Max)) ? (float)s.Max : 0.0f,
                                Min = (s.Min != null && !float.IsNaN((float)s.Min))? (float)s.Min : 0.0f,
                                ValuesTimeWindow = s.ValuesTimeWindow.TotalSeconds,
                                Values = fromHardwareSensorValue(s.Values)
                            };
#pragma warning restore CS8629  // Nullable value type may be null.
                            byte[] sensorData = new byte[0];
                            if (Config.FeatureEnableMemoryMapAllHardwareData) {
                                subHardware.Sensors.Add(sensor);
                            }
                            sensorData = Utf8Json.JsonSerializer.Serialize(sensor);
                            int offset = (int)stream.Position;
                            index.Add(new DataIndex {
                                Identifier = s.Identifier.ToString(),
                                Size = sensorData.Length,
                                Offset = offset,
                                SensorName = s.Name.Trim(),
                                SensorType = s.SensorType.ToString(),
                                HardwareName = s.Hardware.Name.Trim()
                            });
                            stream.Write(sensorData, 0, sensorData.Length);
                            stream.WriteByte(0);
                        }
                    }

                    foreach (ISensor s in h.Sensors) {
                        try {
                            DataSensor sensor = new DataSensor {
                                Name = s.Name,
                                HardwareId = hardware.Identifier.ToString(),
                                HardwareName = hardware.Name,
                                HardwareType = hardware.HardwareType.ToString(),
                                Identifier = s.Identifier.ToString(),
                                SensorType = s.SensorType.ToString(),
                                Value = (s.Value != null && !float.IsNaN((float)s.Value)) ? (float)s.Value : 0.0f,
                                Max = (s.Max != null && !float.IsNaN((float)s.Max)) ? (float)s.Max : 0.0f,
                                Min = (s.Min != null && !float.IsNaN((float)s.Min))? (float)s.Min : 0.0f,
                                ValuesTimeWindow = s.ValuesTimeWindow.TotalSeconds,
                                Values = fromHardwareSensorValue(s.Values)
                            };

                            byte[] sensorData = new byte[0];

                            if (Config.FeatureEnableMemoryMapAllHardwareData) {
                                hardware.Sensors.Add(sensor);
                            }
                            sensorData = Utf8Json.JsonSerializer.Serialize(sensor);
                            int offset = (int)stream.Position;
                            index.Add(new DataIndex {
                                Identifier = s.Identifier.ToString(),
                                Size = sensorData.Length,
                                Offset = offset,
                                SensorName = s.Name.Trim(),
                                SensorType = s.SensorType.ToString(),
                                HardwareName = s.Hardware.Name.Trim()
                            });
                            stream.Write(sensorData, 0, sensorData.Length);
                            stream.WriteByte(0);
                        } catch (Exception ex) {
                            if (IsDebug)
                                Debug.WriteLine(ex.ToString());
                            throw;
                        }
                    }
                }
                byte[] indexBytes = new byte[0];
                if (Config.IndexFormat == 1) {
                    indexBytes = Utf8Json.JsonSerializer.Serialize(index);
                } else if (Config.IndexFormat == 2) {
                    indexBytes = MessagePackSerializer.Serialize(index);
                }

                byte[] dataBytes = stream.ToArray();
                if (IsDebug) {
                    Console.WriteLine(" WRITING INDEX ---------- {0} bytes -- {1} ", indexBytes.Length,
                                      Utf8Json.JsonSerializer.PrettyPrint(indexBytes));
                    Console.WriteLine(" WRITING DATA ---------- {0} bytes -- {1} ", dataBytes.Length,
                                      Encoding.UTF8.GetString(dataBytes));
                }

                Metadata m = new Metadata {
                    UpdateInterval = Config.UpdateIntervalMilliseconds,
                    LastUpdate = lastUpdate,
                };
                MemoryMappedSensors.instance.writeSensors(indexBytes, dataBytes, m);

                if (Config.FeatureEnableMemoryMapAllHardwareData) {
                    byte[] rootBytes = Utf8Json.JsonSerializer.Serialize(root);
                    MemoryMappedSensors.instance.writeHardware(rootBytes, m);
                    if (IsDebug) {
                        Console.WriteLine(" WRITING ROOT ---------- {0} bytes -- {1} ", rootBytes.Length,
                                          Encoding.UTF8.GetString(rootBytes));
                    }
                }
            }
        }

        List<DataSensorValue> fromHardwareSensorValue(IEnumerable<LibreHardwareMonitor.Hardware.SensorValue> from) {
            List<DataSensorValue> ret = new List<DataSensorValue>();
            foreach (LibreHardwareMonitor.Hardware.SensorValue fh in from) {
                ret.Add(new DataSensorValue {
                    Value = fh.Value,
                    Time = fh.Time,
                });
            }

            return ret;
        }

        public void updateHardwareStatus() {
            long lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (MemoryStream stream = recyclableMemoryStreamManager.GetStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    foreach (IHardware h in computer.Hardware) {
                        if(h == null) {
                            continue;
                        }
                        if (h.HardwareType.Equals(HardwareType.Storage) && h is StorageDevice storageDevice) {
                            bool isNvme = storageDevice.Storage.IsNVMe;

                            if (!isNvme) {
                                List<DataSmartAttribute> attrList = new List<DataSmartAttribute>();
                                HwStatusInfo hwStatus = new HwStatusInfo {
                                    Identifier = h.Identifier.ToString(),
                                    Name = h.Name.Trim(),
                                    HardwareType = h.HardwareType.ToString(),
                                    HwStatusType = HwStatusType.STORAGE_SMART_ATA,
                                };

                                foreach (var sa in storageDevice.Attributes) {
                                    if (sa.Id == 0x00) continue;
                                    var raw = sa.Attribute?.Attribute;

                                    attrList.Add(new DataSmartAttribute {
                                        Id = sa.Id,
                                        Name = sa.Name ?? "Unknown",
                                        Threshold = raw?.Threshold ?? sa.Threshold,
                                        Flags = raw?.StatusFlags ?? 0,
                                        RawValue = raw.HasValue && raw.Value.RawValue != null
                                            ? new List<Byte>(raw.Value.RawValue.Take(6))
                                            : new List<Byte>(BitConverter.GetBytes((ulong)sa.Value).Take(6)),
                                        CurrentValue = raw?.CurrentValue ?? 0,
                                        WorstValue = raw?.WorstValue ?? 0,
                                    });
                                }

                                byte[] hwStatusInfo = Utf8Json.JsonSerializer.Serialize(hwStatus);
                                byte[] hwStatusData = Utf8Json.JsonSerializer.Serialize(attrList);
                                writer.Write(8 + hwStatusInfo.Length + 4 + hwStatusData.Length + 1);
                                writer.Write((int)hwStatusInfo.Length);
                                writer.Write((int)hwStatus.HwStatusType);
                                writer.Write(hwStatusInfo);
                                writer.Write((int)hwStatusData.Length);
                                writer.Write(hwStatusData);
                                writer.Write((byte)0);
                                if (IsDebug) {
                                    Console.WriteLine(Utf8Json.JsonSerializer.PrettyPrint(hwStatusData));
                                }
                            } else {
                                var smart = storageDevice.Storage?.Smart;
                                if (smart == null) continue;
                                HwStatusInfo hwStatus = new HwStatusInfo {
                                    Identifier = h.Identifier.ToString(),
                                    Name = h.Name.Trim(),
                                    HardwareType = h.HardwareType.ToString(),
                                    HwStatusType = HwStatusType.STORAGE_SMART_NVME
                                };

                                short temp = (short)(smart.Temperature ?? 0);

                                // Map NVMe attributes from unified SMART list
                                byte availableSpare = 0, availableSpareThreshold = 0, percentageUsed = 0, criticalWarning = 0;
                                ulong hostReadCommands = 0, hostWriteCommands = 0;
                                ulong unsafeShutdowns = 0, mediaErrors = 0, errorInfoLogEntryCount = 0;
                                ulong controllerBusyTime = 0;
                                uint warningTempTime = (uint)(smart.TemperatureWarning ?? 0);
                                uint criticalTempTime = (uint)(smart.TemperatureCritical ?? 0);

                                foreach (var attr in storageDevice.Attributes) {
                                    var name = attr.Name?.ToLowerInvariant() ?? "";
                                    var raw = (ulong)attr.Value;
                                    if (name.Contains("available spare") && !name.Contains("threshold"))
                                        availableSpare = (byte)Math.Min(raw, 255);
                                    else if (name.Contains("available spare") && name.Contains("threshold"))
                                        availableSpareThreshold = (byte)Math.Min(raw, 255);
                                    else if (name.Contains("percentage used"))
                                        percentageUsed = (byte)Math.Min(raw, 255);
                                    else if (name.Contains("critical warning"))
                                        criticalWarning = (byte)raw;
                                    else if (name.Contains("host read"))
                                        hostReadCommands = raw;
                                    else if (name.Contains("host write"))
                                        hostWriteCommands = raw;
                                    else if (name.Contains("unsafe shutdown"))
                                        unsafeShutdowns = raw;
                                    else if (name.Contains("media") && name.Contains("error"))
                                        mediaErrors = raw;
                                    else if (name.Contains("error") && name.Contains("log"))
                                        errorInfoLogEntryCount = raw;
                                    else if (name.Contains("controller busy"))
                                        controllerBusyTime = raw;
                                }

                                DataNvmeSmart nvmeSmart = new DataNvmeSmart {
                                    AvailableSpare = availableSpare,
                                    AvailableSpareThreshold = availableSpareThreshold,
                                    ControllerBusyTime = controllerBusyTime,
                                    CriticalCompositeTemperatureTime = criticalTempTime,
                                    CriticalWarning = criticalWarning,
                                    DataUnitRead = smart.HostReads ?? 0,
                                    DataUnitWritten = smart.HostWrites ?? 0,
                                    ErrorInfoLogEntryCount = errorInfoLogEntryCount,
                                    HostReadCommands = hostReadCommands,
                                    HostWriteCommands = hostWriteCommands,
                                    MediaErrors = mediaErrors,
                                    PercentageUsed = percentageUsed,
                                    PowerCycle = smart.PowerOnCount,
                                    PowerOnHours = smart.DetectedPowerOnHours,
                                    Temperature = temp,
                                    TemperatureSensors = temp != 0
                                        ? new short[] { temp }
                                        : Array.Empty<short>(),
                                    UnsafeShutdowns = unsafeShutdowns,
                                    WarningCompositeTemperatureTime = warningTempTime
                                };
                                byte[] hwStatusInfo = Utf8Json.JsonSerializer.Serialize(hwStatus);
                                byte[] hwStatusData = Utf8Json.JsonSerializer.Serialize(nvmeSmart);
                                writer.Write(8 + hwStatusInfo.Length + 4 + hwStatusData.Length + 1);
                                writer.Write((int)hwStatusInfo.Length);
                                writer.Write((int)hwStatus.HwStatusType);
                                writer.Write(hwStatusInfo);
                                writer.Write((int)hwStatusData.Length);
                                writer.Write(hwStatusData);
                                writer.Write((byte)0);
                            }
                        }
                    }
                    Metadata m = new Metadata {
                        UpdateInterval = Config.HwStatusUpdateIntervalMinutes * 60,
                        LastUpdate = lastUpdate,
                    };

                    MemoryMappedSensors.instance.writeStatus(stream.ToArray(), m);
                }
            }
        }

        public void close() {
            try {
                computer.Close();
            } catch (Exception) { }
        }
    }
}
