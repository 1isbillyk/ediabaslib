﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PsdzClient.Programming;

namespace PsdzClient
{
    public partial class FormMain : Form
    {
        private ProgrammingService programmingService;
        private bool taskActive = false;

        public FormMain()
        {
            InitializeComponent();
        }

        private void UpdateDisplay()
        {
            bool hostRunning = programmingService != null && programmingService.IsPsdzPsdzServiceHostInitialized();
            buttonStartHost.Enabled = !taskActive && !hostRunning;
            buttonStopHost.Enabled = !taskActive && hostRunning;
            buttonClose.Enabled = !taskActive && !hostRunning;
            buttonAbort.Enabled = taskActive;
        }

        private bool LoadSettings()
        {
            try
            {
                textBoxIstaFolder.Text = Properties.Settings.Default.IstaFolder;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool StoreSettings()
        {
            try
            {
                Properties.Settings.Default.IstaFolder = textBoxIstaFolder.Text;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void UpdateStatus(string message = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    UpdateStatus(message);
                }));
                return;
            }

            textBoxStatus.Text = message ?? string.Empty;
            textBoxStatus.SelectionStart = textBoxStatus.TextLength;
            textBoxStatus.Update();
            textBoxStatus.ScrollToCaret();

            UpdateDisplay();
        }

        private async Task<bool> StartProgrammingServiceTask(string dealerId)
        {
            return await Task.Run(() => StartProgrammingService(dealerId)).ConfigureAwait(false);
        }

        private bool StartProgrammingService(string dealerId)
        {
            try
            {
                if (!StopProgrammingService())
                {
                    return false;
                }
                programmingService = new ProgrammingService(textBoxIstaFolder.Text, dealerId);
                programmingService.StartPsdzServiceHost();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private async Task<bool> StopProgrammingServiceTask()
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => StopProgrammingService()).ConfigureAwait(false);
        }

        private bool StopProgrammingService()
        {
            try
            {
                if (programmingService != null)
                {
                    programmingService.Psdz.Shutdown();
                    programmingService.CloseConnectionsToPsdzHost();
                    programmingService.Dispose();
                    programmingService = null;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonAbort_Click(object sender, EventArgs e)
        {

        }

        private void buttonIstaFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialogIsta.SelectedPath = textBoxIstaFolder.Text;
            DialogResult result = folderBrowserDialogIsta.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxIstaFolder.Text = folderBrowserDialogIsta.SelectedPath;
                UpdateDisplay();
            }
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            StringBuilder sbMessage = new StringBuilder();
            UpdateStatus(sbMessage.ToString());
            if (!StopProgrammingServiceTask().Wait(10000))
            {
                sbMessage.AppendLine("Host stop failed");
                UpdateStatus(sbMessage.ToString());
            }

            UpdateDisplay();
            StoreSettings();
            timerUpdate.Enabled = false;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            LoadSettings();
            UpdateDisplay();
            UpdateStatus();
            timerUpdate.Enabled = true;
        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void buttonStartHost_Click(object sender, EventArgs e)
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Starting host ...");
            UpdateStatus(sbMessage.ToString());

            StartProgrammingServiceTask("32395").ContinueWith(task =>
            {
                taskActive = false;
                if (task.Result)
                {
                    sbMessage.AppendLine("Host started");
                }
                else
                {
                    sbMessage.AppendLine("Host start failed");
                }
                UpdateStatus(sbMessage.ToString());
            });

            taskActive = true;
            UpdateDisplay();
        }

        private void buttonStopHost_Click(object sender, EventArgs e)
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.AppendLine("Stopping host ...");
            UpdateStatus(sbMessage.ToString());

            StopProgrammingServiceTask().ContinueWith(task =>
            {
                taskActive = false;
                if (task.Result)
                {
                    sbMessage.AppendLine("Host stopped");
                }
                else
                {
                    sbMessage.AppendLine("Host stop failed");
                }
                UpdateStatus(sbMessage.ToString());
            });

            taskActive = true;
            UpdateDisplay();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool active = taskActive;
            if (programmingService != null && programmingService.IsPsdzPsdzServiceHostInitialized())
            {
                active = true;
            }

            if (active)
            {
                e.Cancel = true;
            }
        }
    }
}
