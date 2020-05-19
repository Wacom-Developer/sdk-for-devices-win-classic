using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Forms;
using Wacom.Devices;
using Wacom.SmartPadCommunication;

namespace WillDevicesSampleApp
{
  public class AppObjects
  {
    public static readonly AppObjects Instance = new AppObjects();
    private static readonly string SaveFileName = "SavedData";

    private AppObjects()
    {
      AppId = new SmartPadClientId(0xFA, 0xAB, 0xC1, 0xE0, 0xF1, 0x77);
    }

    public IDigitalInkDevice Device
    {
      get;
      set;
    }

    public SmartPadClientId AppId
    {
      get;
    }

    public InkDeviceInfo DeviceInfo
    {
      get;
      set;
    }


    public static async Task SerializeDeviceInfoAsync(InkDeviceInfo deviceInfo)
    {
      try
      {
        using (FileStream fs = File.Create(Path.Combine(Application.LocalUserAppDataPath, SaveFileName)))
        {
          await Task.Run(new Action(() => deviceInfo.ToStream(fs)));
        }
      }
      catch (Exception)
      {
      }
    }

    public static async Task<InkDeviceInfo> DeserializeDeviceInfoAsync()
    {
      try
      {
        using (FileStream fs = File.OpenRead(Path.Combine(Application.LocalUserAppDataPath, SaveFileName)))
        {
          return await InkDeviceInfo.FromStreamAsync(fs);
        }
      }
      catch (Exception)
      {
      }

      return null;
    }


    public static Matrix CalculateTransform(uint deviceWidth, uint deviceHeight, uint ptSizeInMicrometers)
    {
      float scaleFactor = ptSizeInMicrometers * micrometerToDip;

      Matrix m = new Matrix();

      //ScaleTransform st = new ScaleTransform();
      //st.ScaleX = scaleFactor;
      //st.ScaleY = scaleFactor;
      m.Scale(scaleFactor, scaleFactor);

      //RotateTransform rt = new RotateTransform();
      //rt.Angle = 90;
      ////m.Rotate(90);

      //TranslateTransform tt = new TranslateTransform();
      //tt.X = deviceHeight * scaleFactor;
      //tt.Y = 0;
      m.Translate(deviceHeight * scaleFactor, 0);

      //TransformGroup tg = new TransformGroup();
      //tg.Children.Add(st);
      //tg.Children.Add(rt);
      //tg.Children.Add(tt);

      //return tg.Value;
      return m;
    }

    public static string GetStringForDeviceStatus(DeviceStatus deviceStatus)
    {
      string text = string.Empty;

      switch (deviceStatus)
      {
        case DeviceStatus.Idle:
          break;

        case DeviceStatus.ExpectingButtonTapToConfirmConnection:
          text = "Tap the Central Button to confirm the connection.";
          break;

        case DeviceStatus.ExpectingButtonTapToReconnect:
          text = "Tap the Central Button to restore the connection.";
          break;

        case DeviceStatus.HoldButtonToEnterUserConfirmationMode:
          text = "Press and hold the Central Button to enter user confirmation mode.";
          break;

        case DeviceStatus.AcknowledgeConnectionConfirmationTimeout:
          text = "The connection confirmation period expired.";
          break;
      }

      return text;
    }

#if false
    public async Task<bool> ShowPairingModeEnabledDialogAsync()
    {
      var dialog = new MessageDialog($"The device {DeviceInfo.DeviceName} is in pairing mode. How do you want proceed?");
      dialog.Commands.Add(new UICommand("Keep using the device") { Id = 0 });
      dialog.Commands.Add(new UICommand("Forget the device") { Id = 1 });

      var dialogResult = await dialog.ShowAsync();

      return ((int)dialogResult.Id == 0);
    } 
#endif

    public const float micrometerToDip = 96.0f / 25400.0f;
  }
}