using System;

/// This code should be placed in a separate
/// assembly that is automatically-loaded at
/// startup. Additionally, no types derived
/// from Command<T> should appear in this
/// assembly.

namespace Autodesk.AutoCAD.Runtime
{
   [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
   public class CommandAttribute : Attribute, ICommandLineCallable
   {
      private string groupName;
      private string globalName;
      private string localizedNameId;
      private CommandFlags flags;
      private Type contextMenuExtensionType;
      private string helpFileName;
      private string helpTopic;
      public virtual string HelpTopic => helpTopic;
      public virtual string HelpFileName => helpFileName;
      public virtual Type ContextMenuExtensionType => contextMenuExtensionType;
      public virtual CommandFlags Flags => flags;
      public virtual string GroupName => groupName;
      public virtual string LocalizedNameId => localizedNameId;
      public virtual string GlobalName => globalName;

      public CommandAttribute(string globalName)
      {
         groupName = null;
         this.globalName = globalName;
         this.localizedNameId = null;
         this.flags = CommandFlags.Modal;
         this.contextMenuExtensionType = null;
         this.helpFileName = null;
         this.helpTopic = null;
      }

      public CommandAttribute(string globalName, CommandFlags flags)
      {
         this.groupName = null;
         this.globalName = globalName;
         this.localizedNameId = null;
         this.flags = flags;
         this.contextMenuExtensionType = null;
         this.helpFileName = null;
         this.helpTopic = null;
      }

      public CommandAttribute(string groupName, string globalName, CommandFlags flags)
      {
         this.groupName = groupName;
         this.globalName = globalName;
         this.localizedNameId = null;
         this.flags = flags;
         this.contextMenuExtensionType = null;
         this.helpFileName = null;
         this.helpTopic = null;
      }

      public CommandAttribute(string groupName, string globalName, string localizedNameId, CommandFlags flags)
      {
         this.groupName = groupName;
         this.globalName = globalName;
         this.localizedNameId = localizedNameId;
         this.flags = flags;
         this.contextMenuExtensionType = null;
         this.helpFileName = null;
         this.helpTopic = null;
      }

      public CommandAttribute(string groupName, string globalName, string localizedNameId, CommandFlags flags, Type contextMenuExtensionType)
      {
         this.groupName = groupName;
         this.globalName = globalName;
         this.localizedNameId = localizedNameId;
         this.flags = flags;
         this.contextMenuExtensionType = contextMenuExtensionType;
         this.helpFileName = null;
         this.helpTopic = null;
      }

      public CommandAttribute(string groupName, string globalName, string localizedNameId, CommandFlags flags, string helpTopic)
      {
         this.groupName = groupName;
         this.globalName = globalName;
         this.localizedNameId = localizedNameId;
         this.flags = flags;
         this.contextMenuExtensionType = null;
         this.helpFileName = null;
         this.helpTopic = helpTopic;
      }

      public CommandAttribute(string groupName, string globalName, string localizedNameId, CommandFlags flags, Type contextMenuExtensionType, string helpFileName, string helpTopic)
      {
         this.groupName = groupName;
         this.globalName = globalName;
         this.localizedNameId = localizedNameId;
         this.flags = flags;
         this.contextMenuExtensionType = contextMenuExtensionType;
         this.helpFileName = helpFileName;
         this.helpTopic = helpTopic;
      }
   }


}
