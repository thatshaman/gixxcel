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

namespace Gixxcel
{
    public enum GW2EntryType
    {
        Empty = 0,
        String = 1,
        Other = 2
    }

    public enum GW2Language
    {
        English = 0,
        Korean = 1,
        French = 2,
        German = 3,
        Spanish = 4,
        Chinese = 5
    }

    [Serializable]
    public class GW2Entry
    {
        public string value = "";
        public int row = -1;
        public GW2EntryType type = GW2EntryType.Empty;
        public DateTime stamp;
        
    }
}
