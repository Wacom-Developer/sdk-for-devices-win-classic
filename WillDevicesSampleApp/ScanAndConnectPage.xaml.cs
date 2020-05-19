using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using Wacom;
using Wacom.Devices;
using Wacom.Devices.Enumeration;

namespace WillDevicesSampleApp
{
    public sealed partial class ScanAndConnectPage : Page
    {
        InkDeviceWatcherUSB m_watcherUSB;
        InkDeviceInfo m_connectingDeviceInfo;
        ObservableCollection<InkDeviceInfo> m_deviceInfos = new ObservableCollection<InkDeviceInfo>();


        public ScanAndConnectPage()
        {
            this.InitializeComponent();

            this.DataContext = this;


            m_watcherUSB = new InkDeviceWatcherUSB();
            m_watcherUSB.DeviceAdded += OnDeviceAdded;
            m_watcherUSB.DeviceRemoved += OnDeviceRemoved;


            Loaded += ScanAndConnectPage_Loaded;
            Unloaded += ScanAndConnectPage_Unloaded;


        }

        public ObservableCollection<InkDeviceInfo> DeviceInfos
        {
            get
            {
                return m_deviceInfos;
            }
        }

        private void ScanAndConnectPage_Navigating(object sender, NavigatingCancelEventArgs e)
        {

            // Navigate back if possible, and if the event has not 
            // already been handled .
            if (e.NavigationMode == NavigationMode.Back
              && NavigationService != null
              && NavigationService.CanGoBack
              && e.Cancel == false)
                StopScanning();

        }

        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {

        }

        private void ScanAndConnectPage_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigating += ScanAndConnectPage_Navigating;
            NavigationService.Navigated += NavigationService_Navigated;

            AppObjects.Instance.DeviceInfo = null;

            if (AppObjects.Instance.Device != null)
            {
                AppObjects.Instance.Device.Close();
                AppObjects.Instance.Device = null;
            }

            StartScanning();
            KeepAlive = true;
        }

        private void ScanAndConnectPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (AppObjects.Instance.Device != null)
            {
                AppObjects.Instance.Device.DeviceStatusChanged -= OnDeviceStatusChanged;
            }

            StopWatchers();

        }

        private void StartScanning()
        {
            StartWatchers();

            BtnUsbScanSetScanningAndDisabled();
            TextBoxBleSetText();
            TextBoxUsbSetText();
        }

        private void StopScanning()
        {
            StopWatchers();

            BtnUsbScanSetScanAndDisabled();
            TextBoxBleSetEmpty();
            TextBoxUsbSetEmpty();
        }

        private void StartWatchers()
        {
            m_watcherUSB.Start();
        }

        private void StopWatchers()
        {
            m_watcherUSB.Stop();
        }

        private async void OnButtonConnectClick(object sender, RoutedEventArgs e)
        {
            int index = listView.SelectedIndex;

            if ((index < 0) || (index >= m_deviceInfos.Count))
                return;

            IDigitalInkDevice device = null;
            m_connectingDeviceInfo = m_deviceInfos[index];

            btnConnect.IsEnabled = false;

            StopScanning();

            try
            {
                device = await InkDeviceFactory.Instance.CreateDeviceAsync(m_connectingDeviceInfo, AppObjects.Instance.AppId, true, false, OnDeviceStatusChanged);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder($"Device creation failed:\n{ex.Message}");
                string indent = "  ";
                for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
                {
                    sb.Append($"\n{indent}{inner.Message}");
                    indent = indent + "  ";
                }

                MessageBox.Show(sb.ToString());
            }

            if (device == null)
            {
                m_connectingDeviceInfo = null;
                btnConnect.IsEnabled = true;
                StartScanning();
                return;
            }

            AppObjects.Instance.DeviceInfo = m_connectingDeviceInfo;
            AppObjects.Instance.Device = device;
            m_connectingDeviceInfo = null;

            await AppObjects.SerializeDeviceInfoAsync(AppObjects.Instance.DeviceInfo);

            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void OnButtonUsbScanClick(object sender, RoutedEventArgs e)
        {
            if (m_watcherUSB.Status != DeviceWatcherStatus.Started &&
              m_watcherUSB.Status != DeviceWatcherStatus.Stopping &&
              m_watcherUSB.Status != DeviceWatcherStatus.EnumerationCompleted)
            {
                m_watcherUSB.Start();
                BtnUsbScanSetScanningAndDisabled();
                TextBoxUsbSetText();
            }
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            var ignore = Task.Run(() =>
            {
                tbBle.Text = AppObjects.GetStringForDeviceStatus(e.Status); // FIX: make a switch on the transport protocol to switch the message for each text boxF
            });
        }

        private void OnDeviceAdded(object sender, InkDeviceInfo info)
        {
            //var ignore = Task.Run( () =>
            //{
            m_deviceInfos.Add(info);
            //});
        }

        private void OnDeviceRemoved(object sender, InkDeviceInfo info)
        {
            //var ignore = Task.Run( () =>
            //{
            RemoveDevice(info);
            //});
        }

#if false
    private void OnAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
    {
      StopWatchers();
    }

    private void OnAppResuming(object sender, object e)
    {
      if (AppObjects.Instance.DeviceInfo == null)
      {
        StartScanning();
      }
    } 
#endif

        private void RemoveDevice(InkDeviceInfo info)
        {
            int index = -1;

            for (int i = 0; i < m_deviceInfos.Count; i++)
            {
                if (m_deviceInfos[i].DeviceId == info.DeviceId)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                m_deviceInfos.RemoveAt(index);
            }
        }

        #region Set UI Elements

        private void BtnUsbScanSetScanningAndDisabled()
        {
            btnUsbScan.Content = "Scanning";
            btnUsbScan.IsEnabled = false;
        }

        private void BtnUsbScanSetScanAndEnabled()
        {
            btnUsbScan.Content = "Scan";
            btnUsbScan.IsEnabled = true;
        }

        private void BtnUsbScanSetScanAndDisabled()
        {
            btnUsbScan.Content = "Scan";
            btnUsbScan.IsEnabled = false;
        }

        private void TextBoxBleSetText()
        {
            tbUsb.Text = "Connect the device to a USB port and turn it on.";
        }

        private void TextBoxBleSetEmpty()
        {
            tbBle.Text = string.Empty;
        }

        private void TextBoxUsbSetText()
        {
            tbUsb.Text = "Connect the device to a USB port and turn it on.";
        }

        private void TextBoxUsbSetEmpty()
        {
            tbUsb.Text = string.Empty;
        }

        #endregion
    }
}
