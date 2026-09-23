# Mecânicas do Jogo (Elementos Neutros e de Sistema)

Como **Combo Tactics** é um RPG Tático de turnos focado em duelos de arena 1v1 sem fases de exploração de mapas, o jogo **não possui** armadilhas físicas, barreiras espalhadas ou caixas destrutíveis pelo cenário (conforme o documento de Game Design, "o jogo não tem hazards, todo o dano vem das batalhas"). 

No entanto, há diversos sistemas mecânicos, elementos de ambiente e regras que funcionam de forma independente dos personagens e governam o mundo do jogo. Abaixo está o detalhamento dessas mecânicas:

## 1. Sistema de Mudança de Arena (Sorteio de Ambiente)
Esta é a principal mecânica de "hazard" do jogo. A arena é um ambiente dinâmico que afeta os lutadores de ambos os lados da batalha.
*   **Funcionamento:** A cada 3 rodadas, ocorre um sorteio (estilo roleta de cassino) que altera completamente o cenário.
*   **Impacto no Gameplay:** O ambiente aplica um modificador de **+10% ou -10% de dano**, favorecendo ou prejudicando personagens de acordo com seu elemento ou atributos:
    *   **Mar:** Favorece personagens de Água / Prejudica personagens de Fogo.
    *   **Vulcão:** Favorece personagens de Fogo / Prejudica personagens de Planta.
    *   **Floresta:** Favorece personagens de Planta / Prejudica personagens de Água.
    *   **Lua (Baixa gravidade):** Favorece personagens com baixa Velocidade / Prejudica personagens com alta Velocidade.

## 2. Janela de Resposta (Desvio e Contra-ataque)
Uma mecânica interativa (QTE - Quick Time Event) que quebra o ritmo passivo dos turnos convencionais.
*   **Funcionamento:** No momento em que um personagem é atacado, abre-se uma curta janela de tempo. Se o jogador (ou o oponente) clicar no tempo correto, ele reage ativamente ao invés de apenas receber o dano.
*   **Possíveis Resultados:**
    *   **Desvio:** O dano do ataque é 100% anulado.
    *   **Contra-ataque:** O alvo não apenas anula o dano, mas devolve o golpe no oponente.

## 3. Gestão de Stamina e Turnos
A base estrutural do combate onde as regras de ações são regidas.
*   **Economia de Ações:** O jogador só pode realizar **uma ação** por turno: (1) Atacar, (2) Usar Item ou (3) Trocar de Personagem ativo.
*   **Barra de Stamina:** Mecânica neutra que acumula energia baseada na interatividade do combate. A barra carrega sob três condições:
    1. Causar dano ao inimigo.
    2. Sofrer dano do inimigo.
    3. Defender um golpe com sucesso (ativando a janela de defesa).
*   Quando a barra de stamina atinge 100%, libera o **Ataque Especial** do personagem.

## 4. Itens Consumíveis e Mochila
Itens são recursos táticos finitos que alteram a batalha sem depender do kit de habilidades do personagem em campo. Eles ocupam o turno inteiro para serem utilizados.
*   **Kit Médico:** Cura o HP do personagem ativo.
*   **Remédio:** Limpa efeitos negativos ("debuffs") aplicados ao personagem.
*   **Poções (+10%):** Aumentam o Ataque ou a Defesa temporariamente durante a batalha.

## 5. Sistema de Efeitos e Status (AoT - Ao Redor do Tempo)
As condições de status duram por exatos **3 turnos**. Uma mecânica muito importante é que **os efeitos acompanham o lutador mesmo que ele seja trocado e vá para o banco de reservas**.
*   **Hazards Temporários (DoTs - Damage over Time):** Status como *Chamas*, *Afogamento* e *Veneno*, que infligem dano contínuo sempre ao fim do turno.
*   **Benefícios Contínuos (HoTs - Heal over Time):** Status como *Regeneração*, que cura passivamente o personagem a cada fim de turno.

## 6. O Cálculo de Combate (Efetividade e Defesa)
O motor matemático por trás do jogo, atuando como o verdadeiro "juiz" das partidas, já que todos os personagens possuem poder de combate (status) equivalente de fábrica.
*   **Corte da Defesa:** Cada ponto numérico em Defesa reduz **0,5%** do dano recebido. O limite de absorção ("teto de armadura") é de **50% de corte de dano** (alcançado com 100 pontos de Defesa).
*   **Multiplicadores de Vantagem:**
    *   Vantagem Simples (Classe **ou** Elemento): +30% de Dano Causado / -30% Dano Recebido.
    *   Vantagem Dupla (Classe **e** Elemento): +50% de Dano Causado / -50% Dano Recebido.

## 7. Economia e Loja (Meta-jogo)
Mecânica que ocorre fora da arena de combate, incentivando o ciclo de gameplay.
*   **Sistema de Moedas:** A moeda de troca é ganha baseada em desempenho:
    *   Vitória = 100 moedas.
    *   Derrota = 50 moedas.
    *   Ganhar o Torneio (Boss) = 500 moedas.
*   **Aprimoramento Permanente:** O jogador investe moedas para ganhar +3% de atributos brutos (HP, Atq, Def, Vel), limitando-se a 5 aprimoramentos por status (teto máximo de ganho de +15%).
