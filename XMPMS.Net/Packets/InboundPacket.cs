// ======================================================================
//  Unreal2 XMP Master Server
//  Copyright (C) 2010-2011  Adam Mummery-Smith
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//  Copyright Notice:
//  Unreal and the Unreal logo are registered trademarks of Epic
//  Games, Inc. ALL RIGHTS RESERVED.
// ======================================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace XMPMS.Net.Packets
{
    /// <summary>
    /// InboundPacket is a container for received data which provides methods for reading specific data
    /// types such as strings, arrays and dictionaries from the byte array
    /// </summary>
    public class InboundPacket : Packet
    {
        /// <summary>
        /// Overall receieved packet length
        /// </summary>
        private int packetLength = 0;

        /// <summary>
        /// UDP packets omit the length header but do include a version number in the header
        /// </summary>
        protected int udpPacketVersion = 0;

        /// <summary>
        /// Received data
        /// </summary>
        protected byte[] data;

        /// <summary>
        /// Current data location
        /// </summary>
        protected int pointer = 0;

        /// <summary>
        /// Get the current location of the internal data pointer in this packet
        /// </summary>
        public int Pointer
        {
            get { return pointer; }
        }

        /// <summary>
        /// Get the length of the packet as defined in the packet header
        /// </summary>
        public int Length
        {
            get { return packetLength; }
        }

        /// <summary>
        /// True if this is an empty inbound packet (no data were receieved)
        /// </summary>
        public virtual bool Empty
        {
            get { return false; }
        }

        /// <summary>
        /// Check the packet length matches the declared length
        /// </summary>
        public virtual bool Valid
        {
            get { return data.Length == packetLength + 4; }
        }

        /// <summary>
        /// Protected constructor, used by subclasses
        /// </summary>
        protected InboundPacket()
        {
        }

        /// <summary>
        /// Creates a new inbound packet with the specified byte array
        /// </summary>
        /// <param name="data"></param>
        protected InboundPacket(byte[] data, bool udp)
        {
            this.data = data;

            if (udp)
            {
                packetLength = data.Length - 4;

                if (data.Length >= 4)
                    udpPacketVersion = PopInt();
            }
            else
            {
                packetLength = PopInt();
            }
        }

        /// <summary>
        /// Creates a new inbound packet with the specified byte array
        /// </summary>
        /// <param name="data"></param>
        public InboundPacket(byte[] data)
        {
            this.data = data;
            packetLength = PopInt();
        }

        /// <summary>
        /// Rewind the pointer to the start of the packet
        /// </summary>
        public void Rewind()
        {
            pointer = 4;
        }

        /// <summary>
        /// Rewind the pointer by the specified number of bytes
        /// </summary>
        /// <param name="offset">Number of bytes to offset the pointer</param>
        public void Rewind(int offset)
        {
            if (offset < 1) return;
            pointer = Math.Max(4, pointer - offset);
        }

        /// <summary>
        /// Pop a byte from the packet at the current location and advance the pointer
        /// </summary>
        /// <returns>Byte at the pointer</returns>
        public virtual byte PopByte()
        {
            byte result = data[pointer];
            pointer ++;
            return result;
        }

        /// <summary>
        /// Pop multiple bytes from the packet at the current location and advance the pointer
        /// </summary>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Bytes at the current location</returns>
        public virtual byte[] PopBytes(int count)
        {
            byte[] result = new byte[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = data[pointer];
                pointer++;
            }

            return result;
        }

        /// <summary>
        /// Pop all remaining bytes from the packet at the current location and advance the pointer to the end
        /// </summary>
        /// <returns>All remaining bytes in the packet</returns>
        public virtual byte[] PopBytes()
        {
            byte[] result = new byte[data.Length - pointer];
            Buffer.BlockCopy(data, pointer, result, 0, result.Length);
            pointer = data.Length;
            return result;
        }

        /// <summary>
        /// Pop an unsigned short (16-bit) integer from the packet at the current location and advance the pointer
        /// </summary>
        /// <returns>Unsigned short from the current location</returns>
        public virtual ushort PopUShort()
        {
            ushort result = (ushort)(data[pointer] + (data[pointer + 1] * 0x100));
            pointer += 2;
            return result;
        }

        /// <summary>
        /// Pops a signed (twos-complement) 32-bit integer from the packet at the current location and advances the pointer
        /// </summary>
        /// <returns>Signed integer from the current location</returns>
        public virtual int PopInt()
        {
            int result = BitConverter.ToInt32(data, pointer);
            pointer += 4;
            return result;
        }

        /// <summary>
        /// Pops a signed (sign bit) variable-length integer from the packet at the current location and advance the pointer
        /// </summary>
        /// <returns>Signed integer from the current location</returns>
        public virtual int PopCompactIndex()
        {
            bool signed = false;
            int value = PopCompactIndex(out signed);
            return signed ? value * -1 : value;
        }

        /// <summary>
        /// Pops a variable length unsigned integer from the packet at the current location and advances the pointer, returns
        /// the sign bit in the out parameter (XMP uses the sign bit to indicate unicode strings)
        /// </summary>
        /// <param name="signed">Returns the sign bit from the value</param>
        /// <returns>Unsigned integer value</returns>
        public virtual int PopCompactIndex(out bool signed)
        {
            int value = (data[pointer] & 0x3F) << 0x01;
            signed = ((data[pointer++] & 0x80) == 0x80);

            if ((data[pointer - 1] & 0x40) == 0x40)
            {
                for (int offset = 1; offset < 5; offset++)
                {
                    value += (data[pointer++] & 0x1F) << (offset * 0x07);
                    if ((data[pointer] & 0x80) == 0x00) break;
                }
            }

            return value >> 0x01;
        }

        /// <summary>
        /// Pops a string from the packet at the specified location. The first byte should be the length specifier
        /// and the string should be null-terminated
        /// </summary>
        /// <returns></returns>
        public virtual string PopString()
        {
            string result = "";
            bool bUnicode = false;
            int length = PopCompactIndex(out bUnicode); // sign bit indicates a unicode string in XMP

            if (length == 0)
            {
                return "";
            }
            else if (bUnicode)
            {
                length *= 2;
                result = Encoding.Unicode.GetString(data, pointer, length - 2);
            }
            else
            {
                result = Encoding.ASCII.GetString(data, pointer, length - 1);
            }

            pointer += length;
            return result;
        }

        /// <summary>
        /// Pops an array from the packet at the specified location and advances the pointer
        /// </summary>
        /// <returns></returns>
        public virtual string[] PopArray()
        {
            // First byte is the array size, followed by strings
            int arraySize = PopCompactIndex();
            int arrayPos = 0;

            string[] parts = new string[arraySize];

            while (pointer < packetLength - 1 && arrayPos < arraySize)
            {
                parts[arrayPos] = PopString();
                arrayPos++;
            }

            return parts;
        }

        /// <summary>
        /// Pops a key/value array from the packet at the current position and advances the pointer
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> PopKeyValueArray()
        {
            Dictionary<string, string> kva = new Dictionary<string, string>();

            // We need to pop twice as many elements as entries since each entry consists of two strings
            int arraySize = PopCompactIndex() * 2;

            int arrayPos = 0;

            // Array of string parts, which are initially interleaved keys and values
            string[] parts = new string[arraySize];

            while (arrayPos < arraySize)
            {
                parts[arrayPos] = PopString();
                arrayPos++;
            }

            // Create key/value pairs by de-interleaving the sequence of string values
            for (int i = 0; i < arraySize; i += 2)
                kva[parts[i]] = parts[i + 1];

            return kva;
        }

        /// <summary>
        /// Pops an array of the specified struct from the packet at the current position and advances the pointer
        /// </summary>
        /// <returns></returns>
        public T[] PopStructArray<T> () where T : new()
        {
            List<T> result = new List<T>();

            // Like normal arrays, the array data are prefixed with an array length byte which tells us how much to read
            int arraySize = PopCompactIndex();

            for (int i = 0; i < arraySize; i++)
            {
                // The specified struct must have a constructor which takes the parameters in order
                List<object> paramList = new List<object>();

                // Enumerate the fields of the struct and pop relevant data from the packet
                foreach (FieldInfo field in typeof(T).GetFields())
                    paramList.Add(DynamicPopValue(field.FieldType));

                // Create the new struct with the received data and add it to the list
                result.Add((T)Activator.CreateInstance(typeof(T), paramList.ToArray()));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Pops a value from the packet based on the supplied type, used by PopStructArray
        /// </summary>
        /// <param name="popType"></param>
        /// <returns></returns>
        private object DynamicPopValue(Type popType)
        {
            if (popType == typeof(string))                      return PopString();
            if (popType == typeof(int))                         return PopInt();
            if (popType == typeof(byte))                        return PopByte();                               
            if (popType == typeof(ushort))                      return PopUShort();
            if (popType == typeof(string[]))                    return PopArray();
            if (popType == typeof(Dictionary<string, string>))  return PopKeyValueArray();

            // For enums, do a dynamic pop based on the underlying type
            if (popType.BaseType == typeof(Enum))
                return DynamicPopValue(Enum.GetUnderlyingType(popType));

            return null;
        }

        /// <summary>
        /// Returns the contents of this packet in printable hexadecimal format for debugging purposes
        /// </summary>
        public override string PrintBytes()
        {
            return PrintBytes(false);
        }

        /// <summary>
        /// Returns the contents of this packet in printable hexadecimal format for debugging purposes
        /// </summary>
        public virtual string PrintBytes(bool printHeader)
        {
            return PrintBytes(printHeader ? 0 : 4);
        }

        /// <summary>
        /// Returns the contents of this packet in printable hexadecimal format for debugging purposes
        /// </summary>
        public virtual string PrintBytes(int startIndex)
        {
            if (data == null) return "[NULL]";

            List<string> result = new List<string>();

            for (int i = startIndex; i < data.Length; i++)
                result.Add(String.Format("{0:x2}", data[i]));

            return String.Format("[{0}]", String.Join(" ", result.ToArray()));
        }

        /// <summary>
        /// Returns the contents of this packet in printable format for debugging purposes
        /// </summary>
        public override string Print()
        {
            return Print(4);
        }

        /// <summary>
        /// Returns the contents of this packet in printable format for debugging purposes
        /// </summary>
        /// <param name="startIndex">Start index to print from</param>
        /// <returns></returns>
        public virtual string Print(int startIndex)
        {
            if (data == null) return "<NULL>";

            string result = "";

            for (int i = startIndex; i < data.Length; i++)
            {
                if (data[i] > 31 && data[i] < 127)
                    result += (char)data[i];
                else
                {
                    result += String.Format("<0x{0:x2}>", data[i]);
                }
            }

            return result;
        }
    }
}
