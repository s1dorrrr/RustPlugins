using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System;
using Oxide.Core;
using Newtonsoft.Json;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("Check", "<s1dorftp>", "1.0.0")]
    public class Check : RustPlugin
    {
        #region Init
        private void Init()
        {

            permission.RegisterPermission(config.ModerPerm, this);
            permission.RegisterPermission(config.SuspectPerm, this);
            AddCovalenceCommand("afk", "CmdAFK");
            AddCovalenceCommand("msg", "cmdSendMsg");
            RegisterLang();
            Puts("hui");
        }

        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/Reports", PlayerReports);
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "VizovOverlay");
                tempVKIDs.Remove(player.UserIDString);
            }
        }
        private Dictionary<string, string> tempVKIDs = new Dictionary<string, string>();

        #endregion

        #region Config
        PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                TGTOKEN = "",
                TGCHATID = "",
                DiscordTime = 180,
                ModerPerm = "check.admin",
                SuspectPerm = "check.suspect"
            };
        }

        private class PluginConfig
        {
            [JsonProperty("TGTOKEN")]
            public string TGTOKEN = "";
            [JsonProperty("TGChatID")]
            public string TGCHATID = "";
            [JsonProperty("DiscordTimer")]
            public float DiscordTime = 180;
            [JsonProperty("ModerPerm")]
            public string ModerPerm = "check.admin";
            [JsonProperty("SuspectPerm")]
            public string SuspectPerm = "check.discord";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Lang
        private void RegisterLang()
        {
            Puts("hui2");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MSG.FROM"] = "Message from moderator: ",
                ["Header.SubFindEmpty"] = "No player was found",
                ["Cooldown"] = "Wait %TIME% sec.",
                ["Sent"] = "Report succesful sent",
                ["DSnoArgs"] = "Using /discord <@nick123 / Nick#0000>",
                ["Contact.Sent"] = "You sent discord: ",
                ["Contact.SentWait"] = "Accept request from administrator, if you sent the incorrect discord send correct using /discord <@nick123 / Nick#0000>",
                ["Check.Text"] = "<size=14>\n</size>YOU ARE CALLED FOR CHECK. You must pass a check for cheats.\nYou have <color=#ff4f00><b>3 minutes</b></color> to send discord, and accept friends.\n\n<color=white>IF YOU LEAVE THE SERVER, YOU WILL BE BANNED!</color>"
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MSG.FROM"] = "Сообщение от модератора: ",
                ["Header.SubFindEmpty"] = "Игрок не найден",
                ["Cooldown"] = "Подожди %TIME% сек.",
                ["Sent"] = "Жалоба успешно отправлена",
                ["DSnoArgs"] = "Используйте /discord <@nick123 / Nick#0000>",
                ["Contact.Sent"] = "Вы отправили дискорд:",
                ["Contact.SentWait"] = "<size=12>Примите заявку от администратора, если вы отправили некорректный дискорд отправьте его снова /discord <@nick123 / Nick#0000>.</size>",
                ["Check.Text"] = "<size=14>\n</size>Вы подозреваетесь в использовании читов. Пройдите проверку на наличие читов.\nНапишите свой дискорд используя команду <color=#FD0>/discord</color> в течение <color=#FD0>3 минут</color>.\n\n<color=white>ЕСЛИ ВЫ ПОКИНЕТЕ СЕРВЕР, ВЫ БУДЕТЕ ЗАБАНЕНЫ!</color>"
            }, this, "ru");

            PrintWarning("Core: языковые файлы загружены / lang files loaded");
        }
        #endregion

        #region UI
        private readonly Hash<string, CuiElementContainer> playerUIContainers = new Hash<string, CuiElementContainer>();
        private void ShowVizov(BasePlayer player, string discord = "")
        {
            if (player == null) return;

            string text = lang.GetMessage("Check.Text", this, player.UserIDString);
            DestroyPlayerUI(player);

            var elements = new CuiElementContainer();

            elements.Add(new CuiPanel
            {
                Image = { Color = "0.23 0.23 0.23 0.9", FadeIn = 0.15f },
                RectTransform = { AnchorMin = "0 0.85", AnchorMax = "1 1" },
                FadeOut = 0f
            }, "Hud", "VizovOverlay");

            elements.Add(new CuiLabel
            {
                Text = {
                    Text = text,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.9",
                    FontSize = 18,
                    FadeIn = 0f
                },
                RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                },
                FadeOut = 0f
            }, "VizovOverlay", "VizovText");
            Effect effect = new Effect("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab".ToLower(), player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(effect, player.Connection);

            playerUIContainers[player.UserIDString] = elements;
            SendUI(player, elements);
        }

        private void SendUI(BasePlayer player, CuiElementContainer elements)
        {
            if (player == null || elements == null) return;

            var json = elements.ToJson();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", json);
        }
        private void HideVizov(BasePlayer player)
        {
            if (player == null) return;

            CuiElementContainer elements;
            if (playerUIContainers.TryGetValue(player.UserIDString, out elements))
            {
                DestroyPlayerUI(player);
                SendUI(player, new CuiElementContainer());
            }
        }

        private void DestroyPlayerUI(BasePlayer player)
        {
            if (player == null) return;

            CuiElementContainer elements;
            if (playerUIContainers.TryGetValue(player.UserIDString, out elements))
            {
                CuiHelper.DestroyUi(player, "VizovText");
                CuiHelper.DestroyUi(player, "VizovOverlay");
                playerUIContainers.Remove(player.UserIDString);
            }
        }
        #endregion

        #region Hooks
        private void OnTeamKick(RelationshipManager.PlayerTeam playerTeam, BasePlayer basePlayer, ulong target)
        {
            if (permission.UserHasPermission(basePlayer.UserIDString, config.SuspectPerm))
            {
                string message = $"Игрок {basePlayer} покинул";
                foreach (ulong member in playerTeam.members)
                {
                    message += $" команду:\nID команды:{playerTeam.teamID} \n{member.ToString()}";
                    VKLog(message);
                }
            }
        }
        private void OnTeamLeave(RelationshipManager.PlayerTeam playerTeam, BasePlayer basePlayer)
        {
            if (permission.UserHasPermission(basePlayer.UserIDString, config.SuspectPerm))
            {
                string message = $"Игрок {basePlayer} покинул";
                foreach (ulong member in playerTeam.members)
                {
                    message += $" команду:\nID команды:{playerTeam.teamID} \n{member.ToString()}";
                    //Puts(message);
                    VKLog(message);
                }
            }
        }

        private Timer discordTimer;
        private void DiscordTimer(BasePlayer player)
        {
            discordTimer = timer.Once(config.DiscordTime, () =>
            {
                string timeout = $"Время ожидания дискорда вышло! Можете банить {player} за Игнорирование Проверки";
                VKLog(timeout);
            });
        }

        private const float AFKThreshold = 1.0f;
        private bool IsPlayerAFK(BasePlayer player)
        {
            float idleTime = player.IdleTime;
            return idleTime >= AFKThreshold;
        }

        private float GetSecondsSinceLastActive(BasePlayer player)
        {
            float idleTime = player.IdleTime;
            return idleTime;
        }

        private BasePlayer FindPlayerBySteamId(string steamId)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.UserIDString == steamId)
                    return player;
                if (player.displayName == steamId)
                    return player;
            }
            return null;
        }
        #endregion

        #region Commands
        [ConsoleCommand("find")]
        private void cmdFind(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length == 0)
            {
                var message = $"Использование: find <steamid/nickname>";
                VKLog(message);
                return;
            }

            string identifier = arg.Args[0];
            var players = covalence.Players.FindPlayers(identifier);

            if (players != null && players.Count() > 0)
            {
                if (players.Count() == 1)
                {

                    var player = players.First();
                    ulong player1 = Convert.ToUInt64(player.Id);
                    if (BasePlayer.activePlayerList.Any(p => p.UserIDString == player.Id))
                    {
                        var founOnline = $"Игрок {player.Name}: \nSteamID: {player.Id}\nСтатус: Игрок на сервере\nhttps://steamcommunity.com/id/{player.Id}\nhttps://rustcheatcheck.ru/panel/player/{player.Id}\n";
                        RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(player1);

                        if (playerTeam == null || playerTeam.members.Count <= 1)
                        {
                            founOnline += $"Игрок {player1} не состоит в команде";
                            VKLog(founOnline);
                            return;
                        }

                        founOnline += $"Команда игрока {player1}:\n";
                        foreach (ulong member in playerTeam.members)
                        {
                            founOnline += $"ID Команды: {playerTeam.teamID}\nLeader: {playerTeam.teamLeader}\nMembers: {member.ToString()}";
                            VKLog(founOnline);
                            return;
                        }

                    }
                    else
                    {
                        //var ip = GetPlayerIP(player);
                        //Vector3 position = GetPlayerPosition(player);
                        var message = $"Игрок {player.Name}: \nSteamID: {player.Id}\nСтатус: Не в сети\nhttps://steamcommunity.com/id/{player.Id}\nhttps://rustcheatcheck.ru/panel/player/{player.Id}\n";
                        //VKLog(message);
                        RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(player1);

                        if (playerTeam == null || playerTeam.members.Count <= 1)
                        {
                            message += $"Игрок {player1} не состоит в команде";
                            VKLog(message);
                            return;
                        }

                        message += $"Команда игрока {player1}:\n";
                        foreach (ulong member in playerTeam.members)
                        {
                            message += $"ID Команды: {playerTeam.teamID}\nLeader: {playerTeam.teamLeader}\nMembers: {member.ToString()}";
                            VKLog(message);
                            return;
                        }
                    }
                }
                else
                {
                    var message = "Найдено несколько игроков с указанным никнеймом:";
                    foreach (var player in players)
                    {
                        message += $"\n{player.Name}-{player.Id}";
                    }
                    VKLog(message);
                }
            }
            else
            {
                var message = "Игрок не найден";
                VKLog(message);
            }
        }

        [ConsoleCommand("team")]
        private void ShowTeam(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Connection != null) return;

            ulong num = arg.GetUInt64(0, (ulong)0);
            if (num == 0)
            {
                BasePlayer player_ = BasePlayer.Find(arg.GetString(0));
                if (player_ == null)
                {
                    string NFmessage = "Игрок не найден";
                    VKLog(NFmessage);
                    return;
                }

                num = player_.userID;
            }

            RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(num);

            if (playerTeam == null || playerTeam.members.Count <= 1)
            {
                string message = $"Игрок {num} не состоит в команде";
                VKLog(message);
                return;
            }

            string info = $"Команда игрока {num}:\n";
            foreach (ulong member in playerTeam.members)
            {
                info += $"ID Команды: {playerTeam.teamID}\nLeader: {playerTeam.teamLeader}\nMembers: {member.ToString()}";
                VKLog(info);
                return;
            }
        }

        [ConsoleCommand("check")]
        private void CheckCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2)
            {
                string q = "Укажите корректный steamid и vkid";
                VKLog(q);
                return;
            }

            var steamId = arg.Args[0];
            var vkId = arg.Args[1];

            if (steamId.Length != 17)
            {
                string q = "Укажите корректный steamid";
                VKLog(q);
                return;
            }

            var player = FindPlayerBySteamId(steamId);
            if (player == null)
            {
                var messages = $"Игрок {steamId} не найден";
                Puts(messages);
                VKLog(messages);
                return;
            }

            if (!tempVKIDs.ContainsKey(steamId))
            {
                tempVKIDs.Add(steamId, vkId);
            }

            string cmd = $"o.grant user {steamId} {config.SuspectPerm}";
            string vizov = $"Игрок {player} был вызван на проверку";
            ulong ts = ulong.Parse(steamId);
            Puts(ts.ToString());
            rust.RunServerCommand(cmd);
            VKLog(vizov);
            ShowVizov(player);
            DiscordTimer(player);
            PlayerReports.Remove(ts);
        }

        [ConsoleCommand("stop")]
        private void StopCommand(ConsoleSystem.Arg arg)
        {
            var steamId = arg.GetString(0);
            var player = FindPlayerBySteamId(steamId);
            if (player == null)
            {
                var messages = $"Игрок {player} не найден";
                Puts(messages);
                VKLog(messages);
                return;
            }
            HideVizov(player);
            if (discordTimer != null && !discordTimer.Destroyed)
            {
                discordTimer.Destroy();
            }
            string cmd = $"o.revoke user {steamId} {config.SuspectPerm}";
            string Poff = $"Проверка игрока {player} была остановлена";
            rust.RunServerCommand(cmd);
            VKLog(Poff);
            tempVKIDs.Remove(player.UserIDString);
        }

        private void CmdAFK(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                var message0 = ("Usage: /afk <steamID>");
                VKLog(message0);
                Puts(message0);
                return;
            }

            ulong steamID;
            if (!ulong.TryParse(args[0], out steamID))
            {
                var message00 = "Неверный синтаксис.";
                VKLog(message00);
                Puts(message00);
                return;
            }

            BasePlayer targetPlayer = BasePlayer.Find(args[0]);
            if (targetPlayer == null)
            {
                var message = ("Игрок не найден.");
                Puts(message);
                VKLog(message);
                return;
            }

            if (IsPlayerAFK(targetPlayer))
            {
                float afkTime = GetSecondsSinceLastActive(targetPlayer);
                var message1 = ($"Игрок {targetPlayer} АФК: {(int)afkTime} сек.");
                Puts(message1);
                VKLog(message1);
            }
            else
            {
                var message2 = ($"Игрок {targetPlayer} не АФК.");
                Puts(message2);
                VKLog(message2);
            }
        }

        private void cmdSendMsg(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                var message = "Используй /msg <serverID> <steamId> <message>";
                VKLog(message);
                return;
            }

            ulong steamID;
            if (!ulong.TryParse(args[0], out steamID))
            {
                var message00 = "Неверный синтаксис.";
                VKLog(message00);
                Puts(message00);
                return;
            }

            BasePlayer targetPlayer = BasePlayer.Find(args[0]);
            if (targetPlayer == null)
            {
                var message = ("Игрок не найден.");
                Puts(message);
                VKLog(message);
                return;
            }

            string text = lang.GetMessage("MSG.FROM", this, player.Id);
            text += args[1];
            SendReply(targetPlayer, text);
            var notice = "Сообщение от модератора";
            targetPlayer.SendConsoleCommand("gametip.showgametip", notice);
            timer.Once(5f, () =>
            {
                targetPlayer.SendConsoleCommand("gametip.hidegametip");
            });
            string resp = $"Игрок {targetPlayer} получил сообщение: {text}";
            VKLog(resp);
        }
        [ChatCommand("discord")]
        private void SendDiscord(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.SuspectPerm))
            {
                SendReply(player, "You don't have permission to use this command!");
                return;
            }
            if (args.Length == 0)
            {
                string message = lang.GetMessage("DSnoArgs", this, player.UserIDString);
                SendReply(player, message);
                return;
            }

            string discordName = args[0];
            if (discordName.Length <= 2)
            {
                SendReply(player, "<color=#ff4f00>Неверный дискорд!</color>");
                return;
            }

            SendReply(player, $"{lang.GetMessage("Contact.Sent", this, player.UserIDString)} {discordName}");
            SendReply(player, lang.GetMessage("Contact.SentWait", this, player.UserIDString));

            if (tempVKIDs.TryGetValue(player.UserIDString, out string vkId))
            {
                string sendDiscordMessage = $"@id{vkId} Игрок {player.displayName} предоставил дискорд для проверки: {discordName}";
                VKLog(sendDiscordMessage);
            }

            if (discordTimer != null && !discordTimer.Destroyed)
            {
                discordTimer.Destroy();
            }
        }

        #endregion

        #region Reports
        void OnPlayerReported(BasePlayer reporter, string targetName, string targetId, string subject, string message, string type)
        {
            var victum = BasePlayer.activePlayerList.FirstOrDefault(x => x.UserIDString == targetId);

            var report = new Report();
            report.TargetName = targetName;
            report.ReporterName = reporter.displayName;
            //report.TargetID = targetId;
            report.ReportReason = "[RUST]: " + subject;
            report.ReporterID = reporter.userID;
            report.TargetID = ulong.Parse(targetId);
            var userID = ulong.Parse(targetId);
            if (PlayerReports.ContainsKey(userID))
                PlayerReports[userID].Reports.Add(report);
            else
            {
                PlayerReports[userID] = new UserReportData();
                PlayerReports[userID].UserName = targetName;
                PlayerReports[userID].Reports = new List<Report>();
                PlayerReports[userID].Reports.Add(report);
            }

            string reportMessage = $"" +
                $"Игрок {reporter.displayName} [{reporter.userID}] отправил репорт на игрока {report.TargetName} [{userID}]." +
                $"\nПричина: {report.ReportReason}." +
                $"\nКоличество открытых репортов на игрока: {PlayerReports[userID].Reports.Count}";

            PrintWarning(reportMessage);
            VKLog(reportMessage);
        }
        [ChatCommand("report")]
        private void ReportCommand(BasePlayer reporter, string targetName, string[] args)
        {
            Puts("1");

            if (args.Length < 2)
            {
                SendReply(reporter, "Usage: /report NameOrID reason");
                return;
            }
            targetName = args[0];
            string message = args[1];
            Puts(targetName);
            var player = FindPlayerBySteamId(targetName);
            //Server.Broadcast(player.UserIDString);
            if (player == null)
            {
                SendReply(reporter, $"Игрок {targetName} не найден/не в сети");
                return;
            }
            Puts(player.ToString());


            var report = new Report();
            report.TargetName = player.displayName;
            report.ReporterName = reporter.displayName;
            report.TargetID = player.userID;
            report.ReportReason = "[RUST]: " + message;
            report.ReporterID = reporter.userID;
            var userID = ulong.Parse(player.UserIDString);
            if (PlayerReports.ContainsKey(userID))
                PlayerReports[userID].Reports.Add(report);
            else
            {
                PlayerReports[userID] = new UserReportData();
                PlayerReports[userID].UserName = targetName;
                PlayerReports[userID].Reports = new List<Report>();
                PlayerReports[userID].Reports.Add(report);
            }

            string reportMessage = $"" +
                $"Игрок {reporter.displayName} [{reporter.userID}] отправил репорт на игрока {report.TargetName} [{userID}]." +
                $"\nПричина: {report.ReportReason}." +
                $"\nКоличество открытых репортов на игрока: {PlayerReports[userID].Reports.Count}";

            PrintWarning(reportMessage);
            VKLog(reportMessage);

        }
        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name + "/Reports"))
                PlayerReports = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, UserReportData>>(Name + "/Reports");
            else
                Interface.Oxide.DataFileSystem.WriteObject(Name + "/Reports", PlayerReports = new Dictionary<ulong, UserReportData>());
        }
        [ConsoleCommand("reportlist")]
        private void ReportListCommand()
        {
            Puts("2");

            var topPlayers = PlayerReports.OrderByDescending(entry => entry.Value.Reports.Count)
                                          .Take(10)
                                          .ToList();
            string reportListMsg = "Список репортов на игроков: \n";
            foreach (var entry in topPlayers)
            {
                BasePlayer player = BasePlayer.FindByID(entry.Key);
                if (player != null && player.IsConnected)
                {
                    reportListMsg += $"{entry.Value.UserName}[{entry.Key}] - {entry.Value.Reports.Count} репортов\n";
                }
            }
            VKLog(reportListMsg);
        }

        private void OnServerInitialized()
        {
            cmd.AddConsoleCommand("reportlist", this, "ReportListCommand");
            LoadData();
            string log = $"1: {config.TGTOKEN}\n 2: {config.TGCHATID}\n 3: {config.SuspectPerm}\n 4: {config.ModerPerm}";
            Puts(log);
        }

        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/Reports", PlayerReports);
        }

        public class Report
        {
            public string TargetName;
            public string ReporterName;
            public ulong TargetID;
            public ulong ReporterID;
            public string ReportReason;
        }
        public class UserReportData
        {
            public string UserName;
            public List<Report> Reports;
        }
        Dictionary<ulong, UserReportData> PlayerReports = new Dictionary<ulong, UserReportData>();
        #endregion

        #region Logging
        void VKLog(string Message)
        {
            Puts("Send");
            int RandomID = UnityEngine.Random.Range(0, 9999);
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?peer_id={config.TGCHATID}&random_id={RandomID}&message={URLEncode(Message)}&access_token={config.TGTOKEN}&v=5.131", null, (code, response) => { }, this);
            Puts(Message);
            //webrequest.Enqueue($"https://api.telegram.org/bot{config.TGTOKEN}/sendMessage?chat_id={config.TGCHATID}&text={URLEncode(Message)}", null, (code, response) => { }, this);
        }
        public string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }
        #endregion

        #region BanSystem WIP
        [ConsoleCommand("addban")]
        void AddBanCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2)
            {
                Puts("Usage: addban <steamid> <reason> [time]");
                return;
            }

            string steamid = arg.Args[0];
            string reason = arg.Args[1];
            string time = arg.Args.Length > 2 ? arg.Args[2] : "";

            ulong id;
            if (!ulong.TryParse(steamid, out id))
            {
                Puts("Invalid SteamID.");
                return;
            }

            long expiry = 0;
            if (!string.IsNullOrEmpty(time))
            {
                expiry = CalculateExpiry(time);
                if (expiry == 0)
                {
                    Puts("Invalid time format. Use '7d' for days or '30m' for minutes.");
                    return;
                }
            }

            expiry = expiry > 0 ? expiry : -1;

            webrequest.Enqueue($"http://localhost/API/banlist.php/?action=ban&steamid={id}&reason={reason}&time={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}&unbanDate={expiry}", null, (code, response) =>
            {
                if (code == 200)
                {
                    Puts($"Player with SteamID {steamid} banned successfully.");
                }
                else
                {
                    Puts($"Failed to ban player with SteamID {steamid}. Response code: {code}");
                }
            }, this);

            string cmd = $"ban {steamid} {reason} {time}";
            rust.RunServerCommand(cmd);
        }

        long CalculateExpiry(string time)
        {
            if (time.EndsWith("d", true, CultureInfo.InvariantCulture))
            {
                if (int.TryParse(time.TrimEnd('d', 'D'), out int days))
                {
                    return DateTimeOffset.UtcNow.AddDays(days).ToUnixTimeSeconds();
                }
            }
            else if (time.EndsWith("m", true, CultureInfo.InvariantCulture))
            {
                if (int.TryParse(time.TrimEnd('m', 'M'), out int minutes))
                {
                    return DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds();
                }
            }

            return 0;
        }
        #endregion
    }
}