using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Tools;

namespace HandyWinget.Control
{
    public partial class InfoBar
    {
        #region Dependency Property
        public static readonly DependencyProperty SeverityProperty =
           DependencyProperty.Register("Severity", typeof(Severity), typeof(InfoBar), new PropertyMetadata(Severity.Information, OnSeverityChanged));

        public Severity Severity
        {
            get { return (Severity) GetValue(SeverityProperty); }
            set { SetValue(SeverityProperty, value); }
        }

        private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (InfoBar) d;
            ctl.ChangeBackground((Severity) e.NewValue);
        }


        public static readonly DependencyProperty IsOpenProperty =
           DependencyProperty.Register("IsOpen", typeof(bool), typeof(InfoBar), new PropertyMetadata(true, OnVisibilityChanged));

        public bool IsOpen
        {
            get { return (bool) GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (InfoBar) d;
            ctl.ChangeVisibility((bool) e.NewValue);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(InfoBar));

        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(InfoBar));

        public string Message
        {
            get { return (string) GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty ActionButtonProperty =
            DependencyProperty.Register("ActionButton", typeof(ContentControl), typeof(InfoBar));

        public ContentControl ActionButton
        {
            get { return (ContentControl) GetValue(ActionButtonProperty); }
            set { SetValue(ActionButtonProperty, value); }
        }

        public static readonly DependencyProperty ContentPropertyProperty =
            DependencyProperty.Register("Content", typeof(ContentControl), typeof(InfoBar));

        public ContentControl Content
        {
            get { return (ContentControl) GetValue(ContentPropertyProperty); }
            set { SetValue(ContentPropertyProperty, value); }
        }

        #endregion

        public InfoBar()
        {
            InitializeComponent();
            ChangeBackground(Severity);
        }

        private void ChangeBackground(Severity severity)
        {
            switch (severity)
            {
                case Severity.Warning:
                    Background = ResourceHelper.GetResource<Brush>("WarningSeverity");
                    iconBox.UriSource = new Uri(@"/Resources/Warning.png", UriKind.RelativeOrAbsolute);
                    break;
                case Severity.Information:
                    Background = ResourceHelper.GetResource<Brush>("InformationSeverity");
                    iconBox.UriSource = new Uri(@"/Resources/Info.png", UriKind.RelativeOrAbsolute);
                    break;
                case Severity.Success:
                    Background = ResourceHelper.GetResource<Brush>("SuccessSeverity");
                    iconBox.UriSource = new Uri(@"/Resources/Success.png", UriKind.RelativeOrAbsolute);
                    break;
                case Severity.Error:
                    iconBox.UriSource = new Uri(@"/Resources/Error.png", UriKind.RelativeOrAbsolute);
                    Background = ResourceHelper.GetResource<Brush>("ErrorSeverity");
                    break;
            }
        }

        private void ChangeVisibility(bool value)
        {
            if (value)
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ((Panel) this.Parent).Children.Remove(this);
        }
    }
}
