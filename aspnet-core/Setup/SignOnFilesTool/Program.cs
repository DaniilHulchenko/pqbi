using CMDExecute;
using System;
using System.IO;

namespace SignOnFilesTool
{
    class Program
    {
        //private const string APP_FOLDER_PATH = PQSCADA_PATH + @"\ElspecScadaDevExpressDllCopyScript\bin\Release";

        static void Main(string[] args)
        {
            var currentPath = Directory.GetCurrentDirectory();

            string parentPath = FindParentDirectory("aspnet-core");
            string signOnDirectory = Path.Combine(parentPath, "sign");

            CMD writeExecute = new CMD();

            //SignFile(writeExecute, @"C:\PQSCADA\ForSign\PQS.Server.dll");

            bool signOnlyOnExe = true;
            if (signOnlyOnExe)
            {
                foreach (var exeFile in Directory.GetFiles(signOnDirectory, "*.exe", SearchOption.AllDirectories))
                {
                    SignFile(writeExecute, exeFile);
                }
            }
            else
            {
                string[] filesToSign = Directory.GetFiles(signOnDirectory, "*.*", SearchOption.AllDirectories);
                foreach (var fileToSign in filesToSign)
                {
                    SignFile(writeExecute, fileToSign);
                }
            }

            Console.WriteLine("Finished");
            Console.Read();
        }

        public static string FindParentDirectory(string searchForDirectory)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current != null && current.Name != searchForDirectory)
            {
                current = current.Parent;
            }

            if (current == null)
            {
                throw new DirectoryNotFoundException("Could not find 'aspnet-core' directory in parent hierarchy.");
            }


            return current.FullName;
        }

        public static void SignFile(CMD writeExecute, string filePath)
        {
            Console.WriteLine(writeExecute.WriteCommand("signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /sha1 bf3d924968448d310a69da83b3ec57a089f643cb " + "\"" + filePath + "\""));
        }
    }
}
