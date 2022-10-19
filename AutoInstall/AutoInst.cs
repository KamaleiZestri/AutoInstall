using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;

using System.Collections;
using System.Collections.Immutable;
using System.Globalization;

public class AutoInst
{
    //key = regex searchable; value = runnable exe options
    static Dictionary<string, string> settingsDict = new Dictionary<string, string>();

//TODO 5. lngterm: settings.txt to regex arguements for exe. should regenerate settings file if dead too
// 6. lngterm: make all exes run too, just at the end

    
    public static void Main(string[] args)
    {
        initSettings();

        Console.WriteLine("Choose an option to install the following MSIs and EXEs next to this program:");
        Console.WriteLine("-----------------------");
        outputInstallables();
        Console.WriteLine("-----------------------");
        Console.WriteLine("Press a number then enter to run.");
        Console.WriteLine("[1] Install all, then reboot.");
        Console.WriteLine("[2] Install all, NO reboot.");
        Console.WriteLine("Anything else to exit.");
        string key = Console.ReadLine();

        switch(key)
        {
            case "1":
                InstallAll();
                Console.WriteLine("Rebooting...");
                Thread.Sleep(3000);
                Reboot();
                break;
            case "2":
                InstallAll();
                Console.ReadKey();
                break;
            default:
                System.Environment.Exit(0);
                break;
        }
    }
    private static Dictionary<string, List<string>> generateInstallables()
    {
        Dictionary<string, List<string>> allInstallables = new Dictionary<string,List<string>>();
        List<string> exes = new();
        List<string> msis = new();
    
        DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        allInstallables.Add("exes", exes);
        allInstallables.Add("msis", msis);

        foreach(FileInfo file in dir.GetFiles())
        {
            if (file.Name.EndsWith(".msi"))
                allInstallables["msis"].Add(file.Name);
            else if (file.Name.EndsWith(".exe") && !string.IsNullOrEmpty(isInSettings(file.Name)))
                allInstallables["exes"].Add(file.Name);
        }

        // Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        // exes.Remove(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));

        return allInstallables;
    }
    private static void outputInstallables()
    {
        Dictionary<string, List<string>> installables = generateInstallables();
        int curr = 1;

        if (installables.Count == 0)
        {
            Console.WriteLine("Nothing to install!");
        }

        foreach(string msi in installables["msis"])
        {
            Console.WriteLine(curr + ". " + msi);
            curr++;
        }
        foreach(string exe in installables["exes"])
        {
            Console.WriteLine(curr + ". " + exe);
            curr++;
        }
    }
    private static void InstallAll()
    {
        Console.WriteLine("Now installing all MSI files and running selected exe EXE files in this programs' directory.");

        Dictionary<string, List<string>> installables = generateInstallables();

        foreach(string msi in installables["msis"])
        {
            InstallSingleMSI(msi);
        }
        foreach(string exe in installables["exes"])
        {
            InstallSingleExe(exe, isInSettings(exe));
        }

        Console.WriteLine("Done installing!");
    }
    private static void InstallSingleMSI(string fileName)
    {
        //https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/msiexec

        Console.WriteLine("Deploying " + fileName + "...");

        Process proc = new Process();
        proc.StartInfo.FileName = "msiexec";
        proc.StartInfo.Arguments = " /quiet /i \"" + fileName + "\" /norestart";
        proc.StartInfo.Verb = "runas";

        proc.Start();
        proc.WaitForExit();

        Console.WriteLine(fileName + " Installed!");
    }

    private static void InstallSingleExe(string fileName, string arguments)
    {
        //dotexe installs require special arguments, unlike simple MSIs.
        Console.WriteLine("Deploying " + fileName + "...");

        ProcessStartInfo psi = new ProcessStartInfo(fileName);
        psi.Arguments = arguments;
        psi.Verb = "runas";
        psi.CreateNoWindow = true;
        psi.WindowStyle = ProcessWindowStyle.Hidden;
        psi.UseShellExecute = false;
        
        Process exe = Process.Start(psi);
        exe.WaitForExit();

        Console.WriteLine(fileName + " Installed!");
    }
    private static void Reboot()
    {
        //https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/shutdown
        ProcessStartInfo reboot = new ProcessStartInfo("shutdown.exe");
        reboot.Arguments = "-r -t 0";
        reboot.CreateNoWindow = true;
        reboot.UseShellExecute = false;
        reboot.ErrorDialog = false;
        
        Process.Start(reboot);
    }
    ///<summary>
    //regex each value in keys of settings to the given filename
    ///return successful regex value
    ///return empty or null otherwise
    ///</summary>
    private static string isInSettings(string fileName)
    {
        
        string found = "";

        foreach(KeyValuePair<string, string> kvpair in settingsDict)
        {
            //TODO maybe improve? is currently case sensitive
            if(Regex.IsMatch(fileName, kvpair.Key))
            {
                found = kvpair.Value;
                break;
            }
        }

        return found;
    }
    public static void initSettings()
    {
        string buff = "";
        int count = 0;

        if(!File.Exists("settings.txt"))
        {
            File.WriteAllText("settings.txt", 
            @"//This is the settings file to determine which .exe programs to run.
//Comments lines should have comment marks like this.
//Any two consecutive lines without comments will be read as the following:
//Line 1: Value to find in a .exe filename. That .exe will be run.
//Line 2: Options to include when running the .exe to (hopefully) make it silent install.
//Included is vlc for example.
vlc
/S /NCRC /L=1033");
        }

        //find 2 back-to-back lines. read in format of line 1= key, line 2=value
        //restart search if line with "//" delimiter detected
        foreach (string line in File.ReadAllLines("settings.txt"))
        {
            if (line.StartsWith("//"))
                count = 0;
            else
                count ++;

            if (count == 1)
                buff = line;
            else if (count == 2)
                settingsDict[buff] = line;
        }
    }
    //TODO Analyze for potential activinspire workaround 
    //1. https://www.codeproject.com/Questions/5298642/How-to-install-applications-from-software-center-t
    //2. https://stackoverflow.com/questions/60633838/how-to-install-msi-with-silent-installation
}
