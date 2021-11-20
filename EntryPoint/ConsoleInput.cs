using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EntryPoint.Program;
using static ServerCore.Util_Server;
using static ServerCore.DataTypes;
using ServerCore;

namespace EntryPoint
{
    public static class ConsoleInput
    {
        const string SPAWN_RUNE = "-spawn_rune";

        /* COMMANDS EXAMPLES
         
        -clients    // shows clients
        -keys       // shows keys for IpEndPoints (?)
        
        -spawn_rune|-1|Random            // spawns rune in all playrooms [-1] with random type [Random]      
        -spawn_rune|1|Balck              // spawns rune in playroom 1 [1] with type Black [Black]

        tcp 'message'       // shows that 'message' should be sent by TCP (DEFAULT VALUE IF SKIPPED)
        udp 'message'       // shows that 'message' should be sent by UDP
        anytext             // command will be sent to all clients by TCP (DEFAULT VALUE)

         */
        public static void ReadConsole()
        {
            string consoleString = Console.ReadLine();

            if (consoleString != "")
            {
                if (consoleString.StartsWith('-'))
                {
                    if (consoleString.Equals("-clients"))
                    {
                        CustomDebug_ShowClients();
                    }
                    else if (consoleString.Equals("-keys"))
                    {
                        CustomDebug_ShowStoredIPs();
                    }
                    else if (consoleString.StartsWith(SPAWN_RUNE))
                    {
                        OnSpawnRuneCustomCommand(consoleString);
                    }
                }
                else if (consoleString.StartsWith("tcp "))
                {
                    consoleString = consoleString.Replace("tcp ", "");
                    SendMessageToAllClients(consoleString);
                }
                else if (consoleString.StartsWith("udp "))
                {
                    consoleString = consoleString.Replace("udp ", "");
                    SendMessageToAllClients(consoleString, MessageProtocol.UDP);
                }
                else
                {
                    SendMessageToAllClients(consoleString);
                }
            }
        }
        static void OnSpawnRuneCustomCommand(string command)
        {
            try
            {
                string[] subcommands = command.Split("|");

                int playroomId = Int32.Parse(subcommands[1]);
                Enum.TryParse(subcommands[2], out ServerCore.DataTypes.Rune runeResult);

                PlayroomManager.ConsoleCommand_SpawnRune(playroomId, runeResult);
            }
            catch (Exception e)
            {
                Console.WriteLine("[Console]: wrong command");
            }
        }
    }
}
