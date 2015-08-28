using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

//Rodrigo Campos
//rod.apd[at]gmail.com

namespace SingleBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            CheckArguments(args);

            var currentDir = args[0];

            FindAndExecute(currentDir);
        }

        private static void CheckArguments(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine("Argumento não informado.");
                Environment.Exit(1);
            }

            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Diretório inválido.");
                Environment.Exit(1);
            }            
        }

        private static void FindAndExecute(string currentDir)
        {
            string[] projects = Directory.GetFiles(currentDir, "*.csproj");

            if (projects.Count() > 0)
            {
                ExecuteProcess(projects[0]);
                Environment.Exit(0);
            }
            else if (Path.Equals(Directory.GetParent(currentDir).FullName, Directory.GetDirectoryRoot(currentDir)))
            {
                Console.WriteLine("Arquivo .csproj não encontrado.");
                Environment.Exit(1);
            }
            else
            {
                FindAndExecute(Directory.GetParent(currentDir).FullName);
            }
        }

        private static void ExecuteProcess(string csprojFile)
        {
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");

            string fullPath = Path.Combine(systemRoot, @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe");

            if (!File.Exists(fullPath))
            {
                Console.WriteLine("MSbuild.exe não foi encontrado.");
                Environment.Exit(1);
            }
            
            ProcessStartInfo compilerInfo = new ProcessStartInfo();
            compilerInfo.Arguments = String.Concat("/t:Build /nologo /clp:NoSummary;ErrorsOnly; /target:Compile /verbosity:quiet ", csprojFile);
            compilerInfo.FileName = fullPath;
            compilerInfo.WindowStyle = ProcessWindowStyle.Hidden;
            compilerInfo.CreateNoWindow = true;
            compilerInfo.UseShellExecute = false;
            compilerInfo.RedirectStandardError = true;

            int exitCode = 0;

            try
            {
                using (Process compiler = Process.Start(compilerInfo))
                {
                    Console.WriteLine(compiler.StandardError.ReadToEnd());

                    compiler.WaitForExit();

                    exitCode = compiler.ExitCode;
                }
            }
            catch(Exception)
            {
                Environment.Exit(1);
            }

            if (exitCode == 0)
                Console.WriteLine("Build ok.");
            else
                Console.WriteLine("Erro no build.");
        }
    }
}
