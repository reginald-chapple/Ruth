namespace Ruth.Modules.Crm.Domain.People;

public static partial class DoubleMetaphoneEncoder
{
    private sealed partial class EncoderState
    {
        private void HandleG()
        {
            if (CharAt(_index + 1) == 'H')
            {
                if (_index == 0)
                {
                    Add("K");
                }
                else if (_index > 0 && !IsVowel(CharAt(_index - 1)))
                {
                    Add("K");
                }

                _index += 2;
            }
            else if (CharAt(_index + 1) == 'N')
            {
                Add("KN", "N");
                _index += 2;
            }
            else if (MatchesAt(_index + 1, "E", "I", "Y"))
            {
                Add("J", "K");
                _index += 2;
            }
            else
            {
                Add("K");
                _index += CharAt(_index + 1) == 'G' ? 2 : 1;
            }
        }

        private void HandleH()
        {
            var isLeadingOrAfterVowel = _index == 0 || IsVowel(CharAt(_index - 1));
            if (isLeadingOrAfterVowel && IsVowel(CharAt(_index + 1)))
            {
                Add("H");
                _index += 2;
            }
            else
            {
                _index++;
            }
        }

        private void HandleP()
        {
            if (CharAt(_index + 1) == 'H')
            {
                Add("F");
                _index += 2;
            }
            else
            {
                Add("P");
                _index += CharAt(_index + 1) is 'P' or 'B' ? 2 : 1;
            }
        }

        private void HandleS()
        {
            if (MatchesAt(_index, "SH", "SIO", "SIA"))
            {
                Add("X");
                _index += 2;
            }
            else if (MatchesAt(_index, "SCH"))
            {
                Add("SK", "X");
                _index += 3;
            }
            else if (MatchesAt(_index, "SC") && MatchesAt(_index + 2, "I", "E", "Y"))
            {
                Add("S");
                _index += 3;
            }
            else
            {
                Add("S");
                _index += CharAt(_index + 1) is 'S' or 'Z' ? 2 : 1;
            }
        }

        private void HandleT()
        {
            if (MatchesAt(_index, "TION", "TIA", "TCH"))
            {
                Add("X");
                _index += 3;
            }
            else if (MatchesAt(_index, "TH"))
            {
                Add("0", "T");
                _index += 2;
            }
            else
            {
                Add("T");
                _index += CharAt(_index + 1) is 'T' or 'D' ? 2 : 1;
            }
        }

        private void HandleW()
        {
            if (MatchesAt(_index, "WR"))
            {
                Add("R");
                _index += 2;
            }
            else if (_index == 0 && (IsVowel(CharAt(_index + 1)) || MatchesAt(_index, "WH")))
            {
                Add("A", "F");
                _index++;
            }
            else
            {
                _index++;
            }
        }

        private void HandleX()
        {
            if (_index == _word.Length - 1 && MatchesAt(_index - 2, "IAU", "EAU", "AU", "OU"))
            {
                _index++;
                return;
            }

            Add("KS");
            _index += CharAt(_index + 1) is 'C' or 'X' ? 2 : 1;
        }

        private void HandleZ()
        {
            if (CharAt(_index + 1) == 'H')
            {
                Add("J");
                _index += 2;
            }
            else
            {
                Add("S", "TS");
                _index += CharAt(_index + 1) == 'Z' ? 2 : 1;
            }
        }
    }
}
