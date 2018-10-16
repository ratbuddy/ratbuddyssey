using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Ratbuddyssey
{
    /// <summary>
    /// Interaction logic for RatbuddysseyHome.xaml
    /// </summary>
    public partial class RatbuddysseyHome : Page
    {
        Audyssey parsedAudyssey = null;
        private string filename;
        private string trimmedFilename;

        public RatbuddysseyHome()
        {
            InitializeComponent();
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey files (*.ady)|*.ady";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                currentFile.Content = filename;
                String audysseyFile = File.ReadAllText(filename);
                parsedAudyssey = JsonConvert.DeserializeObject<Audyssey>(audysseyFile, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
                if(parsedAudyssey!=null)
                {
                    this.DataContext = parsedAudyssey;
                }

            }
        }

        private void ReloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will reload the .ady file and discard all changes since last save", "Are you sure?", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                String audysseyFile = File.ReadAllText(filename);
                parsedAudyssey = JsonConvert.DeserializeObject<Audyssey>(audysseyFile, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
                if (parsedAudyssey != null)
                {
                    this.DataContext = parsedAudyssey;
                }
            }
        }

        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
            string reSerialized = JsonConvert.SerializeObject(parsedAudyssey, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
//            trimmedFilename = (filename.Substring(0, filename.Length - 3));
//            File.WriteAllText(trimmedFilename + "modified_by_ratbuddyssey.ady", reSerialized);

            File.WriteAllText(filename, reSerialized);
        }

        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            trimmedFilename = (filename.Substring(0, filename.Length - 3));
            dlg.FileName = filename;
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey calibration (.ady)|*.ady";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filename = dlg.FileName;
                string reSerialized = JsonConvert.SerializeObject(parsedAudyssey, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

                File.WriteAllText(filename, reSerialized);

            }
        }

        private void ExitProgram_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Close();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Shout out to AVS Forum, use at your own risk!");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(keyTbx.Text) && !string.IsNullOrEmpty(valueTbx.Text))
            {
                ((DetectedChannel)channelsView.SelectedValue).CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair(keyTbx.Text, valueTbx.Text));
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            MyKeyValuePair pair = b.DataContext as MyKeyValuePair;
            ((DetectedChannel)channelsView.SelectedValue).CustomTargetCurvePointsDictionary.Remove(pair);            
        }
    }
}
