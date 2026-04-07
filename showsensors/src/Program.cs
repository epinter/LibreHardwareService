
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

namespace ShowSensors {
    internal class HardwareUpdateVisitor : IVisitor {
        public void VisitComputer(IComputer computer) {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware) {
            try {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) {
                    subHardware.Accept(this);
                }
            } catch (Exception) {
                // ignored
            }
        }
        public void VisitSensor(ISensor sensor) {
        }
        public void VisitParameter(IParameter parameter) {
        }
    }

    internal class Program {
        static void Main(string[] args) {
            Computer computer = new Computer {
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

            foreach (IHardware h in computer.Hardware) {
                Console.WriteLine("Hardware: name='{0}'; type='{1}'; identifier='{2}';", h.Name, h.HardwareType, h.Identifier);

                if (h.HardwareType.Equals(HardwareType.Storage) && h is StorageDevice storageDevice) {
                    foreach (var sa in storageDevice.Attributes) {
                        if (sa.Id == 0x00) continue;
                        var raw = sa.Attribute?.Attribute;
                        Console.WriteLine(
                            "\t\tsmart-attribute: [[name:'{0}'; id:'0x{1:x2}'; rawValue:'{2}';\n\t\t\tcurrentValue:'{3}'; threshold:'{4}'; worst:'{5}';]]",
                            sa.Name ?? "Unknown",
                            sa.Id,
                            raw.HasValue && raw.Value.RawValue != null ? BitConverter.ToString(raw.Value.RawValue) : sa.Value.ToString(),
                            raw?.CurrentValue ?? 0,
                            raw?.Threshold ?? sa.Threshold,
                            raw?.WorstValue ?? 0);
                    }
                }

                foreach (IHardware sh in h.SubHardware) {
                    Console.WriteLine("\tSubHardware: name='{0}'; type='{1}'; identifier='{2}';", sh.Name, sh.HardwareType,
                                      sh.Identifier);
                    foreach (ISensor s in sh.Sensors) {
                        Console.WriteLine("\t\tSensor: name='{0}'; value='{1}'; type='{2}'; identifier='{3}';", s.Name, s.Value,
                                          s.SensorType, s.Identifier);
                    }
                }
                ;
                foreach (ISensor s in h.Sensors) {
                    Console.WriteLine("\tSensor: name='{0}'; value='{1}'; type='{2}'; identifier='{3}';", s.Name, s.Value,
                                      s.SensorType, s.Identifier);
                }
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
