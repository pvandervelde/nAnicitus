﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18033
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Nanicitus.Service.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Nanicitus.Service.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service shut down complete..
        /// </summary>
        internal static string Log_Messages_ServiceEntryPoint_ServiceStopped {
            get {
                return ResourceManager.GetString("Log_Messages_ServiceEntryPoint_ServiceStopped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting service ....
        /// </summary>
        internal static string Log_Messages_ServiceEntryPoint_StartingService {
            get {
                return ResourceManager.GetString("Log_Messages_ServiceEntryPoint_StartingService", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping service ....
        /// </summary>
        internal static string Log_Messages_ServiceEntryPoint_StoppingService {
            get {
                return ResourceManager.GetString("Log_Messages_ServiceEntryPoint_StoppingService", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provides a gateway for adding and updating symbols and indexed source..
        /// </summary>
        internal static string Service_Description {
            get {
                return ResourceManager.GetString("Service_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Nanicitus symbol index gateway.
        /// </summary>
        internal static string Service_DisplayName {
            get {
                return ResourceManager.GetString("Service_DisplayName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Nanicitus.
        /// </summary>
        internal static string Service_ServiceName {
            get {
                return ResourceManager.GetString("Service_ServiceName", resourceCulture);
            }
        }
    }
}
