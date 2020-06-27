using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace Audyssey
{
    namespace MultEQAvr
    {
        public class UniqueObservableCollection<T> : ObservableCollection<T>
        {
            #region Protected Methods
            protected override void InsertItem(int index, T item)
            {
                if (item.GetType() == typeof(Dictionary<string, string>))
                {
                    foreach (var Item in Items)
                    {
                        if ((Item as Dictionary<string, string>).ContainsKey((item as Dictionary<string, string>).ElementAt(0).Key))
                        {
                            index = Items.IndexOf(Item);
                            Items.Remove(Item);
                            break;
                        }
                    }
                }
                else if (item.GetType() == typeof(Dictionary<string, int>))
                {
                    foreach (var Item in Items)
                    {
                        if ((Item as Dictionary<string, int>).ContainsKey((item as Dictionary<string, int>).ElementAt(0).Key))
                        {
                            index = Items.IndexOf(Item);
                            Items.Remove(Item);
                            break;
                        }
                    }
                }
                else if (item.GetType() == typeof(Dictionary<string, object>))
                {
                    foreach (var Item in Items)
                    {
                        if ((Item as Dictionary<string, object>).ContainsKey((item as Dictionary<string, object>).ElementAt(0).Key))
                        {
                            index = Items.IndexOf(Item);
                            Items.Remove(Item);
                            break;
                        }
                    }
                }
                base.InsertItem(index, item);
            }
            #endregion
        }

        public partial class AudysseyMultEQAvr : INotifyPropertyChanged
        {
            private string _Serialized;

            public string Serialized
            {
                get
                {
                    return _Serialized;
                }
                set
                {
                    _Serialized = value;
                    RaisePropertyChanged("Serialized");
                }
            }

            /*local reference for selected channel from GUI*/
            private Dictionary<string, string> _SelectedItem;
            private string _SelectedChannel = null;

            #region Properties
            public Dictionary<string,string> SelectedItem
            {
                set
                {
                    _SelectedItem = value;
                    SelectedChannel = _SelectedItem.Keys.ElementAt(0);
                    RaisePropertyChanged("SelectedChSetup");
                }
            }
            public string SelectedChannel
            {
                set
                {
                    _SelectedChannel = value.Replace("MIX", "");
                    RaisePropertyChanged("SelectedDisFil");
                    RaisePropertyChanged("SelectedCoefData");
                    RaisePropertyChanged("SelectedSpConfig");
                    RaisePropertyChanged("SelectedDistance");
                    RaisePropertyChanged("SelectedChLevel");
                    RaisePropertyChanged("SelectedCrossover");
                }
            }
            #endregion

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            private void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }       
    }
}
