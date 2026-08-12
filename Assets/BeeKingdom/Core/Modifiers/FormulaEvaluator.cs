using System.Collections.Generic;
using System.Globalization;

namespace BeeKingdom.Core.Modifiers
{
    public sealed class FormulaEvaluator
    {
        private string expression;
        private IReadOnlyDictionary<string, double> variables;
        private int index;

        public double EvaluateFormula(string formula, IReadOnlyDictionary<string, double> variables)
        {
            expression = formula ?? string.Empty;
            this.variables = variables ?? new Dictionary<string, double>();
            index = 0;
            return ParseExpression();
        }

        private double ParseExpression()
        {
            double value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+')) value += ParseTerm();
                else if (Match('-')) value -= ParseTerm();
                else return value;
            }
        }

        private double ParseTerm()
        {
            double value = ParseFactor();
            while (true)
            {
                SkipWhitespace();
                if (Match('*')) value *= ParseFactor();
                else if (Match('/'))
                {
                    double divisor = ParseFactor();
                    value = divisor == 0d ? value : value / divisor;
                }
                else return value;
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();
            if (Match('('))
            {
                double value = ParseExpression();
                Match(')');
                return value;
            }

            if (index < expression.Length && (char.IsLetter(expression[index]) || expression[index] == '_'))
            {
                string name = ParseIdentifier();
                return variables.TryGetValue(name, out double value) ? value : 0d;
            }

            return ParseNumber();
        }

        private string ParseIdentifier()
        {
            int start = index;
            while (index < expression.Length && (char.IsLetterOrDigit(expression[index]) || expression[index] == '_'))
            {
                index++;
            }
            return expression.Substring(start, index - start);
        }

        private double ParseNumber()
        {
            int start = index;
            if (index < expression.Length && expression[index] == '-') index++;
            while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
            {
                index++;
            }
            string token = expression.Substring(start, index - start);
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0d;
        }

        private bool Match(char c)
        {
            if (index >= expression.Length || expression[index] != c) return false;
            index++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (index < expression.Length && char.IsWhiteSpace(expression[index])) index++;
        }
    }
}
