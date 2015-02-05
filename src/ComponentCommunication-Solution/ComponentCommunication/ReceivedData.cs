using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SE_lab
{
    public class ReceivedData
    {
        public const int BUFFERSIZE = 4096;
        public int m_offset;
        public byte[] m_buffer;
        public ReceivedData()
        {
            m_offset = 0;
            m_buffer = new byte[BUFFERSIZE];
        }
    }
}
