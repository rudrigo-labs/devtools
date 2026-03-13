namespace DevTools.Organizer.Models;

public enum OrganizerAction
{
    WouldMove,
    Moved,
    Duplicate,
    Ignored,
    Error
}

public sealed record OrganizerPlanItem(
    string Source,
    string Target,
    string Category,
    string Reason,
    OrganizerAction Action);

public sealed record OrganizerStats(
    int TotalFiles,
    int EligibleFiles,
    int WouldMove,
    int Duplicates,
    int Ignored,
    int Errors);

public sealed record OrganizerResult(
    string InboxPath,
    string OutputPath,
    OrganizerStats Stats,
    IReadOnlyList<OrganizerPlanItem> Plan);

public sealed class OrganizerRequest
{
    public string InboxPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int MinScore { get; set; } = 3;
    public bool Apply { get; set; } = false;
}

public sealed class OrganizerConfig
{
    public string[] AllowedExtensions { get; set; } = [".pdf", ".txt", ".md", ".doc", ".docx"];
    public double FileNameWeight { get; set; } = 2.0;
    public bool DeduplicateByHash { get; set; } = true;
    public bool DeduplicateByName { get; set; } = true;
    public int DeduplicateFirstLines { get; set; } = 0;

    public List<OrganizerCategory> Categories { get; set; } =
    [
        new OrganizerCategory("Curriculos",    "Curriculos",    ["curriculo", "cv", "resume", "experiencia", "formacao", "skills", "linkedin"]),
        new OrganizerCategory("Manuais",       "Manuais",       ["manual", "instrucoes", "instalacao", "garantia", "manutencao", "modelo", "especificacoes"]),
        new OrganizerCategory("Contratos",     "Contratos",     ["contrato", "clausula", "contratante", "contratado", "vigencia", "rescisao"]),
        new OrganizerCategory("Comprovantes",  "Comprovantes",  ["comprovante", "pagamento", "transferencia", "pix", "recibo", "boleto", "extrato"]),
        new OrganizerCategory("NotasFiscais",  "NotasFiscais",  ["nota fiscal", "nf-e", "danfe", "chave de acesso", "emitente", "destinatario"])
    ];
}

public sealed class OrganizerCategory
{
    public OrganizerCategory() { }

    public OrganizerCategory(string name, string folder, string[] keywords)
    {
        Name = name;
        Folder = folder;
        Keywords = keywords;
    }

    public string Name { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string[] NegativeKeywords { get; set; } = Array.Empty<string>();
    public int KeywordWeight { get; set; } = 2;
    public int NegativeWeight { get; set; } = 2;
    public int? MinScore { get; set; }
}
