﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        private readonly string _baseUrl = "http://www.baidu.com";
        private readonly string _configPath = Path.Combine(Application.StartupPath, "config.xml");
        private readonly string _initialUrl = "http://10.22.115.123";
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

            AutoConnect(Convert.ToInt32(updnReconnect.Value));
        }

        private void AutoConnect(int interval)
        {
            var registry = new Registry();

            if (interval != 0)
            {
                registry.Schedule(() =>
                {
                    if (IsOffline()) AutoLogin();
                }).ToRunNow().AndEvery(interval).Minutes();
            }
            else
            {
                if (IsOffline()) AutoLogin();
            }
        }

        private void AutoLogin()
        {
            var url = txtUrl.Text.Trim();

            Process.Start(url);
            Thread.Sleep(3000);
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
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadString(_baseUrl);
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }

        private void LoadSettings()
        {
            var serializer = new XmlSerializer(typeof(Settings));

            StreamReader reader = null;
            try
            {
                if (File.Exists(_configPath))
                {
                    reader = new StreamReader(_configPath);
                    var settings = serializer.Deserialize(reader) as Settings;

                    if (settings != null)
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
                    txtUrl.Text = _initialUrl;
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
                if (!File.Exists(_configPath)) File.Create(_configPath);

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
            AutoConnect(Convert.ToInt32(updnReconnect.Value));
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

        private void trayIcon_Click(object sender, EventArgs e)
        {
        }

        private void frmMain_SizeChanged(object sender, EventArgs e)
        {
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
        }

        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) WindowState = FormWindowState.Normal;
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
    }
}