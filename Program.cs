using CommandLine;
using System;

namespace FixAudiobookTitle
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    new FileProcessor(o).Run();
                });
#if DEBUG
            Console.Write("Press [Enter] to continue...");
            Console.ReadLine();
#endif
        }
    }
}
