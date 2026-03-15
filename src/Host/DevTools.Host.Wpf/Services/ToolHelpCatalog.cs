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
            "Snapshot - Execucao",
            "Gerar um resumo textual do projeto para analise.",
            "Selecione pasta raiz e pasta de saida. Ajuste filtros e clique em Executar.",
            "Raiz: C:\\repo\\app | Saida: C:\\out | Extensoes: .cs,.md",
            "Use os filtros para reduzir ruido e gerar um arquivo mais util."),

        ["snapshot:configuration"] = new(
            "Snapshot - Configuracao",
            "Salvar um perfil de snapshot reutilizavel.",
            "Preencha nome/descricao e os campos do perfil, depois clique em Salvar.",
            "Perfil: \"Snapshot API\" com filtros de bin/obj e extensoes .cs,.json",
            "Use configuracoes nomeadas para evitar retrabalho."),

        ["rename:execution"] = new(
            "Rename - Execucao",
            "Renomear termos em lote no projeto.",
            "Informe texto antigo/novo, escolha modo e clique em Executar.",
            "Antigo: OldCompany | Novo: NewCompany | Simulacao: ligado",
            "Execute em simulacao antes de aplicar alteracoes reais."),

        ["harvest:execution"] = new(
            "Harvest - Execucao",
            "Minerar codigo reutilizavel de um projeto.",
            "Selecione origem/destino, ajuste score minimo e clique em Executar.",
            "Origem: C:\\repo | Destino: C:\\harvest | Score minimo: 5",
            "Comece com score baixo e refine depois."),

        ["harvest:configuration"] = new(
            "Harvest - Configuracao",
            "Salvar parametros padrao de mineracao.",
            "Crie uma configuracao com caminhos e filtros e clique em Salvar.",
            "Perfil: \"Harvest Backend\" com filtros de extensao e pastas ignoradas",
            "Perfis diferentes ajudam por tipo de projeto."),

        ["imagesplit:execution"] = new(
            "Image Split - Execucao",
            "Recortar componentes detectados em uma imagem.",
            "Selecione arquivo, pasta de saida e parametros de deteccao. Execute.",
            "Input: sprite.png | Alpha: 10 | Minimo: 3x3",
            "Ajuste alpha/minimo para evitar cortes indevidos."),

        ["searchtext:execution"] = new(
            "Search Text - Execucao",
            "Buscar texto em arquivos com filtros.",
            "Defina raiz, padrao e opcoes (regex, case, globs). Clique em Buscar.",
            "Padrao: \"IService\" | Include: **/*.cs | Regex: desligado",
            "Use regex apenas quando necessario."),

        ["organizer:execution"] = new(
            "Organizer - Execucao",
            "Organizar arquivos por categorias.",
            "Informe pasta de entrada, score minimo e clique em Executar.",
            "Entrada: C:\\docs | Score: 3 | Aplicar: desmarcado (simulacao)",
            "Rode primeiro em simulacao para validar a classificacao."),

        ["utf8convert:execution"] = new(
            "UTF8 Convert - Execucao",
            "Converter arquivos para UTF-8 em lote.",
            "Escolha raiz, globs e opcoes (BOM/backup/simulacao). Execute.",
            "Raiz: C:\\repo | Include: **/*.cs | Simulacao: ligado",
            "Mantenha backup ligado em conversoes iniciais."),

        ["migrations:execution"] = new(
            "Migrations - Execucao",
            "Executar comandos dotnet ef.",
            "Selecione a configuracao e execute a acao desejada.",
            "Acao: AddMigration | Nome: AddUsers | Provider: Sqlite",
            "Confira projeto startup e DbContext antes de executar."),

        ["migrations:configuration"] = new(
            "Migrations - Configuracao",
            "Salvar parametros de migration por ambiente/projeto.",
            "Preencha caminhos, DbContext e provider. Depois clique em Salvar.",
            "Perfil: \"API-Sqlite\" com startup e projeto de migrations",
            "Use um perfil por contexto/provedor."),

        ["sshtunnel:execution"] = new(
            "SSH Tunnel - Execucao",
            "Iniciar/parar tunel SSH com parametros salvos.",
            "Selecione configuracao e clique em Executar para iniciar/parar.",
            "Host: ssh.server.com | Local: 5433 -> Remote: 5432",
            "Valide porta local livre antes de iniciar."),

        ["sshtunnel:configuration"] = new(
            "SSH Tunnel - Configuracao",
            "Salvar conexao SSH e mapeamento de portas.",
            "Preencha host/usuario/chave e portas. Clique em Salvar.",
            "Perfil: \"DB-Prod\" | Host 22 | 127.0.0.1:5433 -> 127.0.0.1:5432",
            "Mantenha nomes claros para evitar conexoes erradas."),

        ["ngrok:execution"] = new(
            "Ngrok - Execucao",
            "Abrir tunel publico para servico local.",
            "Selecione configuracao, informe protocolo/porta e execute.",
            "Protocolo: http | Porta: 5000 | Acao: iniciar tunel",
            "Confirme token/regiao configurados antes de iniciar."),

        ["ngrok:configuration"] = new(
            "Ngrok - Configuracao",
            "Salvar token e parametros padrao do ngrok.",
            "Preencha auth token, regiao e opcoes padrao. Clique em Salvar.",
            "Perfil: \"Dev Local\" | Regiao: sa | Token configurado",
            "Evite expor portas sensiveis sem controle."),

        ["notes:execution"] = new(
            "Notes - Execucao",
            "Criar, editar e salvar notas.",
            "Abra/edite a nota e clique em Salvar nota.",
            "Titulo: Reuniao | Formato: .md | Conteudo com checklist",
            "Use titulos curtos e padrao para facilitar busca."),

        ["notes:configuration"] = new(
            "Notes - Configuracao",
            "Definir pasta padrao e integracao Google Drive.",
            "Configure pasta local e credenciais, depois clique em Salvar.",
            "Pasta: C:\\Notas | Google Drive: Client ID/Secret preenchidos",
            "Guarde credenciais com cuidado e revise permissao da pasta.")
    };

    public static bool TryGet(string? key, out ToolHelpContent content)
    {
        if (!string.IsNullOrWhiteSpace(key) && Items.TryGetValue(key, out content!))
            return true;

        content = default!;
        return false;
    }
}
