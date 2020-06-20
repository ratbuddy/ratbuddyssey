using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Audyssey
{
    namespace MultEQAvr
    {
        interface IDis
        {
            #region Properties
            string EqType { get; set; }
            string ChData { get; set; }
            #endregion
        }

        interface IFil
        {
            #region Properties
            ObservableCollection<sbyte> FilData { get; set; }
            ObservableCollection<sbyte> DispData { get; set; }
            #endregion
        }

        public class AvrDisFil : IDis, IFil, INotifyPropertyChanged
        {
            private string _EqType = null;
            private string _ChData = null;
            private ObservableCollection<sbyte> _FilData = new ObservableCollection<sbyte>();
            private ObservableCollection<sbyte> _DispData = new ObservableCollection<sbyte>();

            #region Properties
            public string EqType
            {
                get
                {
                    return _EqType;
                }
                set
                {
                    _EqType = value;
                    RaisePropertyChanged("EqType");
                }
            }
            public string ChData
            {
                get
                {
                    return _ChData;
                }
                set
                {
                    _ChData = value;
                    RaisePropertyChanged("ChData");
                }
            }
            public ObservableCollection<sbyte> FilData
            {
                get
                {
                    return _FilData;
                }
                set
                {
                    _FilData = value;
                    RaisePropertyChanged("FilData");
                }
            }
            public ObservableCollection<sbyte> DispData
            {
                get
                {
                    return _DispData;
                }
                set
                {
                    _DispData = value;
                    RaisePropertyChanged("DispData");
                }
            }
            #endregion

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            #endregion

            #region methods
            private void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }

        public partial class AudysseyMultEQAvr : INotifyPropertyChanged
        {
            private ObservableCollection<AvrDisFil> _AvrDisFil = new ObservableCollection<AvrDisFil>();
            private ObservableCollection<Int32[]> _AvrCoefData = new ObservableCollection<Int32[]>();

            #region Properties
            public ObservableCollection<AvrDisFil> DisFil
            {
                get
                {
                    return _AvrDisFil;
                }
                set
                {
                    _AvrDisFil = value;
                    RaisePropertyChanged("DisFil");
                }
            }
            [JsonIgnore]
            public AvrDisFil CurrentDisFil
            {
                get
                {
                    if (_SelectedChannel != null)
                    {
                        foreach (var avrDisFil in _AvrDisFil)
                        {
                            if ((avrDisFil.ChData.Equals(_SelectedChannel)) &&
                                (avrDisFil.EqType.Equals(_SeletedEqType)))
                            {
                                CurrentCoefData = CoefData[_AvrDisFil.IndexOf(avrDisFil)];
                                RaisePropertyChanged("CurrentCoefData");
                                return avrDisFil;
                            }
                        }
                    }
                    return null;
                }
                set
                {
                }
            }
            public ObservableCollection<Int32[]> CoefData
            {
                get
                {
                    return _AvrCoefData;
                }
                set
                {
                    _AvrCoefData = value;
                    RaisePropertyChanged("CoefData");
                }
            }
            [JsonIgnore]
            public Int32[] CurrentCoefData //TODO add to the GUI
            {
                get
                {
                    return _AvrCoefData.ElementAt(_SelectedChannelIndex);
                }
                set
                {
                }
            }
            #endregion
        }
    }
}