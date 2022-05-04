
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Runtime.Serialization;


namespace LibreHardwareService
{
	public struct HwStatusInfo
	{
		private string identifier;
		private string name;
		private string hardwareType;

		private HwStatusType hwStatusType;

		[DataMember(Name = "identifier")]
		public string Identifier { get => identifier; set => identifier = value ?? ""; }

		[DataMember(Name = "hardwareType")]
		public string HardwareType { get => hardwareType; set => hardwareType = value ?? ""; }

		[DataMember(Name = "name")]
		public string Name { get => name; set => name = value ?? ""; }

		[DataMember(Name = "hwStatusType")]
		public HwStatusType HwStatusType { get => hwStatusType; set => hwStatusType = value; }
	}
}
