
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using MessagePack;
using System.Runtime.Serialization;

namespace LibreHardwareService
{
	[MessagePackObject]
	public struct DataIndex
	{
		private string identifier;
		private string sensorName;
		private string sensorType;
		private string hardwareName;

		[DataMember(Name = "identifier")]
		[Key(0)]
		public string Identifier { get => identifier; set => identifier = value ?? ""; }

		[DataMember(Name = "offset")]
		[Key(1)]
		public int Offset { get; set; }

		[DataMember(Name = "size")]
		[Key(2)]
		public int Size { get; set; }

		[DataMember(Name = "sensorName")]
		[Key(3)]
		public string SensorName { get => sensorName; set => sensorName = value ?? ""; }

		[DataMember(Name = "sensorType")]
		[Key(4)]
		public string SensorType { get => sensorType; set => sensorType = value ?? ""; }

		[DataMember(Name = "hardwareName")]
		[Key(5)]
		public string HardwareName { get => hardwareName; set => hardwareName = value ?? ""; }
	}
}