using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

//Rodrigo Campos
//rod.apd[at]gmail.com

namespace SingleBuild
{

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
        private const string TIME_ELAPSED_FORMAT = "Tempo de execução: {0}ms";
        private const string CSPROJ_NOT_FOUND = "Arquivo .csproj não encontrado.";
        private const string CSPROJ_FILTER = "*.csproj";
        private const string INVALID_DIRECTORY = "Diretório inválido.";
        private const string INVALID_ARGUMENT = "Argumento não informado.";

        #endregion << CONSTANTS

        static void Main(string[] args)
        {
            CheckArguments(args);

            var fullPath = args[0];

            var info = new FileInfo {
                fileName = GetFileNameFromPath(fullPath),
                directory = GetDirectoryFromPath(fullPath)
            };

            FindAndExecute(info);
        } 

        private static void CheckArguments(string[] args)
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

        private static void FindAndExecute(FileInfo info)
        {
            var currentDir = info.directory;

            string[] projects = Directory.GetFiles(currentDir, CSPROJ_FILTER);

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
            else if (Path.Equals(Directory.GetParent(currentDir).FullName, Directory.GetDirectoryRoot(currentDir)))
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

        private static void ExecuteProcess(string csprojFile)
        {
            int exitCode = 0;

            string msbuildPath = GetMSbuildPath();

            ProcessStartInfo compilerInfo = GetMSbuildProcessInfo(msbuildPath, csprojFile);

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

            Console.WriteLine(TIME_ELAPSED_FORMAT, stopwatch.ElapsedMilliseconds.ToString());

            SuccessExit();
        }

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

        private static ProcessStartInfo GetMSbuildProcessInfo(string msbuildPath, string csprojFile)
        {
            ProcessStartInfo compilerInfo = new ProcessStartInfo();
            compilerInfo.Arguments = String.Concat("/t:Build /nologo /clp:NoSummary;ErrorsOnly; /target:Compile /verbosity:quiet ", csprojFile);
            compilerInfo.FileName = msbuildPath;
            compilerInfo.WindowStyle = ProcessWindowStyle.Hidden;
            compilerInfo.CreateNoWindow = true;
            compilerInfo.UseShellExecute = false;
            compilerInfo.RedirectStandardError = true;

            return compilerInfo;
        }

        #region >> UTILITY        
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
