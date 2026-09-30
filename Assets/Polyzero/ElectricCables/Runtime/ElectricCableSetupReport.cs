using System.Collections.Generic;
using System.Text;

namespace Game.Spline
{
    public sealed class ElectricCableSetupReport
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<string> successes = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public IReadOnlyList<string> Successes => successes;
        public bool HasErrors => errors.Count > 0;
        public bool HasWarnings => warnings.Count > 0;
        public bool IsValid => !HasErrors;

        public void Error(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }

        public void Warning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message);
            }
        }

        public void Success(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                successes.Add(message);
            }
        }

        public string ToConsoleString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Electric Cable Setup Report");
            builder.AppendLine("===========================");

            AppendGroup(builder, "Errors", errors);
            AppendGroup(builder, "Warnings", warnings);
            AppendGroup(builder, "OK", successes);

            if (errors.Count == 0 && warnings.Count == 0)
            {
                builder.AppendLine("Setup looks good.");
            }

            return builder.ToString();
        }

        private static void AppendGroup(StringBuilder builder, string title, List<string> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            builder.AppendLine();
            builder.AppendLine(title + ":");
            for (int i = 0; i < items.Count; i++)
            {
                builder.AppendLine("- " + items[i]);
            }
        }
    }
}
