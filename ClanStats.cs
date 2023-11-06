using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("ClanStats", "staticDev", "1.3.0")]
    [Description("Text based tracking system that depends on the clans plugin.")]
    class ClanStats : RustPlugin
    {

        #region Fields

        [PluginReference]
        private Plugin Clans;
        //Player stats by Ankawi
        #endregion
        Dictionary<ulong, HitInfo> LastWounded = new Dictionary<ulong, HitInfo>();

        static HashSet<PlayerData> LoadedPlayerData = new HashSet<PlayerData>();
        static HashSet<ClanData> LoadedClanData = new HashSet<ClanData>();
        List<UIObject> UsedUI = new List<UIObject>();

        #region UI Classes

        // UI Classes - Created by LaserHydra
        class UIColor
        {
            double red;
            double green;
            double blue;
            double alpha;

            public UIColor(double red, double green, double blue, double alpha)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
                this.alpha = alpha;
            }

            public override string ToString()
            {
                return $"{red.ToString()} {green.ToString()} {blue.ToString()} {alpha.ToString()}";
            }
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public UIObject()
            {
            }

            public string RandomString()
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine));
            }

            public void Destroy(BasePlayer player)
            {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", uiName);
            }

            public string AddPanel(string name, double left, double top, double width, double height, UIColor color, bool mouse = false, string parent = "Overlay")
            {
                name = name + RandomString();

                string type = "";
                if (mouse) type = "NeedsCursor";

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Image"},
                                {"color", color.ToString()}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            },
                            new Dictionary<string, string> {
                                {"type", type}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "Overlay", int alignmode = 0)
            {
                name = name + RandomString(); text = text.Replace("\n", "{NEWLINE}"); string align = "";

                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddButton(string name, double left, double top, double width, double height, UIColor color, string command = "", string parent = "Overlay", string closeUi = "")
            {
                name = name + RandomString();

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Button"},
                                {"close", closeUi},
                                {"command", command},
                                {"color", color.ToString()},
                                {"imagetype", "Tiled"}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddImage(string name, double left, double top, double width, double height, UIColor color, string url = "", string parent = "Overlay")
            {
                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Button"},
                                {"sprite", "assets/content/textures/generic/fulltransparent.tga"},
                                {"url", url},
                                {"color", color.ToString()},
                                {"imagetype", "Tiled"}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString().Replace(",", ".")} {((1 - top) - height).ToString().Replace(",", ".")}"},
                                {"anchormax", $"{(left + width).ToString().Replace(",", ".")} {(1 - top).ToString().Replace(",", ".")}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }
        #endregion

        #region Data
        class PlayerData
        {
            public ulong id;
            public string name;
            public string clanTag;
            public int kills;
            public int deaths;
            internal float KDR => deaths == 0 ? kills : (float)Math.Round(((float)kills) / deaths, 1);

            internal static void TryLoad(BasePlayer player)
            {
                if (Find(player) != null)
                    return;

                PlayerData data = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>($"ClanStats/player_data/{player.userID}");

                if (data == null || data.id == 0)
                {
                    data = new PlayerData
                    {
                        id = player.userID,
                        name = player.displayName,
                        kills = 0,
                        deaths = 0
                    };
                }
                else
                    data.Update(player);

                data.Save();
                LoadedPlayerData.Add(data);
            }

            internal void Update(BasePlayer player)
            {
                name = player.displayName;
                Save();
            }

            internal void Save() => Interface.Oxide.DataFileSystem.WriteObject($"ClanStats/player_data/{id}", this, true);
            internal static PlayerData Find(BasePlayer player)
            {

                PlayerData data = LoadedPlayerData.ToList().Find((p) => p.id == player.userID);

                return data;
            }
        }

       class ClanData
        {
            
            public string clanTag;
            public int clanKills;
            public int clanDeaths;
            internal float clanKDR => clanDeaths == 0 ? clanKills : (float)Math.Round(((float)clanKills) / clanDeaths, 1);

            internal static void TryLoadClans(string tag)
            {

                ClanData data = Interface.Oxide.DataFileSystem.ReadObject<ClanData>($"ClanStats/clan_data/{tag}");
                string tagToSet = tag;

                if (data != null)
                {   
                    data.clanTag = tag;
                    data.Save();
                    return;
                }
                else
                {
                    data = new ClanData

                    {
                        clanTag = tagToSet,
                        clanKills = 0,
                        clanDeaths = 0
                    };
                    data.Save();
                    LoadedClanData.Add(data);
                }
            }
            
            internal void Save() => Interface.Oxide.DataFileSystem.WriteObject($"ClanStats/clan_data/{clanTag}", this, true);
        }


        #endregion

        #region Hooks
        void OnPlayerConnected(BasePlayer player)
        {
            PlayerData.TryLoad(player);
            var clanTag = GetClanOf(player.userID);
            var data = PlayerData.Find(player);
            if (data != null)
            {
                if(data.clanTag != clanTag)
                {
                    data.clanTag = clanTag;
                    data.Save();
                }
                ClanData.TryLoadClans(data.clanTag);
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerData.TryLoad(player);
            var clanTag = GetClanOf(player.userID);
            var data = PlayerData.Find(player);
            if (data != null)
            {
                if(data.clanTag != clanTag)
                {
                    data.clanTag = clanTag;
                    data.Save();
                }
                ClanData.TryLoadClans(data.clanTag);
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                foreach (var ui in UsedUI)
                    ui.Destroy(player);
            }
        }

        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                PlayerData.TryLoad(player);
            }

        }
        HitInfo TryGetLastWounded(ulong id, HitInfo info)
        {
            if (LastWounded.ContainsKey(id))
            {
                HitInfo output = LastWounded[id];
                LastWounded.Remove(id);
                return output;
            }

            return info;
        }

        void OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (info?.Initiator?.ToPlayer() != null)
            {
                NextTick(() =>
                {
                    if (player.IsWounded())
                        LastWounded[player.userID] = info;
                });
            }
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            var clanTag = GetClanOf(player.userID);
            var data = PlayerData.Find(player);
            if (data != null)
            {
                if(data.clanTag != clanTag)
                {
                    data.clanTag = clanTag;
                    data.Save();
                }
                ClanData.TryLoadClans(data.clanTag);
            }

            try
            {
                if (player == info.Initiator) return;
                if (player == null || info.Initiator == null) return;

                if (player.IsWounded())
                {
                    info = TryGetLastWounded(player.userID, info);
                }
                if (info?.Initiator != null && info.Initiator is BasePlayer)
                {
                    PlayerData victimData = PlayerData.Find(player);
                    PlayerData attackerData = PlayerData.Find((BasePlayer)info.Initiator);

                    victimData.deaths++;
                    attackerData.kills++;

                    victimData.Save();
                    attackerData.Save();

                    if (!string.IsNullOrEmpty(victimData.clanTag))
                    {
                        ClanData victimClan = LoadedClanData.FirstOrDefault(clan => clan.clanTag == victimData.clanTag);
                        if (victimClan != null)
                        {
                            victimClan.clanDeaths++;
                            victimClan.Save();
                        }
                    }

                    if (!string.IsNullOrEmpty(attackerData.clanTag))
                    {
                        ClanData attackerClan = LoadedClanData.FirstOrDefault(clan => clan.clanTag == attackerData.clanTag);
                        if (attackerClan != null)
                        {
                            attackerClan.clanKills++;
                            attackerClan.Save();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        #endregion

        #region UI Handling
        //Revamp GUI to show switch for clans-players and always display current players stats and clan stats if applicable
        void DrawKDRWindow(BasePlayer player)
        {
            UIObject ui = new UIObject();
            string panel = ui.AddPanel("panel1", 0.0132382892057026, 0.0285714285714286, 0.958248472505092, 0.874285714285714, new UIColor(0, 0, 0, 1), true, "Overlay");
            ui.AddText("label8", 0.675876726886291, 0.248366013071895, 0.272051009564293, 0.718954248366013, new UIColor(1, 1, 1, 1), GetNames(), 24, panel, 7);
            ui.AddText("label7", 0.483528161530287, 0.248366013071895, 0.0563230605738576, 0.718954248366013, new UIColor(1, 1, 1, 1), GetKDRs(), 24, panel, 6);
            ui.AddText("label6", 0.269925611052072, 0.248366013071895, 0.0456960680127524, 0.718954248366013, new UIColor(1, 1, 1, 1), GetDeaths(), 24, panel, 6);
            ui.AddText("label5", 0.0786397449521785, 0.248366013071895, 0.0456960680127524, 0.718954248366013, new UIColor(1, 1, 1, 1), GetTopKills(), 24, panel, 6);
            string close = ui.AddButton("button1", 0.849096705632306, 0.0326797385620915, 0.124335812964931, 0.0871459694989107, new UIColor(1, 0, 0, 1), "", panel, panel);
            ui.AddText("button1_Text", 0, 0, 1, 1, new UIColor(0, 0, 0, 1), "Close", 19, close, 3);
            ui.AddText("label4", 0.470775770456961, 0.163398692810458, 0.0935175345377258, 0.0610021786492375, new UIColor(1, 0, 0, 1), "K/D Ratio", 24, panel, 7);
            ui.AddText("label3", 0.260361317747078, 0.163398692810458, 0.0722635494155154, 0.0610021786492375, new UIColor(1, 0, 0, 1), "Deaths", 24, panel, 7);
            ui.AddText("label2", 0.0786397449521785, 0.163398692810458, 0.0467587672688629, 0.0610021786492375, new UIColor(1, 0, 0, 1), "Kills", 24, panel, 7);
            ui.AddText("label1", 0.675876726886291, 0.163398692810458, 0.125398512221041, 0.0610021786492375, new UIColor(1, 0, 0, 1), "Player Name", 24, panel, 7);

            ui.Draw(player);
            UsedUI.Add(ui);
        }
        private void LoadSleepers()
        {
            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
                PlayerData.TryLoad(player);
        }
        string GetTopKills()
        {
            LoadSleepers();
            return string.Join("\n", LoadedPlayerData.OrderByDescending((d) => d.kills).Select((d) => $"{d.kills}").Take(15).ToArray());
        }
        string GetDeaths()
        {
            return string.Join("\n", LoadedPlayerData.OrderByDescending((d) => d.kills).Select((d) => $"{d.deaths}").Take(15).ToArray());
        }
        string GetKDRs()
        {
            return string.Join("\n", LoadedPlayerData.OrderByDescending((d) => d.kills).Select((d) => $"{d.KDR}").Take(15).ToArray());
        }
        string GetNames()
        {
            return string.Join("\n", LoadedPlayerData.OrderByDescending((d) => d.kills).Select((d) => $"{d.name}").Take(15).ToArray());
        }
        #endregion

        
        //add a new command to delete data, use WriteObject(null)
        #region Commands
        /*
        [ChatCommand("top")]
        void cmdTop(BasePlayer player, string command, string[] args)
        {
            DrawKDRWindow(player);
        }
        */

        [ChatCommand("myclan")]
        void cmdTop(BasePlayer player, string command, string[] args)
        {
            var clanTag = GetClanOf(player.userID);
            var data = PlayerData.Find(player);
            if (data != null)
            {
                if(data.clanTag != clanTag)
                {
                    data.clanTag = clanTag;
                    data.Save();
                }
                ClanData.TryLoadClans(data.clanTag);
            }
            int position = FindClanPosition(data.clanTag);
            ClanData clanData = Interface.Oxide.DataFileSystem.ReadObject<ClanData>($"ClanStats/clan_data/{data.clanTag}");
            PrintToChat(player, $"Your clan is #{position}. Kills: {clanData.clanKills}");
        }


        [ChatCommand("top")]
        void cmdMyclan(BasePlayer player, string command, string[] args)
        {
            var topClans = GetTopClans();
            if (topClans.Any())
            {
                PrintToChat(player, "<color=#fbff00>-------- Top Clans --------</color>");
                int position = 1;
                foreach (var clan in topClans)
                {
                    PrintToChat(player, $"{position}. Clan: {clan.Key}, Kills: {clan.Value}");
                    position++;
                }
            }
            else
            {
                PrintToChat(player, "No clans found.");
            }
        }

        private List<string> GetClanTags()
        {
            string clanDataPath = Interface.Oxide.DataFileSystem.GetFile("clan_data").Filename;
            string json = File.ReadAllText(clanDataPath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, ClanData>>>(json);

            List<string> clanTags = new List<string>();

            foreach (var clan in data["clans"])
            {
                clanTags.Add(clan.Key);
            }

            return clanTags;
        }

        private Dictionary<string, int> CreateClanKillList()
        {
            var clanTags = GetClanTags();
            var clanKillList = new Dictionary<string, int>();

            foreach (var tag in clanTags)
            {
                try
                {
                    ClanData data = Interface.Oxide.DataFileSystem.ReadObject<ClanData>($"ClanStats/clan_data/{tag}");
                    int clanKills = data.clanKills;

                    if (data != null)
                    {
                        clanKillList.Add(tag, clanKills);
                    }
                    else
                    {
                        Console.WriteLine($"No data found for clan tag {tag}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while processing clan tag {tag}: {ex.Message}");
                }
            }

            return clanKillList;
        }

        private Dictionary<string, int> GetTopClans()
        {
            var clanKillList = CreateClanKillList();
            var sortedClans = clanKillList.OrderByDescending(x => x.Value).Take(3).ToDictionary(x => x.Key, x => x.Value);
            return sortedClans;
        }

        private Dictionary<string, int> GetAllClans()
        {
            var clanKillList = CreateClanKillList();
            var sortedClans = clanKillList.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            return sortedClans;
        }

        private int FindClanPosition(string clanTag)
        {
            var topClans = GetAllClans();
            if (topClans.ContainsKey(clanTag))
            {
                var sortedClans = topClans.OrderByDescending(x => x.Value).ToList();
                for (int i = 0; i < sortedClans.Count; i++)
                {
                    if (sortedClans[i].Key == clanTag)
                    {
                        return i + 1; // Adding 1 to convert 0-based index to position
                    }
                }
            }
            return -1; // Clan not found
        }

        [ChatCommand("kdr")]
        void cmdKdr(BasePlayer player, string command, string[] args)
        {
            var clanTag = GetClanOf(player.userID);
            var data = PlayerData.Find(player);
            if (data != null)
            {
                if(data.clanTag != clanTag)
                {
                    data.clanTag = clanTag;
                    data.Save();
                }
                ClanData.TryLoadClans(data.clanTag);
            }
            GetCurrentStats(player);
        }

        void GetCurrentStats(BasePlayer player)
        {
            PlayerData data = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>($"ClanStats/player_data/{player.userID}");
            
            int kills = data.kills;
            int deaths = data.deaths;
            string playerName = data.name;
            float kdr = data.KDR;

            if (!string.IsNullOrEmpty(data.clanTag)) 
            {
                ClanData clanData = Interface.Oxide.DataFileSystem.ReadObject<ClanData>($"ClanStats/clan_data/{data.clanTag}");

                string clanTag = data.clanTag;
                int clanKills = clanData.clanKills;
                int clanDeaths = clanData.clanDeaths;
                float clanKDR = clanData.clanKDR;

                PrintToChat(player, "<color=#fbff00> Player Name : </color>" + $"{playerName}"
                        + "\n" + "<color=#ffffff> Kills : </color>" + $"{kills}"
                        + "\n" + "<color=#ffffff> Deaths : </color>" + $"{deaths}"
                        + "\n" + "<color=#ffffff> K/D Ratio : </color>" + $"{kdr}"
                        + "\n" + "<color=#00ffd0> Clan Stats : </color>" + $"{clanTag}"
                        + "\n" + "<color=#ffffff> Clan Kills : </color>" + $"{clanKills}"
                        + "\n" + "<color=#ffffff> Clan Deaths : </color>" + $"{clanDeaths}"
                        + "\n" + "<color=#ffffff> Clan K/D Ratio : </color>" + $"{clanKDR}");
            } else
            {
                PrintToChat(player, "<color=#fbff00> Player Name : </color>" + $"{playerName}"
                        + "\n" + "<color=#ffffff> Kills : </color>" + $"{kills}"
                        + "\n" + "<color=#ffffff> Deaths : </color>" + $"{deaths}"
                        + "\n" + "<color=#ffffff> K/D Ratio : </color>" + $"{kdr}"
                        + "\n" + "<color=#00ffd0> Clan Stats : </color>" + "Use '/clan create {tag}' to begin viewing clan data.");
            }
        }

        //Add way to get top 3 clans and their stats
        #endregion
        
        #region ClansMethod
        private string GetClanOf(ulong playerId)
        {
            if (Clans == null)
            {
                return string.Empty;
            }

            return (string)Clans.Call("GetClanOf", playerId);
        }

        #endregion
    }
}