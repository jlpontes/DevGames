# Combinados do projeto (para pessoas e agentes de IA)

Vários agentes trabalham neste repositório ao mesmo tempo. Estas regras evitam que um desfaça o trabalho do outro.

## Onde cada coisa mora

| O quê | Onde | Regra |
|---|---|---|
| Regras de batalha | `Scripts/Core` | C# puro, determinístico. Toda mudança de estado da batalha acontece aqui e vira um `BattleEvent`. |
| Conteúdo, história, progressão, save | `Scripts/Game` | C# puro. Personagens e guardiões em `BuiltInContent`, missões em `BuiltInContent.Missions`, textos da história em `StoryScript`. |
| Telas, animação, efeitos, arte | `Scripts/Presentation`, `Resources/Art` | Só desenha. Lê o estado pelos serviços (`App.Services`) e pelo `BattleSnapshot`; nunca altera o `BattleEngine` direto. |
| Testes e simulação | `Scripts/Tests` | Toda regra nova ganha teste. Mudou número de balanceamento: rode `PrintBalanceReport` e a `CampaignSimulation`. |

## Regras

- **Save só pelo `ProfileService`.** Nada de chaves soltas no `PlayerPrefs`: elas escapam do save em dois slots, da migração de versão e do "Zerar progresso".
- **História segue o Caderno (p.2, p.3, p.8) e o GDD (R9 a R14).** Texto novo entra no `StoryScript`; a tela só formata.
- **A UI não decide regra.** Se a tela precisa de um número ou de uma condição (custo do Altar, quem está no espectro, se a missão foi cumprida), pergunte ao serviço (`Journey`, `Missions`, `Launcher`).
- **Antes de commitar,** confira que nenhum arquivo de outro agente ficou no meio de uma mudança, e commite só os seus arquivos.
- **Commits:** autor jlpontes, mensagem em inglês no imperativo, sem linha de coautoria.
