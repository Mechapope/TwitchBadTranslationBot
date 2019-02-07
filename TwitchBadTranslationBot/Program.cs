using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchBadTranslationBot
{
    class Program
    {
        static void Main(string[] args)
        {
            //read values from config file
            string botName = ConfigurationManager.AppSettings["botName"];
            string authToken = ConfigurationManager.AppSettings["authToken"];
            string[] streams = ConfigurationManager.AppSettings["streamsToBot"].ToLower().Split(',');
            string commandName = ConfigurationManager.AppSettings["commandName"];
            int commandCooldown = int.Parse(ConfigurationManager.AppSettings["commandCooldown"]);
            int numTranslation = int.Parse(ConfigurationManager.AppSettings["numTranslation"]);
            string[] supportedLanguages = ConfigurationManager.AppSettings["supportedLanguages"].Split(',');

            //create a bot for each stream
            for (int i = 0; i < streams.Length; i++)
            {
                Bot b = new Bot("irc.twitch.tv", 6667, botName, authToken, streams[i], commandName, commandCooldown, numTranslation, supportedLanguages);
                Thread thread = new Thread(new ThreadStart(b.Start));
                thread.Start();
            }

            //chill out
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
