using OpenHardwareMonitor.Hardware;

namespace DivoomPCMontiorTool
{
    public class DivoomDeviceInfo
    {
        public int DeviceId { get; set; }

        public int Hardware { get; set; }

        public string DeviceName { get; set; }
        public string DevicePrivateIP { get; set; }
        public string DeviceMac { get; set; }

    }
    public class DivoomDeviceList
    {

        public DivoomDeviceInfo[] DeviceList { get; set; }

    }
    partial class DivoomPCMontiorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox divoomList;
        private System.Windows.Forms.ListBox LCDList;
        private System.Windows.Forms.Button refreshList;
        private System.Windows.Forms.TextBox CpuUse;
        private System.Windows.Forms.TextBox CpuTemp;
        private System.Windows.Forms.TextBox GpuUse;
        private System.Windows.Forms.TextBox GpuTemp;
        private System.Windows.Forms.TextBox DispUse;
        private System.Windows.Forms.TextBox HddUse;
        private System.Windows.Forms.Label LCDMsg;
        private System.Windows.Forms.Label DeviceListMsg;
        private System.Windows.Forms.Label HardwareInfo;
        private DivoomDeviceList LocalList;
        private int SelectLCDID;
        private System.Windows.Forms.Timer timer;
        private string DeviceIPAddr;
        private int LcdIndependence;

        UpdateVisitor UpdateVisitor;
        Computer Computer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.refreshList = new System.Windows.Forms.Button();
            this.CpuUse = new System.Windows.Forms.TextBox();
            this.CpuTemp = new System.Windows.Forms.TextBox();
            this.GpuUse = new System.Windows.Forms.TextBox();
            this.GpuTemp = new System.Windows.Forms.TextBox();
            this.DispUse = new System.Windows.Forms.TextBox();
            this.HddUse = new System.Windows.Forms.TextBox();
            this.divoomList = new System.Windows.Forms.ListBox();
            this.LCDList = new System.Windows.Forms.ListBox();
            this.LCDMsg = new System.Windows.Forms.Label();
            this.DeviceListMsg = new System.Windows.Forms.Label();
            this.HardwareInfo = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // refreshList
            // 
            this.refreshList.Location = new System.Drawing.Point(206, 288);
            this.refreshList.Name = "refreshList";
            this.refreshList.Size = new System.Drawing.Size(100, 25);
            this.refreshList.TabIndex = 0;
            this.refreshList.Text = "Refresh";
            this.refreshList.UseVisualStyleBackColor = true;
            this.refreshList.Click += new System.EventHandler(this.RefreshList_Click);
            // 
            // CpuUse
            // 
            this.CpuUse.Location = new System.Drawing.Point(206, 32);
            this.CpuUse.Name = "CpuUse";
            this.CpuUse.Size = new System.Drawing.Size(100, 20);
            this.CpuUse.TabIndex = 1;
            // 
            // CpuTemp
            // 
            this.CpuTemp.Location = new System.Drawing.Point(206, 65);
            this.CpuTemp.Name = "CpuTemp";
            this.CpuTemp.Size = new System.Drawing.Size(100, 20);
            this.CpuTemp.TabIndex = 1;
            // 
            // GpuUse
            // 
            this.GpuUse.Location = new System.Drawing.Point(206, 98);
            this.GpuUse.Name = "GpuUse";
            this.GpuUse.Size = new System.Drawing.Size(100, 20);
            this.GpuUse.TabIndex = 1;
            // 
            // GpuTemp
            // 
            this.GpuTemp.Location = new System.Drawing.Point(206, 130);
            this.GpuTemp.Name = "GpuTemp";
            this.GpuTemp.Size = new System.Drawing.Size(100, 20);
            this.GpuTemp.TabIndex = 1;
            // 
            // DispUse
            // 
            this.DispUse.Location = new System.Drawing.Point(206, 162);
            this.DispUse.Name = "DispUse";
            this.DispUse.Size = new System.Drawing.Size(100, 20);
            this.DispUse.TabIndex = 1;
            // 
            // HddUse
            // 
            this.HddUse.Location = new System.Drawing.Point(206, 188);
            this.HddUse.Name = "HddUse";
            this.HddUse.Size = new System.Drawing.Size(100, 20);
            this.HddUse.TabIndex = 1;
            // 
            // divoomList
            // 
            this.divoomList.Location = new System.Drawing.Point(100, 32);
            this.divoomList.Name = "divoomList";
            this.divoomList.Size = new System.Drawing.Size(100, 238);
            this.divoomList.TabIndex = 2;
            this.divoomList.SelectedIndexChanged += new System.EventHandler(this.DivoomList_SelectedIndexChanged);
            // 
            // LCDList
            // 
            this.LCDList.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5"});
            this.LCDList.Location = new System.Drawing.Point(20, 32);
            this.LCDList.Name = "LCDList";
            this.LCDList.Size = new System.Drawing.Size(74, 238);
            this.LCDList.TabIndex = 2;
            this.LCDList.SelectedIndexChanged += new System.EventHandler(this.LCDList_SelectedIndexChanged);
            // 
            // LCDMsg
            // 
            this.LCDMsg.Location = new System.Drawing.Point(20, 11);
            this.LCDMsg.Name = "LCDMsg";
            this.LCDMsg.Size = new System.Drawing.Size(74, 22);
            this.LCDMsg.TabIndex = 3;
            this.LCDMsg.Text = "Select LCD";
            // 
            // DeviceListMsg
            // 
            this.DeviceListMsg.Location = new System.Drawing.Point(100, 11);
            this.DeviceListMsg.Name = "DeviceListMsg";
            this.DeviceListMsg.Size = new System.Drawing.Size(100, 22);
            this.DeviceListMsg.TabIndex = 4;
            this.DeviceListMsg.Text = "Devices";
            // 
            // HardwareInfo
            // 
            this.HardwareInfo.Location = new System.Drawing.Point(206, 11);
            this.HardwareInfo.Name = "HardwareInfo";
            this.HardwareInfo.Size = new System.Drawing.Size(100, 22);
            this.HardwareInfo.TabIndex = 5;
            this.HardwareInfo.Text = "Hardware";
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 2000;
            this.timer.Tick += new System.EventHandler(this.DivoomSendHttpInfo);
            // 
            // DivoomPCMontiorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(319, 325);
            this.Controls.Add(this.CpuUse);
            this.Controls.Add(this.CpuTemp);
            this.Controls.Add(this.GpuUse);
            this.Controls.Add(this.GpuTemp);
            this.Controls.Add(this.DispUse);
            this.Controls.Add(this.HddUse);
            this.Controls.Add(this.refreshList);
            this.Controls.Add(this.divoomList);
            this.Controls.Add(this.LCDList);
            this.Controls.Add(this.LCDMsg);
            this.Controls.Add(this.DeviceListMsg);
            this.Controls.Add(this.HardwareInfo);
            this.Name = "DivoomPCMontiorForm";
            this.Text = "DivoomPcTool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}

