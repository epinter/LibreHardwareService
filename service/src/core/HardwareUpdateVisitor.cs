
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using LibreHardwareMonitor.Hardware;
using System;

namespace LibreHardwareService
{
	internal class HardwareUpdateVisitor : IVisitor
	{

		public void VisitComputer(IComputer computer)
		{
			computer.Traverse(this);
		}
		public void VisitHardware(IHardware hardware)
		{
			try
			{
				hardware.Update();
				foreach (IHardware subHardware in hardware.SubHardware)
				{
					subHardware.Accept(this);
				}
			} catch (Exception)
			{
				//ignored
			}
		}
		public void VisitSensor(ISensor sensor) { }
		public void VisitParameter(IParameter parameter) { }
	}
}
