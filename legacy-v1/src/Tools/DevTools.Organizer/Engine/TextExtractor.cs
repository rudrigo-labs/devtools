using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace DevTools.Organizer.Engine;

internal sealed class TextExtractor
{
    public string Extract(string filePath, int maxChars = 30000)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            if (ext is ".txt" or ".md" or ".log")
                return File.ReadAllText(filePath);

            if (ext == ".pdf")
            {
                var sb = new StringBuilder();
                using var doc = PdfDocument.Open(filePath);
                foreach (var page in doc.GetPages())
                {
                    sb.AppendLine(page.Text);
                    if (sb.Length > maxChars) break;
                }
                return sb.ToString();
            }

            if (ext == ".doc")
            {
                return TryExtractDoc(filePath, maxChars);
            }

            if (ext == ".docx")
            {
                using var archive = ZipFile.OpenRead(filePath);
                var entry = archive.GetEntry("word/document.xml");
                if (entry is null)
                    return string.Empty;

                using var stream = entry.Open();
                var doc = XDocument.Load(stream);
                var sb = new StringBuilder();

                foreach (var paragraph in doc.Descendants().Where(e => e.Name.LocalName == "p"))
                {
                    foreach (var text in paragraph.Descendants().Where(e => e.Name.LocalName == "t"))
                    {
                        sb.Append(text.Value);
                        if (sb.Length > maxChars) break;
                    }
                    sb.AppendLine();
                    if (sb.Length > maxChars) break;
                }

                return sb.ToString();
            }
        }
        catch
        {
            return string.Empty;
        }

        return string.Empty;
    }

    private static string TryExtractDoc(string filePath, int maxChars)
    {
        var hwpfType = Type.GetType("NPOI.HWPF.UserModel.HWPFDocument, NPOI");
        if (hwpfType is null)
            return string.Empty;

        using var stream = File.OpenRead(filePath);
        object? doc = null;
        try
        {
            doc = Activator.CreateInstance(hwpfType, stream);
            var prop = hwpfType.GetProperty("DocumentText");
            var text = prop?.GetValue(doc) as string ?? string.Empty;
            return text.Length > maxChars ? text[..maxChars] : text;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            if (doc is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
