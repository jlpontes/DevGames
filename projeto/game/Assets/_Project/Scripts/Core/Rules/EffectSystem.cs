using System;

namespace ComboTactics.Core
{
    /// <summary>Aplicação dos efeitos de 3 turnos. Reaplicar o mesmo efeito renova a duração, sem acumular.</summary>
    public static class EffectSystem
    {
        public static ActiveEffect Apply(Fighter source, Fighter target, EffectSpec spec)
        {
            var existing = target.FindEffect(spec.Id);
            if (existing != null) target.Effects.Remove(existing);

            int value = 0;
            if (spec.Kind == EffectKind.DamageOverTime)
                value = Math.Max(1, (int)Math.Round(spec.Value * source.Spec.Attack, MidpointRounding.AwayFromZero));
            else if (spec.Kind == EffectKind.HealOverTime)
                value = Math.Max(1, (int)Math.Round(spec.Value * target.Spec.MaxHp, MidpointRounding.AwayFromZero));

            var effect = new ActiveEffect(spec, value);
            target.Effects.Add(effect);
            return effect;
        }
    }
}
