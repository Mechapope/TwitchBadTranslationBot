﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TwitchBadTranslationBot
{
    class Bot
    {
        public string userName;
        public string channel;
        public string commandName;
        public int numOfTranslations = 3;
        public string[] supportedLanguages = { "af", "sq", "az", "eu", "ca", "hr", "cs", "da", "nl", "en", "eo", "et", "tl", "fi", "fr", "gl", "de", "ht", "hu", "is", "id", "ga", "it", "la", "lv", "lt", "ms", "mt", "no", "pl", "pt", "ro", "sk", "sl", "es", "sw", "sv", "tr", "vi", "cy" };
        public int commandCooldown = 60;


        private TcpClient _tcpClient;
        private StreamReader _inputStream;
        private StreamWriter _outputStream;
        private static readonly HttpClient client = new HttpClient();

        public Bot(string ip, int port, string userName, string authToken, string channel, string commandName)
        {
            try
            {
                this.userName = userName;
                this.channel = channel;
                this.commandName = commandName;

                _tcpClient = new TcpClient(ip, port);
                _inputStream = new StreamReader(_tcpClient.GetStream());
                _outputStream = new StreamWriter(_tcpClient.GetStream());

                //try to join the room
                _outputStream.WriteLine("PASS " + authToken);
                _outputStream.WriteLine("NICK " + userName);
                _outputStream.WriteLine("USER " + userName + " 8 * :" + userName);
                _outputStream.WriteLine("JOIN #" + channel);
                _outputStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Start()
        {
            while (true)
            {
                //read message from chat room
                string message = ReadMessage();
                Console.WriteLine(message); //print raw irc messages

                DateTime lastPostDate = DateTime.MinValue;
                
                if(message.StartsWith("PING"))
                {
                    //respond to pings
                    SendIrcMessage("PING irc.twitch.tv");
                }
                else if (message.Contains("PRIVMSG"))
                {
                    //format: ":[user]![user]@[user].tmi.twitch.tv PRIVMSG #[channel] :[message]"
                    //parse message
                    int intIndexParseSign = message.IndexOf('!');
                    string userName = message.Substring(1, intIndexParseSign - 1);
                    intIndexParseSign = message.IndexOf(" :");
                    message = message.Substring(intIndexParseSign + 2);

                    //Console.WriteLine(message); //full message for debugging

                    if (message.StartsWith(commandName) && lastPostDate.AddSeconds(commandCooldown) < DateTime.Now)
                    {
                        //repeat message
                        SendPublicChatMessage(message.Substring(commandName.Length + 1));
                        lastPostDate = DateTime.Now;
                    }
                }
            }
        }

        public void SendIrcMessage(string message)
        {
            try
            {
                _outputStream.WriteLine(message);
                _outputStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendPublicChatMessage(string message)
        {
            try
            {
                SendIrcMessage(":" + userName + "!" + userName + "@" + userName + ".tmi.twitch.tv PRIVMSG #" + channel + " :" + message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public string ReadMessage()
        {
            try
            {
                string message = _inputStream.ReadLine();
                return message;
            }
            catch (Exception ex)
            {
                return "Error receiving message: " + ex.Message;
            }
        }

        private string GetBadTranslation(string text)
        {
            Random r = new Random();
            //first language always has to be english
            string previousLanguage = "en";

            for (int i = 0; i < numOfTranslations; i++)
            {
                //pick a random language to translate to
                string nextLanguage = supportedLanguages[r.Next(supportedLanguages.Count())];

                //check that next language isnt the same as prev language
                while (nextLanguage == previousLanguage)
                {
                    nextLanguage = supportedLanguages[r.Next(supportedLanguages.Count())];
                }

                text = Translate(text, previousLanguage, nextLanguage);

                previousLanguage = nextLanguage;
            }

            while (previousLanguage == "en")
            {
                previousLanguage = supportedLanguages[r.Next(supportedLanguages.Count())];
            }

            //finally, translate back to english
            text = Translate(text, previousLanguage, "en");

            return text;
        }

        private string FreeTranslate(string text, string inLanguage, string outLanguage)
        {
            //https://translate.googleapis.com/translate_a/single?ie=UTF-8&oe=UTF-8&multires=1&client=gtx&sl=en&tl=fr&dt=t&q=hello%20world
            return "";
        }

        private string Translate(string text, string inLanguage, string outLanguage)
        {
            string apiKey = "";
            try
            {
                string translateUrl = "https://translation.googleapis.com/language/translate/v2?q=" + WebUtility.UrlEncode(text) + "&target=" + outLanguage + "&format=text&source=" + inLanguage + "&key=" + apiKey;

                var response = client.PostAsync(translateUrl, null).Result;
                string contents = response.Content.ReadAsStringAsync().Result;

                RootObject resultObj = JsonConvert.DeserializeObject<RootObject>(contents);

                StringBuilder sb = new StringBuilder();

                foreach (var item in resultObj.data.translations)
                {
                    sb.Append(item.translatedText);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "error";
            }
        }
    }
}
