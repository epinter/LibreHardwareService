
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Text;
using MessagePack;
using Microsoft.IO;
using LibreHardwareMonitor.Hardware.Storage;
using static LibreHardwareService.ConfigHelper;

namespace LibreHardwareService {
    internal class SensorsManager {
        private readonly Computer computer;
        private readonly HardwareUpdateVisitor updateVisitor;
        private TimeSpan sensorsTimeWindow = TimeSpan.Zero;
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;
        private DateTime lastHwStatusUpdate = DateTime.MinValue;

        public bool isDebug { get; internal set; }

        public SensorsManager() {
            computer = new Computer {
                IsCpuEnabled = true,        IsGpuEnabled = true,     IsBatteryEnabled = true,
                IsPsuEnabled = true,        IsMemoryEnabled = true,  IsMotherboardEnabled = true,
                IsControllerEnabled = true, IsNetworkEnabled = true, IsStorageEnabled = true,
            };
            updateVisitor = new HardwareUpdateVisitor();
            if (Config.SensorsTimeWindowSeconds > 0) {
                sensorsTimeWindow = TimeSpan.FromSeconds(Config.SensorsTimeWindowSeconds);
            }

            computer.Open();
            recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public int getSensorsTimeWindow() {
            return sensorsTimeWindow.Minutes;
        }

        public void updateHardwareSensors() {
            computer.Accept(updateVisitor);

            computer.Accept(new SensorVisitor(delegate(ISensor sensor) { sensor.ValuesTimeWindow = sensorsTimeWindow; }));

            if (lastHwStatusUpdate < DateTime.Now.AddMinutes(-1 * Config.HwStatusUpdateIntervalMinutes)) {
                updateHardwareStatus();
                lastHwStatusUpdate = DateTime.Now;
            }

            long lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<DataIndex> index = new List<DataIndex>();

            List<DataHardware> root = new List<DataHardware>();
            using (MemoryStream stream = recyclableMemoryStreamManager.GetStream()) {
                foreach (IHardware h in computer.Hardware) {
#pragma warning disable CS8601  // Possible null reference assignment.
                    DataHardware hardware = new DataHardware { Name = h.Name,
                                                               HardwareType = h.HardwareType.ToString(),
                                                               Identifier = h.Identifier.ToString(),
                                                               Parent = h.Parent?.Identifier.ToString(),
                                                               Sensors = new List<DataSensor>(),
                                                               SubHardware = new List<DataHardware>() };
                    if (Config.FeatureEnableMemoryMapAllHardwareData) {
                        root.Add(hardware);
                        byte[] hardwareData = new byte[0];
                        hardwareData = Utf8Json.JsonSerializer.Serialize(hardware);
                    }
                    foreach (IHardware sh in h.SubHardware) {
                        DataHardware subHardware = new DataHardware { Name = sh.Name,
                                                                      HardwareType = sh.HardwareType.ToString(),
                                                                      Identifier = sh.Identifier.ToString(),
                                                                      Parent = sh.Parent?.Identifier.ToString(),
                                                                      Sensors = new List<DataSensor>(),
                                                                      SubHardware = new List<DataHardware>() };
#pragma warning restore CS8601  // Possible null reference assignment.
                        if (Config.FeatureEnableMemoryMapAllHardwareData) {
                            hardware.SubHardware.Add(subHardware);

                            byte[] subHardwareData = new byte[0];

                            subHardwareData = Utf8Json.JsonSerializer.Serialize(subHardware);
                        }
                        foreach (ISensor s in sh.Sensors) {
#pragma warning disable CS8629  // Nullable value type may be null.
                            DataSensor sensor = new DataSensor { Name = s.Name,
                                                                 HardwareId = subHardware.Identifier.ToString(),
                                                                 HardwareName = subHardware.Name,
                                                                 HardwareType = subHardware.HardwareType.ToString(),
                                                                 Identifier = s.Identifier.ToString(),
                                                                 SensorType = s.SensorType.ToString(),
                                                                 Value = (float)s.Value,
                                                                 Max = (float)s.Max,
                                                                 Min = (float)s.Min,
                                                                 ValuesTimeWindow = s.ValuesTimeWindow.TotalSeconds,
                                                                 Values = fromHardwareSensorValue(s.Values) };
#pragma warning restore CS8629  // Nullable value type may be null.
                            byte[] sensorData = new byte[0];
                            if (Config.FeatureEnableMemoryMapAllHardwareData) {
                                subHardware.Sensors.Add(sensor);
                            }
                            sensorData = Utf8Json.JsonSerializer.Serialize(sensor);
                            int offset = (int)stream.Position;
                            index.Add(new DataIndex { Identifier = s.Identifier.ToString(), Size = sensorData.Length,
                                                      Offset = offset, SensorName = s.Name, SensorType = s.SensorType.ToString(),
                                                      HardwareName = s.Hardware.Name });
                            stream.Write(sensorData, 0, sensorData.Length);
                            stream.WriteByte(0);
                        }
                    }

                    foreach (ISensor s in h.Sensors) {
                        try {
                            DataSensor sensor = new DataSensor { Name = s.Name,
                                                                 HardwareId = hardware.Identifier.ToString(),
                                                                 HardwareName = hardware.Name,
                                                                 HardwareType = hardware.HardwareType.ToString(),
                                                                 Identifier = s.Identifier.ToString(),
                                                                 SensorType = s.SensorType.ToString(),
                                                                 Value = s.Value != null ? (float)s.Value : 0.0f,
                                                                 Max = s.Max != null ? (float)s.Max : 0.0f,
                                                                 Min = s.Min != null ? (float)s.Min : 0.0f,
                                                                 ValuesTimeWindow = s.ValuesTimeWindow.TotalSeconds,
                                                                 Values = fromHardwareSensorValue(s.Values) };
                            byte[] sensorData = new byte[0];

                            if (Config.FeatureEnableMemoryMapAllHardwareData) {
                                hardware.Sensors.Add(sensor);
                            }
                            sensorData = Utf8Json.JsonSerializer.Serialize(sensor);
                            int offset = (int)stream.Position;
                            index.Add(new DataIndex { Identifier = s.Identifier.ToString(), Size = sensorData.Length,
                                                      Offset = offset, SensorName = s.Name, SensorType = s.SensorType.ToString(),
                                                      HardwareName = s.Hardware.Name });
                            stream.Write(sensorData, 0, sensorData.Length);
                            stream.WriteByte(0);
                        } catch (Exception ex) {
                            if (isDebug)
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
                if (isDebug) {
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
                    if (isDebug) {
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
                        if (h.HardwareType.Equals(HardwareType.Storage)) {
                            if (h is AtaStorage) {
                                List<DataSmartAttribute> attrList = new List<DataSmartAttribute>();
                                HwStatusInfo hwStatus = new HwStatusInfo {
                                    Identifier = h.Identifier.ToString(),
                                    Name = h.Name,
                                    HardwareType = h.HardwareType.ToString(),
                                    HwStatusType = HwStatusType.STORAGE_SMART_ATA,
                                };
                                AtaStorage storage = (AtaStorage)h;
                                LibreHardwareMonitor.Interop.Kernel32.SMART_ATTRIBUTE[] attrs = storage.Smart.ReadSmartData();
                                LibreHardwareMonitor.Interop.Kernel32.SMART_THRESHOLD[] thresholds =
                                    storage.Smart.ReadSmartThresholds();

                                foreach (LibreHardwareMonitor.Interop.Kernel32.SMART_ATTRIBUTE a in attrs) {
                                    if (a.Id == 0x00) {
                                        break;
                                    }

#pragma warning disable CS8600  // Converting null literal or possible null value to non-nullable type.
                                    SmartAttribute attr = storage.SmartAttributes.FirstOrDefault(s => s.Id == a.Id);
#pragma warning restore CS8600  // Converting null literal or possible null value to non-nullable type.
                                    string attrName = "Unknown";
                                    if (attr != null) {
                                        attrName = attr.Name;
                                    }

                                    byte threshold = 0;
                                    foreach (LibreHardwareMonitor.Interop.Kernel32.SMART_THRESHOLD t in thresholds) {
                                        if (t.Id == a.Id) {
                                            threshold = t.Threshold;
                                        }
                                    }
                                    attrList.Add(new DataSmartAttribute {
                                        Id = a.Id,
                                        Name = attrName,
                                        Threshold = threshold,
                                        Flags = a.Flags,
                                        RawValue = new List<Byte>(a.RawValue),
                                        CurrentValue = a.CurrentValue,
                                        WorstValue = a.WorstValue,
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
                                if (isDebug) {
                                    Console.WriteLine(Utf8Json.JsonSerializer.PrettyPrint(hwStatusData));
                                }
                            } else if (h is NVMeGeneric) {
                                NVMeGeneric n = (NVMeGeneric)h;
                                NVMeHealthInfo nh = n.Smart.GetHealthInfo();
                                HwStatusInfo hwStatus = new HwStatusInfo { Identifier = h.Identifier.ToString(), Name = h.Name,
                                                                           HardwareType = h.HardwareType.ToString(),
                                                                           HwStatusType = HwStatusType.STORAGE_SMART_NVME };
                                DataNvmeSmart nvmeSmart =
                                    new DataNvmeSmart { AvailableSpare = nh.AvailableSpare,
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
                                                        WarningCompositeTemperatureTime = nh.WarningCompositeTemperatureTime };
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
            computer.Close();
        }
    }
}
