// ======================================================================
//  Unreal2 XMP Master Server
//  Copyright (C) 2010-2011  Adam Mummery-Smith
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//  Copyright Notice:
//  Unreal and the Unreal logo are registered trademarks of Epic
//  Games, Inc. ALL RIGHTS RESERVED.
// ======================================================================

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Text;
using XMPMS.Net.Packets;

namespace XMPMS.Core
{
    /// <summary>
    /// Encapsulates information for a player on a server. There is no continuity of players and player objects
    /// are destroyed each time a server sends a new gamestate packet.
    /// </summary>
    [DataContract(Name = "Player", Namespace = "http://schemas.datacontract.org/2004/07/XMPMS"), Serializable]
    public class Player : IExtensibleDataObject, INotifyPropertyChanged 
    {
        /// <summary>Player number, not really a unique identifier, just identifies the position of the player in the list</summary>
        [DataMember]
        public  int                        PlayerNumber  { get; private set;  }

        /// <summary>Player ID, player's index on the server</summary>
        [DataMember]
        public  int                        PlayerID      { get; private set;  }

        /// <summary>Player's IP address and port</summary>
        [DataMember]
        public  string                     Address       { get; private set;  }

        /// <summary>Player name</summary>
        [DataMember]
        public  string                     Name          { get; private set;  }

        /// <summary>Player's unknown to the server</summary>
        [DataMember]
        public  int                        Ping          { get; private set;  }

        /// <summary>Player score</summary>
        [DataMember]
        public  int                        Score         { get; private set;  }

        /// <summary>Player's stats ID, always zero</summary>
        [DataMember]
        public  int                        StatsID       { get; private set;  }

        /// <summary>Array of additional client data (protocol 2226 and above)</summary>
        [DataMember]
        public  Dictionary<string, string> Info          { get; internal set; }

        /// <summary>Extra data deserialised by the serialisation routine</summary>
        [Browsable(false)]
        public ExtensionDataObject         ExtensionData { get; set;          }

        /// <summary>
        /// Raised when a property value is changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public Player()
        { }

        /// <summary>
        /// Creates a new player object from information in a received server data packet
        /// </summary>
        /// <param name="packet">Packet containing player information at the current pointer position</param>
        /// <param name="address">Player address, from the server info header</param>
        public Player(Server server, InboundPacket packet, string address)
        {
            Address      = address;
            PlayerNumber = packet.PopInt();         OnPropertyChanged("PlayerNumber");

            if (server.Version > 2225)
            {
                PlayerID = packet.PopInt();         OnPropertyChanged("PlayerID");
            }

            Name          = packet.PopString();     OnPropertyChanged("Name");
            Ping          = packet.PopInt();        OnPropertyChanged("Ping");
            Score         = packet.PopInt();        OnPropertyChanged("Score");
            StatsID       = packet.PopInt();        OnPropertyChanged("StatsID");

            if (server.Version > 2225)
            {
                Info = packet.PopKeyValueArray();   OnPropertyChanged("Info");
            }
            else
            {
                Info = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Does what it says on the tin
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;

            if ((propertyChanged != null))
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
