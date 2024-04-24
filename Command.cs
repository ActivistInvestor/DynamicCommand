using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;
using AcRx = Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.Runtime
{
   /// <summary>
   /// ActivistInvestor / Tony T
   /// 
   /// Command<T> Class
   /// 
   /// A base type for classes that implement 
   /// dynamically-defined AutoCAD commands that
   /// can act like registered commands and like
   /// ICommands.
   /// 
   /// The novel aspect of this class is that it
   /// can be used to implement both a registered
   /// command as well as an ICommand with a single
   /// unified implementation.
   /// 
   /// To define and implement a command, derive
   /// a type from this class. By default, the name
   /// of the command is the name of the derived
   /// class, but can be specified explicitly using
   /// the included [Command] attribute (included
   /// in the file CommandAttribute.cs).
   /// 
   /// Once created, an instance of a derived type
   /// can be invoked by issuing the command's name
   /// on the command line, or can be invoked via
   /// the UI framework as an ICommand, by binding
   /// the static Instance property to a UI element's
   /// Command property or be exposed as a property 
   /// of a view model class (see the example below).
   /// 
   /// The [Command] Attribute can be applied to a
   /// derived type to specify the same information
   /// which the CommandMethod attribute allows you
   /// to provide (it's essentially a knock-off of
   /// same), such as the command's name, and the
   /// CommandFlags). See CommandAttribute.cs. 
   /// 
   /// In the current version, the only properties
   /// of the CommandAttribute that are used are
   /// the GlobalName and Flags properties.
   /// 
   /// Note that the roadmap for this class is to 
   /// merge it with the DocumentRelayCommand class 
   /// that can be found here:
   /// 
   ///   https://github.com/ActivistInvestor/CommunityToolKitExtensions/blob/main/LegacyDocumentRelayCommand.cs
   ///    
   /// That will provide the ability to implement an 
   /// ICommand that can also act as a registered 
   /// command, and is currently a work-in-progress.
   /// Some of that work is completed, but it remains
   /// largely-untested. Please provide feedback if 
   /// you encounter any issues.
   /// 
   /// There are also some major design decisions that
   /// are currently being contemplated, relating to
   /// the singleton pattern, and how that relates to
   /// using a RelayCommand-type pattern where caller-
   /// supplied delegates implement the command. 
   /// 
   /// The design as-is doesn't allow multiple instances
   /// of the same derived type to be created, but that
   /// would be necessary in order to support delegate-
   /// based command implementation in the same fashion
   /// that RelayCommand does.
   /// 
   /// The roadmap for this type is to fully-incorporate
   /// DocumentRelayCommand functionality as mentioned 
   /// above, but that will require significant changes.
   /// 
   /// Currently, with the exception of using delegates
   /// to implement commands, all of DocumentRelayCommand's
   /// other functionality (e.g., execution in the document
   /// context, etc.) have been incorporated into this type.
   /// 
   /// ************** Caveat Emptor ******************
   /// Notice:
   /// 
   /// This class relies on undocumented, unsupported,
   /// 'for internal use only' APIs. As such, all of
   /// the standard caveats apply. Any use of this API
   /// is entirely at your own risk. 
   /// 
   /// While I doubt that the unsupported APIs will 
   /// suddenly vanish given that the people reponsible 
   /// for them and the managed API are well-aware of 
   /// the fact that many customers have come to depend 
   /// on them (mostly out of need, and resulting from 
   /// significant shortcomings or gaps/holes in the 
   /// public API) one never knows what the future holds.
   /// 
   /// All undocumented/unsupported APIs used by this
   /// code are also used by AutoCAD's Action Recorder.
   /// 
   /// ************************************************
   /// Required Prerequisites:
   /// 
   /// CommandContext.cs   
   /// CommandAttribute.cs 
   /// 
   /// Notes:
   /// 
   /// TODO: Support for localization of command names and
   ///       various other properties of the CommandAttribute
   ///       
   /// </summary>
   /// <typeparam name="T">The concrete type that is
   /// derived from this type. The name of the derived
   /// type by-default becomes the name of the command
   /// unless otherwise specified.</typeparam>

   /// Refactord to encapsulate non-generic functionality
   /// in a non-generic base type. This was necessary to
   /// maintain a static dictionary of all derived types.

   public abstract class CommandBase : IDisposable, ICommand
   {
      protected static Collection commands = new Collection();
      string name;
      CommandAttribute attribute;
      CommandFlags flags = CommandFlags.Modal;
      string group = "DYNAMIC_COMMANDS";
      bool disposed;
      object defaultParameter = null;
      InvocationContext context = InvocationContext.None;
      bool IsDisposed => disposed;

      protected CommandBase(string globalName = null)
      {
         string tempName;
         if(!string.IsNullOrWhiteSpace(globalName))
            tempName = globalName.ToUpper();
         else
            tempName = this.GetType().Name.ToUpper();
         this.name = ValidateCommandName(tempName);
         ValidateSingletonInstance();
         attribute = TrySetFromAttribute(
            this.GetType().GetCustomAttribute<CommandAttribute>(false));
         RegisterCommand(this.name, this.execute);
      }

      protected CommandAttribute TrySetFromAttribute(CommandAttribute attribute)
      {
         if(attribute != null)
         {
            if(!string.IsNullOrEmpty(attribute.GlobalName))
               this.name = attribute.GlobalName;
            if(!string.IsNullOrEmpty(attribute.GroupName))
               this.group = attribute.GroupName;
            this.flags = attribute.Flags;
         }
         return attribute;
      }

      /// <summary>
      /// It is up to derived types to override this to
      /// enforce the singleton pattern. This base type
      /// does not, but provides entry points that allow
      /// derived types to do it.
      /// 
      /// If a derived type enforces the singleton pattern,
      /// it should not have a public constructor.
      /// 
      /// Enforcing the singleton pattern in derived types
      /// requires this to be overridden to validates that 
      /// there is not already an instance of the concrete 
      /// derived type created. If an instance has already 
      /// been created and assigned to the static Instance 
      /// property, an exception should be thrown.
      /// 
      /// If an instance has not been previously-created, 
      /// this instance should be assigned to the static 
      /// Instance property.
      /// 
      /// Note that this is called from the constructor
      /// of this type, which happens before constructors
      /// of derived types are called. 
      /// </summary>
      /// <exception cref="InvalidOperationException"></exception>

      protected virtual CommandBase ValidateSingletonInstance()
      {
         return this;
      }

      /// <summary>
      /// Gets the singleton instance if that pattern is
      /// enforced by a derived type that overrides this
      /// (in which case, the singleton instance should be 
      /// the current instance).
      /// 
      /// Note that this is called from the constructor
      /// of this type, which happens before constructors
      /// of derived types are called. 
      /// </summary>
      /// <returns></returns>

      protected virtual CommandBase GetSingletonInstance()
      {
         return null;
      }

      /// <summary>
      /// Validates the argument as being elegible for
      /// registering as a command with the given name.
      /// If the given name cannot be registered as a 
      /// command (e.g., the name is already in use) an 
      /// exception should be thrown.
      /// 
      /// If the name argument can be used as the name
      /// of the registered command, it is assigned to
      /// the GlobalName property and name field.
      /// 
      /// Note that this is called from the constructor
      /// of this type, which happens before constructors
      /// of derived types are called. 
      /// </summary>
      /// <param name="name"></param>
      /// <exception cref="InvalidOperationException"></exception>

      protected virtual string ValidateCommandName(string name)
      {
         if(commands.Contains(name))
            throw new InvalidOperationException($"Duplicate command name: {name}");
         if(IsCommandNameInUse(name))
            throw new InvalidOperationException(
               $"A command with the name {name} is already defined.");
         return name;
      }

      /// <summary>
      /// Registers a command whose group and command 
      /// names are the values of the GroupName and 
      /// GlobalName properties respectively, according to
      /// the CommandFlags returned by the Flags property.
      /// 
      /// Note that this is called from the constructor
      /// of this type, which happens before constructors
      /// of derived types are called. 
      /// </summary>
      /// <param name="callback">The action that handles
      /// the command's invocation</param>

      protected virtual void RegisterCommand(string name, Action action)
      {
         Utils.AddCommand(group, name, name, Flags, new CommandCallback(action));
         Add(this);
      }

      /// <summary>
      /// Unregisters the previously-registered command
      /// </summary>
      /// <param name="group"></param>
      /// <param name="name"></param>

      /// TODO: instance is a static member of derived generic types
      protected virtual void RevokeCommand()
      {
         Utils.RemoveCommand(group, GlobalName);
         Remove(this);
      }

      /// <summary>
      /// Provided to inform derived types that enforce 
      /// a singleton pattern that the singleton instance
      /// is being disposed, and that they should clear
      /// the static instance field.
      /// </summary>

      protected virtual void ClearSingletonInstance()
      {
      }

      /// <summary>
      /// Adds the instance to the dictionary/collection of
      /// all derived types keyed to their command name.
      /// </summary>
      /// <param name="item"></param>
      /// <exception cref="ArgumentNullException"></exception>
      /// <exception cref="InvalidOperationException"></exception>

      static void Add(CommandBase item)
      {
         if(item == null)
            throw new ArgumentNullException(nameof(item));
         if(string.IsNullOrWhiteSpace(item.GlobalName))
            throw new InvalidOperationException("Global name is null or empty");
         commands.Add(item);
      }

      protected static void Contains(CommandBase item)
         => commands.ContainsItem(item);

      static bool Remove(CommandBase item) => commands.Remove(item);

      /// <summary>
      /// If result is CommandType.Undefined, the command
      /// is undefined and avaialble for use.
      /// 
      /// See Autodesk.AutoCAD.Internal.CommandTypeFlags
      /// for meanings of other values.
      /// </summary>
      /// <param name="name"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentException"></exception>

      public static bool IsCommandNameInUse(string name)
      {
         if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(nameof(name));
         return Utils.IsCommandNameInUse(name) != CommandTypeFlags.NoneCmd;
      }

      public static CommandType GetCommandType(string name)
      {
         if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(nameof(name));
         return (CommandType)Utils.IsCommandNameInUse(name);
      }

      public string GlobalName => name;
      public string GroupName => group;
      public bool IsExecuting => Context != InvocationContext.None;
      public bool IsModal => !Flags.HasFlag(CommandFlags.Session);

      public InvocationContext Context
      {
         get => context;
         protected set
         {
            if(value != context)
            {
               context = value;
               NotifyCanExecuteChanged();
            }
         }
      }

      /// <summary>
      /// Adds the Session flag if this command is not a modal command:
      /// </summary>
      
      protected InvocationContext GetEffectiveContext(InvocationContext invocationContext)
      {
         return invocationContext | (!IsModal ? InvocationContext.Session : 0);
      }

      /// <summary>
      /// The parameter passed to Execute() when the command
      /// is invoked as a registered command. If this value is
      /// not set, the value of the Context property is passed.
      /// This value is also passed to Execute() when invoked
      /// as an ICommand, if the ICommand.Execute() method is
      /// passed a null argumment.
      /// </summary>

      public object DefaultParameter
      {
         get => defaultParameter ?? this.Context;
         set => defaultParameter = value;
      }

      /// <summary>
      /// Can be overridden in a derived type to provide
      /// a different set of CommandFlags. Use with care.
      /// 
      /// Note that this is called from the constructor
      /// of this type, which happens before constructors
      /// of derived types are called. 
      /// </summary>

      public virtual CommandFlags Flags => flags;

      /// <summary>
      /// disposing should never be false until shutdown,
      /// because instances are always reachable as long
      /// as they are elements of the commands collection.
      /// </summary>
      /// <param name="disposing"></param>

      protected virtual void Dispose(bool disposing)
      {
         if(disposing)
         {
            RevokeCommand();
            ClearSingletonInstance();
         }
      }

      /// <summary>
      /// Calling this unregisters the command which 
      /// will cause it to no longer be available as
      /// a registered command. It also clears the
      /// instance field, allowing another instance
      /// to be created and to become the singleton
      /// instance.
      /// 
      /// Note that if a command is to have session 
      /// scope, it isn't necessary to call this at 
      /// shutdown.
      /// </summary>

      public void Dispose()
      {
         if(!disposed)
         {
            Dispose(true);
            disposed = true;
         }
         GC.SuppressFinalize(this);
      }

      /// <summary>
      /// Command callback, invoked by AutoCAD when
      /// the user issues the registered command.
      /// </summary>

      void execute()
      {
         Execute(DefaultParameter, InvocationContext.Implicit);
      }

      void CheckDisposed()
      {
         if(disposed)
            throw new ObjectDisposedException($"{name} has been disposed.");
      }

      /// <summary>
      /// Convenience properties for use by Execute() overrides:
      /// </summary>

      /// The DocumentCollection
      protected static DocumentCollection Documents =>
         Application.DocumentManager;

      /// <summary>
      /// The active Document. Throws an exception if
      /// there is no active document. 
      /// 
      /// Use the HasDocument property to check first 
      /// if needed.
      /// </summary>

      protected static Document Document
      {
         get
         {
            Document doc = Documents.MdiActiveDocument;
            if(doc == null)
               throw new AcRx.Exception(ErrorStatus.NoDocument);
            return doc;
         }
      }

      /// <summary>
      /// Indicates if there is an active document
      /// </summary>

      public static bool HasDocument => Documents.MdiActiveDocument != null;

      /// <summary>
      /// The active document's Editor 
      /// 
      /// Throws an exception if there is no active document.
      /// </summary>
      
      protected static Editor Editor => Document.Editor;

      //////////////////////////////////////////////////////////////
      /// <summary>
      /// ICommand implementation (derived from DocumentRelayCommand)
      /// </summary>

      public event EventHandler CanExecuteChanged;

      protected virtual void NotifyCanExecuteChanged()
      {
         CanExecuteChanged?.Invoke(this, EventArgs.Empty);
      }

      /// <summary>
      /// If set to true, the ICommand will appear to
      /// be unavailable if the drawing editor is not
      /// quiescent. Note that this doesn't prevent a
      /// command from executing as a registered command
      /// by entering its name of the command line.
      /// </summary>

      public bool QuiescentOnly { get; set; } = false;

      /// <summary>
      /// 
      /// This method returns true if:
      /// 
      ///   1. There is an active document, and
      ///   
      ///   2. QuiescentOnly is false, or the active
      ///      document is quiescent, and
      ///      
      ///   3. This command is not currently executing.
      ///   
      /// </summary>
      /// <param name="parameter"></param>
      /// <returns></returns>

      public virtual bool CanExecute(object parameter)
      {
         return CommandContext.CanInvoke(QuiescentOnly, true)
            && !IsExecuting;
      }

      /// <summary>
      /// Invoked by the UI framework only.
      /// </summary>
      /// <param name="parameter"></param>

      /// Notes: cannot assume that this is being called
      /// from the application context. So, if the command
      /// has CommandFlags.Session and this is called from
      /// the document context, Execute() must be run in
      /// the application context, which leads us to the
      /// problem that ExecuteInApplicationContext() may 
      /// not execute the delegate until after this method 
      /// returns control to AutoCAD. Still not sure how to 
      /// deal with this rare edge case.

      void ICommand.Execute(object parameter)
      {
         Execute(parameter, InvocationContext.Explicit);
      }

      /// <summary>
      /// Sets up the instance for a call the virtual Execute() method.
      /// </summary>
      /// <param name="parameter"></param>

      async void Execute(object parameter, InvocationContext context)
      {
         this.Context = GetEffectiveContext(context);
         parameter = parameter ?? DefaultParameter;
         try
         {
            if(Documents.IsApplicationContext)
            {
               if(IsModal)
                  await CommandContext.InvokeAsync(() => this.Execute(parameter));
               else
                  this.Execute(parameter);
            }
            else   // we're running in the document context
            {
               if(IsModal)
                  this.Execute(parameter);
               else
                  /// CommandFlags.Session command in document context...
                  throw new AcRx.Exception(AcRx.ErrorStatus.InvalidContext);

                  // Edge case cannot work because it can't be awaited
                  //   Documents.ExecuteInApplicationContext(
                  //      (obj) => this.Execute(parameter), null);
            }
         }
         finally
         {
            this.Context = InvocationContext.None;
         }
      }

      /// <summary>
      /// Provides a means for ad-hoc execution of the command
      /// from an unknown context.
      /// </summary>
      /// <param name="parameter"></param>

      public void ExecuteExternal(object parameter)
      {
         Execute(parameter, InvocationContext.External);
      }


      /// <summary>
      /// Override this to implement unified command functionality.
      /// The code in the override of this method will execute in
      /// all execution contexts (e.g., as a registered command or
      /// as an ICommand).
      /// 
      /// The Context property of this class indicates how the
      /// command's execution was initiated (AutoCAD command or
      /// ICommand.Execute()).
      /// </summary>
      /// <param name="parameter">The current value of the Context 
      /// property when the command was invoked by AutoCAD as a 
      /// result of the user issuing it. Otherwise this is the value 
      /// that was passed to the ICommand.Execute() method if not 
      /// null, or the value of the DefaultParameter property if the 
      /// value passed to ICommand.Execute() was null.</param>

      protected abstract void Execute(object parameter);



#if(AUTOINIT) // Currently-disabled, untested

      /// <summary>
      /// Infrastructure for automated initialization,
      /// which is disabled because it is currently not 
      /// working as intended.
      /// 
      /// To trigger Automatic initialization of derived
      /// types in all currently and subsequently-loaded
      /// assemblies, call the Initalize() method before
      /// referencing the Instance property of any derived
      /// type. 
      /// 
      /// After one or more instances of a derived concrete
      /// type have been created by referencing the Instance 
      /// property or by calling a constructor, this method 
      /// cannot be used.
      /// </summary>

      static bool initialized = false;
      public static void Initialize()
      {
         if(commands.Count > 0)
            throw new InvalidOperationException(
               "Initialize() must be called before instances are created");
         if(!initialized)
         {
            Initialize(null);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            initialized = true;
         }
      }

      static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
      {
         Initialize(args.LoadedAssembly);
      }

      static void Initialize(Assembly asm)
      {
         foreach(Type type in typeof(CommandBase).GetTypes(asm))
         {
            Activator.CreateInstance(type);
         }
      }

#endif


      /// <summary>
      /// Commands should not be explicitly removed from this
      /// collection. Instead, call their Dispose() method, 
      /// which will remove the instance from the collection.
      /// </summary>

      protected class Collection : KeyedCollection<string, CommandBase>
      {
         /// <summary>
         /// Storing items in triplicate, to allow fast lookup
         /// of instances as well as command names.
         /// </summary>

         HashSet<CommandBase> set = new HashSet<CommandBase>();

         public Collection() : base(StringComparer.OrdinalIgnoreCase)
         {
         }

         public bool ContainsItem(CommandBase item) => set.Contains(item);

         protected override void InsertItem(int index, CommandBase item)
         {
            set.Add(item);
            base.InsertItem(index, item);
         }

         protected override void RemoveItem(int index)
         {
            var item = base[index];
            set.Remove(item);
            base.RemoveItem(index);
            item?.Dispose();
         }

         protected override void ClearItems()
         {
            foreach(var item in base.Items)
               item?.Dispose();
            set.Clear();
            base.ClearItems();
         }

         protected override void SetItem(int index, CommandBase item)
         {
            var existg = base[index];
            if(existg != item)
            {
               set.Remove(existg);
               existg?.Dispose();
            }
            base.SetItem(index, item);
         }

         protected override string GetKeyForItem(CommandBase item)
         {
            return item.GlobalName;
         }
      }
   }


   public abstract class Command<T> : CommandBase
      where T : Command<T>
   {
      static T instance = null;

      protected Command()
      {
         if(instance == null)
            instance = (T) this;
      }

      /// <summary>
      /// The base type does not enforce the singleton pattern.
      /// It leaves that up to derived types. This generic type
      /// must be a singleton, because the command name is by-
      /// default the name of the class, hence multiple instances 
      /// of the same type will try to register a command with the 
      /// same name, causing a failure.
      /// </summary>
      /// <returns></returns>
      /// <exception cref="InvalidOperationException"></exception>

      protected override CommandBase ValidateSingletonInstance()
      {
         var instance = GetSingletonInstance();
         if(instance != null && instance != this)
            throw new InvalidOperationException(
               $"Singleton violation: {this.GetType().Name}, Use the Instance property");
         instance = this;
         return this;
      }

      protected override CommandBase GetSingletonInstance()
      {
         return instance;
      }

      /// <summary>
      /// Provided for future enhancements that might allow 
      /// multiple instances, along with a 'default' singleton 
      /// instances (one of several possibilities).
      /// </summary>
      
      public bool IsSingleton => instance == this;
      
      /// <summary>
      /// Clears the singleton instance when it is
      /// disposed.
      /// </summary>

      protected override void ClearSingletonInstance()
      {
         if(instance == this)
            instance = null;
      }

      /// <summary>
      /// Accesses (and if necessary, creates) the singleton 
      /// instance of the concrete derived type.
      /// 
      /// Creating multiple instances of the same derived 
      /// type is not possible. If an instance has already 
      /// been created, the constructor throws an exception.
      /// </summary>

      public static T Instance 
      { 
         get 
         {
            if(instance == null)
               Activator.CreateInstance<T>();
            return (T) instance;
         } 
      }
   }

   [Flags]
   public enum InvocationContext
   {
      None = 0,       // Not currently executing.
      Implicit = 1,   // Invoked by AutoCAD as a registered command
      Explicit = 2,   // Invoked via some other means (e.g., ICommand)
      Session = 4,    // Executing in the application execution context
      External = 8    // Invoked by a call to ExternalExecute() 
   }

   /// <summary>
   /// Corresponds to Autodesk.AutoCAD.Internal.CommandTypeFlags
   /// </summary>
   
   [Flags]
   public enum CommandType
   {
      Undefined = 0,
      Core = 1,
      ARX = 2,
      Lisp = 3,
      ActionMacro = 4,
      Setvar = 5
   }

   static class CommandTypeExtensions
   {
      public static bool IsStatic(this Type type)
      {
         return !(type.IsAbstract && type.IsSealed);
      }

      /// <summary>
      /// Gets all non-abstract types derived from the
      /// type this method is invoked on, in the given
      /// Assembly, or all assemblies.
      /// </summary>
      /// <param name="baseType"></param>
      /// <param name="asm"></param>
      /// <returns></returns>
      /// <exception cref="ArgumentNullException"></exception>
      
      public static IEnumerable<Type> GetConcreteTypes(this Type baseType, Assembly asm = null)
      {
         if(baseType == null)
            throw new ArgumentNullException(nameof(baseType));
         if(baseType.IsStatic())
            return Enumerable.Empty<Type>();
         var list = asm != null ? new Assembly[] { asm } 
           : AppDomain.CurrentDomain.GetAssemblies();
         return list.SelectMany(asm => asm.ExportedTypes)
           .Where(type => !type.IsAbstract && type.IsSubclassOf(baseType));
      }
   }

   //////////////////////////////////////////////////////////
   /// Examples:

   /// <summary>
   /// A minimal implementation of a working command that
   /// draws a Circle. The name of the command is the name
   /// of the class (MYCIRCLE).
   /// 
   /// Unlike CommandMethod commands, the command is not
   /// registered and avaiable for use until an instance of 
   /// the class is created.
   /// 
   /// Also unlike CommandMethod commands, there is not a
   /// separate instance of this class created for every
   /// open document. This class is strictly a singleton.
   /// </summary>
   
   public class MyCircle : Command<MyCircle>
   {
      protected override void Execute(object parameter)
      {
         Editor.Command("._CIRCLE", "10,10", "2");
      }
   }

   /// <summary>
   /// Another minimal implementation that draws a line,
   /// and uses the [Command] attribute to explicitly-
   /// specify the name of the command ("MYLINE"):
   /// </summary>

   [Command("MYLINE")]
   public class CanBeAnyName : Command<CanBeAnyName>
   {
      protected override void Execute(object parameter)
      {
         // Note that because these commands execute
         // in the document context by default, the
         // Editor's Command() method can be used:

         Editor.Command("._LINE", "2,2", "8,4", "");
      }
   }

   /// In addition to being able to execute as registered
   /// commands, a Command<T> can also be used in MVVM 
   /// scenarios because a Command<T> is also an ICommand:
   /// 
   /// Important:
   /// 
   /// When executed from a UI framework as an ICommand, 
   /// the Execute() method runs in the document execution 
   /// context, provided that the instance does not have 
   /// the CommandFlags.Session flag (by default, it does
   /// not). 
   /// 
   /// The Flags property is virtual and can be overridden 
   /// to provide a different value. Because this property 
   /// is used from the constructor of the base type, it 
   /// cannot be assigned from a derived type's constructor
   /// (which runs after base type constructors).
   /// 
   /// The CommandFlags can also be assigned using the 
   /// CommandAttribute (see CommandAttribute.cs). If the 
   /// CommandFlags.Session flag is present in the Flags, 
   /// the Execute() method runs in the application context.
   
   /// Example usage in MVVM and UIElement scenarios:
   
   public class MyViewModel
   {
      public ICommand DrawCircleCommand => MyCircle.Instance;

      public ICommand DrawLineCommand => CanBeAnyName.Instance;
   }

   public class Button     // proxy for System.Windows.Controls.Button
   {
      public ICommand Command { get; set; }
   }

   public class ExampleButtons
   {
      public Button drawCircleButton = new Button();
      public Button drawLineButton = new Button();

      public ExampleButtons()
      {
         drawCircleButton.Command = MyCircle.Instance;
         drawLineButton.Command = CanBeAnyName.Instance;
      }
   }

}
