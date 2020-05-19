using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Ink;

using Wacom.Devices;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace WillDevicesSampleApp
{
    public sealed partial class RealTimeInkPage : Page, INotifyPropertyChanged
    {
        private CancellationTokenSource m_cts = new CancellationTokenSource();

        //Stroke collection used for real-time rendering 
        private StrokeCollection _strokes = new StrokeCollection();
        private double m_scale = 1.0;
        private Size m_deviceSize;
        private bool m_addNewStrokeToModel = true;
        private static float maxP = 1.402218f;
        private static float pFactor = 1.0f / (maxP - 1.0f);

        public event PropertyChangedEventHandler PropertyChanged;

        public StrokeCollection Strokes //Used as databinding into XAML
        {
            get
            {
                return _strokes;
            }
        }

        public RealTimeInkPage()
        {
            this.InitializeComponent();

            Loaded += RealTimeInkPage_Loaded;
        }


        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            m_cts.Cancel();
        }

        private async void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                IRealTimeInkService service = AppObjects.Instance.Device.GetService(InkDeviceService.RealTimeInk) as IRealTimeInkService;

                if ((service != null) && service.IsStarted)
                {
                    await service.StopAsync(m_cts.Token);
                }

            }

        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        ~RealTimeInkPage()
        {
            System.Diagnostics.Debug.WriteLine("Finalizer: RealTimeInkPage");
        }

        private async void RealTimeInkPage_Loaded(object sender, RoutedEventArgs e)
        {
            IDigitalInkDevice device = AppObjects.Instance.Device;

            device.Disconnected += OnDeviceDisconnected;

            NavigationService.Navigating += NavigationService_Navigating;
            NavigationService.Navigated += NavigationService_Navigated;

            IRealTimeInkService service = device.GetService(InkDeviceService.RealTimeInk) as IRealTimeInkService;
            service.NewPage += OnNewPage; //Clear page on new page or layer
            service.NewLayer += OnNewPage;
            m_Canvas.DataContext = this;
            try
            {
                uint width = (uint)await device.GetPropertyAsync("Width", m_cts.Token);
                uint height = (uint)await device.GetPropertyAsync("Height", m_cts.Token);
                uint ptSize = (uint)await device.GetPropertyAsync("PointSize", m_cts.Token);


                m_deviceSize.Width = width;
                m_deviceSize.Height = height;

                SetCanvasScaling();

                service.StrokeStarted += Service_StrokeStarted;
                service.StrokeUpdated += Service_StrokeUpdated;
                service.StrokeEnded += Service_StrokeEnded;
                service.HoverPointReceived += Service_HoverPointReceived; ;

                if (!service.IsStarted)
                {
                    await service.StartAsync(true, m_cts.Token);
                }

            }
            catch (Exception)
            {
            }


        }

        private void Service_HoverPointReceived(object sender, HoverPointReceivedEventArgs e)
        {
            string hoverPointCoords = string.Empty;
            switch (e.Phase)
            {
                case Wacom.Ink.InputPhase.Begin:
                case Wacom.Ink.InputPhase.Move:
                    hoverPointCoords = string.Format("X:{0:0.0}, Y:{1:0.0}", e.X, e.Y);
                    break;
            }
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                textBlockHoverCoordinates.Text = hoverPointCoords;
            }));
        }

        private void Service_StrokeEnded(object sender, StrokeEndedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                var pathPart = e.PathPart;
                var data = pathPart.Data.GetEnumerator();


                //Data is stored XYW
                float x = -1;
                float y = -1;
                float w = -1;

                if (data.MoveNext())
                {
                    x = data.Current;
                }

                if (data.MoveNext())
                {
                    y = data.Current;
                }

                if (data.MoveNext())
                {
                    //Clamp to 0.0 -> 1.0
                    w = Math.Max(0.0f, Math.Min(1.0f, (data.Current - 1.0f) * pFactor));
                }

                var point = new System.Windows.Input.StylusPoint(x * m_scale, y * m_scale, w);
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    _strokes[_strokes.Count - 1].StylusPoints.Add(point);
                    NotifyPropertyChanged("Strokes");
                }));

                m_addNewStrokeToModel = true;

            }));


        }

        private void Service_StrokeUpdated(object sender, StrokeUpdatedEventArgs e)
        {
            var pathPart = e.PathPart;
            var data = pathPart.Data.GetEnumerator();

            //Data is stored XYW
            float x = -1;
            float y = -1;
            float w = -1;

            if (data.MoveNext())
            {
                x = data.Current;
            }

            if (data.MoveNext())
            {
                y = data.Current;
            }

            if (data.MoveNext())
            {
                //Clamp to 0.0 -> 1.0
                w = Math.Max(0.0f, Math.Min(1.0f, (data.Current - 1.0f) * pFactor));
            }

            var point = new System.Windows.Input.StylusPoint(x * m_scale, y * m_scale, w);
            if (m_addNewStrokeToModel)
            {
                m_addNewStrokeToModel = false;
                var points = new System.Windows.Input.StylusPointCollection();
                points.Add(point);

                var stroke = new Stroke(points);
                stroke.DrawingAttributes = m_DrawingAttributes;

                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    _strokes.Add(stroke);
                }));
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    _strokes[_strokes.Count - 1].StylusPoints.Add(point);
                }));
            }

        }

        private void Service_StrokeStarted(object sender, StrokeStartedEventArgs e)
        {
            m_addNewStrokeToModel = true;
        }

        private void OnNewPage(object sender, EventArgs e)
        {
            var ignore = Task.Run(() =>
            {
                _strokes.Clear();

            });
        }

        private void OnDeviceDisconnected(object sender, EventArgs e)
        {
            AppObjects.Instance.Device = null;

            MessageBox.Show($"The device {AppObjects.Instance.DeviceInfo.DeviceName} was disconnected.");

            NavigationService.Navigate(new ScanAndConnectPage());
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetCanvasScaling();
        }

        private void SetCanvasScaling()
        {
            IDigitalInkDevice device = AppObjects.Instance.Device;

            if (device != null)
            {
                double sx = m_Canvas.ActualWidth / m_deviceSize.Width;
                double sy = m_Canvas.ActualHeight / m_deviceSize.Height;
                m_scale = Math.Min(sx, sy);
            }
        }

    }
}
