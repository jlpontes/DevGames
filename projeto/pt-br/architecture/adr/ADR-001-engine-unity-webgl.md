# ADR-001: Unity WebGL como engine e plataforma

**Status:** Aceito
**Data:** 02/10/2026
**Decisores:** Time de desenvolvimento

## Contexto
O Combo Tactics precisa rodar num navegador de PC sem instalação (Caderno p.4). O time tem 3–5 desenvolvedores, cerca de 4 semanas até o lançamento em novembro de 2026, e já conhece Unity/C#. O jogo é 2D com câmera lateral fixa, por turnos, sem física em tempo real e sem rede.

## Decisão
Usar Unity (LTS atual) com build para **WebGL**.

## Opções Consideradas

### Opção A: Unity WebGL
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Média. A engine é conhecida, mas o WebGL tem suas peculiaridades |
| Custo | Gratuito (Unity Personal) |
| Escalabilidade | Suficiente para um jogo 2D por turnos |
| Familiaridade do time | **Alta** |

**Prós:** sem curva de aprendizado; ferramentas do editor (Inspector, Animator, ScriptableObjects); o mesmo projeto pode ir depois para desktop ou mobile.
**Contras:** download inicial grande (cerca de 10–30 MB comprimido); primeiro carregamento lento; sem threads; o áudio precisa de um gesto do usuário; mais fraco no Safari do celular.

### Opção B: Phaser 3 + TypeScript
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa para 2D na web |
| Custo | Gratuito |
| Escalabilidade | Suficiente |
| Familiaridade do time | Baixa |

**Prós:** pacote pequeno que carrega na hora; feito para a web.
**Contras:** o time aprenderia uma stack nova faltando 4 semanas; sem editor visual.

### Opção C: Godot (export HTML5)
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Média |
| Custo | Gratuito |
| Escalabilidade | Suficiente |
| Familiaridade do time | Baixa |

**Prós:** editor leve; bom para 2D.
**Contras:** ferramenta e linguagem novas; o export web tem seus próprios problemas de compatibilidade.

## Análise de Trade-offs
Com 4 semanas, **a familiaridade do time pesa mais que o encaixe com a plataforma**. O Phaser combina melhor com a web, mas o risco de aprender uma stack nova é maior que o custo do tempo de carregamento do Unity. Um jogo por turnos não sofre com os limites de desempenho do WebGL.

## Consequências
- Fica mais fácil: o time é produtivo desde o primeiro dia, e os designers ajustam o conteúdo no editor.
- Fica mais difícil: tamanho do build e tempo de carregamento precisam de orçamento; as diferenças entre navegadores precisam ser testadas cedo.
- Revisitar: para lançar no celular, considerar builds nativos de Android/iOS em vez de WebGL no celular.

## Itens de Ação
1. [ ] Fixar a versão LTS do Unity em `ProjectSettings/ProjectVersion.txt` e garantir que todo o time use a mesma.
2. [ ] Configurar o player WebGL: Brotli, Decompression Fallback, Strip Engine Code e uma tela de carregamento própria.
3. [ ] Publicar um build vazio no itch.io ou no GitHub Pages na semana 1.
