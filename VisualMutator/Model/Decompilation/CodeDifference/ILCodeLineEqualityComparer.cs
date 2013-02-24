namespace VisualMutator.Model.Decompilation.CodeDifference
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class ILCodeLineEqualityComparer : IEqualityComparer<string>
    {
        private IEqualityComparer<string> baseComparer = EqualityComparer<string>.Default;

        public bool Equals(string x, string y)
        {
            return baseComparer.Equals(
                NormalizeLine(x),
                NormalizeLine(y));
        }

        public int GetHashCode(string obj)
        {
            return baseComparer.GetHashCode(NormalizeLine(obj));
        }

        private string NormalizeLine(string line)
        {
            //IL_00ca: 

            
            line = line.Trim();

            var regex = new Regex(@"^IL_[\D\d]*: ");

            line = regex.Replace(line, "");

            var index = line.IndexOf("//");
            if (index >= 0)
            {
                return line.Substring(0, index);
            }
            else if (line.StartsWith("#"))
            {
                return string.Empty;
            }
            else
            {
                return line;
            }

        }
    }
}