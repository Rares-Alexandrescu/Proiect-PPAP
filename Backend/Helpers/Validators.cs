using System.Text.RegularExpressions;

namespace Backend.Helpers
{
    public static class Validators
    {
        public static bool EsteEmailValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) 
                return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static bool EsteNumePrenumeValid(string numeSauPrenume)
        {

            if (string.IsNullOrWhiteSpace(numeSauPrenume)) return false;

            return Regex.IsMatch(numeSauPrenume, @"^\p{L}+$");

        }

        public static bool EsteParolaLunga(string parola)
        {
            if (string.IsNullOrWhiteSpace(parola) || parola.Length < 8) return false;
            return true;
        }

        public static bool AreParolaCaracterMare(string parola)
        {
            if (!parola.Any(char.IsUpper)) return false;
            return true;
        }

        public static bool AreParolaCifra(string parola) 
        {
            if (!parola.Any(char.IsDigit)) return false;
            return true;
        }

        public static bool EsteCnpValid(string cnp)
        {
            return !string.IsNullOrWhiteSpace(cnp) && cnp.Length == 13 && cnp.All(char.IsDigit);
        }

        public static bool ParoleleCoincid(string parolaConfirmare, string parola)
        {
            return string.Equals(parola, parolaConfirmare);
        }
    }
}