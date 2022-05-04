
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
	public struct DataNvmeSmart
	{
		[DataMember(Name = "availableSpare")]
		[Key(0)]
		public byte AvailableSpare { get; set; }

		[DataMember(Name = "availableSpareThreshold")]
		[Key(1)]
		public byte AvailableSpareThreshold { get; set; }

		[DataMember(Name = "controllerBusyTime")]
		[Key(2)]
		public ulong ControllerBusyTime { get; set; }

		[DataMember(Name = "criticalCompositeTemperatureTime")]
		[Key(3)]
		public uint CriticalCompositeTemperatureTime { get; set; }

		[DataMember(Name = "criticalWarning")]
		[Key(4)]
		public byte CriticalWarning { get; set; }

		[DataMember(Name = "dataUnitRead")]
		[Key(5)]
		public ulong DataUnitRead { get; set; }

		[DataMember(Name = "dataUnitWritten")]
		[Key(6)]
		public ulong DataUnitWritten { get; set; }

		[DataMember(Name = "errorInfoLogEntryCount")]
		[Key(7)]
		public ulong ErrorInfoLogEntryCount { get; set; }

		[DataMember(Name = "hostReadCommands")]
		[Key(8)]
		public ulong HostReadCommands { get; set; }

		[DataMember(Name = "hostWriteCommands")]
		[Key(9)]
		public ulong HostWriteCommands { get; set; }

		[DataMember(Name = "mediaErrors")]
		[Key(10)]
		public ulong MediaErrors { get; set; }

		[DataMember(Name = "percentageUsed")]
		[Key(11)]
		public byte PercentageUsed { get; set; }

		[DataMember(Name = "powerCycle")]
		[Key(12)]
		public ulong PowerCycle { get; set; }

		[DataMember(Name = "powerOnHours")]
		[Key(13)]
		public ulong PowerOnHours { get; set; }

		[DataMember(Name = "temperature")]
		[Key(14)]
		public short Temperature { get; set; }

		[DataMember(Name = "temperatureSensors")]
		[Key(15)]
		public short[] TemperatureSensors { get; set; }

		[DataMember(Name = "unsafeShutdowns")]
		[Key(16)]
		public ulong UnsafeShutdowns { get; set; }

		[DataMember(Name = "warningCompositeTemperatureTime")]
		[Key(17)]
		public uint WarningCompositeTemperatureTime { get; set; }

	}
}
