// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using FluentAssertions;
using NexaMediaServer.Common;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests for the SortName utility to ensure consistent language-aware normalization.
/// </summary>
public class SortNameTests
{
    /// <summary>
    /// English leading articles (a, an, the) should be removed.
    /// </summary>
    [Theory]
    [InlineData(" The Lord of the Rings ", "en", "Lord of the Rings")]
    [InlineData("The Beatles", "en", "Beatles")]
    [InlineData("A Clockwork Orange", "en", "Clockwork Orange")]
    [InlineData("An American Tail", "en", "American Tail")]
    public void EnglishArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// French leading articles and elisions (e.g., l', d') should be removed.
    /// </summary>
    [Theory]
    [InlineData("L’Amour", "fr", "Amour")]
    [InlineData("L'amour", "fr", "amour")]
    [InlineData("Le Fabuleux Destin d’Amélie Poulain", "fr", "Fabuleux Destin d’Amélie Poulain")]
    [InlineData("Une histoire vraie", "fr", "histoire vraie")]
    public void FrenchArticlesAndElisionsAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// German leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("Der Untergang", "de", "Untergang")]
    [InlineData("Die Ärzte", "de", "Ärzte")]
    [InlineData("Das Boot", "de", "Boot")]
    [InlineData("Eine Reise", "de", "Reise")]
    public void GermanArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Spanish leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("El Laberinto del Fauno", "es", "Laberinto del Fauno")]
    [InlineData("La La Land", "es", "La Land")]
    [InlineData("Los Planetas", "es", "Planetas")]
    [InlineData("Una Noche", "es", "Noche")]
    public void SpanishArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Italian leading articles and elisions should be removed.
    /// </summary>
    [Theory]
    [InlineData("Il Postino", "it", "Postino")]
    [InlineData("L’Avventura", "it", "Avventura")]
    [InlineData("Lo Chiamavano Trinità", "it", "Chiamavano Trinità")]
    [InlineData("Una Giornata Particolare", "it", "Giornata Particolare")]
    public void ItalianArticlesAndElisionsAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Portuguese leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("O Homem que Copiava", "pt", "Homem que Copiava")]
    [InlineData("A Cidade de Deus", "pt", "Cidade de Deus")]
    [InlineData("Os Paralamas do Sucesso", "pt", "Paralamas do Sucesso")]
    [InlineData("Uma Aventura", "pt", "Aventura")]
    public void PortugueseArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Dutch leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("De Staat", "nl", "Staat")]
    [InlineData("Het Nieuws", "nl", "Nieuws")]
    [InlineData("Een Nieuwe Dag", "nl", "Nieuwe Dag")]
    public void DutchArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Swedish leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("En man som heter Ove", "sv", "man som heter Ove")]
    [InlineData("Ett äventyr", "sv", "äventyr")]
    [InlineData("Den ofrivillige golfaren", "sv", "ofrivillige golfaren")]
    public void SwedishArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Norwegian leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("En natt på byen", "no", "natt på byen")]
    [InlineData("Et dukkehjem", "no", "dukkehjem")]
    [InlineData("Den lille havfrue", "no", "lille havfrue")]
    public void NorwegianArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Danish leading articles should be removed.
    /// </summary>
    [Theory]
    [InlineData("En mand der hedder Ove", "da", "mand der hedder Ove")]
    [InlineData("Et eventyr", "da", "eventyr")]
    [InlineData("Den lille havfrue", "da", "lille havfrue")]
    public void DanishArticlesAreRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Leading punctuation/quotes/brackets should be trimmed.
    /// </summary>
    [Theory]
    [InlineData("— The Office", "en", "Office")]
    [InlineData("(The) Leftovers", "en", "Leftovers")]
    [InlineData("\"The\" Wire", "en", "Wire")]
    [InlineData("[The] Mandalorian", "en", "Mandalorian")]
    public void LeadingPunctuationIsRemoved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }

    /// <summary>
    /// Diacritics should be preserved after normalization.
    /// </summary>
    [Theory]
    [InlineData("Été", "fr", "Été")]
    [InlineData("Señorita", "es", "Señorita")]
    [InlineData("Ångström", "sv", "Ångström")]
    public void DiacriticsArePreserved(string title, string lang, string expected)
    {
        SortName.Generate(title, lang).Should().Be(expected);
    }
}
