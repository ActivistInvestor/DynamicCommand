using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;

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
   /// ************** Caveat Emptor ******************
   /// 
   /// This class relies on undocumented, unsupported,
   /// 'for internal use only' APIs. As such, all of
   /// the standard caveats apply. While I doubt that
   /// these APIs will suddenly vanish given that the
   /// people reponsible for the managed API are very
   /// aware of the fact that customers have come to
   /// depend on them, one never knows what the future
   /// holds.
   /// 
   /// ************************************************
   /// Required Prerequisites:
   /// 
   /// CommandContext.cs   
   /// CommandAttribute.cs 
   /// 
   /// </summary>
   /// <typeparam name="T">The concrete type that is
   /// derived from this type</typeparam>

   public abstract class Command<T> : IDisposable, ICommand
      where T : Command<T>
   {
      static T instance = null;
      string name;
      CommandFlags flags = CommandFlags.Modal;
      string group = "DynamicCommands";
      bool disposed;
      object defaultParameter = null;
      InvocationContext context = InvocationContext.None;
      protected static Collection commands = new Collection();

      // This will be implemented in a derived type
      // to support the pattern used by RelayCommand,
      // once the issue of Singularity is resolved.
      //
      // Action<ICommand, object> action = null;
      //
      // We have discovered a basic flaw in the design
      // of RelayCommand, which is that instances of the
      // command do not pass themselves as a parameter
      // to the delegate that handles the command. That
      // makes it difficult to use the same delegate with
      // multiple RelayCommands, since the delegate has
      // no way to distinguish which of them called it,
      // short of resorting to call stack trickery.

      protected Command()
      {
         name = typeof(T).Name;
         var attribute = this.GetType().GetCustomAttribute<CommandAttribute>(false);
         if(attribute != null)
         {
            if(!string.IsNullOrEmpty(attribute.GlobalName)) 
               this.name = attribute.GlobalName;
            this.flags = attribute.Flags;
            if(!string.IsNullOrEmpty(attribute.GroupName))
               this.group = attribute.GroupName;
         }
         ValidateCommand(name);
         ValidateInstance();
         Register(group, name, Flags, execute);
      }

      protected virtual void ValidateInstance()
      {
         if(instance != null)
            throw new InvalidOperationException(
               $"Singleton violation: {name}, Use the Instance property");
         instance = (T) this;
      }

      protected virtual void ValidateCommand(string name)
      {
         if(commands.Contains(name))
            throw new InvalidOperationException($"Duplicate command name: {name}");
         var typeflags = Utils.IsCommandNameInUse(name);
         if(typeflags != CommandTypeFlags.NoneCmd)
            throw new InvalidOperationException(
               $"A command with the name {name} is already defined.");
      }

      protected virtual void Register(string group, string name, CommandFlags flags, CommandCallback callback)
      {
         commands.Add(this);
         Utils.AddCommand(group, name, name, Flags, callback);
      }

      protected virtual void Revoke(string group, string name)
      {
         if(instance == this)
            instance = null;
         Utils.RemoveCommand(group, name);
         commands.Remove(this);
      }

      public string GlobalName => name;
      public string GroupName => group;

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

      public bool IsModal => !Flags.HasFlag(CommandFlags.Session);

      /// <summary>
      /// The parameter passed to Execute() when the command
      /// is invoked as a registered command. If this value is
      /// not set, the value of the Context property is passed.
      /// </summary>

      public object DefaultParameter 
      {
         get => defaultParameter ?? this.Context;
         set => defaultParameter = value;
      }

      /// <summary>
      /// Can be overridden in a derived type to provide
      /// a different set of CommandFlags. Use with care.
      /// </summary>
      public virtual CommandFlags Flags => flags;

      /// <summary>
      /// Creating multiple instances of derived types is
      /// not possible. If an instance has already been 
      /// created, the constructor throws an exception.
      /// </summary>
      
      public static T Instance 
      { 
         get 
         {
            if(instance == null)
               Activator.CreateInstance<T>();
            return instance;
         } 
      }

      /// <summary>
      /// Command callback, invoked by AutoCAD when
      /// the user issues the registered command.
      /// </summary>

      void execute()
      {
         this.Context = InvocationContext.Implicit
            | (!IsModal ? InvocationContext.Session : 0);
         try
         {
            Execute(DefaultParameter);
         }
         finally 
         {  
            this.Context = InvocationContext.None;
         }
      }

      protected virtual void Dispose(bool disposing)
      {
         if(disposing)
         {
            Revoke(group, name);
         }
      }

      /// <summary>
      /// Calling this unregisters the command which 
      /// will cause it to no longer be available.
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
      /// Convenience properties for use by Execute() overrides:
      /// </summary>

      /// The DocumentCollection
      protected static DocumentCollection Documents => 
         Application.DocumentManager;
      /// <summary>
      /// The active Document:
      /// </summary>
      protected static Document Document =>
         Documents.MdiActiveDocument ??
         throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.NoDocument);
      /// <summary>
      /// The active Editor:
      /// </summary>
      protected static Editor Editor => Document.Editor;

      /// <summary>
      /// ICommand implementation
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
      /// TODO (Merging with DocumentRelayCommand)
      /// </summary>
      /// <param name="parameter"></param>
      /// <returns></returns>

      public virtual bool CanExecute(object parameter)
      {
         return CommandContext.CanInvoke(QuiescentOnly, true)
            && Context == InvocationContext.None;
      }

      /// <summary>
      /// Invoked by the UI framework only.
      /// </summary>
      /// <param name="parameter"></param>

      async void ICommand.Execute(object parameter)
      {
         this.Context = InvocationContext.Explicit;
         parameter = parameter ?? DefaultParameter;
         try
         {
            if(IsModal && Documents.IsApplicationContext)
            {
               await CommandContext.InvokeAsync(() => this.Execute(parameter));
            }
            else
            {
               this.Execute(parameter);
            }
         }
         finally
         {
            this.Context = InvocationContext.None;
         }
      }

      /// <summary>
      /// Override this to implement unified command functionality.
      /// The code in the override of this method will execute in
      /// all execution contexts (e.g., as a registered command or
      /// as an ICommand).
      /// </summary>
      /// <param name="parameter">InvocationContext.Implicit 
      /// when the command was invoked by AutoCAD as a result 
      /// of the user issuing it. Otherwise this is whatever 
      /// value was passed to the ICommand.Execute() method.</param>

      protected abstract void Execute(object parameter);

#if(AUTOINIT) // Currently-disabled, untested

      /// <summary>
      /// Infrastructure for automated initialization,
      /// which is a work-in-progress and untested.
      /// 
      /// To trigger Automatic initialization of derived
      /// types in all currently and subsequently-loaded
      /// assemblies, call the Initalize() method, before
      /// referencing the Instance property of any derived
      /// type. 
      /// 
      /// After one or more instances have been created by
      /// referencing the Instance property or by calling
      /// a constructor, this method cannot be used.
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
         foreach(Type type in typeof(Command<T>).GetTypes(asm))
         {
            Activator.CreateInstance<T>();
         }
      }

#endif

      /// <summary>
      /// Commands should not be explicitly removed from this
      /// collection. Instead, call their Dispose() method, 
      /// which will remove the instance from the collection.
      /// </summary>

      protected class Collection : KeyedCollection<string, Command<T>>
      {
         HashSet<Command<T>> set = new HashSet<Command<T>>();

         public Collection() : base(StringComparer.OrdinalIgnoreCase) 
         {
         }

         protected override void InsertItem(int index, Command<T> item)
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

         protected override void SetItem(int index, Command<T> item)
         {
            var existg = base[index];
            if(existg != item)
            {
               set.Remove(existg);
               existg?.Dispose();
            }
            base.SetItem(index, item);
         }

         protected override string GetKeyForItem(Command<T> item)
         {
            return item.name;
         }
      }
   }

   [Flags]
   public enum InvocationContext
   {
      None = 0,
      Implicit = 1,        // Invoked by AutoCAD as a registered command
      Explicit = 2,        // Invoked via some other means (e.g., ICommand)
      Session = 4,         // Invoked in application execution context
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
      
      public static IEnumerable<Type> GetTypes(this Type baseType, Assembly asm = null)
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
