using System;

namespace InteractiveShell.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting shell session");
            
            using (var shellSession = ShellSession.CreateVerbose(10))
            {
                string command = "docker run -d --name mongo-shell mongo";
                Console.WriteLine($"Executing: {command}");
                shellSession.ExecuteCommand(command);

                command = "docker rm -f mongo-shell";
                Console.WriteLine($"Executing: {command}");
                shellSession.ExecuteCommand(command);
            }
        }
    }
}
