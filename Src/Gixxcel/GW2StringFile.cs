/*  
    Copyright 2013 That Shaman - thatshaman.blogspot.com
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
        private static readonly byte[] FourCC = System.Text.Encoding.ASCII.GetBytes("strs");

        public List<GW2Entry> Items = new List<GW2Entry>();
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
            byte[] fileBuffer = System.IO.File.ReadAllBytes(file);

            // Byte 00 + 01 = string length
            // Byte 04      = string type
            byte[] header = new byte[6];
            byte[] strs = new byte[4];

            // Start reading after strs
            long position = 4;

            // Stores blocksize
            long blocksize = 0;

            // Stores row number in file
            int row = 0;

            // Make sure your file is at least 6 bytes
            if (fileBuffer.Length > 6)
            {
                Array.Copy(fileBuffer, 0, strs, 0, 4);

                // Check FourCC and make sure the file uses a valid language
                if (fileBuffer[fileBuffer.Length - 2] < 5 && FourCC.SequenceEqual(strs))
                {
                    // Set language
                    Language = (GW2Language) fileBuffer[fileBuffer.Length - 2];

                    // Keep reading the file, we don't need the last 2 language bytes.
                    while (position < fileBuffer.Length - 2)
                    {
                        // Create a new entry
                        GW2Entry entry = new GW2Entry();
                        entry.row = row;
                        entry.stamp = timestamp;

                        // Read block header
                        Array.Copy(fileBuffer, position, header, 0, 6);
                        position += 6;

                        // Get the string size
                        blocksize = header[0] + (header[1] * 256) - 6;
                        
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

                        // Add entry
                        // NOTE: I've only set it to store actual strings to save all types change this line to:
                        //
                        //      Items.Add(entry);
                        //
                        if (entry.type == GW2EntryType.String) Items.Add(entry);

                        // Next row
                        row++;
                    }
                }
            }

            fileBuffer = null;

            // Done!
            return Items.Count;
        }
    }
}
