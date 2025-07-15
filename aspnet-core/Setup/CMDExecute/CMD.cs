using System;
using System.Diagnostics;
using System.Text;

namespace CMDExecute
{
    public class CMD
    {
        const string NEW_PROMPT = "--Prompt_end";

        private Process _proc;
        private ProcessStartInfo _procStartInfo;
        private StringBuilder errorStringBuilder;
        private bool isErrorExist;

        public CMD()
        {
            _procStartInfo = new System.Diagnostics.ProcessStartInfo();
            _procStartInfo.FileName = @"C:\WINDOWS\system32\cmd.exe";
            _procStartInfo.Arguments = "/k";

            _procStartInfo.RedirectStandardInput = true;
            _procStartInfo.RedirectStandardOutput = true;
            _procStartInfo.RedirectStandardError = true;
            _procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            _procStartInfo.CreateNoWindow = true;

            errorStringBuilder = new StringBuilder();
            // Now we create a process, assign its ProcessStartInfo and start it
            _proc = Process.Start(_procStartInfo);

            // replacing standard prompt in order to determine end of command output
            _proc.StandardInput.WriteLine("prompt " + NEW_PROMPT);
            _proc.StandardInput.Flush();

            _proc.ErrorDataReceived += new DataReceivedEventHandler(NetErrorDataHandler);
            _proc.BeginErrorReadLine();
            _proc.StandardOutput.ReadLine();
            _proc.StandardOutput.ReadLine();
        }

        public string WriteCommand(string cmdCommand)
        {
            isErrorExist = false;
            StringBuilder commandResult = new StringBuilder();
            try
            {
                // send command to its input
                _proc.StandardInput.WriteLine(cmdCommand);
                _proc.StandardInput.WriteLine();
                _proc.StandardInput.Flush();


                //_proc.WaitForExit();

                int linesCounter = 0;
                while (true)
                {
                    string line = _proc.StandardOutput.ReadLine();

                    if (line == NEW_PROMPT) // end of command output
                        break;

                    if (linesCounter != 0)
                        commandResult.AppendLine(line);
                    linesCounter++;
                    if (isErrorExist)
                    {
                        while (true)
                        {
                            line = _proc.StandardOutput.ReadLine();
                            if (line == NEW_PROMPT) // end of command output
                                break;
                        }

                        isErrorExist = false;
                        return errorStringBuilder.ToString();
                    }
                }
                return commandResult.ToString();

            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        private void NetErrorDataHandler(object sendingProcess,
    DataReceivedEventArgs errLine)
        {
            // Write the error text to the file if there is something
            // to write and an error file has been specified.

            errorStringBuilder.Clear();
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                isErrorExist = true;
                errorStringBuilder.AppendLine(errLine.Data);
            }
        }

        public void ExitProcess()
        {
            _proc.Close();
        }
    }
}
