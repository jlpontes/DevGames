# Documento de Level Design - Nível: Batalha contra o Treinador de Água

## 1. Visão Geral para Implementação (Overview)
- **Nome Técnico/ID:** `level_mid_water_trainer`
- **Posição na Progressão:** Meio de Jogo (fase 2 de 4 da campanha: Fogo → **Água** → Planta → Boss).
- **Descrição:** Batalha de arena 1v1 (com equipes de personagens alternáveis) contra um Treinador focado no Elemento Água.
- **Duração Estimada:** ~5 minutos (condicionado ao esgotamento dos personagens de um dos lados).

## 2. Progressão e Objetivos
- **Objetivo principal:** Derrotar o oponente e passar de fase.
- **Por que o jogador está aqui:** Para ganhar moedas e evoluir os personagens.
- **O que muda ao concluir:** A próxima fase (Treinador de Planta) é desbloqueada e as recompensas são pagas.

## 3. Setup e Estado Inicial do Nível
Variáveis e estados que devem ser inicializados ao carregar a cena:
- **Equipe do Jogador:** Carregar do save/meta-jogo atual (status, aprimoramentos, itens, personagens). Até 3 itens entram na batalha (GDD R8).
- **Equipe Inimiga (IA - Treinador de Água):** 
  - **Tamanho da equipe:** 3 personagens.
  - **Composição:** os três personagens de Água: Guerreiro de Água (Tubarão-guerreiro), Mago de Água (Poseidon), Tanque de Água (Baleia) (GDD R10).
  - **Nível de Atributos:** Escalados para "Mid-game": atributos base +7% (GDD R10).
  - **Itens:** 1 Kit Médico, 1 Remédio.
- **Ambiente Inicial (Arena):** Iniciar na arena **"Mar"**, a arena do treinador (+10% de dano para personagens de Água, −10% para personagens de Fogo), para estabelecer a vantagem temática inicial do inimigo.
- **Interface de Usuário (HUD) Necessária no Carregamento:**
  - Barras de HP e Stamina (inicia em 0%) para ambos os lados.
  - Indicadores de personagens no banco de reservas (com HP e status visíveis).
  - Botões de Ação do jogador: [Atacar], [Mochila], [Trocar Personagem].
  - Indicador visual do Ambiente Atual ("Mar") e contador de rodadas até o próximo sorteio de arena (inicia em 3).

## 4. Comportamento da IA (Regras de Prioridade)
A IA inimiga escolhe a primeira regra que se aplica, nesta ordem:
1. **Sobrevivência:** Se o HP do ativo < 20% e houver *Kit Médico* disponível na mochila inimiga -> Usar Kit Médico.
2. **Troca por Vantagem:** Se um personagem do banco tiver vantagem (elemento ou classe) sobre o ativo do jogador e o ativo da IA não tiver -> 70% de chance de [Trocar Personagem] para ele.
3. **Gerenciamento de Status:** Se sofrer DoT (Veneno/Chamas) por 2 turnos -> Usar *Remédio* se houver, senão [Trocar Personagem].
4. **Ofensiva (Especial):** Se Stamina = 100% -> [Ataque Especial].
5. **Ofensiva (Básica):** Caso nenhuma das anteriores se aplique -> [Atacar] com a habilidade normal que causa mais dano.
6. **Reação (QTE de Defesa):** Quando atacada, a IA tem uma chance fixa de 40% de acertar a Janela de Resposta: 3 em cada 4 acertos são desvios e 1 em cada 4 é contra-ataque.

## 5. Fluxo do Turno (Game Loop Técnico)
O motor de combate deve executar o seguinte fluxo repetidamente até a Condição de Fim:

*   **Fase 1: Checagem de Ambiente (Início da Rodada)**
    *   Aumentar contador de rodadas. Se for múltiplo de 3, disparar o evento "Sorteio de Ambiente" (Mar, Vulcão, Floresta, Lua).
    *   Atualizar modificadores de arena (+10%/-10%). Eles ficam ativos até o próximo sorteio.
*   **Fase 2: Escolha de Ação**
    *   Os dois lados escolhem: (1) Atacar, (2) Usar Item, (3) Trocar.
*   **Fase 3: Ordem de Execução**
    *   Trocas resolvem primeiro; as demais ações seguem a ordem de Velocidade (empate decidido no sorteio).
*   **Fase 4: Execução e QTE (Janela de Resposta)**
    *   Se a ação for [Atacar]: Tocar animação de antecipação e abrir janela de QTE para o defensor.
    *   Processar resultado do QTE: Desvio (100% do dano evitado), Contra-ataque (dano evitado e a primeira habilidade normal do defensor acerta o atacante) ou Falha.
*   **Fase 5: Resolução de Dano e Stamina**
    *   Calcular dano final: Ataque - (Ataque * (Defesa * 0.5%)). Limite de corte = 50%.
    *   Aplicar multiplicadores de Vantagem (+30%/+50% causado, −30%/−50% recebido, multiplicados entre si quando mistos) e de Arena (+10%/-10%).
    *   Carregar a barra de Stamina de cada jogador: +20 ao causar dano, +15 ao sofrer dano, +25 ao desviar ou contra-atacar com sucesso.
*   **Fase 6: Aplicação de AoT (Efeitos Temporais)**
    *   Aplicar triggers de dano (DoTs) ou cura (HoTs) aos personagens afetados (ativos e no banco).
    *   Decrescer contador do efeito (-1 turno). Se chegar a 0, remover debuff/buff.
*   **Fase 7: Checagem de Morte**
    *   Se HP <= 0, disparar animação de derrota do personagem.
    *   Se houver reservas, forçar input de [Troca de Personagem].
    *   Se não houver reservas -> Disparar Condição de Fim.

## 6. Elementos de Gameplay
- **Eventos importantes:** troca de ambiente, troca de personagem, ataques e defesas (QTE), uso de item, derrota de personagem, ataque especial.
- **Novidade:** Primeiro oponente com equipe inteira de Água; a batalha começa numa arena que favorece o inimigo.
- **Conteúdo opcional ou segredo:** Nenhum.

## 7. Condições de Fim e Recompensas
- **Win Condition (Vitória do Jogador):** HP de todos os personagens da IA chegar a 0.
  - **Eventos:** Disparar tela de Vitória. 
  - **Meta-jogo:** Conceder +100 moedas. Salvar progresso (Desbloquear próximo nível).
- **Lose Condition (Derrota do Jogador):** HP de todos os personagens do Jogador chegar a 0.
  - **Eventos:** Disparar tela de Derrota.
  - **Meta-jogo:** Conceder +50 moedas de consolação. A fase pode ser repetida.
