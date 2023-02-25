
// Copyright (C) 2022 Emerson Pinter - All Rights Reserved
//

/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace LibreHardwareService
{
	internal class MemoryMappedSensors
	{
		private static MemoryMappedFile mmfSensors;
		private static MemoryMappedFile mmfAllHardware;
		private static MemoryMappedFile mmfStatus;

		private readonly int MMAP_SIZE = Config.MemoryMapLimitKb;
		private readonly MemoryMappedViewAccessor acessorSensors;
		private readonly MemoryMappedViewAccessor acessorAllHardware;
		private readonly MemoryMappedViewAccessor acessorStatus;

		private readonly Mutex mutexSensors;
		private readonly Mutex mutexAllHardware;
		private readonly Mutex mutexStatus;

		private DateTime lastLogLimit = DateTime.MinValue;

		private class Nested
		{
			// Explicit static constructor to tell C# compiler
			// not to mark type as beforefieldinit
			static Nested()
			{
			}

			internal static readonly MemoryMappedSensors instance = new MemoryMappedSensors();
		}

		private MemoryMappedSensors()
		{
			try
			{
				var sidEveryonee = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				NTAccount everyone = (NTAccount)sidEveryonee.Translate(typeof(NTAccount));

				MemoryMappedFileSecurity security = new MemoryMappedFileSecurity();
				security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(everyone, MemoryMappedFileRights.Read, AccessControlType.Allow));

				MutexSecurity mtxSecSensors = new MutexSecurity();
				mtxSecSensors.AddAccessRule(new MutexAccessRule(everyone, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));

				mmfSensors = MemoryMappedFile.CreateNew(Constants.FILENAME_SENSORS, MMAP_SIZE * 1024, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, System.IO.HandleInheritability.Inheritable);
				acessorSensors = mmfSensors.CreateViewAccessor();

				mmfStatus = MemoryMappedFile.CreateNew(Constants.FILENAME_STATUS, MMAP_SIZE * 1024, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, System.IO.HandleInheritability.Inheritable);
				acessorStatus = mmfStatus.CreateViewAccessor();

				if (Config.Feature.EnableMemoryMapAllHardwareData)
				{
					mmfAllHardware = MemoryMappedFile.CreateNew(Constants.FILENAME_ALLHARDWARE, MMAP_SIZE * 1024, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, System.IO.HandleInheritability.Inheritable);
					acessorAllHardware = mmfAllHardware.CreateViewAccessor();
					
					MutexSecurity mtxSecAllHardware = new MutexSecurity();

					mtxSecAllHardware.AddAccessRule(new MutexAccessRule(everyone, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));
					mutexAllHardware = new Mutex(false, Constants.MUTEXNAME_ALLHARDWARE);
					mutexAllHardware.SetAccessControl(mtxSecAllHardware);
					Log.Info("Memory Mapped Files (non-persisted) created:\n{0}\n{1}\n{2}\nMutex:\n{3}\n{4}\n{5}\n", Constants.FILENAME_SENSORS, Constants.FILENAME_ALLHARDWARE, Constants.FILENAME_STATUS, Constants.MUTEXNAME_SENSORS, Constants.MUTEXNAME_ALLHARDWARE, Constants.MUTEXNAME_STATUS);
				} else
				{
					Log.Info("Memory Mapped Files (non-persisted) created:\n{0}\n{1}\nMutex:\n{2}\n{3}\n", Constants.FILENAME_SENSORS, Constants.FILENAME_STATUS, Constants.MUTEXNAME_SENSORS, Constants.MUTEXNAME_STATUS);

				}
				mutexSensors = new Mutex(false, Constants.MUTEXNAME_SENSORS);
				mutexSensors.SetAccessControl(mtxSecSensors);

				MutexSecurity mtxSecStatus = new MutexSecurity();
				mtxSecStatus.AddAccessRule(new MutexAccessRule(everyone, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));
				mutexStatus = new Mutex(false, Constants.MUTEXNAME_STATUS);
				mutexStatus.SetAccessControl(mtxSecStatus);
			}
			catch(Exception ex) {
				string errorMessage = "Error creating memory mapped files, please check if the service is running with administrative privileges (LocalSystem), or if the service is already running.";
				Log.Error(errorMessage);
				Debug.WriteLine(errorMessage);
				Log.Error(ex.Message);
				System.Environment.Exit(2);
				throw;
			}
		}

		public static MemoryMappedSensors Instance { get { return Nested.instance; } }

		~MemoryMappedSensors()
		{
			mutexSensors?.Dispose();
			mutexAllHardware?.Dispose();
			mutexStatus?.Dispose();

			acessorSensors?.Dispose();
			acessorStatus?.Dispose();
			acessorAllHardware?.Dispose();

			mmfSensors?.Dispose();
			mmfStatus?.Dispose();
			mmfAllHardware?.Dispose();
		}

		public void WriteSensors(byte[] index, byte[] data, Metadata metadata)
		{
			try
			{
				//Acquire mutex ownership with timeout, the client should open the mutex before read the content and release it afterwards.
				mutexSensors.WaitOne(10);
			}
			catch (AbandonedMutexException)
			{
				//a buggy client left a mutex and didn't release, we should ignore the exception (only this service writes to the memory map file)
			}
			try
			{		
				int length = index.Length + data.Length;
				if ((length / 1024) > MMAP_SIZE && lastLogLimit < DateTime.Now.AddMinutes(-1 * Config.MemoryMapLimitLogIntervalMinutes))
				{
					Log.Error("Data being written to memory map is {0} bytes, larger than the limit {1} kb", (length / 1024), MMAP_SIZE);
					lastLogLimit = DateTime.Now;
				}

				int metadataBlockSize = 4 + metadata.MetadataSize;
				int indexFormat = Config.IndexFormat; //1=json, 2=msgpack
				int indexLen = (int)index.Length;
				int dataLen = (int)data.Length;
				int[] reservedBytes = { (int)0, (int)0, (int)0, (int)0 };
				int[] headerFieldsOffsets = {
												metadataBlockSize, //index-length
												metadataBlockSize + 4, //index-offset
												metadataBlockSize + 8, //index-format
												metadataBlockSize + 12, //data-length
												metadataBlockSize + 16, //data-offset
												metadataBlockSize + 20  //reserved-bytes
											};

				int indexOffset = metadataBlockSize + reservedBytes.Length + (headerFieldsOffsets[headerFieldsOffsets.Length - 1] * 4);
				int dataOffset = indexOffset + index.Length + 4;

				acessorSensors.Write(0, metadata.MetadataSize);
				acessorSensors.Write(4, metadata.UpdateInterval);
				acessorSensors.Write(8, metadata.LastUpdate);

				//write a header with location of index and data
				acessorSensors.Write(headerFieldsOffsets[0], indexLen);
				acessorSensors.Write(headerFieldsOffsets[1], indexOffset);
				acessorSensors.Write(headerFieldsOffsets[2], (int)indexFormat);
				acessorSensors.Write(headerFieldsOffsets[3], dataLen);
				acessorSensors.Write(headerFieldsOffsets[4], dataOffset);
				acessorSensors.WriteArray(headerFieldsOffsets[5], reservedBytes, 0, reservedBytes.Length);
				//write index and data (all offsets inside the index are relative to the start of the index)
				acessorSensors.WriteArray(indexOffset, index, 0, index.Length);
				acessorSensors.Write(indexOffset + index.Length, (int)0);//4-bytes padding
				acessorSensors.WriteArray(dataOffset, data, 0, data.Length);
			}
			catch (Exception ex)
			{
				Log.Error("Error writing to memory mapped file, service stopping: " + ex.Message);
				System.Environment.Exit(1);
			}
			try
			{
				mutexSensors.ReleaseMutex();
			}
			catch
			{
				// if we catch the exception in the WaitOne when a client don't release the mutex, we can get a another here, so ignore it too
			}
		}

		public void WriteHardware(byte[] data, Metadata metadata)
		{
			if (Config.Feature.EnableMemoryMapAllHardwareData)
			{
				try
				{
					//Acquire mutex ownership with timeout, the client should open the mutex before read the content and release it afterwards.
					mutexAllHardware.WaitOne(10);
				}
				catch (AbandonedMutexException)
				{
					//a buggy client left a mutex and didn't release, we should ignore the exception (only this service writes to the memory map file)
				}

				try
				{
					Write(data, acessorAllHardware, metadata);
				}
				catch (Exception ex)
				{
					Log.Error("Error writing to memory mapped file, service stopping: "+ex.Message);
					System.Environment.Exit(1);
				}

				try
				{
					mutexSensors.ReleaseMutex();
				}
				catch
				{
					// if we catch the exception in the WaitOne when a client don't release the mutex, we can get a another here, so ignore it too
				}
			}
		}

		public void WriteStatus(byte[] data, Metadata metadata)
		{
			try
			{
				//Acquire mutex ownership with timeout, the client should open the mutex before read the content and release it afterwards.
				mutexStatus.WaitOne(10);
			}
			catch (AbandonedMutexException)
			{
				//a buggy client left a mutex and didn't release, we should ignore the exception (only this service writes to the memory map file)
			}

			try
			{
				Write(data, acessorStatus, metadata);
			}
			catch (Exception ex)
			{
				Log.Error("Error writing to memory mapped file, service stopping: " + ex.Message);
				System.Environment.Exit(1);
			}

			try
			{
				mutexStatus.ReleaseMutex();
			}
			catch
			{
				// if we catch the exception in the WaitOne when a client don't release the mutex, we can get a another here, so ignore it too
			}
		}

		private void Write(byte[] data, MemoryMappedViewAccessor acessor, Metadata metadata)
		{
			if ((data.Length / 1024) > MMAP_SIZE && lastLogLimit < DateTime.Now.AddMinutes(-1*Config.MemoryMapLimitLogIntervalMinutes))
			{
				Log.Error("Data being written to memory map is {0} bytes, larger than the limit {1} kb", (data.Length / 1024), MMAP_SIZE);
				lastLogLimit = DateTime.Now;
			}
			Console.WriteLine("     -- WRITING {0}B of data", data.Length);
			int metadataBlockSize = 4 + metadata.MetadataSize;

			acessor.Write(0, metadata.MetadataSize);
			acessor.Write(4, metadata.UpdateInterval);
			acessor.Write(8, metadata.LastUpdate);
			acessor.Write(metadataBlockSize, (int) data.Length);
			acessor.WriteArray(metadataBlockSize + 4, data, 0, data.Length);
		}
	}
}
