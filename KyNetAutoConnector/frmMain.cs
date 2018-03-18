using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using WindowsInput;
using WindowsInput.Native;
using AutostartManagement;
using FluentScheduler;

namespace KyNetAutoConnector
{
    public partial class frmMain : Form
    {
        private readonly string _configPath = Path.Combine(Application.StartupPath, "config.xml");
        private readonly InputSimulator _inputSimulator = new InputSimulator();


        public frmMain()
        {
            InitializeComponent();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (!IsOffline())
            {
                MessageBox.Show("当前已联网，不进行自动连接", "注意", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            AutoConnect();
        }

        private void AutoConnect()
        {
            if (IsOffline())
            {
                if (AutoClosingMessageBox.Show("10 秒后开始自动连接，是否需要取消操作？", "问题", 5000, MessageBoxButtons.YesNo,
                        DialogResult.No) == DialogResult.No)
                {
                    AutoLogin();
                }
            }
        }

        private void AutoLogin()
        {
            var url = txtUrl.Text.Trim();

            Process.Start(url);
            Thread.Sleep(5000);
            InputTab();
            InputData(txtUsername.Text.Trim());
            InputTab();
            InputData(txtPassword.Text.Trim());
            InputEnter();
            Thread.Sleep(3000);
            CloseBrowser();
        }

        private void InputData(string data)
        {
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A); //全选当前输入框
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DELETE);
            _inputSimulator.Keyboard.TextEntry(data);
            _inputSimulator.Keyboard.Sleep(50);
        }

        private void InputTab()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            _inputSimulator.Keyboard.Sleep(50);
        }

        private void InputEnter()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            _inputSimulator.Keyboard.Sleep(50);
        }

        private void CloseBrowser()
        {
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_W);
            _inputSimulator.Keyboard.Sleep(50);
        }

        private bool IsOffline()
        {
            const string baseHost = "www.baidu.com";

            var ping = new Ping();

            try
            {
                var reply = ping.Send(baseHost, 1000);
                if (reply != null && reply.Status == IPStatus.Success) return false;
            }
            catch
            {
                // ignored
            }

            return true;
        }

        private void LoadSettings()
        {
            var serializer = new XmlSerializer(typeof(Settings));
            const string initialUrl = "http://10.22.115.123";
            StreamReader reader = null;

            try
            {
                if (File.Exists(_configPath))
                {
                    reader = new StreamReader(_configPath);

                    if (serializer.Deserialize(reader) is Settings settings)
                    {
                        txtUrl.Text = settings.Url;
                        txtUsername.Text = settings.Username;
                        txtPassword.Text = settings.Password;
                        chkRunWhenStartup.Checked = settings.RunWhenStartup;
                        updnReconnect.Value = settings.AutoReconnectInterval;
                    }
                }
                else
                {
                    txtUrl.Text = initialUrl;
                    SaveSettigns();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }

        private void SaveSettigns()
        {
            var settings = new Settings
            {
                Url = txtUrl.Text.Trim(),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text.Trim(),
                RunWhenStartup = chkRunWhenStartup.Checked,
                AutoReconnectInterval = Convert.ToInt32(updnReconnect.Value)
            };

            var serializer = new XmlSerializer(typeof(Settings));
            StreamWriter writer = null;

            try
            {
                writer = new StreamWriter(_configPath);
                serializer.Serialize(writer, settings);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                writer?.Close();
            }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettigns();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            LoadSettings();

            SetAutoConnectSchedule(Convert.ToInt32(updnReconnect.Value));
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show(this, "确定退出程序吗?", "问题", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) == DialogResult.No)
                e.Cancel = true;
        }

        private void chkRunWhenStartup_CheckedChanged(object sender, EventArgs e)
        {
            var autostartEnabled = chkRunWhenStartup.Checked;
            const bool registerShortcutForAllUser = false;
            var autostartManager = new AutostartManager(Application.ProductName, Application.ExecutablePath,
                registerShortcutForAllUser);

            if (autostartEnabled)
            {
                if (!autostartManager.IsAutostartEnabled()) autostartManager.EnableAutostart();
            }
            else
            {
                if (autostartManager.IsAutostartEnabled()) autostartManager.DisableAutostart();
            }
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void toolStripMenuItemShow_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        private void frmMain_SizeChanged(object sender, EventArgs e)
        {
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
        }

        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) WindowState = FormWindowState.Normal;
        }

        private void lnkHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var helpUrl = "https://github.com/ningboliuwei/KyNetAutoConnector/blob/master/README.md";

            Process.Start(helpUrl);
        }

        private void lnkOpenStartupFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var userStartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            Process.Start(userStartupFolder);
        }

        private void updnReconnect_ValueChanged(object sender, EventArgs e)
        {
            SetAutoConnectSchedule(Convert.ToInt32(updnReconnect.Value));
        }

        private void SetAutoConnectSchedule(int interval)
        {
            JobManager.RemoveAllJobs();

            if (interval != 0)
                JobManager.AddJob(AutoConnect, s => s.ToRunEvery(interval).Minutes());
            else
                JobManager.Stop();
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

        private void toolStripMenuItemConnect_Click(object sender, EventArgs e)
        {
            btnRun_Click(null, null);
        }
    }
}