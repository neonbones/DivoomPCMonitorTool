using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace DivoomPCMontiorTool
{
    public partial class DivoomPCMontiorForm : Form
    {
        // Constants for HTTP requests
        private const string BASE_API_URL = "http://app.divoom-gz.com";
        private const string DEVICE_LIST_ENDPOINT = "/Device/ReturnSameLANDevice";
        private const string LCD_INFO_ENDPOINT = "/Channel/Get5LcdInfoV2";
        private const string POST_ENDPOINT = "/post";
        private const string HTTP_PROTOCOL = "http://";
        private const string HTTP_PORT = ":80";
        private const int HTTP_TIMEOUT = 1000;
        private const string JSON_CONTENT_TYPE = "application/json";
        private const string FORM_CONTENT_TYPE = "application/x-www-form-urlencoded";
        private const string UTF8_ENCODING = "utf-8";

        // Constants for Divoom commands
        private const string UPDATE_PC_INFO_COMMAND = "Device/UpdatePCParaInfo";
        private const string SET_CLOCK_SELECT_COMMAND = "Channel/SetClockSelectId";
        private const int DEFAULT_CLOCK_ID = 625;
        private const int TIME_GATE_HARDWARE_ID = 400;

        // Constants for value formatting
        private const string DEFAULT_VALUE = "--";
        private const string CELSIUS_SUFFIX = "°C";
        private const string PERCENT_SUFFIX = "%";
        private const int MAX_PERCENT_LENGTH = 2;

        // Constants for UI
        private const string CPU_TEMP_PREFIX = "CpuTemp:";
        private const string CPU_USE_PREFIX = "CpuUse:";
        private const string GPU_TEMP_PREFIX = "GpuTemp:";
        private const string GPU_USE_PREFIX = "GpuUse:";
        private const string HDD_USE_PREFIX = "HddUse:";

        // Private class fields
        private UpdateVisitor _updateVisitor;
        private Computer _computer;
        private string _deviceIpAddr;
        private DivoomDeviceList _localList;
        private int _selectLcdId;

        public DivoomPCMontiorForm()
        {
            InitializeComponent();

            _updateVisitor = new UpdateVisitor();
            _computer = new Computer
            {
                HDDEnabled = false
            };
            _computer.Open();

            LCDMsg.Visible = false;
            LCDList.Visible = false;
            DivoomUpdateDeviceList();
        }

        /// <summary>
        /// Sends HTTP POST request to the specified URL
        /// </summary>
        /// <param name="url">URL for the request</param>
        /// <param name="sendData">Data to send</param>
        /// <param name="result">Request result</param>
        /// <returns>0 on success, -1 on error</returns>
        public static int HttpPost(string url, string sendData, out string result)
        {
            result = string.Empty;

            try
            {
                var data = Encoding.UTF8.GetBytes(sendData);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.Method = "POST";
                request.ContentType = JSON_CONTENT_TYPE;
                request.ContentLength = data.Length;
                request.Timeout = HTTP_TIMEOUT;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
                return -1;
            }
            
            return 0;
        }

        /// <summary>
        /// Sends HTTP POST request with form data
        /// </summary>
        /// <param name="url">URL for the request</param>
        /// <param name="postDataStr">Form data</param>
        /// <returns>Server response</returns>
        public static string HttpPost2(string url, string postDataStr)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = FORM_CONTENT_TYPE;
            Encoding encoding = Encoding.UTF8;
            byte[] postData = encoding.GetBytes(postDataStr);
            request.ContentLength = postData.Length;
            
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, encoding))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Performs HTTP GET request
        /// </summary>
        /// <param name="url">URL for the request</param>
        /// <returns>Server response</returns>
        public static string HttpGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding(UTF8_ENCODING)))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Gets formatted temperature value for the specified hardware types
        /// </summary>
        private string GetFormattedTemperature(params HardwareType[] hardwareTypes)
        {
            var formattedTemperature = DEFAULT_VALUE;
            try
            {
                var hardware = _computer.Hardware.FirstOrDefault(x => hardwareTypes.Contains(x.HardwareType));
                if (hardware == null)
                {
                    return formattedTemperature;
                }

                var tempSensors = hardware.Sensors.Where(x => x.SensorType == SensorType.Temperature);
                if (!tempSensors.Any())
                {
                    return formattedTemperature;
                }

                var temperature = Math.Round(tempSensors.Average(x => (decimal)x.Value));
                return $"{temperature}{CELSIUS_SUFFIX}";
            }
            catch
            {
                return formattedTemperature;
            }
        }

        /// <summary>
        /// Gets formatted usage value for the specified hardware types
        /// </summary>
        private string GetFormattedUsage(params HardwareType[] hardwareTypes)
        {
            var formattedUsage = DEFAULT_VALUE;
            try
            {
                var hardware = _computer.Hardware.FirstOrDefault(x => hardwareTypes.Contains(x.HardwareType));
                if (hardware == null)
                {
                    return formattedUsage;
                }

                var loadSensors = hardware.Sensors.Where(x => x.SensorType == SensorType.Load);
                var loadSensor = hardware.HardwareType == HardwareType.CPU 
                    ? loadSensors.LastOrDefault()
                    : loadSensors.FirstOrDefault();

                if (loadSensor == null)
                {
                    return formattedUsage;
                }

                var usage = loadSensor.Value.ToString();
                if (usage.Length > MAX_PERCENT_LENGTH)
                {
                    usage = usage.Substring(0, MAX_PERCENT_LENGTH);
                }
                return $"{usage}{PERCENT_SUFFIX}";
            }
            catch
            {
                return formattedUsage;
            }
        }

        /// <summary>
        /// Gets formatted CPU temperature value
        /// </summary>
        private string GetCpuTemperature() => GetFormattedTemperature(HardwareType.CPU);

        /// <summary>
        /// Gets formatted GPU temperature value
        /// </summary>
        private string GetGpuTemperature() => GetFormattedTemperature(HardwareType.GpuNvidia, HardwareType.GpuAti);

        /// <summary>
        /// Gets formatted HDD temperature value
        /// </summary>
        private string GetHddTemperature() => GetFormattedTemperature(HardwareType.HDD);

        /// <summary>
        /// Gets formatted CPU usage value
        /// </summary>
        private string GetCpuUsage() => GetFormattedUsage(HardwareType.CPU);

        /// <summary>
        /// Gets formatted GPU usage value
        /// </summary>
        private string GetGpuUsage() => GetFormattedUsage(HardwareType.GpuNvidia, HardwareType.GpuAti);

        /// <summary>
        /// Gets formatted RAM usage value
        /// </summary>
        private string GetRamUsage()
        {
            try
            {
                var memInfo = new MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
                };

                GlobalMemoryStatusEx(ref memInfo);

                var usagePercent = (memInfo.ullTotalPhys - memInfo.ullAvailPhys) * 100 / memInfo.ullTotalPhys;
                var usage = usagePercent.ToString();
                if (usage.Length > MAX_PERCENT_LENGTH)
                {
                    usage = usage.Substring(0, MAX_PERCENT_LENGTH);
                }
                return $"{usage}{PERCENT_SUFFIX}";
            }
            catch
            {
                return DEFAULT_VALUE;
            }
        }

        /// <summary>
        /// Sends system information to the Divoom device
        /// </summary>
        private void DivoomSendHttpInfo(object sender, EventArgs e)
        {
            try
            {
                _computer.Accept(_updateVisitor);
            }
            catch
            {
                return;
            }

            if (_deviceIpAddr == null || _localList == null || _localList.DeviceList == null || _localList.DeviceList.Length == 0)
            {
                return;
            }
            

            var postInfo = new DivoomDevicePostList();
            var postItem = new DivoomDevicePostItem();
            postInfo.Command = UPDATE_PC_INFO_COMMAND;
            postInfo.ScreenList = new DivoomDevicePostItem[1];
            postItem.DispData = new string[6];

            if (_deviceIpAddr.Length <= 0 || _computer == null)
            {
                return;
            }
            
            postItem.LcdId = _selectLcdId;

            var cpuTempValue = GetCpuTemperature();
            var cpuUseValue = GetCpuUsage();
            var gpuTempValue = GetGpuTemperature();
            var gpuUseValue = GetGpuUsage();
            var hardDiskUseValue = GetHddTemperature();
            var ramUseValue = GetRamUsage();

            postItem.DispData[0] = cpuUseValue;
            postItem.DispData[1] = gpuUseValue;
            postItem.DispData[2] = cpuTempValue;
            postItem.DispData[3] = gpuTempValue;
            postItem.DispData[4] = ramUseValue;
            postItem.DispData[5] = hardDiskUseValue;
            postInfo.ScreenList[0] = postItem;
            
            CpuTemp.Text = $"{CPU_TEMP_PREFIX}{cpuTempValue}";
            CpuUse.Text = $"{CPU_USE_PREFIX}{cpuUseValue}";
            GpuTemp.Text = $"{GPU_TEMP_PREFIX}{gpuTempValue}";
            GpuUse.Text = $"{GPU_USE_PREFIX}{gpuUseValue}";
            HddUse.Text = $"{HDD_USE_PREFIX}{hardDiskUseValue}";
    
            string paraInfo = JsonConvert.SerializeObject(postInfo);

            HttpPost($"{HTTP_PROTOCOL}{_deviceIpAddr}{HTTP_PORT}{POST_ENDPOINT}", paraInfo, out _);
        }

        /// <summary>
        /// Updates the list of Divoom devices in the local network
        /// </summary>
        private void DivoomUpdateDeviceList()
        {
            string urlInfo = $"{BASE_API_URL}{DEVICE_LIST_ENDPOINT}";
            string deviceList = HttpGet(urlInfo);
            _localList = JsonConvert.DeserializeObject<DivoomDeviceList>(deviceList);
            divoomList.Items.Clear();
            
            if (_localList?.DeviceList != null)
            {
                for (int i = 0; i < _localList.DeviceList.Length; i++)
                {
                    divoomList.Items.Add(_localList.DeviceList[i].DeviceName);
                }
            }
        }

        /// <summary>
        /// Handler for the refresh list button click
        /// </summary>
        private void RefreshList_Click(object sender, EventArgs e)
        {
            DivoomUpdateDeviceList();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        public static extern void GlobalMemoryStatusEx(ref MEMORYSTATUSEX stat);

        /// <summary>
        /// Sends the clock selection command to the Divoom device
        /// </summary>
        private void DivoomSendSelectClock()
        {
            _deviceIpAddr = _localList.DeviceList[divoomList.SelectedIndex].DevicePrivateIP;

            int lcdIndependenceResult = 0;
            if (_localList.DeviceList[divoomList.SelectedIndex].Hardware == TIME_GATE_HARDWARE_ID)
            {
                string urlInfo = $"{BASE_API_URL}{LCD_INFO_ENDPOINT}?DeviceType=LCD&DeviceId={_localList.DeviceList[divoomList.SelectedIndex].DeviceId}";
                string independenceStr = HttpGet(urlInfo);
                
                if (!string.IsNullOrEmpty(independenceStr))
                {
                    var independenceInfo = JsonConvert.DeserializeObject<DivoomTimeGateIndependenceInfo>(independenceStr);
                    lcdIndependenceResult = independenceInfo.LcdIndependence;
                }
                
                LCDMsg.Visible = true;
                LCDList.Visible = true;
            }
            else
            {
                LCDMsg.Visible = false;
                LCDList.Visible = false;
            }

            var postInfo = new DivoomDeviceSelectClockInfo
            {
                LcdIndependence = lcdIndependenceResult,
                Command = SET_CLOCK_SELECT_COMMAND,
                LcdIndex = LCDList.SelectedIndex,
                ClockId = DEFAULT_CLOCK_ID
            };

            string postInfoRequest = JsonConvert.SerializeObject(postInfo);
            HttpPost($"{HTTP_PROTOCOL}{_deviceIpAddr}{HTTP_PORT}{POST_ENDPOINT}", postInfoRequest, out _);
        }

        /// <summary>
        /// Handler for the selected Divoom device change
        /// </summary>
        private void DivoomList_SelectedIndexChanged(object sender, EventArgs e)
        {
            DivoomSendSelectClock();
        }

        /// <summary>
        /// Handler for the selected LCD screen change
        /// </summary>
        private void LCDList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string rawValue = LCDList.SelectedItems[0].ToString();
            _selectLcdId = Convert.ToInt32(rawValue) - 1;

            if (_localList?.DeviceList != null && _localList.DeviceList.Any())
            {
                if (divoomList.SelectedIndex > 0 && divoomList.SelectedIndex < _localList.DeviceList.Count())
                {
                    DivoomSendSelectClock();
                }
            }
        }

        /// <summary>
        /// Handler for the process exit event
        /// </summary>
        public void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("exit");
        }
    }
}
