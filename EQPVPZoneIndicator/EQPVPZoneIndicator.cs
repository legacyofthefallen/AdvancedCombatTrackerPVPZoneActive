using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

/*
* Project: EverQuest DPS Plugin
* Original: EverQuest 2 English DPS Localization plugin developed by EQAditu
* Description: Missing from the arsenal of the plugin based Advanced Combat Tracker to track EverQuest's current combat messages.  Ignores chat as that is displayed in game.
*/

namespace LotFPlugins
{
    public class EQPVPZoneIndicator : UserControl, IActPluginV1
    {
        #region Designer generated code (Avoid editing)
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                //watcherForDebugFile?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EQPVPZoneIndicator));
            this.pvpIndicator = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // pvpIndicator
            // 
            resources.ApplyResources(this.pvpIndicator, "pvpIndicator");
            this.pvpIndicator.Name = "pvpIndicator";
            // 
            // EQDPSParser
            // 
            this.Controls.Add(this.pvpIndicator);
            this.Name = "EQDPSParser";
            resources.ApplyResources(this, "$this");
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        #endregion

        #region Class Members       
        Regex pvpAreaEnter;
        #region UI Class Members
        TreeNode optionsNode = null;
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        #endregion
        private Label pvpIndicator;
        Action updateIndicator;

        #endregion

        /// <summary>
        /// Constructor that calls initialize component
        /// </summary>
        public EQPVPZoneIndicator()
        {
            InitializeComponent();
        }

        #region Plugin Class Interface Methods
        /// <summary>
        /// Called by the ACT program to start the plugin initialization
        /// Calls regex initialization methods and check for update methods
        /// assigns methods to the delegates in ActGlobals class
        /// </summary>
        /// <param name="pluginScreenSpace"></param>
        /// <param name="pluginStatusText"></param>
        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var

            //pluginScreenSpace.Controls.Add(this);
            this.Dock = DockStyle.Fill;

            foreach (TreeNode tn in ActGlobals.oFormActMain.OptionsTreeView.Nodes)
            {
                if (tn.Text.Equals("Data Correction"))
                {
                    Action optionsControlSetsAdd = () =>
                    {
                        optionsNode = tn.Nodes.Add($"{Properties.PluginRegex.pluginName}");
                        // Register our user control(this) to our newly create node path.  All controls added to the list will be laid out left to right, top to bottom
                        ActGlobals.oFormActMain.OptionsControlSets.Add($@"Data Correction\{Properties.PluginRegex.pluginName}",
                            new List<Control> { this });
                        Label lblConfig = new Label
                        {
                            AutoSize = true,
                            Text = "Find the applicable options in the Options tab, Data Correction section."
                        };
                        //Image img = new Bitmap(Properties.PluginRegex.logo);

                        //lblConfig.Image = img;
                        lblConfig.ImageAlign = ContentAlignment.MiddleLeft;
                        lblConfig.TextAlign = ContentAlignment.MiddleCenter;

                        pluginScreenSpace.Controls.Add(lblConfig);
                    };

                    if (ActGlobals.oFormActMain.InvokeRequired)
                    {
                        ActGlobals.oFormActMain.Invoke(optionsControlSetsAdd);
                    }
                    else
                    {
                        optionsControlSetsAdd.Invoke();
                    }
                    break;
                }
            }
            updateIndicator = new Action(() =>
            {
                this.pvpIndicator.BackColor = Color.Red;
                pvpIndicator.Text = "PVP ZONE";
            });

            pvpAreaEnter = new Regex(Properties.PluginRegex.PVPAreaEnter, RegexOptions.Compiled);
            ChangePluginStatusLabel($"{Properties.PluginRegex.pluginName} {Properties.PluginRegex.pluginStarted}");
            SetEventsForParsing();
        }

        private void SetEventsForParsing()
        {
            Action SetEvents = new Action(() =>
            {
                ActGlobals.oFormActMain.GetDateTimeFromLog += new FormActMain.DateTimeLogParser(ParseEQTimeStampFromLog);
                ActGlobals.oFormActMain.OnLogLineRead += new LogLineEventDelegate(FormActMain_LogLineRead);
            });
            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.Invoke(SetEvents);
            }
            else
            {
                SetEvents.Invoke();
            }
        }

        /// <summary>
        /// Removes methods from the delegates assigned during initialization
        /// attemps to save the settings and then update the plugin dock with status of the exit
        /// </summary>
        public void DeInitPlugin()
        {
            Action removeOptionsFromMainForm = () =>
            {
                if (!(optionsNode == null))    // If we added our user control to the Options tab, remove it
                {
                    optionsNode.Remove();
                    ActGlobals.oFormActMain.OptionsControlSets.Remove($@"Data Correction\{Properties.PluginRegex.pluginName}");
                }
            };

            Action removeEventsFromFormMain = () =>
            {
                ActGlobals.oFormActMain.GetDateTimeFromLog -= ParseEQTimeStampFromLog;
                ActGlobals.oFormActMain.BeforeLogLineRead -= FormActMain_LogLineRead;
            };

            void runDeInitActions()
            {
                ActGlobals.oFormActMain.Invoke(removeOptionsFromMainForm);
                ActGlobals.oFormActMain.Invoke(removeEventsFromFormMain);
            }

            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                runDeInitActions();
            }
            else
            {
                removeOptionsFromMainForm.Invoke();
                removeEventsFromFormMain.Invoke();
            }
            ChangePluginStatusLabel($"{Properties.PluginRegex.pluginName} {Properties.PluginRegex.pluginExited}");
        }
        #endregion

        #region Parsing Eventers
        /// <summary>
        /// Attemps to parse previous logline if is exists in parse
        /// </summary>
        /// <param name="isImport"></param>
        /// <param name="logInfo"></param>
        private void FormActMain_LogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (pvpAreaEnter.Match(logInfo.logLine).Success)
            {
                if(ActGlobals.oFormActMain.InvokeRequired)
                {
                    ActGlobals.oFormActMain.Invoke(updateIndicator);
                }
                else
                {
                    updateIndicator.Invoke();
                }
            }
        }

        #endregion


        /// <summary>
        /// updates the status label with thread safety based on whether the plugin needs to invoke the codes in separate thread to update the user interface control
        /// </summary>
        /// <param name="status"></param>
        private void ChangePluginStatusLabel(String status)
        {
            ChangeTextInControl(lblStatus, status);
        }

        private void ChangeTextInControl(Control control, String text)
        {
            switch (control.InvokeRequired)
            {
                case true:
                    control.Invoke(new Action(() =>
                    {
                        control.Text = text;
                    }));
                    break;
                case false:
                    control.Text = text;
                    break;
                default:
                    break;
            }
        }

        #region String Parsing
        /// <summary>
        /// Parsess the date and time based on the EverQuest character log time stamp format
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns>DateTime</returns>
        internal DateTime ParseEQTimeStampFromLog(String timeStamp)
        {
            DateTime.TryParseExact(timeStamp, Properties.PluginRegex.eqDateTimeStampFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out DateTime currentEQTimeStamp);
            return currentEQTimeStamp;
        }

        #endregion
    }
}
