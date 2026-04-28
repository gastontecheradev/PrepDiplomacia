using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PrepDiplomacia.Infrastructure.Services;

/// <summary>
/// Genera slugs URL-friendly: "El concurso 2027 — Análisis" → "el-concurso-2027-analisis".
/// </summary>
public static class SlugHelper
{
    public static string Generar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        // 1. Normalizar (quitar acentos)
        var normalized = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        var sinAcentos = sb.ToString().Normalize(NormalizationForm.FormC);

        // 2. Lowercase + reemplazar no-alfanuméricos por guiones
        var lower = sinAcentos.ToLowerInvariant();
        var slug = Regex.Replace(lower, @"[^a-z0-9]+", "-");

        // 3. Trim guiones
        slug = slug.Trim('-');

        // 4. Limitar longitud
        if (slug.Length > 200) slug = slug.Substring(0, 200).TrimEnd('-');

        return slug;
    }
}
