﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAwareness.Detectors
{
    internal class Recall
    {
        public static Menu.MenuItemSettings RecallDetector = new Menu.MenuItemSettings(typeof(Recall));

        public List<Packet.S2C.Teleport.Struct> Recalls = new List<Packet.S2C.Teleport.Struct>();

        public Recall()
        {
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsEnemy)
                {
                    Recalls.Add(new Packet.S2C.Teleport.Struct(enemy.NetworkId, Packet.S2C.Teleport.Status.Unknown, Packet.S2C.Teleport.Type.Unknown, 0, 0));
                }
            }
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
        }

        ~Recall()
        {
            Recalls = null;
        }

        public bool IsActive()
        {
            return Detector.Detectors.GetActive() && RecallDetector.GetActive();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            RecallDetector.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("DETECTORS_RECALL_MAIN"), "SAwarenessDetectorsRecall"));
            RecallDetector.MenuItems.Add(
                RecallDetector.Menu.AddItem(new MenuItem("SAwarenessDetectorsRecallPingTimes", Language.GetString("GLOBAL_PING_TIMES")).SetValue(new Slider(0, 5, 0))));
            RecallDetector.MenuItems.Add(
                RecallDetector.Menu.AddItem(new MenuItem("SAwarenessDetectorsRecallLocalPing", Language.GetString("GLOBAL_PING_LOCAL")).SetValue(true)));
            RecallDetector.MenuItems.Add(
                RecallDetector.Menu.AddItem(new MenuItem("SAwarenessDetectorsRecallChatChoice", Language.GetString("GLOBAL_CHAT_CHOICE")).SetValue(
                        new StringList(new[]
                        {
                            Language.GetString("GLOBAL_CHAT_CHOICE_NONE"), 
                            Language.GetString("GLOBAL_CHAT_CHOICE_LOCAL"), 
                            Language.GetString("GLOBAL_CHAT_CHOICE_SERVER")
                        }))));
            RecallDetector.MenuItems.Add(
                RecallDetector.Menu.AddItem(new MenuItem("SAwarenessDetectorsRecallSpeech", Language.GetString("GLOBAL_VOICE")).SetValue(false)));
            RecallDetector.MenuItems.Add(
                RecallDetector.Menu.AddItem(new MenuItem("SAwarenessDetectorsRecallActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return RecallDetector;
        }

        private void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            if (!IsActive())
                return;
            try
            {
                Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
                HandleRecall(decoded);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RecallProcess: " + ex);
            }
        }

        private void HandleRecall(Packet.S2C.Teleport.Struct recallEx)
        {
            int time = Environment.TickCount - Game.Ping;

            for (int i = 0; i < Recalls.Count; i++)
            {
                Packet.S2C.Teleport.Struct recall = Recalls[i];
                if (true/*recallEx.Type == Recall.ObjectType.Player*/)
                {
                    var obj = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recall.UnitNetworkId);
                    var objEx = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recallEx.UnitNetworkId);
                    if (obj == null)
                        continue;
                    if (obj.NetworkId == objEx.NetworkId) //already existing
                    {
                        recall = recallEx;
                        //recall.Recall2 = new Recall.Struct();

                        var percentHealth = (int)((obj.Health / obj.MaxHealth) * 100);
                        String sColor = "<font color='#FFFFFF'>";
                        String color = (percentHealth > 50
                            ? "<font color='#00FF00'>"
                            : (percentHealth > 30 ? "<font color='#FFFF00'>" : "<font color='#FF0000'>"));
                        if (recallEx.Status == Packet.S2C.Teleport.Status.Start)
                        {
                            String text = (recallEx.Type == Packet.S2C.Teleport.Type.Recall
                                ? "recalling"
                                : "porting");
                            recall.Start = (int)Game.Time;
                            if (
                                RecallDetector.GetMenuItem("SAwarenessDetectorsRecallChatChoice")
                                    .GetValue<StringList>()
                                    .SelectedIndex == 1)
                            {
                                Game.PrintChat(obj.ChampionName + " {0} with {1} hp {2}({3})", text,
                                    (int)obj.Health, color, percentHealth);
                            }
                            else if (
                                RecallDetector.GetMenuItem("SAwarenessDetectorsRecallChatChoice")
                                    .GetValue<StringList>()
                                    .SelectedIndex == 2 &&
                                Menu.GlobalSettings.GetMenuItem("SAwarenessGlobalSettingsServerChatPingActive")
                                    .GetValue<bool>())
                            {
                                Game.Say(obj.ChampionName + " {0} with {1} hp {2}({3})", text, (int)obj.Health,
                                    color, percentHealth);
                            }
                            if (RecallDetector.GetMenuItem("SAwarenessDetectorsRecallSpeech").GetValue<bool>())
                            {
                                Speech.Speak(obj.ChampionName + " " + text);
                            }
                        }
                        else if (recallEx.Status == Packet.S2C.Teleport.Status.Finish)
                        {
                            String text = (recallEx.Type == Packet.S2C.Teleport.Type.Recall
                                ? "recalled"
                                : "ported");
                            if (
                                RecallDetector.GetMenuItem("SAwarenessDetectorsRecallChatChoice")
                                    .GetValue<StringList>()
                                    .SelectedIndex == 1)
                            {
                                Game.PrintChat(obj.ChampionName + " {0} with {1} hp {2}({3})", text,
                                    (int)obj.Health, color, percentHealth);
                            }
                            else if (
                                RecallDetector.GetMenuItem("SAwarenessDetectorsRecallChatChoice")
                                    .GetValue<StringList>()
                                    .SelectedIndex == 2 &&
                                Menu.GlobalSettings.GetMenuItem(
                                    "SAwarenessGlobalSettingsServerChatPingActive").GetValue<bool>())
                            {
                                Game.Say(obj.ChampionName + " {0} with {1} hp {2}({3})", text,
                                    (int)obj.Health, color, percentHealth);
                            }
                            if (RecallDetector.GetMenuItem("SAwarenessDetectorsRecallSpeech").GetValue<bool>())
                            {
                                Speech.Speak(obj.ChampionName + " " + text);
                            }
                        }
                        else
                        {
                            if (
                                RecallDetector.GetMenuItem("SAwarenessDetectorsRecallChatChoice")
                                    .GetValue<StringList>()
                                    .SelectedIndex == 1)
                            {
                                Game.PrintChat(obj.ChampionName + " canceled with {0} hp", (int)obj.Health);
                            }
                            else if (
                                RecallDetector.GetMenuItem("SAwarenessDetectorsRecallChatChoice")
                                    .GetValue<StringList>()
                                    .SelectedIndex == 2 &&
                                Menu.GlobalSettings.GetMenuItem(
                                    "SAwarenessGlobalSettingsServerChatPingActive").GetValue<bool>())
                            {
                                Game.Say(obj.ChampionName + " canceled with {0} hp", (int)obj.Health);
                            }
                            if (RecallDetector.GetMenuItem("SAwarenessDetectorsRecallSpeech").GetValue<bool>())
                            {
                                Speech.Speak(obj.ChampionName + " canceled");
                            }
                        }
                        return;
                    }
                }
            }
        }
    }
}
