# Documento de Level Design - Nível: Batalha contra o Treinador de Água

## 1. Visão Geral para Implementação (Overview)
- **Nome Técnico/ID:** `level_mid_water_trainer`
- **Posição na Progressão:** Meio de Jogo (Mid-game).
- **Descrição:** Batalha de arena 1v1 (com equipes de personagens alternáveis) contra um Treinador focado no Elemento Água.
- **Duração Estimada:** ~5 minutos (condicionado ao esgotamento dos personagens de um dos lados).

## 2. Setup e Estado Inicial do Nível
Variáveis e estados que devem ser inicializados ao carregar a cena:
- **Equipe do Jogador:** Carregar do save/meta-jogo atual (status, aprimoramentos, itens, personagens).
- **Equipe Inimiga (IA - Treinador de Água):** 
  - **Tamanho da equipe:** 3 personagens.
  - **Composição obrigatória:** Pelo menos 2 personagens do Elemento Água.
  - **Nível de Atributos:** Escalados para "Mid-game" (Atributos base + ~7% de aprimoramento).
- **Ambiente Inicial (Arena):** Iniciar forçadamente na arena **"Mar"** (+10% dano para personagens de Água) para estabelecer vantagem temática inicial ao inimigo.
- **Interface de Usuário (HUD) Necessária no Carregamento:**
  - Barras de HP e Stamina (inicia em 0%) para ambos os lados.
  - Indicadores de personagens no banco de reservas (com HP e status visíveis).
  - Botões de Ação do jogador: [Atacar], [Mochila], [Trocar Personagem].
  - Indicador visual do Ambiente Atual ("Mar") e contador de turnos até o próximo sorteio de arena (inicia em 3).

## 3. Comportamento da IA (Máquina de Estados)
A IA inimiga deve tomar decisões baseadas em regras de prioridade decrescente:
1. **Sobrevivência:** Se o HP do ativo < 20% e houver *Kit Médico* disponível na mochila inimiga -> Usar Kit Médico.
2. **Vantagem Elemental:** Se o jogador colocar um personagem de Fogo e a IA tiver um personagem de Água no banco -> 70% de chance de [Trocar Personagem] para buscar multiplicador de +30% ou +50% de dano.
3. **Gerenciamento de Status:** Se sofrer DoT (Veneno/Chamas) por 2 turnos -> Usar *Remédio* ou [Trocar Personagem].
4. **Ofensiva (Especial):** Se Stamina = 100% -> Prioridade máxima para [Ataque Especial].
5. **Ofensiva (Básica):** Caso nenhuma das anteriores se aplique -> [Atacar].
6. **Reação (QTE de Defesa):** Quando atacada, a IA tem uma chance fixa (ex: 40%) de acertar o QTE (Janela de Resposta) para Desvio ou Contra-ataque.

## 4. Fluxo do Turno (Game Loop Técnico)
O motor de combate deve executar o seguinte fluxo repetidamente até a Condição de Fim:

*   **Fase 1: Checagem de Ambiente (Início da Rodada)**
    *   Aumentar contador de rodadas. Se for múltiplo de 3:
    *   Disparar evento "Sorteio de Ambiente" (Mar, Vulcão, Floresta, Lua).
    *   Atualizar modificadores de arena (+10%/-10%).
*   **Fase 2: Escolha de Ação**
    *   Aguardar input do Jogador ou da IA: (1) Atacar, (2) Usar Item, (3) Trocar.
*   **Fase 3: Execução e QTE (Janela de Resposta)**
    *   Se a ação for [Atacar]: Tocar animação de antecipação e abrir janela de QTE para o defensor.
    *   Processar resultado do QTE (Desvio 100% dano evitado, Contra-ataque, ou Falha).
*   **Fase 4: Resolução de Dano e Stamina**
    *   Calcular dano final considerando: Base Atq - (Base Atq * (Defesa * 0.5%)). Limite de corte = 50%.
    *   Aplicar multiplicadores de Vantagem Elemental (+30%/+50%) e Arena (+10%/-10%).
    *   Incrementar barra de Stamina de ambos (atacante, defensor que sofreu dano, ou defensor que realizou QTE de sucesso).
*   **Fase 5: Aplicação de AoT (Efeitos Temporais)**
    *   Aplicar triggers de dano (DoTs) ou cura (HoTs) aos personagens afetados (ativos e no banco).
    *   Decrescer contador do efeito (-1 turno). Se chegar a 0, remover debuff/buff.
*   **Fase 6: Checagem de Morte**
    *   Se HP <= 0, disparar animação de derrota do personagem.
    *   Se houver reservas, forçar input de [Troca de Personagem].
    *   Se não houver reservas -> Disparar Condição de Fim.

## 5. Condições de Fim e Recompensas
- **Win Condition (Vitória do Jogador):** HP de todos os personagens da IA chegar a 0.
  - **Eventos:** Disparar tela de Vitória. 
  - **Meta-jogo:** Conceder +100 moedas. Salvar progresso (Desbloquear próximo nível).
- **Lose Condition (Derrota do Jogador):** HP de todos os personagens do Jogador chegar a 0.
  - **Eventos:** Disparar tela de Derrota.
  - **Meta-jogo:** Conceder +50 moedas de consolação.
