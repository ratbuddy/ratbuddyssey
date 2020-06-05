using System.ComponentModel;

namespace Audyssey
{
    namespace MultEQAvr
    {
        public class AvrDisFil : INotifyPropertyChanged
        {
            private string _EqType = null;
            private string _ChData = null;
            private sbyte[] _FilData = null;
            private sbyte[] _DispData = null;

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
            public sbyte[] FilData
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
            public sbyte[] DispData
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