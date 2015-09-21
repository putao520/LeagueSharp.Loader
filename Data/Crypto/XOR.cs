#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// XOR.cs is part of LeagueSharp.Loader.
// 
// LeagueSharp.Loader is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LeagueSharp.Loader is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Loader. If not, see <http://www.gnu.org/licenses/>.

#endregion

namespace LeagueSharp.Loader.Data.Crypto
{
    using System;

    internal static class XOR
    {
        private static string _b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijk.mnopqrstuvwxyz-123456789+/=";

        public static string Decode(string data, string key)
        {
            var m = 0;
            var binarydata = "";

            foreach (var c in data)
            {
                var v = (GetNFromB64(c) - m) / 4;
                binarydata += DecToBinary(v, 4);

                if (++m > 3)
                {
                    m = 0;
                }
            }

            var keypos = 0;
            var decoded = "";
            for (var i = 0; i < binarydata.Length; i += 8)
            {
                if (i + 8 > binarydata.Length)
                {
                    break;
                }
                var c = BinToDec(binarydata.Substring(i, 8));
                var dc = (c - key.Length) ^ (int)key[keypos];

                if (++keypos >= key.Length)
                {
                    keypos = 0;
                }

                decoded += new string((char)dc, 1);
            }
            return decoded;
        }

        public static string Encode(string data, string key)
        {
            var keypos = 0;
            var binarydata = "";

            foreach (var c in data)
            {
                var xor = ((int)c ^ (int)key[keypos]) + (key.Length);

                if (++keypos >= key.Length)
                {
                    keypos = 0;
                }

                binarydata += DecToBinary(xor, 8);
            }

            var m = 0;
            var cipher = "";

            for (var i = 0; i < binarydata.Length; i += 4)
            {
                var v = BinToDec(binarydata.Substring(i, 4));
                cipher += GetB64FromN(v * 4 + m);

                if (++m > 3)
                {
                    m = 0;
                }
            }
            return cipher;
        }

        private static int BinToDec(string Binary)
        {
            return Convert.ToInt32(Binary, 2);
        }

        private static string DecToBinary(int value, int length)
        {
            var binString = "";

            while (value > 0)
            {
                binString += value % 2;
                value /= 2;
            }

            var reverseString = "";
            foreach (var c in binString)
            {
                reverseString = new string((char)c, 1) + reverseString;
            }
            binString = reverseString;

            binString = new string((char)'0', length - binString.Length) + binString;

            return binString;
        }

        private static string GetB64FromN(int n)
        {
            if (n > _b64.Length)
            {
                return "=";
            }

            return new string(_b64[n], 1);
        }

        private static int GetNFromB64(char n)
        {
            return _b64.IndexOf(n);
        }
    }
}