using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Xamarin.Forms;
using Android.Bluetooth;
using System;
using Java.Util;
using System.Text.RegularExpressions;
using Syncfusion.SfGauge.XForms;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Syncfusion.XForms.ProgressBar;

namespace inspirit5
{
    public enum CONNECTION_STATUS
    {
        CONNECTED,
        DISCONNECTED
    };

    public partial class App : Application
    {
        public static Queue<int> heartRate;
        public static Queue<int> tempeature;
        public static Queue<int> muscleTension;
        public static Queue<int> galvanicSkinResp;
        public static Queue<int> SPO2; //make this a member of the right class

        //not entirely sure what this Task nonsense does but it works so no touchy
        //return value doesn't matter
        public async Task<int> PostRequest(String csvRow)
        {
            if (csvRow == "")
                return 1;
            String json = "{\"payload\":[";
            json += csvRow.Replace("NULL", "\"NULL\"");
            json += "]}";
            try{
                try
                {   
                    //port 3000 like a scrubec2-35-170-243-227.compute-1.amazonaws.com
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(@"http://ec2-35-170-243-227.compute-1.amazonaws.com:3000");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    LOG(json);
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();   
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        String[] result = streamReader.ReadToEnd().Split('\n');

                        int j = 0;
                        double i = 0;
                        //result 1 is the number of true predictions
                        if (double.TryParse(result[1], out i))
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                ((StackLayoutExample)MainPage).needlePointer.Value = ((int)(i * 100.0));
                                LOG(((StackLayoutExample)MainPage).needlePointer.Value + "");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    LOG(e.ToString());
                }
            }
            catch (Exception e)
            {
                LOG(e.ToString());
            }
            await Task.Delay(100);
            return 1;
        }

        public static CONNECTION_STATUS connectionStatus = CONNECTION_STATUS.DISCONNECTED;

        public StackLayoutExample getMainPage(){
            return (StackLayoutExample)MainPage;
        }

        public App()
        {
            MainPage = new StackLayoutExample();
        }

        protected override void OnStart()
        {
            heartRate = new Queue<int>();
            tempeature = new Queue<int>();
            muscleTension = new Queue<int>();
            galvanicSkinResp = new Queue<int>();
            SPO2 = new Queue<int>();

            Task.Run(async () => HandleBluetooth("Bluefruit MYO"));

            // Handle when your app starts
        }

        private Boolean SetupAdapter(ref BluetoothAdapter adapter)
        {
            adapter = BluetoothAdapter.DefaultAdapter;
            if (adapter == null)
            {
                LOG("No Bluetooth adapter found.  Sleep for 5 seconds and retry.");
                return false;
            }
            else if (!adapter.IsEnabled)
            {
                LOG("Bluetooth not enabled.  Sleep for 5 seconds and retry.");
                return false;
            }
            return true;
        }

        private Boolean FindNamedDevice(String name, BluetoothAdapter adapter, ref BluetoothDevice device)
        {
            foreach (var bd in adapter.BondedDevices)
            {
                LOG("Paired devices found: " + bd.Name.ToUpper());

                if (bd.Name.ToUpper().IndexOf(name.ToUpper()) >= 0)
                {

                    LOG("Found " + bd.Name + ". Try to connect with it!");
                    device = bd;
                    return true;
                }
            }
            return false;
        }

        private async void HandleBluetooth(string deviceName)
        {
            BluetoothDevice device = null;
            BluetoothAdapter adapter = null;
            BluetoothGatt BthSocket = null;
            BGattCallback callback = new BGattCallback();
            callback.setApp(this);
            while (SetupAdapter(ref adapter) == false)
            {
                await Task.Delay(1000);
            }

            while (FindNamedDevice(deviceName, adapter, ref device) == false)
            {
                await Task.Delay(1000);
            }

            bool isSet = false;
            while (true)
            {
                if (connectionStatus == CONNECTION_STATUS.DISCONNECTED)
                {
                    BthSocket = device.ConnectGatt(Xamarin.Forms.Forms.Context, false, callback);
                    isSet = false;
                    await Task.Delay(5000);
                }
                BthSocket.DiscoverServices();
                LOG("Awaiting to do UI updates...");
                await Task.Delay(1000);

                foreach (BluetoothGattService service in BthSocket.Services)
                {
                    if (service.Uuid.ToString() != "0000180d-0000-1000-8000-00805f9b34fb")
                        continue;
                    LOG("SERVICE ID " + service.Uuid.ToString());
                    foreach (BluetoothGattCharacteristic characteristic in service.Characteristics)
                    {
                        LOG("CHARACTERISTIC ID: " + characteristic.Uuid.ToString());

                        if (characteristic.Uuid.ToString() == "00002a37-0000-1000-8000-00805f9b34fb")
                        {
                            if (isSet)
                                break;
                            BthSocket.SetCharacteristicNotification(characteristic, true);
                            UUID id = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
                            BluetoothGattDescriptor descriptor = characteristic.GetDescriptor(id);
                            descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                            Thread.Sleep(5000);
                            BthSocket.WriteDescriptor(descriptor).ToString();
                            isSet = true;
                        }
                    }
                }
            }
        }

        public void UpdateView(String label, int message)
        {
            BarPointer view = null;

            switch (label)
            {
                case "000":
                    view = ((StackLayoutExample)MainPage).GSR;
                    break;
                case "001":
                    view = ((StackLayoutExample)MainPage).MYO;
                    break;
                case "010":
                    view = ((StackLayoutExample)MainPage).Temp;
                    break;
                case "011":
                    view = ((StackLayoutExample)MainPage).SPO2;
                    break;
                case "100":
                    view = ((StackLayoutExample)MainPage).HeartRate;
                    break;
                default:
                    LOG("View not found: " + label);
                    return;
            }
            Device.BeginInvokeOnMainThread(() =>
            {
                view.Value = message;
            });
        }

        public static void LOG(String text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
    class TimerExampleState
    {
        public int counter = 0;
        public System.Threading.Timer tmr;
    }

    public class BGattCallback : BluetoothGattCallback
    {
        bool locked = false;

        static TimerExampleState s = new TimerExampleState();

        static Object lockObj = new Object();

        App app;
        public void setApp(App app){
            this.app = app;
        }

        TimerCallback timerDelegate;
        System.Threading.Timer timer;

        public BGattCallback() : base() {
            timerDelegate = new TimerCallback(purge);
            timer = new System.Threading.Timer(timerDelegate, s, 1000, 1000);
        }

        public void purge(Object state)
        {
            String output = "";
            lock (lockObj)
            {
                locked = true;
                //string path = (string)Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
                //string filename = Path.Combine(path, "april14.txt");
                //used for data collection
                if (App.heartRate.Count != 0)
                    output += App.heartRate.Average().ToString() + ", ";
                else{
                    locked = false;
                    return;
                }
                if (App.tempeature.Count != 0)
                    output += App.tempeature.Average().ToString() + ", ";
                else{
                    locked = false;
                    return;
                }
                if (App.muscleTension.Count != 0)
                    output += App.muscleTension.Average().ToString() + ", ";
                else{
                    locked = false;
                    return;
                }
                if (App.galvanicSkinResp.Count != 0)
                    output += App.galvanicSkinResp.Average().ToString() + ", ";
                else{
                    locked = false;
                    return;
                }
                if (App.SPO2.Count != 0)
                    output += App.SPO2.Average().ToString() + ", ";
                else{
                    locked = false;
                    return;
                }
                String stamp = DateTime.UtcNow.ToString();
                stamp = Regex.Replace(stamp, @".{1,2}/.{1,2}/.... ", "");
                stamp = stamp.Replace(" AM", "");
                stamp = stamp.Replace(" PM", "");
                stamp = stamp.Replace(":", "");

                output += stamp;

                App.heartRate = new Queue<int>();
                App.tempeature = new Queue<int>();
                App.muscleTension = new Queue<int>();
                App.galvanicSkinResp = new Queue<int>();
                App.SPO2 = new Queue<int>();

                /*
                using (var streamWriter = new StreamWriter(filename, true))
                {
                    streamWriter.WriteLine(output);
                }/**/
                locked = false;
            }
            Task.Run(async () => app.PostRequest(output));

        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState state)
        {
            if (state == ProfileState.Connected)
            {
                App.LOG("Made a connection");
            }
            else
            {
                App.LOG("Connection Dropped");
                App.connectionStatus = CONNECTION_STATUS.DISCONNECTED;
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            App.LOG("CHARACTERISTIC ID: " + characteristic.Uuid.ToString());
            byte[] data = characteristic.GetValue();
            if (locked)
                return;
            if (data != null && data.Length > 0)
            {
                lock (lockObj)
                {
                    locked = true;
                    int value = data[1];
                    String binary = IntToString(value);
                    String view = binary.Substring(5);
                    binary = binary.Substring(0, 5);
                    value = StringToInt(binary);
                    App.LOG("DATA " + value);
                    switch (view)
                    {
                        case "000":
                            App.galvanicSkinResp.Enqueue(value);
                            break;
                        case "001":
                            App.muscleTension.Enqueue(value);
                            break;
                        case "010":
                            App.tempeature.Enqueue(value);
                            break;
                        case "011":
                            App.SPO2.Enqueue(value);
                            break;
                        case "100":
                            App.heartRate.Enqueue(value);
                            break;
                    }
                    value = (int)(((double)value / 31) * 100);
                    app.UpdateView(view, value);
                    locked = false;
                }
            }
        }

        private String IntToString(int x)
        {
            String data = "";
            for (int i = 0; i < 8; i++)
            {
                if (x % 2 == 1)
                {
                    data = '1' + data;
                }
                else
                {
                    data = '0' + data;
                }
                x = x / 2;
            }
            return data;
        }

        private int StringToInt(String x)
        {
            int decimalValue = 0;
            int base_val = 1;
            for (int i = x.Length - 1; i >= 0; i--)
            {
                if (x[i] == '1')
                {
                    decimalValue += base_val;
                }
                base_val *= 2;
            }
            return decimalValue;
        }
    }

    public class StackLayoutExample : ContentPage
    {
        public SfCircularGauge circularGauge;
        public NeedlePointer needlePointer;
        public BarPointer HeartRate;
        public BarPointer MYO;
        public BarPointer SPO2;
        public BarPointer Temp;
        public BarPointer GSR;

        public StackLayoutExample()
        {
            circularGauge = new SfCircularGauge();
            Header header = new Header();
            header.Text = "Stress Levels";
            header.ForegroundColor = Color.Black;
            circularGauge.Headers.Add(header);


            ObservableCollection<Scale> scales = new ObservableCollection<Scale>();
            Scale scale = new Scale();
            needlePointer = new NeedlePointer();
            needlePointer.Value = 50;
            scale.Pointers.Add(needlePointer);

            Range rangeGood = new Range();
            rangeGood.StartValue = 0;
            rangeGood.EndValue = 33;
            rangeGood.Color = Color.Green;
            scale.Ranges.Add(rangeGood);

            Range rangeOk = new Range();
            rangeOk.StartValue = 33;
            rangeOk.EndValue = 66;
            rangeOk.Color = Color.Yellow;
            scale.Ranges.Add(rangeOk);

            Range rangeBad = new Range();
            rangeBad.StartValue = 66;
            rangeBad.EndValue = 100;
            rangeBad.Color = Color.Red;
            scale.Ranges.Add(rangeBad);

            scales.Add(scale);
            circularGauge.Scales = scales;

            BackgroundColor = Color.White;

            SfLinearGauge linearGauge = new SfLinearGauge();
            LinearHeader linearHeader = new LinearHeader();
            linearHeader.Text = "HeartRate";
            linearGauge.Header = linearHeader;
            LinearScale linearScale = new LinearScale();
            linearScale.ScaleBarColor = Color.FromHex("#e0e0e0");
            linearScale.LabelColor = Color.FromHex("#424242");
            linearScale.LabelOffset = -10;
            linearScale.ShowTicks = false;
            linearScale.ShowLabels = false;
            linearGauge.Scales.Add(linearScale);
            HeartRate = new BarPointer();
            HeartRate.Value = 50;
            HeartRate.Color = Color.FromHex("#757575");
            linearScale.Pointers.Add(HeartRate);

            SfLinearGauge linearGauge1 = new SfLinearGauge();
            LinearHeader linearHeader1 = new LinearHeader();
            linearHeader1.Text = "MYO";
            linearGauge1.Header = linearHeader1;
            LinearScale linearScale1 = new LinearScale();
            linearScale1.ScaleBarColor = Color.FromHex("#e0e0e0");
            linearScale1.LabelColor = Color.FromHex("#424242");
            linearScale1.LabelOffset = -10;
            linearScale1.ShowTicks = false;
            linearScale1.ShowLabels = false;
            linearGauge1.Scales.Add(linearScale1);
            MYO = new BarPointer();
            MYO.Value = 50;
            MYO.Color = Color.FromHex("#757575");
            linearScale1.Pointers.Add(MYO);

            SfLinearGauge linearGauge2 = new SfLinearGauge();
            LinearHeader linearHeader2 = new LinearHeader();
            linearHeader2.Text = "SPO2";
            linearGauge2.Header = linearHeader2;
            LinearScale linearScale2 = new LinearScale();
            linearScale2.ScaleBarColor = Color.FromHex("#e0e0e0");
            linearScale2.LabelColor = Color.FromHex("#424242");
            linearScale2.LabelOffset = -10;
            linearScale2.ShowTicks = false;
            linearScale2.ShowLabels = false;
            linearGauge2.Scales.Add(linearScale2);
            SPO2 = new BarPointer();
            SPO2.Value = 50;
            SPO2.Color = Color.FromHex("#757575");
            linearScale2.Pointers.Add(SPO2);

            SfLinearGauge linearGauge3 = new SfLinearGauge();
            LinearHeader linearHeader3 = new LinearHeader();
            linearHeader3.Text = "Temperature";
            linearGauge3.Header = linearHeader3;
            LinearScale linearScale3 = new LinearScale();
            linearScale3.ScaleBarColor = Color.FromHex("#e0e0e0");
            linearScale3.LabelColor = Color.FromHex("#424242");
            linearScale3.LabelOffset = -10;
            linearScale3.ShowTicks = false;
            linearScale3.ShowLabels = false;
            linearGauge3.Scales.Add(linearScale3);
            Temp = new BarPointer();
            Temp.Value = 50;
            Temp.Color = Color.FromHex("#757575");
            linearScale3.Pointers.Add(Temp);

            SfLinearGauge linearGauge4 = new SfLinearGauge();
            LinearHeader linearHeader4 = new LinearHeader();
            linearHeader4.Text = "GSR";
            linearGauge4.Header = linearHeader4;
            LinearScale linearScale4 = new LinearScale();
            linearScale4.ScaleBarColor = Color.FromHex("#e0e0e0");
            linearScale4.LabelColor = Color.FromHex("#424242");
            linearScale4.LabelOffset = -10;
            linearScale4.ShowTicks = false;
            linearScale4.ShowLabels = false;
            linearGauge4.Scales.Add(linearScale4);
            GSR = new BarPointer();
            GSR.Value = 50;
            GSR.Color = Color.FromHex("#757575");
            linearScale4.Pointers.Add(GSR);



            ScrollView scroll = new ScrollView();
            StackLayout stack = new StackLayout
            {
                Spacing = 0,
                Children = { circularGauge, linearGauge, linearGauge1, linearGauge2, linearGauge3, linearGauge4 }
            };
            scroll.Content = stack;
            Content = scroll;
        }
    }
}