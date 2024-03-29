
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Runtime.Serialization;

namespace LibreHardwareService {
    public struct DataSensorValue {
        [DataMember(Name = "value")]
        public float Value { get; set; }

        [DataMember(Name = "time")]
        public DateTime Time { get; set; }
    }
}
