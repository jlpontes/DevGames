# ADR-005: Save local em PlayerPrefs JSON, sem backend

**Status:** Proposto
**Data:** 02/10/2026
**Decisores:** Time de desenvolvimento

## Contexto
Os dados persistentes são poucos: moedas, inventário de itens, níveis de evolução (9 personagens × 4 atributos), progresso da campanha e configurações. Bem menos de 10 KB. O Caderno prevê pacotes de moedas pagos para depois do lançamento, e o time decidiu **não ter backend no MVP**. O jogo é gratuito no navegador; o hot-seat local é o único multiplayer.

## Decisão
Serializar uma classe `SaveData` versionada com `JsonUtility` no `PlayerPrefs`, atrás de uma interface `ISaveStore`. As gravações alternam entre duas chaves (`save_a` / `save_b`) com contador e checksum, e o carregamento escolhe a mais recente válida, então uma gravação interrompida nunca destrói o save anterior. No WebGL, o `PlayerPrefs` fica no IndexedDB do navegador. Chamar `PlayerPrefs.Save()` depois de cada mudança (fim da batalha, compra na loja, mudança de configuração).

```csharp
[Serializable] class SaveData {
    public int version = 1;
    public int coins;
    public List<ItemStack> items;            // itemId, count
    public List<UpgradeEntry> upgrades;      // characterId, hp, atk, def, spd (0..5)
    public int campaignStage;                // 0..4
    public Settings settings;
}
interface ISaveStore { SaveData Load(); void Save(SaveData d); }
```

## Opções Consideradas

### Opção A: PlayerPrefs + JSON (escolhida)
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa |
| Custo | Gratuito, algumas horas |
| Escalabilidade | Limite de ~1 MB no WebGL, muito mais que o necessário |
| Familiaridade do time | Alta |

**Prós:** funciona igual no editor e no WebGL; sem servidor.
**Contras:** perdido se o jogador limpar os dados do navegador; fácil de trapacear (editar as moedas). Aceitável sem dinheiro real e sem PvP online.

### Opção B: Arquivo em `Application.persistentDataPath`
Também fica no IndexedDB no WebGL, mas exige cuidado extra para garantir que as gravações sejam descarregadas. Adiciona complexidade sem benefício neste tamanho.

### Opção C: Backend (Firebase/Supabase) com contas
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Alta (autenticação, erros de rede, modo offline) |
| Custo | Plano gratuito, mas muito tempo de desenvolvimento |
| Escalabilidade | Entre aparelhos; necessário para pagamentos reais |
| Familiaridade do time | Baixa–Média |

**Prós:** save na nuvem; permite monetização. **Contras:** não cabe em 4 semanas junto com o jogo inteiro.

## Análise de Trade-offs
Nada no MVP precisa de confiança no servidor: as moedas não compram nada real. O ponto de troca `ISaveStore` mantém um futuro `CloudSaveStore` como uma mudança contida.

## Consequências
- Fica mais fácil: zero infraestrutura; funciona offline depois de carregar.
- Fica mais difícil: sem progresso entre aparelhos; o save pode ser editado.
- Revisitar: antes de adicionar **pacotes de moedas com dinheiro real**, levar moedas e compras para um backend com autoridade no servidor. Uma carteira no cliente não é confiável para pagamentos.

## Itens de Ação
1. [ ] Implementar o `PlayerPrefsSaveStore` com campo `version` e um gancho de migração.
2. [ ] Tratar save corrompido ou ausente começando um perfil novo e registrando um aviso.
3. [ ] Adicionar um menu de debug: zerar save, adicionar moedas (só em builds de desenvolvimento).
