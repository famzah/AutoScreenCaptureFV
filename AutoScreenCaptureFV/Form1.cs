using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private DateTime start_timer;

        public Form1()
        {
            InitializeComponent();

            String saved_path = RegistryHelper.GetSetting("Settings", "RootFolder", "");
            if (saved_path == "")
            {
                textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                textBox1.Text = saved_path;
            }

            showStatus("Stopped", false);
            numericUpDown1.Value = Convert.ToInt32(RegistryHelper.GetSetting("Settings", "Interval", "60"));
            numericUpDown2.Value = Convert.ToInt32(RegistryHelper.GetSetting("Settings", "Cleanup", "10080"));
        }

        public void HandleAutoStart() {
            string autoStartConf = RegistryHelper.GetSetting("Settings", "AutoStart", "0");
            checkBox1.Checked = (autoStartConf == "1");

            if (checkBox1.Checked)
            {
                button3_Click(null, null); // click the "Start" button
                ShowNotifyIcon();
                //this.Hide(); // the form is not visible by default
            }
            else
            {
                this.Show();
            }
        }

        private Bitmap Get_screen()
        {
            Size s = Screen.PrimaryScreen.Bounds.Size;
            Bitmap bt = new Bitmap(s.Width, s.Height);
            Graphics g = Graphics.FromImage(bt);
            g.CopyFromScreen(0, 0, 0, 0, s);
            return bt;
        }

        private void showStatus(String message, Boolean is_error)
        {
            toolStripStatusLabel1.Text = message;
            toolStripStatusLabel1.ForeColor = is_error ? Color.Red : Color.Black;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Bitmap bt;
            bt = Get_screen();
            pictureBox1.Image = bt;

            start_timer = DateTime.Now;

            String rootdir = textBox1.Text.Trim();
            if (rootdir.Length == 0)
            {
                showStatus("Snapshot not saved", true);
                return;
            }

            String timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"); // 2012-03-27-22-43-02
            String savefile = Path.Combine(rootdir, "screenshot-" + timestamp + ".jpg");

            if (File.Exists(savefile))
            {
                showStatus("Error, snapshot file already exists: " + savefile, true);
                return;
            }

            try
            {
                bt.Save(savefile, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                showStatus("Error saving snapshot file: " + savefile, true);
                return;
            }

            showStatus("Snapshot saved: " + savefile, false);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1_Tick(null, null);
            timer1.Interval = Convert.ToInt32(numericUpDown1.Value)*1000;
            start_timer = DateTime.Now;
            toolStripProgressBar1.Maximum = timer1.Interval / 1000; // in seconds
            toolStripProgressBar1.Minimum = 0;
            timer1.Enabled = true;
            timer2.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = false;
            numericUpDown1.Enabled = false;
            numericUpDown2.Enabled = false;
            textBox1.Enabled = false;
            button1.Enabled = false;
            RegistryHelper.SaveSetting("Settings", "Interval", numericUpDown1.Value.ToString());
            RegistryHelper.SaveSetting("Settings", "Cleanup", numericUpDown2.Value.ToString());
            RegistryHelper.SaveSetting("Settings", "RootFolder", textBox1.Text);
            RegistryHelper.SaveSetting("Settings", "AutoStart", checkBox1.Checked ? "1" : "0");
            button4.Enabled = true;
            timer3.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer3.Enabled = false;
            timer2.Enabled = false;
            toolStripProgressBar1.Value = 0;
            timer1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = true;
            numericUpDown1.Enabled = true;
            numericUpDown2.Enabled = true;
            textBox1.Enabled = true;
            button1.Enabled = true;
            button4.Enabled = false;
            showStatus("Stopped", false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBox1.Text;
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                toolStripProgressBar1.Value = (DateTime.Now - start_timer).Seconds;
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                // sometimes we overflow with a bit
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;

            string[] files = Directory.GetFiles(textBox1.Text.Trim());
            Array.Sort(files, (a, b) => String.Compare(b, a)); // NEWEST are first

            ArrayList imgfiles = new ArrayList();
            Regex re_imgfile = new Regex("screenshot-\\d{4}-\\d{2}-\\d{2}-\\d{2}-\\d{2}-\\d{2}\\.jpg");
            foreach (string full_name in files)
            {
                string short_name = Path.GetFileName(full_name);
                if (re_imgfile.IsMatch(short_name) == false)
                {
                    continue;
                }
                imgfiles.Add(full_name);
            }

            int keep_count = Convert.ToInt32(numericUpDown2.Value);
            if (keep_count > 0)
            {
                if (imgfiles.Count > keep_count)
                {
                    imgfiles.RemoveRange(0, keep_count);
                    foreach (string filename in imgfiles)
                    {
                        //File.Delete(filename);
                        FileSystem.DeleteFile(filename, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                }
            }

            button4.Enabled = true;
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            timer3.Enabled = false;
            button4_Click(null, null);
            if (timer1.Enabled == true)
            {
                timer3.Enabled = true;
            }
        }

        void ShowNotifyIcon()
        {
            notifyIcon1.BalloonTipText = "The capture is still working in background.";
            notifyIcon1.BalloonTipTitle = this.Text;
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(1000);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                ShowNotifyIcon();
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // if we received a FormClosing event and are still minimized
            // this means that we're probably forcibly closing the application
            // like when the Windows account is logged off, or when Windows
            // is being shut down
            if (!this.Visible)
            {
                return; // just close the application
            }

            DialogResult res = MessageBox.Show(
                    "You are about to end the program. Are you sure you want to continue?",
                    "Confirm close",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2
            );
            if (res == DialogResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }

    // http://www.codeproject.com/Articles/16274/Saving-Registry-Settings
    public class RegistryHelper
    {
        private static string FormRegKey(string sSect)
        {
            return sSect;
        }
        public static void SaveSetting(string Section, string Key, string Setting)
        {

            string text1 = FormRegKey(Section);
            RegistryKey key1 =

            Application.UserAppDataRegistry.CreateSubKey(text1);
            if (key1 == null)
            {
                return;
            }
            try
            {
                key1.SetValue(Key, Setting);
            }
            catch (Exception exception1)
            {
                return;
            }
            finally
            {
                key1.Close();
            }

        }
        public static string GetSetting(string Section, string Key, string Default)
        {
            if (Default == null)
            {
                Default = "";
            }
            string text2 = FormRegKey(Section);
            RegistryKey key1 = Application.UserAppDataRegistry.OpenSubKey(text2);
            if (key1 != null)
            {
                object obj1 = key1.GetValue(Key, Default);
                key1.Close();
                if (obj1 != null)
                {
                    if (!(obj1 is string))
                    {
                        return null;
                    }
                    return (string)obj1;
                }
                return null;
            }
            return Default;
        }
    }
}
