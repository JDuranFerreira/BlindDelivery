using System;
using System.Collections.Generic;

// Dev builds only (M8 acceptance: the debug console is absent from production builds).
// UNITY_EDITOR keeps it alive for EditMode tests; a game's own overlay hosts it.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
namespace Template.Services.OptIn
{
    /// <summary>
    /// Bonus system 4 (opt-in file, not base wiring): the test-build debug console — cheat
    /// commands plus a ring of the session log, compiled out of production builds by the define.
    /// The console owns no rendering: a game wires its own overlay/UI onto this API.
    /// </summary>
    public sealed class DebugConsole
    {
        /// <summary>One registered cheat/test command. Args are the whitespace-split tokens after the name.</summary>
        public delegate void ConsoleCommand(string[] args);

        readonly Dictionary<string, (string help, ConsoleCommand run)> commands =
            new Dictionary<string, (string, ConsoleCommand)>(StringComparer.OrdinalIgnoreCase);

        readonly Queue<string> log = new Queue<string>();
        readonly int logCapacity;

        public DebugConsole(int logCapacity = 200)
        {
            if (logCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(logCapacity));
            }

            this.logCapacity = logCapacity;
        }

        /// <summary>Whether the game's overlay should be showing the console. The game drives it.</summary>
        public bool IsOpen { get; set; }

        /// <summary>Registers a command. A duplicate name replaces the old binding; last wins.</summary>
        public void RegisterCommand(string name, string help, ConsoleCommand handler)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A command name is required.", nameof(name));
            }

            commands[name.Trim()] = (help ?? "", handler ?? throw new ArgumentNullException(nameof(handler)));
        }

        /// <summary>Command names currently bound, sorted (a game renders them as its help list).</summary>
        public IReadOnlyList<string> CommandNames
        {
            get
            {
                List<string> names = new List<string>(commands.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names;
            }
        }

        /// <summary>
        /// Executes one input line: splits on whitespace, name first, the rest are args. Returns
        /// false — and logs — for an unknown command or an empty line; the game decides what UI
        /// feedback that maps to.
        /// </summary>
        public bool TryExecute(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string[] tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!commands.TryGetValue(tokens[0], out (string help, ConsoleCommand run) command))
            {
                Log("Unknown command: " + tokens[0]);
                return false;
            }

            string[] args;
            if (tokens.Length > 1)
            {
                args = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, args, 0, tokens.Length - 1);
            }
            else
            {
                args = Array.Empty<string>();
            }

            command.run(args);
            return true;
        }

        /// <summary>Appends one line to the console log; the oldest line falls off the ring.</summary>
        public void Log(string message)
        {
            log.Enqueue(message ?? "");
            while (log.Count > logCapacity)
            {
                log.Dequeue();
            }
        }

        /// <summary>The newest <paramref name="count"/> lines, oldest first. The game renders them.</summary>
        public IReadOnlyList<string> LogTail(int count = 20)
        {
            int take = Math.Min(Math.Max(count, 0), log.Count);
            string[] tail = new string[take];
            int offset = log.Count - take;
            int index = 0;
            foreach (string line in log)
            {
                if (offset-- > 0)
                {
                    continue;
                }

                tail[index++] = line;
            }

            return tail;
        }
    }
}
#endif
