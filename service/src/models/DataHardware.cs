
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Runtime.Serialization;

namespace LibreHardwareService {
    public struct DataHardware {
        private string hardwareType;
        private string identifier;
        private string name;
        private string parent;
        private List<DataSensor> sensors;
        private List<DataHardware> subHardware;

        [DataMember(Name = "hardwareType")]
        public string HardwareType {
            get => hardwareType;
            set => hardwareType = value ?? "";
        }

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

        [DataMember(Name = "parent")]
        public string Parent {
            get => parent;
            set => parent = value ?? "";
        }

        [DataMember(Name = "sensors")]
        public List<DataSensor> Sensors {
            get => sensors;
            set => sensors = value ?? new List<DataSensor>();
        }

        [DataMember(Name = "subHardware")]
        public List<DataHardware> SubHardware {
            get => subHardware;
            set => subHardware = value ?? new List<DataHardware>();
        }
    }
}
