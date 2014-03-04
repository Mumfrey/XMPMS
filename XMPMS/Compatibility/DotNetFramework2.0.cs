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
using System.Text;

// Ugly nasty support for compiling under .net framework 2.0
// May not work any more ?!?

#if !WCF

    #warning "WCF functionality disabled - System.ServiceModel.Security provided by compatibility class"
    namespace System.ServiceModel.Security
    {
        public class UserNamePasswordClientCredential
        {
            public string UserName { get; set; }
            public string Password { get; set; }
        }
    }

    #warning "WCF functionality disabled - System.ServiceModel.Description provided by compatibility class"
    namespace System.ServiceModel.Description
    {
        public class ClientCredentials
        {
            public System.ServiceModel.Security.UserNamePasswordClientCredential UserName { get; set; }
        }
    }

    #warning "WCF functionality disabled - System.ServiceModel.Channels provided by compatibility class"
    namespace System.ServiceModel.Channels
    {
        public class Binding
        {
        }
    }

    #warning "WCF functionality disabled - System.ServiceModel provided by compatibility class"
    namespace System.ServiceModel
    {
        public enum InstanceContextMode
        {
            Single
        }

        public interface IServiceBehavior
        {
        }

        public class ServiceBehaviorAttribute : Attribute, IServiceBehavior
        {
            public InstanceContextMode InstanceContextMode
            {
                get;
                set;
            }
        }

        public class EndpointAddress
        {
            public EndpointAddress(string uri)
            {
            }
        }

        public class ServiceHost
        {
        }

        public class HttpTransportSecurity
        {
        }

        public class BasicHttpSecurity
        {
            public HttpTransportSecurity Transport { get; set; }
        }

        public class BasicHttpBinding : System.ServiceModel.Channels.Binding
        {
            public BasicHttpSecurity Security { get; set;  }

            public BasicHttpBinding(BasicHttpSecurityMode mode)
            {
            }
        }

        public class OperationContractAttribute : Attribute
        {
        }

        public class ServiceContractAttribute : Attribute
        {
        }

        public enum BasicHttpSecurityMode
        {
            TransportCredentialOnly
        }

        public enum HttpClientCredentialType
        {
            Basic
        }

        public class ClientBase<TChannel>
        {
            protected TChannel Channel;

            protected System.ServiceModel.Description.ClientCredentials ClientCredentials;

            public ClientBase(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress endpoint)
            {
            }
        }

        public class FaultException : Exception
        {
            public FaultException(string message)
                : base(message)
            { }
        }
    }

    #warning "WCF functionality disabled - System.IdentityModel.Selectors provided by compatibility class"
    namespace System.IdentityModel.Selectors
    {
        public abstract class UserNamePasswordValidator
        {
            public abstract void Validate(string userName, string password);
        }
    }

    #warning "WCF functionality disabled - System.Runtime.Serialization provided by compatibility class"
    namespace System.Runtime.Serialization
    {
        public interface IExtensibleDataObject
        {
        }

        public class ExtensionDataObject
        {
        }

        public class DataContractAttribute : Attribute
        {
            public string Name { get; set; }
            public string Namespace { get; set; }
        }

        public class DataMemberAttribute : Attribute
        {
        }
    }
#endif
