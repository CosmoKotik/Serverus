using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterServer.Core
{
    internal class BufferManager
    {
        private List<byte> _buffer = new List<byte>();

        #region Add/Insert

        public void AddInt(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            //_buffer.Add((byte)bytes.Length);
            _buffer.AddRange(bytes);
        }
        public void AddLong(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            //_buffer.Add((byte)bytes.Length);
            _buffer.AddRange(bytes);
        }
        public void AddString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            _buffer.Add((byte)bytes.Length);
            _buffer.AddRange(bytes);
        }
        public void AddBytes(byte[] value)
        {
            _buffer.Add((byte)value.Length);
            _buffer.AddRange(value);
        }
        public void AddByte(byte value)
        {
            _buffer.Add((byte)1);
            _buffer.Add(value);
        }

        public void SetPacketId(byte id)
        {
            _buffer = new List<byte>();

            if (_buffer.Count < 1)
                _buffer.Add(id);
            else
                _buffer[0] = id;
        }

        #endregion

        #region Get/Retreive
        public int GetPacketId()
        {
            if (_buffer.Count > 0)
            {
                int id = _buffer[0];
                _buffer.RemoveAt(0);
                return id;
            }
            return -1;
        }

        public int GetInt()
        {
            byte[] result = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                result[i] = _buffer[i];
            }

            _buffer.RemoveRange(0, 4);

            return BitConverter.ToInt32(result);
        }
        /*public int GetLong()
        {
            byte[] result = new byte[_buffer[0]];

            for (int i = 1; i < _buffer[0] + 1; i++)
            {
                result[i - 1] = _buffer[i];
            }

            _buffer.RemoveRange(0, _buffer[0] + 1);

            return BitConverter.ToInt32(result);
        }*/
        public long GetLong()
        {
            byte[] result = new byte[8];

            for (int i = 0; i < 8; i++)
            {
                result[i] = _buffer[i];
            }

            _buffer.RemoveRange(0, 8);

            return BitConverter.ToInt64(result);
        }
        public string GetString()
        {
            byte[] result = new byte[_buffer[0]];

            for (int i = 1; i < (int)_buffer[0] + 1; i++)
            {
                result[i - 1] = _buffer[i];
            }

            _buffer.RemoveRange(0, (int)_buffer[0] + 1);

            return Encoding.UTF8.GetString(result);
        }

        #endregion

        public void SetBytes(byte[] bytes)
        {
            _buffer = bytes.ToList();
        }
        public byte[] GetBytes()
        {
            return _buffer.ToArray();
        }
    }
}
