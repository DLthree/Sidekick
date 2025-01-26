using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sidekick.Common.Database;
using Sidekick.Common.Platform;
using Sidekick.Common.Platform.Interprocess;
using Sidekick.Common.Ui;
using Sidekick.Mock;
using Sidekick.Modules.Chat;
using Sidekick.Modules.Development;
using Sidekick.Modules.General;
using Sidekick.Modules.Maps;
using Sidekick.Modules.Trade;
using Sidekick.Modules.Wealth;
using Sidekick.Apis.GitHub;
using Sidekick.Apis.Poe;
using Sidekick.Apis.PoeNinja;
using Sidekick.Apis.PoePriceInfo;
using Sidekick.Apis.PoeWiki;
using Sidekick.Common.Updater;
using Sidekick.Common;
using Sidekick.Common.Blazor;
using Sidekick.Common.Ui.Views;
using Sidekick.Apis.Poe.Parser;
using Serilog.Debugging;
using System.Threading.Tasks;
using System;
using System.Text;
using Sidekick.Apis.Poe.Bulk;
using Sidekick.Apis.Poe.Bulk.Models;
using Sidekick.Apis.Poe.Parser.Properties.Filters;
using Sidekick.Apis.Poe.Trade.Models;
using Sidekick.Apis.Poe.Trade.Results;
using Sidekick.Common.Extensions;
using Sidekick.Common.Game.Items;
using Sidekick.Common.Settings;
using Sidekick.Apis.Poe.Parser.Properties;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Sidekick.Common.Initialization;

public class MyService(
    IItemParser itemParser,
    ITradeFilterService tradeFilterService,
    ITradeSearchService tradeSearchService)
{
    public async Task<List<TradeItem>> Gogogo(string itemText) 
    {
        var item = await itemParser.ParseItemAsync(itemText);

        var propertyFilters = await tradeFilterService.GetPropertyFilters(item);
        var modifierFilters = tradeFilterService
                          .GetModifierFilters(item)
                          .ToList();
        var pseudoFilters = tradeFilterService
                        .GetPseudoModifierFilters(item)
                        .ToList();
        var tradeItems = new List<TradeItem>();
        var itemTradeResult = await tradeSearchService.Search(
            item,
            propertyFilters,
            modifierFilters,
            pseudoFilters);

        var ids = itemTradeResult.Result?
                  .Skip(tradeItems?.Count ?? 0)
                  .Take(10)
                  .ToList();
        if (ids?.Count == 0)
        {
            return [];
        }

        if (itemTradeResult.Id != null && ids != null)
        {
            var result = await tradeSearchService.GetResults(item.Header.Game, itemTradeResult.Id, ids, pseudoFilters);
            tradeItems?.AddRange(result);
        }
        return tradeItems ?? [];
    }

}

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var settingsService = host.Services.GetRequiredService<ISettingsService>();
        await settingsService.Set(SettingKeys.LanguageParser, "en");
        await settingsService.Set(SettingKeys.LanguageUi, "en");
        await settingsService.Set(SettingKeys.LeagueId, "poe2.Standard");

        var serviceProvider = host.Services.GetRequiredService<IServiceProvider>();
        await Initialize(serviceProvider);

        var service = new MyService(
            host.Services.GetRequiredService<IItemParser>(),
            host.Services.GetRequiredService<ITradeFilterService>(),
            host.Services.GetRequiredService<ITradeSearchService>()
        );
        
        string item = @"
Item Class: Rings
Rarity: Rare
Doom Whorl
Prismatic Ring
--------
Quality (Fire Modifiers): +20% (augmented)
--------
Requirements:
Level: 44
--------
Item Level: 73
--------
+9% to all Elemental Resistances (implicit)
--------
Adds 15 to 34 Fire damage to Attacks
+83 to maximum Life
+11 to maximum Mana
+27 to Dexterity
+12 to Intelligence
+17% to Chaos Resistance
";
        var results = await service.Gogogo(item);

        foreach (var result in results)
        {
            Console.WriteLine($"Item: {result}");
        }
    }

    private static async Task Initialize(IServiceProvider serviceProvider)
    {
        var  configuration = serviceProvider.GetRequiredService<IOptions<SidekickConfiguration>>();
        var  logger = serviceProvider.GetRequiredService<ILogger<Main>>();
        foreach (var serviceType in configuration.Value.InitializableServices)
        {
            var service = serviceProvider.GetRequiredService(serviceType);
            if (service is not IInitializableService initializableService)
            {
                continue;
            }

            logger.LogInformation($"[Initialization] Initializing {initializableService.GetType().FullName}");
            await initializableService.Initialize();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddLocalization();
                services
                    // Common
                    .AddSidekickCommon()
                    // .AddSidekickCommonBlazor()
                    .AddSidekickCommonDatabase(SidekickPaths.DatabasePath)
                    // .AddSidekickCommonUi()
                    // .AddSingleton<IInterprocessService, InterprocessService>()

                    // Apis
                    // .AddSidekickGitHubApi()
                    .AddSidekickPoeApi()
                    // .AddSidekickPoeNinjaApi()
                    // .AddSidekickPoePriceInfoApi()
                    // .AddSidekickPoeWikiApi()
                    // .AddSidekickUpdater()

                    // Modules
                    // .AddSidekickChat()
                    // .AddSidekickDevelopment()
                    .AddSidekickGeneral()
                    // .AddSidekickMaps()
                    .AddSidekickTrade()
                    // .AddSidekickWealth()

                    // // Mocks
                    .AddSidekickMocks();
                    
            });
}


