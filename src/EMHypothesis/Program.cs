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
// Observations:
//   * you need to submit the query, you can't just copy and paste the trade ID (because the query sent to the API is what gives you the ID's)
//   * the online web page is the best UI way to build a query
//   * we might as well just pull the sum and cost info from the web interface
//   * export from web to a file would be nice

// https://www.pathofexile.com/trade2/search/poe2/Standard/gvYY6DKsQ
// https://www.pathofexile.com/api/trade2/search/poe2/Standard
// request: {"query":{"status":{"option":"online"},"stats":[{"type":"and","filters":[{"id":"explicit.stat_1050105434","disabled":true,"value":{"min":80}},{"id":"explicit.stat_789117908","disabled":true},{"id":"explicit.stat_3299347043","disabled":false,"value":{"min":81}},{"id":"implicit.stat_3299347043","disabled":true}],"disabled":false},{"type":"weight","filters":[{"id":"explicit.stat_4080418644","disabled":false},{"id":"explicit.stat_3261801346","disabled":false},{"id":"explicit.stat_328541901","disabled":false},{"id":"explicit.stat_1379411836","disabled":false,"value":{"weight":3}},{"id":"implicit.stat_1379411836","disabled":true,"value":{"weight":3}},{"id":"implicit.stat_3261801346","disabled":true},{"id":"implicit.stat_4080418644","disabled":true},{"id":"implicit.stat_328541901","disabled":true},{"id":"explicit.stat_2901986750","disabled":false,"value":{"weight":3}},{"id":"implicit.stat_2901986750","disabled":false,"value":{"weight":3}},{"id":"explicit.stat_3372524247","disabled":false},{"id":"explicit.stat_1671376347","disabled":false},{"id":"explicit.stat_4220027924","disabled":false},{"id":"implicit.stat_3372524247","disabled":false},{"id":"implicit.stat_4220027924","disabled":false},{"id":"implicit.stat_1671376347","disabled":false},{"id":"explicit.stat_2923486259","disabled":false,"value":{"weight":2}},{"id":"implicit.stat_2923486259","disabled":false,"value":{"weight":2}}],"disabled":false,"value":{"min":80}},{"type":"weight","filters":[{"id":"explicit.stat_3032590688","disabled":false},{"id":"implicit.stat_3032590688","disabled":true},{"id":"explicit.stat_4067062424","disabled":false},{"id":"explicit.stat_1754445556","disabled":false},{"id":"explicit.stat_1573130764","disabled":false},{"id":"explicit.stat_210067635","disabled":true,"value":{"weight":3}},{"id":"explicit.stat_681332047","disabled":true,"value":{"weight":3}},{"id":"explicit.stat_3962278098","disabled":false,"value":{"weight":0.5}}],"disabled":false,"value":{"min":20}}],"filters":{"type_filters":{"filters":{"category":{"option":"accessory.ring"}},"disabled":false},"trade_filters":{"filters":{"price":{"min":null,"max":20,"option":"exalted"},"indexed":{"option":"3days"}},"disabled":false}}},"sort":{"price":"asc"}}
// response: {
//     "id": "gvYY6DKsQ",
//     "complexity": 186,
//     "result": [
//         "309a6607904a1c405858717b8bab4f6736a24b021ce45d6434654d70dcfb0d42",
//         "74c31fc138d724484ecabf4544dcac824e1bd8ad6060b808fafe365a2cb98e5e",
//         "e1548a62a90cdf7ea8f62d05554852a3f11e9adfee4c23175ee81a242f8bc3c7",
//         "b9f0a383192226938d11512067b075069c110796dd73835ea9c187974d12c6e1",
//         "cc2f5dc8efa5a283688e82d4e9ca25e8f0ef5dc11fd7655cfed816fcd7ae7a0e",
//         "8175be733f238095ca1a30427b8c2eae4f8d0c376f37cd9c8164fed248493ab4",
//         "6617fcc1fae7c21570f015e07a79ec109dec0921a50f397ffaa8061794c582ba",
//         "86386d61a13ee91fb11664b77782db8a2693c8c7b9be98970189865d53d879fb",
//         "b068709854846966b32b52f414f56b65e3503d914aaffeb1dfcd806f819f9a72",
//         "35c17758227e768df71f6bf77c29a654d870157fda9f2037ecaf66884a90ba3e",
//         "18365d79b5dec5ba82b133ec8b068dc9e3b8b975c507c201f069af0bee08e3c2",
//         "1c136a1d64eb298baf441bfec80f2916e4b2d5c1a831fca70a7bc5f6f84989e5",
//         "a7b7eb4413f4dde919dd0735ba11d8d1e997aa56cb1abfebf9b85d1838dc341d",
//         "c9c6c3cd654a25c36ef995f192b3af612390d0bfa48a46e6447c0696504ff77e",
//         "d480971a16ee0064f5887325ed1982387ccf9c3845922663e3bad8f1ea525b1f",
//         "e780358c7612a5b0b8623751532403f8dceced9b570ee5b2fe664e8d06b6ab71",
//         "35ee996871add95fbe7a00cb935273dad4d6fb91ecea7e6a534ffa0644c14e40",
//         "6e2e36b3df7ba0c0ef1efeca947312b48b6a4edbf87fdacd108e903d68a7eee5",
//         "511897e1b3242f85dd562467e42938d506e497e27c9275a0c907df23fc1a5e20",
//         "c599e96c690e2b1ce2eb30c37feb087a60a8af411f8e1ee7ff81d8228ec770ca",
//         "81bfa16cffbbb6d61f36330e7ce54e10f1809f22361d567a0e09eaa1f4a2a4ff",
//         "db87a238b25d90a021d634e6385bd5b52a481d3bdd5aea8e6ef6adfaf26cca54",
//         "448d3d2cbdfb38700b0e47f3f458b4be0b9498b814fafff944bc246ee56906a0",
//         "c06a90dfa0dc693e604004ed6e3929871586aee8e68a45c06543e7cdd685bc68",
//         "bcee6eebc5a4e58653a80a541a88b09d15cf1531fef729b9a241ca26b2306f53",
//         "f6391ca5962d3ed8b8366b54feba15bd80aa19e888a5366ffa6cb55690921dbd",
//         "a603a9695bce5fb9dc5a09f878ea885f09c58115e6a7487d5bd277f1d68171b0",
//         "406bf2f3caedc1f4211e4195ba9d4274782a172fd39e7adff46089efdcc067f5",
//         "68d4fd127f19ad21dbd1bc99918d3e6fdf80bbb1b6b1d0381645427c9ac55de2",
//         "c40e29f11eae60cb9834189b39095b60f541849f146ade6d1b0277536d92a575",
//         "172bba76c38282db03cfdc0f8a3f707e0477918b5609930ac31b781ac5a60dfb",
//         "11fda1dae0cc556dff7a851aaf9a5adaf69375be2a74e308c118b5c8581c1f90",
//         "e32a3345c89c0803349bc11adf176d9d1cfa4d43d5926e51638b81068040c6db",
//         "f8b3483e01ea90808ee65875d76e4743ae0f55ba6c678b2d14baf68f9480d6a4",
//         "e9c0c91d1db3261d2e643b7b8f99762a97679be1f56318646c44eed6f6d8b634",
//         "28e1e59b30c0f327e64b18bd21fd00923e19604dc08cc935a079e3f8c4109a69",
//         "0f8905f83581cfd822f9a1a27f4b252f698efe53cb10bf19434a5fd578a5e870",
//         "75e8c335ab4249792084f04d2fa91a6c38626aad4f7019390195776162b7d0d6",
//         "844c7caea716141bd1a7658704698eeaddd4e1696b2e30cd301f44288db18cdf",
//         "160ab063e879481a7937db5954d75ae0de20747e05f8744aa0983dcc3f1616fb",
//         "9f372595ed3391bb58b265f56461098c26855e30f9bfe38892e507ccc56b9d40",
//         "ae28d9a09c8f869bbcffe8a6621d8879e8067b1c679ffbf73b68d70ab5b1522d",
//         "df3bfc9a70a4e5f7e026134e8fc39cad950d4d6c1a20aba127b9532f482e073b",
//         "86f9aabd9ed3c47fdcfedbe50be158a7ab2521fa14106e827473883cad32d3f9",
//         "48b2a83ec073b20dd55f1e7f230399a190c16aec3fa4088067188601239d558d",
//         "3d34f18b726dc66d1925a21f9e129d2914b501641330b460ccb5be9459c0faea",
//         "7a4bc6714dba4ab6f13cd403aabcaec841af09d345bbcb3c02f2ad468fc4e544",
//         "798f7dc0ca1cc88481fd7df5a3a999de5ec4e9637a9357988edd5c20beb63d7d",
//         "4023df1ab679a89764d0f2c12359613f0655b258720667939162fccfecfbf9bd",
//         "c2609c417d7096b520adb3297a1f4e6e1f506f6bbe72f98c7c642755b4bba76e",
//         "eb7422c8bd7ada81cf0ac0a1e850bfe97079ba09df09d6d94dc8ba9dcee96aeb",
//         "5e0322d330a89bf615ea0ca60306997f18afae4f8655c98ed71203787e181416",
//         "4151537138831fd94a284ca7271294b78d681e0ac3cec25f6c3865e977f362bc",
//         "53a272f32c3ae08880be80e92cc36d75ff7207836b38f8c5b37da0fd55500d3a",
//         "55253f7e87cb6eef96fe0b08d65b33ce1bf1719d7c0c670c8e97eee1b4c10ddb",
//         "65cb576d0c231133e16274c5f5286e34cf9cc46d60055b4b6546737191fabad3",
//         "a0a27c204a3e43f86e334cfd0b58dbe803000685cbcdeef599fe9bae205eea89",
//         "d1c30ae59184257ef2527512f4dd8ffe37d5ef4097a04ea7d87c5b52ba7af536",
//         "00094304ff269927b2c45ad0ddc6e4871965ac0b21179c01a306e1680a70f12b",
//         "3363506c2826a20b9caf1675a98406d58b5fcc5db65df68574d85ac8ad7f8669",
//         "43a8cc784c04e7b72bd350fb148b40b5f3ccf7e4c783beb4029a4d56c276d6e6",
//         "c83fcbe73d893ae73d87b60d5ee273e0b0ea8436cc4c4120ebb6389eaed6a053",
//         "7cacbfb86102e2e6cbe5909f3d3408703a77c0a82ac529aaa125e840d77a65f8",
//         "36e223728b48235a55e5573e6e57c09314d8e6b9f22347bcd89960f2dd6601c1",
//         "cb75a58f54d69e55596b23bc3d887f172729e97b982ae2e88472d10a3b163f5c",
//         "be9212acc1aba88b5fb05e46b9940e6765d0907dbe5e4f234a4c922f7ac8eedc",
//         "221c47014e79473aad418cbbe4e9af4cb0301c0759726a12adab49f60577b161",
//         "a343b12dd22e384a70d520158adba011f76e96c0369cb7bc30a1ffae7c00d0a9",
//         "eb04d95e396e3168eaa0e8442b1a00c36689684a33df5f79beb9a40cdc3bb861",
//         "bc9144a7863e18f9ca497da66e335b1718b5036e1ca14f40b0470b79ac72bd5b",
//         "3bcf5d3133fa0fcaea693fd1489fafb9aab8c9a5e8224aea491da7496f07b982",
//         "3c73f49db57a4817329529dab40f50365b14496f65361cbe93e65e087b26d4ca",
//         "a6c9e77f042cfb465b2ebb2519dbcb0351a455a455b3f0c9faa45c006ef4d528",
//         "4d4217e67310324f34e9a048b51030007ab8514d196155a4ad7b85544524a150",
//         "52ddb1cfa2d6979db5e6809d2593fd323aca71b3e28b9d97e2bdbbe192f3477b",
//         "81c4531587faa0fc341a143032c15104df0be6593cd5bffcbde765be0912347d",
//         "6408f159df12c36c2f25ac4d178bb59cf8041c93bf2a04a68109f09b74875979",
//         "ff5ffbeabc444c2dc30ed96356741f11568793e5e8dff144a83fd7664f36edd7",
//         "202a6bc3ce4951c216fb8cad84b0d0b8800b2c182f2723acd00f3c53d7e5b82c",
//         "698c63e51397d8678d93490cc9462005e47f8d23780fcd11591d59e2ee86aac6",
//         "af8aa5a089a7d348246b90b83b590e40a0aad516730c1a9473c07fe5431a77ff",
//         "04ca4f0b33f2a3f7e1a08bcfc386ff24380f37f3d0ab027e28af66885803f05f",
//         "a40f7ccf646fa04b7a7044b1041b53a3335558cb660ee5faefec0eaa920fb48a",
//         "354a007a49f2a017324d7491f0edc0d2bdc551869d13606853244005eb6ff316",
//         "0751b90309e79a1a7163aaa495052b8afb6974ae634712b7c298dff3e2b42fd3",
//         "4653f806db44c1111316a99a107e1706332d61f26085b2d681f587e1e8f38cd4",
//         "5eb652990a12b68c3eab1f827764d787709855f6208263871588654228a3c280",
//         "728a71e30ca5c8f1484e25239b64c80a2708ffc05cb1804c24d00d0d4dd86bbe",
//         "7b902dd845beecc0734014eb67ecbddad2a1927d4ec9b846b909fc503f698ce2",
//         "0f850e25e1af57dbdb95b1674dd59b7db22676d58576958d07a1602c8ae5feae",
//         "314618afaf5e5e2a2c78ac602cf21faacda612c8b30ee627968e1a705ee94e7b",
//         "b30ad568526b27fce201b7e2a4b6aea563983ad42e537ec2f69dfbf89b1dc109",
//         "291a23584702e795c1edc909ce6304859c5c7962e64903ba1bc0ccce414f672e",
//         "e35d724a5d8f90f4e7b9760f6c78b2c9551ee067792f7b87dc83cbe3476a2d6a",
//         "3d41f062ead382da879f186644c14da05759c812c5f601b4bda2c41433c2fb59",
//         "7f99641439ba1abcf02ea7cf4c5b5ba84239abd9407625718fb8100c6a9cebe0",
//         "64ac40b6a19bc1bf505bee6118d8ab8262f59323783f83ffdf968676dad24d1f",
//         "26b96e7344c14eb11dfe688fde6be22e8bd3bce7da7b444c4537055e1268bd7f",
//         "5a9298d3af67c171227ebca7cc30a20eb809cc75be14f9a1185c97c9f92c7011",
//         "ba32888b7aecbf9664d8182a5d3f0234fcc5efa113ba287e38f438a6ccbe4a8c"
//     ],
//     "total": 262
// }
                // "pseudoMods": [
                //     "Sum: 22",
                //     "Sum: 92"
                // ]

                // "price": {
                //     "type": "~price",
                //     "amount": 1,
                //     "currency": "exalted"
                // }

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


