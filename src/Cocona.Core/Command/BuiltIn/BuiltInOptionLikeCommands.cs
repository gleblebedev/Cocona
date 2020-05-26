using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cocona.Application;
using Cocona.Command.Features;
using Cocona.CommandLine;
using Cocona.Help;
using Cocona.ShellCompletion;
using Cocona.ShellCompletion.Candidate;

namespace Cocona.Command.BuiltIn
{
    public class BuiltInOptionLikeCommands
    {
        public static CommandOptionLikeCommandDescriptor Help { get; }
            = new CommandOptionLikeCommandDescriptor("help", new []{ 'h'}, new CommandDescriptor(
                typeof(BuiltInOptionLikeCommands).GetMethod(nameof(ShowHelp)),
                nameof(ShowHelp),
                Array.Empty<string>(),
                "Show help message",
                new[]
                {
                    new CommandServiceParameterDescriptor(typeof(ICoconaAppContextAccessor), "appContext"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaCommandProvider), "commandProvider"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaCommandHelpProvider), "commandHelpProvider"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaConsoleProvider), "console"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaHelpRenderer), "helpRenderer"),
                },
                Array.Empty<CommandOptionDescriptor>(),
                Array.Empty<CommandArgumentDescriptor>(),
                Array.Empty<CommandOverloadDescriptor>(),
                Array.Empty<CommandOptionLikeCommandDescriptor>(),
                CommandFlags.None,
                null
            ), CommandOptionFlags.None);

        public static CommandOptionLikeCommandDescriptor Version { get; }
            = new CommandOptionLikeCommandDescriptor("version", Array.Empty<char>(), new CommandDescriptor(
                typeof(BuiltInOptionLikeCommands).GetMethod(nameof(ShowVersion)),
                nameof(ShowVersion),
                Array.Empty<string>(),
                "Show version",
                new []
                {
                    new CommandServiceParameterDescriptor(typeof(ICoconaCommandHelpProvider), "commandHelpProvider"), 
                    new CommandServiceParameterDescriptor(typeof(ICoconaConsoleProvider), "console"), 
                    new CommandServiceParameterDescriptor(typeof(ICoconaHelpRenderer), "helpRenderer"), 
                },
                Array.Empty<CommandOptionDescriptor>(),
                Array.Empty<CommandArgumentDescriptor>(),
                Array.Empty<CommandOverloadDescriptor>(),
                Array.Empty<CommandOptionLikeCommandDescriptor>(),
                CommandFlags.None,
                null
            ), CommandOptionFlags.None);

        public static CommandOptionLikeCommandDescriptor Completion { get; }
            = new CommandOptionLikeCommandDescriptor("completion", Array.Empty<char>(), new CommandDescriptor(
                typeof(BuiltInOptionLikeCommands).GetMethod(nameof(GenerateCompletionSource)),
                nameof(GenerateCompletionSource),
                Array.Empty<string>(),
                "Generate a shell completion code",
                new ICommandParameterDescriptor[]
                {
                    new CommandServiceParameterDescriptor(typeof(ICoconaConsoleProvider), "console"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaCommandProvider), "commandProvider"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaShellCompletionCodeProvider), "shellCompletionCodeProvider"),
                    new CommandArgumentDescriptor(typeof(string), "shell", 0, "A target shell.", CoconaDefaultValue.None, Array.Empty<Attribute>()),
                },
                Array.Empty<CommandOptionDescriptor>(),
                new[]
                {
                    new CommandArgumentDescriptor(typeof(string), "shell", 0, "A target shell.", CoconaDefaultValue.None, Array.Empty<Attribute>()),
                },
                Array.Empty<CommandOverloadDescriptor>(),
                Array.Empty<CommandOptionLikeCommandDescriptor>(),
                CommandFlags.None,
                null
            ), CommandOptionFlags.None);

        public static CommandOptionLikeCommandDescriptor CompletionCandidates { get; }
            = new CommandOptionLikeCommandDescriptor("completion-candidates", Array.Empty<char>(), new CommandDescriptor(
                typeof(BuiltInOptionLikeCommands).GetMethod(nameof(GetCompletionCandidates)),
                nameof(GetCompletionCandidates),
                Array.Empty<string>(),
                "Generate a shell completion candidates",
                new ICommandParameterDescriptor[]
                {
                    new CommandServiceParameterDescriptor(typeof(ICoconaConsoleProvider), "console"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaShellCompletionCodeProvider), "shellCompletionCodeGenerator"),
                    new CommandServiceParameterDescriptor(typeof(ICoconaCompletionCandidates), "completionCandidates"),
                    new CommandArgumentDescriptor(typeof(string), "target", 0, "A target option.", CoconaDefaultValue.None, Array.Empty<Attribute>()),
                    new CommandArgumentDescriptor(typeof(string[]), "arguments", 0, "Original arguments.", CoconaDefaultValue.None, Array.Empty<Attribute>()),
                },
                Array.Empty<CommandOptionDescriptor>(),
                new[]
                {
                    new CommandArgumentDescriptor(typeof(string), "target", 0, "A target option.", CoconaDefaultValue.None, Array.Empty<Attribute>()),
                    new CommandArgumentDescriptor(typeof(string[]), "arguments", 0, "Original arguments.", CoconaDefaultValue.None, Array.Empty<Attribute>()),
                },
                Array.Empty<CommandOverloadDescriptor>(),
                Array.Empty<CommandOptionLikeCommandDescriptor>(),
                CommandFlags.Hidden,
                null
            ), CommandOptionFlags.Hidden);

        public static ValueTask<int> ShowHelp(
            [FromService]ICoconaAppContextAccessor appContext,
            [FromService]ICoconaCommandProvider commandProvider,
            [FromService]ICoconaCommandHelpProvider commandHelpProvider,
            [FromService]ICoconaConsoleProvider console,
            [FromService]ICoconaHelpRenderer helpRenderer
        )
        {
            var feature = appContext.Current!.Features.Get<ICoconaCommandFeature>()!;
            var commandCollection = feature.CommandCollection ?? commandProvider.GetCommandCollection(); // nested or root
            var targetCommand = feature.CommandStack.Last();
            var help = targetCommand.IsPrimaryCommand
                ? commandHelpProvider.CreateCommandsIndexHelp(commandCollection, feature.CommandStack.Take(feature.CommandStack.Count - 1).ToArray())
                : commandHelpProvider.CreateCommandHelp(targetCommand, feature.CommandStack.Take(feature.CommandStack.Count - 1).ToArray());

            console.Output.Write(helpRenderer.Render(help));
            return new ValueTask<int>(129);
        }

        public static ValueTask<int> ShowVersion(
            [FromService]ICoconaCommandHelpProvider commandHelpProvider,
            [FromService]ICoconaConsoleProvider console,
            [FromService]ICoconaHelpRenderer helpRenderer
        )
        {
            console.Output.Write(helpRenderer.Render(commandHelpProvider.CreateVersionHelp()));
            return new ValueTask<int>(0);
        }

        public static ValueTask<int> GenerateCompletionSource(
            [FromService]ICoconaConsoleProvider console,
            [FromService]ICoconaCommandProvider commandProvider,
            [FromService]ICoconaShellCompletionCodeProvider shellCompletionCodeProvider,
            [Argument]string shellName
        )
        {
            if (!shellCompletionCodeProvider.CanHandle(shellName))
            {
                console.Error.Write($"Error: Shell completion for '{shellName}' is not supported. (Supported shells: {string.Join(", ", shellCompletionCodeProvider.SupportedTargets)})");
                return new ValueTask<int>(1);
            }

            shellCompletionCodeProvider.Generate(shellName, console.Output, commandProvider.GetCommandCollection());
            return new ValueTask<int>(0);
        }

        // --completion-candidates <shell>:<paramName> -- <incompleted command line...>
        // WARN: The option must be processed before '--help' or '--version' options.
        //       If '--completion-candidates' option is provided, '--help' and '--version' options are also always provided.
        //       And these options prevent to perform unintended **destructive** action if the command doesn't support on-the-fly candidates feature.
        //       Fortunately, Cocona rejects unknown options by default. This options guard is fail-safe.
        public static ValueTask<int> GetCompletionCandidates(
            [FromService]ICoconaConsoleProvider console,
            [FromService]ICoconaShellCompletionCodeProvider shellCompletionCodeGenerator,
            [FromService]ICoconaCompletionCandidates completionCandidates,
            [Argument]string target,
            [Argument]string[] arguments
        )
        {
            var parts = target.Split(new[] { ':' }, 2);
            var (shellTarget, paramName) = (parts[0], parts[1]);

            var candidates = completionCandidates.GetOnTheFlyCandidates(paramName, arguments, 0, null);
            shellCompletionCodeGenerator.GenerateOnTheFlyCandidates(shellTarget, console.Output, candidates);
            return new ValueTask<int>(0);
        }
    }
}
