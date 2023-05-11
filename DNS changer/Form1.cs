using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNS_changer
{
    public partial class Form1 : Form
        
    {
        public void cmdrunner(string command)
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + command)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process proc = new Process())
            {
                proc.StartInfo = procStartInfo;
                proc.Start();

                string output = proc.StandardOutput.ReadToEnd();

                if (string.IsNullOrEmpty(output))
                    output = proc.StandardError.ReadToEnd();

            }
        }
        public string dnstocmd(string pri,string sec)
        {
            string cmnd = "";
            cmnd += "wmic nicconfig where (IPEnabled=TRUE) call SetDNSServerSearchOrder (\""+ pri+ "\", \"" + sec + "\")";
            return cmnd;
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //GOOGLE
            cmdrunner(dnstocmd("8.8.8.8","8.8.4.4"));
            MessageBox.Show("DNS Changed successfully !","DNS",MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //CloudFlare
            cmdrunner(dnstocmd("1.1.1.1", "1.0.0.1"));
            MessageBox.Show("DNS Changed successfully !", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Shecan
            cmdrunner(dnstocmd("178.22.122.100", "185.51.200.2"));
            MessageBox.Show("DNS Changed successfully !", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Neustar
            cmdrunner(dnstocmd("156.154.70.5", "156.154.71.5"));
            MessageBox.Show("DNS Changed successfully !", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            //OpenDNS
            cmdrunner(dnstocmd("208.67.222.222", "208.67.220.220"));
            MessageBox.Show("DNS Changed successfully !", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Safe
            cmdrunner(dnstocmd("195.46.39.39", "195.46.39.40"));
            MessageBox.Show("DNS Changed successfully !", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Auto_Click(object sender, EventArgs e)
        {
            //Auto
            cmdrunner("wmic nicconfig where (IPEnabled=TRUE) call SetDNSServerSearchOrder ()");
            MessageBox.Show("DNS Changed successfully !", "DNS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



        private static IPAddress GetDnsAdress()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

                    foreach (IPAddress dnsAdress in dnsAddresses)
                    {
                        return dnsAdress;
                    }
                }
            }

            throw new InvalidOperationException("Unable to find DNS Address");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            label13.Text = GetDnsAdress().ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                label13.Text = GetDnsAdress().ToString();
            }
            catch (Exception)
            { 
                label13.Text = "DISABLED NETWORK!";
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Phoenix15049/DNSchanger");
        }


    }
}
