/*  
    Copyright 2013 - 2021 That Shaman - thatshaman.com
    This file is part of Gixxcel.

    Gixxcel is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Gixxcel is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Gixxcel.  If not, see <http://www.gnu.org/licenses/>
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gixxcel
{
    [Serializable]
    public class GW2StringFile
    {
        // GW2 string files start with strs.
        private static readonly byte[] FourCC = Encoding.ASCII.GetBytes("strs");

        public List<GW2Entry> Items = new();
        public GW2Language Language = GW2Language.English;
        public string Filename = "";

        /// <summary>
        /// Empty GW2 string collection.
        /// </summary>
        public GW2StringFile()
        {
        }

        /// <summary>
        /// Creates a GW2 string collection and parses a string file.
        /// </summary>
        /// <param name="file">Raw string file extracted from GW2.dat or Local.dat</param>
        public GW2StringFile(string file)
        {
            Read(file);
        }

        /// <summary>
        /// Creates a GW2 string collection and parses a string file.
        /// </summary>
        /// <param name="file">Raw string file extracted from GW2.dat or Local.dat</param>
        /// <param name="timestamp">Mark entries with custom timestamp</param>
        public GW2StringFile(string file, DateTime timestamp)
        {
            Read(file, timestamp);
        }

        /// <summary>
        /// Parses a GW2 string file.
        /// </summary>
        /// <param name="file">Raw string file extracted from GW2.dat or Local.dat</param>
        /// <returns>Number of strings in collection</returns>
        public int Read(string file)
        {
            return Read(file, DateTime.Now);
        }

        /// <summary>
        /// Parses a GW2 string file.
        /// </summary>
        /// <param name="file">Raw string file extracted from GW2.dat or Local.dat</param>
        /// <param name="timestamp">Mark entries with custom timestamp</param>
        /// <returns>Number of strings in collection</returns>
        public int Read(string file, DateTime timestamp)
        {
            Items.Clear();
            Filename = Path.GetFileName(file);

            // Open the string file.
            byte[] fileBuffer = File.ReadAllBytes(file);

            // Byte 00 + 01 = string length
            // Byte 04      = string type
            byte[] header = new byte[6];
            byte[] strs = new byte[4];

            // Start reading after strs
            long position = strs.Length;

            // Stores row number in file
            int row = 0;

            // Make sure your file is at least 6 bytes
            if (fileBuffer.Length > header.Length)
            {
                Array.Copy(fileBuffer, 0, strs, 0, strs.Length);

                // Check FourCC and make sure the file uses a valid language
                if (fileBuffer[^2] < 6 && FourCC.SequenceEqual(strs))
                {
                    // Set language
                    Language = (GW2Language)fileBuffer[^2];

                    if (Language == GW2Language.Chinese)
                    {
                        Language = GW2Language.Chinese;
                    }

                    // Keep reading the file, we don't need the last 2 language bytes.
                    while (position < fileBuffer.Length - 2)
                    {
                        // Create a new entry
                        GW2Entry entry = new();
                        entry.row = row;
                        entry.stamp = timestamp;

                        // Read block header
                        Array.Copy(fileBuffer, position, header, 0, header.Length);
                        position += header.Length;

                        // Stores blocksize
                        // Get the string size
                        long blocksize = header[0] + (header[1] * 256) - header.Length;

                        if (blocksize <= 0)
                        {
                            // Empty block
                            entry.type = GW2EntryType.Empty;
                            entry.value = string.Empty;
                        }
                        else
                        {
                            // Read the block
                            if (header[4] == 16)
                            {
                                // UTF-16 String

                                entry.value = Encoding.Unicode.GetString(fileBuffer, (int)position, (int)blocksize);


                                entry.type = GW2EntryType.String;
                            }
                            else
                            {
                                // Other String
                                entry.type = GW2EntryType.Other;
                            }

                            // Moving on...
                            position += blocksize;
                        }

                        if (entry.type == GW2EntryType.String) Items.Add(entry);

                        // Next row
                        row++;
                    }
                }
            }


            // Done!
            return Items.Count;
        }
    }
}
