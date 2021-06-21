using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace InstallAndRunCorsairs
{
    class Program
    {
        static string currentPath;
        static string dataPath;
        static string vhdxPath;
        static string exeWorkingDir;
        static string exePath;
        static string READMEPath;

        /// <summary>
        /// Mount virtual disk
        /// </summary>

        static void MountDisk()
        {
            if (Directory.Exists(dataPath) == false)
            {
                Directory.CreateDirectory(dataPath);
            }

            string arguments = "";
            var psi = new ProcessStartInfo
            {
                FileName = "diskpart.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            var p = new Process();
            p.StartInfo = psi;
            p.Start();
            p.StandardInput.WriteLine(string.Format(@"select vdisk file = ""{0}""", vhdxPath));
            p.StandardInput.WriteLine("attach vdisk noerr");
            p.StandardInput.WriteLine("select part 1");
            p.StandardInput.WriteLine("list volume");

            var sb = new StringBuilder();

            Thread.Sleep(1000);
            string curLine = p.StandardOutput.ReadLine();
            sb.AppendLine(curLine);
            while(curLine.Contains("FAKE-FAT32") == false && p.StandardOutput.EndOfStream == false)
            {
                curLine = p.StandardOutput.ReadLine();
                sb.AppendLine(curLine);
            }
            Regex rx = new Regex("^  [a-zA-Z]+ (\\d+).*");
            string volumeNumber = rx.Replace(curLine, "$1");
            p.StandardInput.WriteLine(string.Format("select volume {0}",volumeNumber));
            p.StandardInput.WriteLine(string.Format(@"assign mount=""{0}""", dataPath));
            p.StandardInput.WriteLine("exit");
            p.WaitForExit();

            sb.AppendLine(p.StandardOutput.ReadToEnd());
            sb.AppendLine(p.StandardError.ReadToEnd());

            Console.WriteLine(sb.ToString());

            if (p.ExitCode != 0)
            {
                Console.Error.WriteLine("Cannot mount fake disk. Restart with administrative rights or check permissions.");
                System.Environment.Exit(1);
            }

            if (File.Exists(READMEPath) == false)
            {
                Console.Error.WriteLine("Error mounting VHDX file.");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Extraction error and exits
        /// </summary>

        static void ExtractError()
        {
            Console.Error.WriteLine("Exe file has not correct format.");
            Environment.Exit(1);
        }

        /// <summary>
        /// Extract data from setup
        /// </summary>

        static void ExtractData(string exe)
        {
            string tempDir = Path.GetTempFileName();
            File.Delete(tempDir);
            Directory.CreateDirectory(tempDir);

            FileStream fsExe = File.OpenRead(exe);

            const int beginning_length = 10240;

            var beginning = new byte[beginning_length];
            if (fsExe.Read(beginning, 0, beginning_length) != beginning_length)
            {
                ExtractError();
            }

            var psi = new ProcessStartInfo
            {
                FileName = "innoextract.exe",
                WorkingDirectory = currentPath,
                Arguments = string.Format(@"--extract ""{0}"" --gog --output-dir ""{1}"" --progress", exe, dataPath),
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                CreateNoWindow = false
            };
            var p = new Process
            {
                StartInfo = psi
            };

            p.Start();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                Console.Error.WriteLine("Cannot extract exe file.");
            }
        }

        /// <summary>
        /// Install GOG version of GOG in dataPath
        /// </summary>

        static void InstallGOGCorsairs()
        {
            string[] exeSearch = Directory.GetFiles(currentPath, "setup_corsairs_gold_*.exe");
            string exe = "";
            if (exeSearch.Length == 0)
            {
                Console.Error.WriteLine("setup_corsairs_gold_*.exe must be put asside this scrîpt if not previously installed.");
                Environment.Exit(1);
            }
            else
            {
                exe = exeSearch[0];
            }

            ExtractData(exe);
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command like argument</param>

        static void Main(string[] args)
        {
            currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            dataPath = Path.Combine(currentPath, "data");
            vhdxPath = Path.Combine(currentPath, "2Go-FAT32-Disk.vhdx");
            exeWorkingDir = Path.Combine(dataPath, "app");
            exePath = Path.Combine(exeWorkingDir, "Corsairs.exe");
            READMEPath = Path.Combine(dataPath, "README.txt");

            if (File.Exists(READMEPath) == false)
            {
                MountDisk();
            }

            if (File.Exists(exePath) == false)
            {
                InstallGOGCorsairs();
            }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                CreateNoWindow = false,
                WorkingDirectory = exeWorkingDir
            };
            Process.Start(psi);
        }
    }
}
