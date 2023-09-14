using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using Expression = EnvDTE.Expression;
using DTE = EnvDTE.DTE;
using Document = EnvDTE.Document;
using TextDocument = EnvDTE.TextDocument;

namespace LocalsJsonDumper
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DumpObjectCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;
        public const int ContextMenuCommandId = 0x0101;
        public const int LocalsContextMenuCommandId = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e352a0e9-68b9-427d-87f5-7db47da08b18");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="DumpObjectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DumpObjectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            var menuCommandIDc = new CommandID(CommandSet, ContextMenuCommandId);
            var menuItemc = new MenuCommand(ContextMenuExecute, menuCommandIDc);

            commandService.AddCommand(menuItemc);

            dte = GetDTE();
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DumpObjectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in DumpObjectCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new DumpObjectCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>      
        private void Execute(object sender, EventArgs e)
        {
            if (!CorrectStateToExecute())
            {
                return;
            }

            LaunchDialog();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Done in CorrectStateToExecute")]
        private void ContextMenuExecute(object sender, EventArgs e)
        {
            if (!CorrectStateToExecute())
            {
                return;
            }
            Document doc = dte.ActiveDocument;

            TextDocument txt = doc.Object() as TextDocument;

            var selection = txt.Selection;
            var possibleLocalName = selection.Text;
            if (selection.IsEmpty)
            {
                var leftPoint = selection.AnchorPoint.CreateEditPoint();
                leftPoint.WordLeft(1);
                var rightPoint = selection.ActivePoint.CreateEditPoint();
                rightPoint.WordRight(1);
                var madeSelectionText = leftPoint.GetText(rightPoint);
                possibleLocalName = madeSelectionText.Trim();
            }

            LaunchDialog(possibleLocalName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Done in CorrectStateToExecute")]
        private void LaunchDialog(string selectedLocal = "")
        {
            var debugger = dte.Debugger;
            if (debugger.CurrentStackFrame is null)
            {
                System.Windows.Forms.MessageBox.Show($"CurrentStackFrame is not available.");
                return;
            }
            var locals = debugger.CurrentStackFrame.Locals;

            var localList = new List<Expression>();
            foreach (Expression item in locals)
            {
                localList.Add(item);
            }

            new ExportDialog(localList, selectedLocal).ShowDialog();
        }

        private bool CorrectStateToExecute()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var debugger = dte.Debugger;
            if (debugger.CurrentStackFrame is null)
            {
                System.Windows.Forms.MessageBox.Show($"CurrentStackFrame is not available.");
                return false;
            }

            return true;
        }

        private DTE GetDTE()
        {
            DTE dte = ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE;

            });
            return dte;
        }
    }
}
