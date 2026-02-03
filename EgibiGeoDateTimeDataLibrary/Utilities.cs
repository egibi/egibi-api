using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EgibiGeoDateTimeDataLibrary
{
    public static class Utilities
    {
        public static bool? ConvertDst(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (value == "1")
                    return true;
                return false;
            }

            return null;
        }

        public static int? ConvertTimeStart(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                int? converted = Convert.ToInt32(value);
                return converted;
            }
            return null;
        }

        public static int? ConvertGmtOffset(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                int? converted = Convert.ToInt32(value);
                return converted;
            }

            return null;
        }
    }
}
