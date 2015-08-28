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
        private const string BUILD_FAIL = "Build falhou!";
        private const string BUILD_SUCCEEDED = "Build OK!";

        static void Main(string[] args)
        {
            CheckArguments(args);

            var fullPath = args[0];

            var info = new FileInfo { 
                fileName =  Path.GetFileName(fullPath),
                directory = Path.GetDirectoryName(fullPath)
            };

            FindAndExecute(info);
        }

        private static void CheckArguments(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine("Argumento não informado.");
                FailExit();
            }

            if (!Directory.Exists(Path.GetDirectoryName(args[0])))
            {
                Console.WriteLine("Diretório inválido.");
                FailExit();
            }            
        }

        private static void FindAndExecute(FileInfo info)
        {
            var currentDir = info.directory;

            string[] projects = Directory.GetFiles(currentDir, "*.csproj");

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
                Console.WriteLine("Arquivo .csproj não encontrado.");
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
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");

            string fullPath = Path.Combine(systemRoot, @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe");

            int exitCode = 0;

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("MSbuild.exe não foi encontrado.");
                FailExit();
            }
            
            ProcessStartInfo compilerInfo = new ProcessStartInfo();
            compilerInfo.Arguments = String.Concat("/t:Build /nologo /clp:NoSummary;ErrorsOnly; /target:Compile /verbosity:quiet ", csprojFile);
            compilerInfo.FileName = fullPath;
            compilerInfo.WindowStyle = ProcessWindowStyle.Hidden;
            compilerInfo.CreateNoWindow = true;
            compilerInfo.UseShellExecute = false;
            compilerInfo.RedirectStandardError = true;

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

            Console.WriteLine("Tempo de execução: {0}ms", stopwatch.ElapsedMilliseconds.ToString());

            SuccessExit();
        }

        private static void FailExit()
        {
            Environment.Exit(1);
        }

        private static void SuccessExit()
        {
            Environment.Exit(0);
        }
    }
}
