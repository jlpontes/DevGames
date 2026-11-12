using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>
    /// Raiz de composição da camada de jogo. Cria os serviços uma vez e os entrega prontos;
    /// a apresentação recebe este objeto e não instancia serviços por conta própria.
    /// </summary>
    public sealed class GameServices
    {
        public readonly ContentDatabase Content;
        public readonly EconomyConfig Economy;
        public readonly BattleRules Rules;
        public readonly ProfileService Profile;
        public readonly StoreService Store;
        public readonly CampaignService Campaign;
        public readonly JourneyService Journey;
        public readonly MissionService Missions;
        public readonly BattleLauncher Launcher;

        public GameServices(ISaveStore saveStore, ContentDatabase content = null, EconomyConfig economy = null, BattleRules rules = null)
        {
            Content = content ?? BuiltInContent.Create();
            Economy = economy ?? new EconomyConfig();
            Rules = rules ?? new BattleRules();
            Profile = new ProfileService(saveStore, Content, Economy);
            Store = new StoreService(Profile, Content, Economy);
            Campaign = new CampaignService(Profile, Content);
            Journey = new JourneyService(Profile, Content, Economy);
            Missions = new MissionService(Profile, Journey, Content);
            Launcher = new BattleLauncher(Content, Economy, Rules, Profile, Campaign, Journey);
        }
    }
}
