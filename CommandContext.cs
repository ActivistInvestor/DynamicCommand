/// CommandContext.cs  ActivistInvestor / Tony T.
///
/// AutoCAD .NET Utility classes for executing 
/// code in the document execution context.

using System;
using System.Threading.Tasks;
using AcRx = Autodesk.AutoCAD.Runtime;

namespace Autodesk.AutoCAD.ApplicationServices
{

   public static class CommandContext
   {
      static readonly DocumentCollection docs = Application.DocumentManager;

      /// <summary>
      /// Gets a value indicating if an operation can
      /// execute, based on two conditions:
      /// 
      ///   1. If there is an active document.
      ///   
      ///   2. If there is an active document 
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
      /// 
      /// Note that this API interprets an active 'non-document'
      /// tab (such as the New Document tab) the same as no open
      /// documents, where the intent is to determine if a command 
      /// or operation that depends on an active document can run.
      /// 
      /// Both the Invoke() and InvokeAsync() overloads of this 
      /// class throw an eNoDocument exception when called while
      /// there is no active document, so this API can be used to 
      /// check that before calling them.
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
         return doc == null ? !documentRequired 
            : !quiescentOnly || doc.Editor.IsQuiescent;
      }

      /// <summary>
      /// Returns a value indicating if the active 
      /// document is quiescent.
      /// 
      /// >>> Returns false if there is no active document <<<
      /// 
      /// </summary>

      public static bool IsQuiescent =>
         docs.MdiActiveDocument?.Editor.IsQuiescent == true;

      /// <summary>
      /// Gets a value indicating if the calling code is
      /// running in the application execution context.
      /// </summary>
      public static bool IsApplicationContext => docs.IsApplicationContext;

      /// <summary>
      /// Returns a value indicating if there is an active document
      /// 
      /// This value can be false even when there are one or more
      /// open documents, in which case there is a 'non-document'
      /// tab active, such as the New Document tab.
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
      /// by delegate type (Action, Action<T>, and Action<Document>).
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
      /// impersonate and replace the argument.
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
      /// <paramref name="async"/>A value indicating if the
      /// call to the <paramref name="action"/> should be 
      /// aynchronously waited.</param>
      /// <returns>An action that executes the given <paramref name="action"/>
      /// in the document execution context</returns>

      /// TODO:
      /// Prototype for revisions to eliminate DocInvokeAsync()
      /// overloads, in favor of an optional async argument to
      /// overloads of this method.

      public static Action DocInvoke(this Action action, bool async = false)
      {
         if(action == null)
            throw new ArgumentNullException(nameof(action));
         if(!async)
            return () => Invoke(action);
         else
            return async () => await InvokeAsync(action);
      }

      /// <summary>
      /// Can be invoked on an EventHandler to cause it to
      /// execute in the document context. The result of
      /// this method wraps the event handler, and can be
      /// added to the event. See the example below.

      /// TODO:
      /// Prototype for revisions to eliminate DocInvokeAsync()
      /// overloads, in favor of an optional async argument to
      /// overloads of DocInvoke()

      public static EventHandler DocInvoke(this EventHandler handler, bool async = false)
      {
         if(!async)
            return (s, e) => Invoke(() => handler(s, e));
         else
            return async (s, e) => await InvokeAsync(() => handler(s, e));
      }

      /// <summary>
      /// This example shows how to use DocInvoke() to cause a
      /// handler for the Application.Idle event to run in the
      /// Document execution context:
      /// </summary>

      public static class Example
      {
         public static void Run()
         {
            /// Add the event:
            Application.Idle += docIdle;

            /// Remove the event:
            Application.Idle -= docIdle;
         }

         /// <summary>
         /// The wrapped handler for the idle event:
         /// </summary>

         static EventHandler docIdle = ((EventHandler)idle).DocInvoke();

         /// <summary>
         /// The handler for the idle event:
         /// </summary>

         private static void idle(object sender, EventArgs e)
         {
            Application.DocumentManager.MdiActiveDocument.Editor
               .WriteMessage("\nI'm running in the document context");
            
            /// Remove the event (note that the 
            /// wrapped event handler is passed):

            Application.Idle -= docIdle;
         }
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
         return () => Invoke(action); 
      }

      /// <summary>
      /// TODO: Will replace these with optional async arguments to the above
      /// </summary>

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