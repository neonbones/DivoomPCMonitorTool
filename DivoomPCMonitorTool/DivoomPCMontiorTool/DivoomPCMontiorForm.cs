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
        public DivoomPCMontiorForm()
        {
            InitializeComponent();

            UpdateVisitor = new UpdateVisitor();
            Computer = new Computer
            {
                HDDEnabled = false
            };
            Computer.Open();

            LCDMsg.Visible = false;
            LCDList.Visible = false;
            DivoomUpdateDeviceList();
        }
        public static int HttpPost(string url, string sendData, out string reslut)
        {
            reslut = "";

            try
            {
                var data = Encoding.UTF8.GetBytes(sendData);
                HttpWebRequest wbRequest = (HttpWebRequest)WebRequest.Create(url);
                wbRequest.Proxy = null;
                wbRequest.Method = "POST";
                wbRequest.ContentType = "application/json";
                wbRequest.ContentLength = data.Length;
                wbRequest.Timeout = 1000;

                using (Stream wStream = wbRequest.GetRequestStream())
                {
                    wStream.Write(data, 0, data.Length);
                }

                var wbResponse = (HttpWebResponse)wbRequest.GetResponse();
                using (Stream responseStream = wbResponse.GetResponseStream())
                {
                    using (StreamReader sReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        reslut = sReader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                reslut = e.Message;
                return -1;
            }
            return 0;
        }

        public static string HttpPost2(string url, string postDataStr)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            Encoding encoding = Encoding.UTF8;
            byte[] postData = encoding.GetBytes(postDataStr);
            request.ContentLength = postData.Length;
            Stream myRequestStream = request.GetRequestStream();
            myRequestStream.Write(postData, 0, postData.Length);
            myRequestStream.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, encoding);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public static string HttpGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        private void DivoomSendHttpInfo(object sender, EventArgs e)
        {
            try
            {
                Computer.Accept(UpdateVisitor);
            }
            catch
            {
                return;
            }

            if (DeviceIPAddr == null || LocalList == null || LocalList.DeviceList == null || LocalList.DeviceList.Length == 0)
            {
                return;

            }
            string CpuTemp_value = "--", CpuUse_value = "--", GpuTemp_value = "--", GpuUse_value = "--", HardDiskUse_value = "--";

            DivoomDevicePostList PostInfo = new DivoomDevicePostList();
            DivoomDevicePostItem PostItem = new DivoomDevicePostItem();
            PostInfo.Command = "Device/UpdatePCParaInfo";
            PostInfo.ScreenList = new DivoomDevicePostItem[1];
            PostItem.DispData = new string[6];

            if (DeviceIPAddr.Length > 0 && Computer != null)
            {
                PostItem.LcdId = SelectLCDID;
              
                for (int i = 0; i < Computer.Hardware.Length; i++)
                {
                    if (Computer.Hardware[i].HardwareType == HardwareType.CPU)
                    {
                        for (int j = 0; j < Computer.Hardware[i].Sensors.Length; j++)
                        {
 
                            if (Computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                            {
                                CpuTemp_value = Computer.Hardware[i].Sensors[j].Value.ToString();
                                CpuTemp_value += "C";
                            }
                            else if (Computer.Hardware[i].Sensors[j].SensorType == SensorType.Load)
                            {
                                CpuUse_value = Computer.Hardware[i].Sensors[j].Value.ToString();
                                if (CpuUse_value.Length > 2)
                                {
                                    CpuUse_value = CpuUse_value.Substring(0, 2);
                                }
                                CpuUse_value += "%";
                            }
                        }
                    }
                    else if (Computer.Hardware[i].HardwareType == HardwareType.GpuNvidia ||
                        Computer.Hardware[i].HardwareType == HardwareType.GpuAti)
                    {
                        for (int j = 0; j < Computer.Hardware[i].Sensors.Length; j++)
                        {
                            if (Computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                            {
                                GpuTemp_value = Computer.Hardware[i].Sensors[j].Value.ToString();
                                GpuTemp_value += "C";
                            }
                            else if (Computer.Hardware[i].Sensors[j].SensorType == SensorType.Load)
                            {
                                GpuUse_value = Computer.Hardware[i].Sensors[j].Value.ToString();
                                if (GpuUse_value.Length > 2)
                                {
                                    GpuUse_value = GpuUse_value.Substring(0, 2);
                                }
                                GpuUse_value += "%";
                            }
                        }
                    }
                    else if (Computer.Hardware[i].HardwareType == HardwareType.HDD)
                    {
                        for (int j = 0; j < Computer.Hardware[i].Sensors.Length; j++)
                        {
                            if (Computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                            {
                                HardDiskUse_value = Computer.Hardware[i].Sensors[j].Value.ToString();
                                HardDiskUse_value += "C";
                                break;
                            }
                        }
                    }
                }

                MEMORYSTATUSEX memInfo = new MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
                };

                GlobalMemoryStatusEx(ref memInfo);

                PostItem.DispData[2] = CpuTemp_value;
                PostItem.DispData[0] = CpuUse_value;
                PostItem.DispData[3] = GpuTemp_value;
                PostItem.DispData[1] = GpuUse_value;
                PostItem.DispData[5] = HardDiskUse_value;
                PostInfo.ScreenList[0] = PostItem;
                CpuTemp.Text = "CpuTemp:" + CpuTemp_value;
                CpuUse.Text = "CpuUse:" + CpuUse_value;
                GpuTemp.Text = "GpuTemp:" + GpuTemp_value;
                GpuUse.Text = "GpuUse:" + GpuUse_value;
                HddUse.Text = "HddUse:" + HardDiskUse_value;
        
                string para_info = JsonConvert.SerializeObject(PostInfo);

                HttpPost("http://" + DeviceIPAddr + ":80/post", para_info, out _);
            }
        }
        private void DivoomUpdateDeviceList()
        {
            int i;
            string urlInfo = "http://app.divoom-gz.com/Device/ReturnSameLANDevice";
            string device_list = HttpGet(urlInfo);
            LocalList = JsonConvert.DeserializeObject<DivoomDeviceList>(device_list);
            divoomList.Items.Clear();
            for (i = 0; LocalList.DeviceList != null && i < LocalList.DeviceList.Length; i++)
            {
                divoomList.Items.Add(LocalList.DeviceList[i].DeviceName);
            }

        }
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
        private void DivoomSendSelectClock()
        {
            DeviceIPAddr = LocalList.DeviceList[divoomList.SelectedIndex].DevicePrivateIP;

            if (LocalList.DeviceList[divoomList.SelectedIndex].Hardware == 400)
            {
                string url_info = "http://app.divoom-gz.com/Channel/Get5LcdInfoV2?DeviceType=LCD&DeviceId=" + LocalList.DeviceList[divoomList.SelectedIndex].DeviceId;
                string independenceStr = HttpGet(url_info);
                if (independenceStr != null && independenceStr.Length > 0)
                {
                    DivoomTimeGateIndependenceInfo IndependenceInfo = JsonConvert.DeserializeObject<DivoomTimeGateIndependenceInfo>(independenceStr);

                    LcdIndependence = IndependenceInfo.LcdIndependence;

                }
                LCDMsg.Visible = true;
                LCDList.Visible = true;

            }
            else
            {
                LCDMsg.Visible = false;
                LCDList.Visible = false;
            }

            DivoomDeviceSelectClockInfo PostInfo = new DivoomDeviceSelectClockInfo();

            PostInfo.LcdIndependence = LcdIndependence;
            PostInfo.Command = "Channel/SetClockSelectId";
            PostInfo.LcdIndex = this.LCDList.SelectedIndex;
            PostInfo.ClockId = 625;

            string postInfoRequest = JsonConvert.SerializeObject(PostInfo);
            HttpPost("http://" + DeviceIPAddr + ":80/post", postInfoRequest, out _);

        }
        private void DivoomList_SelectedIndexChanged(object sender, EventArgs e)
        {
            DivoomSendSelectClock();
        }

        private void LCDList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string raw_value = this.LCDList.SelectedItems[0].ToString();
            SelectLCDID = Convert.ToInt32(raw_value) - 1;

            if(LocalList != null && LocalList.DeviceList!=null && LocalList.DeviceList.Count() > 0)
            {
                if (divoomList.SelectedIndex > 0 && this.divoomList.SelectedIndex < LocalList.DeviceList.Count())
                {
                    DivoomSendSelectClock();
                }

            }


        }
        public void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("exit");
        }
    }
}
