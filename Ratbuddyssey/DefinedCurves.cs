using Audyssey.MultEQApp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratbuddyssey
{
    public static class DefinedCurves
    {
        static string[] tooleBasseCurve = new[] { "{20.0,6.3}", "{22.4,6.3}", "{25.2,6.3}", "{28.3,6.3}",
            "{31.7,6.3}", "{35.6,6.2}", "{39.9,6.2}", "{44.8,6.1}",
            "{50.2,6.0}", "{56.4,5.8}", "{63.2,5.5}", "{71.0,5.1}",
            "{79.6,4.6}", "{89.3,4.0}", "{100.2,3.3}", "{112.5,2.6}",
            "{126.2,1.9}", "{141.6,1.4}", "{158.9,1.2}", "{178.3,1.1}",
            "{200.0,1.0}", "{224.4,0.9}", "{251.8,0.9}", "{282.5,0.7}",
            "{317.0,0.7}", "{355.7,0.6}", "{399.1,0.4}", "{447.7,0.4}",
            "{502.4,0.3}", "{563.7,0.2}", "{632.5,0.2}", "{709.6,0.1}",
            "{796.2,0.1}", "{893.4,0.0}", "{1002.4,0.0}", "{1124.7,-0.1}",
            "{1261.9,-0.2}", "{1415.9,-0.3}", "{1588.7,-0.4}", "{1782.5,-0.5}",
            "{2000.0,-0.6}", "{2244.0,-0.6}", "{2517.9,-0.6}", "{2825.1,-0.7}",
            "{3169.8,-0.7}", "{3556.6,-0.7}", "{3990.5,-0.7}", "{4477.4,-0.6}",
            "{5023.8,-0.5}", "{5636.8,-0.3}", "{6324.6,-0.2}", "{7096.3,0.1}",
            "{7962.1,0.2}", "{8933.7,0.2}", "{10023.7,0.3}", "{11246.8,0.2}",
            "{12619.1,0.2}", "{14158.9,0.5}", "{15886.6,0.7}", "{17825.0,1.1}",
            "{20000.0,1.9}" };
        public static ObservableCollection<MyKeyValuePair> TooleBassValues
        {
            get;
            private set;
        }

        static DefinedCurves()
        {
            //Initialize the toole+bass curve
            TooleBassValues = DetectedChannel.ConvertStringArrayToDictionary(tooleBasseCurve);
        }

    }
}
