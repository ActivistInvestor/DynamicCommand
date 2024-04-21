/// CommandContext.cs  ActivistInvestor / Tony T.
///
/// AutoCAD .NET Utility classes for executing 
/// code in the document execution context.

using System;
using System.Threading.Tasks;
using AcRx = Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.ApplicationServices
{

   /// <summary>
   /// This class encapsulates and isolates all AutoCAD API-
   /// dependent functionality. Generally, types that use the
   /// methods of this class should not contain AutoCAD types 
   /// or method calls, espcially if they contain methods that
   /// can be jit'd at design-time.
   /// </summary>

   public static class CommandContext
   {
      static readonly DocumentCollection docs = Application.DocumentManager;

      /// <summary>
      /// Gets a value indicating if an operation can execute 
      /// based on two conditions:
      /// 
      ///   1. If there is an open document.
      ///   
      ///   2. If there is an open document 
      ///      that is in a quiescent state.
      ///      
      /// To find out if an operation can execute, requiring
      /// an open document, quiescent or not, the default
      /// arguments can be used:
      ///  
      ///    CommandContext.CanInvoke();
      ///      
      /// The arguments specify which of the conditions are
      /// applicable and tested.
      /// </summary>
      /// <param name="quiescentOnly">A value indicating if the
      /// operation cannot be performed if the document is not 
      /// quiescent</param>
      /// <param name="documentRequired">A value indicating if the
      /// operation cannot be performed if there is no active document</param>
      /// <returns>A value indicating if the operation can be performed</returns>

      public static bool CanInvoke(bool quiescentOnly = false, bool documentRequired = true)
      {
         Document doc = docs.MdiActiveDocument;
         return (!documentRequired || doc != null)
            && (!quiescentOnly || doc.Editor.IsQuiescent);
      }

      /// <summary>
      /// Returns a value indicating if the active 
      /// document is quiescent.
      /// Returns false if there is no document.
      /// </summary>

      public static bool IsQuiescent =>
         docs.MdiActiveDocument?.Editor.IsQuiescent == true;

      /// <summary>
      /// Gets a value indicating if the calling code is
      /// running in the application execution context.
      /// </summary>
      public static bool IsApplicationContext => docs.IsApplicationContext;

      /// <summary>
      /// Return a value indicating if there is an active document
      /// </summary>
      public static bool HasDocument => docs.MdiActiveDocument != null;

      /// <summary>
      /// Invokes the given action in the document execution context.
      /// If called from the document execution context, the action
      /// is invoked synchronously. Otherwise, the action is invoked
      /// asynchronously in the document execution context. 
      /// 
      /// Overloads of this method have yet to be documented. There
      /// are 3 overloads of both InvokeAsync() and Invoke(). 
      /// 
      /// The three overloads of Invoke() and InvokeAsync() vary only
      /// by the delegate type (Action, Action<T>, and Action<Document>).
      /// 
      /// InvokeAsync() overloads return a Task and can be awaited.
      /// 
      /// Invoke() overloads return void and cannot be awaited.
      /// 
      /// All overloads require an active document.
      /// 
      /// The overloads that take actions having a Document
      /// parameter receive the Document that is active at 
      /// the point when the delegate is invoked.
      /// </summary>
      /// <typeparam name="T">The type of the argument passed to the action</typeparam>
      /// <param name="action">The action to execute</param>
      /// <param name="parameter">A value passed as the parameter to the action</param>
      /// <returns>A Task representing the asynchronous operation</returns>
      /// <exception cref="Autodesk.AutoCAD.Runtime.Exception">There is no
      /// active document</exception>

      public static async Task InvokeAsync<T>(Action<T> action, T parameter)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.MdiActiveDocument == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(docs.IsApplicationContext)
         {
            await docs.ExecuteInCommandContextAsync((unused) =>
            {
               action(parameter);
               return Task.CompletedTask;
            }, null);
         }
         else
         {
            action(parameter);
         }
      }

      /// <summary>
      /// Invokes the given action in the document execution context
      /// </summary>
      /// <param name="action">The action to execute</param>
      /// <returns>A Task</returns>
      /// <exception cref="Autodesk.AutoCAD.Runtime.Exception"></exception>

      public static async Task InvokeAsync(Action action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.MdiActiveDocument == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(!docs.IsApplicationContext)
         {
            action();
         }
         else
         {
            await docs.ExecuteInCommandContextAsync((unused) =>
            {
               action();
               return Task.CompletedTask;
            }, null);
         }
      }

      public static void Invoke(Action action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.MdiActiveDocument == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(!docs.IsApplicationContext)
         {
            action();
         }
         else
         {
            var result = docs.ExecuteInCommandContextAsync((unused) =>
            {
               action();
               return Task.CompletedTask;
            }, null);
         }
      }

      public static void Invoke<T>(Action<T> action, T parameter)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.MdiActiveDocument == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(!docs.IsApplicationContext)
         {
            action(parameter);
         }
         else
         {
            var result = docs.ExecuteInCommandContextAsync((unused) =>
            {
               action(parameter);
               return Task.CompletedTask;
            }, null);
         }
      }

      public static void Invoke(Action<Document> action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.MdiActiveDocument == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(!docs.IsApplicationContext)
         {
            action(docs.MdiActiveDocument);
         }
         else
         {
            var result = docs.ExecuteInCommandContextAsync((unused) =>
            {
               action(docs.MdiActiveDocument);
               return Task.CompletedTask;
            }, null);
         }
      }




      /// <summary>
      /// Invokes the given action in the document execution context
      /// </summary>
      /// <param name="action">An Action<Document> to execute. The 
      /// action is passed the Document that is active at the point
      /// when it is executed.</param>
      /// <returns>A Task</returns>
      /// <exception cref="Autodesk.AutoCAD.Runtime.Exception"></exception>

      public static async Task InvokeAsync(Action<Document> action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(docs.MdiActiveDocument == null)
            throw new AcRx.Exception(AcRx.ErrorStatus.NoDocument);
         if(!docs.IsApplicationContext)
         {
            action(Application.DocumentManager.MdiActiveDocument);
         }
         else
         {
            await docs.ExecuteInCommandContextAsync((unused) =>
            {
               action(Application.DocumentManager.MdiActiveDocument);
               return Task.CompletedTask;
            }, null);
         }
      }

      /// <summary>
      /// Extension methods that target System.Action,
      /// System.Action<T>, and System.Action<Document>.
      /// 
      /// Returns an Action that when invoked, will 
      /// execute the argument action in the document
      /// execution context. The returned Action can
      /// replace the argument.
      /// 
      /// It is recommended that the delegate not capture
      /// the active document, or anything dependent on the
      /// active document. Use the overload that takes an
      /// Action<Document> and use the Document parameter,
      /// as that is guarenteed to be the active document 
      /// at the point when the action executes.
      /// </summary>
      /// <param name="action">The action that is to
      /// execute in the document execution context</param>
      /// <returns>An action that executes the given <paramref name="action"/>
      /// in the document execution context</returns>

      public static Action DocInvoke(this Action action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         return () => { var unused = InvokeAsync(action); };
      }

      /// <summary>
      /// Can be invoked on an EventHandler to cause it to
      /// execute in the document context:

      public static EventHandler DocInvoke(this EventHandler handler)
      {
         return delegate(object sender, EventArgs e)
         {
            var unused = InvokeAsync(() => handler(sender, e));
         };
      }

      /// <summary>
      /// Can be invoked on an Event Handler that accepts
      /// an event argument of type T:
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="action"></param>
      /// <returns></returns>

      public static Delegate DocInvoke(this Delegate action) // where T: Delegate
      {
         return delegate(object sender, EventArgs e)
         {
            var unused = InvokeAsync(() => action.DynamicInvoke(sender, e));
         };
      }

      public static Action DocInvoke<T>(this Action<T> action, T arg)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         return async () => await InvokeAsync(action, arg);
      }

      public static Action DocInvoke(this Action<Document> action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         return () => { var unused = InvokeAsync(action); };
      }

      public static Action DocInvokeAsync(this Action action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         return async () => await InvokeAsync(action);
      }

      public static Action DocInvokeAsync<T>(this Action<T> action, T arg)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         return async () => await InvokeAsync(action, arg);
      }

      public static Action DocInvokeAsync(this Action<Document> action)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         return async () => await InvokeAsync(action);
      }

   }

}