# Game Design Document

## 1. Visão Geral do Jogo

- **Título:** Combo Tactics
- **Gênero e Sistema:** RPG de turno focado em gerenciamento de esquadrão e combate tático 1v1 alternado.
- **Público-Alvo:** Fãs de RPG tático (+14 anos), entusiastas de otimização de builds e jogadores de PvP competitivo.
- **Classificação Indicativa:** 14 anos.
- **Modos de Jogo:** Multiplayer, Singleplayer, Player versus Player (PvP), Player vs IA.
- **Temática Central:** Um torneio de arena/clube da luta onde o jogador gerencia e reveza uma equipe de três lutadores em combates de alto risco.
- **Diferenciais:** Habilidades combinatórias de classe e sistema de apostas/gambling.
- **Referências e Competidores:** Persona, Pokémon, Baldur's Gate, Expedition 33.

## 2. Resumo da Jogabilidade

- **Estrutura de Equipe:** O jogador escolhe e gerencia uma equipe de 3 personagens de diferentes classes e elementos.
- **Menu Fora de Combate:** Permite a evolução de atributos dos personagens e a escolha da equipe.
- **Dinâmica de Combate:** Duelos 1v1 por turnos.
- **Ações por Turno:** O jogador pode escolher entre três ações:
  1. Usar um ataque/habilidade.
  2. Usar um item.
  3. Trocar o personagem ativo na arena.
- **Condição de Vitória:** A equipe que derrotar todos os integrantes do time adversário e sobreviver vence.

## 3. Efetividade e Dano

### 3.1 Efetividade entre Elementos

Ciclo de vantagem: **Fogo → Planta → Água → Fogo**.

| Elemento | É forte contra | É fraco contra |
| --- | --- | --- |
| Fogo | Planta | Água |
| Planta | Água | Fogo |
| Água | Fogo | Planta |

### 3.2 Efetividade entre Classes

Ciclo de vantagem: **Guerreiro → Mago → Tanque → Guerreiro**.

| Classe | É forte contra | É fraco contra |
| --- | --- | --- |
| Guerreiro | Mago | Tanque |
| Mago | Tanque | Guerreiro |
| Tanque | Guerreiro | Mago |

### 3.3 Como o Dano é Calculado

**Velocidade:** A Velocidade não entra na conta, ela só decide quem age primeiro no turno.

**Ataque:** Valor proporcional ao dano aplicado.

**Defesa:** Cada ponto de Defesa corta 0,5% do dano recebido. Como a Defesa vai até 100, o corte máximo é de 50%.

| Defesa | Dano recebido |
| --- | --- |
| 20 | −10% |
| 50 | −25% |
| 100 | −50% |

**Efetividade:** É aplicada uma porcentagem no Ataque/Defesa.

| Vantagens do personagem | Atacando | Defendendo |
| --- | --- | --- |
| Nenhuma | dano normal | dano normal |
| Uma (elemento **ou** classe) | +30% de dano causado | −30% de dano recebido |
| Duas (elemento **e** classe) | +50% de dano causado | −50% de dano recebido |

Exemplo: um Mago de Fogo contra um Tanque de Planta tem vantagem de classe (Mago → Tanque) e de elemento (Fogo → Planta). Nesse confronto ele causa +50% de dano e recebe −50% de dano.

## 4. Personagens e Status Base

Os valores abaixo foram equilibrados por simulação: os nove personagens têm poder de combate equivalente, e o resultado de cada duelo é decidido pelas vantagens de elemento e classe da seção 3.3, não por atributos superiores.

### 4.1 Guerreiros

| Personagem | Aparência | HP | Ataque | Defesa | Velocidade | Habilidades |
| --- | --- | --- | --- | --- | --- | --- |
| Guerreiro de Fogo | Mula sem cabeça | 620 | 130 | 45 | 77 | Corte Flamejante, Investida Vulcânica, Brado de Calor |
| Guerreiro de Água | Guerreiro-tubarão | 620 | 119 | 55 | 90 | Lâmina de Correnteza, Impacto Geiser, Defesa Fluida |
| Guerreiro de Planta | Curupira | 620 | 112 | 65 | 73 | Esmagamento com Raízes, Escudo de Madeira, Golpe Espinhoso |

### 4.2 Magos

| Personagem | Aparência | HP | Ataque | Defesa | Velocidade | Habilidades |
| --- | --- | --- | --- | --- | --- | --- |
| Mago de Fogo | Bruxo-fênix | 560 | 171 | 15 | 52 | Bola de Fogo, Explosão Solar, Chuva de Meteoros |
| Mago de Água | Poseidon | 560 | 160 | 25 | 65 | Jato de Pressão, Prisão Gélida, Nevasca |
| Mago de Planta | Cogumelo | 560 | 153 | 35 | 48 | Chicote de Vinhas, Esporos Tóxicos, Rajada de Folhas Cortantes |

### 4.3 Tanques

| Personagem | Aparência | HP | Ataque | Defesa | Velocidade | Habilidades |
| --- | --- | --- | --- | --- | --- | --- |
| Tanque de Fogo | Dragão | 880 | 73 | 75 | 27 | Escudo de Magma, Provocação Ardente, Impacto de Cinzas |
| Tanque de Água | Baleia | 880 | 68 | 85 | 40 | Barreira Hidráulica, Carapaça de Gelo, Maremoto Punitivo |
| Tanque de Planta | Árvore | 880 | 63 | 95 | 23 | Muralha de Troncos, Absorção Vital, Casca Grossa |

## 5. Mecânicas

Como **Combo Tactics** é um RPG Tático de turnos focado em duelos de arena 1v1 sem fases de exploração de mapas, o jogo **não possui** armadilhas físicas, barreiras espalhadas ou caixas destrutíveis pelo cenário (todo o dano vem das batalhas). No entanto, há diversos sistemas mecânicos, elementos de ambiente e regras que funcionam de forma independente dos personagens e governam o mundo do jogo.

### 5.1 Loja (Economia e Meta-jogo)

A loja fica disponível no menu fora de combate e é onde o jogador gasta as moedas que acumula nas batalhas para comprar itens e evoluir os personagens. 
- **Sistema de Moedas:** Vitória = 100 moedas; Derrota = 50 moedas; Ganhar o Torneio (Boss) = 500 moedas.
- Os itens são consumíveis usados como uma das três ações do turno (ocupam o turno inteiro para serem utilizados), e o catálogo é Kit Médico (cura HP do ativo), Remédio (limpa debuffs e efeitos negativos), Poção de Ataque e Defesa (+10%).
- A evolução é individual por personagem e soma 3% ao valor base de HP, Ataque, Defesa ou Velocidade, com no máximo 5 evoluções por atributo (teto de +15% de atributos brutos).

### 5.2 Efeitos Positivos e Negativos (AoT - Ao Redor do Tempo)

Os efeitos são aplicados por ataques especiais e duram por exatos 3 turnos. 
- Efeitos negativos (DoTs - Damage over Time) incluem Chamas, Afogamento e Veneno, que causam dano contínuo sempre ao fim de cada turno, e podem ser removidos com remédios.
- Efeitos positivos (HoTs - Heal over Time) incluem regeneração, que devolve HP passivamente a cada turno, e aumentos temporários de atributo. 

> Os efeitos acompanham o personagem mesmo quando ele está fora de batalha.

### 5.3 Stamina e Gestão de Turnos

A base estrutural do combate onde as regras de ações são regidas. O jogador só pode realizar **uma ação** por turno: (1) Atacar, (2) Usar Item ou (3) Trocar de Personagem ativo.
Cada jogador tem uma barra de stamina neutra que, quando cheia (100%), libera o ataque especial. A barra carrega baseada na interatividade do combate sob três condições: ao causar dano ao inimigo, ao sofrer dano do inimigo e ao defender com sucesso (ativando a janela de defesa).

### 5.4 Ataque Especial

Cada personagem tem três ataques: dois sempre disponíveis para o kit padrão, e o especial, muito mais forte, que exige a barra de stamina cheia.

### 5.5 Desvio e Contra-ataque (Janela de Resposta)

Uma mecânica interativa (QTE - Quick Time Event) que quebra o ritmo passivo dos turnos convencionais. Quando o personagem do jogador é alvo de um ataque, abre-se uma curta janela de tempo em que ele pode clicar no momento certo para reagir ativamente ao invés de apenas receber dano. Acertar a janela resulta em:
- **Desvio:** O dano do ataque é 100% anulado.
- **Contra-ataque:** O alvo não apenas anula o dano, mas devolve o golpe no oponente.

### 5.6 Efeitos de Rodadas (Mudança de Arena)

Esta é a principal mecânica de "hazard" do jogo. A arena é um ambiente dinâmico que afeta os lutadores de ambos os lados da batalha.
A cada 3 rodadas, um sorteio no estilo roleta de cassino altera completamente o ambiente da arena, que passa a favorecer alguns personagens e prejudicar outros. O efeito do ambiente aplica um modificador de +-10% no dano.

Ambientes e seus efeitos:

| Ambiente | Favorece | Prejudica |
| --- | --- | --- |
| Mar | Personagens de Água (+10% de dano) | Personagens de Fogo  (-10% de dano) |
| Vulcão | Personagens de Fogo (+10% de dano) | Personagens de Planta (-10% de dano) |
| Floresta | Personagens de Planta (+10% de dano) | Personagens de Água (-10% de dano) |
| Lua (Baixa gravidade) | Personagens de baixa Velocidade | Personagens de alta Velocidade |
