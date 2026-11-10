namespace ComboTactics.Core
{
    /// <summary>As três ações de turno do GDD §2. Contrato congelado entre núcleo, IA e interface.</summary>
    public interface ICommand
    {
        int Side { get; }
    }

    /// <summary>SkillIndex 0 e 1 são as habilidades normais; o último índice é o especial.</summary>
    public sealed class AttackCommand : ICommand
    {
        public int Side { get; private set; }
        public readonly int SkillIndex;
        public AttackCommand(int side, int skillIndex) { Side = side; SkillIndex = skillIndex; }
        public override string ToString() { return "Attack(" + Side + "," + SkillIndex + ")"; }
    }

    public sealed class UseItemCommand : ICommand
    {
        public int Side { get; private set; }
        public readonly string ItemId;
        public UseItemCommand(int side, string itemId) { Side = side; ItemId = itemId; }
        public override string ToString() { return "Item(" + Side + "," + ItemId + ")"; }
    }

    /// <summary>TeamIndex é a posição do lutador na equipe (0 a 2), não a posição no banco.</summary>
    public sealed class SwitchCommand : ICommand
    {
        public int Side { get; private set; }
        public readonly int TeamIndex;
        public SwitchCommand(int side, int teamIndex) { Side = side; TeamIndex = teamIndex; }
        public override string ToString() { return "Switch(" + Side + "," + TeamIndex + ")"; }
    }
}
