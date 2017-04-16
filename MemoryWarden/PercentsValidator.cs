using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MemoryWarden
{
    class PercentsValidator : ValidationRule
    {
        //Validates that the user types numbers into the threshold cells.

        public PercentsValidator()
        {
            this.ValidationStep = ValidationStep.RawProposedValue;
        }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int result;
            if (int.TryParse(value.ToString(), out result)) {
                if (result > 100) return new ValidationResult(false, "Percents can't be over 100.");
                if (result < 0) return new ValidationResult(false, "Negative percents not allowed.");
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "Could not convert \"" + value.ToString() + "\" to int.");
        }
    }
}
