using System;
using System.Collections.Generic;
using System.Linq;
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LocalDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                OutPut.Text = "...GENERATING...";
                OutPut.TextAlignment = TextAlignment.Center;
                var dropDown = sender as ComboBox;
                var selectedLocal = Locals.FirstOrDefault(i => i.Name == dropDown.SelectedValue.ToString());
                dropDown.IsDropDownOpen = false;
                TypeInfo.Text = selectedLocal.Type.ToString();
                CopyButton.IsEnabled = false;
                GenerateInTask(selectedLocal);
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.Message;
            }
        }

        private void GenerateInTask(EnvDTE.Expression experssion)
        {
            _ = Task.Run(async () =>
              {
                  try
                  {
                      await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                      var generator = new JsonGenerator();
                      var json = generator.GenerateJson(experssion);
                      OutPut.Text = json;
                      OutPut.TextAlignment = TextAlignment.Left;
                      CopyButton.IsEnabled = true;
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

        private void CopyToClipBoardButtonClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutPut.Text);
        }
    }
}