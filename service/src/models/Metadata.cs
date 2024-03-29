
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace LibreHardwareService {
    internal struct Metadata {
        int updateInterval;
        long lastUpdate;

        public int UpdateInterval {
            get => updateInterval;
            set => updateInterval = value;
        }
        public long LastUpdate {
            get => lastUpdate;
            set => lastUpdate = value;
        }
        public int MetadataSize { get => sizeof(int) + sizeof(long); }
    }
}
