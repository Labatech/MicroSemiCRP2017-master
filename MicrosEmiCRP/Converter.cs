using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MicrosEmiCRP
{
    static class Converter
    {
        public static string Hex2String(string hex)
        {
            string str = "";
            int num = hex.Length / 2;
            for (int i = 0; i < num; i++)
            {
                string str2 = hex.Substring(i * 2, 2);
                str = str + ((char)Convert.ToUInt16(str2, 0x10)).ToString();
            }
            return str;
        }

        public static string byteListToString(List<Byte> l)
        {
            if (l == null)
            {
                return "";
            }
            byte[] array = new byte[l.Count()];
            int i = 0;
            foreach (Byte current in l)
            {
                array[i] = current;
                i++;
            }
            
            return array.ToString();
        }
    }
}
