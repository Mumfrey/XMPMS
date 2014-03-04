using System;
using System.Collections.Generic;
using System.Text;
using XMPMS.Net.Packets;
using System.Text.RegularExpressions;

namespace DevTools
{
    public class PacketAnalyser
    {
        private InboundPacket packet;

        private string structure;

        private int unknownCounter = 0;

        public string Structure
        {
            get
            {
                return structure;
            }

            set
            {
                structure = value;
                Analyse();
            }
        }

        private List<string> result = new List<string>();

        public string Result
        {
            get
            {
                return String.Join("\r\n", result.ToArray());
            }
        }

        private string tail = String.Empty;

        public string Tail
        {
            get
            {
                return tail;
            }
        }

        Regex structureRegex = new Regex(@"^(?<pop>[buicsk]):?(?<name>|.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture); 

        public PacketAnalyser(InboundPacket packet)
        {
            this.packet = packet;
            this.tail = packet.Print();
        }    

        private void Analyse()
        {
            result.Clear();
            unknownCounter = 0;
            tail = packet.PrintBytes();
            packet.Rewind();

            MatchCollection variables = structureRegex.Matches(structure.Replace("\r", ""));

            bool continueParsing = true;

            foreach (Match variable in variables)
            {
                string name = (variable.Groups["name"].Value != "") ? variable.Groups["name"].Value : String.Format("Unknown{0}", ++unknownCounter);

                switch (variable.Groups["pop"].Value.ToLower())
                {
                    case "b":
                        continueParsing = PopByte(name);
                        break;

                    case "u":
                        continueParsing = PopUShort(name);
                        break;

                    case "i":
                        continueParsing = PopInt(name);
                        break;

                    case "c":
                        continueParsing = PopCompactIndex(name);
                        break;

                    case "s":
                        continueParsing = PopString(name);
                        break;

                    case "k":
                        continueParsing = PopKeyValueArray(name);
                        break;
                }

                if (!continueParsing) break;
            }

            tail = packet.Print(packet.Pointer);
        }

        private bool PopByte(string name)
        {
            try
            {
                byte b = packet.PopByte();
                return Log("byte {0} = 0x{1:x2}", name, b);
            }
            catch
            {
                return Error("No byte available");
            }
        }

        private bool PopUShort(string name)
        {
            try
            {
                ushort u = packet.PopUShort();
                return Log("ushort {0} = 0x{1:x4}", name, u);
            }
            catch
            {
                return Error("No ushort available");
            }
        }

        private bool PopInt(string name)
        {
            try
            {
                int i = packet.PopInt();
                return Log("int {0} = {1}", name, i);
            }
            catch
            {
                return Error("No int available");
            }
        }

        private bool PopCompactIndex(string name)
        {
            try
            {
                int c = packet.PopCompactIndex();
                return Log("compactindex {0} = {1}", name, c);
            }
            catch
            {
                return Error("No compactindex available");
            }
        }

        private bool PopString(string name)
        {
            try
            {
                string s = packet.PopString();
                return Log("string {0} = \"{1}\"", name, s);
            }
            catch
            {
                return Error("No string available");
            }
        }

        private bool PopKeyValueArray(string name)
        {
            try
            {
                Dictionary<string, string> kva = new Dictionary<string, string>();

                // We need to pop twice as many elements as entries since each entry consists of two strings
                int arraySize = packet.PopCompactIndex();

                Log("array {0}[{1}] = ", name, arraySize);

                arraySize *= 2;
                string[] parts = new string[arraySize];

                for (int arrayPos = 0; arrayPos < arraySize; arrayPos++)
                {
                    parts[arrayPos] = packet.PopString();

                    if (arrayPos % 2 == 1)
                    {
                        Log("    [{0}]\"{1}\" => \"{2}\"", arrayPos / 2, parts[arrayPos - 1], parts[arrayPos]);
                    }
                }

                return true;
            }
            catch
            {
                return Error("Error reading array");
            }
        }

        private bool Log(string message, params object[] args)
        {
            result.Add(String.Format(message, args));
            return true;
        }

        private bool Error(string message)
        {
            result.Add(String.Format("ERROR: {0}", message));
            return false;
        }
    }
}
