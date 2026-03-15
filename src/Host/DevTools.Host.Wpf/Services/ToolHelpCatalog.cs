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
            "Gerar um resumo textual do projeto para análise.",
            "Selecione pasta raiz e pasta de saída. Ajuste filtros e clique em Executar.",
            "Raiz: C:\\repo\\app | Saída: C:\\out | Extensões: .cs,.md",
            "Use os filtros para reduzir ruído e gerar um arquivo mais útil."),

        ["snapshot:configuration"] = new(
            "Snapshot - Configuração",
            "Salvar um perfil de snapshot reutilizável.",
            "Preencha nome/descrição e os campos do perfil, depois clique em Salvar.",
            "Perfil: \"Snapshot API\" com filtros de bin/obj e extensões .cs,.json",
            "Use configurações nomeadas para evitar retrabalho."),

        ["rename:execution"] = new(
            "Rename - Execução",
            "Renomear termos em lote no projeto.",
            "Informe texto antigo/novo, escolha modo e clique em Executar.",
            "Antigo: OldCompany | Novo: NewCompany | Simulação: ligado",
            "Execute em simulação antes de aplicar alterações reais."),

        ["harvest:execution"] = new(
            "Harvest - Execução",
            "Minerar código reutilizável de um projeto.",
            "Selecione origem/destino, ajuste score mínimo e clique em Executar.",
            "Origem: C:\\repo | Destino: C:\\harvest | Score mínimo: 5",
            "Comece com score baixo e refine depois."),

        ["harvest:configuration"] = new(
            "Harvest - Configuração",
            "Salvar parâmetros padrão de mineração.",
            "Crie uma configuração com caminhos e filtros e clique em Salvar.",
            "Perfil: \"Harvest Backend\" com filtros de extensao e pastas ignoradas",
            "Perfis diferentes ajudam por tipo de projeto."),

        ["imagesplit:execution"] = new(
            "Image Split - Execução",
            "Recortar componentes detectados em uma imagem.",
            "Selecione arquivo, pasta de saída e parâmetros de detecção. Execute.",
            "Input: sprite.png | Alpha: 10 | Mínimo: 3x3",
            "Ajuste alpha/mínimo para evitar cortes indevidos."),

        ["searchtext:execution"] = new(
            "Search Text - Execução",
            "Buscar texto em arquivos com filtros.",
            "Defina raiz, padrão e opções (regex, case, globs). Clique em Buscar.",
            "Padrão: \"IService\" | Include: **/*.cs | Regex: desligado",
            "Use regex apenas quando necessário."),

        ["organizer:execution"] = new(
            "Organizer - Execução",
            "Organizar arquivos por categorias.",
            "Informe pasta de entrada, score mínimo e clique em Executar.",
            "Entrada: C:\\docs | Score: 3 | Aplicar: desmarcado (simulação)",
            "Rode primeiro em simulação para validar a classificação."),

        ["utf8convert:execution"] = new(
            "UTF8 Convert - Execução",
            "Converter arquivos para UTF-8 em lote.",
            "Escolha raiz, globs e opções (BOM/backup/simulação). Execute.",
            "Raiz: C:\\repo | Include: **/*.cs | Simulação: ligado",
            "Mantenha backup ligado em conversões iniciais."),

        ["migrations:execution"] = new(
            "Migrations - Execução",
            "Executar comandos dotnet ef.",
            "Selecione a configuração e execute a ação desejada.",
            "Ação: AddMigration | Nome: AddUsers | Provider: Sqlite",
            "Confira projeto startup e DbContext antes de executar."),

        ["migrations:configuration"] = new(
            "Migrations - Configuração",
            "Salvar parâmetros de migration por ambiente/projeto.",
            "Preencha caminhos, DbContext e provider. Depois clique em Salvar.",
            "Perfil: \"API-Sqlite\" com startup e projeto de migrations",
            "Use um perfil por contexto/provedor."),

        ["sshtunnel:execution"] = new(
            "SSH Tunnel - Execução",
            "Iniciar/parar túnel SSH com parâmetros salvos.",
            "Selecione configuração e clique em Executar para iniciar/parar.",
            "Host: ssh.server.com | Local: 5433 -> Remote: 5432",
            "Valide porta local livre antes de iniciar."),

        ["sshtunnel:configuration"] = new(
            "SSH Tunnel - Configuração",
            "Salvar conexão SSH e mapeamento de portas.",
            "Preencha host/usuário/chave e portas. Clique em Salvar.",
            "Perfil: \"DB-Prod\" | Host 22 | 127.0.0.1:5433 -> 127.0.0.1:5432",
            "Mantenha nomes claros para evitar conexões erradas."),

        ["ngrok:execution"] = new(
            "Ngrok - Execução",
            "Abrir túnel público para serviço local.",
            "Selecione configuração, informe protocolo/porta e execute.",
            "Protocolo: http | Porta: 5000 | Ação: iniciar túnel",
            "Confirme token/região configurados antes de iniciar."),

        ["ngrok:configuration"] = new(
            "Ngrok - Configuração",
            "Salvar token e parâmetros padrão do ngrok.",
            "Preencha auth token, região e opções padrão. Clique em Salvar.",
            "Perfil: \"Dev Local\" | Região: sa | Token configurado",
            "Evite expor portas sensíveis sem controle."),

        ["notes:execution"] = new(
            "Notes - Execução",
            "Criar, editar e salvar notas.",
            "Abra/edite a nota e clique em Salvar nota.",
            "Título: Reuniao | Formato: .md | Conteúdo com checklist",
            "Use títulos curtos e padrão para facilitar busca."),

        ["notes:configuration"] = new(
            "Notes - Configuração",
            "Definir pasta padrão e integração Google Drive.",
            "Configure pasta local e credenciais, depois clique em Salvar.",
            "Pasta: C:\\Notas | Google Drive: Client ID/Secret preenchidos",
            "Guarde credenciais com cuidado e revise permissão da pasta.")
    };

    public static bool TryGet(string? key, out ToolHelpContent content)
    {
        if (!string.IsNullOrWhiteSpace(key) && Items.TryGetValue(key, out content!))
            return true;

        content = default!;
        return false;
    }
}
