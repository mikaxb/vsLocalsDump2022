using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LocalsJsonDumper
{
    /// <summary>
    /// Interaction logic for WpfDialog.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Done in constructor.")]
    public partial class ExportDialog : Window
    {
        public ExportDialog(List<EnvDTE.Expression> locals, string selectedLocal = "")
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            InitializeComponent();
            Locals = locals;
            PopulateDropDown(selectedLocal);
        }

        private List<EnvDTE.Expression> Locals { get; set; }

        private EnvDTE.Expression SelectedLocal { get; set; }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LocalDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var dropDown = sender as ComboBox;
                SelectedLocal = Locals.FirstOrDefault(i => i.Name == dropDown.SelectedValue.ToString());
                dropDown.IsDropDownOpen = false;
                TypeInfo.Text = SelectedLocal.Type.ToString();
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.Message;
            }
        }

        private void GenerateInTask(EnvDTE.Expression expression, TimeSpan timeout, uint maxDepth)
        {
            _ = Task.Run(async () =>
              {
                  try
                  {
                      await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                      var generator = new JsonGenerator();
                      var json = generator.GenerateJson(expression, timeout, maxDepth);
                      OutPut.Text = json;
                      OutPut.TextAlignment = TextAlignment.Left;
                      CopyButton.IsEnabled = true;
                      GenerateButton.IsEnabled = true;
                  }
                  catch (Exception ex)
                  {
                      TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                      OutPut.Text = ex.Message;
                  }
              });
        }


        private void PopulateDropDown(string preSelectedValue)
        {
            try
            {
                Dispatcher.VerifyAccess();
                Locals.ForEach(i => LocalDropDown.Items.Add(i.Name));
                LocalDropDown.SelectedValue = preSelectedValue;
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.Message;
            }
        }

        private void GenerateButtonClick(object sender, RoutedEventArgs e)
        {
            OutPut.Text = "<< GENERATING >>";
            OutPut.TextAlignment = TextAlignment.Center;
            CopyButton.IsEnabled = false;
            GenerateButton.IsEnabled = false;

            if(ValidateAndParseInput(MaxDepthInput.Text, out var maxDepth) && ValidateAndParseInput(TimeoutInput.Text, out var timeout))
            {
                GenerateInTask(SelectedLocal, TimeSpan.FromSeconds(timeout), maxDepth);
            }
            else
            {
                CopyButton.IsEnabled = true;
                GenerateButton.IsEnabled = true;
                OutPut.Text = "< Invalid input. Use unsigned integers. >";
                OutPut.TextAlignment = TextAlignment.Center;
            }          
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
    }
}