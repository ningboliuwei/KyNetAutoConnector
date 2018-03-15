using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace KyNetAutoConnector
{
    public partial class frmMain : Form
    {
        private InputSimulator _inputSimulator = new InputSimulator();
        private string _configPath = Path.Combine(Application.ExecutablePath, "config.xml");

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (IsOffline())
            {
                AutoLogin();
            }
        }

        public void AutoLogin()
        {
            var url = txtUrl.Text.Trim();

            System.Diagnostics.Process.Start(url);
            Thread.Sleep(3000);
            InputTab();
            InputData(txtUsername.Text.Trim());
            InputTab();
            InputData(txtPassword.Text.Trim());
            InputEnter();
            Thread.Sleep(3000);
            CloseBrowser();
        }

        public void InputData(string data)
        {
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A); //全选当前输入框
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DELETE);
            _inputSimulator.Keyboard.TextEntry(data);
            _inputSimulator.Keyboard.Sleep(50);
        }

        public void InputTab()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            _inputSimulator.Keyboard.Sleep(50);
        }

        public void InputEnter()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            _inputSimulator.Keyboard.Sleep(50);
        }

        public void CloseBrowser()
        {
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_W);
            _inputSimulator.Keyboard.Sleep(50);
        }

        public bool IsOffline()
        {
            var ping = new Ping();
            var reply = ping.Send("www.baidu.com", 1000);//百度IP
            if (reply != null && reply.Status == IPStatus.Success)
            {
                return false;
            }

            return true;
        }

        public void LoadSettings()
        {

        }

        public void SaveSettigns()
        {
            var settings = new Settings()
            {
                Url = txtUrl.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text.Trim(),
                RunWhenStartup = chkRunWhenStartup.Checked,
                AutoReconnectInterval = Convert.ToInt32(updnReconnect.Value)
            };

            var serializer = new XmlSerializer(typeof(Settings));
            var writer = new StreamWriter(_configPath);
            serializer.Serialize(writer, settings);
        }

        [Serializable]
        public class Settings
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool RunWhenStartup { get; set; }
            public int AutoReconnectInterval { get; set; }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettigns();
        }
    }
}
