﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioSwitcher.AudioApi.CoreAudio;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Flow_Control.Scripts;
using Flow_Control.Properties;
using AATUV3.Scripts;
using UXTU.Scripts.Intel;
using Microsoft.Win32;
using System.Management;
using System.Windows.Threading;

namespace Flow_Control.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();
            string deviceName = MotherboardInfo.Product;
            deviceName = deviceName.Substring(0, deviceName.LastIndexOf('_'));
            deviceName = deviceName.Replace("ROG ", null);
            lblDeviceName.Text = deviceName;

            if (Settings.Default.PowerFix == true)
            {
                rdEnableFix.IsChecked = true;
                rdEnableFix.Tag = FindResource("enable");
                rdDisableFix.Tag = FindResource("disable");
            }
            else
            {
                rdEnableFix.Tag = FindResource("disable");
                rdDisableFix.Tag = FindResource("enable");
                rdDisableFix.IsChecked = true;
            }

            if (Settings.Default.Boot == true)
            {
                rdEnableBoot.IsChecked = true;
                rdEnableBoot.Tag = FindResource("enable");
                rdDisableBoot.Tag = FindResource("disable");
            }
            else
            {
                rdEnableBoot.Tag = FindResource("disable");
                rdDisableBoot.Tag = FindResource("enable");
                rdDisableBoot.IsChecked = true;
            }

            lblCPUName.Text = GetSystemInfo.GetCPUName().Replace("with Radeon Graphics", null);
            lbliGPUName.Text = GetSystemInfo.GetiGPUName().Replace("(R)", null);
            lbldGPUName.Text = GetSystemInfo.GetdGPUName().Replace("Laptop GPU", null);

            if (GetSystemInfo.GetdGPUName() == null || GetSystemInfo.GetdGPUName() == "")
            {
                dGPUName.Visibility = Visibility.Collapsed;
            }

            if (deviceName.Contains("Flow Z13")) PowerFix.Visibility = Visibility.Visible;
            else PowerFix.Visibility = Visibility.Collapsed;

            switchProfile(Settings.Default.ACProfile);

            if (Settings.Default.ACProfile == 0) rdSilent.IsChecked = true;
            else if (Settings.Default.ACProfile == 1) rdPerf.IsChecked = true;
            else if (Settings.Default.ACProfile == 2) rdTurbo.IsChecked = true;
            else if (Settings.Default.ACProfile == 3) rdMan.IsChecked = true;

            tbBatPercent.Value = Settings.Default.BatLimit;
            setBatteryLimit();

            GetSystemInfo.getBrightness();
            tbDisplayPercent.Value = Convert.ToUInt32(GetSystemInfo.brightness);

            BasicExeBackend.Garbage_Collect();

            lblCPUFan.Text = $"{CPUFanSpeed()} RPM";
            lblGPUFan.Text = $"{GPUFanSpeed()} RPM";

            //set up timer for sensor update
            DispatcherTimer sensor = new DispatcherTimer();
            sensor.Interval = TimeSpan.FromSeconds(2);
            sensor.Tick += SensorUpdate_Tick;
            sensor.Start();
        }

        void SensorUpdate_Tick(object sender, EventArgs e)
        {
            if (Menu1.Visibility == Visibility.Visible)
            {
                lblCPUFan.Text = $"{CPUFanSpeed()} RPM";
                lblGPUFan.Text = $"{GPUFanSpeed()} RPM";
            }
        }

        public static string GPUFanSpeed()
        {
            string gpuFan = RunCLI.RunCommand("Powershell.exe (Get-WmiObject -Namespace root/WMI -Class AsusAtkWmi_WMNB).DSTS(0x00110014)", true);
            var result = gpuFan.Split('\n');
            string output = result[12].Substring(result[12].IndexOf(':') + 2);
            int gpuSpeed = Convert.ToInt32(output);
            string hexValue = gpuSpeed.ToString("X");

            uint fanSpeed = Convert.ToUInt32(hexValue, 16);
            fanSpeed = (fanSpeed - 0x00010000) * 0x64;
            return fanSpeed.ToString();
        }

        public static string CPUFanSpeed()
        {
            string cpuFan = RunCLI.RunCommand("Powershell.exe (Get-WmiObject -Namespace root/WMI -Class AsusAtkWmi_WMNB).DSTS(0x00110013)", true);
            var result = cpuFan.Split('\n');
            string output = result[12].Substring(result[12].IndexOf(':') + 2);
            int cpuSpeed = Convert.ToInt32(output);
            string hexValue = cpuSpeed.ToString("X");

            uint fanSpeed = Convert.ToUInt32(hexValue, 16);
            fanSpeed = (fanSpeed - 0x00010000) * 0x64;
            return fanSpeed.ToString();
        }

        public async void switchProfile(int ACProfile)
        {
            if (ACProfile == 0)
            {
                imgACProfile.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ACProfiles/Silent.png"));
            }
            if (ACProfile == 1)
            {
                imgACProfile.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ACProfiles/Bal.png"));
            }
            if (ACProfile == 2)
            {
                imgACProfile.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ACProfiles/Turbo.png"));
            }
            if (ACProfile == 3)
            {
                imgACProfile.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ACProfiles/Windows.png"));
            }

            Settings.Default.ACProfile = ACProfile;
            Settings.Default.Save();

            await Task.Run(() => ApplySettings.AppleACSettings(ACProfile));
            await Task.Run(() => ApplyFix());
        }

        private void rdSilent_Click(object sender, RoutedEventArgs e)
        {
            switchProfile(0);
        }

        private void rdPerf_Click(object sender, RoutedEventArgs e)
        {
            switchProfile(1);
        }

        private void rdTurbo_Click(object sender, RoutedEventArgs e)
        {
            switchProfile(2);
        }

        private void rdMan_Click(object sender, RoutedEventArgs e)
        {
            switchProfile(3);
        }

        public static Guid DLAHI_GUID = new Guid("{5c4c3332-344d-483c-8739-259e934c9cc8}");
        public static string DLAHI_Instance = @"SWD\DRIVERENUM\OEM_DAL_COMPONENT&4&293F28F0&0";

        public static Guid DTTDE_GUID = new Guid("{5c4c3332-344d-483c-8739-259e934c9cc8}");
        public static string DTTDE_Instance = @"SWD\DRIVERENUM\{BC7814A1-A80E-44B3-87C6-652EAC676387}#DTTEXTCOMPONENT&4&DE2304&0";

        private void rdDisableFix_Click(object sender, RoutedEventArgs e)
        {
            rdEnableFix.Tag = FindResource("disable");
            rdDisableFix.Tag = FindResource("enable");
            Settings.Default.PowerFix = false;
            Settings.Default.Save();
        }

        private void rdEnableFix_Click(object sender, RoutedEventArgs e)
        {
            rdEnableFix.Tag = FindResource("enable");
            rdDisableFix.Tag = FindResource("disable");
            Settings.Default.PowerFix = true;
            Settings.Default.Save();
        }

        private void ApplyFix()
        {
            System.Threading.Thread.Sleep(25000);

            if (Settings.Default.PowerFix == true)
            {
                DeviceHelper.SetDeviceEnabled(DLAHI_GUID, DLAHI_Instance, true);
                DeviceHelper.SetDeviceEnabled(DTTDE_GUID, DTTDE_Instance, true);
            }
        }

        private void rdDisableBoot_Click(object sender, RoutedEventArgs e)
        {
            rdEnableBoot.Tag = FindResource("disable");
            rdDisableBoot.Tag = FindResource("enable");
            Settings.Default.Boot = false;
            Settings.Default.Save();

            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);
            key.DeleteValue("MyApplication", false);
        }

        private void rdEnableBoot_Click(object sender, RoutedEventArgs e)
        {
            rdEnableBoot.Tag = FindResource("enable");
            rdDisableBoot.Tag = FindResource("disable");
            Settings.Default.Boot = true;
            Settings.Default.Save();

            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);
            key.SetValue("MyApplication", System.Reflection.Assembly.GetExecutingAssembly().Location.ToString());
        }

        private async void tbDisplayPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblDisplayPercent.Text = ((int)tbDisplayPercent.Value).ToString() + "%";

            int newBirghtness = (int)tbDisplayPercent.Value;
            await Task.Run(() =>
            {
                var mclass = new ManagementClass("WmiMonitorBrightnessMethods")
                {
                    Scope = new ManagementScope(@"\\.\root\wmi")
                };
                var instances = mclass.GetInstances();
                var args = new object[] { 1, newBirghtness };
                foreach (ManagementObject instance in instances)
                {
                    instance.InvokeMethod("WmiSetBrightness", args);
                }
                BasicExeBackend.Garbage_Collect();
                return;
            });
        }

        private async void tbVolPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblVolPercent.Text = ((int)tbVolPercent.Value).ToString() + "%";
            int newVolume = (int)tbVolPercent.Value;
            await Task.Run(() =>
            {
                CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
                //Set volume of current sound device
                defaultPlaybackDevice.Volume = newVolume;
                BasicExeBackend.Garbage_Collect();
                return;
            });
        }

        private void tbBatPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblBatPercent.Text = ((int)tbBatPercent.Value).ToString() + "%";
        }

        private void tbBatPercent_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            setBatteryLimit();
        }

        private async void setBatteryLimit()
        {
            string value = ((int)tbBatPercent.Value).ToString();

            Settings.Default.BatLimit = (int)tbBatPercent.Value;
            Settings.Default.Save();

            if ((int)tbBatPercent.Value >= 50 && Convert.ToInt32(value) >= 50) await Task.Run(() => RunCLI.RunPowerShellCommand($"Powershell.exe (Get-WmiObject -Namespace root/WMI -Class AsusAtkWmi_WMNB).DEVS(0x00120057, {value})", false));
        }
    }
}
