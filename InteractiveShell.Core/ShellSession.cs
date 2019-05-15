using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace InteractiveShell.Core
{
    public class ShellSession : IShell, IDisposable
    {
        private PowerShell _powerShellInstance;
        private bool _verbose;
        private int _counterMilliseconds;
        private int _statusCheckMilliseconds;

        public int StatusCheckMilliSeconds
        {
            get { return _statusCheckMilliseconds; }
            set
            {
                if(value < 1)
                {
                    throw new ArgumentOutOfRangeException("The status check must be greater than 0");
                }
                _statusCheckMilliseconds = value;
            }
        }

        private ShellSession(bool verbose, int counterSeconds)
        {
            _verbose = verbose;
            _counterMilliseconds = counterSeconds * 1000;
            _statusCheckMilliseconds = 100;
            _powerShellInstance = PowerShell.Create();
        }

        public static ShellSession Create()
        {
            return new ShellSession(false, -1);
        }

        public static ShellSession CreateVerbose(int counterSeconds = 1)
        {
            if(counterSeconds < 1)
            {
                throw new ArgumentOutOfRangeException("The counter must be greater than 0");
            }

            return new ShellSession(true, counterSeconds);
        }

        public void ExecuteCommand(string command, int timeoutSeconds = 30)
        {
            if (String.IsNullOrEmpty(command))
            {
                throw new NullReferenceException("The command must not be blank");
            }
            if(timeoutSeconds < 1)
            {
                throw new ArgumentOutOfRangeException("The timeout must be greater than 0");
            }

            int timeoutMilliseconds = timeoutSeconds * 1000;

            _powerShellInstance.Commands.Clear();

            try
            {
                _powerShellInstance.AddScript(command);

                // prepare a new collection to store output stream objects
                PSDataCollection<PSObject> outputCollection = new PSDataCollection<PSObject>();
                //outputCollection.DataAdded += (obj,e) =>
                //{
                //    PrintPSOutput((PSDataCollection<PSObject>)obj);
                //};

                IAsyncResult result = _powerShellInstance.BeginInvoke<PSObject, PSObject>(null, outputCollection);

                int i = 0;
                int countSecs = 0;
                while (result.IsCompleted == false)
                {
                    if (i > 0)
                    {
                        if(i == timeoutMilliseconds)
                        {
                            _powerShellInstance.Stop();
                            throw new TimeoutException($"Command exceeded timeout of {timeoutSeconds}s");
                        }

                        if(i % _counterMilliseconds == 0)
                        {
                            countSecs++;
                            Console.WriteLine($"Command run for {countSecs}s");
                        }
                    }

                    Thread.Sleep(_statusCheckMilliseconds);
                    i+= _statusCheckMilliseconds;
                }

                // check the other output streams (for example, the error stream)
                if (_powerShellInstance.Streams.Error.Count > 0)
                {
                    foreach (var error in _powerShellInstance.Streams.Error)
                    {
                        if (_verbose)
                        {
                            Console.WriteLine(error.ToString());
                        }
                    }
                    ReportErrors(_powerShellInstance.Streams.Error.Select(e => e.ToString()));
                }
                else
                {
                    PrintPSOutput(outputCollection);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing command", ex);
            }
        }

        private void PrintPSOutput(PSDataCollection<PSObject> outputCollection)
        {
            foreach (PSObject outputItem in outputCollection)
            {
                // if null object was dumped to the pipeline during the script then a null
                // object may be present here. check for null to prevent potential NRE.
                if (outputItem != null)
                {
                    string output = outputItem.BaseObject.ToString();

                    if (_verbose)
                    {
                        Console.WriteLine(output);
                    }

                    if (output.IndexOf("failed") > -1)
                    {
                        ReportErrors(outputCollection.Where(i => i != null).Select(i => $"{i.BaseObject.ToString()}"));
                    }
                }
            }
        }

        private void ReportErrors(IEnumerable<string> errorMessages)
        {
            var errorLog = String.Join(";", errorMessages.ToArray());
            throw new ApplicationFailedException(errorLog);
        }

        public void Dispose()
        { 
            _powerShellInstance?.Dispose();
        }
    }
}
