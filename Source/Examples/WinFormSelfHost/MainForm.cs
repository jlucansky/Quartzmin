using Quartz;
using Quartzmin;
using System;
using System.Windows.Forms;

namespace WinFormSelfHost
{
    public partial class MainForm : Form
    {
        IScheduler scheduler;

        public MainForm()
        {
            InitializeComponent();

            CreateScheduler();
        }

        void CreateScheduler()
        {
            scheduler = DemoScheduler.Create(start: false).Result;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            scheduler.Start();
            btnStop.Enabled = true;
            btnStart.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;

            scheduler.Shutdown();
            CreateScheduler();
        }

        private void link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(((LinkLabel)sender).Text);
        }
    }
}
