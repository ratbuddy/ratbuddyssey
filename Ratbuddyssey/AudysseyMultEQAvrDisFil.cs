using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public class AvrFil : IFil, INotifyPropertyChanged
        {
            private ObservableCollection<sbyte> _FilData = new ObservableCollection<sbyte>();
            private ObservableCollection<sbyte> _DispData = new ObservableCollection<sbyte>();

            #region Properties
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
    }
}