using System.Text;
using System.ComponentModel;

namespace Audyssey
{
    namespace MultEQApp
    {
        public class ChannelReport : INotifyPropertyChanged
        {
            private int? _enSpeakerConnect = null;
            private int? _customEnSpeakerConnect = null;
            private bool? _isReversePolarity = null;
            private decimal? _distance = null;

            public int? EnSpeakerConnect
            {
                get
                {
                    return _enSpeakerConnect;
                }
                set
                {
                    _enSpeakerConnect = value;
                    RaisePropertyChanged("EnSpeakerConnect");
                }
            }
            public int? CustomEnSpeakerConnect
            {
                get { return _customEnSpeakerConnect; }
                set
                {
                    _customEnSpeakerConnect = value;
                    RaisePropertyChanged("CustomEnSpeakerConnect");
                }
            }
            public bool? IsReversePolarity
            {
                get { return _isReversePolarity; }
                set
                {
                    _isReversePolarity = value;
                    RaisePropertyChanged("IsReversePolarity");
                }
            }
            public decimal? Distance
            {
                get { return _distance; }
                set
                {
                    _distance = value;
                    RaisePropertyChanged("Distance");
                }
            }

            public bool ShouldSerializeCustomEnSpeakerConnect()
            {
                if (CustomEnSpeakerConnect != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                foreach (var property in this.GetType().GetProperties())
                {
                    sb.Append(property + "=" + property.GetValue(this, null) + "\r\n");
                }

                return sb.ToString();
            }

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged;
            #endregion

            #region methods
            protected void RaisePropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }
    }
}