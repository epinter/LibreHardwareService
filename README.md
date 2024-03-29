# LibreHardwareService
## A hardware sensors service using LibreHardwareMonitor library

[![Latest Release](https://img.shields.io/github/release/epinter/LibreHardwareService.svg)](https://github.com/epinter/LibreHardwareService/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/epinter/LibreHardwareService/total.svg)](https://github.com/epinter/LibreHardwareService/releases/latest)
[![Release Date](https://img.shields.io/github/release-date/epinter/LibreHardwareService.svg)](https://github.com/epinter/LibreHardwareService/releases/latest)
[![License](https://img.shields.io/github/license/epinter/LibreHardwareService.svg)](https://github.com/epinter/LibreHardwareService/blob/main/LICENSE)

## Introduction

LibreHardwareService is a Windows service that writes all sensors data from [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) to shared memory, so libraries and programs like [LibHardwareService](https://github.com/epinter/lhwservice) can retrieve all the sensors values available without any knowledge or link to LibreHardwareMonitor dll or binary. The service is developed to run with administrative priviledges (LocalSystem), in a way that plugins like [Rainmeter-Lhws](https://github.com/epinter/rainmeter-lhws) can access all hardware sensors while running without administrative privileges. ***You don't need to run or install LibreHardwareMonitor!***

## Security

This service writes sensors data to shared memory (see [models](https://github.com/epinter/LibreHardwareService/tree/main/service/src/models) to know what kind of data is written). The permissions of the shared-memory files ([Non-persisted Memory-Mapped files](https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files)) are set to permit all local users to read all the data, this way programs like Rainmeter doesn't have to run with administrative privileges. The shared memory doesn't contain sensitive information, the data written is exclusively related to hardware sensors (like hardware name, brand, sensor name, type, values,etc...) and status like S.M.A.R.T. The shared-memory is never read by this service, this means the everyone-read permission on the memory-mapped files doesn't represent a security risk to your system because the service ignores and overwrites any data written in the shared memory. No data is exposed to network.

## Requiremens

.NET 8.0

## Why another way to access LibreHardwareMonitor sensors ?

While trying to use LibreHardwareMonitor with Rainmeter, I didn't find any way to make Rainmeter to access the sensor data without requiring administrative privileges and keeping low cpu usage (more than 30 sensors in the same skin). Running this service with a plugin in Rainmeter, I can keep the CPU usage lower than 0.5% versus 3% used by some other solutions. So I created this service and [LibHardwareService](https://github.com/epinter/lhwservice).

## Usage

After installation, you will need a software to read and parse the memory-mapped files. An example is the [Rainmeter-Lhws](https://github.com/epinter/rainmeter-lhws) plugin, that access all sensors data through [LibHardwareService](https://github.com/epinter/lhwservice). 

## List all available sensors from LibreHardwareMonitor

In the installation directory, execute ShowSensors.exe as administrator.

## Configuration

The service can be configured using appsettings.json:

```
        /// indexFormat (default 2)
        /// Format of the index that is written on the memory map
        /// 1 = JSON (easier to parse and highly compatible)
        /// 2 - MessagePack (best performance)
        
        /// memoryMapLimitLogIntervalMinutes (default 10 minutes)
        /// Minimum interval (minutes) to log warning when data written is above limit. To avoid logspam.
        
        /// updateIntervalSeconds (default 1 second)
        /// Time interval in seconds to collect the sensor data and write to memory map.
        
        /// hwStatusUpdateIntervalMinutes (default 60 minutes)
        /// Time interval in minutes to collect the hardware status (like storage smart attributes) and write to memory map.
        
        /// sensorsTimeWindowSeconds (default 0)
        /// Time window to keep sensor values. The number of values kept in memory will increase when the window is increased.
        /// Increases CPU usage and memory usage. 0 disables the feature.
        
        /// memoryMapLimitKb (default 1MB)
        /// Limit of the memory map.

        /// featureEnableMemoryMapAllHardwareData (default false)
        /// Enable/disable hardware tree data.
```

## Memory-Mapped files format

There are three memory-mapped files written by this service:
 - Sensors (an index that contains the offset, name and type of each sensor block is written, so when the client is reading the sensor, it will just need to parse the index, get the sensor offset and parse it, there's no need to parse all the sensors to get only one):
```
    Metadata Length (4-byte integer)
    Metadata:
        UpdateInerval (4-byte integer)
        LastUpdate (8-byte long)
    Header
        Index Length (4-byte integer)
        Index Offset (4-byte integer)
        Index Format (4-byte integer)
        Data Length (4-byte integer)
        Data Offset (4-byte integer)
        Reserved (16 bytes)
    Index (an array of MessagePack or Json objects, see DataIndex.cs and IndexFormat config)
    Padding (4-bytes filled with zeros)
    Data (an array of json objects, see DataSensor.cs
```          

 - HardwareTree (a tree representation of all hardware and sensors data LibreHardwareMonitor exposes)

```
    Metadata Length (4-byte integer)
    Metadata:
        UpdateInerval (4-byte integer)
        LastUpdate (8-byte long)
    Data Length (4-byte integer)
    Data (an array of json objects, see DataHardware.cs) 
```

 - Status (currently used for storage SMART), the data is written as follows:
```
    Metadata Length (4-byte integer)
    Metadata:
        UpdateInerval (4-byte integer)
        LastUpdate (8-byte long)
    Data Length (4-byte integer)
    Data
        HwStatusInfo:
            Block Length (4-byte integer, info+data)
            Info Length (4-byte integer)
            Type (4-byte integer)
            Json serialized HwStatusInfo
            Data Length (4-byte integer)
            Data
            0x00 (one byte filled with zero)
```

For more details, see SensorsManager.cs and MemoryMappedSensors.cs.

## License

LibreHardwareService is licensed under the terms of [Mozilla Public License Version 2.0](https://www.mozilla.org/en-US/MPL/2.0/).

This software uses the open source libraries [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), [Utf8Json](https://github.com/neuecc/Utf8Json) and [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp). The source code and their licenses can be obtained at their respective github/websites.

### This project is not part of LibreHardwareMonitor. ###

