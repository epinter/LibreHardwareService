
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using MessagePack;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LibreHardwareService
{
	[MessagePackObject]
	public struct DataSmartAttribute
	{
		/*
		 *  from the document e05171r0 - SMART Attribute Overview, from t13.org
		 
		---- SMART Attribute Table
		Offset		Length(bytes)	Description
		0			2				SMART structure version (this is vendor-specific) 
		2			12				Attribute entry 1 
		2+(12)		12				Attribute entry 2 
		. . . 
		2+(12*29)	12				Attribute entry 30

		---- Entry in the Attribute Table 
		Length(bytes)	Description
		1				Attribute ID (range 0x01-0xFF, 0x00 is invalid)
		2				Flags
										Bit		Description
										0		Pre-fail/Advisory bit 
												This bit is applicable only when the value of this attribute is less than or 
												equal to its threshhold. 
												0 : Advisory: The usage of age of the device has 
												exceeded its intended design life period 
												1: Pre-failure notification: 
												Failure is predicted within 24 hours 

										1		Online data collection bit 
												0: This value of this attribute is only updated during offline activities 
												1: The value of this attribute is updated during 
												both normal operation and offline activities
										2-5		vendor-specific
										6-15	reserved
		1				Value
		8				Vendor-Specific (This should not be compared with other devices or other vendors.)
		 *
		 */
		private byte id;
		private string name;
		private short flags;
		private List<Byte> rawValue;
		private byte currentValue;
		private byte worstValue;
		private byte threshold;


		[DataMember(Name = "id")]
		[Key(0)]
		public byte Id { get => id; set => id = value; }
		
		[DataMember(Name = "name")]
		[Key(1)]
		public string Name { get => name; set => name = value ?? ""; }
		
		[DataMember(Name = "flags")]
		[Key(2)]
		public short Flags { get => flags; set => flags = value; }

		[DataMember(Name = "rawValue")]
		[Key(3)]
		public List<Byte> RawValue { get => rawValue; set => rawValue = value; }

		[DataMember(Name = "currentValue")]
		[Key(4)]
		public byte CurrentValue { get => currentValue; set => currentValue = value; }

		[DataMember(Name = "worstValue")]
		[Key(5)]
		public byte WorstValue { get => worstValue; set => worstValue = value; }

		[DataMember(Name = "threshold")]
		[Key(6)]
		public byte Threshold { get => threshold; set => threshold = value; }

		/**
		 * The device usage has exceeded the expected lifetime.
		 */
		[DataMember(Name = "advisory")]
		[Key(7)]
		public bool IsAdvisory
		{
			get
			{
				return currentValue <= threshold && threshold > 0 && (flags & 1) == 0;
			}
		}

		/**
		 * Failure is imminent.
		 */
		[DataMember(Name = "prefail")]
		[Key(8)]
		public bool IsPreFail
		{
			get
			{
				return currentValue <= threshold && threshold > 0 && (flags & 1) == 1;
			}
		}
	}
}
