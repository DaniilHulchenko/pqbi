using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.IO;

namespace StartService
{
    public class CustomActions
    {

        [CustomAction]
        public static ActionResult DeleteFiles(Session session)
        {
            string source = "Application";    
            try
            {              
                //System.Windows.Forms.MessageBox.Show(msiWindow, $"{"start"}", "Advanced Installer custom action");

                //if (!EventLog.SourceExists(source))
                //{
                //    EventLog.WriteEntry(source, $"event viewer source {source} not exist!", EventLogEntryType.Error);
                //    // Do not attempt to create new source, may fail without admin rights
                //    return ActionResult.Failure;
                //}


                // Read APPDIR from installer
                string appDir = session["APPDIR"];

                // Full path to your PowerShell script
                string deploymentPath = Path.Combine(appDir, "deployment");

                if (Directory.Exists(deploymentPath))
                    Directory.Delete(deploymentPath);

                return ActionResult.Success; // success
            }
            catch (Exception ex)
            {            
                EventLog.WriteEntry(source, $"Exception: {ex}", EventLogEntryType.Error);
                return ActionResult.Failure; // error
            }
        }       
    }
}
