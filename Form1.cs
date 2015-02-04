#region

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PVPNetConnect;

#endregion

namespace LoL_Account_Checker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            regionsComboBox.SelectedIndex = 0;
        }

        private void InputFileTextBoxDClick(object sender, MouseEventArgs e)
        {
            var ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                inputFileTextBox.Text = ofd.FileName;
            }
        }

        private void OutputFileTextBoxDClick(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog { FileName = "output.txt" };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                outputFileTextBox.Text = sfd.FileName;
            }
        }

        private void CheckAcccountsClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(inputFileTextBox.Text))
            {
                MessageBox.Show("Account's file missing.");
                return;
            }

            if (string.IsNullOrEmpty(outputFileTextBox.Text))
            {
                MessageBox.Show("Output file missing.");
                return;
            }


            if (!File.Exists(inputFileTextBox.Text))
            {
                MessageBox.Show("Account's file does not exist.");
                return;
            }

            button1.Enabled = false;

            var region = (Region) regionsComboBox.SelectedIndex;


            var bw = new BackgroundWorker { WorkerReportsProgress = true };

            bw.DoWork += (o, args) =>
            {
                try
                {
                    var b = o as BackgroundWorker;

                    var sr = new StreamReader(inputFileTextBox.Text);
                    var sw = new StreamWriter(outputFileTextBox.Text);

                    var totalLines = sr.ReadToEnd().Split(new[] { '\n' }).Count();

                    var lineCount = 0;

                    sr.DiscardBufferedData();
                    sr.BaseStream.Seek(0, SeekOrigin.Begin);
                    sr.BaseStream.Position = 0;

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var accountData = line.Split(new[] { ':' });

                        if (accountData.Count() < 2)
                        {
                            continue;
                        }

                        var username = accountData[0];
                        var password = accountData[1];


                        var completed = false;
                        var result = Client.Result.Error;

                        var client = new Client(region, username, password);

                        client.OnReport += (sender1, r) =>
                        {
                            completed = true;
                            result = r;
                        };

                        var start = DateTime.Now;
                        while (true)
                        {
                            if (start.AddSeconds(1) > DateTime.Now)
                                continue;
                            
                            if (client.Completed)
                                break;


                            start = DateTime.Now;
                            // wait
                        }

                        if (result == Client.Result.Success)
                        {
                            sw.WriteLine(
                                "Account: {0} | Password: {1} | Summoner Name: {2} | Level: {3} | RP: {4} | IP: {5} | Champions: {6} | Skins: {7} | Rune Pages: {8}",
                                client.Data.Username, client.Data.Password, client.Data.SummonerName, client.Data.Level,
                                client.Data.RpBalance, client.Data.Ipbalance, client.Data.Champions, client.Data.Skins,
                                client.Data.RunePages);

                            client.Disconnect();
                        }
                        else
                        {
                            sw.WriteLine(
                                "Account: {0} | Password: {1} | Error: {2}", client.Data.Username, client.Data.Password,
                                client.ErrorMessage);
                        }

                        Console.WriteLine("[{0:HH:mm}] <{1}> Completed!", DateTime.Now, client.Data.Username);

                        lineCount++;

                        b.ReportProgress((lineCount * 100) / totalLines);
                    }

                    sw.Close();
                    sr.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            bw.ProgressChanged += (o, args) => { toolStripProgressBar1.Value = args.ProgressPercentage; };

            bw.RunWorkerCompleted += (o, args) =>
            {
                button1.Enabled = true;

                if (MessageBox.Show("Finished!\nWanna see the results?", ":^)", MessageBoxButtons.YesNo) ==
                    DialogResult.Yes)
                {
                    Process.Start(outputFileTextBox.Text);
                }
            };

            bw.RunWorkerAsync();
        }
    }
}