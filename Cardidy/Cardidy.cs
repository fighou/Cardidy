﻿using Dedge.Model;

namespace Dedge;

/// <summary>
/// Validate or identify card number and cvv with Cardidy
/// </summary>
public static class Cardidy
{
    // The order matters since some cards can be superseded by other
    // ie. VisaElectron should be checked before Visa
    // ie. Verve should be checked before Discover
    private static readonly IEnumerable<ICard> knownCards = new ICard[]
    {
        new AmericanExpress(),
        new Uatp(),
        new UnionPay(),
        new Jcb(),
        new Mir(),

        new Verve(),
        new Discover(),

        new MaestroUk(),
        new Maestro(),
        new MasterCard(),

        new VisaElectron(),
        new Visa(),
    };

    /// <summary>
    /// Pass card number and it will return issuing network if found
    /// </summary>
    /// <remarks>
    /// This function doesn't use regex, instead it compares digit by digit. 
    /// Because we're not using regex in this function, it's easier to maintain.
    /// Most card type detection tutorials and libraries use regular expressions without references, often omitting or incorrectly detecting card types.
    /// This <a href="https://web.archive.org/web/20180904130300/https://creditcardjs.com/credit-card-type-detection">guide</a> explains the card type detection process.
    /// The closer we are to an official spec is the <a href="https://en.wikipedia.org/wiki/Payment_card_number#Issuer_identification_number_(IIN)">Wikipedia's Payment card number page</a>.
    /// This library aims to follow it.
    /// </remarks>
    /// <param name="number">The card number to identify</param>
    /// <param name="validateLength">Validate the length as part of the string identification. A false value can be useful to identify fragment of a card number. Default is true.</param>
    /// <param name="ignoreNoise">Ignore common noise found in card number. This noise is any of `- .`. Default is false.</param>
    /// <param name="handleAnonymization">Set any non-digits to zero. It is common to use "X" and "#" to hide some digits. Default is false.</param>
    /// <example>
    /// <code>
    /// var card = "4771320594033";
    /// var isVisa = Cardidy.Identify(card) == CardType.Visa;
    /// </code>
    /// </example>
    /// <returns>The issuing network identified.</returns>
    public static CardType Identify(string number, bool validateLength = true, bool ignoreNoise = false, bool handleAnonymization = false)
    {
        if (string.IsNullOrWhiteSpace(number))
            return CardType.Unknown;

        var numbers = (ignoreNoise ? CleanNoise(number) : number).ToList();
        var isShady = !StartsWithDigit(numbers) || (!handleAnonymization && HasNotANumber(numbers));
        if (isShady)
            return CardType.Unknown;

        var digits = numbers.Select(c => int.TryParse(c.ToString(), out var i) ? i : 0).ToList();
        var firstSixDigits = digits.Take(6).ToNumber().PadRight(6, 0);
        var card = knownCards.FirstOrDefault(knownCard => knownCard.Prefixes
                .Any(p => IsMatching(p, firstSixDigits, validateLength, knownCard.Lengths, digits.Count))
        );
        return card?.Name ?? CardType.Unknown;
    }

    private static bool HasNotANumber(IEnumerable<char> numbers) => numbers.Any(x => !char.IsDigit(x));

    private static IEnumerable<char> CleanNoise(string source) => source.Where(c => !"- .".Contains(c));

    private static bool StartsWithDigit(IEnumerable<char> source) => source is not null && char.IsDigit(source.FirstOrDefault());

    private static bool IsMatching(PaddedRange prefix, int firstSixDigits, bool validateLength, IEnumerable<int> lengths, int digitsCount) =>
        prefix.Contains(firstSixDigits) && (!validateLength || lengths.Contains(digitsCount));

    /// <summary>
    /// Pass card cvv and it will return its likely valitidy.
    /// </summary>
    /// <remarks>
    /// This function checks if the cvv contains only digits and if its length matchs the given issuing network. 
    /// </remarks>
    /// <param name="cvv">The cvv to validate</param>
    /// <param name="cardType">The idenfied issuing network</param>
    /// <example>
    /// <code>
    /// var cvv = "123";
    /// var visaCvvIsValid = Cardidy.IsCvvValid(CardType.Visa, cvv);
    /// </code>
    /// </example>
    /// <returns>True if it is seems alright.</returns>
    public static bool IsCvvValid(string cvv, CardType cardType)
        => cardType != CardType.Unknown && (cvv?.All(char.IsDigit) ?? false)
        && (cardType == CardType.AmericanExpress ? cvv.Length == 4 : cvv.Length == 3);
}