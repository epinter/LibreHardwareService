using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace System.IO.MemoryMappedFiles
{
    public class MemoryMappedFileSecurity : ObjectSecurity<MemoryMappedFileRights>
    {
        public MemoryMappedFileSecurity()
            : base(false, ResourceType.KernelObject)
        { }

        [System.Security.SecuritySafeCritical]
        internal MemoryMappedFileSecurity(SafeMemoryMappedFileHandle safeHandle, AccessControlSections includeSections)
            : base(false, ResourceType.KernelObject, safeHandle, includeSections)
        { }

        [System.Security.SecuritySafeCritical]
        internal void PersistHandle(SafeHandle handle)
        {
            Persist(handle);
        }
    }

    public static class MemoryMappedFileFactory
    {
        private static readonly ConstructorInfo _ctor = typeof(MemoryMappedFile).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, new Type[1] { typeof(SafeMemoryMappedFileHandle) })!;

        public static MemoryMappedFile CreateNew(string mapName, long capacity, MemoryMappedFileAccess access,
            MemoryMappedFileOptions options, MemoryMappedFileSecurity memoryMappedFileSecurity, HandleInheritability inheritability)
        {
            if (mapName != null && mapName.Length == 0) throw new ArgumentException(nameof(mapName));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (IntPtr.Size == 4 && capacity > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (access < MemoryMappedFileAccess.ReadWrite || access > MemoryMappedFileAccess.ReadWriteExecute) throw new ArgumentOutOfRangeException(nameof(access));
            if (access == MemoryMappedFileAccess.Write) throw new ArgumentException(nameof(access));
            if (((int)options & ~((int)(MemoryMappedFileOptions.DelayAllocatePages))) != 0) throw new ArgumentOutOfRangeException(nameof(options));
            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable) throw new ArgumentOutOfRangeException(nameof(inheritability));

            SafeMemoryMappedFileHandle handle = CreateCore(mapName, inheritability, memoryMappedFileSecurity, access, options, capacity);
            // MemoryMappedFile(SafeMemoryMappedFileHandle handle) ctor is private
            return (MemoryMappedFile)_ctor.Invoke(new object[] {handle });
        }

        private static SafeMemoryMappedFileHandle CreateCore(string? mapName, HandleInheritability inheritability, 
            MemoryMappedFileSecurity memoryMappedFileSecurity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, long capacity)
        {
            SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability, memoryMappedFileSecurity, out var pinningHandle);

            // split the long into two ints
            int capacityLow = (int)(capacity & 0x00000000FFFFFFFFL);
            int capacityHigh = (int)(capacity >> 32);

            try
            {

                SafeMemoryMappedFileHandle handle = CreateFileMapping(new IntPtr(-1), ref secAttrs, GetPageAccess(access) | (int)options,
                    capacityHigh, capacityLow, mapName);

                int errorCode = Marshal.GetLastWin32Error();
                if (!handle.IsInvalid && errorCode == 0xB7) // ERROR_ALREADY_EXISTS
                {
                    handle.Dispose();
                    throw Marshal.GetExceptionForHR(errorCode)!;
                }
                else if (handle.IsInvalid)
                {
                    throw Marshal.GetExceptionForHR(errorCode)!;
                }

                return handle;
            }
            finally
            {
                if (pinningHandle.HasValue)
                {
                    pinningHandle.Value.Free();
                }
            }
        }

        private unsafe static SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability,
                                        MemoryMappedFileSecurity memoryMappedFileSecurity, out GCHandle? pinningHandle)
        {
            pinningHandle = null;
            SECURITY_ATTRIBUTES secAttrs = default;

            if ((inheritability & HandleInheritability.Inheritable) != 0 ||
                memoryMappedFileSecurity != null)
            {
                secAttrs.nLength = (uint)Marshal.SizeOf(secAttrs);

                if ((inheritability & HandleInheritability.Inheritable) != 0)
                {
                    secAttrs.bInheritHandle = BOOL.TRUE;
                }

                // For ACLs, get the security descriptor from the MemoryMappedFileSecurity.
                if (memoryMappedFileSecurity != null)
                {
                    byte[] sd = memoryMappedFileSecurity.GetSecurityDescriptorBinaryForm();
                    pinningHandle = GCHandle.Alloc(sd, GCHandleType.Pinned);
                    fixed (byte* pSecDescriptor = sd)
                        secAttrs.lpSecurityDescriptor = (IntPtr)pSecDescriptor;
                }
            }
            return secAttrs;
        }

        private static int GetPageAccess(MemoryMappedFileAccess access)
        {
            switch (access)
            {
                case MemoryMappedFileAccess.Read: return 0x02;
                case MemoryMappedFileAccess.ReadWrite: return 0x04;
                case MemoryMappedFileAccess.CopyOnWrite: return 0x08;
                case MemoryMappedFileAccess.ReadExecute: return 0x20;
                default:
                    Debug.Assert(access == MemoryMappedFileAccess.ReadWriteExecute);
                    return 0x40;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        private static extern SafeMemoryMappedFileHandle CreateFileMapping(
            IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpAttributes,
            int fProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName
            );

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            internal uint nLength;
            internal IntPtr lpSecurityDescriptor;
            internal BOOL bInheritHandle;
        }

        internal enum BOOL : int
        {
            FALSE = 0,
            TRUE = 1,
        }
    }
}