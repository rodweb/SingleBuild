using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

//Rodrigo Campos
//rod.apd[at]gmail.com

namespace RC.SingleBuild
{

    /// <summary>
    /// Light object for trasfering basic file information to the FindAndExecute method.
    /// </summary>
    internal class FileInfo
    {
        public string fileName { get; set; }
        public string directory { get; set; }
    }

    class Program
    {
        #region >> CONSTANTS
        
        private const string BUILD_FAIL = "Build falhou!";
        private const string BUILD_SUCCEEDED = "Build OK!";
        private const string ELAPSED_TIME_FORMAT = "Tempo de execução: {0}ms";
        private const string CSPROJ_NOT_FOUND = "Arquivo .csproj não encontrado.";
        private const string PROJECT_FILE_FILTER = "*.csproj";
        private const string INVALID_DIRECTORY = "Diretório inválido.";
        private const string INVALID_ARGUMENT = "Argumento não informado.";
        private const string EMPTY_STRING = "";

        #endregion << CONSTANTS

        static void Main(string[] args)
        {
            ValidateArguments(args);

            var fullPath = args[0];

            var info = new FileInfo {
                fileName = GetFileNameFromPath(fullPath),
                directory = GetDirectoryFromPath(fullPath)
            };

            FindAndExecute(info);
        }

        #region >> PRIVATE STATIC METHODS

        /// <summary>
        /// Validate command-line arguments.
        /// </summary>
        /// <param name="args">Arguments passedin to the main program.</param>
        private static void ValidateArguments(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine(INVALID_ARGUMENT);
                FailExit();
            }

            var dirPath = GetDirectoryFromPath(args[0]);

            if (!Directory.Exists(dirPath))
            {
                Console.WriteLine(INVALID_DIRECTORY);
                FailExit();
            }            
        }

        /// <summary>
        /// Makes recursive lookups on parent directories fiels until a project file is found.
        /// </summary>
        /// <param name="info">Instance of Info class.</param>
        private static void FindAndExecute(FileInfo info)
        {
            var currentDir = info.directory;

            string[] projects = Directory.GetFiles(currentDir, PROJECT_FILE_FILTER);

            if (projects.Count() > 0)
            {
                if (info.fileName.Equals(string.Empty))
                {
                    ExecuteProcess(projects[0]);
                }
                else
                {
                    foreach (var project in projects)
                    {
                        if (isStringInFile(project, info.fileName))
                        {
                            ExecuteProcess(project);
                            break;
                        }
                    }
                }
                SuccessExit();
            }
            else if (!ParentDirectoryExists(currentDir))
            {
                Console.WriteLine(CSPROJ_NOT_FOUND);
                FailExit();
            }
            else
            {
                info.directory = Directory.GetParent(currentDir).FullName;
                FindAndExecute(info);
            }
        }

        /// <summary>
        /// Checks whether or not an parent directory exists.
        /// </summary>
        /// <param name="dir">The directory</param>
        /// <returns>True or False.</returns>
        private static bool ParentDirectoryExists(string dir)
        {
            return (Directory.GetParent(dir) != null);
        }

        /// <summary>
        /// Execute MSbuild on the specified file.
        /// </summary>
        /// <param name="projectFile">project filename to build.</param>
        private static void ExecuteProcess(string projectFile)
        {
            int exitCode = 0;

            string msbuildPath = GetMSbuildPath();

            ProcessStartInfo compilerInfo = GetMSbuildProcessInfo(msbuildPath, projectFile);

            Stopwatch stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                using (Process compiler = Process.Start(compilerInfo))
                {
                    Console.WriteLine(compiler.StandardError.ReadToEnd());

                    compiler.WaitForExit();

                    exitCode = compiler.ExitCode;
                }
            }
            catch (Exception)
            {
                FailExit();
            }
            finally
            {
                stopwatch.Stop();
            }

            if (exitCode == 0)
                Console.WriteLine(BUILD_SUCCEEDED);
            else
                Console.WriteLine(BUILD_FAIL);

            Console.WriteLine(ELAPSED_TIME_FORMAT, stopwatch.ElapsedMilliseconds.ToString());

            SuccessExit();
        }

        /// <summary>
        /// Get the MSbuild fullpath based on %SystemRoot%.
        /// </summary>
        /// <returns>MSbuild fullpath.</returns>
        private static string GetMSbuildPath()
        {
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");

            string msbuildPath = Path.Combine(systemRoot, @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe");

            if (!File.Exists(msbuildPath))
            {
                Console.WriteLine("MSbuild.exe não foi encontrado.");
                FailExit();
            }
            return msbuildPath;
        }

        /// <summary>
        /// Prepares the MSbuild execution.
        /// </summary>
        /// <param name="msbuildPath">MSbuild fullpath.</param>
        /// <param name="projectFile">project file fullpath.</param>
        /// <returns>MSbuild ProcessStartInfo instance.</returns>
        private static ProcessStartInfo GetMSbuildProcessInfo(string msbuildPath, string projectFile)
        {
            ProcessStartInfo compilerInfo = new ProcessStartInfo();
            compilerInfo.Arguments = String.Concat("/t:Build /nologo /clp:NoSummary;ErrorsOnly; /target:Compile /verbosity:quiet ", projectFile);
            compilerInfo.FileName = msbuildPath;
            compilerInfo.WindowStyle = ProcessWindowStyle.Hidden;
            compilerInfo.CreateNoWindow = true;
            compilerInfo.UseShellExecute = false;
            compilerInfo.RedirectStandardError = true;

            return compilerInfo;
        }

        #endregion << PRIVATE STATIC METHODS

        #region >> UTILITY   
        private static bool isStringInFile(string file, string text)
        {
            var found = false;
            foreach (var line in File.ReadLines(file))
            {
                if (line.Contains(text))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        private static string GetDirectoryFromPath(string fullPath)
        {
            return (File.Exists(fullPath) ? Path.GetDirectoryName(fullPath) : fullPath);
        }

        private static string GetFileNameFromPath(string fullPath)
        {
            return (File.Exists(fullPath) ? Path.GetFileName(fullPath) : string.Empty);
        }     

        private static void FailExit()
        {
            Environment.Exit(1);
        }

        private static void SuccessExit()
        {
            Environment.Exit(0);
        }
        #endregion << UTILITY
    }
}
