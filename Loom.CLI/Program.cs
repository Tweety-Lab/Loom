using CommandLine;
using Loom.CLI.Commands;
using System.Reflection;

namespace Loom.CLI;

public class Program
{
    static void Main(string[] args)
    {
        Type[] commandTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(Command).IsAssignableFrom(t) && t != typeof(Command)).ToArray();
        CommandLine.Parser.Default.ParseArguments(args, commandTypes).WithParsed<object>(obj =>
        {
            if (obj is Command command)
                command.Execute();
        });
    }
}
