using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HandyControl.Tools;

namespace HandyWinget.Control
{
    public class EnumToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Severity))
                throw new ArgumentException("value not of type Severity");
            Severity sv = (Severity) value;

            switch (sv)
            {
                case Severity.Warning:
                    return ResourceHelper.GetResource<Brushes>("WarningSeverity");
                case Severity.Information:
                    return ResourceHelper.GetResource<Brushes>("InformationSeverity");
                case Severity.Success:
                    return ResourceHelper.GetResource<Brushes>("SuccessSeverity");
                case Severity.Error:
                    return ResourceHelper.GetResource<Brushes>("ErrorSeverity");
                default:
                    return ResourceHelper.GetResource<Brushes>("WarningSeverity");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
