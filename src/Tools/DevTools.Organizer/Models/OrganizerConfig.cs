namespace DevTools.Organizer.Models;

public sealed class OrganizerConfig
{
    public string[] AllowedExtensions { get; set; } = [".pdf", ".txt", ".md", ".doc", ".docx"];
    public int MinScoreDefault { get; set; } = 3;
    public double FileNameWeight { get; set; } = 2.0;
    public bool DeduplicateByHash { get; set; } = true;
    public bool DeduplicateByName { get; set; } = true;
    public int DeduplicateFirstLines { get; set; } = 0;

    public List<OrganizerCategory> Categories { get; set; } = new()
    {
        new OrganizerCategory("Curriculos", "Curriculos", ["curriculo", "cv", "resume", "experiencia", "formacao", "skills", "linkedin"]),
        new OrganizerCategory("Manuais", "Manuais", ["manual", "instrucoes", "instalacao", "garantia", "manutencao", "modelo", "especificacoes"]),
        new OrganizerCategory("Contratos", "Contratos", ["contrato", "clausula", "contratante", "contratado", "vigencia", "rescisao"]),
        new OrganizerCategory("Comprovantes", "Comprovantes", ["comprovante", "pagamento", "transferencia", "pix", "recibo", "boleto", "extrato"]),
        new OrganizerCategory("NotasFiscais", "NotasFiscais", ["nota fiscal", "nf-e", "danfe", "chave de acesso", "emitente", "destinatario"])
    };
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
