using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Npgsql.Internal.TypeHandlers.DateTimeHandlers;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Log;
using Robust.Shared.Configuration;
using Robust.Shared;
using Serilog.Sinks.Http;
using System.Net.Http.Headers;
using Content.Shared.Random;
using Robust.Shared.Console;
using static Robust.Shared.Console.ConsoleHost;
using TerraFX.Interop.Windows;
using Robust.Server.Console;
using Robust.Shared.Utility;

namespace Content.Server.SS220.BackEndApi
{
    public sealed partial class ServerControlController
    {
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IConsoleHost _conHost = default!;
        [Dependency] private readonly IServerConsoleHost _clientConsoleHost = default!;

        private string? _watchdogToken;
        private string? _watchdogKey;

        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _configurationManager.OnValueChanged(CVars.WatchdogToken, _ => UpdateToken());
            _configurationManager.OnValueChanged(CVars.WatchdogKey, _ => UpdateToken());

            UpdateToken();
        }

        public void PostInitialize()
        {
            _sawmill = Logger.GetSawmill("watchdogApi");
            _statusHost.AddHandler(BackRequestHandler);
        }

        private async Task<bool> BackRequestHandler(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/console-command")
            {
                return false;
            }

            var auth = context.RequestHeaders["WatchdogToken"];

            if (auth != _watchdogToken)
            {
                // Holy shit nobody read these logs please.
                _sawmill.Info(@"Failed auth: ""{0}"" vs ""{1}""", auth, _watchdogToken);
                await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
                return true;
            }

            var command = context.RequestHeaders["command"];

            if (string.IsNullOrWhiteSpace(command))
            {
                await context.RespondErrorAsync(HttpStatusCode.BadRequest);

                return true;
            }

            _taskManager.RunOnMainThread(() => RunConsoleCommand(command));

            await context.RespondAsync("Success", HttpStatusCode.OK);

            return true;
        }

        private void RunConsoleCommand(string command)
        {
            var args = new List<string>();

            CommandParsing.ParseArguments(command, args);

            var commandName = args[0];

            if (_clientConsoleHost.AvailableCommands.TryGetValue(commandName, out var conCmd)) // command registered
            {
                args.RemoveAt(0);
                var cmdArgs = args.ToArray();
                if (!ShellCanExecute(shell, cmdName))
                {
                    shell.WriteError($"Unknown command: '{cmdName}'");
                    return;
                }

                AnyCommandExecuted?.Invoke(shell, cmdName, command, cmdArgs);
                conCmd.Execute(shell, command, cmdArgs);
            }
        }

        private void UpdateToken()
        {
            var tok = _configurationManager.GetCVar(CVars.WatchdogToken);
            var key = _configurationManager.GetCVar(CVars.WatchdogKey);
            var baseUrl = _configurationManager.GetCVar(CVars.WatchdogBaseUrl);
            _watchdogToken = string.IsNullOrEmpty(tok) ? null : tok;
            _watchdogKey = string.IsNullOrEmpty(key) ? null : key;
            // _baseUri = string.IsNullOrEmpty(baseUrl) ? null : new Uri(baseUrl);

            //if (_watchdogKey != null && _watchdogToken != null)
            //{
            //    var paramStr = $"{_watchdogKey}:{_watchdogToken}";
            //    var param = Convert.ToBase64String(Encoding.UTF8.GetBytes(paramStr));

            //    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", param);
            //}
            //else
            //{
            //    _httpClient.DefaultRequestHeaders.Authorization = null;
            //}
        }
    }
}
