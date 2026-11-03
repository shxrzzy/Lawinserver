using System.Globalization;
using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class TimelineRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/fortnite/api/calendar/v1/timeline", GetTimeline);
    }

    private static async Task GetTimeline(HttpContext ctx)
    {
        var memory = Functions.GetVersionInfo(ctx);

        var activeEvents = new List<JsonObject>();
        void AddEvent(string eventType, string activeUntil = "9999-01-01T00:00:00.000Z", string activeSince = "2020-01-01T00:00:00.000Z")
        {
            activeEvents.Add(new JsonObject
            {
                ["eventType"] = eventType,
                ["activeUntil"] = activeUntil,
                ["activeSince"] = activeSince
            });
        }

        AddEvent($"EventFlag.Season{memory.Season}");
        AddEvent($"EventFlag.{memory.Lobby}");

        switch (memory.Season)
        {
            case 3:
                AddEvent("EventFlag.Spring2018Phase1");
                if (memory.Build >= 3.1) AddEvent("EventFlag.Spring2018Phase2");
                if (memory.Build >= 3.3) AddEvent("EventFlag.Spring2018Phase3");
                if (memory.Build >= 3.4) AddEvent("EventFlag.Spring2018Phase4");
                break;

            case 4:
                AddEvent("EventFlag.Blockbuster2018");
                AddEvent("EventFlag.Blockbuster2018Phase1");
                if (memory.Build >= 4.3) AddEvent("EventFlag.Blockbuster2018Phase2");
                if (memory.Build >= 4.4) AddEvent("EventFlag.Blockbuster2018Phase3");
                if (memory.Build >= 4.5) AddEvent("EventFlag.Blockbuster2018Phase4");
                break;

            case 5:
                AddEvent("EventFlag.RoadTrip2018");
                AddEvent("EventFlag.Horde");
                AddEvent("EventFlag.Anniversary2018_BR");
                AddEvent("EventFlag.LTM_Heist");
                if (memory.Build == 5.10) AddEvent("EventFlag.BirthdayBattleBus");
                break;

            case 6:
                AddEvent("EventFlag.LTM_Fortnitemares");
                AddEvent("EventFlag.LTM_LilKevin");
                if (memory.Build >= 6.20)
                {
                    AddEvent("EventFlag.Fortnitemares");
                    AddEvent("EventFlag.FortnitemaresPhase1");
                    AddEvent("POI0");
                }
                if (memory.Build >= 6.22) AddEvent("EventFlag.FortnitemaresPhase2");
                if (memory.Build == 6.20 || memory.Build == 6.21)
                {
                    AddEvent("EventFlag.LobbySeason6Halloween");
                    AddEvent("EventFlag.HalloweenBattleBus");
                }
                break;

            case 7:
                AddEvent("EventFlag.Frostnite");
                AddEvent("EventFlag.LTM_14DaysOfFortnite");
                AddEvent("EventFlag.LTE_Festivus");
                AddEvent("EventFlag.LTM_WinterDeimos");
                AddEvent("EventFlag.LTE_S7_OverTime");
                break;

            case 8:
                AddEvent("EventFlag.Spring2019");
                AddEvent("EventFlag.Spring2019.Phase1");
                AddEvent("EventFlag.LTM_Ashton");
                AddEvent("EventFlag.LTM_Goose");
                AddEvent("EventFlag.LTM_HighStakes");
                AddEvent("EventFlag.LTE_BootyBay");
                if (memory.Build >= 8.2) AddEvent("EventFlag.Spring2019Phase2");
                break;

            case 9:
                AddEvent("EventFlag.Season9.Phase1");
                AddEvent("EventFlag.Anniversary2019_BR");
                AddEvent("EventFlag.LTM_14DaysOfSummer");
                AddEvent("EventFlag.LTM_Mash");
                AddEvent("EventFlag.LTM_Wax");
                if (memory.Build >= 9.2) AddEvent("EventFlag.Season9.Phase2");
                break;

            case 10:
                AddEvent("EventFlag.Mayday");
                AddEvent("EventFlag.Season10.Phase2");
                AddEvent("EventFlag.Season10.Phase3");
                AddEvent("EventFlag.LTE_BlackMonday");
                AddEvent("EventFlag.S10_Oak");
                AddEvent("EventFlag.S10_Mystery");
                for (var i = 1; i <= 10; i++) AddEvent($"EventFlag.Season10_UrgentMission_{i:D2}");
                break;

            case 11:
                AddEvent("EventFlag.LTE_CoinCollectXP");
                AddEvent("EventFlag.LTE_Fortnitemares2019");
                AddEvent("EventFlag.LTE_Galileo_Feats");
                AddEvent("EventFlag.LTE_Galileo");
                AddEvent("EventFlag.LTE_WinterFest2019");
                if (memory.Build >= 11.2) AddEvent("EventFlag.Starlight");
                if (memory.Build < 11.3)
                {
                    if (memory.Build >= 11.01) AddEvent("EventFlag.Season11.Fortnitemares.Quests.Phase1");
                    if (memory.Build >= 11.10)
                    {
                        AddEvent("EventFlag.Season11.Fortnitemares.Quests.Phase2");
                        AddEvent("EventFlag.Season11.Fortnitemares.Quests.Phase3");
                        AddEvent("EventFlag.Season11.Fortnitemares.Quests.Phase4");
                        AddEvent("EventFlag.StormKing.Landmark");
                    }
                }
                else
                {
                    AddEvent("EventFlag.HolidayDeco");
                    AddEvent("EventFlag.Season11.WinterFest.Quests.Phase1");
                    AddEvent("EventFlag.Season11.WinterFest.Quests.Phase2");
                    AddEvent("EventFlag.Season11.WinterFest.Quests.Phase3");
                    AddEvent("EventFlag.Season11.Frostnite");
                }
                if (memory.Build == 11.31 || memory.Build == 11.40)
                {
                    AddEvent("EventFlag.Winterfest.Tree");
                    AddEvent("EventFlag.LTE_WinterFest");
                    AddEvent("EventFlag.LTE_WinterFest2019");
                }
                break;

            case 12:
                AddEvent("EventFlag.LTE_SpyGames");
                AddEvent("EventFlag.LTE_JerkyChallenges");
                AddEvent("EventFlag.LTE_Oro");
                AddEvent("EventFlag.LTE_StormTheAgency");
                break;

            case 14:
                AddEvent("EventFlag.LTE_Fortnitemares_2020");
                break;

            case 15:
                for (var i = 1; i <= 15; i++) AddEvent($"EventFlag.LTQ_S15_Legendary_Week_{i:D2}");
                AddEvent("EventFlag.Event_HiddenRole");
                AddEvent("EventFlag.Event_OperationSnowdown");
                AddEvent("EventFlag.Event_PlumRetro");
                break;

            case 16:
                for (var i = 1; i <= 12; i++) AddEvent($"EventFlag.LTQ_S16_Legendary_Week_{i:D2}");
                AddEvent("EventFlag.Event_NBA_Challenges");
                AddEvent("EventFlag.Event_Spire_Challenges");
                break;

            case 17:
                AddEvent("EventFlag.Event_TheMarch");
                AddEvent("EventFlag.Event_O2_Challenges");
                AddEvent("EventFlag.LTE_Buffet_PreQuests");
                AddEvent("EventFlag.LTE_Buffet_Attend");
                AddEvent("EventFlag.LTE_Buffet_PostQuests");
                AddEvent("EventFlag.LTE_Buffet_Cosmetics");
                AddEvent("EventFlag.Event_CosmicSummer");
                AddEvent("EventFlag.Event_IslandGames");
                for (var i = 1; i <= 14; i++) AddEvent($"EventFlag.LTQ_S17_Legendary_Week_{i:D2}");
                AddEvent("EventFlag.LTQ_S17_CB_Radio");
                AddEvent("EventFlag.LTQ_S17_Sneak_Week");
                AddEvent("EventFlag.LTQ_S17_Yeet_Week");
                AddEvent("EventFlag.LTQ_S17_Zap_Week");
                AddEvent("EventFlag.LTQ_S17_Bargain_Bin_Week");
                break;

            case 18:
                AddEvent("EventFlag.LTE_Season18_BirthdayQuests");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_06");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_07");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_08");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_09");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_10");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_11");
                AddEvent("EventFlag.LTQ_S18_Repeatable_Weekly_12");
                AddEvent("EventFlag.Event_Fornitemares_2021");
                AddEvent("EventFlag.Event_HordeRush");
                AddEvent("EventFlag.Event_SoundWave");
                AddEvent("EventFlag.LTE_Season18_TextileQuests");
                AddEvent("EventFlag.S18_WildWeek_Shadows");
                AddEvent("EventFlag.S18_WildWeek_Bargain");
                AddEvent("EventFlag.S18_Haste");
                break;

            case 19:
                AddEvent("EventFlag.LTM_Hyena");
                AddEvent("EventFlag.LTM_Vigilante");
                AddEvent("EventFlag.LTM_ZebraWallet");
                AddEvent("EventFlag.LTE_Galileo_Feats");
                AddEvent("EventFlag.Event_S19_Trey");
                AddEvent("EventFlag.Event_S19_DeviceQuestsPart1");
                AddEvent("EventFlag.Event_S19_DeviceQuestsPart2");
                AddEvent("EventFlag.Event_S19_DeviceQuestsPart3");
                AddEvent("EventFlag.Event_S19_Gow_Quests");
                AddEvent("EventFlag.Event_S19_MonarchLevelUpPack");
                AddEvent("EventFlag.S19_WinterfestCrewGrant");
                AddEvent("EventFlag.S19_WildWeeks_Chicken");
                AddEvent("EventFlag.S19_WildWeeks_BargainBin");
                AddEvent("EventFlag.S19_WildWeeks_Spider");
                AddEvent("EventFlag.S19_WildWeeks_Primal");
                if (memory.Build == 19.01)
                {
                    AddEvent("EventFlag.LTE_WinterFest");
                    AddEvent("WF_IG_AVAIL");
                }
                break;

            case 20:
                AddEvent("Event_S20_AliQuest");
                AddEvent("Event_S20_EmicidaQuest");
                AddEvent("EventFlag.S20_WildWeeks_Bargain");
                AddEvent("Event_S20_WildWeeks_Chocolate");
                AddEvent("Event_S20_WildWeeks_Purple");
                AddEvent("EventFlag.CovertOps_Phase1");
                AddEvent("EventFlag.CovertOps_Phase2");
                AddEvent("EventFlag.CovertOps_Phase3");
                AddEvent("EventFlag.CovertOps_Phase4");
                AddEvent("Event_S20_LevelUpPack");
                AddEvent("Event_S20_May4thQuest");
                AddEvent("EventFlag.NoBuildQuests");
                AddEvent("EventFlag.Event_S20_NoBuildQuests_TokenGrant");
                break;

            case 21:
                AddEvent("Event_S21_FallFest");
                AddEvent("Event_S21_IslandHopper");
                AddEvent("Event_S21_CRRocketQuest");
                AddEvent("Event_S21_GenQuest");
                AddEvent("Event_S21_Stamina");
                AddEvent("EventFlag.Event_S21_LevelUpPack");
                AddEvent("EventFlag.Event_NoSweatSummer");
                AddEvent("Event_S21_WildWeeks_BargainBin");
                AddEvent("Event_S21_WildWeeks_Fire");
                AddEvent("Event_S21_WildWeeks_Kondor");
                break;

            case 22:
                AddEvent("EventFlag.Event_S22_BirthdayQuests");
                AddEvent("EventFlag.Event_S22_DistantEcho");
                AddEvent("EventFlag.Event_S22_AyaQuest");
                AddEvent("EventFlag.Event_S22_FNCSQuest");
                AddEvent("EventFlag.S22FortnitemaresQuests");
                AddEvent("EventFlag.S22Headset");
                AddEvent("EventFlag.S22HordeRush");
                AddEvent("EventFlag.S22VistaQuest");
                AddEvent("EventFlag.Event_S22_LevelUpPack");
                AddEvent("EventFlag.Event_S22NarrativePart1");
                AddEvent("EventFlag.Event_S22_RebootRally");
                AddEvent("EventFlag.S22_WildWeek_Avian");
                AddEvent("EventFlag.S22_WildWeek_Bargain");
                break;

            case 23:
                for (var i = 1; i <= 12; i++) AddEvent($"EventFlag.Event_S23_Weekly_{i:D2}");
                AddEvent("EventFlag.Event_S23Cipher");
                AddEvent("EventFlag.Event_S23_Emerald");
                AddEvent("EventFlag.Event_S23_FindIt");
                AddEvent("EventFlag.Event_S23_CreedQuests");
                AddEvent("EventFlag.Event_S23_Lettuce");
                AddEvent("EventFlag.Event_S23_LevelUpPack");
                AddEvent("EventFlag.Event_S23NarrativePart1");
                AddEvent("EventFlag.Event_S23NarrativePart2");
                AddEvent("EventFlag.Event_S23NarrativePart3");
                AddEvent("EventFlag.Event_S23NarrativePart4");
                AddEvent("EventFlag.Event_S23NarrativePart5");
                AddEvent("EventFlag.Event_S23NarrativePart4BonusGoal");
                AddEvent("EventFlag.Event_S23_RebootRally");
                AddEvent("EventFlag.Event_S23_SunBurst");
                AddEvent("EventFlag.Event_S23_ZeroWeek");
                if (memory.Build == 23.10)
                {
                    AddEvent("EventFlag.LTE_WinterFest");
                    AddEvent("EventFlag.LTE_WinterFestTab");
                    AddEvent("WF_GUFF_AVAIL");
                }
                break;

            case 24:
                AddEvent("EventFlag.Event_S24_SpringFling");
                AddEvent("EventFlag.Event_S24_TigerRoot");
                AddEvent("EventFlag.Event_S24_Epicenter");
                AddEvent("EventFlag.Event_S24_SunBurst");
                AddEvent("EventFlag.Event_S24_LevelUpPack");
                AddEvent("EventFlag.Event_S24_NarrativeQuests");
                AddEvent("EventFlag.Event_S24_RebootRally");
                break;

            case 25:
                AddEvent("EventFlag.Event_S25_14DOS");
                AddEvent("EventFlag.Event_S25_AloeCrouton");
                AddEvent("EventFlag.Event_S25_LevelUpPack");
                AddEvent("EventFlag.Event_S25_Maze");
                AddEvent("EventFlag.Event_S25_RebootRally");
                break;

            case 26:
                AddEvent("EventFlag.Event_S26_BirthdayQuests");
                AddEvent("EventFlag.Event_S26_FNM");
                AddEvent("EventFlag.Event_S26_Mash");
                AddEvent("EventFlag.Event_S26_Intertwine");
                AddEvent("EventFlag.Event_S26_LevelUpPack");
                break;

            case 27:
                AddEvent("EventFlag.Event_S27_RebootRally");
                break;

            case 28:
                AddEvent("EventFlag.Event_S28_LevelUpPass");
                AddEvent("EventFlag.Event_S28_Prelude");
                AddEvent("EventFlag.Event_S28_RebootRally");
                AddEvent("EventFlag.Event_S28_Winterfest");
                break;

            case 29:
                AddEvent("Event_S29_ColdDayPrelude");
                AddEvent("EventFlag.Event_S29_LevelUpPass");
                AddEvent("EventFlag.Event_S29_RebootRally");
                AddEvent("EventFlag.Event_S29_SeasonalActivation");
                AddEvent("EventFlag.Event_S29_WhiplashWW");
                break;

            case 30:
                AddEvent("EventFlag.Event_S30_FlatWare");
                AddEvent("Event_GreenhousePrelude");
                AddEvent("EventFlag.Event_S30_LevelUpPass");
                AddEvent("EventFlag.Event_S30_RebootRally");
                AddEvent("EventFlag.Event_S30_StoryQuests");
                AddEvent("EventFlag.Event_S30_StoryQuests_P6");
                AddEvent("EventFlag.Event_S30_AllSweat");
                break;

            case 31:
                AddEvent("EventFlag.Event_S31_Birthday");
                AddEvent("EventFlag.Event_S31_QuestDeckBed");
                AddEvent("Event_S31_FoundQuests_GreenTown");
                AddEvent("Event_S31_FoundQuests_PB_P01");
                AddEvent("Event_S31_FoundQuests_PB_P02");
                AddEvent("EventFlag.Event_S31_FoundQuests_TroutWrist");
                AddEvent("EventFlag.Event_S31_LevelUpPass");
                AddEvent("EventFlag.Event_S31_Mash");
                AddEvent("EventFlag.Event_S31_MobileQuests");
                AddEvent("EventFlag.Event_S31_RebootRally");
                AddEvent("EventFlag.Event_S31_Sweatember");
                AddEvent("EventFlag.Event_S31_ToadJam");
                break;

            case 32:
                AddEvent("EventFlag.Event_S32_RebootRally");
                AddEvent("EventFlag.Event_S32_ScytheGold");
                break;

            case 33:
                if (memory.Build == 33.11)
                {
                    AddEvent("EventFlag.Winterfest_S33_CabinRewards");
                    AddEvent("EventFlag.S33_WinterFestTab");
                    AddEvent("WF_GUFF_AVAIL");
                    AddEvent("EventFlag.S33_WinterFest");
                }
                AddEvent("EventFlag.Event_S33_BlondeJaw");
                AddEvent("EventFlag.Event_S33_Scenario_BatterBoi");
                AddEvent("EventFlag.Event_Scenario_Shell");
                AddEvent("EventFlag.Event_S33_NarrativeBattleQuest");
                AddEvent("EventFlag.Event_S33_ChatQuest");
                break;

            case 34:
                AddEvent("EventFlag.Event_S34_Bling");
                AddEvent("EventFlag.Event_S34_CollectionQuests");
                AddEvent("EventFlag.Event_S34_Lanternfest");
                AddEvent("EventFlag.Event_S34_LTE_Quests");
                AddEvent("EventFlag.Event_S34_SummerRoadtrip_Quests");
                AddEvent("EventFlag.Event_S34_RoomCharge");
                AddEvent("EventFlag.Event_S34_SprocketPoppy");
                AddEvent("EventFlag.Event_S34_StoryQuests_EOS_01");
                AddEvent("EventFlag.Event_S34_StoryQuests_EOS_02");
                AddEvent("EventFlag.Event_S34_StoryQuests_P1");
                AddEvent("EventFlag.Event_S34_WeaponExpertise");
                AddEvent("EventFlag.Event_S34_WeaponExpertiseSniper");
                break;

            case 35:
                AddEvent("EventFlag.Event_S35_FoundQuests");
                AddEvent("EventFlag.Event_S35_MidSeasonCharacter_Quests");
                AddEvent("EventFlag.Event_S35_SummerRoadtrip_Quests");
                break;

            case 36:
                AddEvent("EventFlag.Event_S36_IslandStories");
                AddEvent("EventFlag.Event_S36_MidSeasonCharacter_Quests");
                AddEvent("EventFlag.Event_S36_SummerRoadtrip_Quests");
                AddEvent("EventFlag.Event_PistolCanary_Quests");
                AddEvent("EventFlag.Event_PistolCanary_Quests_After");
                AddEvent("EventFlag.Event_PistolCanary_Quests_After_2");
                AddEvent("EventFlag.Event_S36_14Days");
                AddEvent("EventFlag.Event_S36_WeaponExpertise");
                break;

            case 37:
                AddEvent("EventFlag.Event_S37_ArrowTube");
                AddEvent("EventFlag.Event_S37_CommunityQuests");
                AddEvent("EventFlag.Event_S37_Delulu");
                AddEvent("EventFlag.Event_S37_PulseLancer");
                AddEvent("EventFlag.Event_S37_WeaponExpertise");
                AddEvent("EventFlag.Event_S37_Mash");
                AddEvent("EventFlag.Event_S37_Birthday");
                break;

            case 38:
                AddEvent("EventFlag.Event_S38_MSBPC");
                AddEvent("EventFlag.Event_S38_Cinematic");
                AddEvent("EventFlag.Event_S38_Delulu");
                AddEvent("EventFlag.Event_S38_Stories_P01");
                AddEvent("EventFlag.Event_S38_Stories_P02");
                AddEvent("EventFlag.Event_S38_Stories_P03");
                AddEvent("EventFlag.Event_S38_Stories_P04");
                AddEvent("EventFlag.Event_S38_Stories");
                break;

            case 39:
                if (memory.Build == 39.11)
                {
                    AddEvent("EventFlag.Winterfest_S39_CabinRewards");
                    AddEvent("EventFlag.S39_WinterFestTab");
                    AddEvent("WF_GUFF_AVAIL");
                    AddEvent("EventFlag.S39_WinterFest");
                }
                AddEvent("EventFlag.Event_S39_BattleBus");
                AddEvent("EventFlag.Event_S39_HoneyFiber");
                AddEvent("EventFlag.Event_S39_Story_P01");
                AddEvent("EventFlag.Event_S39_Story_P02");
                AddEvent("EventFlag.Event_S39_Story_P03");
                AddEvent("EventFlag.Event_S39_Story_P04");
                AddEvent("EventFlag.Event_S39_UmbraMolt");
                AddEvent("EventFlag.Event_S39_LnL_DailyLogin_P01");
                AddEvent("EventFlag.Event_S39_LnL_DailyLogin_P02");
                break;

            case 40:
                AddEvent("EventFlag.Event_S40_BR_CommunityDay_Quests");
                AddEvent("EventFlag.Event_S40_BR_Elites_Quests");
                AddEvent("EventFlag.Event_S40_LlamaRama");
                AddEvent("EventFlag.Event_S40_SpearShawl");
                AddEvent("EventFlag.Event_S40_STWPromo");
                AddEvent("EventFlag.Event_HulkShip_General");
                AddEvent("EventFlag.Event_HulkShip_Island_01");
                AddEvent("EventFlag.Event_HulkShip_Island_02");
                AddEvent("EventFlag.Event_HulkShip_Island_03");
                AddEvent("EventFlag.Event_HulkShip_Shiba");
                break;

            case 41:
                AddEvent("EventFlag.PearlTaffy_Quests");
                AddEvent("EventFlag.Event_S41_SpriteMastery_P01");
                AddEvent("EventFlag.Event_S41_SpriteMastery_P02");
                AddEvent("EventFlag.Event_S41_SpriteMastery_P03");
                AddEvent("EventFlag.Event_S41_SpriteMastery_P04");
                AddEvent("EventFlag.41_30_Story_P01");
                AddEvent("EventFlag.41_30_Story_P02");
                AddEvent("EventFlag.41_30_Story_P03");
                AddEvent("EventFlag.41_30_Story_P04");
                AddEvent("EventFlag.Event_S41_Story_P01");
                AddEvent("EventFlag.Event_S41_Story_P02");
                AddEvent("EventFlag.Event_S41_Story_P03");
                break;

            case 42:
                AddEvent("EventFlag.Event_S42_BlockStackCosmetics");
                AddEvent("EventFlag.Event_S42_SpriteMastery_P01");
                AddEvent("EventFlag.Event_S42_SpriteMastery_P02");
                AddEvent("EventFlag.Event_S42_SpriteMastery_P03");
                AddEvent("EventFlag.Event_S42_SpriteMastery_P04");
                AddEvent("EventFlag.Event_S42_SpriteMastery_P05");
                AddEvent("EventFlag.Event_S42_CosmicThunder_TutorialQuests");
                AddEvent("EventFlag.Event_S42_NarrowFlea");
                AddEvent("EventFlag.PreviousSeason_SpriteDust");
                AddEvent("EventFlag.ProgressionTrack_S42");
                AddEvent("EventFlag.Event_S42_Audiologs_4200");
                AddEvent("EventFlag.Event_S42_Story_BlockStack_BonusGoals");
                AddEvent("EventFlag.Event_S42_Story_BlockStack_P01");
                AddEvent("EventFlag.Event_S42_Story_BlockStack_P02");
                AddEvent("EventFlag.Event_S42_Story_HeroicTale_BonusGoals");
                AddEvent("EventFlag.Event_S42_Story_HeroicTale");
                AddEvent("EventFlag.Event_S42_Story_PhoenixKick_BonusGoals");
                AddEvent("EventFlag.Event_S42_Story_PhoenixKick");
                AddEvent("EventFlag.Event_S42_Story_SheerWill_BonusGoals");
                AddEvent("EventFlag.Event_S42_Story_SheerWill_P01");
                AddEvent("EventFlag.Event_S42_Story_SheerWill_P02");
                break;
        }

        if (memory.Build >= 24.3 && memory.Build <= 25)
        {
            AddEvent("EventFlag.HordeV3");
            AddEvent("EventFlag.HordeV3.Week02");
            AddEvent("EventFlag.HordeV3.Week03");
            AddEvent("EventFlag.HordeV3.Week04");
            AddEvent("EventFlag.HordeV3.Week05");
            AddEvent("EventFlag.HordeV3.Week06");
            AddEvent("EventFlag.HordeV3.Week07");
            AddEvent("EventFlag.HordeV3.Week08");
        }

        if (AppConfig.GetBool("Profile", "bAllSTWEventsActivated", true) && memory.Season >= 13)
        {
            var events = new List<string>
            {
                "EventFlag.Blockbuster2018", "EventFlag.Blockbuster2018Phase1", "EventFlag.Blockbuster2018Phase2", "EventFlag.Blockbuster2018Phase3", "EventFlag.Blockbuster2018Phase4",
                "EventFlag.Fortnitemares", "EventFlag.FortnitemaresPhase1", "EventFlag.FortnitemaresPhase2", "EventFlag.Frostnite", "EventFlag.HolidayDeco",
                "EventFlag.Horde", "EventFlag.HordeV3", "EventFlag.Mayday", "EventFlag.Outpost", "EventFlag.Phoenix.Adventure",
                "EventFlag.Phoenix.Fortnitemares", "EventFlag.Phoenix.Fortnitemares.Clip", "EventFlag.Phoenix.NewBeginnings", "EventFlag.Phoenix.NewBeginnings.SpringTraining",
                "EventFlag.Phoenix.RoadTrip", "EventFlag.Phoenix.Winterfest", "EventFlag.Phoenix.Winterfest.GhostOfChristmas", "EventFlag.RoadTrip2018",
                "EventFlag.STWBrainstorm", "EventFlag.STWFennix", "EventFlag.STWIrwin", "EventFlag.STWMiniBoss", "EventFlag.Season10.Phase2", "EventFlag.Season10.Phase3",
                "EventFlag.Season11.Fortnitemares.Quests.Phase1", "EventFlag.Season11.Fortnitemares.Quests.Phase2", "EventFlag.Season11.Fortnitemares.Quests.Phase3",
                "EventFlag.Season11.Fortnitemares.Quests.Phase4", "EventFlag.Season11.Frostnite", "EventFlag.Season11.WinterFest.Quests.Phase1",
                "EventFlag.Season11.WinterFest.Quests.Phase2", "EventFlag.Season11.WinterFest.Quests.Phase3", "EventFlag.Season12.NoDancing.Quests",
                "EventFlag.Season12.Spies.Quests", "EventFlag.Season9.Phase1", "EventFlag.Season9.Phase2", "EventFlag.Spring2018Phase1",
                "EventFlag.Spring2018Phase2", "EventFlag.Spring2018Phase3", "EventFlag.Spring2018Phase4", "EventFlag.Spring2019", "EventFlag.Spring2019.Phase1",
                "EventFlag.Spring2019.Phase2", "EventFlag.Spring2026", "EventFlag.Starlight", "EventFlag.StormKing.Landmark", "EventFlag.STWHuntMonster",
                "EventFlag.STWOutlandish", "EventFlag.YarrrTwo"
            };

            var activeEventTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in activeEvents) activeEventTypes.Add(J.AsString(e["eventType"]));
            foreach (var e in events)
            {
                if (!activeEventTypes.Contains(e))
                {
                    AddEvent(e);
                    activeEventTypes.Add(e);
                }
            }
        }

        var stateTemplate = new JsonObject
        {
            ["activeStorefronts"] = new JsonArray(),
            ["eventNamedWeights"] = new JsonObject(),
            ["seasonNumber"] = memory.Season,
            ["seasonTemplateId"] = $"AthenaSeason:athenaseason{memory.Season}",
            ["matchXpBonusPoints"] = 0,
            ["seasonBegin"] = "2020-01-01T13:00:00Z",
            ["seasonEnd"] = "9999-01-01T14:00:00Z",
            ["seasonDisplayedEnd"] = "9999-01-01T07:30:00Z",
            ["weeklyStoreEnd"] = "9999-01-01T00:00:00Z",
            ["stwEventStoreEnd"] = "9999-01-01T00:00:00.000Z",
            ["stwWeeklyStoreEnd"] = "9999-01-01T00:00:00.000Z",
            ["sectionStoreEnds"] = new JsonObject { ["Featured"] = "9999-01-01T00:00:00.000Z" },
            ["dailyStoreEnd"] = "9999-01-01T00:00:00Z"
        };

        var states = new List<JsonObject>
        {
            new JsonObject
            {
                ["validFrom"] = "2020-01-01T00:00:00.000Z",
                ["activeEvents"] = EventArray(activeEvents),
                ["state"] = stateTemplate.DeepClone()
            }
        };

        JsonObject NewState(string validFrom, List<JsonObject> events)
        {
            return new JsonObject
            {
                ["validFrom"] = validFrom,
                ["activeEvents"] = EventArray(events),
                ["state"] = stateTemplate.DeepClone()
            };
        }

        if (memory.Build == 4.5)
        {
            if (AppConfig.GetBool("Events", "bEnableGeodeEvent", false))
            {
                var geodeStart = AppConfig.Get("Events", "geodeEventStartDate", "2020-01-01T00:00:00.000Z");
                var geodeState0 = (JsonObject)states[0];
                geodeState0["activeEvents"] = EventArray(activeEvents);
                ((JsonArray)geodeState0["activeEvents"]).Add(new JsonObject { ["eventType"] = "EventFlag.BR_S4_Geode_Countdown", ["activeUntil"] = geodeStart });
                states.Add(NewState(geodeStart, activeEvents));

                var geodeEnd = AddMinutes(geodeStart, 3);
                var geodeState = (JsonObject)states[1];
                geodeState["activeEvents"] = EventArray(activeEvents);
                ((JsonArray)geodeState["activeEvents"]).Add(new JsonObject { ["eventType"] = "EventFlag.BR_S4_Geode_Begin", ["activeUntil"] = geodeEnd });
                activeEvents.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S4_Geode_Over", ["activeUntil"] = "9999-01-01T00:00:00.000Z" });
                if (AppConfig.GetBool("Events", "bEnableCrackInTheSky", false))
                {
                    activeEvents.Add(new JsonObject
                    {
                        ["eventType"] = "EventFlag.BR_S4_Rift_Growth",
                        ["activeUntil"] = AddMinutes(geodeEnd, 13.6 * 24 * 60)
                    });
                }
                states.Add(NewState(geodeEnd, activeEvents));
            }

            if (AppConfig.GetBool("Events", "bEnableS4OddityPrecursor", false))
            {
                for (var i = 1; i <= 8; i++)
                {
                    var start = AddMinutes(AppConfig.Get("Events", "S4OddityEventStartDate", "2020-01-01T00:00:00.000Z"), AppConfig.GetDouble("Events", "S4OddityEventsInterval", 0) * (i - 1));
                    activeEvents.Add(new JsonObject { ["eventType"] = $"EventFlag.BR_S4_Oddity_0{i}_Tell", ["activeUntil"] = start });
                }
                var lastState = (JsonObject)states[states.Count - 1];
                lastState["activeEvents"] = EventArray(activeEvents);
            }

            if (AppConfig.GetBool("Events", "bEnableS4OddityExecution", false))
            {
                for (var i = 1; i <= 8; i++)
                {
                    var start = AddMinutes(AppConfig.Get("Events", "S4OddityEventStartDate", "2020-01-01T00:00:00.000Z"), AppConfig.GetDouble("Events", "S4OddityEventsInterval", 0) * (i - 1));
                    activeEvents.Add(new JsonObject { ["eventType"] = $"EventFlag.BR_S4_Oddity_0{i}_Event", ["activeUntil"] = "9999-01-01T00:00:00.000Z" });
                    var tellIndex = -1;
                    for (var j = 0; j < activeEvents.Count; j++)
                    {
                        if (J.AsString(activeEvents[j]["eventType"]) == $"EventFlag.BR_S4_Oddity_0{i}_Tell") { tellIndex = j; break; }
                    }
                    if (tellIndex >= 0) activeEvents.RemoveAt(tellIndex);
                    states.Add(NewState(start, activeEvents));
                }
            }
        }

        if (memory.Build == 5.21)
        {
            if (AppConfig.GetBool("Events", "bEnableS5OddityPrecursor", false))
            {
                states.Add(NewState(AppConfig.Get("Events", "S5OddityPrecursorDate", "2020-01-01T00:00:00.000Z"), activeEvents));
                var secondState = (JsonObject)states[1];
                secondState["activeEvents"] = EventArray(activeEvents);
                ((JsonArray)secondState["activeEvents"]).Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Oddity_Tomato_Tell", ["activeUntil"] = "9999-01-01T00:00:00.000Z" });
            }
            if (AppConfig.GetBool("Events", "bEnableS5OddityExecution", false))
            {
                states.Add(NewState(AppConfig.Get("Events", "S5OddityExecutionDate", "2020-01-01T00:00:00.000Z"), activeEvents));
                var lastState2 = (JsonObject)states[states.Count - 1];
                lastState2["activeEvents"] = EventArray(activeEvents);
                ((JsonArray)lastState2["activeEvents"]).Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Oddity_Tomato_Event", ["activeUntil"] = "9999-01-01T00:00:00.000Z" });
            }
        }

        if (memory.Build == 5.30)
        {
            if (AppConfig.GetBool("Events", "bEnableBlockbusterRiskyEvent", false))
            {
                states[0]["activeEvents"] = EventArray(activeEvents);
            }
            if (AppConfig.GetBool("Events", "bEnableCubeLightning", false))
            {
                states[0]["activeEvents"] = EventArray(activeEvents);
                var st0 = (JsonObject)states[0];
                var st0Arr = (JsonArray)st0["activeEvents"];
                st0Arr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Rift_Corrupt", ["activeUntil"] = AppConfig.Get("Events", "cubeSpawnDate", "2020-01-01T00:00:00.000Z") });
                st0Arr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_Lightning", ["activeUntil"] = AppConfig.Get("Events", "cubeSpawnDate", "2020-01-01T00:00:00.000Z") });
                activeEvents.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_TurnOn", ["activeUntil"] = "9999-01-01T00:00:00.000Z" });
                states.Add(NewState(AppConfig.Get("Events", "cubeSpawnDate", "2020-01-01T00:00:00.000Z"), activeEvents));
            }
        }

        if (memory.Build == 5.41)
        {
            if (AppConfig.GetBool("Events", "bEnableCubeLake", false))
            {
                states[0]["activeEvents"] = EventArray(activeEvents);
                var st0b = (JsonObject)states[0];
                var st0bArr = (JsonArray)st0b["activeEvents"];
                st0bArr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_StartMove", ["activeUntil"] = AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z") });
                st0bArr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_TurnOn", ["activeUntil"] = AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z") });
                states.Add(NewState(AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z"), activeEvents));
                var secondState = (JsonObject)states[1];
                secondState["activeEvents"] = EventArray(activeEvents);
                var secondArr = (JsonArray)secondState["activeEvents"];
                secondArr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_StartMove", ["activeUntil"] = AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z") });
                secondArr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_TurnOn", ["activeUntil"] = AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z") });
                secondArr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_MoveTo8", ["activeUntil"] = AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z") });
                var end = AddMinutes(AppConfig.Get("Events", "cubeLakeDate", "2020-01-01T00:00:00.000Z"), 1.5);
                states.Add(NewState(end, activeEvents));
                var thirdState = (JsonObject)states[2];
                thirdState["activeEvents"] = EventArray(activeEvents);
                var thirdArr = (JsonArray)thirdState["activeEvents"];
                thirdArr.Add(new JsonObject { ["eventType"] = "EventFlag.BR_S5_Cube_Destination", ["activeUntil"] = "9999-01-01T00:00:00.000Z" });
            }
        }

        var clientEvents = new JsonObject
        {
            ["states"] = ToArray(states),
            ["cacheExpire"] = "9999-01-01T22:28:47.830Z"
        };

        var response = new JsonObject
        {
            ["channels"] = new JsonObject
            {
                ["client-matchmaking"] = new JsonObject { ["states"] = new JsonArray(), ["cacheExpire"] = "9999-01-01T22:28:47.830Z" },
                ["client-events"] = clientEvents
            },
            ["eventsTimeOffsetHrs"] = 0,
            ["cacheIntervalMins"] = 10,
            ["currentTime"] = Functions.IsoNow()
        };

        await ResponseUtil.Json(ctx, response);
    }

    private static JsonArray ToArray(List<JsonObject> items)
    {
        var arr = new JsonArray();
        foreach (var i in items) arr.Add(i);
        return arr;
    }

    private static JsonArray EventArray(List<JsonObject> events)
    {
        var arr = new JsonArray();
        foreach (var e in events) arr.Add(e);
        return arr;
    }

    private static string AddMinutes(string iso, double minutes)
    {
        var dt = DateTime.Parse(iso, CultureInfo.InvariantCulture);
        dt = dt.AddMinutes(minutes);
        return dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }
}

