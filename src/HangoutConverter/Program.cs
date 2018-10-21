using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HangoutConverter
{
    class Program
    {
        static void Main(string[] args)
        {        
            if (args.Length != 4)
            {
                Console.WriteLine("HangoutConverter to convert google takeout archive files of hangout history to beautiful PDF. To use this tool you have to download and unzip your hangout archive from https://takeout.google.com");
                Console.WriteLine("Usage: ");
                Console.WriteLine("    HangoutConverter.exe <path-to-hangouts.json-file> \"<name-of-first-person>\" \"<name-of-second-person>\" <pdf-file-name>");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("    HangoutConverter.exe takeout-20181010T070302Z-001\\Takeout\\Hangouts\\Hangouts.json \"John Doe\" \"Jane Roe\" result.pdf");
            }

            string backupFile = args[0];
            string participant1 = args[1];
            string participant2 = args[2];
            string pdfFile = args[3];

            try
            {
                Console.WriteLine($"Start processing file {backupFile} to extract conversations of {participant1} and {participant2} to {pdfFile}");
                string[] split = backupFile.Split('\\');
                string backupFileBaseDir = string.Join("\\", split.Take(split.Length - 1));
                string historyFile = Path.Combine(backupFileBaseDir, "chat.json");

                var history = BackupParser.ExtractConversation(backupFile, participant1, participant2);
                string jsonStr = JsonConvert.SerializeObject(history, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(historyFile, jsonStr);

                var chat = JsonConvert.DeserializeObject<ChatHistory>(File.ReadAllText(historyFile));
                PdfBuilder.CreatePdf(chat, pdfFile);
                try
                {
                    Console.WriteLine("Opening default PDF viewer");
                    Process.Start(pdfFile);
                }
                catch (Exception pdfException)
                {
                    Console.WriteLine($"Unable to open PDF viewer ({pdfException}).");
                }
            }
            catch (Exception fatalEx)
            {
                Console.WriteLine($"Error {fatalEx.Message} of type {fatalEx} at {fatalEx.StackTrace}");
            }
        }
    }
}
