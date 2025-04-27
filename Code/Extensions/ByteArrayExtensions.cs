using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Extensions
{
    public static class ByteArrayExtensions
    {
        public static void Write<T>(this byte[] self, T aValue, ref int aInOutAt)
        {
            Buffer.BlockCopy(new T[1] { aValue }, 0, self, aInOutAt, Marshal.SizeOf<T>());
            aInOutAt += Marshal.SizeOf<T>();
        }
    }
}
