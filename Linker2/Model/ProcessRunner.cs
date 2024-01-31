using System.Diagnostics;

namespace Linker2.Model;

public static class ProcessRunner
{
    public static void Start(string executablePath, string commandLineArgs)
    {
        var startinfo = new ProcessStartInfo()
        {
            FileName = executablePath,
            Arguments = commandLineArgs,
        };
        Process.Start(startinfo);
    }
}
