using System;
using System.IO;
using System.Text;
using Itage.MimeHtml2Html;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Mime2Html
{
    internal class Program
    {
        [Argument(0)] private string Source { get; } = null!;
        private static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private void OnExecute()
        {
            Console.OutputEncoding = Encoding.Unicode;
            
            ILoggerFactory loggerFactory = LoggerFactory.Create(o => { o.AddConsole().SetMinimumLevel(LogLevel.Debug); });
            var converter = new MimeConverter(loggerFactory.CreateLogger<MimeConverter>());
            string outputFilename = Path.ChangeExtension(Source, "html");
            using FileStream sourceStream = File.OpenRead(Source);
            using FileStream destinationStream = File.Open(outputFilename, FileMode.Create);
            bool result = converter.Convert(sourceStream, destinationStream).Result;
            if (!result)
            {
                Environment.Exit(-1);
            }
        }
    }
}