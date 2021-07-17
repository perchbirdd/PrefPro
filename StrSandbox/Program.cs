﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace StrSandbox
{
    class Program
    {
        private static bool male = true;
        private static string playerName = "Not Perchbird";
        
        // Paste a dump here, and write a function to handle what you want
        private static byte[] test = new byte[]
        {
            0x49, 0x20, 0x73, 0x68, 0x61, 0x6C, 0x6C, 0x20, 0x73, 0x61, 0x79, 0x20, 0x69, 0x74, 0x20, 0x70, 0x6C,
            0x61, 0x69, 0x6E, 0x2C, 0x20, 0x4C, 0x75, 0x63, 0x69, 0x61, 0x6E, 0x65, 0x3A, 0x20, 0x74, 0x68, 0x69,
            0x73, 0x20, 0x2, 0x8, 0xF, 0xE9, 0x5, 0xFF, 0x6, 0x77, 0x6F, 0x6D, 0x61, 0x6E, 0xFF, 0x4, 0x6D, 0x61,
            0x6E, 0x3, 0x20, 0x69, 0x73, 0x20, 0x6E, 0x6F, 0x74, 0x20, 0x66, 0x69, 0x74, 0x20, 0x74, 0x6F, 0x20,
            0x77, 0x69, 0x65, 0x6C, 0x64, 0x20, 0x61, 0x20, 0x62, 0x6F, 0x77, 0x2E, 0x20, 0x46, 0x6F, 0x72, 0x20,
            0x6F, 0x75, 0x72, 0x20, 0x73, 0x61, 0x6B, 0x65, 0xE2, 0x94, 0x80, 0x61, 0x6E, 0x64, 0x20, 0x2, 0x8, 0xE,
            0xE9, 0x5, 0xFF, 0x5, 0x68, 0x65, 0x72, 0x73, 0xFF, 0x4, 0x68, 0x69, 0x73, 0x3, 0xE2, 0x94, 0x80, 0x77,
            0x65, 0x20, 0x73, 0x68, 0x6F, 0x75, 0x6C, 0x64, 0x20, 0x72, 0x65, 0x76, 0x6F, 0x6B, 0x65, 0x20, 0x2,
            0x8, 0xD, 0xE9, 0x5, 0xFF, 0x4, 0x68, 0x65, 0x72, 0xFF, 0x4, 0x68, 0x69, 0x73, 0x3, 0x20, 0x6D, 0x65,
            0x6D, 0x62, 0x65, 0x72, 0x73, 0x68, 0x69, 0x70, 0x2E, 0x0
        };

        private static byte[] test2 = new byte[]
        {
            0x48, 0x69, 0x65, 0x2C, 0x20, 0x6D, 0x79, 0x20, 0x63, 0x68, 0x69, 0x6C, 0x64, 0x72, 0x65, 0x6E, 0x2C,
            0x20, 0x69, 0x6E, 0x74, 0x6F, 0x20, 0x74, 0x68, 0x65, 0x20, 0x64, 0x61, 0x72, 0x6B, 0x21, 0x20, 0x2,
            0x8, 0xC, 0xE9, 0x5, 0xFF, 0x4, 0x53, 0x68, 0x65, 0xFF, 0x3, 0x48, 0x65, 0x3, 0x20, 0x77, 0x69, 0x6C,
            0x6C, 0x20, 0x6E, 0x6F, 0x74, 0x20, 0x62, 0x65, 0x20, 0x64, 0x65, 0x6E, 0x69, 0x65, 0x64, 0x2E, 0x2E,
            0x2E, 0x0
        };

        private static byte[] lastNameTest = new byte[]
        {
            0x41, 0x68, 0x2C, 0x20, 0x42, 0x6C, 0x61, 0x64, 0x65, 0x20, 0x2, 0x2C, 0xD, 0xFF, 0x7, 0x2, 0x29, 0x3,
            0xEB, 0x2, 0x3, 0xFF, 0x2, 0x20, 0x3, 0x3, 0x2E, 0x20, 0x48, 0x61, 0x76, 0x65, 0x20, 0x79, 0x6F, 0x75,
            0x20, 0x72, 0x65, 0x63, 0x65, 0x69, 0x76, 0x65, 0x64, 0x20, 0x77, 0x6F, 0x72, 0x64, 0x20, 0x79, 0x65,
            0x74, 0x20, 0x66, 0x72, 0x6F, 0x6D, 0x20, 0x43, 0x6F, 0x6D, 0x6D, 0x61, 0x6E, 0x64, 0x65, 0x72, 0x20,
            0x42, 0x61, 0x6A, 0x73, 0x61, 0x6C, 0x6A, 0x65, 0x6E, 0x3F, 0x20, 0x49, 0x66, 0x20, 0x6E, 0x6F, 0x74,
            0x2C, 0x20, 0x49, 0x20, 0x77, 0x6F, 0x75, 0x6C, 0x64, 0x20, 0x62, 0x65, 0x20, 0x6D, 0x6F, 0x72, 0x65,
            0x20, 0x74, 0x68, 0x61, 0x6E, 0x20, 0x68, 0x61, 0x70, 0x70, 0x79, 0x20, 0x74, 0x6F, 0x20, 0x61, 0x70,
            0x70, 0x72, 0x69, 0x73, 0x65, 0x20, 0x79, 0x6F, 0x75, 0x20, 0x6F, 0x66, 0x20, 0x63, 0x6F, 0x6E, 0x64,
            0x69, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x20, 0x6F, 0x6E, 0x20, 0x74, 0x68, 0x65, 0x20, 0x73, 0x6F, 0x75,
            0x74, 0x68, 0x65, 0x72, 0x6E, 0x20, 0x66, 0x72, 0x6F, 0x6E, 0x74, 0x2E, 0x0
        };

        private static byte[] fullNameTest =
        {
            0x48, 0x6D, 0x3F, 0x20, 0x49, 0x73, 0x20, 0x74, 0x68, 0x61, 0x74, 0x20, 0x77, 0x68, 0x6F, 0x20,
            0x49, 0x20, 0x74, 0x68, 0x69, 0x6E, 0x6B, 0x20, 0x69, 0x74, 0x20, 0x69, 0x73, 0x3F, 0x20, 0x4E,
            0x6F, 0x2C, 0x20, 0x6E, 0x6F, 0x2C, 0x20, 0x69, 0x74, 0x27, 0x73, 0x20, 0x6D, 0x79, 0x20, 0x69,
            0x6D, 0x61, 0x67, 0x69, 0x6E, 0x61, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x70, 0x6C, 0x61, 0x79, 0x69,
            0x6E, 0x67, 0x20, 0x74, 0x72, 0x69, 0x63, 0x6B, 0x73, 0x20, 0x61, 0x67, 0x61, 0x69, 0x6E, 0x2E,
            0x20, 0x54, 0x68, 0x65, 0x72, 0x65, 0x27, 0x73, 0x20, 0x61, 0x62, 0x73, 0x6F, 0x6C, 0x75, 0x74,
            0x65, 0x6C, 0x79, 0x20, 0x6E, 0x6F, 0x20, 0x77, 0x61, 0x79, 0x20, 0x02, 0x29, 0x03, 0xEB, 0x02,
            0x03, 0x20, 0x77, 0x6F, 0x75, 0x6C, 0x64, 0x20, 0x62, 0x65, 0x20, 0x68, 0x65, 0x72, 0x65, 0x2E,
            0x00
        };

        private static byte[] omegaSubject =
        {
            0x28, 0x2D, 0x4F, 0x6D, 0x65, 0x67, 0x61, 0x2D, 0x29, 0x49, 0x20, 0x77, 0x69, 0x6C, 0x6C, 0x20, 0x73, 0x75,
            0x62, 0x6A, 0x65, 0x63, 0x74, 0x20, 0x74, 0x68, 0x65, 0x20, 0x75, 0x6E, 0x6B, 0x6E, 0x6F, 0x77, 0x6E, 0x20,
            0x74, 0x6F, 0x20, 0x74, 0x68, 0x65, 0x20, 0x69, 0x6D, 0x70, 0x65, 0x72, 0x66, 0x65, 0x63, 0x74, 0x2E, 0x20,
            0x54, 0x65, 0x73, 0x74, 0x20, 0x73, 0x75, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x20, 0x2, 0x29, 0x3, 0xEB, 0x2,
            0x3, 0x20, 0x77, 0x69, 0x6C, 0x6C, 0x20, 0x70, 0x72, 0x6F, 0x63, 0x65, 0x65, 0x64, 0x20, 0x74, 0x6F, 0x20,
            0x61, 0x20, 0x73, 0x75, 0x70, 0x70, 0x6C, 0x65, 0x6D, 0x65, 0x6E, 0x74, 0x61, 0x6C, 0x20, 0x73, 0x74, 0x61,
            0x67, 0x65, 0x2E, 0x00
        };

        private static byte[] omegaFullNameCrash =
        {
            0x28, 0x2D, 0x4F, 0x6D, 0x65, 0x67, 0x61, 0x2D, 0x29, 0x54, 0x65, 0x73, 0x74, 0x20, 0x73, 0x75, 0x62, 0x6A,
            0x65, 0x63, 0x74, 0x20, 0x2, 0x29, 0x3, 0xEB, 0x2, 0x3, 0x2C, 0x20, 0x79, 0x6F, 0x75, 0x20, 0x61, 0x72,
            0x65, 0x20, 0x70, 0x65, 0x72, 0x6D, 0x69, 0x74, 0x74, 0x65, 0x64, 0x20, 0x74, 0x6F, 0x20, 0x6C, 0x65, 0x61,
            0x76, 0x65, 0x2E, 0x20, 0x52, 0x65, 0x2D, 0x65, 0x6E, 0x74, 0x65, 0x72, 0x20, 0x74, 0x68, 0x65, 0x20, 0x74,
            0x65, 0x73, 0x74, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64, 0x20, 0x77, 0x68, 0x65, 0x6E, 0x20, 0x79, 0x6F, 0x75,
            0x72, 0x20, 0x73, 0x74, 0x72, 0x65, 0x6E, 0x67, 0x74, 0x68, 0x20, 0x68, 0x61, 0x73, 0x20, 0x72, 0x65, 0x74,
            0x75, 0x72, 0x6E, 0x65, 0x64, 0x20, 0x74, 0x6F, 0x20, 0x6F, 0x70, 0x74, 0x69, 0x6D, 0x61, 0x6C, 0x20, 0x6C,
            0x65, 0x76, 0x65, 0x6C, 0x73, 0x2E, 0x00
        };

        static unsafe void Main1(string[] args)
        {
            var toTest = omegaFullNameCrash;
            // SeStringManager mgr = new SeStringManager(new DataManager(ClientLanguage.English));
            
            fixed (byte* text = toTest)
            {
                PrintStackedView(text);
                // PrintStackedView(toTest, 50, 50);

                // ProcessGenderedParam(text);
                // HandlePtr(mgr, text);

                PrintStackedView(text);
                // PrintStackedView(toTest, 50, 50);
            }
        }

        private static string lastValid = "";
        public static unsafe void Main(string[] args)
        {
            while (true)
            {
                var inStr = Console.ReadLine().Trim();
                lastValid = SanitizeName(inStr.Split(' ')[0], inStr.Split(' ')[1]);
                Console.WriteLine(lastValid);
            }
        }

        private static string SanitizeName(string first, string last)
        {
            string newFirst = first;
            string newLast = last;
            
            // Save the last valid name for fail cases
            // string lastValid = _configuration.NameConfigs[_prefPro.PlayerName];
            
            if (newFirst.Length > 15 || newLast.Length > 15)
                return lastValid;
            string combined = $"{newFirst}{newLast}";
            if (combined.Length > 20)
                return lastValid;

            newFirst = Regex.Replace(newFirst, "[^A-Za-z'\\-\\s{1}]", "");
            newLast = Regex.Replace(newLast, "[^A-Za-z'\\-\\s{1}]", "");

            return $"{newFirst} {newLast}";
        }

        private static unsafe void HandlePtr(SeStringManager mgr, byte* ptr)
        {
            var byteList = new List<byte>();
            int i = 0;
            while (ptr[i] != 0)
                byteList.Add(ptr[i++]);
            var byteArr = byteList.ToArray();
            
            // Write handlers, put them here
            SeString parsed = mgr.Parse(byteArr);
            for (int payloadIndex = 0; payloadIndex < parsed.Payloads.Count; payloadIndex++)
            {
                var thisPayload = parsed.Payloads[payloadIndex];
                if (thisPayload.Type == PayloadType.Unknown)
                {
                    // Add handlers here
                    parsed.Payloads[payloadIndex] = HandleGenderPayload(parsed.Payloads[payloadIndex]);
                    parsed.Payloads[payloadIndex] = TestNamePayload(parsed.Payloads[payloadIndex]);
                }
            }
            var encoded = parsed.Encode();
            
            int j;
            for (j = 0; j < encoded.Length; j++)
                ptr[j] = encoded[j];

            ptr[j] = 0;
        }

        private static Payload HandleGenderPayload(Payload thisPayload)
        {
            byte[] reEncode = thisPayload.Encode();
            if (reEncode[0] == 2 && reEncode[1] == 8 && reEncode[3] == 0xE9 && reEncode[4] == 5)
            {
                int femaleStart = 7;
                int femaleLen = reEncode[6] - 1;
                int maleStart = femaleStart + femaleLen + 2;
                int maleLen = reEncode[maleStart - 1] - 1;

                int len = male ? maleLen : femaleLen;
                int start = male ? maleStart : femaleStart;

                byte[] newTextBytes = new byte[len];
                for (int c = 0; c < newTextBytes.Length; c++)
                    newTextBytes[c] = reEncode[start + c];

                return new TextPayload(Encoding.ASCII.GetString(newTextBytes));
            }
            
            return thisPayload;
        }

        private static Payload TestNamePayload(Payload thisPayload)
        {
            byte[] reEncode = thisPayload.Encode();
            if (reEncode[1] == 0x29 && reEncode[2] == 0x3)
            {
                return new TextPayload("asdfmovie5");
                // return new TextPayload(Encoding.ASCII.GetString(newTextBytes));
            }
            
            return thisPayload;
        }

        public static void Main2(String args)
        {
            // var byteList = new List<byte>();
            // int i = 0;
            // while (ptr[i] != 0)
            //     byteList[i] = ptr[i];
        }

        private static unsafe void PrintStackedView(byte* ptr, int start, int len = 0)
        {
            var byteList = new List<byte>();
            int q = 0;
            while (ptr[q] != 0)
                byteList.Add(ptr[q++]);
            var arr = byteList.ToArray();
            int length = len;
            if (len == 0)
                length = arr.Length;

            StringBuilder numOut = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
                numOut.Append($"{i % 100:D2} ");

            StringBuilder textOut = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                string app;
                if (arr[i] >= 32 && arr[i] <= 126)
                    app = Encoding.ASCII.GetString(arr, i, 1).PadLeft(2, ' ');
                else
                    app = "[]";

                textOut.Append($"{app} ");
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
                sb.Append($"{arr[i]:X2} ");

            Console.WriteLine(numOut.ToString().Substring(start * 3, Math.Min((arr.Length - start) * 3, length * 3)));
            Console.WriteLine(textOut.ToString().Substring(start * 3, Math.Min((arr.Length - start) * 3, length * 3)));
            Console.WriteLine(sb.ToString().Substring(start * 3, Math.Min((arr.Length - start) * 3, length * 3)));
        }

        private static unsafe void PrintStackedView(byte* arr)
        {
            PrintStackedView(arr, 0);
        }

        private static string ByteArrayStr(byte[] arr)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
                sb.Append($"{arr[i]:X2} ");
            return sb.ToString();
        }

        private static unsafe void ProcessGenderedParam(byte* ptr)
        {
            int len = 0;
            byte* text2 = ptr;
            while (*text2 != 0)
            {
                text2++;
                len++;
            }

            byte[] newText = new byte[len];

            int currentPos = 0;

            for (int i = 0; i < len; i++)
            {
                if (ptr[i] == 2 && ptr[i + 1] == 8 && ptr[i + 3] == 0xE9 && ptr[i + 4] == 5)
                {
                    int codeStart = i;
                    int codeLen = ptr[i + 2] + 2;

                    int femaleStart = codeStart + 7;
                    int femaleLen = ptr[codeStart + 6] - 1;
                    int maleStart = femaleStart + femaleLen + 2;
                    int maleLen = ptr[maleStart - 1] - 1;

                    if (male)
                    {
                        for (int pos = maleStart; pos < maleStart + maleLen; pos++)
                        {
                            newText[currentPos] = ptr[pos];
                            currentPos++;
                        }
                    }
                    else
                    {
                        for (int pos = femaleStart; pos < femaleStart + femaleLen; pos++)
                        {
                            newText[currentPos] = ptr[pos];
                            currentPos++;
                        }
                    }

                    // Console.WriteLine($"Code: {Encoding.ASCII.GetString((ptr + codeStart), codeLen)}");
                    Console.WriteLine($"Prog: {Encoding.ASCII.GetString(newText)}");
                    Console.WriteLine(
                        $"femStart: {femaleStart} femLen: {femaleLen} maleStart: {maleStart} maleLen: {maleLen}");
                    i += codeLen;
                }
                else
                {
                    newText[currentPos] = ptr[i];
                    currentPos++;
                }
            }

            for (int i = 0; i < len; i++)
                ptr[i] = newText[i];
        }
    }
}