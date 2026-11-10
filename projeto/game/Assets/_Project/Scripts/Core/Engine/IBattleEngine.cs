using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>Resposta de cada chamada ao motor. Se Ok for falso, o estado não mudou.</summary>
    public sealed class StepResult
    {
        private static readonly BattleEvent[] NoEvents = new BattleEvent[0];

        public readonly bool Ok;
        public readonly string Error;
        public readonly IReadOnlyList<BattleEvent> Events;

        private StepResult(bool ok, string error, IReadOnlyList<BattleEvent> events) { Ok = ok; Error = error; Events = events; }

        public static StepResult Success(IReadOnlyList<BattleEvent> events) { return new StepResult(true, null, events); }
        public static StepResult Fail(string error) { return new StepResult(false, error, NoEvents); }
    }

    /// <summary>
    /// Máquina de estados da batalha. Nunca espera por tempo nem por entrada: recebe comandos,
    /// devolve eventos e informa em Pending o que precisa para continuar.
    /// </summary>
    public interface IBattleEngine
    {
        BattleState State { get; }
        BattleRules Rules { get; }
        BattleCatalog Catalog { get; }

        PendingKind Pending { get; }
        /// <summary>Quem precisa responder quando Pending é Reaction ou ForcedSwitch.</summary>
        int PendingSide { get; }

        bool IsOver { get; }
        /// <summary>0 ou 1; -1 em caso de empate. Só vale quando IsOver é verdadeiro.</summary>
        int WinnerSide { get; }

        /// <summary>Seed e comandos enviados: reproduzem a batalha inteira num relatório de bug.</summary>
        int Seed { get; }
        IReadOnlyList<string> CommandLog { get; }

        bool IsLegal(ICommand command);
        IReadOnlyList<ICommand> LegalCommands(int side);
        /// <summary>Dano que a habilidade causaria agora no lutador ativo do oponente.</summary>
        DamageResult PreviewDamage(int attackerSide, int skillIndex);

        StepResult SubmitActions(ICommand side0, ICommand side1);
        StepResult SubmitReaction(ReactionResult result);
        StepResult SubmitForcedSwitch(int teamIndex);
    }
}
