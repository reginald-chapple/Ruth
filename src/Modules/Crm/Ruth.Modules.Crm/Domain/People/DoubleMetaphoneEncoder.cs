using System.Text;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>
/// Pure C# Lawrence Phillips Double Metaphone encoder producing up to 4-character primary and alternate phonetic keys.
/// </summary>
public static partial class DoubleMetaphoneEncoder
{
    private const int MaxKeyLength = 4;

    /// <summary>Computes primary and optional alternate Double Metaphone keys for <paramref name="word" />.</summary>
    public static DoubleMetaphoneResult Encode(string? word)
    {
        var normalized = NormalizeLetters(word);
        if (normalized.Length == 0)
        {
            return new DoubleMetaphoneResult(string.Empty, AlternateKey: null);
        }

        var state = new EncoderState(normalized);
        state.HandleLeadingExceptions();
        state.ProcessAll();
        return state.BuildResult();
    }

    private static string NormalizeLetters(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input.Trim())
        {
            var upper = char.ToUpperInvariant(ch);
            if (upper is (>= 'A' and <= 'Z') or 'Ç' or 'Ñ')
            {
                sb.Append(upper);
            }
        }

        return sb.ToString();
    }

    private sealed partial class EncoderState(string word)
    {
        private readonly string _word = word;
        private readonly StringBuilder _primary = new(MaxKeyLength);
        private readonly StringBuilder _alternate = new(MaxKeyLength);
        private int _index;

        private bool IsComplete =>
            _primary.Length >= MaxKeyLength && _alternate.Length >= MaxKeyLength;

        public void HandleLeadingExceptions()
        {
            if (MatchesAt(0, "GN", "KN", "PN", "WR", "PS"))
            {
                _index++;
            }
            else if (CharAt(0) == 'X')
            {
                Add("S");
                _index++;
            }
        }

        public void ProcessAll()
        {
            while (_index < _word.Length && !IsComplete)
            {
                ProcessCurrentChar();
            }
        }

        public DoubleMetaphoneResult BuildResult()
        {
            var primary =
                _primary.Length > MaxKeyLength
                    ? _primary.ToString(0, MaxKeyLength)
                    : _primary.ToString();
            var alternate =
                _alternate.Length > MaxKeyLength
                    ? _alternate.ToString(0, MaxKeyLength)
                    : _alternate.ToString();

            return string.Equals(primary, alternate, StringComparison.Ordinal)
                ? new DoubleMetaphoneResult(primary, AlternateKey: null)
                : new DoubleMetaphoneResult(primary, alternate);
        }

        private void ProcessCurrentChar()
        {
            var ch = _word[_index];
            if (IsVowel(ch))
            {
                if (_index == 0)
                {
                    Add("A");
                }

                _index++;
                return;
            }

            if (TryHandleSimpleConsonant(ch))
            {
                return;
            }

            HandleComplexConsonant(ch);
        }

        private static readonly Dictionary<char, (string Code, char RepeatChar)> _simpleConsonants =
            new()
            {
                ['B'] = ("P", 'B'),
                ['Ç'] = ("S", '\0'),
                ['F'] = ("F", 'F'),
                ['K'] = ("K", 'K'),
                ['L'] = ("L", 'L'),
                ['M'] = ("M", 'M'),
                ['N'] = ("N", 'N'),
                ['Ñ'] = ("N", 'N'),
                ['Q'] = ("K", 'Q'),
                ['R'] = ("R", 'R'),
                ['V'] = ("F", 'V'),
            };

        private bool TryHandleSimpleConsonant(char ch)
        {
            if (!_simpleConsonants.TryGetValue(ch, out var mapping))
            {
                return false;
            }

            Add(mapping.Code);
            _index +=
                mapping.RepeatChar != '\0' && CharAt(_index + 1) == mapping.RepeatChar ? 2 : 1;
            return true;
        }

        private void HandleComplexConsonant(char ch)
        {
            switch (ch)
            {
                case 'C':
                    HandleC();
                    break;
                case 'D':
                    HandleD();
                    break;
                case 'G':
                    HandleG();
                    break;
                case 'H':
                    HandleH();
                    break;
                case 'J':
                    Add("J", "H");
                    _index += CharAt(_index + 1) == 'J' ? 2 : 1;
                    break;
                case 'P':
                    HandleP();
                    break;
                case 'S':
                    HandleS();
                    break;
                case 'T':
                    HandleT();
                    break;
                case 'W':
                    HandleW();
                    break;
                case 'X':
                    HandleX();
                    break;
                case 'Z':
                    HandleZ();
                    break;
                default:
                    _index++;
                    break;
            }
        }

        private void HandleC()
        {
            if (MatchesAt(_index, "CH"))
            {
                Add("X", "K");
                _index += 2;
            }
            else if (MatchesAt(_index, "CK", "CG", "CQ"))
            {
                Add("K");
                _index += 2;
            }
            else if (MatchesAt(_index, "CI", "CE", "CY"))
            {
                Add("S");
                _index += 2;
            }
            else
            {
                Add("K");
                _index += CharAt(_index + 1) == 'C' ? 2 : 1;
            }
        }

        private void HandleD()
        {
            if (MatchesAt(_index, "DGE", "DGI", "DGY"))
            {
                Add("J");
                _index += 3;
            }
            else if (MatchesAt(_index, "DT", "DD"))
            {
                Add("T");
                _index += 2;
            }
            else
            {
                Add("T");
                _index++;
            }
        }

        private void Add(string primary, string? alternate = null)
        {
            _primary.Append(primary);
            _alternate.Append(alternate ?? primary);
        }

        private char CharAt(int idx)
        {
            return idx >= 0 && idx < _word.Length ? _word[idx] : '\0';
        }

        private bool MatchesAt(int start, params string[] patterns)
        {
            if (start < 0 || start >= _word.Length)
            {
                return false;
            }

            foreach (var pattern in patterns)
            {
                if (
                    start + pattern.Length <= _word.Length
                    && _word.AsSpan(start, pattern.Length).SequenceEqual(pattern.AsSpan())
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsVowel(char ch)
        {
            return ch is 'A' or 'E' or 'I' or 'O' or 'U' or 'Y';
        }
    }
}
