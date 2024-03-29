
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Runtime.Serialization;

namespace LibreHardwareService {
    public struct DataSensor {
        private string identifier;
        private string name;
        private string sensorType;
        private string hardwareId;
        private string hardwareName;
        private string hardwareType;
        private List<DataSensorValue> values;

        [DataMember(Name = "identifier")]
        public string Identifier {
            get => identifier;
            set => identifier = value ?? "";
        }

        [DataMember(Name = "name")]
        public string Name {
            get => name;
            set => name = value ?? "";
        }

        [DataMember(Name = "sensorType")]
        public string SensorType {
            get => sensorType;
            set => sensorType = value ?? "";
        }

        [DataMember(Name = "hardwareId")]
        public string HardwareId {
            get => hardwareId;
            set => hardwareId = value ?? "";
        }

        [DataMember(Name = "hardwareName")]
        public string HardwareName {
            get => hardwareName;
            set => hardwareName = value ?? "";
        }

        [DataMember(Name = "hardwareType")]
        public string HardwareType {
            get => hardwareType;
            set => hardwareType = value ?? "";
        }

        [DataMember(Name = "value")]
        public float Value { get; set; }

        [DataMember(Name = "max")]
        public float Max { get; set; }

        [DataMember(Name = "min")]
        public float Min { get; set; }

        [DataMember(Name = "valuesTimeWindow")]
        public double ValuesTimeWindow { get; set; }

        [DataMember(Name = "values")]
        public List<DataSensorValue> Values {
            get => values;
            set => values = value ?? new List<DataSensorValue>();
        }
    }
}
