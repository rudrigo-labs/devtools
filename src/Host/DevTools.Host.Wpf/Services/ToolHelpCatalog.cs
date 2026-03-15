namespace DevTools.Host.Wpf.Services;

public sealed record ToolHelpContent(
    string Title,
    string Objetivo,
    string ComoUsar,
    string Exemplo,
    string Observacoes);

public static class ToolHelpCatalog
{
    private static readonly Dictionary<string, ToolHelpContent> Items = new(StringComparer.OrdinalIgnoreCase)
    {
        ["snapshot:execution"] = new(
            "Snapshot - Execução",
            "Gerar um retrato textual do projeto para revisão técnica, documentação e contexto para análise de código.",
            Lines(
                "1. Defina a pasta raiz do projeto que será analisado.",
                "2. Escolha a pasta de saída onde os arquivos do snapshot serão gravados.",
                "3. Ajuste filtros de exclusão e extensões para reduzir ruído (ex.: bin, obj, artefatos).",
                "4. Execute primeiro em um escopo menor para validar o volume gerado.",
                "5. Revise o resultado e refine os filtros até o snapshot ficar objetivo."),
            Lines(
                "Cenário: preparar material para revisão de arquitetura.",
                "Raiz: C:\\repo\\api",
                "Saída: C:\\tmp\\snapshot-api",
                "Extensões incluídas: .cs, .json, .md",
                "Pastas ignoradas: bin, obj, .git"),
            Lines(
                "Prefira snapshots menores e focados por módulo quando o projeto for grande.",
                "Quando o resultado ficar extenso, reduza extensões e aumente exclusões.",
                "Evite incluir arquivos gerados automaticamente para não poluir a análise.")),

        ["snapshot:configuration"] = new(
            "Snapshot - Configuração",
            "Salvar perfis de snapshot reutilizáveis para repetir execuções sem reconfigurar todos os campos.",
            Lines(
                "1. Informe um nome de perfil claro (ex.: Snapshot API Produção).",
                "2. Preencha os caminhos padrão de entrada e saída.",
                "3. Defina extensões, globs e pastas ignoradas conforme o tipo de projeto.",
                "4. Salve e valide carregando o perfil novamente.",
                "5. Mantenha perfis separados por contexto (backend, frontend, monorepo)."),
            Lines(
                "Perfil: Snapshot Backend",
                "Inclui: .cs, .json, .md",
                "Ignora: bin, obj, .git, node_modules",
                "Uso esperado: gerar insumo para auditoria técnica semanal"),
            Lines(
                "Padronizar perfis reduz erros de operação entre equipes.",
                "Atualize o perfil sempre que a estrutura do repositório mudar.",
                "Evite nomes genéricos para facilitar manutenção.")),

        ["rename:execution"] = new(
            "Rename - Execução",
            "Renomear termos em lote no conteúdo e na estrutura de arquivos com segurança e rastreabilidade.",
            Lines(
                "1. Preencha texto antigo e texto novo com atenção à grafia exata.",
                "2. Escolha o modo de renomeação (geral ou focado em namespace).",
                "3. Configure filtros de exclusão de pastas e extensões não textuais.",
                "4. Rode em simulação para revisar impactos antes de aplicar.",
                "5. Execute de fato somente após validar o preview das alterações."),
            Lines(
                "Antigo: OldCompany.Module",
                "Novo: NewCompany.Module",
                "Modo: Namespace .NET",
                "Simulação: ligada",
                "Exclusões: bin, obj, .git"),
            Lines(
                "Sempre faça backup ou commit antes de renomeações amplas.",
                "Renomeações de namespace podem afetar caminhos, referências e CI.",
                "Use escopo pequeno no primeiro teste para reduzir risco.")),

        ["harvest:execution"] = new(
            "Harvest - Execução",
            "Minerar código potencialmente reutilizável e gerar um conjunto organizado para reaproveitamento.",
            Lines(
                "1. Defina a pasta de origem do código e a pasta de destino do resultado.",
                "2. Ajuste score mínimo para controlar o nível de relevância.",
                "3. Configure extensões e exclusões para focar em arquivos úteis.",
                "4. Rode sem aplicar para validar o relatório.",
                "5. Quando o resultado estiver bom, execute com cópia dos arquivos selecionados."),
            Lines(
                "Origem: C:\\repo\\monolito",
                "Destino: C:\\repo\\harvest-output",
                "Score mínimo: 6",
                "Aplicar cópia: desmarcado na primeira execução"),
            Lines(
                "Comece com score baixo e aumente gradualmente conforme o volume.",
                "Arquivos utilitários costumam aparecer com melhor qualidade quando filtros estão bem ajustados.",
                "Revise manualmente o material minerado antes de reutilizar.")),

        ["harvest:configuration"] = new(
            "Harvest - Configuração",
            "Cadastrar perfis de mineração por tipo de projeto para acelerar execuções recorrentes.",
            Lines(
                "1. Crie um perfil com nome representando o contexto de uso.",
                "2. Salve origem, destino e filtros padrão no perfil.",
                "3. Defina score mínimo inicial adequado ao seu repositório.",
                "4. Teste com um subconjunto de pastas.",
                "5. Ajuste e salve novamente quando necessário."),
            Lines(
                "Perfil: Harvest Backend",
                "Extensões: .cs, .ts",
                "Ignorados: bin, obj, node_modules",
                "Score padrão: 5"),
            Lines(
                "Perfis separados por domínio evitam mistura de contexto.",
                "Evite usar destino dentro da própria origem para não gerar recursão acidental.",
                "Documente internamente o objetivo de cada perfil.")),

        ["imagesplit:execution"] = new(
            "Image Split - Execução",
            "Detectar componentes visuais em uma imagem e exportar recortes automaticamente.",
            Lines(
                "1. Selecione o arquivo de imagem de entrada.",
                "2. Defina a pasta de saída e prefixo dos arquivos gerados.",
                "3. Ajuste alpha threshold e tamanho mínimo para controlar detecção.",
                "4. Configure índice inicial e extensão de saída.",
                "5. Execute e revise os recortes gerados."),
            Lines(
                "Entrada: sprite.png",
                "Saída: C:\\assets\\slices",
                "Alpha threshold: 10",
                "Mínimo: 3x3 px",
                "Prefixo: sprite"),
            Lines(
                "Se recortar demais, aumente tamanho mínimo.",
                "Se deixar elementos de fora, reduza alpha threshold ou mínimo.",
                "Ative sobrescrita apenas quando tiver certeza do destino.")),

        ["searchtext:execution"] = new(
            "Search Text - Execução",
            "Buscar termos em arquivos com controle por regex, case sensitivity e escopo por glob.",
            Lines(
                "1. Informe a pasta raiz da busca.",
                "2. Digite o padrão de busca (texto simples ou regex).",
                "3. Configure include/exclude globs para focar o escopo.",
                "4. Ajuste opções: regex, diferenciação de maiúsculas e palavra inteira.",
                "5. Execute e use os resultados para navegar rapidamente para os arquivos."),
            Lines(
                "Padrão: IService",
                "Regex: desligado",
                "Include: **/*.cs",
                "Exclude: **/bin/**; **/obj/**",
                "Máximo de resultados: 0 (sem limite)"),
            Lines(
                "Regex é poderosa, mas pode aumentar falsos positivos quando mal definida.",
                "Para buscas amplas, prefira começar sem limite e depois refinar globs.",
                "Em bases grandes, filtros corretos reduzem tempo de execução.")),

        ["organizer:execution"] = new(
            "Organizer - Execução",
            "Classificar e organizar arquivos automaticamente com base em categorias e pontuação.",
            Lines(
                "1. Defina pasta de entrada (e saída, se necessário).",
                "2. Configure score mínimo para aceitação da classificação.",
                "3. Rode em simulação para revisar para onde cada arquivo iria.",
                "4. Ajuste score e regras caso a classificação fique imprecisa.",
                "5. Execute com aplicação real após validar a simulação."),
            Lines(
                "Entrada: C:\\docs",
                "Saída: C:\\docs-organizados",
                "Score mínimo: 3",
                "Aplicar alterações: desmarcado no primeiro teste"),
            Lines(
                "Resultados melhores aparecem quando os nomes de arquivos têm contexto semântico.",
                "Use simulação sempre que mudar score ou regras de classificação.",
                "Evite executar em diretórios sem backup inicial.")),

        ["utf8convert:execution"] = new(
            "UTF8 Convert - Execução",
            "Padronizar codificação de arquivos para UTF-8 em lote, com opções de segurança.",
            Lines(
                "1. Defina a pasta raiz a ser processada.",
                "2. Configure include/exclude globs para limitar o escopo.",
                "3. Escolha opções de BOM e backup conforme a necessidade do projeto.",
                "4. Rode em simulação para conferir o impacto.",
                "5. Execute a conversão real somente após validação."),
            Lines(
                "Raiz: C:\\repo\\backend",
                "Include: **/*.cs; **/*.json",
                "Exclude: **/bin/**; **/obj/**",
                "Backup: ligado",
                "Simulação: ligada na primeira execução"),
            Lines(
                "Mantenha backup ativo em migrações iniciais de codificação.",
                "Nem todo projeto exige BOM; valide o padrão esperado pelo build/linters.",
                "Faça commit antes de conversões massivas.")),

        ["migrations:execution"] = new(
            "Migrations - Execução",
            "Executar fluxos de migration do Entity Framework com configurações salvas por contexto.",
            Lines(
                "1. Selecione a configuração correta (projeto, startup, provider e DbContext).",
                "2. Escolha a ação desejada (add, update, remove, script etc.).",
                "3. Preencha parâmetros obrigatórios, como nome da migration.",
                "4. Revise o resumo exibido antes de executar.",
                "5. Execute e valide logs e artefatos gerados."),
            Lines(
                "Ação: AddMigration",
                "Nome: AddUsersAudit",
                "Provider: Sqlite",
                "Contexto: AppDbContext",
                "Projeto de startup: src/Api"),
            Lines(
                "Garanta que o contexto e o startup estejam apontando para o ambiente correto.",
                "Erros de provider normalmente indicam configuração inconsistente entre projetos.",
                "Após gerar migration, revise o código antes de aplicar em produção.")),

        ["migrations:configuration"] = new(
            "Migrations - Configuração",
            "Cadastrar perfis de migrations para diferentes ambientes, bancos e contextos de aplicação.",
            Lines(
                "1. Informe nome do perfil e descrição do ambiente.",
                "2. Configure projetos de startup e de migrations.",
                "3. Defina DbContext e provider corretos.",
                "4. Salve e valide com uma ação simples (ex.: listagem).",
                "5. Mantenha perfis separados para dev, homologação e produção."),
            Lines(
                "Perfil: API-Sqlite-Dev",
                "Startup: src/Api",
                "Migrations: src/Infra",
                "DbContext: AppDbContext",
                "Provider: Sqlite"),
            Lines(
                "Perfis claros reduzem risco de executar no banco errado.",
                "Padronize nomenclatura para facilitar uso por toda a equipe.",
                "Atualize o perfil sempre que houver mudança estrutural no projeto.")),

        ["sshtunnel:execution"] = new(
            "SSH Tunnel - Execução",
            "Iniciar e encerrar túneis SSH locais para acessar serviços remotos com segurança.",
            Lines(
                "1. Selecione uma configuração de túnel previamente salva.",
                "2. Confira host remoto e mapeamento de portas local/remota.",
                "3. Inicie o túnel e valide o status de conexão.",
                "4. Use o serviço local apontando para a porta local configurada.",
                "5. Encerre o túnel ao finalizar o uso."),
            Lines(
                "Host SSH: ssh.empresa.com:22",
                "Mapeamento: 127.0.0.1:5433 -> 127.0.0.1:5432",
                "Uso típico: acessar banco remoto com cliente local"),
            Lines(
                "Verifique se a porta local está livre antes de iniciar.",
                "Falhas de autenticação costumam estar ligadas à chave ou usuário incorretos.",
                "Não mantenha túnel aberto sem necessidade.")),

        ["sshtunnel:configuration"] = new(
            "SSH Tunnel - Configuração",
            "Salvar dados de conexão SSH e regras de encaminhamento de portas em perfis reutilizáveis.",
            Lines(
                "1. Informe host, porta SSH e usuário.",
                "2. Configure método de autenticação (chave/senha) conforme política do ambiente.",
                "3. Defina host/porta local e host/porta remota do túnel.",
                "4. Salve com nome descritivo do destino.",
                "5. Teste a conexão imediatamente após salvar."),
            Lines(
                "Perfil: DB-Prod",
                "SSH: usuario@ssh.empresa.com:22",
                "Local: 127.0.0.1:5433",
                "Remoto: 127.0.0.1:5432"),
            Lines(
                "Nomes claros evitam conexões em ambiente incorreto.",
                "Prefira autenticação por chave quando possível.",
                "Evite salvar credenciais sensíveis fora do mecanismo seguro definido pela equipe.")),

        ["ngrok:execution"] = new(
            "Ngrok - Execução",
            "Expor um serviço local para acesso externo temporário por meio de túnel público.",
            Lines(
                "1. Selecione um perfil de execução já configurado.",
                "2. Informe protocolo e porta local do serviço.",
                "3. Inicie o túnel e acompanhe URL pública gerada.",
                "4. Use a URL para testes externos (webhook, demo, validação).",
                "5. Encerre o túnel ao concluir o teste."),
            Lines(
                "Protocolo: http",
                "Porta local: 5000",
                "Ação: iniciar túnel",
                "Uso típico: receber webhook em ambiente local"),
            Lines(
                "Confirme token e região antes de iniciar para evitar erro de autenticação.",
                "Não exponha endpoints administrativos sem proteção.",
                "Prefira túneis temporários e controle quem recebe a URL.")),

        ["ngrok:configuration"] = new(
            "Ngrok - Configuração",
            "Cadastrar auth token e parâmetros padrão para execução consistente de túneis.",
            Lines(
                "1. Configure auth token válido da conta ngrok.",
                "2. Defina região padrão e opções recorrentes.",
                "3. Salve o perfil com nome descritivo do cenário de uso.",
                "4. Teste abrindo um túnel simples.",
                "5. Revise periodicamente token e parâmetros de segurança."),
            Lines(
                "Perfil: Dev Local",
                "Região: sa",
                "Token: configurado",
                "Uso: exposição temporária de API local"),
            Lines(
                "Evite compartilhar token em logs ou capturas de tela.",
                "Defina limites de exposição quando possível.",
                "Para ambientes críticos, combine com autenticação adicional na aplicação.")),

        ["notes:execution"] = new(
            "Notes - Execução",
            "Criar, editar, organizar e salvar notas técnicas com fluxo rápido dentro do DevTools.",
            Lines(
                "1. Abra uma nota existente ou clique em Nova nota.",
                "2. Preencha título e selecione formato adequado (.md ou texto).",
                "3. Edite o conteúdo e use Salvar (Ctrl+S) para persistir.",
                "4. Exporte backup ZIP periodicamente para proteção do conteúdo.",
                "5. Use busca e títulos padronizados para localizar notas rapidamente."),
            Lines(
                "Título: Reunião arquitetura",
                "Formato: .md",
                "Conteúdo: decisões técnicas, pendências e checklist",
                "Ação complementar: exportar backup semanal"),
            Lines(
                "Padronize títulos por data ou tema para melhorar organização.",
                "Evite notas muito grandes sem seções; prefira dividir por assunto.",
                "Faça backup antes de limpezas em lote.")),

        ["notes:configuration"] = new(
            "Notes - Configuração",
            "Definir pasta padrão das notas e integração opcional com Google Drive.",
            Lines(
                "1. Selecione a pasta local principal das notas.",
                "2. Se usar Google Drive, configure credenciais OAuth e ID da pasta.",
                "3. Salve a configuração e teste criação/sincronização de uma nota.",
                "4. Revise permissões da conta para evitar erro de acesso.",
                "5. Mantenha credenciais protegidas e fora de repositório."),
            Lines(
                "Pasta local: C:\\Notas",
                "Google Drive: Client ID e Client Secret preenchidos",
                "Pasta remota: ID copiado da URL do Drive"),
            Lines(
                "Nunca commite credenciais em arquivos versionados.",
                "Se trocar de conta Google, valide novamente permissões e pasta alvo.",
                "Mantenha uma rotina de backup local mesmo com sincronização ativa."))
    };

    public static bool TryGet(string? key, out ToolHelpContent content)
    {
        if (!string.IsNullOrWhiteSpace(key) && Items.TryGetValue(key, out content!))
            return true;

        content = default!;
        return false;
    }

    private static string Lines(params string[] lines)
        => string.Join(Environment.NewLine, lines);
}
