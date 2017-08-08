using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

//Rodrigo Campos
//rod.apd[at]gmail.com

namespace SingleBuild
{
    /// <summary>
    /// Light object for trasfering basic file information to the FindAndExecute method.
    /// </summary>
    internal class FileInfo
    {
        public string FileName { get; set; }
        public string Directory { get; set; }
    }

    internal class Program
    {
        #region >> CONSTANTS

        private const string BuildFail = "Build falhou!";
        private const string BuildSucceeded = "Build OK!";
        private const string ElapsedTimeFormat = "Tempo de execução: {0}s";
        private const string CsprojNotFound = "Arquivo .csproj não encontrado.";
        private const string ProjectFileFilter = "*.csproj";
        private const string InvalidDirectory = "Diretório inválido.";
        private const string InvalidArgument = "Argumento não informado.";
        private const string MsbuildNotFound = "MSbuild.exe não foi encontrado.";
        private const string SystemRootEnvVarNotFound = "Variável %SystemRoot% não encontrada.";

        #endregion << CONSTANTS

        private static void Main(string[] args)
        {            
            var path = ValidateArguments(args);

            var info = new FileInfo
            {
                FileName = GetFileNameFromPath(path),
                Directory = GetDirectoryFromPath(path)
            };

            FindAndExecute(info);
        }

        #region >> PRIVATE STATIC METHODS

        /// <summary>
        /// Validate command-line arguments.
        /// </summary>
        /// <param name="args">Arguments passedin to the main program.</param>
        private static string ValidateArguments(IReadOnlyList<string> args)
        {
            var dirPath = (args.Any() ? GetDirectoryFromPath(args[0]) : GetDirectoryFromApplication());

            if (!Directory.Exists(dirPath))
                FailWithMsg(InvalidDirectory);

            return dirPath;
        }

        private static string GetDirectoryFromApplication()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Makes recursive lookups on parent directories fiels until a project file is found.
        /// </summary>
        /// <param name="info">Instance of Info class.</param>
        private static void FindAndExecute(FileInfo info)
        {
            var currentDir = info.Directory;

            var projects = Directory.GetFiles(currentDir, ProjectFileFilter);

            if (projects.Any())
            {
                if (info.FileName.Equals(string.Empty))
                {
                    ExecuteProcess(projects[0]);
                }
                else
                {
                    foreach (var project in projects)
                    {
                        if (!IsStringInFile(project, info.FileName)) continue;

                        ExecuteProcess(project);
                        break;
                    }
                }
                SuccessExit();
            }
            else if (!ParentDirectoryExists(currentDir))
            {
                Console.WriteLine(CsprojNotFound);
                FailExit();
            }
            else
            {
                info.Directory = Directory.GetParent(currentDir).FullName;
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
            var exitCode = 0;

            var msbuildPath = GetMSbuildPath();

            var compilerInfo = GetMSbuildProcessInfo(msbuildPath, projectFile);

            Action compilar = () =>
            {
                using (var compiler = Process.Start(compilerInfo))
                {
                    //TODO: Add fail with msg
                    if (compiler == null) return;

                    Console.WriteLine(compiler.StandardError.ReadToEnd());

                    compiler.WaitForExit();

                    exitCode = compiler.ExitCode;
                }
            };

            WithDuration(compilar);

            Console.WriteLine("Compilando {0}...", projectFile);
            Console.WriteLine(exitCode == 0 ? BuildSucceeded : BuildFail);

            SuccessExit();
        }


        private static void WithDuration(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                action();
            }
            catch
            {
                FailExit();
            }
            finally
            {
                stopwatch.Stop();
            }

            Console.WriteLine(ElapsedTimeFormat, stopwatch.Elapsed.Seconds.ToString());
        }

        /// <summary>
        /// Get the MSbuild fullpath based on %SystemRoot%.
        /// </summary>
        /// <returns>MSbuild fullpath.</returns>
        private static string GetMSbuildPath()
        {
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");

            if (systemRoot == null)
            {
                FailWithMsg(SystemRootEnvVarNotFound);
                throw new NotImplementedException();
            }

            var msbuildPath = Path.Combine(systemRoot, @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe");

            if (!File.Exists(msbuildPath))
                FailWithMsg(MsbuildNotFound);

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
            var compilerInfo = new ProcessStartInfo
            {
                Arguments =
                    string.Concat("/t:Build /nologo /clp:NoSummary;ErrorsOnly; /target:Compile /verbosity:quiet ",
                        projectFile),
                FileName = msbuildPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            return compilerInfo;
        }

        #endregion << PRIVATE STATIC METHODS

        #region >> UTILITY   

        private static void FailWithMsg(string message)
        {
            Console.WriteLine(message);
            FailExit();
        }

        private static bool IsStringInFile(string file, string text)
        {
            return File.ReadLines(file).Any(line => line.Contains(text));
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