using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchBadTranslationBot
{
    class Program
    {
        static void Main(string[] args)
        {
            string botName;
            string authToken;
            string commandName;
            string[] streams;            

            using (StreamReader sr = new StreamReader("botInfo.txt"))
            {
                botName = sr.ReadLine();
                authToken = sr.ReadLine();
                streams = sr.ReadLine().Split(',');
                commandName = sr.ReadLine();
            }

            for (int i = 0; i < streams.Length; i++)
            {
                Bot b = new Bot("irc.twitch.tv", 6667, botName, authToken, streams[i], commandName);
                Thread thread = new Thread(new ThreadStart(b.Start));
                thread.Start();
            }

            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
