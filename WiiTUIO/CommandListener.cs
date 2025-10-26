using System;
using System.Windows;
using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;

namespace WiiTUIO
{
    internal class CommandListener
    {
        public Action<string> OnKeymapRequested;

        private static CommandListener defaultInstance;

        public static CommandListener Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new CommandListener();
                }
                return defaultInstance;
            }
        }

        private CommandListener()
        {
            Task.Run(() => ListenForCommands());
        }

        private async Task ListenForCommands()
        {
            while (true)
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream("Gunmote"))
                {
                    await server.WaitForConnectionAsync();

                    using (StreamReader reader = new StreamReader(server))
                    {
                        string pipeMessage = await reader.ReadLineAsync();
                        if (pipeMessage != null)
                        {
                            string[] splitMessage = pipeMessage.Split(Convert.ToChar(31));
                            string command = splitMessage[0];
                            string value = splitMessage.Length > 1 ? splitMessage[1] : null;
                            Console.WriteLine($"Received command: {command} with value {value}");
                            HandleCommand(command, value);
                        }
                    }
                }
            }
        }

        private void HandleCommand(string command, string value)
        {
            if (command == "exit")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            }
            else if (command == "keymap")
            {
                Console.WriteLine("Received keymap: " + value);
                OnKeymapRequested?.Invoke(value);
            }
        }
    }
}
