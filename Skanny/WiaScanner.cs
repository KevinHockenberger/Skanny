using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Skanny
{
  static class WiaScanner
  {
    class WIA_PROPERTIES
    {
      public const uint WIA_RESERVED_FOR_NEW_PROPS = 1024;
      public const uint WIA_DIP_FIRST = 2;
      public const uint WIA_DPA_FIRST = WIA_DIP_FIRST + WIA_RESERVED_FOR_NEW_PROPS;
      public const uint WIA_DPC_FIRST = WIA_DPA_FIRST + WIA_RESERVED_FOR_NEW_PROPS;
      //
      // Scanner only device properties (DPS)
      //
      public const uint WIA_DPS_FIRST = WIA_DPC_FIRST + WIA_RESERVED_FOR_NEW_PROPS;
      public const uint WIA_DPS_DOCUMENT_HANDLING_STATUS = WIA_DPS_FIRST + 13;
      public const uint WIA_DPS_DOCUMENT_HANDLING_SELECT = WIA_DPS_FIRST + 14;

      public const int WIAFacility = 33;
      public const int WIA_ERROR_GENERAL_ERROR = 1;
      public const int WIA_ERROR_PAPER_JAM = 2;
      public const int WIA_ERROR_PAPER_EMPTY = 3;
      public const int WIA_ERROR_PAPER_PROBLEM = 4;
      public const int WIA_ERROR_OFFLINE = 5;
      public const int WIA_ERROR_BUSY = 6;
      public const int WIA_ERROR_WARMING_UP = 7;
      public const int WIA_ERROR_USER_INTERVENTION = 8;
      public const int WIA_ERROR_ITEM_DELETED = 9;
      public const int WIA_ERROR_DEVICE_COMMUNICATION = 10;
      public const int WIA_ERROR_INVALID_COMMAND = 11;
      public const int WIA_ERROR_INCORRECT_HARDWARE_SETTING = 12;
      public const int WIA_ERROR_DEVICE_LOCKED = 13;
      public const int WIA_ERROR_EXCEPTION_IN_DRIVER = 14;
      public const int WIA_ERROR_INVALID_DRIVER_RESPONSE = 15;
      public const int WIA_S_NO_DEVICE_AVAILABLE = 21;
    }
    class WIA_DPS_DOCUMENT_HANDLING_SELECT
    {
      public const uint FEEDER = 0x00000001;
      public const uint FLATBED = 0x00000002;
    }
    class WIA_DPS_DOCUMENT_HANDLING_STATUS
    {
      public const uint FEED_READY = 0x00000001;
    }

    const string wiaFormatBMP = "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}";
    const string wiaFormatGIF = "{B96B3CB0-0728-11D3-9D7B-0000F81EF32E}";
    const string wiaFormatJPEG = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
    const string wiaFormatPNG = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";
    const string wiaFormatTIFF = "{B96B3CB1-0728-11D3-9D7B-0000F81EF32E}";

    public static Dictionary<string, string> GetDevices()
    {
      Dictionary<string, string> devices = new Dictionary<string, string>();
      WIA.DeviceManager manager = new WIA.DeviceManager();

      foreach (WIA.DeviceInfo info in manager.DeviceInfos)
      {
        foreach (WIA.Property p in info.Properties)
        {
          if (p.Name == "Name")
          {
            devices.Add(info.DeviceID, p.get_Value());
            //Console.WriteLine(p.Name + ":" + p.get_Value());            
          }
        }
      }
      return devices;
    }
    public static List<Image> Scan()
    {
      WIA.ICommonDialog dialog = new WIA.CommonDialog();
      WIA.Device device = dialog.ShowSelectDevice(WIA.WiaDeviceType.UnspecifiedDeviceType, true, false);

      if (device != null)
      {
        return Scan(device.DeviceID);
      }
      else
      {
        throw new Exception("You must select a device for scanning.");
      }
    }
    public static List<Image> Scan(string scannerName)
    {
      List<Image> images = new List<Image>();
      bool hasMorePages = true;
      bool paperEmpty = false;
      while (hasMorePages)
      {
        Dictionary<string, string> devices = WiaScanner.GetDevices();
        var scannerId = devices.FirstOrDefault(x => x.Value == scannerName).Key;
        WIA.DeviceManager manager = new WIA.DeviceManager();
        WIA.Device device = null;
        // select the correct scanner using the provided scannerId parameter
        foreach (WIA.DeviceInfo info in manager.DeviceInfos)
        {
          if (info.DeviceID == scannerId && info.Type == WIA.WiaDeviceType.ScannerDeviceType)
          {
            // connect to scanner
            device = info.Connect();
            break;
          }
        }

        // device was not found
        if (device == null)
        {
          // enumerate available devices
          string availableDevices = "";
          foreach (var d in devices)
          {
            availableDevices += d.Value + "\n";
          }

          // show error with available devices
          throw new Exception("The device with provided ID could not be found. Available Devices:\n" + availableDevices);
        }

        WIA.Item item = SetPaperSetting(device);

        try
        {
          // scan image
          WIA.ICommonDialog wiaCommonDialog = new WIA.CommonDialog();
          WIA.ImageFile image = (WIA.ImageFile)wiaCommonDialog.ShowTransfer(item, wiaFormatJPEG, true);

          //WIA.ImageProcess imp = new WIA.ImageProcess();  // use to compress jpeg.
          //imp.Filters.Add(imp.FilterInfos["Convert"].FilterID);
          ////imp.Filters[1].Properties["FormatID"].set_Value(wiaFormatJPEG);
          //imp.Filters[1].Properties["FormatID"].set_Value(wiaFormatJPEG);
          //imp.Filters[1].Properties["Quality"].set_Value(80); // 1 = low quality, 100 = best
          //image = imp.Apply(image);  // apply the filters

          // get a tempfile path
          string fileName = Path.GetTempFileName();
          // delete any existing file first
          File.Delete(fileName);
          // save to temp file
          image.SaveFile(fileName);
          // make the original (wia) image null
          image = null;
          // get system.drawing image from temporary file and add file to output list
          images.Add(Image.FromFile(fileName, true));

          //var imageBytes = (byte[])image.FileData.get_BinaryData();
          //var stream = new MemoryStream(imageBytes);
          //images.Add(Image.FromStream(stream));
        }
        catch (System.Runtime.InteropServices.COMException cx)
        {
          string ex = string.Empty;
          int comErrorCode = GetWIAErrorCode(cx);
          if (comErrorCode > 0)
          {
            ex = GetErrorCodeDescription(comErrorCode);
          }
          if (comErrorCode == 3 && images.Count == 0)
          {
            throw new Exception(ex);
          }
          else if (comErrorCode == 3 && images.Count > 0)
          {
            paperEmpty = true;
          }
        }

        finally
        {
          item = null;
          // assume there are no more pages
          hasMorePages = false;
          if (!paperEmpty)
          {
            //determine if there are any more pages waiting
            WIA.Property documentHandlingSelect = null;
            WIA.Property documentHandlingStatus = null;

            foreach (WIA.Property prop in device.Properties)
            {
              if (prop.PropertyID == WIA_PROPERTIES.WIA_DPS_DOCUMENT_HANDLING_SELECT)
                documentHandlingSelect = prop;

              if (prop.PropertyID == WIA_PROPERTIES.WIA_DPS_DOCUMENT_HANDLING_STATUS)
                documentHandlingStatus = prop;

            }

            // may not exist on flatbed scanner but required for feeder
            if (documentHandlingSelect != null)
            {
              // check for document feeder
              if ((Convert.ToUInt32(documentHandlingSelect.get_Value()) & WIA_DPS_DOCUMENT_HANDLING_SELECT.FEEDER) != 0)
              {
                hasMorePages = ((Convert.ToUInt32(documentHandlingStatus.get_Value()) & WIA_DPS_DOCUMENT_HANDLING_STATUS.FEED_READY) != 0);
              }
            }
          }
        }
      }
      return images;
    }
    private static WIA.Item SetPaperSetting(WIA.Device device)
    {
      WIA.Item item = device.Items[1] as WIA.Item;
      double width = 8.5; //8.5 inches
      double height = 11; //11 inches
      //int dpi = Avari_Receiving.Properties.Settings.Default.ScannerDPI;
      int dpi = Skanny.Properties.Settings.Default.ScannerDPI;
      switch (Skanny.Properties.Settings.Default.ScannerColorFormat)
      {
        case (int)ColorFormats.BlackandWhiteOnly:
          try
          {
            item.Properties["6146"].set_Value(4); //b&w
          }
          catch (Exception)
          {
          }
          break;
        case (int)ColorFormats.Grayscale:
          try
          {
            item.Properties["6146"].set_Value(2); //grayscale
          }
          catch (Exception)
          {
          }
          break;
        case (int)ColorFormats.Color:
          try
          {
            item.Properties["6146"].set_Value(1); //color
          }
          catch (Exception)
          {
          }
          try
          {
            item.Properties["4104"].set_Value(24); //color depth - read only on some scanners. catch exception if so.
          }
          catch (Exception)
          {
          }
          break;
        default:
          try
          {
            item.Properties["6146"].set_Value(2); //grayscale
          }
          catch (Exception)
          {
          }
          break;
      }
      item.Properties["6147"].set_Value(dpi);
      item.Properties["6148"].set_Value(dpi);
      item.Properties["6151"].set_Value((int)(dpi * width));
      item.Properties["6152"].set_Value((int)(dpi * height));
      return item;
    }
    public static int GetWIAErrorCode(System.Runtime.InteropServices.COMException cx)
    {
      int origErrorMsg = cx.ErrorCode;
      int errorCode = origErrorMsg & 0xFFFF;
      int errorFacility = ((origErrorMsg) >> 16) & 0x1fff;
      if (errorFacility == WIA_PROPERTIES.WIAFacility) { return errorCode; }
      return -1;
    }
    public static string GetErrorCodeDescription(int errorCode)
    {
      string desc = null;
      switch (errorCode)
      {
        case (WIA_PROPERTIES.WIA_ERROR_GENERAL_ERROR):
          {
            desc = "A general error occurred";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_PAPER_JAM):
          {
            desc = "There is a paper jam";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_PAPER_EMPTY):
          {
            desc = "The feeder tray is empty";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_PAPER_PROBLEM):
          {
            desc = "There is a problem with the paper";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_OFFLINE):
          {
            desc = "The scanner is offline";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_BUSY):
          {
            desc = "The scanner is busy";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_WARMING_UP):
          {
            desc = "The scanner is warming up";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_USER_INTERVENTION):
          {
            desc = "The scanner requires user intervention";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_ITEM_DELETED):
          {
            desc = "An unknown error occurred";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_DEVICE_COMMUNICATION):
          {
            desc = "An error occurred attempting to communicate with the scanner";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_INVALID_COMMAND):
          {
            desc = "The scanner does not understand this command";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_INCORRECT_HARDWARE_SETTING):
          {
            desc = "The scanner has an incorrect hardware setting";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_DEVICE_LOCKED):
          {
            desc = "The scanner device is in use by another application";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_EXCEPTION_IN_DRIVER):
          {
            desc = "The scanner driver reported an error";
            break;
          }
        case (WIA_PROPERTIES.WIA_ERROR_INVALID_DRIVER_RESPONSE):
          {
            desc = "The scanner driver gave an invalid response";
            break;
          }
        default:
          {
            desc = "An unknown error occurred";
            break;
          }
      }
      return desc;
    }

  }
}
