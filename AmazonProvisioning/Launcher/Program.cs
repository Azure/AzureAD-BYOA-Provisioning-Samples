//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Forms;
    using TemplateWizard;

    internal static class Program
    {
        private const string AgentExecutableName = "AmznAgnt.exe";

        private const string AgentBaseAddressDefault = "http://localhost:9000";
        
        private const string SeparatorArguments = " ";

        [STAThread]
        private static void Main(string[] arguments)
        {
            string credentialProfileName = null;
            if (!Program.TryGetCredentialProfileName(out credentialProfileName))
            {
                return;
            }

            List<string> agentArguments = new List<string>();
            agentArguments.Add(credentialProfileName);
            if (arguments != null && arguments.Any())
            {
                agentArguments.AddRange(arguments);
            }
            else
            {
                agentArguments.Add(Program.AgentBaseAddressDefault);
            }

            ProcessStartInfo processInformation = new ProcessStartInfo(Program.AgentExecutableName);
            processInformation.Arguments = string.Join(Program.SeparatorArguments, agentArguments);

            Process agentProcess = null;
            try
            {
                agentProcess = Process.Start(processInformation);
                agentProcess.WaitForExit();
            }
            finally
            {
                if (agentProcess != null)
                {
                    agentProcess.Close();
                    agentProcess = null;
                }
            }
        }

        private static bool TryGetCredentialProfileName(out string credentialProfileName)
        {
            credentialProfileName = null;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            UserInputForm profileManagementForm = new UserInputForm();
            Application.Run(profileManagementForm);

            if (null == profileManagementForm.SelectedAccount)
            {
                return false;
            }

            string credentialProfileNameBuffer = profileManagementForm.SelectedAccount.Name;
            if (string.IsNullOrWhiteSpace(credentialProfileNameBuffer))
            {
                return false;
            }

            credentialProfileName = credentialProfileNameBuffer;
            return true;
        }
    }
}
