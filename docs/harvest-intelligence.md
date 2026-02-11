# DevTools.Harvest V2 – Inteligência Determinística

Status: Concluido (2026-02-07)

## 1. O Porquê da Mudança

A versão atual do **Harvest** é baseada em heurísticas simples (nomes de arquivos e pastas). Embora rápido, isso gera muitos "falsos positivos" e não consegue distinguir um arquivo utilitário vital de um arquivo de código morto (Dead Code) que apenas tem um nome bonito.

A tentação seria usar IA (LLMs) para analisar o código, mas isso viola nossos princípios:
1.  **Custo/Lentidão:** IA exige tokens ou hardware pesado.
2.  **Indeterminismo:** A IA pode "alucinar" ou variar a resposta.
3.  **Filosofia:** Queremos "Certezas > Mágica".

A V2 tornará o Harvest inteligente usando **Matemática de Grafos e Análise Estática**, não IA.

---

## 2. Como vai funcionar (O Motor Matemático)

O novo motor do Harvest (`DevTools.Harvest.Core`) analisará o código fonte buscando **fatos**, não suposições.

### A. Análise de Influência (O Grafo de Dependência)
A métrica mais honesta sobre a importância de um código é: **"Quantas pessoas precisam dele?"**.

1.  **Fan-In (Popularidade):**
    *   O motor varre todos os arquivos `.cs` (ou `.ts`).
    *   Conta quantas vezes a classe `StringHelper` é instanciada ou referenciada (`using` ou chamadas diretas).
    *   **Lógica:** Se 50 arquivos usam `StringHelper`, ele é **Crítico**, independente do nome.

2.  **Fan-Out (Orquestração):**
    *   Conta quantas dependências externas um arquivo tem.
    *   **Lógica:** Se um arquivo chama 50 outros, ele provavelmente é um **Controlador/Orquestrador**, não um utilitário puro.

### B. Categorização Semântica (Densidade de Keywords)
Para saber *o que* o arquivo faz sem ler o código com IA, usaremos **Densidade de Palavras-Chave**.

*   O sistema terá dicionários de temas (Security, Database, UI, IO).
*   **Exemplo:**
    *   Arquivo contém: `Encrypt`, `AES`, `Hash`, `Salt`.
    *   Cálculo: 15 ocorrências em 100 linhas = Alta densidade de **Segurança**.
    *   Tag automática: `[Security]`.

---

## 3. Estrutura de Pastas (Library V2)

Seguindo o padrão da V2, o Harvest será reestruturado assim:

### 📂 `src/DevTools/DevTools.Harvest/`

#### 📦 `DevTools.Harvest.Core`
*   **`Engine/`**
    *   `DependencyGraphBuilder.cs`: Monta o mapa de quem chama quem.
    *   `KeywordAnalyzer.cs`: Conta a densidade de palavras.
    *   `ScoringEngine.cs`: Aplica os pesos (Fan-In vale x2, Fan-Out vale x0.5) e gera o Score final.
*   **`Models/`**
    *   `FileNode.cs`: Representa um arquivo no grafo.
    *   `HarvestReport.cs`: O resultado final com a lista ordenada e explicada.
*   **`Configuration/`**
    *   `HarvestConfig.json`: Definição de pesos e keywords (para não ficar hardcoded).

---

## 4. Explicabilidade (O "Porquê")

Diferente da IA, que é uma "caixa preta", o Harvest V2 explicará exatamente o resultado.

**Exemplo de Saída:**
```text
Arquivo: SecurityUtils.cs
Score: 95/100
-------------------------
Motivo:
[+] Fan-In Alto: Referenciado por 42 arquivos (+40 pts)
[+] Densidade: Alta concentração de termos de 'Security' (+30 pts)
[+] Estático: Contém 5 métodos públicos estáticos (+15 pts)
[-] Complexidade: Baixo Fan-Out (não depende de ninguém) (+10 pts)
```

## 5. Conclusão

Essa abordagem transforma o Harvest de um "leitor de nomes" para uma ferramenta de **Engenharia de Software** real, capaz de identificar dívida técnica, código morto e bibliotecas centrais do sistema, tudo rodando em milissegundos localmente.
```

### Resumo da Execução
1.  **Criado:** `src/DevTools/docs/v2/architecture-refactoring.md` - Detalha a separação Core/CLI/WPF e o padrão de pastas.
2.  **Criado:** `src/DevTools/docs/v2/harvest-intelligence.md` - Detalha a lógica de Fan-In/Fan-Out e densidade de keywords.

Aguardando instruções para iniciar a refatoração do código (Motor) com base nesses documentos.

<!--
[PROMPT_SUGGESTION]Comece a refatoração criando o projeto DevTools.Snapshot.Core e movendo a lógica atual para lá seguindo a nova estrutura.[/PROMPT_SUGGESTION]
[PROMPT_SUGGESTION]Crie a classe base 'Result<T>' no DevTools.Core para ser usada por todas as Engines.[/PROMPT_SUGGESTION]
