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

namespace XMPMS.Properties {
    
    
    public sealed partial class Settings
    {
        /// <summary>
        /// Set a property value by name, used to support setting configuration options on the command line
        /// </summary>
        /// <param name="propertyName">Name of the property to set (case insensitive)</param>
        /// <param name="propertyValue">New value for the property (must parse to the correct type)</param>
        /// <returns>True if the property was set</returns>
        public bool SetProperty(string propertyName, string propertyValue)
        {
            if (this.PropertyValues != null)
            {
                foreach (System.Configuration.SettingsPropertyValue value in this.PropertyValues)
                {
                    if (value.Property.Name.ToLower() == propertyName.ToLower())
                    {
                        return SetPropertyDynamic(value.Property.Name, value.Property.PropertyType, propertyValue);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Ser a property by name, internal function to support SetProperty
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        private bool SetPropertyDynamic(string propertyName, Type propertyType, string propertyValue)
        {
            switch (propertyType.Name)
            {
                case "Boolean": this[propertyName] = Convert.ToBoolean(propertyValue); Save(); return true;
                case "UInt16":  this[propertyName] = Convert.ToUInt16(propertyValue);  Save(); return true;
                case "Int32":   this[propertyName] = Convert.ToInt32(propertyValue);   Save(); return true;
                case "String":  this[propertyName] = propertyValue;                    Save(); return true;
            }

            return false;
        }
    }
}
