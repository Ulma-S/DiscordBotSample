using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Discord Botのメイン機能を提供するクラス
/// </summary>
public class BotCore {
    private DiscordSocketClient m_client;       //Bot本体
    private static CommandService m_commands;   //コマンドが登録されるオブジェクト
    private static IServiceProvider m_service;  //DI用オブジェクト

    /// <summary>
    /// パスワードの様なものなので公開しないこと。
    /// 流出時は https://discord.com/developers/applications から再設定する。
    /// </summary>
    private const string m_token = "";


    private static void Main(string[] args) => new BotCore().MainAsync().GetAwaiter().GetResult();


    /// <summary>
    /// エントリポイントから直接呼ばれる。
    /// 諸々の初期化メソッド。
    /// </summary>
    /// <returns></returns>
    public async Task MainAsync() {
        m_service = new ServiceCollection().BuildServiceProvider();

        m_commands = new CommandService();
        await m_commands.AddModulesAsync(Assembly.GetEntryAssembly(), m_service);   //コマンドを注入

        m_client = new DiscordSocketClient(
            new DiscordSocketConfig() {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
            });

        m_client.Log += Log;    //デバッグ用メソッド登録
        m_client.MessageReceived += CommandReceived;    //コマンド用メソッド登録

        await m_client.LoginAsync(TokenType.Bot, m_token);
        await m_client.StartAsync();

        await Task.Delay(-1);   //待機
    }


    /// <summary>
    /// デバッグ出力用メソッド。
    /// </summary>
    /// <param name="_message"></param>
    /// <returns></returns>
    private Task Log(LogMessage _message) {
        Console.WriteLine(_message.ToString());
        return Task.CompletedTask;
    }


    /// <summary>
    /// 受け取ったコマンドを処理するメソッド。
    /// </summary>
    /// <param name="_message"></param>
    /// <returns></returns>
    private async Task CommandReceived(SocketMessage _message) {
        try {
            var message = _message as SocketUserMessage;

            if (message == null) return;
            if (message.Author.IsBot) return;   //発言者がボットか確認

            int argPos = 0;
            //コマンドか識別する。先頭に!がついているか、このBotヘのメンションをコマンドとして実行する。
            if (message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(m_client.CurrentUser, ref argPos)) {
                var command = new CommandContext(m_client, message);
                try {
                    await m_commands.ExecuteAsync(command, argPos, m_service);
                }
                catch (Exception _e) {
                    Console.WriteLine(_e);
                }
            }
        }
        catch (Exception _e) {
            Console.WriteLine(_e);
        }
    }
}

