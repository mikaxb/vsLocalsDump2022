using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using EnvDTE;
using Expression = EnvDTE.Expression;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LocalsJsonDumper
{
    /// <summary>
    /// Interaction logic for ExportToolWindowControl.
    /// </summary>
    public partial class ExportToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportToolWindowControl"/> class.
        /// </summary>
        public ExportToolWindowControl()
        {
            InitializeComponent();
            LocalDropDown.Items.Clear();
            OutPut.Text = string.Empty;
            EngineChoiceBox.ItemsSource = Engines;
            EngineChoiceBox.SelectionChanged += EngineChanged;
            EngineChoiceBox.SelectedItem = Engines.First(e => e.Generator == EngineGenerator.SystemTextJson);
        }

        private DTE Dte { get; set; }
        private List<Expression> Locals { get; set; }
        private string SelectedLocal { get; set; }

        private delegate Task GeneratorDoneCallBack(string generatorResult);

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private CancellationToken GenerationCancellationToken { get; set; }

        private bool UseSystemTextJson => (EngineChoiceBox.SelectedItem as EngineListItem)?.Generator == EngineGenerator.SystemTextJson;

        private List<EngineListItem> Engines { get; } = new List<EngineListItem>() {
            new EngineListItem() { Generator = EngineGenerator.SystemTextJson, Text = "GetExpression with C# System.Text.Json" },
            new EngineListItem() { Generator = EngineGenerator.TreeClimber, Text = "Traverse Expression tree" }
        };

        public void SetDTE(DTE dte)
        {
            Dte = dte;
        }

        private Expression GetExpressionFromLocals(string localName)
        {
            return Locals.FirstOrDefault(e =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return e.Name == localName;
            });
        }

        private void RenewLocalsFromDebugger()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
            }
            catch
            {
                OutPut.Text = $"Not in correct state to execute. Not running on UI thread.";
                return;
            }

            var debugger = Dte?.Debugger;
            if (debugger?.CurrentStackFrame is null)
            {
                TypeInfo.Text = $"CurrentStackFrame is not available. Is the debugger running?";
                return;
            }
            var locals = debugger.CurrentStackFrame.Locals;

            var localList = new List<Expression>();
            foreach (Expression item in locals)
            {
                localList.Add(item);
            }
            Locals = localList;
        }

        private void LocalDropDown_OnClose(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
            }
            catch
            {
                OutPut.Text = $"Not in correct state to execute. Not running on UI thread.";
                return;
            }
            try
            {
                var dropDown = sender as ComboBox;
                if (dropDown?.SelectedValue != null)
                {
                    SelectedLocal = dropDown.SelectedValue.ToString();
                    TypeInfo.Text = GetExpressionFromLocals(SelectedLocal)?.Type;
                }
            }
            catch (Exception ex)
            {
                OutPut.Text = $"Exception of type {ex.GetType()} occured{Environment.NewLine}{ex.Message}";
            }
        }

        private void LocalDropDown_OnOpen(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
            }
            catch
            {
                OutPut.Text = $"Not in correct state to execute. Not running on UI thread.";
                return;
            }
            RenewLocalsFromDebugger();
            PopulateDropDown();
        }

        private void Generate(string localName, TimeSpan timeout, uint maxDepth, Regex nameIgnoreRegex, Regex typeIgnoreRegex, bool includeFields)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
            }
            catch
            {
                OutPut.Text = $"Not in correct state to execute. Not running on UI thread.";
                return;
            }
            RenewLocalsFromDebugger();
            var expression = GetExpressionFromLocals(localName);
            if (expression is null)
            {
                OutPut.Text = $"Could not find local with name: {localName}";
                return;
            }

            if (UseSystemTextJson)
            {
                GenerateUsingSystemTextJson(localName, includeFields, maxDepth, HandleGeneratorResultAsync);
            }
            else
            {
                CancellationTokenSource = new CancellationTokenSource();
                CancellationTokenSource.CancelAfter(timeout);
                GenerationCancellationToken = CancellationTokenSource.Token;
                GenerationCancellationToken.Register(() => GenerationCancelled());
                GenerateUsingExpressionTree(expression, GenerationCancellationToken, maxDepth, nameIgnoreRegex, typeIgnoreRegex, HandleGeneratorResultAsync);
            }
        }

        private void GenerateUsingSystemTextJson(string localName, bool includeFields, uint maxDepth, GeneratorDoneCallBack callback)
        {
            _ = Task.Run(async () =>
            {
                var result = default(string);
                try
                {
                    Debug.WriteLine("Generation starting");
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var jsonExpression = Dte?.Debugger.GetExpression($"System.Text.Json.JsonSerializer.Serialize({localName},new System.Text.Json.JsonSerializerOptions(){{IncludeFields = {includeFields.ToString().ToLowerInvariant()}}})");
                    if (jsonExpression.IsValidValue)
                    {
                        var unescapedAndTrimmedValue = Regex.Unescape(jsonExpression.Value).Trim('"');
                        var reDeserializedObject = System.Text.Json.JsonSerializer.Deserialize<object>(unescapedAndTrimmedValue);
                        result = System.Text.Json.JsonSerializer.Serialize(reDeserializedObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                    else
                    {
                        result = jsonExpression.Value;
                    }
                }
                catch (Exception ex)
                {
                    result = $"Exception of type {ex.GetType()} occured{Environment.NewLine}{ex.Message}";
                }
                finally
                {
                    Debug.WriteLine("Generation done");
                    await callback(result);
                }
            });
        }

        private void GenerateUsingExpressionTree(Expression expression, CancellationToken cancellationToken, uint maxDepth, Regex nameIgnoreRegex, Regex typeIgnoreRegex, GeneratorDoneCallBack callback)
        {
            _ = Task.Run(async () =>
            {
                var result = default(string);
                try
                {
                    Debug.WriteLine("Generation starting");
                    var generator = new JsonGenerator();
                    var json = generator.GenerateJson(expression, cancellationToken, maxDepth, nameIgnoreRegex, typeIgnoreRegex);
                    result = json;
                }
                catch (Exception ex)
                {
                    result = $"Exception of type {ex.GetType()} occured{Environment.NewLine}{ex.Message}";
                }
                finally
                {
                    Debug.WriteLine("Generation done");
                    await callback(result);
                }
            });
        }

        private async Task HandleGeneratorResultAsync(string result)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            OutPut.Text = result;
            OutPut.TextAlignment = TextAlignment.Left;
            GenerateButton.IsEnabled = true;
            CopyButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }

        private void GenerationCancelled()
        {
            Debug.WriteLine("Generation cancelled");
            _ = Task.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                GenerateButton.IsEnabled = true;
                CopyButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            });
        }

        private void PopulateDropDown()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                LocalDropDown.Items.Clear();
                Locals.OrderBy(e => e.Name).ToList().ForEach(i =>
                {
                    LocalDropDown.Items.Add(i.Name);
                });
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = $"Exception of type {ex.GetType()} occured{Environment.NewLine}{ex.Message}";
            }
        }

        private void GenerateButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedLocal))
            {
                OutPut.Text = "< Could not evaluate Expression. Check that a known type is selected. >";
                OutPut.TextAlignment = TextAlignment.Center;
                return;
            }

            OutPut.Text = "<< GENERATING >>";
            OutPut.TextAlignment = TextAlignment.Center;
            CopyButton.IsEnabled = false;
            GenerateButton.IsEnabled = false;
            CancelButton.IsEnabled = !UseSystemTextJson;

            if (ValidateAndParseInput(MaxDepthInput.Text, out var maxDepth) && ValidateAndParseInput(TimeoutInput.Text, out var timeout))
            {
                Generate(SelectedLocal, TimeSpan.FromSeconds(timeout), maxDepth, new Regex(NameIgnoreRegexInput.Text), new Regex(TypeIgnoreRegexInput.Text), IncludeFields.IsChecked ?? false);
            }
            else
            {
                CopyButton.IsEnabled = true;
                GenerateButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                OutPut.Text = "< Invalid input. Use unsigned integers. >";
                OutPut.TextAlignment = TextAlignment.Center;
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            CancellationTokenSource?.Cancel();
        }

        private bool ValidateAndParseInput(string input, out uint result)
        {
            if (uint.TryParse(input, out uint parsed))
            {
                result = parsed;
                return true;
            }
            result = default;
            return false;
        }

        private void CopyToClipBoardButtonClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutPut.Text);
        }

        private void EngineChanged(object sender, RoutedEventArgs e)
        {
            if (UseSystemTextJson)
            {
                SystemTextControls.Visibility = Visibility.Visible;
                TreeClimberControls.Visibility = Visibility.Collapsed;
                RegexControls.Visibility = Visibility.Collapsed;       
            }
            else
            {
                SystemTextControls.Visibility = Visibility.Collapsed;
                TreeClimberControls.Visibility = Visibility.Visible;               
                RegexControls.Visibility = Visibility.Visible;
            }
        }

        private enum EngineGenerator
        {
            SystemTextJson,
            TreeClimber
        }

        private class EngineListItem
        {
            public EngineGenerator Generator { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}