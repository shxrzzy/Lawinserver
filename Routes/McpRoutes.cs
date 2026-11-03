using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class McpRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/fortnite/api/game/v2/profile/{accountId}/client/{action}", HandleClient);
        app.MapPost("/fortnite/api/game/v2/profile/{accountId}/dedicated_server/{action}", HandleDedicated);
    }

    private static async Task HandleClient(HttpContext ctx, string accountId, string action)
    {
        if (string.IsNullOrEmpty(RequestUtil.Query(ctx, "profileId")))
        {
            await ResponseUtil.Json(ctx, new JsonObject { ["error"] = "Profile not defined." }, 404);
            return;
        }

        UpdateSeasonData(ctx);
        ctx.Items["body"] = await RequestUtil.ReadObjectAsync(ctx);

        await McpCommon.Gate.WaitAsync();
        try
        {
            switch (action)
            {
                case "SetAffiliateName": await McpSimpleHandlers.SetAffiliateName(ctx); break;
                case "SetHomebaseBanner": await McpSimpleHandlers.SetHomebaseBanner(ctx); break;
                case "SetHomebaseName": await McpSimpleHandlers.SetHomebaseName(ctx); break;
                case "PurchaseHomebaseNode": await McpSimpleHandlers.PurchaseHomebaseNode(ctx); break;
                case "UnlockRewardNode": await McpWinterfestHandler.UnlockRewardNode(ctx); break;
                case "RemoveGiftBox": await McpSimpleHandlers.RemoveGiftBox(ctx); break;
                case "SetPartyAssistQuest": await McpSimpleHandlers.SetPartyAssistQuest(ctx); break;
                case "AthenaPinQuest": await McpSimpleHandlers.AthenaPinQuest(ctx); break;
                case "SetPinnedQuests": await McpSimpleHandlers.SetPinnedQuests(ctx); break;
                case "MarkNewQuestNotificationSent": await McpSimpleHandlers.MarkNewQuestNotificationSent(ctx); break;
                case "IncrementNamedCounterStat": await McpSimpleHandlers.IncrementNamedCounterStat(ctx); break;
                case "ClaimLoginReward": await McpSimpleHandlers.ClaimLoginReward(ctx); break;
                case "SetSeasonPassAutoClaim": await McpSimpleHandlers.SetSeasonPassAutoClaim(ctx); break;
                case "SetItemArchivedStatusBatch": await McpSimpleHandlers.SetItemArchivedStatusBatch(ctx); break;
                case "SetItemFavoriteStatusBatch": await McpSimpleHandlers.SetItemFavoriteStatusBatch(ctx); break;
                case "SetItemFavoriteStatus": await McpSimpleHandlers.SetItemFavoriteStatus(ctx); break;
                case "MarkItemSeen": await McpSimpleHandlers.MarkItemSeen(ctx); break;
                case "SetActiveArchetype": await McpSimpleHandlers.SetActiveArchetype(ctx); break;
                case "SetHeroCosmeticVariants": await McpSimpleHandlers.SetHeroCosmeticVariants(ctx); break;
                case "FortRerollDailyQuest": await McpQuestHandlers.FortRerollDailyQuest(ctx); break;
                case "ClientQuestLogin": await McpQuestHandlers.ClientQuestLogin(ctx); break;
                case "ClaimQuestReward": await McpQuestHandlers.ClaimQuestReward(ctx); break;
                case "RefundItem": await McpRefundHandlers.RefundItem(ctx); break;
                case "RefundMtxPurchase": await McpRefundHandlers.RefundMtxPurchase(ctx); break;
                case "AssignTeamPerkToLoadout": await McpLoadoutHandlers.AssignTeamPerkToLoadout(ctx); break;
                case "AssignGadgetToLoadout": await McpLoadoutHandlers.AssignGadgetToLoadout(ctx); break;
                case "AssignWorkerToSquad": await McpLoadoutHandlers.AssignWorkerToSquad(ctx); break;
                case "AssignWorkerToSquadBatch": await McpLoadoutHandlers.AssignWorkerToSquadBatch(ctx); break;
                case "ModifyQuickbar": await McpLoadoutHandlers.ModifyQuickbar(ctx); break;
                case "AssignHeroToLoadout": await McpLoadoutHandlers.AssignHeroToLoadout(ctx); break;
                case "AssignDefenderToLoadout": await McpLoadoutHandlers.AssignDefenderToLoadout(ctx); break;
                case "AssignWeaponToDefender": await McpLoadoutHandlers.AssignWeaponToDefender(ctx); break;
                case "ClearHeroLoadout": await McpLoadoutHandlers.ClearHeroLoadout(ctx); break;
                case "SetActiveHeroLoadout": await McpLoadoutHandlers.SetActiveHeroLoadout(ctx); break;
                case "UnassignAllSquads": await McpLoadoutHandlers.UnassignAllSquads(ctx); break;
                case "CraftWorldItem": await McpInventoryHandlers.CraftWorldItem(ctx); break;
                case "DestroyWorldItems": await McpInventoryHandlers.DestroyWorldItems(ctx); break;
                case "DisassembleWorldItems": await McpInventoryHandlers.DisassembleWorldItems(ctx); break;
                case "StorageTransfer": await McpInventoryHandlers.StorageTransfer(ctx); break;
                case "RecycleItemBatch": await McpInventoryHandlers.RecycleItemBatch(ctx); break;
                case "ActivateConsumable": await McpInventoryHandlers.ActivateConsumable(ctx); break;
                case "UpgradeItem": await McpItemUpgradeHandlers.UpgradeItem(ctx); break;
                case "UpgradeSlottedItem": await McpItemUpgradeHandlers.UpgradeSlottedItem(ctx); break;
                case "UpgradeItemBulk": await McpItemUpgradeHandlers.UpgradeItemBulk(ctx); break;
                case "ConvertItem": await McpItemUpgradeHandlers.ConvertItem(ctx); break;
                case "ConvertSlottedItem": await McpItemUpgradeHandlers.ConvertSlottedItem(ctx); break;
                case "UpgradeItemRarity": await McpItemUpgradeHandlers.UpgradeItemRarity(ctx); break;
                case "PromoteItem": await McpItemUpgradeHandlers.PromoteItem(ctx); break;
                case "TransmogItem": await McpItemUpgradeHandlers.TransmogItem(ctx); break;
                case "RedeemResearchToken": await McpItemUpgradeHandlers.RedeemResearchToken(ctx); break;
                case "RespecAlteration": await McpItemUpgradeHandlers.RespecAlteration(ctx); break;
                case "UpgradeAlteration": await McpItemUpgradeHandlers.UpgradeAlteration(ctx); break;
                case "RespecResearch": await McpItemUpgradeHandlers.RespecResearch(ctx); break;
                case "RespecUpgrades": await McpItemUpgradeHandlers.RespecUpgrades(ctx); break;
                case "PurchaseResearchStatUpgrade": await McpItemUpgradeHandlers.PurchaseResearchStatUpgrade(ctx); break;
                case "PurchaseOrUpgradeHomebaseNode": await McpItemUpgradeHandlers.PurchaseOrUpgradeHomebaseNode(ctx); break;
                case "ResearchItemFromCollectionBook": await McpCollectionHandlers.ResearchItemFromCollectionBook(ctx); break;
                case "SlotItemInCollectionBook": await McpCollectionHandlers.SlotItemInCollectionBook(ctx); break;
                case "UnslotItemFromCollectionBook": await McpCollectionHandlers.UnslotItemFromCollectionBook(ctx); break;
                case "ClaimCollectionBookRewards": await McpCollectionHandlers.ClaimCollectionBookRewards(ctx); break;
                case "RefreshExpeditions": await McpExpeditionHandlers.RefreshExpeditions(ctx); break;
                case "StartExpedition": await McpExpeditionHandlers.StartExpedition(ctx); break;
                case "AbandonExpedition": await McpExpeditionHandlers.AbandonExpedition(ctx); break;
                case "CollectExpedition": await McpExpeditionHandlers.CollectExpedition(ctx); break;
                case "OpenCardPack": await McpExpeditionHandlers.OpenCardPack(ctx); break;
                case "PopulatePrerolledOffers": await McpExpeditionHandlers.PopulatePrerolledOffers(ctx); break;
                case "EquipBattleRoyaleCustomization": await McpLockerHandlers.EquipBattleRoyaleCustomization(ctx); break;
                case "SetBattleRoyaleBanner": await McpLockerHandlers.SetBattleRoyaleBanner(ctx); break;
                case "SetCosmeticLockerBanner": await McpLockerHandlers.SetCosmeticLockerBanner(ctx); break;
                case "SetCosmeticLockerSlot": await McpLockerHandlers.SetCosmeticLockerSlot(ctx); break;
                case "PutModularCosmeticLoadout": await McpModularLoadoutHandler.PutModularCosmeticLoadout(ctx); break;
                case "PurchaseCatalogEntry": await McpPurchaseCatalogHandler.PurchaseCatalogEntry(ctx); break;
                default: await McpSimpleHandlers.GenericClient(ctx); break;
            }
        }
        finally
        {
            McpCommon.Gate.Release();
        }
    }

    private static async Task HandleDedicated(HttpContext ctx, string accountId, string action)
    {
        if (string.IsNullOrEmpty(RequestUtil.Query(ctx, "profileId")))
        {
            await ResponseUtil.Json(ctx, new JsonObject { ["error"] = "Profile not defined." }, 404);
            return;
        }

        UpdateSeasonData(ctx);
        ctx.Items["body"] = await RequestUtil.ReadObjectAsync(ctx);

        await McpCommon.Gate.WaitAsync();
        try
        {
            await McpSimpleHandlers.GenericDedicated(ctx);
        }
        finally
        {
            McpCommon.Gate.Release();
        }
    }

    
    private static void UpdateSeasonData(HttpContext ctx)
    {
        var memory = Functions.GetVersionInfo(ctx);
        var athena = ProfileStore.Get("athena");
        var attrs = McpCommon.Attrs(athena);
        var changed = false;

        if (J.AsLong(attrs["season_num"]) != memory.Season)
        {
            attrs["season_num"] = memory.Season;
            changed = true;
        }

        if (DataStore.Response("Athena/SeasonData.json") is JsonObject seasonDataRoot &&
            seasonDataRoot[$"Season{memory.Season}"] is JsonObject seasonData)
        {
            if (J.AsLong(attrs["book_purchased"]) != J.AsLong(seasonData["battlePassPurchased"]) ||
                J.AsLong(attrs["book_level"]) != J.AsLong(seasonData["battlePassTier"]) ||
                J.AsDouble(attrs["season_match_boost"]) != J.AsDouble(seasonData["battlePassXPBoost"]) ||
                J.AsDouble(attrs["season_friend_match_boost"]) != J.AsDouble(seasonData["battlePassXPFriendBoost"]))
            {
                attrs["book_purchased"] = seasonData["battlePassPurchased"]?.DeepClone();
                attrs["book_level"] = seasonData["battlePassTier"]?.DeepClone();
                attrs["season_match_boost"] = seasonData["battlePassXPBoost"]?.DeepClone();
                attrs["season_friend_match_boost"] = seasonData["battlePassXPFriendBoost"]?.DeepClone();
                changed = true;
            }
        }

        if (changed) ProfileStore.Save("athena", athena);
    }
}


