using System;
using System.Text;
using KCompiler;
using KCompiler.Cli;

namespace KCompiler.Net
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                NwnnsscompParseResult parsed = NwnnsscompCliParser.Parse(args);
                if (parsed.IsHelp)
                {
                    Console.Out.WriteLine(parsed.ErrorMessage.TrimEnd());
                    return 0;
                }

                if (!parsed.Success)
                {
                    if (!string.IsNullOrEmpty(parsed.ErrorMessage))
                    {
                        Console.Error.WriteLine(parsed.ErrorMessage.TrimEnd());
                    }

                    return 1;
                }

                ManagedNwnnsscomp.CompileFile(
                    parsed.SourcePath,
                    parsed.OutputPath,
                    parsed.Game,
                    parsed.Debug,
                    parsed.NwscriptPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }
    }
}
