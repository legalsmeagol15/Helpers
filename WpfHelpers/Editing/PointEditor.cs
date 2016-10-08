using System;
using System.Collections.Generic;
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

namespace WpfHelpers.Editing
{
    
    public class PointEditor : AbstractPropertyEditor
    {
        static PointEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PointEditor), new FrameworkPropertyMetadata(typeof(PointEditor)));
        }

        private static PointConverter _PointConverter = new PointConverter();

        public override bool IsValid
        {
            get
            {
                object data = Input;
                if (data is Point) return true;
                PointConverter pc = new PointConverter();
                try
                {
                    Point pt = (Point)pc.ConvertFrom(data);
                    return true;
                }
                catch
                {
                    return false;
                }                
            }
        }

        public override object GetValue()
        {
            object data = Input;
            if (data is Point) return data;
            PointConverter pc = new PointConverter();
            try
            {
                Point pt = (Point)pc.ConvertFrom(data);
                return pt;
            }
            catch
            {
                return new Point(double.NaN, double.NaN);
            }
        }
    }
}
