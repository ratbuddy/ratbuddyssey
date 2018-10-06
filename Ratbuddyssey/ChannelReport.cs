using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratbuddyssey
{
    class ChannelReport : INotifyPropertyChanged
    {
        int _enSpeakerConnect = 0;
        int? _customEnSpeakerConnect = null;
        bool _isReversePolarity = false;
        decimal _distance = 0;

        public int EnSpeakerConnect
        {
            get { return _enSpeakerConnect; }
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
        public bool IsReversePolarity
        {
            get { return _isReversePolarity; }
            set
            {
                _isReversePolarity = value;
                RaisePropertyChanged("IsReversePolarity");
            }
        }
        public decimal Distance
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
        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
