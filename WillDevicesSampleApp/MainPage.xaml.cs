using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Text;
using Wacom.Devices;
using Wacom.SmartPadCommunication;

namespace WillDevicesSampleApp
{
	public sealed partial class MainPage : Page
	{
		CancellationTokenSource m_cts = new CancellationTokenSource();
		ObservableCollection<DevicePropertyValuePair> m_propertiesCollection;
		
		public MainPage()
		{
			this.InitializeComponent();

			Loaded += MainPage_Loaded;

			buttonRealTime.IsEnabled = false;
			//buttonScan.IsEnabled = false;

			m_propertiesCollection = new ObservableCollection<DevicePropertyValuePair>()
			{
				new DevicePropertyValuePair("Name"),
				new DevicePropertyValuePair("ESN"),
				new DevicePropertyValuePair("Width"),
				new DevicePropertyValuePair("Height"),
				new DevicePropertyValuePair("Point"),
                new DevicePropertyValuePair("Rate"),
                new DevicePropertyValuePair("Battery")
                
            };

			gridViewDeviceProperties.ItemsSource = m_propertiesCollection;
            
		}

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
            buttonScan.IsEnabled = true;
			buttonRealTime.IsEnabled = false;

      //NavigationService.Navigating += NavigationService_Navigating;
      NavigationService.Navigated += NavigationService_Navigated;

      if (AppObjects.Instance.DeviceInfo == null)
			{
				AppObjects.Instance.DeviceInfo = await AppObjects.DeserializeDeviceInfoAsync();
			}

			if (AppObjects.Instance.DeviceInfo == null)
			{
				textBlockDeviceName.Text = "Not connected to a device, click the \"Scan for Devices\" button and follow the instructions.";
				buttonScan.IsEnabled = true;
				return;
			}

			try
			{
				if (AppObjects.Instance.Device == null)
				{
					AppObjects.Instance.Device = await InkDeviceFactory.Instance.CreateDeviceAsync(AppObjects.Instance.DeviceInfo, AppObjects.Instance.AppId, false, false, OnDeviceStatusChanged);
				}

				AppObjects.Instance.Device.Disconnected += OnDeviceDisconnected;
				AppObjects.Instance.Device.DeviceStatusChanged += OnDeviceStatusChanged;
        AppObjects.Instance.Device.BarCodeScanned += OnBarCodeScanned;
        
        //AppObjects.Instance.Device.PairingModeEnabledCallback = OnPairingModeEnabledAsync;
      }
			catch (Exception ex)
			{
				textBlockDeviceName.Text = $"Cannot init device: {AppObjects.Instance.DeviceInfo.DeviceName} [{ex.Message}]";
				buttonScan.IsEnabled = true;
				return;
			}

			textBlockDeviceName.Text = $"Current device: {AppObjects.Instance.DeviceInfo.DeviceName}";

            buttonRealTime.IsEnabled = true;
			buttonScan.IsEnabled = true;

			textBlockStatus.Text = AppObjects.GetStringForDeviceStatus(AppObjects.Instance.Device.DeviceStatus);

			await DisplayDevicePropertiesAsync();
		}

    private void OnBarCodeScanned(object sender, BarcodeScannedEventArgs e)
    {
      MessageBox.Show($"Barcode scanned: {Encoding.ASCII.GetString(e.BarcodeData)}");
    }

    private void NavigationService_Navigated(object sender, NavigationEventArgs e)
    {
      // from OnNavigatedFrom
      IDigitalInkDevice device = AppObjects.Instance.Device;

      if (device != null)
      {
        device.PairingModeEnabledCallback = null;
        device.DeviceStatusChanged -= OnDeviceStatusChanged;
        device.Disconnected -= OnDeviceDisconnected;
      }

      m_cts.Cancel();

            object q = e.Content as RealTimeInkPage;
            if (q == null)
            {
                Frame o = sender as Frame;
                o.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            }
            
            //Frame.NavigationUIVisibilityProperty = NavigationUIVisibility.Hidden;
      
      // from OnNavigatedTo
      //SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
    }

		private async Task DisplayDevicePropertiesAsync()
		{
            //buttonScan.IsEnabled = false;
            buttonScan.IsEnabled = true;
            IDigitalInkDevice device = AppObjects.Instance.Device;

			try
			{
				m_propertiesCollection[0].PropertyValue = (string)await device.GetPropertyAsync(SmartPadProperties.DeviceName, m_cts.Token);
				m_propertiesCollection[1].PropertyValue = (string)await device.GetPropertyAsync(SmartPadProperties.SerialNumber, m_cts.Token);
				m_propertiesCollection[2].PropertyValue = ((uint)await device.GetPropertyAsync(SmartPadProperties.Width, m_cts.Token)).ToString();
				m_propertiesCollection[3].PropertyValue = ((uint)await device.GetPropertyAsync(SmartPadProperties.Height, m_cts.Token)).ToString();
				m_propertiesCollection[4].PropertyValue = ((uint)await device.GetPropertyAsync(SmartPadProperties.PointSize, m_cts.Token)).ToString();
                m_propertiesCollection[5].PropertyValue = ((uint)await device.GetPropertyAsync(SmartPadProperties.SamplingRate, m_cts.Token)).ToString();
                m_propertiesCollection[6].PropertyValue = ((int)await device.GetPropertyAsync(SmartPadProperties.BatteryLevel, m_cts.Token)).ToString() + "%";
			}
			catch (Exception ex)
			{
				textBlockStatus.Text = $"Exception: {ex.Message}";

                buttonRealTime.IsEnabled = false;
				buttonScan.IsEnabled = true;
			}
		}

		private void ButtonScan_Click(object sender, RoutedEventArgs e)
		{
			NavigationService.Navigate(new ScanAndConnectPage());
		}

		//private void ButtonFileTransfer_Click(object sender, RoutedEventArgs e)
		//{
  //    //NavigationService.Navigate(typeof(FileTransferPage));
		//}

		private void ButtonRealTime_Click(object sender, RoutedEventArgs e)
		{
      NavigationService.Navigate(new RealTimeInkPage());
		}

		private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
		{
			var ignore = Task.Run( () =>
			{
				switch (e.Status)
				{
					case DeviceStatus.Idle:
						textBlockStatus.Text = "";
						buttonRealTime.IsEnabled = true;
						break;

					case DeviceStatus.ExpectingButtonTapToConfirmConnection:
						textBlockStatus.Text = "Tap the Central Button to confirm the connection.";
						buttonRealTime.IsEnabled = false;
						break;

					case DeviceStatus.ExpectingButtonTapToReconnect:
						textBlockStatus.Text = "Tap the Central Button to restore the connection.";
						break;

					case DeviceStatus.HoldButtonToEnterUserConfirmationMode:
						textBlockStatus.Text = "Press and hold the Central Button to enter user confirmation mode.";
						break;

					//case DeviceStatus.AcknowledgeConnectionConfirmationTimeout:
					//	await new MessageDialog("The connection confirmation period expired.").ShowAsync();
					//	Frame.Navigate(typeof(ScanAndConnectPage));
					//	break;
				}
			});
		}

		private void OnDeviceDisconnected(object sender, EventArgs e)
		{
			AppObjects.Instance.Device = null;

			//var ignore = Task.Run( () =>
			{
				MessageBox.Show($"The device {AppObjects.Instance.DeviceInfo.DeviceName} was disconnected.");

				NavigationService.Navigate(new ScanAndConnectPage());
			}
            //);
		}
	}
}
