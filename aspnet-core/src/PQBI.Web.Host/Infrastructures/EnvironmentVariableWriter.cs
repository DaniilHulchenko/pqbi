using System.Runtime.InteropServices;

namespace PQBI.Web.Infrastructures
{
    /// <summary>
    /// The following class main purpose is to log in to a separate file with all the variables which are used in docker and their values, in case a value is not used, a proper indication will be printed
    /// </summary>
    public class EnvironmentVariableWriter
    {
        const string VARIABLE_NOT_USED = "Variable Not Used";
        private readonly Dictionary<string, string> _variables;
        private readonly FileInfo _filePath;

        public EnvironmentVariableWriter()
        {
            _variables = new Dictionary<string, string> {
                { nameof(PQBI_LOG_DIR), VARIABLE_NOT_USED } ,
                { nameof(SEQ_HOST_URL), VARIABLE_NOT_USED } ,
                { nameof(PQBI_RUN_IN_CONTAINER), VARIABLE_NOT_USED } ,
            };

            //var assemblyPath = typeof(EnvironmentVariableWriter).Assembly.Location;

            //// // Get the bin directory by removing the assembly file name
            ////string binDirectory = Path.GetDirectoryName(assemblyPath);
            //var path = Path.Combine("Logs","environments.txt");


            //_filePath = new FileInfo(path);

            //RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }


        public string PQBI_LOG_DIR => GetVariableValueOrNull(nameof(PQBI_LOG_DIR));

        public string SEQ_HOST_URL => GetVariableValueOrNull(nameof(SEQ_HOST_URL));
        //public string BUILDER_REFERER => GetVariableValueOrNull(nameof(BUILDER_REFERER));

        public string PQBI_RUN_IN_CONTAINER => GetVariableValueOrNull(nameof(PQBI_RUN_IN_CONTAINER));


        //public void WriteAllVaribles()
        //{
        //    using (StreamWriter writer = new StreamWriter(_filePath.FullName))
        //    {
        //        foreach (var variable in _variables)
        //        {
        //            writer.WriteLine($"{variable.Key} = {variable.Value}");
        //        }
        //    }
        //}


        private string GetVariableValueOrNull(string variableName)
        {
            string enviromentVarible = null;
            if (_variables.TryGetValue(variableName, out enviromentVarible) && enviromentVarible != null && enviromentVarible.Equals(VARIABLE_NOT_USED, StringComparison.OrdinalIgnoreCase))
            {
                enviromentVarible = _variables[variableName] = Environment.GetEnvironmentVariable(variableName) ?? null;
            }

            return enviromentVarible;
        }
    }
}
